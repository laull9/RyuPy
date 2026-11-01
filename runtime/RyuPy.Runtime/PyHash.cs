using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// Python 的相等性与哈希规则。
/// </summary>
/// <remarks>
/// 单独成文件的原因：字典、集合、<c>list.index/count/remove</c>、<c>in</c> 判定都依赖同一套
/// 规则，任何一处走样都会表现为「同一个键在字典里查不到」这类很难查的 bug。
///
/// 规则要点：
/// <list type="bullet">
/// <item><c>True == 1 == 1.0</c>，且三者哈希相同（CPython 里 <c>hash(True) == hash(1)</c>）；</item>
/// <item>数值与字符串按值比较，其余内置容器按内容比较；</item>
/// <item>只有「不可变」的对象可哈希：list / dict / set / bytearray 抛 <c>TypeError</c>；</item>
/// <item>NaN 不等于自身；字典 / 集合查询按身份命中同一个 nan 对象。</item>
/// </list>
/// </remarks>
/// <remarks>
/// 身份判断的位置是刻意的：Python 里 <c>nan == nan</c> 为 False，但 <c>nan in [nan]</c>、
/// <c>d[nan]</c>（同一个对象）为 True——前者走 <c>PyObject_RichCompare</c>，后者走带指针快判的
/// <c>PyObject_RichCompareBool</c>。因此 <see cref="Equal"/> 不含身份兜底，而
/// <see cref="PyKeyComparer"/> 有，两边不能合并。
/// </remarks>
public static class PyHash
{
    /// <summary>字符串哈希缓存的启用阈值（字符数）。</summary>
    private const int StringHashCacheThreshold = 32;

    /// <summary>
    /// 字符串 → 已算好的哈希。用弱表，避免把长字符串键永久钉在内存里。
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        string,
        System.Runtime.CompilerServices.StrongBox<long>
    > StringHashes = new();

