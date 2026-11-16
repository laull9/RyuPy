// 仅尾部精确double插入保留数值背板及原盒；异构和公开Items继续走通用路径。
using System.Collections;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNumericTailInsertTests
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
    public void FullWidthPositionKeepsOnlyTailInsertionCompact(long index, int position)
    {
        var list = Numeric(1.5, 2.5, 3.5);
        IList slots = Slots(list);
        object fresh = Reference(slots, 0);
        object inserted = 9.5;
        list.Insert(index, inserted);
        if (position == 3) Assert.Same(slots, Slots(list));
        else Assert.Null(Field(list, "_numeric"));
        Assert.Equal(4, list.Count);
        Assert.Same(inserted, list.GetItem(position));
        if (position == 3)
            for (int i = 0; i < 3; i++) Assert.Same(fresh, Reference(slots, i));
        Assert.Equal(new[] { 1.5, 2.5, 3.5 }, Enumerable.Range(0, 4)
            .Where(i => i != position).Select(i => PyNumericListOps.ReadF64(list, (long)i)).ToArray());
    }

    [Fact]
    public void ExistingObservedAndSharedBoxesKeepIdentityAndBits()
    {
        double nan = BitConverter.Int64BitsToDouble(0x7ff8000000000042L);
        var list = Numeric(nan, -0.0);
        object? observed = list.GetItem(0);
        object shared = -0.0;
        IList slots = Slots(list);
        list.Insert(2, shared); list.Insert(long.MaxValue, shared);
        Assert.Same(slots, Slots(list));
        Assert.Same(observed, list.GetItem(0));
        Assert.Same(shared, list.GetItem(2));
        Assert.Same(shared, list.GetItem(3));
        Assert.Equal(0x7ff8000000000042L, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, 0L)));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, 1L)));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, 2L)));
    }

    [Fact]
    public void EmptyRefillAndAliasKeepStorage()
    {
        var list = Numeric();
        PyList alias = list;
        IList slots = Slots(list);
        object first = 1.5, last = 2.5;
        list.Insert(long.MinValue, first);
        alias.Insert(long.MaxValue, last);
        Assert.Same(first, list.GetItem(0));
        Assert.Same(last, list.GetItem(1));
        list.DelItem(0L); list.DelItem(0L);
        alias.Insert(0, first);
        Assert.Same(slots, Slots(list));
        Assert.Same(first, list.GetItem(0));
    }

    [Fact]
    public void NonDoubleValuesKeepOriginalMaterializationPath()
    {
        foreach (object? inserted in new object?[] { 7L, true, null, "text", new object() })
        {
            var list = Numeric(1.5, 2.5);
            object? observed = list.GetItem(1);
            list.Insert(2, inserted);
            Assert.Null(Field(list, "_numeric"));
            Assert.Same(inserted, list.GetItem(2));
            Assert.Same(observed, list.GetItem(1));
        }
    }

    [Fact]
    public void PublishedItemsAndBooleanStorageKeepOriginalPath()
    {
        var list = Numeric(1.5, 2.5);
        var alias = list.Items;
        object? observed = alias[0];
        object inserted = 9.5;
        list.Insert(2, inserted);
        Assert.Same(alias, list.Items);
        Assert.Same(inserted, alias[2]);
        Assert.Same(observed, alias[0]);
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 2L));
        object? flag = flags.GetItem(0);
        flags.Insert(2, inserted);
        Assert.Null(Field(flags, "_booleans"));
        Assert.Same(inserted, flags.GetItem(2));
        Assert.Same(flag, flags.GetItem(0));
    }

    [Fact]
    public void LiveIteratorUsesCurrentIndexAndStaysExhausted()
    {
        var list = Numeric(1.5, 2.5);
        IList slots = Slots(list);
        var iterator = PyIter.Create(list);
        Assert.True(iterator.MoveNext());
        object inserted = 9.5;
        list.Insert(2, inserted);
        Assert.Same(slots, Slots(list));
        Assert.Equal(1.5, iterator.Current());
        Assert.True(iterator.MoveNext());
        Assert.Equal(2.5, iterator.Current());
        Assert.True(iterator.MoveNext());
        Assert.Same(inserted, iterator.Current());
        Assert.False(iterator.MoveNext());
        list.Insert(3, inserted);
        Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void CapacityBoundaryPreservesExistingBoxes()
    {
        var list = Numeric(1.5, 2.5, 3.5, 4.5);
        IList slots = Slots(list);
        object? observed = list.GetItem(1);
        int oldCapacity = (int)slots.GetType().GetProperty("Capacity")!.GetValue(slots)!;
        Assert.Equal(4, oldCapacity);
        object inserted = 9.5;
        list.Insert(4, inserted);
        Assert.Same(slots, Slots(list));
        Assert.True((int)slots.GetType().GetProperty("Capacity")!.GetValue(slots)! > oldCapacity);
        Assert.Same(observed, list.GetItem(1));
        Assert.Same(inserted, list.GetItem(4));
        Assert.Equal(new[] { 1.5, 2.5, 3.5, 4.5, 9.5 }, Enumerable.Range(0, 5)
            .Select(i => PyNumericListOps.ReadF64(list, (long)i)).ToArray());
    }

    [Fact]
    public void TailThenHeadInsertionMaterializesOnceAndPreservesIdentity()
    {
        var list = Numeric(1.5, 2.5);
        IList slots = Slots(list);
        object tail = 8.5, head = 9.5;
        object? observed = list.GetItem(0);
        list.Insert(2, tail);
        Assert.Same(slots, Slots(list));
        list.Insert(0, head);
        Assert.Null(Field(list, "_numeric"));
        var published = list.Items;
        Assert.Same(head, list.GetItem(0));
        Assert.Same(observed, list.GetItem(1));
        Assert.Same(tail, list.GetItem(3));
        list.Insert(4, tail);
        Assert.Same(published, list.Items);
        Assert.Same(tail, list.GetItem(4));
    }

    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("NumericInsertIndex");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ => body()));
        return new PyInstance(cls);
    }
    private static object? Apply(string route, PyList list, object index, object inserted)
    {
        if (route == "direct") return PyOps.CallMethod(list, "insert", [index, inserted], [], []);
        if (route == "root") return PyOps.CallValue(PyObject.GetAttr(PyType.Get("list"), "insert"), [list, index, inserted], [], []);
        object? method = route == "saved" ? PyObject.GetAttr(list, "insert") : PyOps.PrepareMethod(list, "insert");
        return PyOps.CallValue(method, [index, inserted], [], []);
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("root")]
    [InlineData("saved")]
    [InlineData("prepared")]
    public void IndexCallbackRunsOnceBeforeLengthAndPreservesExceptions(string route)
    {
        var list = Numeric(1.5, 2.5);
        IList slots = Slots(list);
        int calls = 0;
        object inserted = 9.5;
        var index = Index(() => { calls++; list.DelItem(0L); return 1L; });
        Assert.Same(PyNone.Instance, Apply(route, list, index, inserted));
        Assert.Equal(1, calls);
        Assert.Same(slots, Slots(list));
        Assert.Same(inserted, list.GetItem(1));
        Assert.Equal(2.5, list.GetItem(0));
        var error = new PyValueError("index exploded");
        index = Index(() => { calls++; throw error; });
        Assert.Same(error, Assert.Throws<PyValueError>(() => Apply(route, list, index, inserted)));
        Assert.Equal(2, calls);
        Assert.Equal(2, list.Count);
        Assert.Same(slots, Slots(list));
    }
}
