using System;
using System.Globalization;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>int</c> 的数值内核：优先用 <see cref="long"/>，溢出时升级到
/// <see cref="BigInteger"/>（ADR-0010）。
/// </summary>
/// <remarks>
/// 为什么不全程用 <see cref="BigInteger"/>：小整数是绝大多数运算的输入，<c>long</c> 没有
/// 分配、能被 JIT 放在寄存器里。代价是每条路径都要判断「是否还在 i64 内」，因此所有结果
/// 都经过 <see cref="Normalize"/> 收敛回 <c>long</c>，让后续的类型判定保持简单。
///
/// # 快路径的写法
///
/// 每个算术入口都先做 `TryI64` 双路判定：
/// <list type="bullet">
/// <item>两边都是 <c>long</c>（含 <c>bool</c> / <c>int</c>）→ 在 <c>checked</c> 块里算；</item>
/// <item>溢出（<see cref="OverflowException"/>，只在真正越界时发生）→ 转 <see cref="BigInteger"/>；</item>
/// <item>任意一边已经是 <see cref="BigInteger"/> → 直接走大整数路径。</item>
/// </list>
///
/// 这样动态路径的小整数运算不再有 <c>new BigInteger</c> 与额外装箱，而大整数语义保持不变。
///
/// <c>bool</c> 在 Python 里是 <c>int</c> 的子类：参与算术与比较时按 0/1 处理，但
/// <c>repr</c> 仍输出 <c>True</c>/<c>False</c>（见 <see cref="PyRepr"/>）。
/// </remarks>
internal static class PyInt
{
    /// <summary>指数 / 移位量的上限，防止一次运算就把进程内存吃光。</summary>
    public const int MaxBitShift = 1_000_000;

    /// <summary>该值是否为 Python <c>int</c>（含 <c>bool</c>）。</summary>
    public static bool IsInt(object? value) =>
        value is long or int or BigInteger or bool;

    /// <summary>判断值能否无损放进 <c>long</c>；<c>bool</c> 按 0/1 处理。</summary>
    public static bool TryI64(object value, out long result)
    {
        switch (value)
        {
            case bool b:
                result = b ? 1L : 0L;
                return true;
            case long l:
                result = l;
                return true;
            case int i:
                result = i;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>转成 <see cref="BigInteger"/>；调用方需保证是 <c>int</c> 语义的值。</summary>
    public static BigInteger ToBigInteger(object value) => value switch
    {
        bool b => b ? BigInteger.One : BigInteger.Zero,
        long l => new BigInteger(l),
        int i => new BigInteger(i),
        BigInteger big => big,
        _ => throw new PyTypeError(
            $"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"
        ),
    };

    /// <summary>收敛表示：能放进 <c>long</c> 就放回 <c>long</c>。</summary>
    /// <remarks>
    /// 不能写成三元表达式 <c>? (long)value : value</c>：C# 会把两个分支统一成
    /// <see cref="BigInteger"/>（<c>long</c> 可隐式转到 <c>BigInteger</c>），
    /// 结果永远装箱成 <c>BigInteger</c>，小整数快路径就失效了。
    /// </remarks>
    public static object Normalize(BigInteger value)
    {
        if (value >= long.MinValue && value <= long.MaxValue)
        {
            return (long)value;
        }
        return value;
    }

    /// <summary>解析十进制字面量（支持前后空白、正负号与下划线分隔，与 <c>int("...")</c> 一致）。</summary>
    public static BigInteger ParseDecimal(string text, string? context = null)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            throw new PyValueError(
                context is null
                    ? "invalid literal for int() with base 10: ''"
                    : $"invalid literal for {context} with base 10: ''"
            );
        }
        string digits = trimmed.Replace("_", "");
        // 仅接受可选符号 + 十进制数字；其余情况按 CPython 报 ValueError
        int start = digits.Length > 0 && (digits[0] == '+' || digits[0] == '-') ? 1 : 0;
        bool digitsOk = digits.Length > start;
        for (int i = start; i < digits.Length && digitsOk; i++)
        {
            digitsOk = digits[i] >= '0' && digits[i] <= '9';
        }
        if (!digitsOk)
        {
            throw new PyValueError($"invalid literal for int() with base 10: {PyRepr.Quote(text)}");
        }
        return BigInteger.Parse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    public static object Add(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            try
            {
                return checked(x + y);
            }
            catch (OverflowException)
            {
                return Normalize(new BigInteger(x) + y);
            }
        }
        return Normalize(ToBigInteger(a) + ToBigInteger(b));
    }

