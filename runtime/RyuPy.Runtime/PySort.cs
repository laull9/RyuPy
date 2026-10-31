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
/// 自底向上的归并排序：O(n log n) 最坏情况、稳定、只需一个与输入等长的辅助数组。
/// 归并时「相等取左」即稳定性；<c>reverse</c> 通过反转比较结果实现，而不是排完再倒序，
/// 因此降序时相等元素仍保持原始顺序。
///
/// 递归归并会为每层调用建立栈帧，宽度成倍增长的迭代写法没有这个问题，也便于
/// 在「相邻两段已经有序」时整段跳过（对近似有序的输入接近 O(n)）。
/// </remarks>
public static class PySort
{
    /// <summary>原地排序。</summary>
    /// <param name="items">待排序元素。</param>
    /// <param name="reverse">是否降序（仍保持稳定）。</param>
    public static void Sort(List<object?> items, bool reverse)
    {
        int count = items.Count;
        if (count < 2)
        {
            return;
        }
        object?[] buffer = new object?[count];
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
                if (Compare(items[middle - 1], items[middle], reverse) <= 0)
                {
                    continue;
                }
                Merge(items, buffer, start, middle, end, reverse);
            }
        }
    }

    /// <summary>归并 <c>[start, middle)</c> 与 <c>[middle, end)</c> 两段。</summary>
    private static void Merge(
        List<object?> items,
        object?[] buffer,
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
            if (Compare(items[left], items[right], reverse) <= 0)
            {
                buffer[write++] = items[left++];
            }
            else
            {
                buffer[write++] = items[right++];
            }
        }
        while (left < middle)
        {
            buffer[write++] = items[left++];
        }
        while (right < end)
        {
            buffer[write++] = items[right++];
        }
        for (int i = start; i < end; i++)
        {
            items[i] = buffer[i];
        }
    }

    /// <summary>按目标顺序比较两个元素：负数表示 <paramref name="a"/> 在前。</summary>
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
