using System;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>tuple</c>：不可变序列。
/// </summary>
/// <remarks>
/// 现在只服务于 <c>exc.args</c> 与后续容器功能的最小子集（<c>len</c> / 下标 / <c>repr</c>）。
/// M2.3 会在这里补齐切片、解包、相等性与哈希；届时元素存储仍是 <c>object?[]</c>，
/// 因为「元组里什么都能放」正是 Python 元组的语义。
/// </remarks>
public sealed class PyTuple
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

    /// <summary>按 <c>repr</c> 规则格式化。</summary>
    public override string ToString() => PyRepr.Repr(this);
}
