// 仅 list.pop 使用的整数索引转换；共享 ToIndex 与普通下标路径保持独立。
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    // 普通机器整数不制造 BigInteger，也不进入用户协议或异常处理区。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ListPopIndex(object? value) => value switch
    {
        long integer => integer,
        int integer => integer,
        bool boolean => boolean ? 1L : 0L,
        _ => ListPopIndexSlow(value),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ListPopIndexSlow(object? value)
    {
        object? integer = value;
        if (!PyInt.IsInt(integer))
        {
            // 类型级协议只执行一次；其正文异常原样传播，不重写用户现场。
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw ListPopIndexError(new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"));
            if (integer is bool)
                throw ListPopIndexError(new PyNotImplementedError("list.pop(): __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer))
                throw ListPopIndexError(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        if (PyInt.TryI64(integer!, out long index)) return index;
        var wide = (BigInteger)integer!;
        // CPython pop 的 ssize_t 参数转换先于空表检查，且使用固定溢出消息。
        if (wide < long.MinValue || wide > long.MaxValue)
            throw ListPopIndexError(new PyOverflowError("Python int too large to convert to C ssize_t"));
        return (long)wide;
    }

    // 仅准备本层新造的错误；无成功路快照或通用调用包装。
    private static PyBaseException ListPopIndexError(PyBaseException error) =>
        (PyBaseException)PyErrors.PrepareRaise(error);
}
