// PIC 核心条目的固定容量惰性分页：只分配实际访问的页，保留原地 ref 读写。
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>
/// 固定 256 项一页的 PIC 条目存储；一个站点的四个槽位始终在同页。
/// </summary>
/// <remarks>
/// 容量由调用方显式传入，创建时只分配根指针数组。页面一旦发布便永久保留，
/// 因此已返回的托管引用在存储生命周期内持续有效。CAS 仅保护页面引用发布；
/// 条目字段沿用原 PIC 的同步边界。竞争失败方可能分配临时页，最终使用胜出页。
/// 读取包含 acquire 和二级寻址，速度与 RSS 收益由 NativeAOT A/B 确认。
/// </remarks>
internal sealed class PagedEntries<T> where T : struct
{
    private const int PageShift = 8;
    private const int PageSize = 1 << PageShift;
    private const int PageMask = PageSize - 1;
    private readonly int _capacity;
    private readonly T[]?[] _pages;

    internal PagedEntries(int totalCapacity)
    {
        if (totalCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalCapacity));
        _capacity = totalCapacity;
        // 避免 capacity + PageMask 在大容量时先溢出。
        _pages = new T[(totalCapacity - 1) / PageSize + 1][];
    }

    internal ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_capacity)
                ThrowIndexOutOfRange();
            int pageIndex = index >> PageShift;
            T[] page = Volatile.Read(ref _pages[pageIndex]) ?? PublishPage(pageIndex);
            return ref page[index & PageMask];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T[] PublishPage(int pageIndex)
    {
        // 从快路读到 null 后，另一个线程可能已完成发布，先复查再分配。
        T[]? existing = Volatile.Read(ref _pages[pageIndex]);
        if (existing is not null)
            return existing;
        var candidate = new T[PageSize];
        return Interlocked.CompareExchange(ref _pages[pageIndex], candidate, null) ?? candidate;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

    // 内部诊断仅用于验证惰性分配；不在命中热路累计统计。
    internal int AllocatedPageCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _pages.Length; i++)
                if (Volatile.Read(ref _pages[i]) is not null)
                    count++;
            return count;
        }
    }

    internal bool IsPageAllocated(int pageIndex) =>
        (uint)pageIndex < (uint)_pages.Length && Volatile.Read(ref _pages[pageIndex]) is not null;
}
