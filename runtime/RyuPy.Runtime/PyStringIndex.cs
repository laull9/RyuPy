// 字符串默认下标的整数转换：精确str与派生默认getitem共用，不扩展其它序列API。
using System.Numerics;
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;

internal static class PyStringIndex
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long Convert(object? key) => key switch
    {
        long number => number,
        int number => number,
        bool flag => flag ? 1L : 0L,
        _ => Slow(key),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Slow(object? original)
    {
        object? integer = original;
        if (integer is not BigInteger)
        {
            // 类型槽仅调用一次；getter、正文及警告处理器异常不catch、不重写现场。
            if (!PyIndexSlot.TryInvoke(original, out integer))
                throw Own(new PyTypeError($"string indices must be integers, not '{PyRepr.TypeName(original)}'"));
            if (integer is bool flag)
            {
                PyWarnings.IndexReturnedStrictSubclass("bool");
                return flag ? 1L : 0L;
            }
        }
        return integer switch
        {
            int number => number,
            long number => number,
            BigInteger number => Narrow(number, original),
            _ => throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})")),
        };
    }

    private static long Narrow(BigInteger number, object? original)
    {
        if (number < long.MinValue || number > long.MaxValue)
            throw Own(new PyIndexError($"cannot fit '{PyRepr.TypeName(original)}' into an index-sized integer"));
        return (long)number;
    }

    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
