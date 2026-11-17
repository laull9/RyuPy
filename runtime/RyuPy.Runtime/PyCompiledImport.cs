// provider字典profile的真实import协议：捕获的builtins活读callee，模块D/D、普通函数D/None，返回Any。
using System;

namespace RyuPy.Runtime;

/// <summary>原始IMPORT_FROM冷协议；provider负责所属解释器的属性、缓存回退与错误字段。</summary>
public interface IPyCompiledImportProvider
{
    /// <summary>先读属性，仅缺属性时消费同一权威sys.modules，不重新调用__import__。</summary>
    object? ImportAttribute(object? imported, string name);
}

public static partial class PyModuleInstances
{
    /// <summary>执行一次IMPORT_NAME；fromlist为None或冻结的名称数组，level保原语法层级。</summary>
    public static object? ImportName(object context, string name, long level, object? fromlist, bool moduleLocals)
    {
        var execution = Context(context);
        if (execution.State.ForeignGlobals is null)
            throw new PyNotImplementedError("provider import requires a real module dictionary");
        if (!execution.TryBuiltin("__import__", out object? importer))
            throw new PyImportError("__import__ not found");
        object globals = execution.State.View;
        object? names = fromlist is object?[] array ? new PyTuple(array) : fromlist;
        // 此profile拒绝locals()及class体import；未物化f_locals的普通函数准确传None。
        return PyOps.CallValue(importer, [name, globals, moduleLocals ? globals : PyNone.Instance,
            names ?? PyNone.Instance, level], [], []);
    }

    /// <summary>IMPORT_FROM每项完成后由MIR立即提交绑定；后项失败不回滚先前绑定。</summary>
    public static object? ImportAttribute(object context, object? imported, string name)
    {
        _ = Context(context);
        return PyProvider.Current is IPyCompiledImportProvider provider
            ? provider.ImportAttribute(imported, name)
            : throw new PyNotImplementedError("provider IMPORT_FROM protocol is not installed");
    }
}
