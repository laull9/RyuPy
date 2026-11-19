// bool 的冷绑定与普通实例长度真值；长度结果复用共享校验，不修改 primitive switch 或通用整数拆箱。

namespace RyuPy.Runtime;

public static partial class PyOps
{
    private static object BuiltinBool(object?[] args, string[] names)
    {
        if (names.Length != 0) throw TruthError(new PyTypeError("bool() takes no keyword arguments"));
        if (args.Length > 1) throw TruthError(new PyTypeError($"bool expected at most 1 argument, got {args.Length}"));
        // 用户协议正文在所有绑定错误处理之外，保原异常身份、帧与调用次数。
        return args.Length != 0 && Truthy(args[0]);
    }

    private static bool TruthyLength(object? value) => LengthResult(value, "truth testing") != 0;

    private static PyBaseException TruthError(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
