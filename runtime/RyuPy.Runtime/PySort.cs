using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>
/// Python 的稳定排序（<c>sorted()</c> 与 <c>list.sort()</c> 共用）。
/// </summary>
/// <remarks>
/// # 为什么不用 <see cref="List{T}.Sort(Comparison{T})"/>
///
/// .NET 的 <c>List.Sort</c> 是内省排序（introsort），**不稳定**；且
/// <c>reverse: True</c> 通常被实现成「升序排完再整体反转」，这会把相等元素的
/// 原始顺序也颠倒过来。CPython 的排序保证稳定：键相等的元素保持输入顺序。
/// 依赖这一点的代码很常见（先按名字排、再按年龄排，等于多重键排序），
/// 排序不稳定会让结果随数据量悄悄变化，很难复现。
///
/// # 只用 <c>&lt;</c>
///
/// 与 CPython 一致，排序只调用「小于」比较：不调用 <c>__eq__</c>，也不假设 <c>==</c> 与
/// <c>&lt;</c> 一致。稳定性来自「只有右侧严格先于左侧时才取右侧」。
///
/// # 实现
///
/// 1. 计算 `key` 的结果在排序开始时**只算一次**（CPython 也如此）；
/// 2. 每 <see cref="RunLength"/> 个元素一段，做二分插入排序（小段上比较次数最少）；
/// 3. 自底向上归并，键 / 值数组与辅助数组之间来回交替（ping-pong），不逐次回写；
///    两段首尾已有序时整段复制，近似有序的输入接近线性；
/// 4. 全部在数组上完成，最后一次性写回列表。比较中途抛异常时列表保持原样。
///
/// <c>reverse</c> 通过反转「谁先于谁」实现，而不是排完再倒序。数值、字符串与元组的比较有
/// 快路径，其它类型（含用户 <c>__lt__</c>）走 <see cref="PyOps.Compare"/>。
/// </remarks>
public static class PySort
{
    /// <summary>插入排序的段长；与 CPython 的 minrun 同量级。</summary>
    private const int RunLength = 32;

    /// <summary>原地排序（元素自身作为键）。</summary>
    /// <param name="items">待排序元素。</param>
    /// <param name="reverse">是否降序（仍保持稳定）。</param>
    public static void Sort(List<object?> items, bool reverse) =>
        SortBy(items, reverse, null);

    /// <summary>原地排序，可提供 key 函数。</summary>
    /// <param name="items">待排序元素。</param>
    /// <param name="reverse">是否降序（仍保持稳定）。</param>
    /// <param name="key">键函数；<c>null</c> 表示用元素自身比较。</param>
    public static void SortBy(
        List<object?> items,
        bool reverse,
        Func<object?, object?>? key
    )
    {
        int count = items.Count;
        if (count < 2)
        {
            return;
        }
        object?[] values = items.ToArray();
        object?[]? keys = null;
        if (key is not null)
        {
            keys = new object?[count];
            for (int i = 0; i < count; i++)
            {
                keys[i] = key(values[i]);
            }
        }

        var sorter = new Sorter(reverse, keys ?? values, keys is null ? null : values);
        sorter.Run();
        sorter.Values.AsSpan().CopyTo(CollectionsMarshal.AsSpan(items));
    }

    /// <summary>一次排序的状态：键数组、可选的值数组与各自的辅助数组。</summary>
    private struct Sorter
    {
        private readonly bool _reverse;
        private object?[] _keys;
        // 无 key 函数时为 null：键就是值，只需要移动一份
        private object?[]? _values;

        public Sorter(bool reverse, object?[] keys, object?[]? values)
        {
            _reverse = reverse;
            _keys = keys;
            _values = values;
        }

        /// <summary>排序后的值（无 key 函数时就是键数组）。</summary>
        public readonly object?[] Values => _values ?? _keys;

        public void Run()
        {
            int count = _keys.Length;
            for (int start = 0; start < count; start += RunLength)
            {
                InsertionSort(start, Math.Min(start + RunLength, count));
            }
            if (count <= RunLength)
            {
                return;
            }
            object?[] keyBuffer = new object?[count];
            object?[]? valueBuffer = _values is null ? null : new object?[count];
            for (int width = RunLength; width < count; width *= 2)
            {
                for (int start = 0; start < count; start += width * 2)
                {
                    int middle = Math.Min(start + width, count);
                    int end = Math.Min(start + width * 2, count);
                    MergeInto(keyBuffer, valueBuffer, start, middle, end);
                }
                (_keys, keyBuffer) = (keyBuffer, _keys);
                if (_values is not null)
                {
                    (_values, valueBuffer) = (valueBuffer, _values);
                }
            }
        }

