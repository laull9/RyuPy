// 真实globals模块的固定C API现场：CIL执行体经单次原生thunk进入，结束时立即释放托管token。
// native保留的frame/code/thunk只有已失效capsule，不能持有执行体、Host或失效GCHandle。
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyCompiledExecutionProvider
{
    private int callCarrierHandles;
    private bool callCarrierUsed, callCarrierPreShutdownGc;
    private sealed class CompiledExecution(CPythonHost host, Action body)
    {
        internal readonly CPythonHost Host = host;
        internal readonly Action Body = body;
        internal int Released;
    }
    /// <inheritdoc />
    public void ExecuteCompiledModule(PyCompiledExecutionRequest request)
    {
        if (request is null || !request.TryConsume(this, out Action? body) || body is null)
            throw new PyRuntimeError("invalid compiled execution request");
        if (request.Globals is not PythonObject globals || !ReferenceEquals(globals.Host, this))
            throw new PyTypeError("compiled execution requires the provider's real globals");
        ExecuteWithCallCarrier(globals, request.File, body);
    }
    internal void ExecuteWithCallCarrier(PythonObject globals, string file, Action body) =>
        RunCallCarrier(globals, file, body);

    private bool TryCallCarrier<T>(Func<T> operation, out T result)
    {
        result = default!;
        int kind = Native.rp_carrier_frame_context(out nint currentGlobals);
        if (kind < 0) ThrowError();
        try
        {
            if (kind == 1) return false; // 真正Python调用现场优先，不被外层CIL元数据覆盖。
            int source = PyTraceback.CompiledNativeCallSite(out object? globals, out string? file);
            if (source is 2 or 3) return false; // Legacy/未知旧来源保原执行，新profile独立提供权威D。
            PythonObject? dictionary = null;
            if (source == 1)
            {
                if (globals is not PythonObject value) return false; // 旧NativeGlobals PyDict不取得新frame能力。
                if (!ReferenceEquals(value.Host, this)) throw new PyTypeError("native caller globals belong to a different execution domain");
                dictionary = value;
                if (kind == 2 && currentGlobals == dictionary.Pointer) return false;
            }
            else if (kind != 0 || Native.rp_external_carrier_available() == 0) return false;
            T captured = default!;
            RunCallCarrier(dictionary, file, () => captured = ExecutePythonCall(operation));
            result = captured; return true;
        }
        finally { Native.rp_decref(currentGlobals); }
    }
    private unsafe nint MakeNativeMetadataText(string text)
    {
        string points = PyOps.UnicodeCodePoints(text);
        fixed (char* units = points) return Check(Native.rp_text_codepoints(units, points.Length));
    }
    private unsafe void RunCallCarrier(PythonObject? globals, string? file, Action body)
    {
        using Lease lease = Enter();
        using var handled = new PythonHandledScope(this);
        if (globals is not null && (!ReferenceEquals(globals.Host, this) || Native.rp_container_kind(globals.Pointer) != 2))
            throw new PyTypeError("compiled execution requires exact globals dict");
        callCarrierUsed = true;
        var execution = new CompiledExecution(this, body);
        GCHandle handle = GCHandle.Alloc(execution);
        Interlocked.Increment(ref callCarrierHandles);
        nint source = 0;
        try
        {
            nint result;
            if (globals is null)
                result = Native.rp_execute_external_carrier(GCHandle.ToIntPtr(handle), &InvokeCompiledExecution, &ReleaseCompiledExecution);
            else
            {
                source = MakeNativeMetadataText(file!);
                result = Native.rp_execute_carrier(globals.Pointer, source, GCHandle.ToIntPtr(handle),
                    &InvokeCompiledExecution, &ReleaseCompiledExecution);
            }
            Native.rp_decref(Check(result));
        }
        finally
        {
            Native.rp_decref(source);
            // P/Invoke解析或source转换失败时native尚未消费；其余路径由native先失效再释放token。
            if (Interlocked.Exchange(ref execution.Released, 1) == 0)
            { handle.Free(); Interlocked.Decrement(ref callCarrierHandles); }
            GC.KeepAlive(globals); GC.KeepAlive(execution);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ReleaseCompiledExecution(nint token)
    {
        try
        {
            GCHandle handle = GCHandle.FromIntPtr(token);
            var execution = (CompiledExecution)handle.Target!;
            if (Interlocked.Exchange(ref execution.Released, 1) == 0)
            { handle.Free(); Interlocked.Decrement(ref execution.Host.callCarrierHandles); }
        }
        catch { Environment.FailFast("compiled execution token ownership corruption"); }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint InvokeCompiledExecution(nint token)
    {
        var execution = (CompiledExecution)GCHandle.FromIntPtr(token).Target!;
        CPythonHost host = execution.Host;
        try
        {
            // 该能力仅Runtime模块入口签发；保持现入口弱登记，不经普通第三方proxy的Pause路径。
            using Lease lease = host.Enter(callback: true);
            using var handled = new ManagedHandledScope(host);
            execution.Body(); return Native.rp_none();
        }
        catch (Exception error)
        {
            try { host.RestoreCallbackError(error as PyBaseException ?? PyErrors.FromClr(error)); }
            catch { Native.rp_error_text("RuntimeError", "compiled execution callback error export failed"); }
            return 0;
        }
    }
    private void WriteCallCarrierReport(System.Text.Json.Utf8JsonWriter writer)
    {
        // 固定native纯计数；关闭后不取GIL，不读取任何CP对象或weakref。
        Native.rp_carrier_stats(out long live, out long calls);
        Native.rp_code_origin_stats(out long origins, out long active);
        writer.WriteStartObject("call_carriers");
        writer.WriteNumber("native_live", live); writer.WriteNumber("native_calls", calls);
        writer.WriteNumber("managed_handles", Volatile.Read(ref callCarrierHandles));
        writer.WriteBoolean("pre_shutdown_gc", callCarrierPreShutdownGc);
        writer.WriteNumber("code_origin_records", origins); writer.WriteNumber("code_origin_active", active);
        writer.WriteEndObject();
    }

}

internal static unsafe partial class Native
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_execute_carrier(nint globals, nint filename, nint token,
        delegate* unmanaged[Cdecl]<nint, nint> invoke, delegate* unmanaged[Cdecl]<nint, void> release);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void rp_carrier_stats(out long live, out long calls);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void rp_code_origin_stats(out long records, out long active);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_carrier_frame_context(out nint globals);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_external_carrier_available();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_execute_external_carrier(nint token,
        delegate* unmanaged[Cdecl]<nint, nint> invoke, delegate* unmanaged[Cdecl]<nint, void> release);
}
