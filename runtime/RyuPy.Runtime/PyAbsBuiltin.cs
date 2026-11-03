// abs 的绑定错误在冷路径保存调用现场；用户 __abs__ 正文保持原调用和异常传播，不重复调用。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    private static object BuiltinAbs(object?[] args, string[] names)
    {
        object? value;
        try
        {
            value = WithKeywords("abs", args, names, () => Single("abs", args));
        }
        catch (PyBaseException error)
        {
            throw (PyBaseException)PyErrors.PrepareRaise(error);
        }
        return AbsValue(value!);
    }
}
