using System;
using System.Collections.Generic;

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
/// # 实现
///
/// 自底向上的归并排序：O(n log n) 最坏情况、稳定、只需与输入等长的辅助数组。
/// 计算 `key` 的结果在排序开始时**只算一次**（CPython 也如此），避免 key 函数
/// 被反复调用产生副作用或性能问题。归并时「相等取左」即稳定性；<c>reverse</c>
/// 通过反转比较结果实现，而不是排完再倒序。
/// </remarks>
public static class PySort
{
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
        var keys = new object?[count];
        for (int i = 0; i < count; i++)
        {
            keys[i] = key is null ? items[i] : key(items[i]);
        }

        var itemBuffer = new object?[count];
        var keyBuffer = new object?[count];
        for (int width = 1; width < count; width *= 2)
        {
            for (int start = 0; start < count; start += width * 2)
            {
                int middle = Math.Min(start + width, count);
                int end = Math.Min(start + width * 2, count);
                if (middle >= end)
                {
                    break;
                }
                // 两段已经符合目标顺序：跳过归并，近似有序的输入可以接近线性
                if (Compare(keys[middle - 1], keys[middle], reverse) <= 0)
                {
                    continue;
                }
                Merge(items, keys, itemBuffer, keyBuffer, start, middle, end, reverse);
            }
        }
    }

    /// <summary>归并 <c>[start, middle)</c> 与 <c>[middle, end)</c> 两段。</summary>
    private static void Merge(
        List<object?> items,
        object?[] keys,
        object?[] itemBuffer,
        object?[] keyBuffer,
        int start,
        int middle,
        int end,
        bool reverse
    )
    {
        int left = start;
        int right = middle;
        int write = start;
        while (left < middle && right < end)
        {
            // 相等时取左边：这是稳定性的唯一来源
            if (Compare(keys[left], keys[right], reverse) <= 0)
            {
                itemBuffer[write] = items[left];
                keyBuffer[write] = keys[left];
                left++;
            }
            else
            {
                itemBuffer[write] = items[right];
                keyBuffer[write] = keys[right];
                right++;
            }
            write++;
        }
        while (left < middle)
        {
            itemBuffer[write] = items[left];
            keyBuffer[write] = keys[left];
            left++;
            write++;
        }
        while (right < end)
        {
            itemBuffer[write] = items[right];
            keyBuffer[write] = keys[right];
            right++;
            write++;
        }
        for (int i = start; i < end; i++)
        {
            items[i] = itemBuffer[i];
            keys[i] = keyBuffer[i];
        }
    }

    /// <summary>按目标顺序比较两个键：负数表示 <paramref name="a"/> 在前。</summary>
    private static int Compare(object? a, object? b, bool reverse)
    {
        int result = CompareValues(a, b);
        // 反转比较结果而不是反转结果数组，保证相等元素不换位
        return reverse ? -result : result;
    }

    private static int CompareValues(object? a, object? b)
    {
        if (PyHash.Equal(a, b))
        {
            return 0;
        }
        return PyOps.Compare(PyOps.Lt, a ?? PyNone.Instance, b ?? PyNone.Instance) ? -1 : 1;
    }
}
