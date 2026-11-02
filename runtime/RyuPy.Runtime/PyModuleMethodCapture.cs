namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>捕获标志由独立Bool局部保存，不能从属性值的类型反推。</summary>
    public static bool IsModule(object? receiver) => receiver is PyModule;

    /// <summary>模块属性在实参前读取原对象；其他接收者保持身份，不分配包装。</summary>
    public static object? CaptureModuleTarget(object? receiver, string name)
    {
        if (receiver is not PyModule) return receiver;
        try { return PyObject.GetAttr(receiver, name); }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    /// <summary>数组调用只展开一次；未捕获接收者继续原方法协议。</summary>
    public static object? CallCapturedMethod(bool captured, object? target, string name,
        object?[] rawArgs, object?[] rawNames, object?[] rawValues) =>
        captured ? CallValue(target, rawArgs, rawNames, rawValues)
            : CallMethod(target, name, rawArgs, rawNames, rawValues);
}
