// 内核已分配模块的Exec绑定事务：代码与字典在绑定点固定，公开token仅由Runtime签发。
// Host先登记peer的双向身份，再调用ExecuteBound；旧函数持State，允许旧peer回流同一环境。
using System;
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;

public static partial class PyModuleInstances
{
    private static readonly ConditionalWeakTable<IPyForeignObject, PyModuleInstanceState> ForeignBindings = new();

    /// <summary>捕获当前已登记Instance代码的不可变token；以后重登记同名不会改其执行体。</summary>
    public static object CaptureRegisteredCode(string name) => PyModules.RegisteredInstanceCode(name);

    /// <summary>绑定内核已创建对象；不执行正文、不初始化或覆盖内核已经写好的字典键。</summary>
    public static PyModule BindForeign(object codeToken, IPyModuleNamespace globals, IPyForeignObject foreignModule)
    {
        if (codeToken is not PyCompiledModuleCode code) throw new PyRuntimeError("invalid compiled module code token");
        ArgumentNullException.ThrowIfNull(globals); ArgumentNullException.ThrowIfNull(foreignModule);
        if (globals.ViewObject is null) throw new PyRuntimeError("module namespace has no real dictionary view");
        if (ForeignBindings.TryGetValue(foreignModule, out var previous))
        {
            if (!ReferenceEquals(previous.Code, code) || !ReferenceEquals(previous.ForeignGlobals, globals))
                throw new PyRuntimeError("foreign module is already bound to a different code or namespace");
            return RestoreForeign(previous, foreignModule);
        }
        var state = new PyModuleInstanceState(code, globals, foreignModule);
        ForeignBindings.Add(foreignModule, state);
        return RestoreForeign(state, foreignModule);
    }

    /// <summary>取得可存入Host弱表的状态token；旧函数owner继续保有该状态。</summary>
    public static object CaptureBinding(PyModule module) => States.TryGetValue(module, out var state) && state.ForeignGlobals is not null
        ? state : throw new PyRuntimeError("module has no foreign binding");

    /// <summary>按Runtime签发状态和原外来对象身份恢复peer；不读取可改模块名或当前注册表。</summary>
    public static PyModule RestoreForeign(object bindingToken, IPyForeignObject foreignModule)
    {
        if (bindingToken is not PyModuleInstanceState { ForeignGlobals: not null, Code: { } code } state
            || !ReferenceEquals(state.ForeignModule, foreignModule))
            throw new PyRuntimeError("invalid foreign module binding or identity");
        if (state.Peer is { } weak && weak.TryGetTarget(out var existing)) return existing;
        var module = new PyModule(code.Name, code.Package, code.File, () => ExecuteBoundState(state),
            state.GetAttr, (_, name, value) => state.SetAttr(name, value), instanceGlobals: true)
            { ForeignObject = foreignModule, State = state.BoundStatus == 2 ? 2 : 1 };
        state.Peer = new(module); States.Add(module, state);
        return module;
    }

    /// <summary>外来Exec精确一次；重入观察部分状态，成功重复不执行，失败重复明确拒绝。</summary>
    public static void ExecuteBound(PyModule module)
    {
        if (!States.TryGetValue(module, out var state) || state.ForeignGlobals is null)
            throw new PyRuntimeError("module has no foreign execution binding");
        ExecuteBoundState(state);
        if (state.BoundStatus == 2) module.State = 2;
    }
    // Host须持解释器GIL和内核per-module导入锁；status=1只用于同一导入事务的循环重入，
    // 不授予跨线程等待/完成语义。这里不另造一套与内核导入锁竞争的锁系统。
    private static void ExecuteBoundState(PyModuleInstanceState state)
    {
        int previous = System.Threading.Interlocked.CompareExchange(ref state.BoundStatus, 1, 0);
        if (previous is 1 or 2) return;
        if (previous == 3) throw new PyRuntimeError("foreign module initialization previously failed");
        try { Execute(state); state.BoundStatus = 2; }
        catch { state.BoundStatus = 3; throw; }
    }

    /// <summary>为Host构造strict builtins提供独立字典；规范函数值身份稳定，写副本不改普通环境。</summary>
    public static PyDict CreateCanonicalBuiltins() => PyModuleBuiltins.Create();
}

public static partial class PyModules
{
    internal static PyCompiledModuleCode RegisteredInstanceCode(string name)
    {
        if (!Registry.TryGetValue(name, out var registration)) throw new PyModuleNotFoundError($"No module named '{name}'");
        return registration.InstanceCode ?? throw new PyNotImplementedError("registered module has no instance execution capability");
    }

    /// <summary>查询权威sys.modules三态；无strict能力时保普通Runtime缓存的既有模块域。</summary>
    public static bool TryGetSysModuleEntry(string name, out object? entry)
    {
        if (PyProvider.Current is IPyStrictImportProvider strict) return strict.TryGetSysModuleEntry(name, out entry);
        bool found = Cache.TryGetValue(name, out var module); entry = module; return found;
    }

    /// <summary>strict原始导入能力，任意替换值保原对象；旧Import返回PyModule的ABI不变。</summary>
    public static object ImportLevel(string name, object? globals, object?[] fromlist, long level) =>
        PyProvider.Current is IPyStrictImportProvider strict ? strict.ImportLevel(name, globals, fromlist, level)
            : throw new PyNotImplementedError("strict import capability is not installed");
}
