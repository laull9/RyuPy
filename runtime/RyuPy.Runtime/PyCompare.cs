using System;

namespace RyuPy.Runtime;

/// <summary>
/// 动态比较的通用实现：排序比较的形态分类、序列字典序与不可比较的 TypeError。
/// </summary>
/// <remarks>
/// <see cref="PyOps.Compare"/> 的入口与 long / double / str 快路径留在 <c>PyOps.cs</c>；
/// 这里是快路径未覆盖时的完整规则：int 与 float 精确比较、NaN 不可排序、
/// 只有同类序列之间可比（tuple 与 list 即使内容相同也是 TypeError）。
/// </remarks>
public static partial class PyOps
{
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
        if ((a is PyStringInstance || b is PyStringInstance)
            && PyStringLayout.TryGet(a, out string subleft) && PyStringLayout.TryGet(b, out string subright))
            return Math.Sign(string.CompareOrdinal(subleft, subright));
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
        PyList or PyListInstance => SequenceKind.List,
        PyBytes or PyByteArray => SequenceKind.Buffer,
        _ => null,
    };

    private static object?[] ElementsOf(object sequence) => sequence switch
    {
        PyTuple tuple => tuple.Items,
        PyList list => list.Items.ToArray(),
        PyListInstance list => list.Payload.Items.ToArray(),
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
}
