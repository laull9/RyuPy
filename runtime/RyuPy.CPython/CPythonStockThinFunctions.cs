// 显式 stock A 薄函数产品 seam：纯 core 的 native 强根与 H 的 owned F 分开。
// 没有配置默认开关或编译器入口；启用、创建、回流、回调和关闭均走现有 Host/GIL 合同。
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyStockThinFunctionProvider
{
    private bool stockThinEnabled;
    private List<WeakReference<StockThinFunctionCore>>? stockThinCores;
    private static int stockThinRoots;
    internal static int StockThinOwnedHandles;

    internal unsafe void EnableStockThinFunctions()
    {
        using Lease lease = Enter();
        if (stockThinEnabled) return;
        if (Native.rp_thin_enable(&InvokeStockThin, &ReleaseStockThin) < 0) ThrowError();
        stockThinEnabled = true;
    }

    // 使用原 F binder；容器导出/关键字准备可能重入，故验证放在实参转换完成之后。
    // 仅候选 H 使用此入口，普通 PythonObject 的布局和调用热路完全保留。
    internal object? CallStockThinFunction(PythonObject function, object?[] args,
        string[] names, object?[] values) => PythonCall(() =>
    {
        object?[] positional = [.. args], keywordValues = [.. values];
        string[] keywordNames = [.. names];
        nint[] keywordPointers = new nint[keywordNames.Length];
        nint tuple = MakeTuple(positional), keywords = 0;
        try
        {
            if (keywordNames.Length != 0)
            {
                keywords = Check(Native.rp_op(16));
                for (int i = 0; i < keywordNames.Length; i++)
                {
                    nint value = ToPython(keywordValues[i]);
                    try
                    {
                        Native.rp_decref(Check(Native.rp_op(17, keywords, value, s: keywordNames[i])));
                        // 独立 owned：重复关键字覆盖旧字典值时，也没有已失效的地址缓存。
                        keywordPointers[i] = Native.rp_incref(value);
                    }
                    finally { Native.rp_decref(value); }
                }
            }
            if (Native.rp_thin_validate(function.Pointer) < 0) ThrowError();
            nint result = Check(Native.rp_op(5, function.Pointer, tuple, keywords));
            try
            {
                // 实际 CP 返回对象就是输入时，保留 CLR 原对象，避免标量导入重新装盒丢别名。
                for (int i = 0; i < positional.Length; i++)
                {
                    nint item = Check(Native.rp_op(18, tuple, n: i));
                    try { if (item == result) return positional[i] ?? PyNone.Instance; }
                    finally { Native.rp_decref(item); }
                }
                for (int i = keywordPointers.Length - 1; i >= 0; i--)
                    if (keywordPointers[i] == result) return keywordValues[i] ?? PyNone.Instance;
                return FromOwned(Native.rp_incref(result));
            }
            finally { Native.rp_decref(result); }
        }
        finally
        {
            Native.rp_decref(tuple); Native.rp_decref(keywords);
            foreach (nint pointer in keywordPointers) Native.rp_decref(pointer);
        }
    });

    internal StockThinFunctionHandle CreateStockThinFunction(object globals,
        StockThinFunctionDefinition definition, StockThinFunctionBody body, int failure = 0) => PythonCall(() =>
    {
        if (!stockThinEnabled) throw new PyNotImplementedError("stock 薄函数候选尚未显式启用");
        // 在分配 GCHandle 前核验静态目标和 globals 转换，失败没有移交责任。
        var core = new StockThinFunctionCore(definition, body);
        return CreateStockThinFunctionCore(globals, core, failure);
    });

    object IPyStockThinFunctionProvider.CreateStockThinFunction(object globals,
        PyStockThinDefinition definition, PyStockThinBody body) => PythonCall(() =>
    {
        if (!stockThinEnabled) throw new PyNotImplementedError("stock 薄函数候选尚未显式启用");
        // 编译 ABI 只能使用当前 Host 的权威精确 dict；禁止将 CLR PyDict 同步成另一份环境。
        // 原入口存储选择错误应立即失败，不能由转换器补出含 H/owned F 的跨堆回边。
        if (globals is not PythonObject dictionary || !ReferenceEquals(dictionary.Host, this)
            || Native.rp_container_kind(dictionary.Pointer) != 2)
            throw new PyTypeError("compiled stock function requires the host's real globals dictionary");
        var source = new StockThinFunctionDefinition(definition.EntryId, definition.Module,
            definition.Name, definition.SourcePath, definition.SourceLine);
        return CreateStockThinFunctionCore(globals, StockThinFunctionCore.CreateCompiled(source, body), 0);
    });

    private StockThinFunctionHandle CreateStockThinFunctionCore(object globals, StockThinFunctionCore core, int failure)
    {
        StockThinFunctionDefinition definition = core.Definition;
        // 仅候选创建时分配弱观察表；移除已死项，避免诊断记录随运行次数无限增长。
        stockThinCores ??= [];
        stockThinCores.RemoveAll(static weak => !weak.TryGetTarget(out _));
        stockThinCores.Add(new(core));
        nint dictionary = ToPython(globals), source = 0, displayName = 0;
        try
        {
            source = MakeNativeMetadataText(definition.SourcePath);
            displayName = ToPython(definition.Name);
            nint token = GCHandle.ToIntPtr(GCHandle.Alloc(core));
            Interlocked.Increment(ref stockThinRoots);
            // make 在所有返回路径消费 token；托管端绝不二次 Free。
            nint f = Check(Native.rp_thin_make_source(dictionary, token, failure, source, definition.SourceLine, displayName));
            try
            {
                nint name = ToPython(definition.Name);
                try
                {
                    Native.rp_decref(Check(Native.rp_op(3, f, name, s: "__name__")));
                    Native.rp_decref(Check(Native.rp_op(3, f, name, s: "__qualname__")));
                }
                finally { Native.rp_decref(name); }
                return (StockThinFunctionHandle)FromOwned(Native.rp_incref(f))!;
            }
            catch { Native.rp_thin_abort(f); throw; }
            finally { Native.rp_decref(f); }
        }
        finally { Native.rp_decref(displayName); Native.rp_decref(source); Native.rp_decref(dictionary); }
    }

    // 从真实 F 查 owner 的 owned 身份，不向普通 objects/reverse/Roots 强根表登记 H。
    private bool TryFromStockThinOwned(nint pointer, out object? result)
    {
        result = null;
        if (!stockThinEnabled) return false;
        nint token = Native.rp_thin_token(pointer);
        if (token == 0) return false;
        try
        {
            var core = (StockThinFunctionCore)GCHandle.FromIntPtr(token).Target!;
            result = core.AcquirePublicHandle(() => new StockThinFunctionOwner(
                new PythonObject(this, Native.rp_incref(pointer))));
            return true;
        }
        finally { Native.rp_decref(pointer); }
    }

    internal (long NativeLive, long Destroyed, long Calls, int CoreRoots, int OwnedHandles, int LiveCores) StockThinStatistics => PythonCall(() =>
    {
        if (!stockThinEnabled) return (0L, 0L, 0L, 0, 0, 0);
        Native.rp_thin_stats(out long live, out long destroyed, out long calls);
        stockThinCores?.RemoveAll(static weak => !weak.TryGetTarget(out _));
        return (live, destroyed, calls, Volatile.Read(ref stockThinRoots),
            Volatile.Read(ref StockThinOwnedHandles), stockThinCores?.Count ?? 0);
    });

    // 有限内部关闭：持有 H、候选 F/P/code 或活动调用时保留完整宿主，让调用方释放后重试。
    // 检查在 state=2 的关闭门内执行，失败恢复 state=1；不会清模块缓存来凑零。
    private void CheckStockThinShutdown()
    {
        if (!stockThinEnabled) return;
        using Lease lease = Enter();
        Native.rp_thin_stats(out long live, out _, out _);
        if (live != 0 || Volatile.Read(ref stockThinRoots) != 0 || Volatile.Read(ref StockThinOwnedHandles) != 0)
            throw new InvalidOperationException("stock 薄函数仍有存活引用，释放并回收后重试关闭");
        if (Native.rp_thin_clear() < 0) ThrowError();
        stockThinEnabled = false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ReleaseStockThin(nint token)
    {
        // native 析构只释放纯 core 根；不依赖 Host 生命周期、不调用 CP API、不让异常穿 ABI。
        try { GCHandle.FromIntPtr(token).Free(); Interlocked.Decrement(ref stockThinRoots); }
        catch { Environment.FailFast("stock core token 所有权损坏"); }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint InvokeStockThin(nint token, nint function, nint argument)
    {
        var host = Instance;
        Lease? lease = null;
        CallbackScope? callback = null;
        ManagedHandledScope? handled = null;
        ProgramOwnedScope? owned = null;
        StockThinFunctionCallLease? environment = null;
        long depth = PyTraceback.CaptureDepth();
        nint result = 0;
        try
        {
            if (host is null) throw new PyRuntimeError("RyuPy host is closed");
            lease = host.Enter(callback: true);
            owned = host.BeginStockProgramCallback();
            callback = new CallbackScope(true);
            handled = new ManagedHandledScope(host);
            environment = new StockThinFunctionCallLease(host, function);
            var core = (StockThinFunctionCore)GCHandle.FromIntPtr(token).Target!;
            host.PullContainers();
            object? value;
            using (new StockBodyScope(environment))
            using (new ManagedCarrierCallbackScope(depth))
                value = core.Invoke(environment, environment.ConvertBorrowed(argument));
            host.PushContainers();
            result = environment.ExportResult(value);
        }
        catch (PyBaseException error)
        {
            if (host is not null && lease is not null) host.RestoreCallbackError(error);
            else Native.rp_error_text(error.PyTypeName, error.Message);
        }
        catch (Exception error)
        {
            if (host is not null) host.stockCallbackFailure = error.ToString();
            if (host is not null && lease is not null) host.RestoreCallbackError(PyErrors.FromClr(error));
            else Native.rp_error_text("RuntimeError", error.Message);
        }
        finally
        {
            // 完整 catch 边界包括 cleanup；任何清理异常转换为 CP 错误，禁止越过 reverse P/Invoke。
            try
            {
                try { environment?.Dispose(); }
                finally
                {
                    try { handled?.Dispose(); }
                    finally
                    {
                        try { callback?.Dispose(); }
                        finally
                        {
                            try { PyTraceback.PopTo(depth + 1); }
                            finally { try { owned?.Dispose(); } finally { lease?.Dispose(); } }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                Native.rp_decref(result); result = 0;
                Native.rp_error_text("RuntimeError", error.Message);
            }
        }
        return result;
    }
}

// owner 仅 H 强持；PythonObject 终结器使用 Host 现有 owned 释放队列，不新增 finalizer C API。
internal sealed class StockThinFunctionOwner : IStockThinFunctionOwner, IDisposable
{
    internal PythonObject Function { get; }
    private int disposed;
    internal StockThinFunctionOwner(PythonObject function)
    {
        Function = function;
        Interlocked.Increment(ref CPythonHost.StockThinOwnedHandles);
        function.Host.ObserveStockProgramHandle(this);
    }
    ~StockThinFunctionOwner() => Dispose();
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        // owned F 只移交现有队列；holder 计数与尚待 drain 的 CP 引用分别观察。
        Function.Dispose(); Interlocked.Decrement(ref CPythonHost.StockThinOwnedHandles);
        GC.SuppressFinalize(this);
    }
    public IStockThinFunctionLease AcquireCall() => new StockThinFunctionCallLease(Function.Host, Function.Pointer);
}

internal sealed class StockThinFunctionCallLease : IStockThinFunctionLease
{
    private readonly CPythonHost host;
    private readonly CPythonHost.Lease lease;
    private nint function;
    // 单次调用的身份租约：标量转换也保留原 CP 对象，返回同一托管值时归还原指针。
    // 记录只存在活动栈，不进入 core；每项独立 owned，覆盖任意转换/重入直至结果导出。
    private readonly List<(object? Value, nint Pointer)> values = [];
    internal StockThinFunctionCallLease(CPythonHost host, nint function)
    {
        this.host = host;
        lease = host.Enter(callback: true);
        this.function = Native.rp_incref(function);
    }
    internal int FrameBuiltinKind(nint value)
    {
        if (function == 0) throw new ObjectDisposedException("stock function lease");
        int kind = Native.rp_thin_frame_builtin(function, value);
        if (kind == -2) host.ThrowError();
        return kind;
    }
    internal int FrameTarget(nint value, out nint bound)
    {
        int kind = Native.rp_thin_frame_target(function, value, out bound);
        if (kind == -2) host.ThrowError();
        return kind;
    }
    internal int FrameCall(nint callable, nint args, nint kwargs)
    {
        int action = Native.rp_thin_frame_call(function, callable, args, kwargs);
        if (action < 0) host.ThrowError();
        return action;
    }
    internal object GlobalsForBuiltin()
    {
        if (function == 0) throw new ObjectDisposedException("stock function lease");
        return host.FromOwned(host.Check(Native.rp_op(2, function, s: "__globals__")))!;
    }
    public void Validate(StockThinFunctionDefinition definition)
    {
        if (function == 0) throw new ObjectDisposedException("stock function lease");
        if (Native.rp_thin_validate(function) < 0) host.ThrowError();
    }
    public object? LoadGlobal(string name)
    {
        if (function == 0) throw new ObjectDisposedException("stock function lease");
        return ConvertOwned(host.Check(Native.rp_thin_global(function, name)));
    }
    internal object? ConvertBorrowed(nint pointer) => ConvertOwned(Native.rp_incref(pointer));
    private object? ConvertOwned(nint pointer)
    {
        try
        {
            object? value = host.FromBorrowed(pointer);
            values.Add((value, pointer)); pointer = 0;
            return value;
        }
        finally { Native.rp_decref(pointer); }
    }
    internal nint ExportResult(object? value)
    {
        for (int i = values.Count - 1; i >= 0; i--)
            if (ReferenceEquals(value, values[i].Value)) return Native.rp_incref(values[i].Pointer);
        return host.ToPython(value);
    }
    public void Dispose()
    {
        if (function == 0) return;
        nint owned = function; function = 0;
        foreach (var value in values) Native.rp_decref(value.Pointer);
        values.Clear();
        Native.rp_decref(owned); lease.Dispose();
    }
}
