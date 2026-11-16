// 切片仅观察被选槽；源槽缓存原盒，结果仍为独立object背板，生命周期观察共享身份。
using System.Collections;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNumericSliceTests
{
    private static PyList Numeric(params double[] values)
    {
        var list = (PyList)PyNumericListOps.Create();
        foreach (double value in values) PyNumericListOps.AppendF64(list, value);
        return list;
    }
    private static object? Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
    private static IList Slots(PyList source) => Assert.IsAssignableFrom<IList>(Field(source, "_numeric"));
    private static int Observed(PyList source)
    {
        IList slots = Slots(source);
        int count = 0;
        for (int i = 0; i < slots.Count; i++) if (Field(slots[i]!, "_reference") is double) count++;
        return count;
    }
    [Fact]
    public void ShortSliceOnlyObservesSelectedSlots()
    {
        var source = Numeric(1.5, 2.5, 3.5, 4.5);
        IList slots = Slots(source);
        var result = source.GetSlice(new PySlice(1L, 3L, 1L));
        Assert.Same(slots, Slots(source));
        Assert.Equal(2, Observed(source));
        Assert.Null(Field(result, "_numeric"));
        Assert.Equal(new object?[] { 2.5, 3.5 }, result.Items);
        Assert.Same(source.GetItem(1), result.GetItem(0));
        Assert.Same(source.GetItem(2), result.GetItem(1));
    }
    [Fact]
    public void EmptyAndInvalidSlicesDoNotObserve()
    {
        var source = Numeric(1.5, 2.5);
        Assert.Equal(0, source.GetSlice(new PySlice(1L, 1L, 1L)).Count);
        Assert.Equal(0, source.GetSlice(new PySlice(0L, 2L, -1L)).Count);
        Assert.Equal("slice step cannot be zero", Assert.Throws<PyValueError>(() => source.GetSlice(new PySlice(0L, 1L, 0L))).PyMessage);
        Assert.Throws<PyTypeError>(() => source.GetSlice(new PySlice("invalid", 1L, 1L)));
        Assert.Equal(0, Observed(source));
    }
    [Fact]
    public void NegativeAndExtremeStepsKeepOrdering()
    {
        var source = Numeric(1.5, 2.5, 3.5, 4.5);
        var reverse = source.GetSlice(new PySlice(-1L, 0L, -2L));
        Assert.Equal(new object?[] { 4.5, 2.5 }, reverse.Items);
        Assert.Equal(2, Observed(source));
        var wide = source.GetSlice(new PySlice(null, null, long.MinValue));
        Assert.Single(wide.Items);
        Assert.Same(reverse.GetItem(0), wide.GetItem(0));
        Assert.Equal(2, Observed(source));
    }
    [Fact]
    public void NaNAndNegativeZeroPreserveBitsAndObservedIdentity()
    {
        double nan = BitConverter.Int64BitsToDouble(0x7ff8000000000042L);
        var source = Numeric(nan, -0.0, 3.5);
        object? observed = source.GetItem(0);
        var result = source.GetSlice(new PySlice(0L, 2L, 1L));
        Assert.Same(observed, result.GetItem(0));
        Assert.Equal(0x7ff8000000000042L, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(result.GetItem(0))));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(result.GetItem(1))));
        Assert.Equal(2, Observed(source));
    }
    [Fact]
    public void OverlappingSlicesAndLaterCopyShareOriginalBoxes()
    {
        var source = Numeric(1.5, 2.5, 3.5);
        var a = source.GetSlice(new PySlice(0L, 2L, 1L));
        var b = source.GetSlice(new PySlice(1L, 3L, 1L));
        Assert.Same(a.GetItem(1), b.GetItem(0));
        var copy = source.Copy();
        Assert.Same(a.GetItem(0), copy.GetItem(0));
        Assert.Same(b.GetItem(1), copy.GetItem(2));
        Assert.Same(copy.GetItem(1), source.Items[1]);
    }
    [Fact]
    public void SourceAndResultMutationsAreIndependent()
    {
        var source = Numeric(1.5, 2.5, 3.5);
        PyList sourceAlias = source;
        var result = source.GetSlice(new PySlice(0L, 2L, 1L));
        object? old = result.GetItem(0);
        sourceAlias.SetItem(0L, 9.5);
        Assert.Same(old, result.GetItem(0));
        result.SetItem(1L, 8.5);
        Assert.Equal(2.5, source.GetItem(1));
        result.Append(7.5);
        Assert.Equal(3, source.Count);
    }
    [Fact]
    public void PublishedItemsAndPriorCopyKeepAliases()
    {
        var source = Numeric(1.5, 2.5);
        var published = source.Items;
        var priorCopy = source.Copy();
        var result = source.GetSlice(new PySlice(null, null, null));
        Assert.Same(published, source.Items);
        Assert.Same(priorCopy.GetItem(0), result.GetItem(0));
        Assert.Same(published[1], result.GetItem(1));
        result.SetItem(0L, 8.5);
        Assert.Equal(1.5, published[0]);
    }
    [Fact]
    public void FullSliceAndIterationPreserveIdentity()
    {
        var source = Numeric(1.5, 2.5, 3.5);
        var iterator = PyIter.Create(source);
        var result = source.GetSlice(new PySlice(null, null, null));
        Assert.Null(Field(source, "_numeric"));
        for (int i = 0; i < result.Count; i++)
        {
            Assert.True(iterator.MoveNext());
            Assert.Same(result.GetItem(i), iterator.Current());
        }
        Assert.False(iterator.MoveNext());
        Assert.Same(source.Items[0], result.GetItem(0));
    }
    [Fact]
    public void BoolAndOrdinaryObjectFallbackPreserveReferences()
    {
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 3L));
        object? backing = Field(flags, "_booleans");
        var selected = flags.GetSlice(new PySlice(1L, 2L, 1L));
        Assert.NotNull(backing);
        Assert.Null(Field(flags, "_booleans"));
        Assert.Same(flags.GetItem(1), selected.GetItem(0));
        object shared = new();
        var objects = new PyList(new object?[] { shared, "text", shared });
        var result = objects.GetSlice(new PySlice(null, null, null));
        Assert.Same(shared, result.GetItem(0));
        Assert.Same(shared, result.GetItem(2));
    }
    [Fact]
    public void ExistingUnsupportedIndexCallbacksStayRejectedWithoutInvocation()
    {
        int calls = 0;
        var cls = new PyClass("SliceIndex");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ => { calls++; return 0L; }));
        var callback = new PyInstance(cls);
        var source = Numeric(1.5, 2.5);
        Assert.Throws<PyTypeError>(() => source.GetSlice(new PySlice(callback, 1L, 1L)));
        Assert.Throws<PyValueError>(() => source.GetSlice(new PySlice(callback, 1L, 0L)));
        Assert.Equal(0, calls);
        Assert.Equal(0, Observed(source));
    }
    private static void RecordRetention(PyList source, string label)
    {
        string? output = Environment.GetEnvironmentVariable("SLICE_RETAINED_OUTPUT");
        if (output is null) return;
        object? numeric = Field(source, "_numeric"), items = Field(source, "_items");
        object storage = numeric ?? items!;
        int capacity = (int)storage.GetType().GetProperty("Capacity")!.GetValue(storage)!;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, label + ".json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            label, count = source.Count, representation = numeric is null ? "object" : "numeric",
            capacity, slotWidth = numeric is null ? IntPtr.Size : 16,
            observed = numeric is null ? source.Count : Observed(source)
        }));
    }

    [Fact]
    public void NearFullRetainedSlotsRemainExplicitUntilCopy()
    {
        var source = Numeric(Enumerable.Range(0, 1024).Select(i => i + 0.25).ToArray());
        var selected = source.GetSlice(new PySlice(0L, 1023L, 1L));
        Assert.Equal(1023, Observed(source));
        RecordRetention(source, "near-full-before-copy");
        var copy = source.Copy();
        Assert.Null(Field(source, "_numeric"));
        Assert.Same(selected.GetItem(1022), copy.GetItem(1022));
        RecordRetention(source, "near-full-after-copy");
    }

    [Fact]
    public void CumulativeObservationAndFullReversePreserveOriginalBoxes()
    {
        var source = Numeric(Enumerable.Range(0, 1024).Select(i => i + 0.25).ToArray());
        var pieces = new List<PyList>();
        for (int i = 0; i < 1024; i += 8) pieces.Add(source.GetSlice(new PySlice((long)i, (long)i + 8, 1L)));
        Assert.Equal(1024, Observed(source));
        RecordRetention(source, "cumulative-before-items");
        var alias = source.Items;
        Assert.Null(Field(source, "_numeric"));
        Assert.Same(alias[0], pieces[0].GetItem(0));
        RecordRetention(source, "cumulative-after-items");
        var fresh = Numeric(1.5, 2.5, 3.5);
        var reverse = fresh.GetSlice(new PySlice(null, null, -1L));
        Assert.Null(Field(fresh, "_numeric"));
        Assert.Equal(new object?[] { 3.5, 2.5, 1.5 }, reverse.Items);
        Assert.Same(fresh.GetItem(2), reverse.GetItem(0));
        RecordRetention(fresh, "full-reverse");
    }

}
