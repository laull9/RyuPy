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

    /// <summary>装箱后的 <c>True</c> 单例。</summary>
    public static readonly object TrueObject = true;

    /// <summary>装箱后的 <c>False</c> 单例。</summary>
    public static readonly object FalseObject = false;

    /// <summary>把 CIL <c>bool</c> 装箱成 Python <c>bool</c>：复用单例，不分配。</summary>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining
    )]
    public static object BoxBool(bool value) => value ? TrueObject : FalseObject;

    /// <summary>把 CIL <c>long</c> 装箱成 Python <c>int</c>：[-5, 256] 复用缓存（见 PyInt.Box）。</summary>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining
    )]
    public static object BoxInt(long value) => PyInt.Box(value);

    /// <summary>超出 i64 的整数字面量：源码里是十进制字符串，构造期转成任意精度整数。</summary>
    public static object FromDecimalString(string text) => PyInt.Normalize(PyInt.ParseDecimal(text));

    /// <summary>二元运算：返回 Python 值（可能升级为 <c>BigInteger</c> 或 <c>double</c>）。</summary>
    /// <remarks>操作码带 <see cref="InPlaceFlag"/> 时按增量赋值处理（见 <c>PySequenceOps.cs</c>）。</remarks>
    public static object Binary(int op, object a, object b)
    {
        if ((op & InPlaceFlag) != 0)
        {
            return InPlaceBinary(op & ~InPlaceFlag, a, b);
        }
        return BinaryCore(op, a, b, inplace: false);
    }

    /// <summary>二元运算的通用实现；<paramref name="inplace"/> 只影响错误消息里的运算符。</summary>
    private static object BinaryCore(int op, object a, object b, bool inplace)
    {
        // 精确内建字符串的 % 已实现格式化槽，不能让右侧 __rmod__ 抢在转换前执行。
        // 其它类型仍沿既有二元/反射协议；数值取模不受此入口影响。
        if (op == Mod && a is string format) return PyPercentFormat.Format(format, b);
        if (op == Mod && a is PyStringInstance subformat) return PyPercentFormat.Format(subformat.Value, b);
        if (op == BitOr && (a is PyDictInstance || b is PyDictInstance)
            && a is not PyMappingProxy and not PyOrderedDict && b is not PyMappingProxy and not PyOrderedDict)
            return PyDictSubclassDispatch.Union(a, b, inplace);
        if (TryBinaryMagic(op, a, b, out object? magic))
        {
            return magic!;
        }
        if (a is PyDictView { UseLayoutProtocol: true } || b is PyDictView { UseLayoutProtocol: true })
            throw PyDictLayout.Own(new PyNotImplementedError("dict view set operations are not supported"));
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
                if ((a is PyStringInstance || b is PyStringInstance)
                    && PyStringLayout.TryGet(a, out string subleft) && PyStringLayout.TryGet(b, out string subright))
                    return string.Concat(subleft, subright);
                if (TryConcat(a, b, out object? concatenated))
                {
                    return concatenated!;
                }
                throw BinaryTypeError("+", a, b, inplace);
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
                throw BinaryTypeError("-", a, b, inplace);
            case Mul:
                if (aInt && bInt)
                {
                    return PyInt.Multiply(a, b);
                }
                if (aNum && bNum)
                {
                    return ToDouble(a) * ToDouble(b);
                }
                if (TryRepeat(a, b, out object? repeated))
                {
                    return repeated!;
                }
                throw BinaryTypeError("*", a, b, inplace);
            case TrueDiv:
                if (!aNum || !bNum)
                {
                    throw BinaryTypeError("/", a, b, inplace);
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
                throw BinaryTypeError("//", a, b, inplace);
            case Mod:
                if (aInt && bInt)
                {
                    return PyInt.Modulo(a, b);
                }
                if (aNum && bNum)
                {
                    return FloatModulo(ToDouble(a), ToDouble(b));
                }
                throw BinaryTypeError("%", a, b, inplace);
            case Pow:
                if (aInt && bInt)
                {
                    return PyInt.Power(a, b);
                }
                if (aNum && bNum)
                {
                    return PyPowerBuiltin.FloatPower(ToDouble(a), ToDouble(b));
                }
                throw BinaryTypeError(inplace ? "**" : "** or pow()", a, b, inplace);
            case BitAnd:
                if (SetIntersect(a, b) is object intersection)
                {
                    return intersection;
                }
                RequireInts("&", a, b, aInt, bInt, inplace);
                return PyInt.BitAnd(a, b);
            case BitOr:
                return BinaryOrValues(a, b, inplace);
            case BitXor:
                if (SetSymmetricDifference(a, b) is object xor)
                {
                    return xor;
                }
                RequireInts("^", a, b, aInt, bInt, inplace);
                return PyInt.BitXor(a, b);
            case LShift:
                RequireInts("<<", a, b, aInt, bInt, inplace);
                return PyInt.ShiftLeft(a, b);
            case RShift:
                RequireInts(">>", a, b, aInt, bInt, inplace);
                return PyInt.ShiftRight(a, b);
            default:
                throw new PyRuntimeError($"unknown binary opcode {op}");
        }
    }

    /// <summary>位或的内建值叶子，不再次分派用户运算槽；proxy已尝试两候选后复用。</summary>
    internal static object BinaryOrValues(object a, object b, bool inplace)
    {
        if (a is PyDictInstance || b is PyDictInstance)
        {
            object result = a is PyDictInstance
                ? PyDictProtocols.Invoke("__or__", a, [b])!
                : PyDictProtocols.Invoke("__ror__", b, [a])!;
            if (result is not PyNotImplemented) return result;
        }
        // 仅新增普通exactdict union；不将旧dict |=回退伪装成复制替换。
        if (!inplace && a is PyDict leftDict && b is PyDict rightDict)
            return PyMappingProxyOperators.DictUnion(leftDict, rightDict);
        if (SetUnion(a, b) is object union) return union;
        RequireInts("|", a, b, PyInt.IsInt(a), PyInt.IsInt(b), inplace);
        return PyInt.BitOr(a, b);
    }

    /// <summary>比较运算：返回 Python <c>bool</c>。NaN 下所有排序比较为假，相等为假、不等为真。</summary>
    public static bool Compare(int op, object a, object b)
    {
        // 快路径：两侧都是 long / double / str 时与通用路径结果相同（不涉及用户魔术方法），
        // 但跳过了 `__eq__` / `__lt__` 探测与 Order 的形态分类。double 的 IEEE 比较本身就满足
        // Python 的 NaN 规则（相等为假、不等为真、排序比较为假）。
        if (a is long x && b is long y)
        {
            return CompareOrdered(op, x.CompareTo(y));
        }
        if (a is double dx && b is double dy)
        {
            return op switch
            {
                Eq => dx == dy,
                Ne => dx != dy,
                Lt => dx < dy,
                Le => dx <= dy,
                Gt => dx > dy,
                Ge => dx >= dy,
                _ => throw new PyRuntimeError($"unknown compare opcode {op}"),
            };
        }
        if (a is string sa && b is string sb)
        {
            return CompareOrdered(op, Math.Sign(string.CompareOrdinal(sa, sb)));
        }
        if ((a is PyStringInstance || b is PyStringInstance)
            && PyStringLayout.TryGet(a, out string subleft) && PyStringLayout.TryGet(b, out string subright))
            return CompareOrdered(op, Math.Sign(string.CompareOrdinal(subleft, subright)));
        if (op is Eq or Ne && (a is PyOrderedDict or PyOrderedDictView || b is PyOrderedDict or PyOrderedDictView))
            return PyOrderedDictComparison.Expression(op, a, b);
        if (a is PyDictView { UseLayoutProtocol: true } || b is PyDictView { UseLayoutProtocol: true })
            return PyDictViewComparisonDispatch.Compare(op, a, b, expression: true);
        if (op is Eq or Ne && (a is PyDictInstance || b is PyDictInstance))
            return PyDictComparisonDispatch.Compare(op, a, b);
        if (op is Eq or Ne && (a is PyListInstance || b is PyListInstance))
            return PyListComparisonDispatch.Compare(op, a, b);
        if (PyOrdinaryRichComparison.TryCompare(op, a, b, out object? ordinary))
            return Truthy(ordinary);
        if (op == Eq)
        {
            return ValuesEqual(a, b);
        }
        if (op == Ne)
        {
            if (a is PyInstance leftGeneric) leftGeneric.Class.GuardInheritedGenericProtocol("__ne__");
            if (b is PyInstance rightGeneric) rightGeneric.Class.GuardInheritedGenericProtocol("__ne__");
            return !ValuesEqual(a, b);
        }
        if (a is PySlice leftSlice && b is PySlice rightSlice)
            return leftSlice.CompareFields(op, rightSlice);
        PySlice.GuardMixedComparison(a, b);
        if (TryRichCompare(op, a, b, out bool compared))
        {
            return compared;
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

    /// <summary>把三路比较结果（负 / 零 / 正）换算成指定比较运算的布尔值。</summary>
    private static bool CompareOrdered(int op, int order) => op switch
    {
        Eq => order == 0,
        Ne => order != 0,
        Lt => order < 0,
        Le => order <= 0,
        Gt => order > 0,
        Ge => order >= 0,
        _ => throw new PyRuntimeError($"unknown compare opcode {op}"),
    };

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
                if (TryUnaryMagic("__neg__", value, out object? negated))
                {
                    return negated!;
                }
                throw new PyTypeError($"bad operand type for unary -: '{PyRepr.TypeName(value)}'");
            case Pos:
                if (PyInt.IsInt(value) || value is double)
                {
                    return value;
                }
                if (TryUnaryMagic("__pos__", value, out object? positive))
                {
                    return positive!;
                }
                throw new PyTypeError($"bad operand type for unary +: '{PyRepr.TypeName(value)}'");
            case Not:
                return !Truthy(value);
            case Invert:
                if (PyInt.IsInt(value))
                {
                    return PyInt.Invert(value);
                }
                if (TryUnaryMagic("__invert__", value, out object? inverted))
                {
                    return inverted!;
                }
                throw new PyTypeError($"bad operand type for unary ~: '{PyRepr.TypeName(value)}'");
            default:
                throw new PyRuntimeError($"unknown unary opcode {op}");
        }
    }

    /// <summary><c>int(x)</c>，结果为动态值。</summary>
    public static object ToIntValue(object value) => value switch
    {
        bool b => b ? 1L : 0L,
        long or int or BigInteger => PyInt.Normalize(PyInt.ToBigInteger(value)),
        double d => PyInt.Normalize(TruncateToInteger(d, "float")),
        string s => PyIntText.Parse(s, 10),
        _ => PyIntConstructor.ConvertObject(value),
    };

    /// <summary><c>float(x)</c>，结果为动态值。</summary>
    public static object ToFloatValue(object value) => value switch
    {
        bool b => b ? 1.0 : 0.0,
        long or int => ToDouble(value),
        BigInteger big => PyFloatConstructor.FromBigInteger(big),
        double => value,
        _ => PyFloatConstructor.ConvertObject(value),
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
        if (PyObject.TryMethod(value, "__abs__", [], out object? result)) return result ?? PyNone.Instance;
        throw (PyBaseException)PyErrors.PrepareRaise(
            new PyTypeError($"bad operand type for abs(): '{PyRepr.TypeName(value)}'"));
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

    private static void RequireInts(
        string symbol,
        object a,
        object b,
        bool aInt,
        bool bInt,
        bool inplace = false
    )
    {
        if (!aInt || !bInt)
        {
            throw BinaryTypeError(symbol, a, b, inplace);
        }
    }

    /// <summary>运算类型不支持；增量赋值的运算符写成 <c>+=</c>（与 CPython 一致）。</summary>
    private static PyTypeError BinaryTypeError(string symbol, object a, object b, bool inplace = false) =>
        new(
            $"unsupported operand type(s) for {symbol}{(inplace ? "=" : "")}: '{PyRepr.TypeName(a)}' and '{PyRepr.TypeName(b)}'"
        );
}
