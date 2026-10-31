using System;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// 动态运算入口：编译产物在无法静态定型时调用的统一 ABI（ADR-0010）。
/// </summary>
/// <remarks>
/// 为什么用「操作码 + object 参数」而不是每个运算符一个方法：ABI 面越小，codegen 的引用表
/// 与运行时越不容易漂移。代价是多一次 <c>switch</c> 分派，相对于装箱成本可以忽略。
///
/// 操作码在 Rust 侧 <c>RuntimeFn</c> / <c>mir::DynOp</c> 中有对应常量，**修改必须两边同步**。
/// 错误一律抛 <see cref="PyBaseException"/> 子类，不把 CLR 异常泄漏成 Python 异常类型。
/// </remarks>
public static class PyOps
{
    /// <summary>二元操作码：<c>a + b</c>。</summary>
    public const int Add = 1;
    /// <summary>二元操作码：<c>a - b</c>。</summary>
    public const int Sub = 2;
    /// <summary>二元操作码：<c>a * b</c>。</summary>
    public const int Mul = 3;
    /// <summary>二元操作码：<c>a / b</c>，结果总是 <c>float</c>。</summary>
    public const int TrueDiv = 4;
    /// <summary>二元操作码：<c>a // b</c>，向负无穷取整。</summary>
    public const int FloorDiv = 5;
    /// <summary>二元操作码：<c>a % b</c>，符号跟随除数。</summary>
    public const int Mod = 6;
    /// <summary>二元操作码：<c>a ** b</c>。</summary>
    public const int Pow = 7;
    /// <summary>二元操作码：<c>a &amp; b</c>。</summary>
    public const int BitAnd = 8;
    /// <summary>二元操作码：<c>a | b</c>。</summary>
    public const int BitOr = 9;
    /// <summary>二元操作码：<c>a ^ b</c>。</summary>
    public const int BitXor = 10;
    /// <summary>二元操作码：<c>a &lt;&lt; b</c>。</summary>
    public const int LShift = 11;
    /// <summary>二元操作码：<c>a &gt;&gt; b</c>。</summary>
    public const int RShift = 12;

    /// <summary>比较操作码：<c>==</c>。</summary>
    public const int Eq = 1;
    /// <summary>比较操作码：<c>!=</c>。</summary>
    public const int Ne = 2;
    /// <summary>比较操作码：<c>&lt;</c>。</summary>
    public const int Lt = 3;
    /// <summary>比较操作码：<c>&lt;=</c>。</summary>
    public const int Le = 4;
    /// <summary>比较操作码：<c>&gt;</c>。</summary>
    public const int Gt = 5;
    /// <summary>比较操作码：<c>&gt;=</c>。</summary>
    public const int Ge = 6;

    /// <summary>一元操作码：<c>-x</c>。</summary>
    public const int Neg = 1;
    /// <summary>一元操作码：<c>+x</c>。</summary>
    public const int Pos = 2;
    /// <summary>一元操作码：<c>not x</c>，结果为 <c>bool</c>。</summary>
    public const int Not = 3;
    /// <summary>一元操作码：<c>~x</c>。</summary>
    public const int Invert = 4;

    /// <summary><c>None</c> 常量。</summary>
    public static object None() => PyNone.Instance;

    /// <summary>超出 i64 的整数字面量：源码里是十进制字符串，构造期转成任意精度整数。</summary>
    public static object FromDecimalString(string text) => PyInt.Normalize(PyInt.ParseDecimal(text));

    /// <summary>二元运算：返回 Python 值（可能升级为 <c>BigInteger</c> 或 <c>double</c>）。</summary>
    public static object Binary(int op, object a, object b)
    {
        bool aInt = PyInt.IsInt(a);
        bool bInt = PyInt.IsInt(b);
        bool aNum = aInt || a is double;
        bool bNum = bInt || b is double;

