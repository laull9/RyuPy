// 分开处理用户 __hash__ 返回值与整数自身 hash；前者 fits-i64 保原值，后者始终遵守整数模数。
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

public static partial class PyHash
{
    private const long IntegerHashModulus = (1L << 61) - 1;

    // 常用小整数只判边界与 -1，不构造 BigInteger，不执行除法；宽 long 不取 Abs，避免 MinValue 溢出。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long IntegerValueHash(long value)
    {
        if (value > -IntegerHashModulus && value < IntegerHashModulus)
            return NormalizeHashSentinel(value);
        return NormalizeHashSentinel(value % IntegerHashModulus);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long NormalizeHashSentinel(long value) => value == -1 ? -2 : value;

    // 用户已被调用一次；只接运行时真实整数表示，不通过 __index__/__int__/__float__ 或结果自己的 __hash__。
    // CP 的槽返回合同与 hash(int) 不同：可放进 i64 时保原数值，只有超界才使用整数值 hash。
    private static long UserHashResult(object? result) => result switch
    {
        bool value => value ? 1 : 0,
        int value => NormalizeHashSentinel(value),
        long value => NormalizeHashSentinel(value),
        BigInteger value when value >= long.MinValue && value <= long.MaxValue =>
            NormalizeHashSentinel((long)value),
        BigInteger value => BigIntegerHash(value),
        _ => throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError("__hash__ method should return an integer")),
    };
}
