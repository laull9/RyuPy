using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyGeneratorSendTests
{
    [Fact]
    public void NonNoneCannotStartButNoneStartsWithoutClosingOnError()
    {
        int calls = 0;
        var generator = new PyGenerator(raw =>
        {
            calls++;
            var frame = (object?[])raw;
            Assert.Same(PyNone.Instance, frame[1]);
            frame[0] = 1L;
            return 42L;
        }, new object?[] { 0L, null }, "send.py", "start");
        var error = Assert.Throws<PyTypeError>(() => generator.Send(12L));
        Assert.Equal("can't send non-None value to a just-started generator", error.PyMessage);
        Assert.Equal(0, calls);
        Assert.Equal(42L, generator.Send(PyNone.Instance));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void SendPreservesIdentityAndNextSendsNone()
    {
        object?[] frame = { 0L, null };
        var generator = new PyGenerator(raw =>
        {
            var slots = (object?[])raw;
            object? received = slots[1];
            // 模拟 MIR 恢复入口：接收值取出后即清空临时发送槽。
            slots[1] = null;
            slots[0] = (long)slots[0]! + 1;
            return received;
        }, frame, "send.py", "echo");
        Assert.Same(PyNone.Instance, generator.Send(null));
        object value = new PyList();
        Assert.Same(value, generator.CallMethod("send", [value]));
        Assert.Null(frame[1]);
        Assert.Same(PyNone.Instance, generator.Next(null, false));
        Assert.True(generator.MoveNext());
        Assert.Same(PyNone.Instance, generator.Current());
    }

    [Fact]
    public void SendReceivesReturnValueOnlyOnceAndClosedAcceptsAnyValue()
    {
        var generator = new PyGenerator(raw =>
        {
            var frame = (object?[])raw;
            if ((long)frame[0]! == 0) { frame[0] = 1L; return null; }
            frame[0] = -1L;
            return frame[1];
        }, new object?[] { 0L, null }, "send.py", "finish");
        Assert.True(generator.MoveNext());
        object value = new PyTuple([1L, 2L]);
        var stop = Assert.Throws<PyStopIteration>(() => generator.Send(value));
        Assert.Same(value, stop.Value);
        Assert.Same(value, Assert.Single(stop.Args));
        var closed = Assert.Throws<PyStopIteration>(() => generator.Send(new object()));
        Assert.Null(closed.Value);
        Assert.Empty(closed.Args);
    }

    [Fact]
    public void RunningCheckPrecedesStartAndEscapingErrorsClose()
    {
        PyGenerator? recursive = null;
        recursive = new PyGenerator(_ => recursive!.Send(1L),
            new object?[] { 0L, null }, "send.py", "recursive");
        var error = Assert.Throws<PyValueError>(() => recursive.Send(null));
        Assert.Equal("generator already executing", error.PyMessage);
        Assert.Throws<PyStopIteration>(() => recursive.Send(2L));
        var failure = new PyGenerator(_ => throw new PyValueError("failure"),
            new object?[] { 0L, null }, "send.py", "failure");
        Assert.Throws<PyValueError>(() => failure.Send(null));
        Assert.Throws<PyStopIteration>(() => failure.Send(3L));
    }

    [Fact]
    public void ArgumentErrorsDoNotStartOrCloseAndStopIterationStillUsesPep479()
    {
        int calls = 0;
        var generator = new PyGenerator(_ =>
        {
            calls++;
            throw new PyStopIteration(5L);
        }, new object?[] { 0L, null }, "send.py", "bad_stop");
        Assert.Throws<PyTypeError>(() => generator.CallMethod("send", []));
        Assert.Throws<PyTypeError>(() => generator.CallMethod("send", [null, null]));
        Assert.Equal(0, calls);
        var error = Assert.Throws<PyRuntimeError>(() => generator.CallMethod("send", [null]));
        Assert.Equal(5L, Assert.IsType<PyStopIteration>(error.Cause).Value);
        Assert.Equal(1, calls);
        Assert.Throws<PyStopIteration>(() => generator.Send(6L));
    }
}
