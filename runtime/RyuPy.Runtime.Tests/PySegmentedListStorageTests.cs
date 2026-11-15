// 分段候选的语义合同；仅测试程序集编入原型，产品背板保持不变。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PySegmentedListStorageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(3079)]
    public void SelfExtendFreezesLengthAndPreservesReferences(int count)
    {
        var list = new PySegmentedListStorage();
        var values = Enumerable.Range(0, count).Select(_ => new object()).ToArray();
        list.Extend(values);
        list.ExtendExact(list);
        Assert.Equal(count * 2, list.Count);
        for (int i = 0; i < count; i++)
        {
            Assert.Same(values[i], list[i]);
            Assert.Same(values[i], list[i + count]);
        }
        Assert.True(list.IsSegmented);
    }

    [Fact]
    public void ExportIsPermanentAndExternalMutationIsVisible()
    {
        var list = new PySegmentedListStorage();
        object shared = new object();
        list.Append(shared);
        var exported = list.Materialize();
        exported.Add(shared);
        list.ExtendExact(list);
        Assert.Equal(4, list.Count);
        Assert.All(list, value => Assert.Same(shared, value));
        exported.Clear();
        list.Append(shared);
        Assert.Same(exported, list.Materialize());
        Assert.Single(exported);
        Assert.False(list.IsSegmented);
    }

    [Fact]
    public void SliceHasIndependentSlotsButSharedElementIdentity()
    {
        var list = new PySegmentedListStorage();
        var shared = new object();
        for (int i = 0; i < 2050; i++) list.Append(shared);
        var slice = list.Slice(new PySlice(null, null, -1L));
        Assert.Equal(2050, slice.Count);
        Assert.All(slice, value => Assert.Same(shared, value));
        slice.SetItem(-1, null);
        Assert.Same(shared, list.GetItem(0));
        Assert.True(list.IsSegmented);
        Assert.Throws<PyIndexError>(() => list.GetItem(long.MinValue));
        Assert.Throws<PyIndexError>(() => list.SetItem(long.MaxValue, null));
    }

    [Fact]
    public void LiveIteratorSeesAppendAndOverwriteButExhaustionSticks()
    {
        var list = new PySegmentedListStorage();
        list.Append("a");
        using var iterator = list.GetEnumerator();
        Assert.True(iterator.MoveNext());
        list.Append("b");
        list.SetItem(-1, "c");
        Assert.True(iterator.MoveNext());
        Assert.Equal("c", iterator.Current);
        list.Clear();
        Assert.False(iterator.MoveNext());
        list.Append("d");
        Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void PopDropsSegmentsAndPreservesOrderAcrossRefill()
    {
        var list = new PySegmentedListStorage();
        var expected = new List<object?>();
        for (int i = 0; i < 4097; i++) { object box = i; list.Append(box); expected.Add(box); }
        for (int i = 4096; i >= 1023; i--) Assert.Same(expected[i], list.Pop());
        object marker = new object();
        list.Append(marker);
        Assert.Same(marker, list.Pop());
        Assert.True(list.IsSegmented);
        Assert.Same(expected[0], list.Pop(0));
        Assert.False(list.IsSegmented);
        Assert.Equal(expected.Skip(1).Take(1022), list);
    }

    [Fact]
    public void FailedConsumptionCommitsOnlySuccessfulPrefix()
    {
        var list = new PySegmentedListStorage();
        object marker = new object();
        IEnumerable<object?> Failing()
        {
            yield return marker;
            throw new InvalidOperationException("sentinel");
        }
        var error = Assert.Throws<InvalidOperationException>(() => list.Extend(Failing()));
        Assert.Equal("sentinel", error.Message);
        Assert.Same(marker, Assert.Single(list));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CrossStorageExtendRetainsSourceAndReferences(bool exportTarget, bool exportSource)
    {
        var source = new PySegmentedListStorage();
        var target = new PySegmentedListStorage();
        for (int i = 0; i < 2061; i++) source.Append(new object());
        for (int i = 0; i < 37; i++) target.Append(null);
        if (exportTarget) target.Materialize();
        if (exportSource) source.Materialize();
        target.ExtendExact(source);
        Assert.Equal(2098, target.Count);
        for (int i = 0; i < source.Count; i++) Assert.Same(source[i], target[i + 37]);
    }
    [Theory]
    [InlineData(1000, 2070, 3, 1000, 357)]
    [InlineData(2070, 1000, -3, 2070, 357)]
    [InlineData(-1, -2051, -1, 3078, 2050)]
    [InlineData(-9999, 9999, 2, 0, 1540)]
    [InlineData(9999, -9999, -2, 3078, 1540)]
    [InlineData(100, 10, 1, 0, 0)]
    [InlineData(-9999, -9998, 1, 0, 0)]
    [InlineData(9999, 10000, 1, 0, 0)]
    public void UniqueSliceElementsExposeCrossSegmentOrdering(
        long start, long stop, long step, int expectedStart, int expectedCount)
    {
        var list = new PySegmentedListStorage();
        var values = Enumerable.Range(0, 3079).Select(_ => new object()).ToArray();
        list.Extend(values);
        var slice = list.Slice(new PySlice(start, stop, step));
        Assert.Equal(expectedCount, slice.Count);
        for (int i = 0; i < expectedCount; i++)
            Assert.Same(values[expectedStart + i * (int)step], slice[i]);
        Assert.True(list.IsSegmented);
        Assert.Throws<PyValueError>(() => list.Slice(new PySlice(null, null, 0L)));
    }

    [Fact]
    public void LiveIteratorTracksDeletionAcrossMaterialization()
    {
        var list = new PySegmentedListStorage();
        var values = Enumerable.Range(0, 1030).Select(_ => new object()).ToArray();
        list.Extend(values);
        using var iterator = list.GetEnumerator();
        for (int i = 0; i < 1023; i++)
        {
            Assert.True(iterator.MoveNext());
            Assert.Same(values[i], iterator.Current);
        }
        var exported = list.Materialize();
        // 删除已消费项使后续槽左移；游标不重置，所以跳过原1023项。
        exported.RemoveAt(0);
        Assert.True(iterator.MoveNext());
        Assert.Same(values[1024], iterator.Current);
        object marker = new object();
        exported[1024] = marker;
        Assert.True(iterator.MoveNext());
        Assert.Same(marker, iterator.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearThenReuseRetainsModeAndLiveCursorPosition(bool materialize)
    {
        var list = new PySegmentedListStorage();
        for (int i = 0; i < 2050; i++) list.Append(new object());
        var exported = materialize ? list.Materialize() : null;
        using var iterator = list.GetEnumerator();
        Assert.True(iterator.MoveNext());
        list.Clear();
        Assert.Equal(0, list.Count);
        Assert.Throws<PyIndexError>(() => list.Pop());
        var values = Enumerable.Range(0, 2051).Select(_ => new object()).ToArray();
        list.Extend(values);
        // 尚未再次恢复过的游标仍在位置1，不会因clear自动封死。
        Assert.True(iterator.MoveNext());
        Assert.Same(values[1], iterator.Current);
        for (int i = 0; i < values.Length; i++) Assert.Same(values[i], list[i]);
        Assert.Equal(!materialize, list.IsSegmented);
        if (materialize) Assert.Same(exported, list.Materialize());
    }
    [Theory]
    [InlineData(false, 0, 0)]
    [InlineData(false, 3079, 0)]
    [InlineData(false, 1000, 2050)]
    [InlineData(false, 1024, 1025)]
    [InlineData(true, 1000, 2050)]
    [InlineData(true, 1024, 1025)]
    public void ContiguousSlicesCopyUniqueReferencesAcrossBothBoundaries(bool exported, int start, int count)
    {
        var list = new PySegmentedListStorage();
        var values = Enumerable.Range(0, 3079).Select(_ => new object()).ToArray();
        list.Extend(values);
        if (exported) list.Materialize();
        var slice = list.SliceRange(start, count);
        var protocolSlice = list.Slice(new PySlice((long)start, (long)start + count, null));
        Assert.Equal(count, slice.Count);
        Assert.Equal(count, protocolSlice.Count);
        for (int i = 0; i < count; i++)
        {
            Assert.Same(values[start + i], slice[i]);
            Assert.Same(values[start + i], protocolSlice[i]);
        }
        object marker = new object();
        slice.Append(marker);
        Assert.Same(marker, slice.GetItem(-1));
        if (count > 0)
        {
            slice.SetItem(0, marker);
            Assert.Same(values[start], list[start]);
        }
        Assert.Equal(!exported, list.IsSegmented);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SliceRange(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SliceRange(0, -1));
        Assert.Throws<ArgumentException>(() => list.SliceRange(3079, 1));
        Assert.Throws<ArgumentException>(() => list.SliceRange(int.MaxValue, int.MaxValue));
    }
}
