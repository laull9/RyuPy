using Xunit;

namespace RyuPy.Runtime.Tests;

// traceback 目前使用进程级帧栈，涉及该栈的测试不与其它集合并行。
[CollectionDefinition("生成器帧栈", DisableParallelization = true)]
public class GeneratorFrameCollection { }

[Collection("生成器帧栈")]
public class PyGeneratorTests
{
    [Fact]
    public void ConstructionIsLazyAndNoneDoesNotMeanExhaustion()
    {
        int calls = 0;
        object?[] frame = { 0L, new object() };
        var generator = new PyGenerator(raw =>
        {
            calls++;
            var slots = (object?[])raw;
            long state = (long)slots[0]!;
            slots[0] = state == 2 ? -1L : state + 1;
            return state == 0 ? PyNone.Instance : state == 1 ? 42L : null;
        }, frame, "test.py", "values");
        Assert.Equal(0, calls);
        Assert.Same(generator, PyIter.Create(generator));
        Assert.True(generator.MoveNext());
        Assert.Same(PyNone.Instance, generator.Current());
        Assert.True(generator.MoveNext());
        Assert.Equal(42L, generator.Current());
        Assert.False(generator.MoveNext());
        Assert.False(generator.MoveNext());
        Assert.Equal(3, calls);
        Assert.All(frame, value => Assert.Null(value));
        Assert.Null(generator.Current());
    }

    [Fact]
    public void FramesRemainIndependentAndCellsRemainShared()
    {
        object cell = PyOps.CellNew();
        PyOps.CellSet(cell, 10L);
        PyResume resume = raw =>
        {
            var frame = (object?[])raw;
            frame[0] = (long)frame[0]! + 1;
            return PyOps.CellGet(frame[1]!, "value");
        };
        object?[] firstFrame = { 0L, cell }, secondFrame = { 0L, cell };
        var first = new PyGenerator(resume, firstFrame, "test.py", "first");
        var second = new PyGenerator(resume, secondFrame, "test.py", "second");
        Assert.True(first.MoveNext());
        Assert.Equal(10L, first.Current());
        PyOps.CellSet(cell, 20L);
        Assert.True(second.MoveNext());
        Assert.True(first.MoveNext());
        Assert.Equal(20L, first.Current());
        Assert.Equal(20L, second.Current());
        Assert.Equal(2L, firstFrame[0]);
        Assert.Equal(1L, secondFrame[0]);
    }

    [Fact]
    public void ExceptionsCloseGeneratorAndPreserveSourceWithoutLeakingFrames()
    {
        var generator = new PyGenerator(_ =>
        {
            PyTraceback.SetLine(17);
            throw new PyValueError("broken");
        }, new object?[] { 0L }, "generator.py", "broken");
        var error = Assert.Throws<PyValueError>(() => generator.MoveNext());
        string text = PyTraceback.Format(error);
        Assert.Contains("generator.py", text);
        Assert.Contains("line 17", text);
        Assert.False(generator.MoveNext());
        AssertNoFrames();
    }

    [Fact]
    public void SuspensionAndCompletionDoNotLeakFrames()
    {
        var generator = new PyGenerator(raw =>
        {
            var frame = (object?[])raw;
            frame[0] = (long)frame[0]! == 0 ? 1L : -1L;
            return null;
        }, new object?[] { 0L }, "generator.py", "pause");
        Assert.True(generator.MoveNext());
        Assert.Null(generator.Current());
        AssertNoFrames();
        Assert.False(generator.MoveNext());
        AssertNoFrames();
    }

    [Fact]
    public void ReentryRaisesValueErrorAndEscapingErrorClosesGenerator()
    {
        PyGenerator? generator = null;
        generator = new PyGenerator(_ => generator!.MoveNext(),
            new object?[] { 0L }, "test.py", "recursive");
        var error = Assert.Throws<PyValueError>(() => generator.MoveNext());
        Assert.Equal("generator already executing", error.PyMessage);
        Assert.False(generator.MoveNext());
        AssertNoFrames();
    }

    [Fact]
    public void EscapingStopIterationBecomesRuntimeErrorWithCause()
    {
        var stop = new PyStopIteration(7L);
        var generator = new PyGenerator(_ => throw stop,
            new object?[] { 0L }, "test.py", "stop");
        var error = Assert.Throws<PyRuntimeError>(() => generator.MoveNext());
        Assert.Equal("generator raised StopIteration", error.PyMessage);
        Assert.Same(stop, error.Cause);
        Assert.Same(stop, error.Context);
        Assert.NotEmpty(stop.Frames);
        Assert.False(generator.MoveNext());
        AssertNoFrames();
    }

    [Fact]
    public void ProtocolMethodsAndUnsupportedOperationsAreExplicit()
    {
        var generator = new PyGenerator(raw =>
        {
            ((object?[])raw)[0] = -1L;
            return null;
        }, new object?[] { 0L }, "test.py", "empty");
        Assert.Equal("generator", PyRepr.TypeName(generator));
        Assert.Same(generator, PyOps.CallMethod(generator, "__iter__", [], [], []));
        Assert.Throws<PyTypeError>(() => generator.CallMethod("__next__", [1L]));
        Assert.Throws<PyStopIteration>(() => generator.CallMethod("__next__", []));
        foreach (string method in new[] { "send", "throw", "close" })
            Assert.Throws<PyNotImplementedError>(() => generator.CallMethod(method, []));
        Assert.Throws<PyAttributeError>(() => generator.CallMethod("missing", []));
    }

