using System;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>tuple</c>：不可变序列。
/// </summary>
/// <remarks>
/// 元素存储保持 <c>object?[]</c>；长度、索引及存储入口保持非虚方法。
/// 允许内部namedtuple使用专用派生载体，普通tuple不增加类引用字段。
/// 解除sealed会扩大CLR类型匹配范围并可能改变JIT决策，不代表零成本。
/// </remarks>
public class PyTuple
{
    /// <summary>元素数组。</summary>
    public object?[] Items { get; }

    /// <summary>用元素数组构造。</summary>
    /// <param name="items">元素。</param>
    public PyTuple(object?[] items) => Items = items;

    /// <summary>元素个数。</summary>
    public int Length => Items.Length;

    /// <summary>按 Python 下标规则取元素（支持负数，越界抛 <c>IndexError</c>）。</summary>
    /// <param name="index">下标。</param>
    public object? GetItem(long index)
    {
        long normalized = index < 0 ? index + Items.Length : index;
        if (normalized < 0 || normalized >= Items.Length)
        {
            throw new PyIndexError($"tuple index out of range");
        }
        return Items[normalized];
    }

    /// <summary>按下标或切片取元素。</summary>
    /// <param name="key">整数下标或切片。</param>
    public object GetItem(object key)
    {
        if (key is PySlice slice)
        {
            var (start, _, step, count) = slice.Indices(Items.Length);
            var result = new object?[count];
            long index = start;
            for (long i = 0; i < count; i++, index += step)
            {
                result[i] = Items[(int)index];
            }
            return new PyTuple(result);
        }
        return GetItem(key switch
        {
            bool b => b ? 1L : 0L,
            long l => l,
            int i => i,
            _ => throw new PyTypeError(
                $"tuple indices must be integers or slices, not {PyRepr.TypeName(key)}"
            ),
        })!;
    }

    /// <summary><c>x in tuple</c>。</summary>
    /// <param name="value">被查找的值。</param>
    public bool Contains(object? value)
    {
        foreach (object? item in Items)
        {
            if (PyHash.Equal(item, value))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>按 <c>repr</c> 规则格式化。</summary>
    public override string ToString() => PyRepr.Repr(this);
}
