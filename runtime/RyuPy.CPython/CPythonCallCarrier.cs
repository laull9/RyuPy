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
        using Lease lease = Enter();
        nint builtins = ExportCapturedBuiltins(request.Builtins);
        try { RunCallCarrier(globals, builtins, request.File, body); }
        finally { Native.rp_decref(builtins); GC.KeepAlive(request); }
    }
    internal void ExecuteWithCallCarrier(PythonObject globals, string file, Action body) =>
        RunCallCarrier(globals, 0, file, body);

    // 保留Runtime字面量dict的既有映射身份；只借单次raw owned，不建立另一份定义点缓存。
    private nint ExportCapturedBuiltins(object view)
    {
        if (view is PythonObject foreign && !ReferenceEquals(foreign.Host, this))
            throw new PyTypeError("native caller builtins belong to a different execution domain");
        nint pointer = Check(ToPython(view));
        // Runtime精确PyDict还可能按显式proxy合同导出mapping；沿原投影，不新增配置限制。
        if (view is PyDict || Native.rp_container_kind(pointer) == 2) return pointer;
        Native.rp_decref(pointer);
        throw new PyTypeError("compiled execution requires captured dictionary builtins");
    }

    private bool TryCallCarrier<T>(Func<T> operation, out T result)
    {
        result = default!;
        int kind = Native.rp_carrier_frame_context_builtins(out nint currentGlobals, out nint currentBuiltins);
        try
        {
            if (kind < 0) ThrowError();
            // Python内部协议继续沿真实frame；只有回调入口后新压入的可信CIL帧才取得切换权。
            if (kind == 1 && !HasNewCompiledCarrierFrame) return false;
            int source = PyTraceback.CompiledNativeCallSite(out object? globals, out object? builtins, out string? file);
            if (source is 2 or 3) return false; // Legacy/未知旧来源保原执行，新profile独立提供权威D。
            PythonObject? dictionary = null;
            if (source == 1)
            {
                if (globals is not PythonObject value) return false; // 旧NativeGlobals PyDict不取得新frame能力。
                if (!ReferenceEquals(value.Host, this)) throw new PyTypeError("native caller globals belong to a different execution domain");
                dictionary = value;
            }
            else if (kind != 0 || Native.rp_external_carrier_available() == 0) return false;
            nint expectedBuiltins = dictionary is null ? 0 : ExportCapturedBuiltins(builtins!);
            try
            {
                if (kind == 2 && dictionary is not null
                    && currentGlobals == dictionary.Pointer && currentBuiltins == expectedBuiltins)
                    return false;
                T captured = default!;
                RunCallCarrier(dictionary, expectedBuiltins, file, () => captured = ExecutePythonCall(operation));
                result = captured; return true;
            }
            finally { Native.rp_decref(expectedBuiltins); GC.KeepAlive(builtins); }
        }
        finally { Native.rp_decref(currentGlobals); Native.rp_decref(currentBuiltins); }
    }
    private unsafe nint MakeNativeMetadataText(string text)
    {
        string points = PyOps.UnicodeCodePoints(text);
        fixed (char* units = points) return Check(Native.rp_text_codepoints(units, points.Length));
    }
    private unsafe void RunCallCarrier(PythonObject? globals, nint builtins, string? file, Action body)
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
                result = Native.rp_execute_carrier_builtins(globals.Pointer, builtins, source, GCHandle.ToIntPtr(handle),
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
            using var boundary = new ManagedCarrierCallbackScope(PyTraceback.CaptureDepth());
            using var handled = new ManagedHandledScope(host);
            execution.Body(); return Native.rp_none();
        }
        catch (Exception error)
        {
            using var boundary = new ManagedCarrierCallbackScope(PyTraceback.CaptureDepth());
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
    internal static extern nint rp_execute_carrier_builtins(nint globals, nint builtins, nint filename, nint token,
        delegate* unmanaged[Cdecl]<nint, nint> invoke, delegate* unmanaged[Cdecl]<nint, void> release);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void rp_carrier_stats(out long live, out long calls);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void rp_code_origin_stats(out long records, out long active);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_carrier_frame_context(out nint globals);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_carrier_frame_context_builtins(out nint globals, out nint builtins);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_external_carrier_available();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_execute_external_carrier(nint token,
        delegate* unmanaged[Cdecl]<nint, nint> invoke, delegate* unmanaged[Cdecl]<nint, void> release);
}
