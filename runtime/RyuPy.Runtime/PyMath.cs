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
/// <remarks>
/// # 溢出为什么在这里转成 Python 异常
///
/// Python 的 <c>int</c> 没有上限，而静态特化路径把它落成 <see cref="long"/>。当结果超出
/// i64 时，CLR 会抛 <see cref="OverflowException"/>——那是一个 .NET 异常，若直接冒到进程
/// 顶层，用户会看到 <c>Unhandled exception. System.OverflowException</c> 与 .NET 调用栈，
/// 既不是 Python 的错误类型，也泄漏了实现细节。
///
/// 因此每个可能溢出的入口都把 CLR 溢出转成 <see cref="PyOverflowError"/>：错误仍然明确，
/// 只是被标记成「RyuPy 的 int64 上限」这一已知偏差（docs/semantics.md §4，ADR-0004）。
/// 动态路径没有这个问题——它走 <see cref="PyInt"/> 的 <c>BigInteger</c> 升级。
/// </remarks>
public static class PyMath
{
    /// <summary>
    /// 静态 i64 路径溢出的统一错误。
    /// </summary>
    /// <remarks>
    /// 消息是 RyuPy 自有文本（CPython 不存在这条路径），措辞点明是**实现上限**而不是
    /// Python 语义，避免用户误以为 CPython 也会这样。
    /// </remarks>
    public static PyOverflowError IntOverflow() =>
        new("int too large to fit in a 64-bit integer (RyuPy static integer limit)");

    /// <summary><c>a // b</c>（int）。除数为 0 时抛 Python <c>ZeroDivisionError</c>。</summary>
    public static long FloorDivI64(long a, long b)
    {
        if (b == 0)
        {
            throw new PyZeroDivisionError("integer division or modulo by zero");
        }
        // long.MinValue / -1 在 CIL 里会抛 OverflowException，而 Python 的结果是 2^63
        if (a == long.MinValue && b == -1)
        {
            throw IntOverflow();
        }
        long q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0)))
        {
            q--;
        }
        return q;
    }

    /// <summary><c>a % b</c>（int），结果符号与除数相同。</summary>
    public static long ModI64(long a, long b)
    {
        if (b == 0)
        {
            throw new PyZeroDivisionError("integer modulo by zero");
        }
        // 同 FloorDivI64：`long.MinValue % -1` 的商溢出，但余数按定义是 0
        if (a == long.MinValue && b == -1)
        {
            return 0;
        }
        long r = a % b;
        if (r != 0 && ((r < 0) != (b < 0)))
        {
            r += b;
        }
        return r;
    }

    /// <summary><c>a / b</c>（float）。除数为 0 时抛 <c>ZeroDivisionError</c>（语义陷阱 #12）。</summary>
    public static double TrueDivF64(double a, double b)
    {
        if (b == 0.0)
        {
            throw new PyZeroDivisionError("float division by zero");
        }
        return a / b;
    }

    /// <summary><c>a // b</c>（float）。</summary>
    public static double FloorDivF64(double a, double b)
    {
        if (b == 0.0)
        {
            throw new PyZeroDivisionError("float floor division by zero");
        }
        return Math.Floor(a / b);
    }

    /// <summary><c>a % b</c>（float），结果符号与除数相同。</summary>
    public static double ModF64(double a, double b)
    {
        if (b == 0.0)
        {
            throw new PyZeroDivisionError("float modulo");
        }
        double r = a % b;
        if (r != 0 && ((r < 0) != (b < 0)))
        {
            r += b;
        }
        return r;
    }

    /// <summary>
    /// <c>a ** b</c>（int），指数为非负时的快速幂。
    /// </summary>
    /// <remarks>
    /// 负指数在 Python 里返回 <c>float</c>，不能塞进 <see cref="long"/>；MIR 构建阶段会让
    /// 这类表达式走动态路径（见 <c>expr_builder</c>），因此这里只处理 <paramref name="b"/> ≥ 0。
    /// 若仍然收到负数，按「静态类型无法表示」报 <see cref="PyValueError"/>，而不是悄悄返回错误值。
    /// </remarks>
    public static long PowI64(long a, long b)
    {
        if (b < 0)
        {
            throw new PyValueError(
                "negative exponent is not supported by static int power (compile with dynamic typing)"
            );
        }
        try
        {
            long result = 1;
            long factor = a;
            long exponent = b;
            while (exponent > 0)
            {
                if ((exponent & 1) != 0)
                {
                    result = checked(result * factor);
                }
                exponent >>= 1;
                if (exponent > 0)
                {
                    factor = checked(factor * factor);
                }
            }
            return result;
        }
        catch (OverflowException)
        {
            throw IntOverflow();
        }
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
            throw new PyValueError("cannot convert float NaN to integer");
        }
        if (double.IsInfinity(value))
        {
            throw new PyOverflowError("cannot convert float infinity to integer");
        }
        // 2^63 无法用 double 精确表示，用 >= / <= 的比较边界更稳妥
        if (value >= 9.2233720368547758e18 || value < -9.2233720368547758e18)
        {
            throw new PyOverflowError("int too large to fit in a 64-bit integer (RyuPy static integer limit)");
        }
        return (long)value;
    }

    /// <summary>
    /// <c>a &lt;&lt; b</c>（int）。移位量 ≥ 64 或结果超出 i64 时抛 <see cref="PyOverflowError"/>。
    /// </summary>
    /// <remarks>
    /// 不能直接用 CIL 的 <c>shl</c>：它把移位量按 63 取模（<c>1 &lt;&lt; 100</c> 会算成
    /// <c>1 &lt;&lt; 36</c>），静默给出错误结果。Python 的移位是任意精度的，静态路径只能
    /// 在结果能放回 i64 时返回，否则按已登记的 int64 上限报错。
    /// </remarks>
    public static long ShiftLeftI64(long value, long count)
    {
        if (count < 0)
        {
            throw new PyValueError("negative shift count");
        }
        if (value == 0)
        {
            return 0;
        }
        if (count >= 64)
        {
            throw IntOverflow();
        }
        int shift = (int)count;
        // 正数检查上界、负数检查下界；边界用「右移后的极值」比较，避免先移位再回判
        if (value > 0 ? value > (long.MaxValue >> shift) : value < (long.MinValue >> shift))
        {
            throw IntOverflow();
        }
        return value << shift;
    }

    /// <summary><c>a &gt;&gt; b</c>（int）：算术右移，移位量 ≥ 64 时结果为 0 或 -1。</summary>
    public static long ShiftRightI64(long value, long count)
    {
        if (count < 0)
        {
            throw new PyValueError("negative shift count");
        }
        // CIL 的 shr 同样会取模：必须显式处理「全部移出」的情况
        if (count >= 64)
        {
            return value < 0 ? -1L : 0L;
        }
        return value >> (int)count;
    }

    /// <summary><c>abs(int)</c>。</summary>
    public static long AbsI64(long value) =>
        value == long.MinValue ? throw IntOverflow() : Math.Abs(value);

    /// <summary><c>abs(float)</c>。</summary>
    public static double AbsF64(double value) => Math.Abs(value);
}
