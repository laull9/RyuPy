using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 序列的 <c>+</c> / <c>*</c> 与增量赋值（<c>x op= y</c>）的原地语义。
/// </summary>
/// <remarks>
/// # 序列拼接与重复
///
/// <c>list + list</c>、<c>tuple + tuple</c>、<c>str * int</c>、<c>[0] * n</c> 等都返回**新对象**；
/// 只允许同类拼接（<c>[1] + (2,)</c> 是 TypeError），重复次数 ≤ 0 得到空序列，
/// <c>bool</c> 视为 0 / 1。错误消息按 CPython 3.12 原文。
///
/// # 增量赋值
///
/// HIR 把 <c>x += y</c> 标成 inplace，codegen 把 <see cref="InPlaceFlag"/> 并进操作码。
/// 可变对象必须**原地修改并返回自身**，否则别名看不到修改：
///
/// <code>
/// a = [1]; b = a
/// a += [2]      # b 也是 [1, 2]，且 a is b
/// </code>
///
/// 覆盖 list（<c>+=</c> 接受任意可迭代对象、<c>*=</c>）、bytearray、set（<c>|= &amp;= -= ^=</c>）
/// 与定义了 <c>__iadd__</c> 等方法的实例；其它值（int / float / str / tuple ...）不可变，
/// 结果与普通二元运算相同，只是错误消息里的运算符写成 <c>+=</c>。
/// </remarks>
public static partial class PyOps
{
    /// <summary>增量赋值的操作码标记位。**与 MIR <c>DynOp::INPLACE_FLAG</c> 同步。**</summary>
    public const int InPlaceFlag = 0x100;

    private static readonly string[] InPlaceMagicNames =
    {
        "", "__iadd__", "__isub__", "__imul__", "__itruediv__", "__ifloordiv__",
        "__imod__", "__ipow__", "__iand__", "__ior__", "__ixor__", "__ilshift__", "__irshift__",
    };

    /// <summary>该值是否可能有不同于普通二元运算的原地语义（快路径据此跳过不可变值）。</summary>
    internal static bool HasInPlaceSlot(object a) =>
        a is PyList or PyByteArray or PySet or PyInstance or PyNativeObject;

    /// <summary>增量赋值：可变对象原地修改并返回自身，否则退回普通二元运算。</summary>
    /// <param name="op">不含标记位的二元操作码。</param>
    /// <param name="a">左操作数（赋值目标的当前值）。</param>
    /// <param name="b">右操作数。</param>
    public static object InPlaceBinary(int op, object a, object b)
    {
        switch (a)
        {
            case PyList list when op == Add:
                // list += 任意可迭代对象（与 list + list 不同，这里不要求同类）
                list.Extend(b);
                return list;
            case PyList list when op == Mul:
                RepeatInPlace(list.Items, RepeatCount(a, b));
                return list;
            case PyByteArray array when op == Add && b is PyBytes or PyByteArray or IPyBufferView:
                array.Extend(b);
                return array;
            case PyByteArray array when op == Mul:
                int times = RepeatCount(a, b);
                array.EnsureCanResize(array.Count * (times - 1));
                int previousCount = array.Count;
                RepeatInPlace(array.Bytes, times);
                array.RecordResize(previousCount);
                return array;
            case PySet set when b is PySet or PyFrozenSet:
                if (SetInPlace(op, set, b is PySet s ? s.Items : ((PyFrozenSet)b).Items))
                {
                    return set;
                }
                break;
            case PyDictInstance dict when op == BitOr:
                return PyDictSubclassDispatch.Union(dict, b, inplace: true);
            case PyListInstance list when op == Add:
                return PyListProtocols.InPlaceAdd(list, b);
            case PyListInstance list when op == Mul:
                return PyListProtocols.InPlaceMultiply(list, b);
            case PyInstance or PyNativeObject when op > 0 && op < InPlaceMagicNames.Length:
                if (PyObject.TryMethod(a, InPlaceMagicNames[op], [b], out object? result)
                    && result is not PyNotImplemented)
                {
                    return result!;
                }
                break;
        }
        return BinaryCore(op, a, b, inplace: true);
    }

    /// <summary>集合的原地运算；不是集合运算符时返回 false。</summary>
    private static bool SetInPlace(int op, PySet set, HashSet<object> other)
    {
        switch (op)
        {
            case BitOr:
                set.Items.UnionWith(other);
                return true;
            case BitAnd:
                set.Items.IntersectWith(other);
                return true;
            case Sub:
                set.Items.ExceptWith(other);
                return true;
            case BitXor:
                set.Items.SymmetricExceptWith(other);
                return true;
            default:
                return false;
        }
    }

