// 实例模块import的显式环境守卫及真实有限默认__import__；不执行自定义hook、不支持相对协议。
using System;
namespace RyuPy.Runtime;

internal static class PyModuleImport
{
    // 独立保存默认函数身份；真实共享字典被覆盖后不能拿覆盖值当作默认身份。
    internal static readonly PyFunction Function = Create();
    private static PyFunction Create()
    {
        var function = PyNativeFunctions.Create("__import__", ["name", "globals", "locals", "fromlist", "level"],
            [null, PyNone.Instance, PyNone.Instance, new PyTuple([]), 0L], 1, Import);
        function.CaptureModule("builtins");
        return function;
    }
    private static object Import(object?[] args)
    {
        if (args[0] is not string name)
            throw new PyTypeError("module name must be a string");
        if (args[4] is not long level || level != 0)
            throw new PyNotImplementedError("compiled __import__ only supports exact integer level zero");
        bool empty = args[3] is PyTuple tuple && tuple.GetType() == typeof(PyTuple) && tuple.Length == 0
            || args[3] is PyList list && list.GetType() == typeof(PyList) && list.Count == 0;
        if (!empty) throw new PyNotImplementedError("compiled __import__ only supports an empty exact list or tuple fromlist");
        if (name.Length == 0) throw new PyValueError("Empty module name");
        if (name.StartsWith(".", StringComparison.Ordinal))
            throw new PyNotImplementedError("compiled __import__ only supports absolute module names");
        // 绝对导入不消费globals/locals；先完成目标导入，再返回点分名称顶层对象。
        PyModule module = PyModules.Import(name);
        int dot = name.IndexOf('.');
        return dot < 0 ? module : PyModules.Import(name[..dot]);
    }
}

public static partial class PyModuleInstances
{
    /// <summary>每条实际import消费前验证捕获环境；未知hook在任何目标导入副作用前拒绝。</summary>
    public static void ImportGuard(object context)
    {
        var builtins = Context(context).Builtins;
        if (!builtins.TryGet("__import__", out object? importer)) throw new PyImportError("__import__ not found");
        if (!ReferenceEquals(importer, PyModuleImport.Function))
            throw new PyNotImplementedError("custom __import__ hooks are not implemented for instance modules");
    }
}
