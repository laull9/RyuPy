// len 与普通实例真值共用的冷长度结果校验；不改变精确容器分派或通用整数拆箱。
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    // operation 仅用于未支持的弃用警告诊断；两入口使用完全相同的长度转换规则。
    private static long LengthResult(object? value, string operation)
    {
        object? integer = value;
        if (!PyInt.IsInt(integer))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw LengthError(new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"));
            if (integer is bool)
                throw LengthError(new PyNotImplementedError($"{operation}: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer))
                throw LengthError(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        // actual bool/int/long 直接返回机器整数，不为了合法长度制造 BigInteger。
        if (integer is bool boolean) return boolean ? 1L : 0L;
        if (integer is int small) return NonnegativeLength(small);
        if (integer is long machine) return NonnegativeLength(machine);
        // 固定 CP3.12.13 的 len/bool 均先拒绝负大整数，再判断正向 ssize_t 上界。
        var length = (BigInteger)integer!;
        if (length.Sign < 0) throw LengthError(new PyValueError("__len__() should return >= 0"));
        if (length > long.MaxValue)
            throw LengthError(new PyOverflowError("cannot fit 'int' into an index-sized integer"));
        return (long)length;
    }

    private static long NonnegativeLength(long length)
    {
        if (length < 0) throw LengthError(new PyValueError("__len__() should return >= 0"));
        return length;
    }

    // 只准备本层新造的错误；用户 __len__/__index__ 正文不 catch，也不重写已有异常现场。
    private static PyBaseException LengthError(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
