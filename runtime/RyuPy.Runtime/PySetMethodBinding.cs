// set.clear 的窄绑定器：所有已有精确集合入口共享校验，成功路不创建包装或捕获异常。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>实参已展开；只在完整绑定成功后修改原集合。</summary>
    internal static object? CallSetClear(PySet set, object?[] args, string[] names)
    {
        if (names.Length != 0)
            throw SetClearBindingError("set.clear() takes no keyword arguments");
        if (args.Length != 0)
            throw SetClearBindingError($"set.clear() takes no arguments ({args.Length} given)");
        set.Clear();
        return PyNone.Instance;
    }

    // 只准备本绑定器新建的错误，不能改写实参展开中来自用户的异常身份和链。
    private static PyTypeError SetClearBindingError(string message) =>
        (PyTypeError)PyErrors.PrepareRaise(new PyTypeError(message));
}