        switch (op)
        {
            case Add:
                if (aInt && bInt)
                {
                    return PyInt.Add(a, b);
                }
                if (aNum && bNum)
                {
                    return ToDouble(a) + ToDouble(b);
                }
                if (a is string left && b is string right)
                {
                    return string.Concat(left, right);
                }
                throw BinaryTypeError("+", a, b);
            case Sub:
                if (aInt && bInt)
                {
                    return PyInt.Subtract(a, b);
                }
                if (aNum && bNum)
                {
                    return ToDouble(a) - ToDouble(b);
                }
                throw BinaryTypeError("-", a, b);
            case Mul:
                if (aInt && bInt)
                {
                    return PyInt.Multiply(a, b);
                }
                if (aNum && bNum)
                {
                    return ToDouble(a) * ToDouble(b);
                }
                // str * int：Python 支持重复，这里先明确拒绝而不是静默返回错误结果
                throw BinaryTypeError("*", a, b);
            case TrueDiv:
                if (!aNum || !bNum)
                {
                    throw BinaryTypeError("/", a, b);
                }
                double divisor = ToDouble(b);
                if (divisor == 0.0)
                {
                    throw new PyZeroDivisionError(
                        aInt && bInt ? "division by zero" : "float division by zero"
                    );
                }
                return ToDouble(a) / divisor;
            case FloorDiv:
                if (aInt && bInt)
                {
                    return PyInt.FloorDivide(a, b);
                }
                if (aNum && bNum)
                {
                    double floorDivisor = ToDouble(b);
                    if (floorDivisor == 0.0)
                    {
                        throw new PyZeroDivisionError("float floor division by zero");
                    }
                    return Math.Floor(ToDouble(a) / floorDivisor);
                }
                throw BinaryTypeError("//", a, b);
            case Mod:
                if (aInt && bInt)
                {
                    return PyInt.Modulo(a, b);
                }
                if (aNum && bNum)
                {
                    return FloatModulo(ToDouble(a), ToDouble(b));
                }
                throw BinaryTypeError("%", a, b);
            case Pow:
                if (aInt && bInt)
                {
                    return PyInt.Power(a, b);
                }
                if (aNum && bNum)
                {
                    return Math.Pow(ToDouble(a), ToDouble(b));
                }
                throw BinaryTypeError("** or pow()", a, b);
            case BitAnd:
                RequireInts("&", a, b, aInt, bInt);
                return PyInt.BitAnd(a, b);
            case BitOr:
                RequireInts("|", a, b, aInt, bInt);
                return PyInt.BitOr(a, b);
            case BitXor:
                RequireInts("^", a, b, aInt, bInt);
                return PyInt.BitXor(a, b);
            case LShift:
                RequireInts("<<", a, b, aInt, bInt);
                return PyInt.ShiftLeft(a, b);
            case RShift:
                RequireInts(">>", a, b, aInt, bInt);
                return PyInt.ShiftRight(a, b);
            default:
                throw new PyRuntimeError($"unknown binary opcode {op}");
        }
    }

    /// <summary>比较运算：返回 Python <c>bool</c>。NaN 下所有排序比较为假，相等为假、不等为真。</summary>
    public static bool Compare(int op, object a, object b)
    {
        if (op == Eq)
        {
            return ValuesEqual(a, b);
        }
        if (op == Ne)
        {
            return !ValuesEqual(a, b);
        }

        int? order = Order(a, b, op);
        if (order is null)
        {
            // NaN 参与排序比较：一律为假
            return false;
        }
        return op switch
        {
            Lt => order < 0,
            Le => order <= 0,
            Gt => order > 0,
            Ge => order >= 0,
            _ => throw new PyRuntimeError($"unknown compare opcode {op}"),
        };
    }

    /// <summary>一元运算。</summary>
    public static object Unary(int op, object value)
    {
        switch (op)
        {
            case Neg:
                if (PyInt.IsInt(value))
                {
                    return PyInt.Negate(value);
                }
                if (value is double d)
                {
                    return -d;
                }
                throw new PyTypeError($"bad operand type for unary -: '{PyRepr.TypeName(value)}'");
            case Pos:
                if (PyInt.IsInt(value) || value is double)
                {
                    return value;
                }
                throw new PyTypeError($"bad operand type for unary +: '{PyRepr.TypeName(value)}'");
            case Not:
                return !Truthy(value);
            case Invert:
                if (PyInt.IsInt(value))
                {
                    return PyInt.Invert(value);
                }
                throw new PyTypeError($"bad operand type for unary ~: '{PyRepr.TypeName(value)}'");
            default:
                throw new PyRuntimeError($"unknown unary opcode {op}");
        }
    }

    /// <summary>真值判定（条件、<c>not</c>、<c>and</c>/<c>or</c> 共用）。</summary>
    public static bool Truthy(object? value) => value switch
    {
        null => false,
        PyNone => false,
        bool b => b,
        long l => l != 0,
        int i => i != 0,
        BigInteger big => !big.IsZero,
        double d => d != 0.0,
        string s => s.Length > 0,
        _ => true,
    };

    /// <summary><c>int(x)</c>，结果为动态值。</summary>
    public static object ToIntValue(object value) => value switch
    {
        bool b => b ? 1L : 0L,
        long or int or BigInteger => PyInt.Normalize(PyInt.ToBigInteger(value)),
        double d => PyInt.Normalize(TruncateToInteger(d, "float")),
        string s => PyInt.Normalize(PyInt.ParseDecimal(s)),
        _ => throw new PyTypeError(
            $"int() argument must be a string, a bytes-like object or a real number, not '{PyRepr.TypeName(value)}'"
        ),
    };

    /// <summary><c>float(x)</c>，结果为动态值。</summary>
    public static object ToFloatValue(object value) => value switch
    {
        bool b => b ? 1.0 : 0.0,
        long or int or BigInteger => ToDouble(value),
        double d => d,
        string s => ParseFloat(s),
        _ => throw new PyTypeError(
            $"float() argument must be a string or a real number, not '{PyRepr.TypeName(value)}'"
        ),
    };

    /// <summary><c>abs(x)</c>，结果为动态值。</summary>
    public static object AbsValue(object value)
    {
        if (PyInt.IsInt(value))
        {
            return PyInt.Abs(value);
        }
        if (value is double d)
        {
            return Math.Abs(d);
        }
        throw new PyTypeError($"bad operand type for abs(): '{PyRepr.TypeName(value)}'");
    }