    /// <summary>计算 Python 哈希值。</summary>
    /// <param name="value">任意值。</param>
    public static long Of(object? value) => value switch
    {
        null or PyNone => 0x1F2A3B4C5D6E7F80L,
        // bool 与 int 的哈希必须一致：hash(True) == hash(1)
        bool b => b ? 1 : 0,
        long l => l,
        int i => i,
        BigInteger big => BigIntegerHash(big),
        double d => DoubleHash(d),
        string s => StringHash(s),
        PyTuple tuple => TupleHash(tuple),
        PyFrozenSet frozen => SetHash(frozen),
        PyBytes bytes => BytesHash(bytes),
        PyList => throw Unhashable("list"),
        PyDict => throw Unhashable("dict"),
        PySet => throw Unhashable("set"),
        PyByteArray => throw Unhashable("bytearray"),
        // 普通对象：有 __hash__ 就用它，否则按身份哈希（与 CPython 默认一致）。
        _ => CustomHash(value)
            ?? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value),
    };

    /// <summary>调用用户 `__hash__`；没有定义时返回 <c>null</c>。</summary>
    private static long? CustomHash(object? value)
    {
        if (!PyObject.TryMethod(value, "__hash__", [], out object? result))
        {
            return null;
        }
        return PyCast.ToInt64(result);
    }

    /// <summary>该值能否作为字典键 / 集合元素。</summary>
    /// <param name="value">任意值。</param>
    public static bool IsHashable(object? value) =>
        value is not (PyList or PyDict or PySet or PyByteArray);

    /// <summary>Python 的 <c>==</c>。</summary>
    /// <param name="a">左操作数。</param>
    /// <param name="b">右操作数。</param>
    public static bool Equal(object? a, object? b)
    {
        // 最常见的键 / 元素类型先比：不可能有用户 __eq__，结果与下面的通用顺序一致
        if (a is long x && b is long y)
        {
            return x == y;
        }
        if (a is string sa0 && b is string sb0)
        {
            return string.Equals(sa0, sb0, StringComparison.Ordinal);
        }
        // NaN 必须排在身份判断之前：即使是同一个 nan 对象，Python 里 nan == nan 也是 False。
        // 字典/集合查询用的身份兜底单独放在 <see cref="PyKeyComparer"/> 里，与 CPython 一致。
        if (a is double na && double.IsNaN(na))
        {
            return false;
        }
        if (b is double nb && double.IsNaN(nb))
        {
            return false;
        }
        if (ReferenceEquals(a, b))
        {
            return true;
        }
        // 用户 `__eq__`：先试左侧，再试右侧（与 CPython 的富比较顺序一致）。
        if (PyObject.TryMethod1(a, "__eq__", b, out object? leftResult)
            && leftResult is not PyNotImplemented)
        {
            return PyOps.Truthy(leftResult);
        }
        if (PyObject.TryMethod1(b, "__eq__", a, out object? rightResult)
            && rightResult is not PyNotImplemented)
        {
            return PyOps.Truthy(rightResult);
        }
        if (a is null or PyNone || b is null or PyNone)
        {
            return (a is null or PyNone) && (b is null or PyNone);
        }
        // bool 先于数值判断：CLR 里 bool 不继承 long，但 Python 里 True == 1
        if (a is bool ba)
        {
            return b is bool bb ? ba == bb : NumericEqual(ba ? 1 : 0, b);
        }
        if (b is bool bb2)
        {
            return NumericEqual(bb2 ? 1 : 0, a);
        }
        if (a is string sa)
        {
            return b is string sb && string.Equals(sa, sb, StringComparison.Ordinal);
        }
        if (a is PyTuple ta)
        {
            return b is PyTuple tb && SequenceEqual(ta.Items, tb.Items);
        }
        switch (a)
        {
            case PyList la:
                return b is PyList lb && SequenceEqual(la.Items, lb.Items);
            case PyDict da:
                return b is PyDict db && DictEqual(da, db);
            case PySet s1:
                return b is PySet s2 && s1.SetEquals(s2);
            case PyFrozenSet f1:
                return b is PyFrozenSet f2 && f1.SetEquals(f2);
            case PyBytes y1:
                return b is PyBytes or PyByteArray && BytesEqual(y1.Bytes, b);
            case PyByteArray y3:
                return b is PyBytes or PyByteArray && BytesEqual(y3.Bytes, b);
        }
        if (IsNumber(a) && IsNumber(b))
        {
            return NumericEqual(a, b);
        }
        return false;
    }

    /// <summary>是否为 Python 数值（bool 也算）。</summary>
    /// <param name="value">任意值。</param>
    public static bool IsNumber(object? value) =>
        value is bool or long or int or BigInteger or double;

    private static bool NumericEqual(object a, object b)
    {
        if (a is double da)
        {
            return b is double db ? da == db : CompareExact(b, da) == 0;
        }
        if (b is double db2)
        {
            return CompareExact(a, db2) == 0;
        }
        // 小整数快路径：绝大多数比较都是两个 long，不需要构造 BigInteger
        if (TryGetI64(a, out long left) && TryGetI64(b, out long right))
        {
            return left == right;
        }
        return PyInt.ToBigInteger(a) == PyInt.ToBigInteger(b);
    }

    /// <summary>把 Python 整数（含 <c>bool</c>）读成 <c>long</c>；超出 i64 时返回 <c>false</c>。</summary>
    private static bool TryGetI64(object value, out long result)
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

    /// <summary>
    /// 整数与浮点数的精确比较：<c>PyInt.CompareWithDouble</c> 是唯一不会丢精度的入口。
    /// </summary>
    /// <remarks>
    /// 直接把 int 转成 <c>double</c> 再比较会静默出错，例如
    /// <c>9007199254740993 == 9007199254740992.0</c>：两者转 double 后同为 9007199254740992.0，
    /// 会得到 True，而 CPython 是按精确值比较的，结果是 False。NaN 返回非 0（不相等）。
    /// </remarks>
    private static int CompareExact(object integer, double value) =>
        PyInt.CompareWithDouble(PyInt.ToBigInteger(integer), value) ?? -1;

    private static bool SequenceEqual(
        System.Collections.Generic.IReadOnlyList<object?> a,
        System.Collections.Generic.IReadOnlyList<object?> b
    )
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (!Equal(a[i], b[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool DictEqual(PyDict a, PyDict b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        foreach (var (key, value) in a.Entries())
        {
            if (!b.TryGet(key, out object? other) || !Equal(value, other))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// <c>bytes</c> 与 <c>bytearray</c> 按内容相等（CPython 里跨这两个类型也成立）。
    /// </summary>
    /// <remarks>
    /// 注意它们<em>不可哈希</em>的对象是 bytearray 一侧——用字面量对比可读范围内的值即可，
    /// 不引入哈希概念：<c>{b"a": 1}</c> 合法而 <c>{bytearray(b"a"): 1}</c> 抛 TypeError。
    /// </remarks>
    private static bool BytesEqual(
        System.Collections.Generic.IReadOnlyList<byte> left,
        object right
    )
    {
        byte[] other = right switch
        {
            PyBytes y => y.Bytes,
            PyByteArray buffer => [.. buffer.Bytes],
            _ => [],
        };
        if (left.Count != other.Length)
        {
            return false;
        }
        for (int i = 0; i < left.Count; i++)
        {
            if (left[i] != other[i])
            {
                return false;
            }
        }
        return true;
    }

    private static long StringHash(string text)
    {
        // CPython 的字符串哈希带随机种子（PYTHONHASHSEED）。这里用确定性实现：
        // 只要「相等字符串哈希相同」且分布合理，语义就成立；具体数值不承诺与 CPython 一致。
        //
        // 长字符串作为字典键时会反复哈希；重算一次是 O(n)，在热循环里会退化成
        // 「每次查找都扫一遍键」。为长度达到阈值的字符串缓存结果（字符串不可变，
        // 缓存不需要失效逻辑），短字符串直接重算——填表本身比计算更贵。
        if (text.Length >= StringHashCacheThreshold
            && StringHashes.TryGetValue(text, out StrongBox<long>? cached))
        {
            return cached.Value;
        }
        const long Modulus = (1L << 61) - 1;
        long hash = 0;
        foreach (char ch in text)
        {
            hash = (hash * 1000003 + ch) % Modulus;
        }
        long result = FitsSigned(hash);
        if (text.Length >= StringHashCacheThreshold)
        {
            // AddOrUpdate 处理并发下的重复插入；值相同，谁赢都无所谓
            StringHashes.AddOrUpdate(text, new StrongBox<long>(result));
        }
        return result;
    }

    private static long BytesHash(PyBytes bytes)
    {
        const long Modulus = (1L << 61) - 1;
        long hash = 0;
        foreach (byte b in bytes.Span)
        {
            hash = (hash * 1000003 + b) % Modulus;
        }
        return FitsSigned(hash);
    }

    private static long TupleHash(PyTuple tuple)
    {
        long hash = 0x345678;
        foreach (object? item in tuple.Items)
        {
            hash = unchecked(hash * 1000003 ^ Of(item));
        }
        return FitsSigned(hash);
    }

    private static long SetHash(PyFrozenSet set)
    {
        long hash = 0;
        foreach (object? item in set.Items)
        {
            hash = unchecked(hash ^ Of(item));
        }
        return FitsSigned(hash);
    }

    private static long BigIntegerHash(BigInteger value)
    {
        // CPython：hash(n) = n mod (2^61 - 1)，负数再取一次补码风格
        const long Modulus = (1L << 61) - 1;
        BigInteger mod = BigInteger.Remainder(value, Modulus);
        if (mod.Sign < 0)
        {
            mod += Modulus;
        }
        long result = (long)mod;
        return value.Sign < 0 ? -result : result;
    }

    private static long DoubleHash(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }
        if (double.IsInfinity(value))
        {
            return value > 0 ? 314159 : -314159;
        }
        // 与整数值相等的浮点必须哈希到同一个值：hash(1.0) == hash(1)
        if (Math.Floor(value) == value && Math.Abs(value) < 9.2e18)
        {
            return (long)value;
        }
        return BitConverter.DoubleToInt64Bits(value);
    }

    private static long FitsSigned(long value) => value;

    private static PyTypeError Unhashable(string typeName) =>
        new($"unhashable type: '{typeName}'");
}

/// <summary>
/// 字典 / 集合的键包装：把「Python 相等 + 哈希」映射成 CLR 的 <see cref="IEqualityComparer{T}"/>。
/// </summary>
public sealed class PyKeyComparer : IEqualityComparer<object>
{
    /// <summary>共享实例。</summary>
    public static readonly PyKeyComparer Instance = new();

    /// <summary>按 Python 规则比较相等（显式实现，避免与 <see cref="object.Equals(object)"/> 混淆）。</summary>
    /// <remarks>
    /// 比数值相等多一层身份快判，对应 CPython 字典查找里的 <c>ep-&gt;me_key == key</c> 指针比较：
    /// 同一个 nan 对象作为键时必须能查到自己。
    /// </remarks>
    /// <param name="x">左值。</param>
    /// <param name="y">右值。</param>
    bool IEqualityComparer<object>.Equals(object? x, object? y) =>
        ReferenceEquals(x, y) || PyHash.Equal(x, y);

    /// <summary>按 Python 规则计算哈希。</summary>
    /// <param name="obj">键。</param>
    public int GetHashCode(object obj)
    {
        long hash = PyHash.Of(obj);
        return unchecked((int)(hash ^ (hash >> 32)));
    }
}
