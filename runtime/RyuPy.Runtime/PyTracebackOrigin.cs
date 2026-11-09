// 帧只保存真实函数来源引用；模块根使用每次执行新建的内部标记，不按文件名猜测身份。
using System;

namespace RyuPy.Runtime;

internal sealed class ModuleExecutionSource
{
    internal readonly object Module;
    internal readonly CompiledCodeSource? Source;
    internal readonly PyDict? Globals;
    internal readonly object EntryModule;
    // 旧ABI的原值语义保持，不将字符串重新解释为代码或注册名。
    internal ModuleExecutionSource(object? module) => Module = EntryModule = module ?? PyNone.Instance;
    internal ModuleExecutionSource(object module, CompiledCodeSource source, PyDict? globals, object entry)
    { Module = module; Source = source; Globals = globals; EntryModule = entry; }
}

public static partial class PyTraceback
{
    // 平行数组不产生逐调用帧对象；PopTo/Reset负责清除已失效的强引用。
    private static object?[] _origins = new object?[InitialFrameCapacity];

    /// <summary>编译器在每次实际模块执行入口创建来源，不跨失败重试复用。</summary>
    public static object CreateModuleExecutionSource(object? module) => new ModuleExecutionSource(module);

    /// <summary>保存已由编译器证明的真实来源；热入口不转换值、不调用用户协议。</summary>
    public static long PushFrameWithOrigin(string file, string function, object? origin) =>
        PushFrameCore(file, function, origin);

    /// <summary>类定义冷路只认最内模块来源；实例直接读自身字典，legacy保注册槽语义。</summary>
    internal static bool TryGetClassDefinitionModule(out object? value)
    {
        value = null;
        // 函数的可写 __module__ 不是类体 globals；未知/静态帧也不能借用外层模块。
        if (_depth == 0 || _origins[_depth - 1] is not ModuleExecutionSource source) return false;
        if (source.Module is PyModuleInstanceState state)
            return state.Globals.TryGet("__name__", out value);
        return source.Module is string key && PyModules.TryGetClassDefinitionModule(key, out value);
    }

    /// <summary>只读最内帧的当前真实模块成员；不是公开Python frame API，不执行真值fallback。</summary>
    public static object GetCallerModule()
    {
        object? origin = _depth == 0 ? null : _origins[_depth - 1];
        return origin switch
        {
            PyFunction function => function.Module,
            ModuleExecutionSource { Module: PyModuleInstanceState state } => state.Globals.GetOrDefault("__name__", PyNone.Instance) ?? PyNone.Instance,
            ModuleExecutionSource module => module.Module,
            // 未知静态/native帧不能向下搜索并冒用外层身份；非法内部来源同样明确失败。
            _ => throw (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError(
                "current frame has no supported function origin")),
        };
    }
}