    /// <summary>整数 / 布尔转 <c>double</c>；超出 <c>double</c> 范围时报 <c>OverflowError</c>。</summary>
    public static double ToDouble(object value) => value switch
    {
        bool b => b ? 1.0 : 0.0,
        double d => d,
        long l => l,
        int i => i,
        BigInteger big => BigIntegerToDouble(big),
        _ => throw new PyTypeError(
            $"must be real number, not '{PyRepr.TypeName(value)}'"
        ),
    };

    private static double BigIntegerToDouble(BigInteger value)
    {
        double result = (double)value;
        if (double.IsInfinity(result))
        {
            throw new PyOverflowError("int too large to convert to float");
        }
        return result;
    }

    /// <summary>向零截断的浮点转整数，带 NaN / Inf 检查（语义陷阱 #13）。</summary>
    public static BigInteger TruncateToInteger(double value, string context)
    {
        if (double.IsNaN(value))
        {
            throw new PyValueError($"cannot convert {context} NaN to integer");
        }
        if (double.IsInfinity(value))
        {
            throw new PyOverflowError($"cannot convert {context} infinity to integer");
        }
        return new BigInteger(Math.Truncate(value));
    }

    private static object FloatModulo(double a, double b)
    {
        if (b == 0.0)
        {
            throw new PyZeroDivisionError("float modulo");
        }
        double remainder = a % b;
        if (remainder != 0.0 && (remainder < 0) != (b < 0))
        {
            remainder += b;
        }
        return remainder;
    }

    private static object ParseFloat(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            throw new PyValueError("could not convert string to float: ''");
        }
        // CPython 接受 inf / infinity / nan（大小写不敏感），.NET 的 TryParse 也接受，
        // 但额外允许 "Nan"/"Infinity" 之外的写法差异需要显式归一化。
        string lowered = trimmed.ToLowerInvariant();
        if (lowered is "inf" or "+inf" or "infinity" or "+infinity")
        {
            return double.PositiveInfinity;
        }
        if (lowered is "-inf" or "-infinity")
        {
            return double.NegativeInfinity;
        }
        if (lowered is "nan" or "+nan" or "-nan")
        {
            return double.NaN;
        }
        if (double.TryParse(
                trimmed,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double parsed))
        {
            return parsed;
        }
        throw new PyValueError($"could not convert string to float: {PyRepr.Quote(text)}");
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is null || a is PyNone || b is null || b is PyNone)
        {
            return (a is null || a is PyNone) && (b is null || b is PyNone);
        }
        bool aInt = PyInt.IsInt(a);
        bool bInt = PyInt.IsInt(b);
        if (aInt && bInt)
        {
            return PyInt.Compare(a, b) == 0;
        }
        if (aInt && b is double bd)
        {
            return PyInt.CompareWithDouble(PyInt.ToBigInteger(a), bd) == 0;
        }
        if (bInt && a is double ad)
        {
            return PyInt.CompareWithDouble(PyInt.ToBigInteger(b), ad) == 0;
        }
        if (a is double da && b is double db)
        {
            return da == db;
        }
        if (a is string sa && b is string sb)
        {
            return string.Equals(sa, sb, StringComparison.Ordinal);
        }
        // 其它类型：M2.5 接入 __eq__，当前按身份比较
        return ReferenceEquals(a, b);
    }

    /// <summary>排序比较；返回 <c>null</c> 表示不可比较（NaN）或类型不支持（抛 TypeError）。</summary>
    private static int? Order(object a, object b, int op)
    {
        string symbol = op switch
        {
            Lt => "<",
            Le => "<=",
            Gt => ">",
            Ge => ">=",
            _ => "?",
        };
        bool aInt = PyInt.IsInt(a);
        bool bInt = PyInt.IsInt(b);
        if (aInt && bInt)
        {
            return PyInt.Compare(a, b);
        }
        if (aInt && b is double bd)
        {
            return PyInt.CompareWithDouble(PyInt.ToBigInteger(a), bd);
        }
        if (bInt && a is double ad)
        {
            int? reversed = PyInt.CompareWithDouble(PyInt.ToBigInteger(b), ad);
            return reversed is null ? null : -reversed;
        }
        if (a is double da && b is double db)
        {
            if (double.IsNaN(da) || double.IsNaN(db))
            {
                return null;
            }
            return da.CompareTo(db);
        }
        if (a is string sa && b is string sb)
        {
            return Math.Sign(string.CompareOrdinal(sa, sb));
        }
        throw new PyTypeError(
            $"'{symbol}' not supported between instances of '{PyRepr.TypeName(a)}' and '{PyRepr.TypeName(b)}'"
        );
    }

    private static void RequireInts(string symbol, object a, object b, bool aInt, bool bInt)
    {
        if (!aInt || !bInt)
        {
            throw BinaryTypeError(symbol, a, b);
        }
    }

    private static PyTypeError BinaryTypeError(string symbol, object a, object b) =>
        new(
            $"unsupported operand type(s) for {symbol}: '{PyRepr.TypeName(a)}' and '{PyRepr.TypeName(b)}'"
        );
}
