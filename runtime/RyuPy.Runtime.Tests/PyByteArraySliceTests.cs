// bytearray切片写入内部合同：转换轮次、别名快照、导出锁、结构版本与异常上下文。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyByteArraySliceTests
{
    private static PyBytes B(string text) => new(Encoding.Latin1.GetBytes(text));
    private static PyByteArray A(string text) => new(Encoding.Latin1.GetBytes(text));
    private static string Text(PyByteArray value) => Encoding.Latin1.GetString(value.Bytes.ToArray());
    private static long Epoch(PyByteArray value) => (long)typeof(PyByteArray).GetProperty("ResizeEpoch", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    private static PySlice S(object? start = null, object? stop = null, object? step = null) => new(start, stop, step);
    private static void Set(PyByteArray target, PySlice slice, object? source) => PyOps.SetItem(target, slice, source);
    private static PyMemoryView View(object source) => (PyMemoryView)PyMemoryView.Create([source], [], []);
    private static PyInstance Index(Func<object?> callback)
    {
        var cls = new PyClass("Index");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ => callback()));
        return new PyInstance(cls);
    }

    [Theory]
    [InlineData(1, 4, 1, "XY", "aXYef")]
    [InlineData(4, 1, 1, "XY", "abcdXYef")]
    [InlineData(0, 6, 2, "XYZ", "XbYdZf")]
    [InlineData(5, -7, -2, "XYZ", "aZcYeX")]
    [InlineData(5, -7, -2, "", "ace")]
    [InlineData(0, 6, 2, "", "bdf")]
    public void ContiguousAndExtendedWritesPreserveUnselectedBytes(int start, int stop, int step, string source, string expected)
    {
        var target = A("abcdef"); long epoch = Epoch(target);
        Set(target, S((long)start, (long)stop, (long)step), B(source));
        Assert.Equal(expected, Text(target)); Assert.Equal(epoch + (expected.Length == 6 ? 0 : 1), Epoch(target));
    }

    [Fact]
    public void OversizedBoundariesAndStepsClampWithoutOverflow()
    {
        BigInteger huge = BigInteger.One << 200; var target = A("abcdef");
        Set(target, S(-huge, huge, huge), B("X")); Assert.Equal("Xbcdef", Text(target));
        Set(target, S(huge, -huge, -huge), B("Y")); Assert.Equal("XbcdeY", Text(target));
        Set(target, S(false, true, true), B("Z")); Assert.Equal("ZbcdeY", Text(target));
        Set(target, S(-huge, huge), B("Q")); Assert.Equal("Q", Text(target));
        Set(target, S(long.MaxValue, long.MinValue, long.MinValue), B("R")); Assert.Equal("R", Text(target));
    }

    [Fact]
    public void DistinctBytearrayConvertsOnceAndOtherExactSourcesTwice()
    {
        foreach (string kind in new[] { "array", "bytes", "self", "view", "list", "tuple", "range" })
        {
            var target = A("ab"); var events = new List<string>(); PyMemoryView? view = null;
            object source = kind switch
            {
                "array" => A("XY"), "bytes" => B("XY"), "self" => target,
                "view" => view = View(B("XY")), "list" => new PyList([88L, 89L]),
                "tuple" => new PyTuple([88L, 89L]), _ => new PyRange(88, 90, 1),
            };
            object I(string name, long value) => Index(() => { events.Add(name); return value; });
            try { Set(target, S(I("start", 0), I("stop", 2), I("step", 1)), source); }
            finally { view?.Release(); }
            Assert.Equal(kind == "array" ? new[] { "step", "start", "stop" } : new[] { "step", "start", "stop", "step", "start", "stop" }, events);
            Assert.Equal(kind == "self" ? "ab" : "XY", Text(target));
        }
    }

    [Fact]
    public void SequenceConversionOccursBetweenSliceRoundsAndUsesFinalLength()
    {
        var target = A("abc"); var events = new List<string>();
        object I(string name, long value) => Index(() => { events.Add(name); return value; });
        var source = new PyList([Index(() => { events.Add("item"); target.Append(33L); return 88L; })]);
        Set(target, S(I("start", -1), I("stop", 99), I("step", 1)), source);
        Assert.Equal(new[] { "step", "start", "stop", "item", "step", "start", "stop" }, events);
        Assert.Equal("abcX", Text(target)); Assert.Equal(1L, Epoch(target));
    }

    [Fact]
    public void StopCallbacksResizeBeforeEachLengthMeasurement()
    {
        foreach (bool distinct in new[] { false, true })
        {
            var target = A("abc"); int calls = 0;
            object stop = Index(() => { calls++; target.Append(33L); return 999L; });
            Set(target, S(-1L, stop), distinct ? A("X") : B("X"));
            Assert.Equal(distinct ? 1 : 2, calls); Assert.Equal(distinct ? "abcX" : "abc!X", Text(target));
        }
    }

    [Fact]
    public void SelfAndOverlappingViewsSnapshotBeforeWriting()
    {
        var target = A("abcd"); Set(target, S(1L, 3L), target); Assert.Equal("aabcdd", Text(target));
        target = A("abcdef"); var root = View(target);
        var slice = (PyMemoryView)root.GetItem(S(0L, 4L));
        var chars = (PyMemoryView)PyOps.CallValue(slice.GetAttr("cast"), ["c"], [], [])!;
        try
        {
            Set(target, S(2L, 6L), chars); Assert.Equal("ababcd", Text(target)); Assert.Equal(0L, Epoch(target));
            Set(target, S(null, null, -1L), root); Assert.Equal("dcbaba", Text(target));
        }
        finally { chars.Release(); slice.Release(); root.Release(); }
        target.Append(33L); Assert.Equal("dcbaba!", Text(target));
    }

    [Fact]
    public void ExportLockOnlyBlocksRealResizeAndMismatchWins()
    {
        var target = A("abcdef"); var view = View(target);
        try
        {
            Set(target, S(0L, 2L), B("XY")); Set(target, S(99L, 99L), B(""));
            Set(target, S(null, null, 2L), B("123")); Assert.Equal("1Y2d3f", Text(target));
            Assert.Equal("attempt to assign bytes of size 1 to extended slice of size 3", Assert.Throws<PyValueError>(() => Set(target, S(null, null, 2L), B("Z"))).PyMessage);
            Assert.Throws<PyBufferError>(() => Set(target, S(null, null, 2L), B("")));
            Assert.Throws<PyBufferError>(() => Set(target, S(0L, 1L), B("")));
            Assert.Equal(0L, Epoch(target));
        }
        finally { view.Release(); }
        Set(target, S(null, null, -2L), B("")); Assert.Equal("123", Text(target)); Assert.Equal(1L, Epoch(target));
    }

    [Fact]
    public void SourceFailuresKeepCallbacksButNeverPartiallyAssign()
    {
        var target = A("abc"); int calls = 0;
        var source = new PyList([Index(() => { calls++; target.Append(33L); return 88L; }), 256L]);
        Assert.Throws<PyValueError>(() => Set(target, S(), source));
        Assert.Equal(1, calls); Assert.Equal("abc!", Text(target)); Assert.Equal(1L, Epoch(target));
        var marker = new PyRuntimeError("source-marker");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Set(target, S(), new PyTuple([Index(() => throw marker)]))));
        Assert.Equal("abc!", Text(target));
        var released = View(B("X")); released.Release();
        Assert.Throws<PyValueError>(() => Set(target, S(), released)); Assert.Equal("abc!", Text(target));
    }

    [Fact]
    public void InvalidSliceStopsBeforeSourceAndUserFailuresKeepIdentity()
    {
        var target = A("abc"); int calls = 0; var marker = new PyRuntimeError("slice-marker");
        var source = new PyList([Index(() => { calls++; return 88L; })]);
        Assert.Equal("slice step cannot be zero", Assert.Throws<PyValueError>(() => Set(target, S(null, null, 0L), source)).PyMessage);
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Set(target, S(null, null, Index(() => throw marker)), source)));
        Assert.Equal(0, calls); Assert.Equal("abc", Text(target));
    }

    [Fact]
    public void UnsupportedIterableAndDescriptorDomainsAreNotSilentlyConsumed()
    {
        var target = A("abc"); int calls = 0;
        var iterable = new PyClass("Iterable"); iterable.SetAttr("__iter__", PyNativeFunctions.CreateVariadic("iter", _ => { calls++; return new PyList(); }));
        Assert.Throws<PyNotImplementedError>(() => Set(target, S(), new PyInstance(iterable)));
        var descriptor = new PyClass("Descriptor"); descriptor.SetAttr("__get__", PyNativeFunctions.CreateVariadic("get", _ => { calls++; return 0L; }));
        var indexed = new PyClass("Indexed"); indexed.SetAttr("__index__", new PyInstance(descriptor));
        Assert.Throws<PyNotImplementedError>(() => Set(target, S(new PyInstance(indexed)), B("X")));
        Assert.Throws<PyNotImplementedError>(() => Set(target, S(Index(() => true)), B("X")));
        var plain = new PyInstance(new PyClass("Plain"));
        PyObject.SetAttr(plain, "__index__", PyNativeFunctions.CreateVariadic("index", _ => { calls++; return 0L; }));
        Assert.Throws<PyTypeError>(() => Set(target, S(plain), B("X")));
        Assert.Equal(0, calls); Assert.Equal("abc", Text(target));
    }

    [Fact]
    public void SecondRoundFailureRetainsSideEffectsAndSelfSourceWasAlreadySnapshotted()
    {
        var target = A("abc"); int calls = 0; var marker = new PyRuntimeError("second round");
        object index = Index(() => { if (++calls == 2) { target.Append(33L); throw marker; } return 0L; });
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Set(target, S(index), B("X"))));
        Assert.Equal(2, calls); Assert.Equal("abc!", Text(target)); Assert.Equal(1L, Epoch(target));
        target = A("abc"); calls = 0;
        index = Index(() => { if (++calls == 2) target.Bytes[0] = 90; return 0L; });
        Set(target, S(index), target); Assert.Equal("abc", Text(target)); Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("append", "XYZ")]
    [InlineData("remove", "X")]
    [InlineData("replace", "XZ")]
    public void ListSourceReadsMutationsDuringElementConversion(string mode, string expected)
    {
        var target = A("abc"); var source = new PyList();
        source.Items.Add(Index(() =>
        {
            if (mode == "append") source.Items.Add(90L);
            else if (mode == "remove") source.Items.RemoveAt(source.Items.Count - 1);
            else source.Items[1] = 90L;
            return 88L;
        }));
        source.Items.Add(89L); Set(target, S(), source); Assert.Equal(expected, Text(target));
    }

    [Fact]
    public void ViewSnapshotSurvivesSecondRoundReleaseAndResize()
    {
        var target = A("abc"); var view = View(target); int calls = 0;
        var start = Index(() => { if (++calls == 2) { view.Release(); target.Append(33L); } return 0L; });
        try { Set(target, S(start), view); }
        finally { view.Release(); }
        Assert.Equal("abc", Text(target)); Assert.Equal(2, calls); Assert.Equal(2L, Epoch(target));
        target.Append(33L); Assert.Equal("abc!", Text(target));
    }

    [Fact]
    public void DistinctBytearrayReadsSourceAfterAllSingleRoundCallbacks()
    {
        var target = A("abc"); var source = A("XY"); var events = new List<string>();
        var start = Index(() => { events.Add("start"); source.Bytes[0] = 90; return 0L; });
        var stop = Index(() => { events.Add("stop"); source.Append(33L); return 3L; });
        Set(target, S(start, stop), source);
        Assert.Equal(new[] { "start", "stop" }, events); Assert.Equal("ZY!", Text(target));
        Assert.Equal(0L, Epoch(target)); Assert.Equal(1L, Epoch(source));
    }

    [Fact]
    public void WarmLiteralSameLengthWritesAllocateNoPerCallObjects()
    {
        var target = A("abcdef"); var slice = S(1L, 3L); object[] sources = [B("XY"), A("XY")];
        foreach (object source in sources)
        {
            for (int index = 0; index < 1000; index++) Set(target, slice, source);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1000; index++) Set(target, slice, source);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0L, allocated);
        }
        Assert.Equal("aXYdef", Text(target)); Assert.Equal(0L, Epoch(target));
    }

    [Fact]
    public void ActiveRegexIteratorAllowsSameLengthWritesAndUnlocksOnExhaustion()
    {
        var target = A("aa"); var pattern = PyRegex.Compile(B("a"));
        var iterator = Assert.IsAssignableFrom<IPyIterator>(PyOps.CallValue(pattern.GetAttr("finditer"), [target], [], []));
        try
        {
            Set(target, S(0L, 1L), B("b")); Assert.Equal("ba", Text(target));
            Assert.Throws<PyBufferError>(() => Set(target, S(), B("")));
            Assert.True(iterator.MoveNext()); Assert.False(iterator.MoveNext());
        }
        finally { while (iterator.MoveNext()) { } }
        Set(target, S(), B("")); Assert.Equal("", Text(target)); Assert.Equal(1L, Epoch(target));
    }

    [Fact]
    public void AssignmentErrorsCaptureAmbientContextAndCallerFrames()
    {
        PyTraceback.Reset(); var ambient = new PyRuntimeError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        var target = A("abc"); var view = View(target);
        try
        {
            PyTraceback.PushFrame("bytearray_slice.py", "caller"); PyTraceback.SetLine(17);
            PyBaseException[] errors = [
                Assert.Throws<PyValueError>(() => Set(target, S(null, null, 0L), B("X"))),
                Assert.Throws<PyTypeError>(() => Set(target, S("bad"), B("X"))),
                Assert.Throws<PyBufferError>(() => Set(target, S(), B(""))),
                Assert.Throws<PyValueError>(() => Set(target, S(), new PyList([256L])))];
            foreach (var error in errors)
            {
                Assert.Same(ambient, error.Context); Assert.Equal("caller", Assert.Single(error.Frames).Function);
                Assert.Equal(17, error.Frames[0].Line);
            }
        }
        finally { view.Release(); PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }
}