    [Fact]
    public void InvalidStateAndClrFailureAlsoCloseGenerator()
    {
        var invalid = new PyGenerator(_ => null, [], "test.py", "invalid");
        Assert.Throws<PyRuntimeError>(() => invalid.MoveNext());
        Assert.False(invalid.MoveNext());
        var clr = new PyGenerator(_ => throw new InvalidOperationException("broken"),
            new object?[] { 0L }, "test.py", "clr");
        Assert.Throws<InvalidOperationException>(() => clr.MoveNext());
        Assert.False(clr.MoveNext());
        AssertNoFrames();
    }

    [Fact]
    public void ReturnValueIsObservedOnlyByTheCompletingNext()
    {
        var value = new PyTuple([1L, 2L]);
        var generator = Returning(value);
        var stop = Assert.Throws<PyStopIteration>(() =>
            PyOps.CallBuiltin("next", [generator], [], []));
        Assert.Same(value, stop.Value);
        Assert.Same(value, Assert.Single(stop.Args));
        Assert.Same(value, PyObject.GetAttr(stop, "value"));
        var args = Assert.IsType<PyTuple>(PyObject.GetAttr(stop, "args"));
        Assert.Same(value, Assert.Single(args.Items));
        AssertEmptyStop(Assert.Throws<PyStopIteration>(() =>
            generator.CallMethod("__next__", [])));
        AssertNoFrames();
    }

    [Fact]
    public void IteratorAndListAndDefaultConsumeCompletionValue()
    {
        var direct = Returning(new object());
        Assert.False(direct.MoveNext());
        AssertEmptyStop(Assert.Throws<PyStopIteration>(() => direct.Next(null, false)));
        var listGenerator = Returning(new object());
        var list = Assert.IsType<PyList>(PyOps.CallBuiltin("list", [listGenerator], [], []));
        Assert.Equal(0, list.Count);
        AssertEmptyStop(Assert.Throws<PyStopIteration>(() => listGenerator.Next(null, false)));
        var defaultGenerator = Returning(new object());
        var fallback = new object();
        Assert.Same(fallback, PyOps.CallBuiltin("next", [defaultGenerator, fallback], [], []));
        AssertEmptyStop(Assert.Throws<PyStopIteration>(() => defaultGenerator.Next(null, false)));
        AssertNoFrames();
    }

    [Fact]
    public void NoneCompletionHasNoArgumentsAndExplicitNextPreservesNonNone()
    {
        foreach (object? value in new object?[] { null, PyNone.Instance })
            AssertEmptyStop(Assert.Throws<PyStopIteration>(() => Returning(value).Next(null, false)));
        var generator = Returning(42L);
        var stop = Assert.Throws<PyStopIteration>(() => generator.CallMethod("__next__", []));
        Assert.Equal(42L, stop.Value);
        Assert.Equal(42L, Assert.Single(stop.Args));
        Assert.Equal("42", stop.PyMessage);
        AssertNoFrames();
    }

    [Fact]
    public void DefaultDoesNotSwallowEscapingStopIterationFromBody()
    {
        var generator = new PyGenerator(_ => throw new PyStopIteration(42L),
            new object?[] { 0L }, "test.py", "bad_stop");
        var error = Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallBuiltin("next", [generator, 99L], [], []));
        Assert.Equal(42L, Assert.IsType<PyStopIteration>(error.Cause).Value);
        AssertEmptyStop(Assert.Throws<PyStopIteration>(() => generator.Next(null, false)));
        AssertNoFrames();
    }

    [Fact]
    public void CompletionDoesNotFormatReturnValueAndFormattingIsNotCached()
    {
        int calls = 0;
        var cls = (PyClass)PyOps.MakeClass("MutableText", [], null);
        cls.SetAttr("__str__", new PyFunction("__str__", PySignature.Create(
            "__str__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => (++calls).ToString(), []));
        object value = cls.Invoke([], [], [])!;
        var generator = Returning(value);
        var stop = Assert.Throws<PyStopIteration>(() => generator.Next(null, false));
        Assert.Equal(0, calls);
        Assert.Same(value, stop.Value);
        Assert.Same(value, Assert.Single(stop.Args));
        Assert.Equal("1", PyRepr.Str(stop));
        Assert.Equal("2", PyRepr.Str(stop));
        Assert.Equal("StopIteration: 3", stop.Render());
        Assert.Contains("StopIteration: 4", PyTraceback.Format(stop));
        Assert.Equal("5", stop.Message);
        Assert.Equal(5, calls);
        Assert.False(generator.MoveNext());
    }

    [Fact]
    public void ThrowingReturnValueStringConversionDoesNotBreakCompletion()
    {
        int calls = 0;
        var cls = (PyClass)PyOps.MakeClass("BrokenText", [], null);
        cls.SetAttr("__str__", new PyFunction("__str__", PySignature.Create(
            "__str__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) =>
            {
                calls++;
                throw new PyValueError("string failure");
            }, []));
        object value = cls.Invoke([], [], [])!;
        var stop = Assert.Throws<PyStopIteration>(() => Returning(value).Next(null, false));
        Assert.Same(value, stop.Value);
        Assert.Throws<PyValueError>(() => PyRepr.Str(stop));
        Assert.Equal(1, calls);
        Assert.Contains("StopIteration: <exception str() failed>", PyTraceback.Format(stop));
        Assert.Equal(2, calls);
    }

    private static PyGenerator Returning(object? value) => new(raw =>
    {
        ((object?[])raw)[0] = -1L;
        return value;
    }, new object?[] { 0L }, "test.py", "return_value");

    private static void AssertEmptyStop(PyStopIteration stop)
    {
        Assert.Null(stop.Value);
        Assert.Empty(stop.Args);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(stop, "value"));
    }

    private static void AssertNoFrames()
    {
        var probe = new PyValueError("probe");
        PyTraceback.Attach(probe);
        Assert.Empty(probe.Frames);
    }
}
