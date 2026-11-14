// 有限数值的精确有理数哈希：仅模运算，不物化10^巨大指数；保留CP整数模数和-1哨兵。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyFiniteNumericHash
{
    internal const long Modulus = (1L << 61) - 1;
    private static readonly BigInteger InverseTen = BigInteger.ModPow(10, Modulus - 2, Modulus);
    private static readonly BigInteger InverseTwo = (Modulus + 1) / 2;

    internal static long Decimal(BigInteger coefficient, long exponent, bool negative) =>
        Scaled(coefficient, exponent, negative, 10, InverseTen);

    internal static long Float(double value)
    {
        if (!double.IsFinite(value)) throw new ArgumentException("此内部入口只接有限float", nameof(value));
        Decompose(value, out BigInteger coefficient, out int exponent, out bool negative);
        return Scaled(coefficient, exponent, negative, 2, InverseTwo);
    }

    // 同一IEEE解码供hash与精确混合比较复用；subnormal的指数固定-1074。
    internal static void Decompose(double value, out BigInteger coefficient, out int exponent, out bool negative)
    {
        ulong bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        negative = (bits >> 63) != 0;
        int encoded = (int)((bits >> 52) & 2047);
        if (encoded == 2047) throw new ArgumentException("不能解码非有限float", nameof(value));
        ulong fraction = bits & ((1UL << 52) - 1);
        coefficient = encoded == 0 ? fraction : fraction | (1UL << 52);
        exponent = encoded == 0 ? -1074 : encoded - 1023 - 52;
    }

    private static long Scaled(BigInteger coefficient, long exponent, bool negative, int radix, BigInteger inverse)
    {
        if (coefficient.Sign < 0) throw new ArgumentException("系数必须非负", nameof(coefficient));
        if (coefficient.IsZero) return 0;
        BigInteger power = new(exponent);
        BigInteger factor = power.Sign >= 0 ? BigInteger.ModPow(radix, power, Modulus)
            : BigInteger.ModPow(inverse, -power, Modulus);
        long result = (long)((coefficient % Modulus) * factor % Modulus);
        if (negative) result = -result;
        return result == -1 ? -2 : result;
    }
}
