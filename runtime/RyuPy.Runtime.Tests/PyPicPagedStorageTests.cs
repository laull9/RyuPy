// 验证 PIC 惰性分页的引用、边界、竞争发布及真实缓存失效行为。
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>
/// PIC 条目惰性分页存储（<see cref="PagedEntries{T}"/>）的单元测试。
/// </summary>
/// <remarks>
/// 重点验证：
/// 1. ref 返回索引器的持久就地修改能力；
/// 2. 跨页边界（255/256）与 4 槽位调用点同页约束；
/// 3. 负索引、边界与越界抛出 IndexOutOfRangeException；
/// 4. 多线程并发请求同一未分配页的不同槽位时，CAS 保证页面唯一发布且无写丢失。
/// </remarks>
public sealed class PyPicPagedStorageTests
{
    private struct TestPicEntry
    {
        public byte Valid;
        public int Site;
        public string? TargetName;
    }

    [Fact]
    public void RefReturnAllowsPersistentInPlaceMutation()
    {
        var storage = new PagedEntries<TestPicEntry>(16384);
        Assert.Equal(0, storage.AllocatedPageCount);
        Assert.False(storage.IsPageAllocated(0));

        // 1. 通过 ref 获取条目并就地修改其字段
        ref TestPicEntry entry = ref storage[10];
        Assert.Equal(0, entry.Valid);
        Assert.Null(entry.TargetName);

        entry.Valid = 1;
        entry.Site = 2;
        entry.TargetName = "func_alpha";

        // 验证所在页面已被分配，且其他页保持未分配
        Assert.Equal(1, storage.AllocatedPageCount);
        Assert.True(storage.IsPageAllocated(0));
        Assert.False(storage.IsPageAllocated(1));

        // 2. 重新索引读取，验证修改已持久化于底层页面存储
        ref readonly TestPicEntry reRead = ref storage[10];
        Assert.Equal(1, reRead.Valid);
        Assert.Equal(2, reRead.Site);
        Assert.Equal("func_alpha", reRead.TargetName);

        // 3. 通过索引器直接整项赋值，同样生效
        storage[10] = new TestPicEntry
        {
            Valid = 2,
            Site = 3,
            TargetName = "func_beta",
        };

        Assert.Equal(2, storage[10].Valid);
        Assert.Equal(3, storage[10].Site);
        Assert.Equal("func_beta", storage[10].TargetName);
    }

    [Fact]
    public void PageBoundaryAndSiteAlignmentAreStrictlyPreserved()
    {
        const int capacity = 16384;
        var storage = new PagedEntries<TestPicEntry>(capacity);

        // 第 0 页末端（255）与第 1 页起始（256）
        const int page0LastIndex = 255;
        const int page1FirstIndex = 256;

        storage[page0LastIndex] = new TestPicEntry { Valid = 1, Site = 63, TargetName = "edge_page0" };
        Assert.True(storage.IsPageAllocated(0));
        Assert.False(storage.IsPageAllocated(1));
        Assert.Equal(1, storage.AllocatedPageCount);

        storage[page1FirstIndex] = new TestPicEntry { Valid = 1, Site = 64, TargetName = "edge_page1" };
        Assert.True(storage.IsPageAllocated(1));
        Assert.Equal(2, storage.AllocatedPageCount);

        // 验证两页数据相互隔离独立
        Assert.Equal("edge_page0", storage[page0LastIndex].TargetName);
        Assert.Equal("edge_page1", storage[page1FirstIndex].TargetName);

        // 实际写读每个站点的四个槽位，并检查只发布已触及的页。
        var allSites = new PagedEntries<TestPicEntry>(capacity);
        for (int site = 0; site < 4096; site++)
        {
            for (int lane = 0; lane < 4; lane++)
                allSites[site * 4 + lane] = new TestPicEntry { Site = site, Valid = (byte)(lane + 1) };
            Assert.Equal(site / 64 + 1, allSites.AllocatedPageCount);
        }
        for (int slot = 0; slot < capacity; slot++)
        {
            Assert.Equal(slot / 4, allSites[slot].Site);
            Assert.Equal((byte)(slot % 4 + 1), allSites[slot].Valid);
        }
    }

