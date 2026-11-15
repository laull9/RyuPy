// 数值pop只观察返回槽；其余槽、已观察盒、非法下标和活迭代器保持原语义。
using System.Collections;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

public class PyNumericPopTests
{
    private static PyList Numeric(params double[] values)
    {
        var list = (PyList)PyNumericListOps.Create();
        foreach (double value in values) PyNumericListOps.AppendF64(list, value);
        return list;
    }
    private static IList Storage(PyList list) => Assert.IsAssignableFrom<IList>(
        typeof(PyList).GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list));
    private static object Reference(IList slots, int index) => slots[index]!.GetType()
        .GetField("_reference", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(slots[index])!;

    [Fact]
    public void MiddlePopKeepsUnobservedSlotsAndOrder()
    {
        var list = Numeric(1.5, -0.0, 3.5);
        IList slots = Storage(list);
        object sentinel = Reference(slots, 0);
        object? value = list.Pop(1);
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(value)));
        Assert.Same(slots, Storage(list));
        Assert.Same(sentinel, Reference(slots, 0));
        Assert.Same(sentinel, Reference(slots, 1));
        Assert.Equal(1.5, PyNumericListOps.ReadF64(list, 0L));
        Assert.Equal(3.5, PyNumericListOps.ReadF64(list, 1L));
    }

    [Fact]
    public void ObservedAndSharedBoxesRetainIdentity()
    {
        var list = Numeric(double.NaN, 2.5);
        object? observed = list.GetItem(0);
        object shared = 9.5;
        list.Append(shared); list.Append(shared);
        Assert.Same(shared, list.Pop());
        Assert.Same(shared, list.GetItem(-1));
        Assert.Same(observed, list.Pop(0));
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void InvalidIndexDoesNotObserveOrMutate()
    {
        var list = Numeric(1.5, 2.5);
        IList slots = Storage(list);
        object sentinel = Reference(slots, 0);
        foreach (long index in new[] { long.MinValue, long.MaxValue, 2L, -3L })
            Assert.Equal("pop index out of range", Assert.Throws<PyIndexError>(() => list.Pop(index)).PyMessage);
        Assert.Equal(2, list.Count);
        Assert.Same(slots, Storage(list));
        Assert.Same(sentinel, Reference(slots, 0));
        Assert.Same(sentinel, Reference(slots, 1));
    }

    [Fact]
    public void EmptyRefillAndLiveIteratorKeepBaselineBehavior()
    {
        var untouched = Numeric(1.5);
        IList originalStorage = Storage(untouched);
        Assert.Equal(1.5, untouched.Pop());
        Assert.Equal("pop from empty list", Assert.Throws<PyIndexError>(() => untouched.Pop()).PyMessage);
        PyNumericListOps.AppendF64(untouched, 9.5);
        Assert.Same(originalStorage, Storage(untouched));
        Assert.Equal(9.5, untouched.Pop());

        var notRead = Numeric(1.5, 2.5, 3.5);
        IList beforeRead = Storage(notRead);
        var deferred = PyIter.Create(notRead);
        Assert.True(deferred.MoveNext());
        Assert.Equal(3.5, notRead.Pop());
        Assert.Same(beforeRead, Storage(notRead));
        Assert.Equal(1.5, deferred.Current());
        Assert.True(deferred.MoveNext());
        Assert.Equal(2.5, deferred.Current());

        var list = Numeric(1.5, 2.5, 3.5);
        var iterator = PyIter.Create(list);
        Assert.True(iterator.MoveNext());
        Assert.Equal(1.5, iterator.Current());
        Assert.Equal(1.5, list.Pop(0));
        Assert.True(iterator.MoveNext());
        Assert.Equal(3.5, iterator.Current());
        Assert.False(iterator.MoveNext());
        list.Pop(); list.Pop();
        Assert.Equal("pop from empty list", Assert.Throws<PyIndexError>(() => list.Pop()).PyMessage);
        PyNumericListOps.AppendF64(list, 8.5);
        Assert.False(iterator.MoveNext());
        Assert.Equal(8.5, list.Pop());
    }

    [Fact]
    public void PublishedObjectAliasIsNotReplaced()
    {
        var list = Numeric(1.5, 2.5);
        var alias = list.Items;
        object? expected = alias[1];
        Assert.Same(expected, list.Pop());
        Assert.Same(alias, list.Items);
        Assert.Single(alias);
    }
}
