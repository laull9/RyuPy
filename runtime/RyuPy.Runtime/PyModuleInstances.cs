// 编译模块实例冷路：代码描述不持实例，唯一真实globals由模块委托及执行context共同持有。
using System;
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;

/// <summary>编译器实例入口ABI；参数是每次执行独立捕获builtins的内部context。</summary>
public delegate void PyModuleExecute(object state);

// 不可变代码身份；注册替换不会改动已创建实例的代码及来源。
internal sealed record PyCompiledModuleCode(string Name, string Package, string File,
    bool IsPackage, PyModuleExecute Execute);

internal sealed class PyModuleInstanceState(PyCompiledModuleCode? code)
{
    internal readonly PyCompiledModuleCode? Code = code;
    internal readonly PyDict Globals = new();
    internal object? GetAttr(string name) => name == "__dict__" ? Globals
        : Globals.TryGet(name, out var value) ? value
        : throw MissingAttr(name);
    // 只认当前精确str名，不转换任意对象，不借不可变注册身份覆盖Python元数据。
    internal PyAttributeError MissingAttr(string name) => new(
        Globals.TryGet("__name__", out var current) && current is string moduleName
            ? $"module '{moduleName}' has no attribute '{name}'"
            : $"module has no attribute '{name}'");
    internal void SetAttr(string name, object? value)
    {
        if (name == "__dict__") throw new PyAttributeError("readonly attribute");
        Globals.Set(name, value);
    }
}

// 每次执行独立捕获引用而非复制字典；重入执行不会改变外层frame的builtins环境。
internal sealed record PyModuleExecutionContext(PyModuleInstanceState State, PyDict Builtins);

/// <summary>CLR宿主与编译器实例ABI；公开loader冷路复用真实字典执行机制。</summary>
public static partial class PyModuleInstances
{
    private static readonly ConditionalWeakTable<PyModule, PyModuleInstanceState> States = new();

    /// <summary>从当前注册的不可变代码描述创建未缓存、未执行的独立实例。</summary>
    public static PyModule Create(string registeredName) => PyModules.CreateRegisteredInstance(registeredName);

    internal static PyModule Create(PyCompiledModuleCode code)
    {
        var state = new PyModuleInstanceState(code);
        var metadata = new PyModuleMetadata(code.Name, code.File, code.IsPackage, code);
        state.Globals.Set("__name__", code.Name); state.Globals.Set("__package__", code.Package);
        state.Globals.Set("__file__", code.File); state.Globals.Set("__doc__", PyNone.Instance);
        state.Globals.Set("__builtins__", PyModuleBuiltins.Default);
        foreach (string name in new[] { "__spec__", "__loader__", "__cached__", "__path__" })
        {
            object? value = metadata.Initial(name);
            if (value is not PyUnbound) state.Globals.Set(name, value);
        }
        // 复用原getter/setter，legacy属性热路不增加CWT查询或实例state字段。
        var module = new PyModule(code.Name, code.Package, code.File, () => Execute(state),
            state.GetAttr, (_, name, value) => state.SetAttr(name, value), fileBackedByGlobal: true, instanceGlobals: true)
            { Metadata = metadata };
        States.Add(module, state);
        return module;
    }

    // 默认模块创建只分配真实存储与五个基本键，不提前插入builtins或绑定来源代码。
    internal static PyModule CreateEmpty(string name)
    {
        var state = new PyModuleInstanceState(null);
        state.Globals.Set("__name__", name);
        foreach (string key in new[] { "__doc__", "__package__", "__loader__", "__spec__" })
            state.Globals.Set(key, PyNone.Instance);
        var module = new PyModule(name, "", "", () => Execute(state), state.GetAttr,
            (_, key, value) => state.SetAttr(key, value), instanceGlobals: true);
        States.Add(module, state);
        return module;
    }

    // 执行来源只由loader提供，目标只提供globals；不能误执行目标自己的默认import代码。
    internal static void Execute(PyCompiledModuleCode code, PyModule target)
    {
        if (!States.TryGetValue(target, out var state))
            throw new PyNotImplementedError("module has no instance globals capability");
        Execute(code, state);
    }