    public static object Subtract(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            try
            {
                return checked(x - y);
            }
            catch (OverflowException)
            {
                return Normalize(new BigInteger(x) - y);
            }
        }
        return Normalize(ToBigInteger(a) - ToBigInteger(b));
    }

    public static object Multiply(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            try
            {
                return checked(x * y);
            }
            catch (OverflowException)
            {
                return Normalize(new BigInteger(x) * y);
            }
        }
        return Normalize(ToBigInteger(a) * ToBigInteger(b));
    }

    public static object Negate(object a)
    {
        if (TryI64(a, out long x))
        {
            return x == long.MinValue ? Normalize(-new BigInteger(x)) : -x;
        }
        return Normalize(-ToBigInteger(a));
    }

    public static object Abs(object a)
    {
        if (TryI64(a, out long x))
        {
            return x == long.MinValue ? Normalize(BigInteger.Abs(new BigInteger(x))) : Math.Abs(x);
        }
        return Normalize(BigInteger.Abs(ToBigInteger(a)));
    }

    /// <summary><c>a // b</c>：向负无穷取整，余数符号跟随除数。</summary>
    public static object FloorDivide(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            if (y == 0)
            {
                throw new PyZeroDivisionError("integer division or modulo by zero");
            }
            // long.MinValue / -1 在 i64 里会溢出：交给大整数路径得到 2^63
            if (!(x == long.MinValue && y == -1))
            {
                long quotient = x / y;
                if ((x % y != 0) && ((x < 0) != (y < 0)))
                {
                    quotient--;
                }
                return quotient;
            }
        }
        BigInteger divisor = ToBigInteger(b);
        if (divisor.IsZero)
        {
            throw new PyZeroDivisionError("integer division or modulo by zero");
        }
        return Normalize(FloorDivRem(ToBigInteger(a), divisor).Quotient);
    }

    /// <summary><c>a % b</c>：结果符号与除数相同。</summary>
    public static object Modulo(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            if (y == 0)
            {
                throw new PyZeroDivisionError("integer modulo by zero");
            }
            if (x == long.MinValue && y == -1)
            {
                return 0L;
            }
            long remainder = x % y;
            if (remainder != 0 && ((remainder < 0) != (y < 0)))
            {
                remainder += y;
            }
            return remainder;
        }
        BigInteger divisor = ToBigInteger(b);
        if (divisor.IsZero)
        {
            throw new PyZeroDivisionError("integer modulo by zero");
        }
        return Normalize(FloorDivRem(ToBigInteger(a), divisor).Remainder);
    }

    /// <summary>
    /// 同时得到向下取整的商与对应余数。CLR 的 <c>DivRem</c> 向零截断，所以负数要先修正。
    /// </summary>
    public static (BigInteger Quotient, BigInteger Remainder) FloorDivRem(
        BigInteger dividend,
        BigInteger divisor
    )
    {
        BigInteger quotient = BigInteger.DivRem(dividend, divisor, out BigInteger remainder);
        if (!remainder.IsZero && remainder.Sign != divisor.Sign)
        {
            quotient -= 1;
            remainder += divisor;
        }
        return (quotient, remainder);
    }

    /// <summary><c>a ** b</c>：负指数返回 <c>float</c>（与 CPython 相同），否则升级为任意精度。</summary>
    public static object Power(object a, object b)
    {
        BigInteger exponent = ToBigInteger(b);
        BigInteger baseValue = ToBigInteger(a);
        if (exponent.Sign < 0)
        {
            if (baseValue.IsZero)
            {
                throw new PyZeroDivisionError("0.0 cannot be raised to a negative power");
            }
            return Math.Pow((double)baseValue, (double)exponent);
        }
        if (exponent > MaxBitShift)
        {
            throw new PyOverflowError("exponent too large for integer power");
        }
        return Normalize(BigInteger.Pow(baseValue, (int)exponent));
    }

    public static object BitAnd(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            return x & y;
        }
        return Normalize(ToBigInteger(a) & ToBigInteger(b));
    }

    public static object BitOr(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            return x | y;
        }
        return Normalize(ToBigInteger(a) | ToBigInteger(b));
    }

    public static object BitXor(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            return x ^ y;
        }
        return Normalize(ToBigInteger(a) ^ ToBigInteger(b));
    }

    public static object Invert(object a)
    {
        if (TryI64(a, out long x))
        {
            return ~x;
        }
        return Normalize(~ToBigInteger(a));
    }

    public static object ShiftLeft(object a, object b)
    {
        int count = ShiftCount(b);
        if (TryI64(a, out long x))
        {
            // 左移没有 overflow 检查：先判断结果能否落回 i64，能则走快路径
            if (count < 64 && x <= (long.MaxValue >> count) && x >= (long.MinValue >> count))
            {
                return x << count;
            }
        }
        return Normalize(ToBigInteger(a) << count);
    }

    public static object ShiftRight(object a, object b)
    {
        int count = ShiftCount(b);
        if (TryI64(a, out long x))
        {
            // Python 的 >> 对负数按无限位补码右移；count >= 64 时结果为 -1 / 0
            return count >= 64 ? (x < 0 ? -1L : 0L) : (x >> count);
        }
        return Normalize(ToBigInteger(a) >> count);
    }

    private static int ShiftCount(object value)
    {
        BigInteger count = ToBigInteger(value);
        if (count.Sign < 0)
        {
            throw new PyValueError("negative shift count");
        }
        if (count > MaxBitShift)
        {
            throw new PyOverflowError("shift count too large");
        }
        return (int)count;
    }

    /// <summary>整数之间的大小比较。</summary>
    public static int Compare(object a, object b)
    {
        if (TryI64(a, out long x) && TryI64(b, out long y))
        {
            return x.CompareTo(y);
        }
        return ToBigInteger(a).CompareTo(ToBigInteger(b));
    }

    /// <summary>
    /// 整数与浮点的精确比较（CPython 语义）：不能先把整数转成 <c>double</c>，
    /// 否则 <c>9007199254740993 == 9007199254740992.0</c> 会错判为真。
    /// </summary>
    /// <returns>-1 / 0 / 1；浮点为 NaN 时返回 <c>null</c>（调用方按「不可比较」处理）。</returns>
    public static int? CompareWithDouble(BigInteger integer, double value)
    {
        if (double.IsNaN(value))
        {
            return null;
        }
        if (double.IsPositiveInfinity(value))
        {
            return -1;
        }
        if (double.IsNegativeInfinity(value))
        {
            return 1;
        }
        // floor 之后整数部分与小数部分都精确：|d| 很大时小数部分恒为 0
        BigInteger floor = new(Math.Floor(value));
        int byFloor = integer.CompareTo(floor);
        if (byFloor != 0)
        {
            return byFloor;
        }
        double fraction = value - Math.Floor(value);
        return fraction > 0 ? -1 : 0;
    }

    /// <summary>
    /// <see cref="CompareWithDouble(BigInteger, double)"/> 的 i64 快路径。
    /// </summary>
    /// <remarks>
    /// 小整数与浮点比较在动态代码里很常见（<c>i &lt; 1e9</c>），构造 <c>BigInteger</c>
    /// 只为比较大小并不划算。这里先按浮点数的整数部分判断，只有落在
    /// 「double 无法精确表示整数」的区间（|v| ≥ 2^53）才回退到大整数比较。
    /// </remarks>
    public static int? CompareWithDouble(long integer, double value)
    {
        if (double.IsNaN(value))
        {
            return null;
        }
        if (double.IsPositiveInfinity(value))
        {
            return -1;
        }
        if (double.IsNegativeInfinity(value))
        {
            return 1;
        }
        if (Math.Abs(value) < 9.007199254740992e15)
        {
            double floor = Math.Floor(value);
            if (integer < floor)
            {
                return -1;
            }
            if (integer > floor)
            {
                return 1;
            }
            return value - floor > 0 ? -1 : 0;
        }
        return CompareWithDouble(new BigInteger(integer), value);
    }
}
