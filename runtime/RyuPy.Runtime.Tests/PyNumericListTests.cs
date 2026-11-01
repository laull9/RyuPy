using System.Reflection;
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>受控数值背板的身份/退化/纯守卫；反射仅用于测试确认背板，生产不反射。</summary>
public class PyNumericListTests
{
    private static PyList Numeric(params double[] values)
    {
        var list = Assert.IsType<PyList>(PyNumericListOps.Create());
        foreach (double value in values) Assert.Same(PyNone.Instance, PyNumericListOps.AppendF64(list, value));
        return list;
    }
    private static object Storage(PyList list)
    {
        object? items = typeof(PyList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list);
        object? numeric = typeof(PyList).GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list);
        Assert.True((items is null) != (numeric is null));
        return items ?? numeric!;
    }
    private static void NumericMode(PyList list) => Assert.Contains("PyNumericSlot", Storage(list).GetType().ToString());
    private static void ObjectMode(PyList list) => Assert.IsType<List<object?>>(Storage(list));

    [Fact]
    public void SingleSlotMaterializationCachesIdentityAndDoesNotRetireTheList()
    {
        var list = Numeric(1234.5, double.NaN, -0.0);
        Assert.Equal(3, list.Count);
        object? first = list.GetItem(0);
        Assert.Same(first, list.GetItem(0));
        NumericMode(list);
        Assert.True(PyNumericListOps.CanReadF64(list, 0L));
        Assert.Equal(1234.5, PyNumericListOps.ReadF64(list, 0L));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyNumericListOps.ReadF64(list, -1L)));
        Assert.True(double.IsNaN(PyNumericListOps.ReadF64(list, 1L)));
        Assert.Same(first, list.GetItem(0));
    }

    [Fact]
    public void ExistingBoxesAndNaNCopiesPreserveSharedIdentity()
    {
        var list = Numeric(double.NaN);
        object shared = 9876.5;
        list.Append(shared); list.Append(shared);
        Assert.Same(shared, list.GetItem(1)); Assert.Same(shared, list.GetItem(2));
        NumericMode(list);
        var copy = list.Copy();
        ObjectMode(list); ObjectMode(copy);
        Assert.Same(list.GetItem(0), copy.GetItem(0));
        Assert.True(PyHash.Equal(list, copy));
        var fresh = Numeric(double.NaN, double.NaN);
        Assert.NotSame(fresh.GetItem(0), fresh.GetItem(1));
    }

    [Fact]
    public void ListMembershipAndIndexRecognizeTheSameNanObject()
    {
        var numeric = Numeric(double.NaN);
        object? shared = numeric.GetItem(0);
        var ordinary = new PyList([shared]);
        foreach (PyList list in new[] { numeric, ordinary })
        {
            Assert.True(list.Contains(shared));
            Assert.Equal(0L, list.IndexOf(shared));
            object distinct = double.NaN;
            Assert.False(list.Contains(distinct));
            Assert.Equal(-1L, list.IndexOf(distinct));
        }
        Assert.False(PyHash.Equal(shared, shared));
    }

    [Fact]
    public void SliceRepeatAndIterationMaterializeOriginalBeforeSharing()
    {
        var list = Numeric(double.NaN, 3.5);
        var slice = list.GetSlice(new PySlice(PyNone.Instance, PyNone.Instance, PyNone.Instance));
        Assert.Same(list.GetItem(0), slice.GetItem(0)); ObjectMode(list);
        var repeatSource = Numeric(double.NaN);
        var repeated = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, repeatSource, 2L));
        Assert.Same(repeatSource.GetItem(0), repeated.GetItem(0));
        Assert.Same(repeated.GetItem(0), repeated.GetItem(1)); ObjectMode(repeatSource);
        var iterSource = Numeric(123.5);
        var iterator = PyIter.Create(iterSource);
        Assert.True(iterator.MoveNext());
        Assert.Same(iterSource.GetItem(0), iterator.Current()); ObjectMode(iterSource);
    }

    [Fact]
    public void MutableItemsAndGenericWritesPermanentlyRetireNumericMode()
    {
        var list = Numeric(1.25, 2.5);
        object? saved = list.GetItem(0);
        List<object?> items = list.Items;
        items[1] = "changed";
        Assert.Same(items, list.Items); Assert.Same(saved, list.GetItem(0));
        Assert.False(PyNumericListOps.CanReadF64(list, 1L));
        PyNumericListOps.AppendF64(list, 3.75);
        ObjectMode(list); Assert.Equal(3.75, list.GetItem(2));
        var mixed = Numeric(4.5); mixed.Append("tail"); ObjectMode(mixed);
        Assert.Equal("tail", mixed.GetItem(1));
        var assigned = Numeric(5.5); assigned.SetItem(0L, 42L); ObjectMode(assigned);
        Assert.False(PyNumericListOps.CanReadF64(assigned, 0L));
    }

    [Fact]
    public void GuardsAreExactPureAndReadMisuseRaisesPythonExceptions()
    {
        int calls = 0;
        var cls = new PyClass("IndexOrContainer");
        cls.SetAttr("__index__", PyRuntimeDataclassTests.Fn("__index__", ["self"], _ => { calls++; return 0L; }));
        cls.SetAttr("__getitem__", PyRuntimeDataclassTests.Fn("__getitem__", ["self", "key"], _ => { calls++; return 1.0; }));
        var custom = new PyInstance(cls); var row = Numeric(1.0); var outer = new PyList([row]);
        Assert.Same(row, PyNumericListOps.PeekList(outer, 0L));
        Assert.Null(PyNumericListOps.PeekList(row, 0L));
        foreach (object index in new object[] { true, 0, custom, long.MaxValue, long.MinValue })
        {
            Assert.False(PyNumericListOps.CanReadF64(row, index));
            Assert.Null(PyNumericListOps.PeekList(outer, index));
        }
        Assert.False(PyNumericListOps.CanReadF64(custom, 0L));
        Assert.Null(PyNumericListOps.PeekList(custom, 0L));
        Assert.Equal(0, calls); NumericMode(row);
        Assert.Throws<PyIndexError>(() => PyNumericListOps.ReadF64(row, 8L));
        Assert.Throws<PyTypeError>(() => PyNumericListOps.ReadF64(row, true));
        Assert.Throws<PyTypeError>(() => PyNumericListOps.ReadF64(new PyList([1L]), 0L));
        Assert.Throws<PyTypeError>(() => PyNumericListOps.AppendF64(custom, 1.0));
    }

    [Fact]
    public void GenericIndexProtocolRunsOnceAndChecksCurrentLength()
    {
        foreach (PyList list in new[] { new PyList([1.0, 2.0]), Numeric(1.0, 2.0) })
        {
            int calls = 0;
            object result = 0L;
            Action callback = () => { };
            var cls = new PyClass("ListIndex");
            cls.SetAttr("__index__", PyRuntimeDataclassTests.Fn("__index__", ["self"], _ =>
            {
                calls++;
                callback();
                return result;
            }));
            var key = new PyInstance(cls);
            Assert.Equal(1.0, list.GetItem(key)); Assert.Equal(1, calls);
            result = new BigInteger(-1);
            Assert.Equal(2.0, list.GetItem(key)); Assert.Equal(2, calls);
            Assert.Equal(2.0, list.GetItem((object)true));
            Assert.Equal(1.0, list.GetItem((object)BigInteger.Zero));
            result = "bad";
            Assert.Throws<PyTypeError>(() => list.GetItem(key)); Assert.Equal(3, calls);
            result = true;
            Assert.Throws<PyNotImplementedError>(() => list.GetItem(key)); Assert.Equal(4, calls);
            result = BigInteger.One << 80;
            Assert.Throws<PyIndexError>(() => list.GetItem(key)); Assert.Equal(5, calls);
            Assert.Throws<PyIndexError>(() => list.GetItem((object)(-(BigInteger.One << 80))));
            Assert.Throws<PyTypeError>(() => list.GetItem((object)1.5));
            result = -1L;
            callback = () => list.Append(3.0);
            Assert.Equal(3.0, list.GetItem(key)); Assert.Equal(6, calls);
            // 回调退化并改变长度后，读写删除都以新背板、新长度执行。
            callback = () => { list.Items.Clear(); list.Append(4.0); };
            list.SetItem(key, 5.0); Assert.Equal(7, calls);
            ObjectMode(list); Assert.Equal(5.0, list.GetItem(0));
            list.DelItem(key); Assert.Equal(8, calls); Assert.Equal(0, list.Count);
            callback = () => list.Items.Clear();
            Assert.Throws<PyIndexError>(() => list.GetItem(key)); Assert.Equal(9, calls);
        }
    }

    [Fact]
    public void CheckedReadsFollowSuccessfulGuardsForBothBackings()
    {
        foreach (PyList list in new[] { new PyList([1.25, double.NaN, -0.0]), Numeric(1.25, double.NaN, -0.0) })
        {
            foreach (long position in new long[] { 0, 1, 2, -1, -2, -3 })
            {
                object index = position;
                Assert.True(PyNumericListOps.CanReadF64(list, index));
                double actual = PyNumericListOps.ReadCheckedF64(list, index);
                double expected = PyNumericListOps.ReadF64(list, index);
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
            }
        }
    }

    [Fact]
    public void DefaultInternalSlotIsInvalidRatherThanNoneOrZero()
    {
        var list = Numeric();
        Type slotType = Storage(list).GetType().GetGenericArguments()[0];
        object empty = Activator.CreateInstance(slotType)!;
        var number = slotType.GetProperty("Number", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var materialize = slotType.GetMethod("Materialize", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.IsType<PyRuntimeError>(Assert.Throws<TargetInvocationException>(() => number.GetValue(empty)).InnerException);
        Assert.IsType<PyRuntimeError>(Assert.Throws<TargetInvocationException>(() => materialize.Invoke(empty, null)).InnerException);
    }

    [Fact]
    public void OrdinaryListsKeepObjectSlotsAndGuardedReadsAllocateNothingPerIteration()
    {
        var ordinary = new PyList([1.5]); ObjectMode(ordinary);
        Assert.True(PyNumericListOps.CanReadF64(ordinary, 0L));
        var numeric = Numeric(7.25); object index = 0L;
        for (int i = 0; i < 10000; i++) PyNumericListOps.ReadF64(numeric, index);
        long before = GC.GetAllocatedBytesForCurrentThread(); double sum = 0;
        for (int i = 0; i < 100000; i++)
            if (PyNumericListOps.CanReadF64(numeric, index)) sum += PyNumericListOps.ReadF64(numeric, index);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(725000.0, sum); Assert.Equal(0, allocated); NumericMode(numeric);
    }
}
