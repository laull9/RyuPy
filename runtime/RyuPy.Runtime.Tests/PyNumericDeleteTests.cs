// 单索引删除不观察被删槽，保留存活槽身份与公开背板；切片保持独立旧路径。
using System.Collections;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

public class PyNumericDeleteTests
{
    private static PyList Numeric(params double[] values)
    {
        var list = (PyList)PyNumericListOps.Create();
        foreach (double value in values) PyNumericListOps.AppendF64(list, value);
        return list;
    }
    private static object? Field(PyList list, string name) => typeof(PyList)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list);
    private static IList Slots(PyList list) => Assert.IsAssignableFrom<IList>(Field(list, "_numeric"));
    private static object Reference(IList slots, int index) => slots[index]!.GetType()
        .GetField("_reference", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(slots[index])!;

    [Fact]
    public void DeleteMovesFreshSlotsWithoutObservationAndPreservesBits()
    {
        double nan = BitConverter.Int64BitsToDouble(0x7ff8000000000042L);
        var list = Numeric(nan, 2.5, -0.0);
        IList slots = Slots(list);
        object fresh = Reference(slots, 0);
        list.DelItem(1L);
        Assert.Same(slots, Slots(list));
        Assert.Equal(2, list.Count);
        Assert.Same(fresh, Reference(slots, 0));
        Assert.Same(fresh, Reference(slots, 1));
        Assert.Equal(0x7ff8000000000042L, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, 0L)));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, 1L)));
    }

    [Fact]
    public void ObservedAndSharedBoxesSurviveDeletionByIdentity()
    {
        var list = Numeric(1.5, double.NaN);
        object? observed = list.GetItem(1);
        object shared = 9.5;
        list.Append(shared); list.Append(shared);
        PyList alias = list;
        list.DelItem(0L);
        Assert.Same(observed, alias.GetItem(0));
        Assert.Same(shared, alias.GetItem(1));
        alias.DelItem(-1L);
        Assert.Same(shared, list.GetItem(-1));
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void InvalidAndEmptyIndicesDoNotObserveOrMutate()
    {
        var list = Numeric(1.5, 2.5);
        IList slots = Slots(list);
        object fresh = Reference(slots, 0);
        foreach (long index in new[] { long.MinValue, long.MaxValue, 2L, -3L })
            Assert.Equal("list index out of range", Assert.Throws<PyIndexError>(() => list.DelItem(index)).PyMessage);
        Assert.Equal(2, list.Count);
        Assert.Same(slots, Slots(list));
        Assert.Same(fresh, Reference(slots, 0));
        list.DelItem(-1L); list.DelItem(0L);
        Assert.Throws<PyIndexError>(() => list.DelItem(-1L));
        PyNumericListOps.AppendF64(list, 8.5);
        Assert.Same(slots, Slots(list));
        Assert.Equal(8.5, list.GetItem(0));
    }

    [Fact]
    public void LiveIteratorKeepsIndexAndExhaustionBehavior()
    {
        var list = Numeric(1.5, 2.5, 3.5);
        IList slots = Slots(list);
        var iterator = PyIter.Create(list);
        Assert.True(iterator.MoveNext());
        list.DelItem(1L);
        Assert.Same(slots, Slots(list));
        Assert.Equal(1.5, iterator.Current()); // 当前普通迭代读取可能物化，仍保留语义。
        Assert.True(iterator.MoveNext());
        Assert.Equal(3.5, iterator.Current());
        Assert.False(iterator.MoveNext());
        list.Append(4.5);
        Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void PublishedObjectStorageRemainsAliased()
    {
        var list = Numeric(1.5, 2.5, 3.5);
        var alias = list.Items;
        object? survivor = alias[2];
        list.DelItem(1L);
        Assert.Same(alias, list.Items);
        Assert.Equal(2, alias.Count);
        Assert.Same(survivor, alias[1]);
        alias.Add("external");
        Assert.Equal("external", list.GetItem(-1));
    }

    [Fact]
    public void BooleanStorageRemainsCompactUntilPublished()
    {
        var list = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 3L));
        object storage = Field(list, "_booleans")!;
        object? singleton = list.GetItem(0);
        list.DelItem(-2L);
        Assert.Same(storage, Field(list, "_booleans"));
        Assert.Null(Field(list, "_items"));
        Assert.Equal(2, list.Count);
        Assert.Same(singleton, list.GetItem(1));
        var alias = list.Items;
        list.DelItem(0L);
        Assert.Same(alias, list.Items);
        Assert.Single(alias);
        Assert.Same(singleton, alias[0]);
    }
}
