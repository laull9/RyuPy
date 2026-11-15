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
}
