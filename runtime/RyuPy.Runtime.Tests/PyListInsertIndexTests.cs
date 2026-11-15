// insert归一化必须保留完整64位下标；边界、背板与元素身份均不得受截断影响。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PyListInsertIndexTests
{
    [Theory]
    [InlineData(long.MinValue, 0)]
    [InlineData(-4294967296L, 0)]
    [InlineData(-2147483649L, 0)]
    [InlineData(-2147483648L, 0)]
    [InlineData(-4L, 0)]
    [InlineData(-3L, 0)]
    [InlineData(-1L, 2)]
    [InlineData(0L, 0)]
    [InlineData(2L, 2)]
    [InlineData(3L, 3)]
    [InlineData(long.MaxValue, 3)]
    public void FullWidthIndexClampsBeforeNarrowing(long index, int position)
    {
        var list = new PyList(new List<object?> { 10L, 20L, 30L });
        object inserted = new();
        list.Insert(index, inserted);
        Assert.Equal(4, list.Count);
        Assert.Same(inserted, list.GetItem(position));
        Assert.Equal(new object?[] { 10L, 20L, 30L },
            list.Items.Where(value => !ReferenceEquals(value, inserted)).ToArray());
    }

    [Fact]
    public void EmptyListAcceptsBothExtremesAndPreservesPublishedAlias()
    {
        var list = new PyList();
        var published = list.Items;
        object first = new(), last = new();
        list.Insert(long.MinValue, first);
        list.Insert(long.MaxValue, last);
        Assert.Same(published, list.Items);
        Assert.Same(first, published[0]);
        Assert.Same(last, published[1]);
    }

    [Fact]
    public void NumericBackingKeepsObservedBoxThroughExtremeInsert()
    {
        var list = (PyList)PyNumericListOps.Create();
        PyNumericListOps.AppendF64(list, double.NaN);
        object? observed = list.GetItem(0);
        object inserted = new();
        list.Insert(-4294967296L, inserted);
        Assert.Same(inserted, list.GetItem(0));
        Assert.Same(observed, list.GetItem(1));
    }
}