    [Fact]
    public void NegativeAndTerminalIndicesThrowOutOfRange()
    {
        var storage = new PagedEntries<TestPicEntry>(16384);

        // 负索引检查
        Assert.Throws<IndexOutOfRangeException>(() => { _ = storage[-1]; });
        Assert.Throws<IndexOutOfRangeException>(() => { _ = storage[-256]; });
        Assert.Throws<IndexOutOfRangeException>(() => { _ = storage[int.MinValue]; });

        // 末端有效索引（16383）应正常工作并命中第 63 页
        storage[16383] = new TestPicEntry { Valid = 1, Site = 4095, TargetName = "terminal" };
        Assert.True(storage.IsPageAllocated(63));
        Assert.Equal("terminal", storage[16383].TargetName);

        // 越界索引（>= 16384）检查
        Assert.Throws<IndexOutOfRangeException>(() => { _ = storage[16384]; });
        Assert.Throws<IndexOutOfRangeException>(() => { _ = storage[16385]; });
        Assert.Throws<IndexOutOfRangeException>(() => { _ = storage[int.MaxValue]; });

        // 构造参数非法性检查
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedEntries<TestPicEntry>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedEntries<TestPicEntry>(-1));
    }

    [Fact]
    public async Task ConcurrentAccessToDifferentSlotsInSamePagePublishesSinglePageWithoutLostWrites()
    {
        var storage = new PagedEntries<TestPicEntry>(16384);
        const int targetPage = 7;
        int baseSlot = targetPage * 256;
        Assert.False(storage.IsPageAllocated(targetPage));

        const int concurrency = 64;
        using var barrier = new ManualResetEventSlim(false);
        var tasks = new Task[concurrency];

        for (int i = 0; i < concurrency; i++)
        {
            int slotOffset = i;
            int globalSlot = baseSlot + slotOffset;
            tasks[i] = Task.Run(() =>
            {
                barrier.Wait();
                ref TestPicEntry entry = ref storage[globalSlot];
                entry.Valid = 1;
                entry.Site = slotOffset;
                entry.TargetName = $"marker_{slotOffset}";
            });
        }

        // 同时放行所有线程，竞争触发 EnsurePageAllocated
        barrier.Set();
        await Task.WhenAll(tasks);

        // 只发布一张页；CAS 失败方可能分配临时页，计数不用于断言分配次数。
        Assert.True(storage.IsPageAllocated(targetPage));
        Assert.Equal(1, storage.AllocatedPageCount);

        // 验证：所有 64 个槽位的并发写入均完好保留，无任何一槽因 CAS 丢弃竞争页而发生写丢失
        for (int i = 0; i < concurrency; i++)
        {
            int globalSlot = baseSlot + i;
            TestPicEntry entry = storage[globalSlot];
            Assert.Equal(1, entry.Valid);
            Assert.Equal(i, entry.Site);
            Assert.Equal($"marker_{i}", entry.TargetName);
        }
    }

    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(4095)]
    public void RealCachesKeepResultsAndInvalidateAcrossPageBoundaries(int site)
    {
        var signature = PySignature.Create("f", ["self"], ["pos"], ["0"]);
        PyFunction Function(long value) =>
            new("f", signature, (_, _, _, _) => value, []);
        PyFunction first = Function(1L);
        PyFunction second = Function(2L);
        Assert.Equal(1L, PyPic.CallValue1(site, first, null));
        Assert.Equal(1L, PyPic.CallValue1(site, first, null));
        Assert.Equal(2L, PyPic.CallValue1(site, second, null));

        var cls = (PyClass)PyOps.MakeClass("PagedCache", [], null);
        cls.SetAttr("method", first);
        cls.SetAttr("class_value", 10L);
        var instance = (PyInstance)cls.Invoke([], [], [])!;
        Assert.Equal(1L, PyPic.CallMethod0(site, instance, "method"));
        Assert.Equal(1L, PyPic.CallMethod0(site, instance, "method"));
        cls.SetAttr("method", second);
        Assert.Equal(2L, PyPic.CallMethod0(site, instance, "method"));

        Assert.Equal(10L, PyPic.GetAttr(site, instance, "class_value"));
        Assert.Equal(10L, PyPic.GetAttr(site, instance, "class_value"));
        cls.SetAttr("class_value", 11L);
        Assert.Equal(11L, PyPic.GetAttr(site, instance, "class_value"));
        PyPic.SetAttr(site, instance, "class_value", 20L);
        PyPic.SetAttr(site, instance, "class_value", 21L);
        Assert.Equal(21L, PyObject.GetAttr(instance, "class_value"));
        Assert.Equal(21L, PyPic.GetAttr(site, instance, "class_value"));
        PyObject.SetAttr(instance, "method", 5L);
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod0(site, instance, "method"));
    }
}
