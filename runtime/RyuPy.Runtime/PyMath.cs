using System;

namespace RyuPy.Runtime;

/// <summary>
/// Python 数值运算中与 CIL 原生指令语义不同的部分。
/// 包含：
/// 1. 向下取整除法（//）与取模（%），区别于 CIL 向零截断的 div/rem；
/// 2. 幂运算（**）；
/// 3. 浮点转整数（int(float)），带 NaN 与 Inf 检查（语义陷阱 #13）；
/// 4. 绝对值运算（abs）。
/// </summary>
public static class PyMath
{
    /// <summary><c>a // b</c>（int）。除数为 0 时应抛 Python <c>ZeroDivisionError</c>。</summary>
    public static long FloorDivI64(long a, long b)
    {
        if (b == 0) throw new DivideByZeroException("division by zero");
        long q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
        return q;
    }

    /// <summary><c>a % b</c>（int），结果符号与除数相同。</summary>
    public static long ModI64(long a, long b)
    {
        if (b == 0) throw new DivideByZeroException("division by zero");
        long r = a % b;
        if (r != 0 && ((r < 0) != (b < 0))) r += b;
        return r;
    }

    /// <summary><c>a // b</c>（float）。</summary>
    public static double FloorDivF64(double a, double b) => Math.Floor(a / b);

    /// <summary><c>a % b</c>（float），结果符号与除数相同。</summary>
    public static double ModF64(double a, double b)
    {
        double r = a % b;
        if (r != 0 && ((r < 0) != (b < 0))) r += b;
        return r;
    }

    /// <summary><c>a ** b</c>（int）。</summary>
    public static long PowI64(long a, long b)
    {
        long result = 1;
        while (b > 0)
        {
            if ((b & 1) != 0) result = checked(result * a);
            b >>= 1;
            if (b > 0) a = checked(a * a);
        }
        return result;
    }

    /// <summary><c>a ** b</c>（float）。</summary>
    public static double PowF64(double a, double b) => Math.Pow(a, b);

    /// <summary>
    /// <c>int(float)</c> 转换，向零截断，并做 NaN 与 Inf 越界检查（语义陷阱 #13）。
    /// </summary>
    public static long F64ToI64(double value)
    {
        if (double.IsNaN(value))
        {
            throw new InvalidOperationException("ValueError: cannot convert float NaN to integer");
        }
        if (double.IsInfinity(value))
        {
            throw new OverflowException("OverflowError: cannot convert float infinity to integer");
        }
        if (value > (double)long.MaxValue || value < (double)long.MinValue)
        {
            throw new OverflowException("OverflowError: Python int too large to convert to C long");
        }
        return (long)value;
    }

    /// <summary><c>abs(int)</c>。</summary>
    public static long AbsI64(long value) => checked(Math.Abs(value));

    /// <summary><c>abs(float)</c>。</summary>
    public static double AbsF64(double value) => Math.Abs(value);
}
