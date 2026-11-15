// 数值列表反转保留延迟物化；清空维持基线行为，核缓存盒、位模式及别名。
using System.Collections;
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PyNumericListReorderTests
{
    private static PyList Numeric(params double[] values)
    {
        var list = Assert.IsType<PyList>(PyNumericListOps.Create());
        foreach (double value in values) PyNumericListOps.AppendF64(list, value);
        return list;
    }

    private static IList NumericStorage(PyList list)
    {
        Assert.Null(typeof(PyList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list));
        return Assert.IsAssignableFrom<IList>(typeof(PyList)
            .GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list));
    }

    private static object SlotReference(IList storage, int index)
    {
        object slot = storage[index]!;
        return slot.GetType().GetField("_reference", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(slot)!;
    }

    [Fact]
    public void ReverseMovesUnobservedSlotsWithoutBoxing()
    {
        var list = Numeric(2.5, -0.0, double.NaN);
        IList storage = NumericStorage(list);
        object fresh = SlotReference(storage, 0);
        list.Reverse();
        Assert.Same(storage, NumericStorage(list));
        for (int i = 0; i < 3; i++) Assert.Same(fresh, SlotReference(storage, i));
        Assert.True(double.IsNaN(PyNumericListOps.ReadF64(list, 0L)));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, 1L)));
        Assert.Equal(2.5, PyNumericListOps.ReadF64(list, 2L));
    }

    [Fact]
    public void ReversePreservesObservedAndSharedBoxIdentity()
    {
        var list = Numeric(double.NaN, 2.5);
        object? observed = list.GetItem(0);
        object shared = 9876.5;
        list.Append(shared); list.Append(shared);
        IList storage = NumericStorage(list);
        list.Reverse();
        Assert.Same(storage, NumericStorage(list));
        Assert.Same(shared, list.GetItem(0));
        Assert.Same(shared, list.GetItem(1));
        Assert.Same(observed, list.GetItem(3));
        list.Reverse();
        Assert.Same(observed, list.GetItem(0));
        Assert.Same(shared, list.GetItem(2));
    }

    [Fact]
    public void ClearKeepsBaselineMaterializationAndRefillSemantics()
    {
        var list = Numeric(1.5, 2.5);
        object? observed = list.GetItem(0);
        list.Clear();
        Assert.Equal(0, list.Count);
        Assert.Null(typeof(PyList).GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list));
        var published = list.Items;
        list.Clear(); list.Reverse();
        Assert.Same(published, list.Items);
        PyNumericListOps.AppendF64(list, 7.5);
        Assert.True(PyNumericListOps.CanReadF64(list, 0L));
        Assert.Equal(7.5, PyNumericListOps.ReadF64(list, 0L));
        Assert.Equal(1.5, Assert.IsType<double>(observed));
    }

    [Fact]
    public void LiveIteratorSeesReorderAndRefillButCannotReviveAfterExhaustion()
    {
        var list = Numeric(1.5, 2.5, 3.5);
        var iterator = PyIter.Create(list);
        list.Reverse();
        Assert.True(iterator.MoveNext());
        Assert.Equal(3.5, iterator.Current());
        list.Clear();
        PyNumericListOps.AppendF64(list, 4.5);
        PyNumericListOps.AppendF64(list, 5.5);
        Assert.True(iterator.MoveNext());
        Assert.Equal(5.5, iterator.Current());
        Assert.False(iterator.MoveNext());
        PyNumericListOps.AppendF64(list, 6.5);
        Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void PublishedObjectBackingRetainsMutationAlias()
    {
        var list = Numeric(1.5, 2.5);
        List<object?> published = list.Items;
        object? first = published[0];
        list.Reverse();
        Assert.Same(published, list.Items);
        Assert.Same(first, published[1]);
        list.Clear();
        Assert.Empty(published);
        Assert.Same(published, list.Items);
        PyNumericListOps.AppendF64(list, 3.5);
        Assert.Single(published);
        Assert.Equal(3.5, published[0]);
    }
}
