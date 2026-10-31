using System;

namespace RyuPy.Runtime;

/// <summary>
/// Python 数值运算中与 CIL 原生指令语义不同的部分。
/// CIL 的 <c>div</c>/<c>rem</c> 向零截断，而 Python 的 <c>//</c>/<c>%</c> 向负无穷取整：
/// <c>-7 // 2 == -4</c>，<c>-7 % 2 == 1</c>（CIL 分别得到 -3 和 -1）。
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

    /// <summary><c>a ** b</c>（int）。TODO(M1.2)：b 为负数时 Python 返回 float，需在类型检查阶段处理。</summary>
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
}
