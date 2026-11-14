// 有限Decimal的比较不扩大公开float构造：内部仅将有限IEEE值精确展开，最多约1075位。
using System;
using System.Globalization;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyDecimalComparison
{
    internal static object Compare(PyDecimal left, object? right, string operation)
    {
        int order;
        if (right is PyDecimal other) order = Ordered(left, other);
        else if (PyInt.IsInt(right!)) order = Ordered(left, PyDecimal.FromInteger(PyInt.ToBigInteger(right!)));
        else if (right is double floating)
        {
            if (double.IsNaN(floating))
            {
                if (operation == "__eq__") return false;
                if (operation == "__ne__") return true;
                throw PyDecimal.Unsupported("ordering with NaN requires decimal.InvalidOperation");
            }
            if (double.IsPositiveInfinity(floating)) order = -1;
            else if (double.IsNegativeInfinity(floating)) order = 1;
            else order = Ordered(left, FromFiniteFloat(floating));
        }
        else return PyNotImplemented.Instance;
        return operation switch
        {
            "__eq__" => order == 0, "__ne__" => order != 0,
            "__lt__" => order < 0, "__le__" => order <= 0,
            "__gt__" => order > 0, "__ge__" => order >= 0,
            _ => throw new InvalidOperationException("未知内部Decimal比较槽"),
        };
    }

    internal static int Ordered(PyDecimal left, PyDecimal right)
    {
        if (left.Coefficient.IsZero && right.Coefficient.IsZero) return 0;
        if (left.Coefficient.IsZero) return right.Negative ? 1 : -1;
        if (right.Coefficient.IsZero) return left.Negative ? -1 : 1;
        if (left.Negative != right.Negative) return left.Negative ? -1 : 1;
        long la = left.Exponent + left.Digits.Length - 1;
        long ra = right.Exponent + right.Digits.Length - 1;
        int result = la.CompareTo(ra);
        if (result == 0)
        {
            // 同阶时只扫描两边已有系数，缺位视为零；绝不扩充10^指数差。
            int size = Math.Max(left.Digits.Length, right.Digits.Length);
            for (int i = 0; i < size; i++)
            {
                char l = i < left.Digits.Length ? left.Digits[i] : '0';
                char r = i < right.Digits.Length ? right.Digits[i] : '0';
                if (l != r) { result = l.CompareTo(r); break; }
            }
        }
        return left.Negative ? -result : result;
    }

    private static PyDecimal FromFiniteFloat(double value)
    {
        PyFiniteNumericHash.Decompose(value, out BigInteger coefficient, out int exponent, out bool negative);
        long decimalExponent = 0;
        if (exponent >= 0) coefficient <<= exponent;
        else { coefficient *= BigInteger.Pow(5, -exponent); decimalExponent = exponent; }
        return new PyDecimal(negative, coefficient.ToString(CultureInfo.InvariantCulture), decimalExponent);
    }
}