    /// <summary>同类序列拼接；不是序列时返回 false，类型不匹配时抛 CPython 的消息。</summary>
    private static bool TryConcat(object a, object b, out object? result)
    {
        switch (a)
        {
            case PyList left:
                if (b is not PyList right)
                {
                    if (b is PyListInstance) { result = PyListProtocols.Concat(left, b); return true; }
                    throw ConcatError("list", b);
                }
                var items = new List<object?>(left.Count + right.Count);
                items.AddRange(left.Items);
                items.AddRange(right.Items);
                result = new PyList(items);
                return true;
            case PyListInstance left:
                result = PyListProtocols.Concat(left, b);
                return true;
            case PyTuple leftTuple:
                if (b is not PyTuple rightTuple)
                {
                    throw ConcatError("tuple", b);
                }
                var joined = new object?[leftTuple.Length + rightTuple.Length];
                leftTuple.Items.CopyTo(joined, 0);
                rightTuple.Items.CopyTo(joined, leftTuple.Length);
                result = new PyTuple(joined);
                return true;
            case string:
            case PyStringInstance:
                throw ConcatError("str", b);
            case PyBytes leftBytes:
                result = new PyBytes([.. leftBytes.Bytes, .. BytesLike(b, "bytes")]);
                return true;
            case PyByteArray leftArray:
                result = new PyByteArray([.. leftArray.Bytes, .. BytesLike(b, "bytearray")]);
                return true;
        }
        result = null;
        return false;
    }

    /// <summary>序列重复：<c>seq * n</c> 与 <c>n * seq</c>。不是序列时返回 false。</summary>
    private static bool TryRepeat(object a, object b, out object? result)
    {
        object sequence;
        object count;
        if (IsSequenceForRepeat(a))
        {
            sequence = a;
            count = b;
        }
        else if (IsSequenceForRepeat(b))
        {
            sequence = b;
            count = a;
        }
        else
        {
            result = null;
            return false;
        }
        if (sequence is PyListInstance listInstance)
        {
            result = PyListProtocols.Repeat(listInstance, count);
            return true;
        }
        if (sequence is PyStringInstance subtext)
        {
            result = PyStringProtocols.Repeat(subtext, count);
            return true;
        }
        int times = RepeatCount(sequence, count);
        switch (sequence)
        {
            case string text:
                result = times == 0 || text.Length == 0
                    ? string.Empty
                    : new StringBuilder(text.Length * times).Insert(0, text, times).ToString();
                return true;
            case PyList list:
                var items = new List<object?>(list.Items);
                RepeatInPlace(items, times);
                result = new PyList(items);
                return true;
            case PyTuple tuple:
                var repeated = new object?[tuple.Length * times];
                for (int i = 0; i < times; i++)
                {
                    tuple.Items.CopyTo(repeated, i * tuple.Length);
                }
                result = new PyTuple(repeated);
                return true;
            case PyBytes bytes:
                var buffer = new byte[bytes.Count * times];
                for (int i = 0; i < times; i++)
                {
                    bytes.Bytes.CopyTo(buffer, i * bytes.Count);
                }
                result = new PyBytes(buffer);
                return true;
            default:
                var array = new List<byte>(((PyByteArray)sequence).Bytes);
                RepeatInPlace(array, times);
                result = new PyByteArray(array);
                return true;
        }
    }

    private static bool IsSequenceForRepeat(object value) =>
        value is string or PyStringInstance or PyList or PyListInstance or PyTuple or PyBytes or PyByteArray;

    /// <summary>
    /// 校验重复次数：必须是整数（bool 视为 0 / 1），负数按 0；超出可分配范围抛
    /// <c>OverflowError</c>，而不是让 .NET 抛出内部异常。CPython 对 list 报 <c>MemoryError</c>，
    /// 运行时尚无该类型，统一用 OverflowError（只在 2^31 级别的超大重复时出现）。
    /// </summary>
    private static int RepeatCount(object sequence, object count)
    {
        long times = count switch
        {
            bool flag => flag ? 1 : 0,
            long value => value,
            int value => value,
            BigInteger => throw new PyOverflowError(
                "cannot fit 'int' into an index-sized integer"
            ),
            _ => throw new PyTypeError(
                $"can't multiply sequence by non-int of type '{PyRepr.TypeName(count)}'"
            ),
        };
        if (times <= 0)
        {
            return 0;
        }
        long length = sequence switch
        {
            string text => text.Length,
            PyList list => list.Count,
            PyTuple tuple => tuple.Length,
            PyBytes bytes => bytes.Count,
            PyByteArray array => array.Count,
            _ => 1,
        };
        if (length != 0 && times > Array.MaxLength / length)
        {
            throw new PyOverflowError("repeated sequence is too long");
        }
        return (int)times;
    }

    /// <summary>把列表内容原地重复 <paramref name="times"/> 次（0 次即清空）。</summary>
    private static void RepeatInPlace<T>(List<T> items, int times)
    {
        if (times == 0)
        {
            items.Clear();
            return;
        }
        int length = items.Count;
        items.Capacity = Math.Max(items.Capacity, length * times);
        for (int i = 1; i < times; i++)
        {
            for (int j = 0; j < length; j++)
            {
                items.Add(items[j]);
            }
        }
    }

    private static byte[] BytesLike(object value, string target) => value switch
    {
        PyBytes bytes => bytes.Bytes,
        PyByteArray array => [.. array.Bytes],
        _ => throw new PyTypeError($"can't concat {PyRepr.TypeName(value)} to {target}"),
    };

    private static PyTypeError ConcatError(string type, object other) =>
        new($"can only concatenate {type} (not \"{PyRepr.TypeName(other)}\") to {type}");
}
