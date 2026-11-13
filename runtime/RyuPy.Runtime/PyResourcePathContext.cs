// importlib.resources.as_file的Path分支：只yield原对象，复用原生生成器/上下文管理协议，完全不触碰文件系统。
namespace RyuPy.Runtime;

internal static class PyResourcePathContext
{
    private const string Doc = "\n    Degenerate behavior for pathlib.Path objects.\n    ";
    private static readonly PyFunction GeneratorFunction = CreateGeneratorFunction();

    internal static object Create(object?[] args, string[] names, object?[] values)
    {
        // Python函数先报告重复/未知关键字，再报告超量位置参数；这里只绑定，不启动生成器。
        if (args.Length > 1) PyBinder.Bind(GeneratorFunction.Signature, args[..1], names, values, GeneratorFunction.Defaults);
        return new PyGeneratorContext(GeneratorFunction, args, names, values);
    }

    private static PyFunction CreateGeneratorFunction()
    {
        var function = PyNativeFunctions.Create("_", ["path"], [null], 1, a =>
            new PyGenerator(Resume, [0L, PyNone.Instance, null, null, a[0]], "importlib.resources._common", "_"));
        PyObject.SetAttr(function, "__doc__", Doc);
        PyObject.SetAttr(function, "__module__", "importlib.resources._common");
        return function;
    }

    private static object? Resume(object raw)
    {
        var frame = (object?[])raw;
        if ((long)frame[0]! == 0)
        {
            frame[0] = 1L;
            return frame[4];
        }
        // 恢复时先消费注入异常，由已有contextlib按原异常身份决定传播；正常完成无清理动作。
        PyOps.GeneratorInput(frame);
        frame[0] = -1L;
        return PyNone.Instance;
    }
}
