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
public static partial class PyOps
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

    /// <summary>按名字构造 Python 异常对象；<paramref name="arg"/> 为 <c>None</c> 时消息为空。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="arg">消息参数。</param>
    public static object MakeException(string typeName, object? arg) =>
        PyErrors.Make(typeName, arg);

    /// <summary><c>raise exc from cause</c>：返回挂了显式原因的新异常对象。</summary>
    /// <param name="exception">被抛出的异常对象。</param>
    /// <param name="cause">显式原因。</param>
    public static object WithCause(object exception, object? cause) =>
        PyErrors.WithCause(exception, cause);

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
                if (SetDifference(a, b) is object difference)
                {
                    return difference;
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
                if (SetIntersect(a, b) is object intersection)
                {
                    return intersection;
                }
                RequireInts("&", a, b, aInt, bInt);
                return PyInt.BitAnd(a, b);
            case BitOr:
                if (SetUnion(a, b) is object union)
                {
                    return union;
                }
                RequireInts("|", a, b, aInt, bInt);
                return PyInt.BitOr(a, b);
            case BitXor:
                if (SetSymmetricDifference(a, b) is object xor)
                {
                    return xor;
                }
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
    /// <remarks>
    /// 空容器为假是 Python 最常见的惯用法（<c>if not items:</c> / <c>while queue:</c>），
    /// 因此每一种容器都必须显式列出——漏掉一类会静默进入错误分支，而不是报错。
    /// 字符串按**码点**判空：UTF-16 的长度不影响「是否为空」这一结论。
    /// </remarks>
    public static bool Truthy(object? value) => value switch
    {
        null => false,
        PyNone => false,
        PyUnbound => false,
        bool b => b,
        long l => l != 0,
        int i => i != 0,
        BigInteger big => !big.IsZero,
        double d => d != 0.0,
        string s => s.Length > 0,
        PyList list => list.Count > 0,
        PyTuple tuple => tuple.Length > 0,
        PyDict dict => dict.Count > 0,
        PySet set => set.Count > 0,
        PyFrozenSet frozen => frozen.Count > 0,
        PyBytes bytes => bytes.Count > 0,
        PyByteArray array => array.Count > 0,
        PyDictView view => view.Count > 0,
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

    /// <summary>Python 的 <c>==</c>（容器、数值、字符串统一规则）。</summary>
    public static bool ValuesEqual(object? a, object? b) => PyHash.Equal(a, b);

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
        SequenceKind? kindA = SequenceKindOf(a);
        SequenceKind? kindB = SequenceKindOf(b);
        if (kindA is { } ka && kindB is { } kb)
        {
            // 序列之间只在同类可比：tuple 与 list 即使内容一致也是 TypeError
            if (ka != kb)
            {
                throw Incomparable(symbol, a, b);
            }
            return Lexicographic(ElementsOf(a), ElementsOf(b), op, symbol);
        }
        if (kindA is not null || kindB is not null)
        {
            throw Incomparable(symbol, a, b);
        }
        throw Incomparable(symbol, a, b);
    }

    /// <summary>序列族：只有同族的序列可以互相排序。</summary>
    private enum SequenceKind
    {
        Tuple,
        List,
        Buffer,
    }

    private static SequenceKind? SequenceKindOf(object value) => value switch
    {
        PyTuple => SequenceKind.Tuple,
        PyList => SequenceKind.List,
        PyBytes or PyByteArray => SequenceKind.Buffer,
        _ => null,
    };

    private static object?[] ElementsOf(object sequence) => sequence switch
    {
        PyTuple tuple => tuple.Items,
        PyList list => list.Items.ToArray(),
        PyBytes bytes => BytesToObjects(bytes.Bytes),
        PyByteArray buffer => BytesToObjects(buffer.Bytes),
        _ => [],
    };

    private static object?[] BytesToObjects(System.Collections.Generic.IReadOnlyList<byte> bytes)
    {
        var items = new object?[bytes.Count];
        for (int i = 0; i < bytes.Count; i++)
        {
            items[i] = (long)bytes[i];
        }
        return items;
    }

    /// <summary>
    /// 序列的字典序比较：逐元素先判等再排序，前缀相同则短的更小。
    /// </summary>
    /// <remarks>
    /// 与 CPython 一致，<c>(1, 2) &lt; (1, "a")</c> 抛的 TypeError 里写的<em>元素</em>类型，
    /// 不是容器类型——所以这里递归回 <see cref="Order"/>，让它按元素重新生成诊断。
    /// </remarks>
    private static int Lexicographic(object?[] a, object?[] b, int op, string symbol)
    {
        int shared = Math.Min(a.Length, b.Length);
        for (int i = 0; i < shared; i++)
        {
            object? left = a[i];
            object? right = b[i];
            bool leftNone = left is null or PyNone;
            bool rightNone = right is null or PyNone;
            if (leftNone && rightNone)
            {
                continue;
            }
            if (leftNone || rightNone)
            {
                throw Incomparable(symbol, left ?? PyNone.Instance, right ?? PyNone.Instance);
            }
            if (!PyHash.Equal(left, right))
            {
                return Order(left!, right!, op)
                    ?? throw Incomparable(symbol, left!, right!);
            }
        }
        return a.Length.CompareTo(b.Length);
    }

    private static PyTypeError Incomparable(string symbol, object a, object b) =>
        new(
            $"'{symbol}' not supported between instances of '{PyRepr.TypeName(a)}' and '{PyRepr.TypeName(b)}'"
        );

    /// <summary>集合的并集；任一侧不是集合时返回 <c>null</c>。</summary>
    private static object? SetUnion(object a, object b) => (a, b) switch
    {
        (PySet left, PySet right) => left.Union(right),
        (PySet left, PyFrozenSet right) => new PySet(left.Union(new PySet(right.Items)).Items),
        (PyFrozenSet left, PyFrozenSet right) => left.Union(right),
        (PyFrozenSet left, PySet right) => new PySet(left.Union(new PyFrozenSet(right.Items)).Items),
        _ => null,
    };

    /// <summary>集合的交集。</summary>
    private static object? SetIntersect(object a, object b) => (a, b) switch
    {
        (PySet left, PySet right) => left.Intersect(right),
        (PySet left, PyFrozenSet right) => new PySet(left.Intersect(new PySet(right.Items)).Items),
        (PyFrozenSet left, PyFrozenSet right) => left.Intersect(right),
        (PyFrozenSet left, PySet right) => new PySet(left.Intersect(new PyFrozenSet(right.Items)).Items),
        _ => null,
    };

    /// <summary>集合的差集。</summary>
    private static object? SetDifference(object a, object b) => (a, b) switch
    {
        (PySet left, PySet right) => left.Difference(right),
        (PySet left, PyFrozenSet right) => new PySet(left.Difference(new PySet(right.Items)).Items),
        (PyFrozenSet left, PyFrozenSet right) => left.Difference(right),
        (PyFrozenSet left, PySet right) => new PySet(left.Difference(new PyFrozenSet(right.Items)).Items),
        _ => null,
    };

    /// <summary>集合的对称差。</summary>
    private static object? SetSymmetricDifference(object a, object b) => (a, b) switch
    {
        (PySet left, PySet right) => left.SymmetricDifference(right),
        (PySet left, PyFrozenSet right) => new PySet(left.SymmetricDifference(new PySet(right.Items)).Items),
        (PyFrozenSet left, PyFrozenSet right) => left.SymmetricDifference(right),
        (PyFrozenSet left, PySet right) => new PySet(left.SymmetricDifference(new PyFrozenSet(right.Items)).Items),
        _ => null,
    };

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
