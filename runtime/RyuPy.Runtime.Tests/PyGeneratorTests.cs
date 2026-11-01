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

    private static void AssertNoFrames()
    {
        var probe = new PyValueError("probe");
        PyTraceback.Attach(probe);
        Assert.Empty(probe.Frames);
    }
}