        /// <summary>右侧键是否必须排在左侧键之前（严格）；相等时为 false，从而保持稳定。</summary>
        private readonly bool Precedes(object? right, object? left) =>
            _reverse ? Less(left, right) : Less(right, left);

        /// <summary><c>[lo, hi)</c> 的二分插入排序：找到第一个「被 pivot 严格先于」的位置。</summary>
        private readonly void InsertionSort(int lo, int hi)
        {
            for (int i = lo + 1; i < hi; i++)
            {
                object? pivot = _keys[i];
                if (!Precedes(pivot, _keys[i - 1]))
                {
                    continue;
                }
                int left = lo;
                int right = i - 1;
                while (left < right)
                {
                    int mid = (left + right) >>> 1;
                    if (Precedes(pivot, _keys[mid]))
                    {
                        right = mid;
                    }
                    else
                    {
                        left = mid + 1;
                    }
                }
                Array.Copy(_keys, left, _keys, left + 1, i - left);
                _keys[left] = pivot;
                if (_values is not null)
                {
                    object? value = _values[i];
                    Array.Copy(_values, left, _values, left + 1, i - left);
                    _values[left] = value;
                }
            }
        }

        /// <summary>把 <c>[start, middle)</c> 与 <c>[middle, end)</c> 归并到辅助数组同一区间。</summary>
        private readonly void MergeInto(
            object?[] keyOut,
            object?[]? valueOut,
            int start,
            int middle,
            int end
        )
        {
            // 只有一段，或两段已经符合目标顺序：整段复制
            if (middle >= end || !Precedes(_keys[middle], _keys[middle - 1]))
            {
                Array.Copy(_keys, start, keyOut, start, end - start);
                if (_values is not null)
                {
                    Array.Copy(_values, start, valueOut!, start, end - start);
                }
                return;
            }
            int left = start;
            int right = middle;
            int write = start;
            while (left < middle && right < end)
            {
                // 右侧严格先于左侧才取右侧：这是稳定性的唯一来源
                int from = Precedes(_keys[right], _keys[left]) ? right++ : left++;
                keyOut[write] = _keys[from];
                if (_values is not null)
                {
                    valueOut![write] = _values[from];
                }
                write++;
            }
            int rest = left < middle ? left : right;
            int restEnd = left < middle ? middle : end;
            Array.Copy(_keys, rest, keyOut, write, restEnd - rest);
            if (_values is not null)
            {
                Array.Copy(_values, rest, valueOut!, write, restEnd - rest);
            }
        }
    }

    /// <summary><c>a &lt; b</c>；常见类型直接比较，其它交给 <see cref="PyOps.Compare"/>。</summary>
    private static bool Less(object? a, object? b)
    {
        if (a is long x && b is long y)
        {
            return x < y;
        }
        if (a is double dx && b is double dy)
        {
            return dx < dy;
        }
        if (a is string sa && b is string sb)
        {
            return string.CompareOrdinal(sa, sb) < 0;
        }
        if (a is PyTuple ta && b is PyTuple tb)
        {
            return TupleLess(ta.Items, tb.Items);
        }
        return PyOps.Compare(PyOps.Lt, a ?? PyNone.Instance, b ?? PyNone.Instance);
    }

    /// <summary>元组的字典序「小于」：跳过相等的前缀，在第一个不等的元素上比较。</summary>
    private static bool TupleLess(object?[] a, object?[] b)
    {
        int shared = Math.Min(a.Length, b.Length);
        for (int i = 0; i < shared; i++)
        {
            object? left = a[i];
            object? right = b[i];
            if (left is long x && right is long y)
            {
                if (x != y)
                {
                    return x < y;
                }
                continue;
            }
            if (left is string sa && right is string sb)
            {
                int order = string.CompareOrdinal(sa, sb);
                if (order != 0)
                {
                    return order < 0;
                }
                continue;
            }
            if (!PyHash.Equal(left, right))
            {
                return Less(left, right);
            }
        }
        return a.Length < b.Length;
    }
}