    /// <summary>每次原地执行，不清未覆盖键、不操作导入缓存、不回滚已写入状态。</summary>
    public static void Execute(PyModule module)
    {
        if (!States.TryGetValue(module, out var state))
            throw new PyNotImplementedError("module has no instance execution capability");
        Execute(state);
    }
    private static void Execute(PyModuleInstanceState state) => Execute(state.Code
        ?? throw new PyNotImplementedError("module has no default import code"), state);

    private static void Execute(PyCompiledModuleCode code, PyModuleInstanceState state)
    {
        // 与exec一致：缺键时在正文前真实补入共享默认环境；已有非法值不能被fallback吞掉。
        if (!state.Globals.TryGet("__builtins__", out object? value))
        {
            value = PyModuleBuiltins.Default;
            state.Globals.Set("__builtins__", value);
        }
        if (value is not PyDict builtins)
            throw new PyNotImplementedError("instance module __builtins__ must be a dictionary");
        code.Execute(new PyModuleExecutionContext(state, builtins));
    }
    private static PyModuleExecutionContext Context(object state) => state as PyModuleExecutionContext
        ?? throw new PyRuntimeError("invalid compiled module execution context");

    /// <summary>Python模块名查找：唯一globals，随后本次执行捕获的真实builtins。</summary>
    public static object? LoadName(object state, string name)
    {
        var context = Context(state);
        if (context.State.Globals.TryGet(name, out var value) || context.Builtins.TryGet(name, out value)) return value;
        if (ReferenceEquals(context.Builtins, PyModuleBuiltins.Default) && PyModuleBuiltins.IsUnsupported(name))
            throw new PyNotImplementedError($"builtin '{name}' is not implemented");
        throw new PyNameError($"name '{name}' is not defined");
    }
    /// <summary>所有公开名字写入唯一globals，不经过第二份镜像。</summary>
    public static void StoreName(object state, string name, object? value) => Context(state).State.Globals.Set(name, value);
    /// <summary>来源保存实例state，绝不按模块名字回注册表寻找另一实例。</summary>
    public static object ExecutionSource(object state) => PyTraceback.CreateModuleExecutionSource(Context(state).State);
    internal static bool IsInstance(PyModule module) => States.TryGetValue(module, out _);
    internal static void DeleteAttr(PyModule module, string name)
    {
        if (!States.TryGetValue(module, out var state))
            throw new PyNotImplementedError("module attribute deletion requires global-slot synchronization");
        if (name == "__dict__") throw new PyAttributeError("readonly attribute");
        if (!state.Globals.DeleteIfPresent(name))
            throw state.MissingAttr(name);
    }
}

public static partial class PyModules
{
    /// <summary>注册真实实例源码能力；仅文件来源1/2有效，未改变任何Python API。</summary>
    public static void RegisterInstanceSourceLocated(string name, string package, string diagnosticFile,
        long locationKind, string locationValue, bool isPackage, PyModuleExecute execute)
    {
        EnsureRegistrationAvoidsCacheAliases(name);
        if (locationKind is not (1 or 2)) throw new PyNotImplementedError("instance modules require an explicit source location");
        string resolved = PyModuleLocations.Resolve(locationKind, locationValue);
        var code = new PyCompiledModuleCode(name, package, resolved, isPackage, execute);
        Registry[name] = new Registration(package, diagnosticFile, () => { },
            _ => throw new PyRuntimeError("instance code has no singleton globals"), SetDynamic,
            resolved, true, new ResourceLocation(isPackage, locationKind, resolved), true, code);
    }
    internal static PyModule CreateRegisteredInstance(string name)
    {
        if (!Registry.TryGetValue(name, out var registration)) throw new PyModuleNotFoundError($"No module named '{name}'");
        if (registration.InstanceCode is not { } code) throw new PyNotImplementedError("registered module has no instance execution capability");
        var module = PyModuleInstances.Create(code);
        if (registration.ResourceLocation is { } source) ResourceLocations.Add(module, source);
        return module;
    }
}
