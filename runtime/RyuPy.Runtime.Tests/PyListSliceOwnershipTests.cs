// 产品切片结果独占背板，保留源物化、引用身份、外部构造防别名及边界合同。
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PyListSliceOwnershipTests
{
    private static object? Field(PyList list, string name) =>
        typeof(PyList).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(list);

    [Theory]
    [InlineData(1000, 2040, 3, 1000, 347)]
    [InlineData(2040, 1000, -3, 2040, 347)]
    [InlineData(-9999, 9999, 1, 0, 2050)]
    [InlineData(100, 10, 1, 0, 0)]
    [InlineData(2050, 9999, 1, 0, 0)]
    public void SlicesHaveIndependentSlotsAndRetainElementReferences(
        long start, long stop, long step, int first, int length)
    {
        object?[] elements = Enumerable.Range(0, 2050).Select(_ => (object?)new object()).ToArray();
        var source = new PyList(elements);
        var slice = source.GetSlice(new PySlice(start, stop, step));
        Assert.Equal(length, slice.Count);
        Assert.NotSame(source.Items, slice.Items);
        for (int i = 0; i < length; i++) Assert.Same(elements[first + i * (int)step], slice.GetItem(i));
        if (length > 0)
        {
            slice.SetItem(0, null);
            Assert.Same(elements[first], source.GetItem(first));
        }
        slice.Append(null);
        Assert.Equal(2050, source.Count);
        source.Clear();
        Assert.Equal(length + 1, slice.Count);
    }

    [Fact]
    public void PublicEnumerableConstructorStillCopiesExternalMutableList()
    {
        object marker = new object();
        var external = new List<object?> { marker };
        var list = new PyList(external);
        external.Clear();
        Assert.Same(marker, list.GetItem(0));
        list.Items.Add(marker);
        Assert.Empty(external);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SpecialBackingMaterializesOnlyForNonemptySlice(bool numeric)
    {
        PyList source;
        string backing;
        if (numeric)
        {
            source = Assert.IsType<PyList>(PyNumericListOps.Create());
            PyNumericListOps.AppendF64(source, double.NaN);
            PyNumericListOps.AppendF64(source, 123.5);
            backing = "_numeric";
        }
        else
        {
            source = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 2L));
            backing = "_booleans";
        }
        Assert.NotNull(Field(source, backing));
        var empty = source.GetSlice(new PySlice(0L, 0L, null));
        Assert.Equal(0, empty.Count);
        Assert.NotNull(Field(source, backing));
        Assert.Throws<PyValueError>(() => source.GetSlice(new PySlice(null, null, 0L)));
        Assert.NotNull(Field(source, backing));
        var result = source.GetSlice(new PySlice(null, null, -1L));
        Assert.Null(Field(source, backing));
        Assert.Same(source.GetItem(1), result.GetItem(0));
        Assert.Same(source.GetItem(0), result.GetItem(1));
        Assert.NotSame(source.Items, result.Items);
        result.Clear();
        Assert.Equal(2, source.Count);
    }

    [Fact]
    public void SliceAndSourceIteratorsRemainIndependentAcrossMutations()
    {
        var source = new PyList([1L, 2L, 3L]);
        var slice = source.GetSlice(new PySlice(null, null, null));
        var sourceIterator = PyIter.Create(source);
        var resultIterator = PyIter.Create(slice);
        Assert.True(sourceIterator.MoveNext());
        Assert.True(resultIterator.MoveNext());
        slice.Items.RemoveAt(0);
        source.Append(4L);
        Assert.True(sourceIterator.MoveNext());
        Assert.Equal(2L, sourceIterator.Current());
        Assert.True(resultIterator.MoveNext());
        Assert.Equal(3L, resultIterator.Current());
        Assert.False(resultIterator.MoveNext());
        Assert.True(sourceIterator.MoveNext());
        Assert.True(sourceIterator.MoveNext());
        Assert.Equal(4L, sourceIterator.Current());
    }
    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(long.MinValue + 1)]
    [InlineData(long.MaxValue - 1)]
    [InlineData(long.MaxValue)]
    public void ExtremeStepsKeepTheSingleSelectedReference(long step)
    {
        object first = new object();
        object last = new object();
        var source = new PyList([first, null, last]);
        var result = source.GetSlice(new PySlice(null, null, step));
        Assert.Equal(1, result.Count);
        Assert.Same(step > 0 ? first : last, result.GetItem(0));
        var bounded = source.GetSlice(step > 0
            ? new PySlice(long.MinValue, long.MaxValue, step)
            : new PySlice(long.MaxValue, long.MinValue, step));
        Assert.Equal(1, bounded.Count);
        Assert.Same(result.GetItem(0), bounded.GetItem(0));
        Assert.Equal(0, source.GetSlice(new PySlice(1L, 1L, step)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptySourceRemainsIndependentAndDoesNotMaterialize(bool numeric)
    {
        var source = numeric ? Assert.IsType<PyList>(PyNumericListOps.Create()) : new PyList();
        foreach (long step in new[] { 1L, -1L, long.MinValue, long.MaxValue })
        {
            var result = source.GetSlice(new PySlice(null, null, step));
            Assert.Equal(0, result.Count);
            result.Append(new object());
            Assert.Equal(0, source.Count);
            if (numeric)
            {
                Assert.NotNull(Field(source, "_numeric"));
                Assert.Null(Field(source, "_items"));
            }
            else Assert.NotSame(source.Items, result.Items);
        }
    }

    [Fact]
    public void PreviouslyCachedNumericBoxSurvivesSliceMaterialization()
    {
        var source = Assert.IsType<PyList>(PyNumericListOps.Create());
        PyNumericListOps.AppendF64(source, double.NaN);
        PyNumericListOps.AppendF64(source, 1234.5);
        object? cached = source.GetItem(0);
        Assert.NotNull(Field(source, "_numeric"));
        var reverse = source.GetSlice(new PySlice(null, null, -1L));
        Assert.Null(Field(source, "_numeric"));
        Assert.Same(cached, reverse.GetItem(-1));
        Assert.Same(cached, source.GetItem(0));
        Assert.Same(cached, source.GetSlice(new PySlice(0L, 1L, null)).GetItem(0));
        reverse.SetItem(-1, null);
        Assert.Same(cached, source.GetItem(0));
    }
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void InvalidSliceBoundsDoNotMaterializeSpecialBacking(bool numeric, bool invalidStart)
    {
        PyList source;
        string backing;
        if (numeric)
        {
            source = Assert.IsType<PyList>(PyNumericListOps.Create());
            PyNumericListOps.AppendF64(source, 123.5);
            backing = "_numeric";
        }
        else
        {
            source = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 2L));
            backing = "_booleans";
        }
        object? original = Field(source, backing);
        var spec = invalidStart ? new PySlice("bad", null, null) : new PySlice(null, "bad", null);
        Assert.Throws<PyTypeError>(() => source.GetSlice(spec));
        Assert.Same(original, Field(source, backing));
        Assert.Null(Field(source, "_items"));
        Assert.Equal(numeric ? 1 : 2, source.Count);
    }
}
