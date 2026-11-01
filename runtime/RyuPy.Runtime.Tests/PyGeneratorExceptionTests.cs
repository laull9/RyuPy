using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyGeneratorExceptionTests
{
    private static object?[] Frame(int extra = 0)
    {
        var frame = new object?[4 + extra];
        frame[0] = 0L;
        return frame;
    }

    [Fact]
    public void InputConsumesSendAndInjectionAndRetryNeverEscapes()
    {
        object value = new PyList();
        object?[] slots = { 0L, value, null, null };
        Assert.Same(value, PyOps.GeneratorInput(slots));
        Assert.Null(slots[1]);
        var error = new PyValueError("injected");
        slots[1] = value;
        slots[2] = error;
        Assert.Same(error, Assert.Throws<PyValueError>(() => PyOps.GeneratorInput(slots)));
        Assert.Null(slots[1]);
        Assert.Null(slots[2]);
        int calls = 0;
        var generator = new PyGenerator(raw =>
        {
            calls++;
            var frame = (object?[])raw;
            if (calls == 1) { PyOps.GeneratorInput(frame); frame[0] = 1L; return PyOps.GeneratorRetry(); }
            return 42L;
        }, Frame(), "exceptions.py", "retry");
        Assert.Equal(42L, generator.Next(null, false));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void RetryDropsUnwoundCalleeTracebackFrames()
    {
        int calls = 0;
        var generator = new PyGenerator(_ =>
        {
            calls++;
            if (calls == 1)
            {
                PyTraceback.SetLine(12);
                PyTraceback.PushFrame("callee.py", "stale");
                return PyOps.GeneratorRetry();
            }
            throw new PyValueError("after retry");
        }, Frame(), "exceptions.py", "retry_trace");
        var error = Assert.Throws<PyValueError>(() => generator.Next(null, false));
        string text = PyTraceback.Format(error);
        Assert.Contains("retry_trace", text);
        Assert.Contains("line 12", text);
        Assert.DoesNotContain("stale", text);
        var probe = new PyValueError("probe");
        PyTraceback.Attach(probe);
        Assert.Empty(probe.Frames);
    }

    [Fact]
    public void NewCloseAndThrowNeverExecuteTheBody()
    {
        int calls = 0;
        PyGenerator New() => new(_ => { calls++; return null; }, Frame(), "exceptions.py", "new");
        var closed = New();
        Assert.Same(PyNone.Instance, closed.Close());
        Assert.Same(PyNone.Instance, closed.Close());
        Assert.False(closed.MoveNext());
        var thrown = New();
        var error = new PyValueError("before start");
        Assert.Same(error, Assert.Throws<PyValueError>(() => thrown.Throw(error)));
        Assert.False(thrown.MoveNext());
        // 尚未进入函数体时，显式注入 StopIteration 不进行 PEP 479 转换。
        var stop = new PyStopIteration(12L);
        Assert.Same(stop, Assert.Throws<PyStopIteration>(() => New().Throw(stop)));
        Assert.Equal(0, calls);
        Assert.Throws<PyValueError>(() => closed.Throw(PyType.Get("ValueError")));
        Assert.Throws<PyTypeError>(() => closed.Throw(12L));
    }

    [Fact]
    public void SuspendedThrowCanBeCaughtAndCanCompleteWithValue()
    {
        var generator = new PyGenerator(raw =>
        {
            var frame = (object?[])raw;
            try { PyOps.GeneratorInput(frame); }
            catch (PyValueError) { frame[0] = 2L; return "caught"; }
            if ((long)frame[0]! == 0) { frame[0] = 1L; return "ready"; }
            frame[0] = -1L;
            return 7L;
        }, Frame(), "exceptions.py", "catch_throw");
        Assert.Equal("ready", generator.Next(null, false));
        Assert.Equal("caught", generator.Throw(PyType.Get("ValueError")));
        Assert.Equal(7L, Assert.Throws<PyStopIteration>(() => generator.Next(null, false)).Value);
        Assert.Throws<PyTypeError>(() => generator.CallMethod("throw", []));
        Assert.Throws<PyTypeError>(() => generator.CallMethod("throw", [new PyValueError("x"), null]));
        Assert.Throws<PyTypeError>(() => generator.CallMethod("close", [null]));
    }

    [Fact]
    public void CloseInjectsExitAndYieldingDuringCloseKeepsSuspension()
    {
        int cleanups = 0;
        var cleaned = new PyGenerator(raw =>
        {
            var frame = (object?[])raw;
            try { PyOps.GeneratorInput(frame); }
            catch (PyGeneratorExit) { cleanups++; throw; }
            frame[0] = 1L;
            return "ready";
        }, Frame(), "exceptions.py", "cleanup");
        cleaned.MoveNext();
        Assert.Same(PyNone.Instance, cleaned.Close());
        Assert.Equal(1, cleanups);
        Assert.False(cleaned.MoveNext());
        var ignored = new PyGenerator(raw =>
        {
            var frame = (object?[])raw;
            try { PyOps.GeneratorInput(frame); }
            catch (PyGeneratorExit) { frame[0] = 2L; return "illegal yield"; }
            if ((long)frame[0]! == 0) { frame[0] = 1L; return "ready"; }
            frame[0] = -1L;
            return 99L;
        }, Frame(), "exceptions.py", "ignore_close");
        ignored.MoveNext();
        Assert.Equal("generator ignored GeneratorExit", Assert.Throws<PyRuntimeError>(() => ignored.Close()).PyMessage);
        Assert.Equal(99L, Assert.Throws<PyStopIteration>(() => ignored.Next(null, false)).Value);
    }

    [Fact]
    public void ActiveHandlerSurvivesYieldButCallerContextIsOnlyBorrowed()
    {
        var own = new PyValueError("own");
        var firstCaller = new PyTypeError("first caller");
        var secondCaller = new PyTypeError("second caller");
        var generator = new PyGenerator(raw =>
        {
            var frame = (object?[])raw;
            PyOps.GeneratorInput(frame);
            switch ((long)frame[0]!)
            {
                case 0:
                    Assert.Same(firstCaller, PyErrors.Current);
                    frame[4] = PyErrors.EnterHandler(own);
                    frame[0] = 1L;
                    return PyErrors.Reraise();
                case 1:
                    Assert.Same(own, PyErrors.Reraise());
                    PyErrors.LeaveHandler(frame[4]);
                    frame[4] = null;
                    frame[0] = 2L;
                    return PyErrors.Current;
                default:
                    Assert.Null(PyErrors.Current);
                    frame[0] = -1L;
                    return null;
            }
        }, Frame(1), "exceptions.py", "contexts");
        object? prior = PyErrors.EnterHandler(firstCaller);
        try
        {
            Assert.Same(own, generator.Next(null, false));
            Assert.Same(firstCaller, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(prior); }
        prior = PyErrors.EnterHandler(secondCaller);
        try
        {
            Assert.Same(secondCaller, generator.Next(null, false));
            Assert.Same(secondCaller, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(prior); }
        Assert.False(generator.MoveNext());
        Assert.Null(PyErrors.Current);
    }

    [Fact]
    public void NoneHandlerEntrySnapshotsWithoutChangingActiveException()
    {
        Assert.Null(PyErrors.Current);
        Assert.Null(PyErrors.EnterHandler(null));
        var outer = new PyValueError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            object? snapshot = PyErrors.EnterHandler(PyNone.Instance);
            Assert.Same(outer, PyErrors.Current);
            Assert.Same(snapshot, PyErrors.EnterHandler(null));
            PyErrors.EnterHandler(new PyTypeError("inner"));
            PyErrors.LeaveHandler(snapshot);
            Assert.Same(outer, PyErrors.Reraise());
            Assert.Throws<PyTypeError>(() => PyErrors.EnterHandler(42L));
            Assert.Same(outer, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(previous); }
        Assert.Null(PyErrors.Current);
    }

    [Fact]
    public void ReentryAndEscapingFailureCloseWithoutLeakingContext()
    {
        PyGenerator? recursive = null;
        recursive = new PyGenerator(_ => recursive!.Close(), Frame(), "exceptions.py", "reenter");
        Assert.Throws<PyValueError>(() => recursive.Next(null, false));
        Assert.False(recursive.MoveNext());
        var outer = new PyValueError("caller");
        object? prior = PyErrors.EnterHandler(outer);
        try
        {
            var generator = new PyGenerator(_ =>
            {
                PyErrors.EnterHandler(new PyTypeError("own"));
                throw new PyValueError("escape");
            }, Frame(), "exceptions.py", "error");
            Assert.Throws<PyValueError>(() => generator.MoveNext());
            Assert.Same(outer, PyErrors.Current);
            Assert.False(generator.MoveNext());
        }
        finally { PyErrors.LeaveHandler(prior); }
        Assert.Null(PyErrors.Current);
    }
}
