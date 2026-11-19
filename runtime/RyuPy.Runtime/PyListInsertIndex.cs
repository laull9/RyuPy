// 仅list.insert使用的索引转换；类型槽调用恰好一次，范围检查在用户回调完成之后。
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyListInsertIndex
{
    internal static long Convert(object? value)
    {
        if (value is not (bool or int or long or BigInteger))
        {
            if (!PyIndexSlot.TryInvoke(value, out object? converted))
                throw Own(new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"));
            value = converted;
            if (value is bool) PyWarnings.IndexReturnedStrictSubclass("bool");
            if (value is not (bool or int or long or BigInteger))
                throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(value)})"));
        }
        return value switch
        {
            bool flag => flag ? 1 : 0,
            int number => number,
            long number => number,
            BigInteger number when number >= long.MinValue && number <= long.MaxValue => (long)number,
            _ => throw Own(new PyOverflowError("Python int too large to convert to C ssize_t")),
        };
    }

    private static PyBaseException Own(PyBaseException error) =>
        (PyBaseException)PyErrors.PrepareRaise(error);
}
