// 显式原因不改变异常身份；from None 隐藏报告链但保留可读 Context，标志可恢复。
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyExceptionSuppressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormatChainBoundariesAreExactAndDoNotMutateExceptions(bool withFrames)
    {
        var context = new PyValueError("implicit");
        var cause = new PyRuntimeError("explicit");
        var error = new PyTypeError("final") { Context = context };
        if (withFrames)
        {
            context.Frames = [new("context.py", "previous", 3)];
            cause.Frames = [new("cause.py", "reason", 5)];
            error.Frames = [new("current.py", "inner", 9), new("current.py", "outer", 7)];
        }
        string contextText = (withFrames
            ? "Traceback (most recent call last):\n  File \"context.py\", line 3, in previous\n" : "")
            + "ValueError: implicit\n";
        string causeText = (withFrames
            ? "Traceback (most recent call last):\n  File \"cause.py\", line 5, in reason\n" : "")
            + "RuntimeError: explicit\n";
        string currentText = (withFrames
            ? "Traceback (most recent call last):\n  File \"current.py\", line 7, in outer\n  File \"current.py\", line 9, in inner\n" : "")
            + "TypeError: final\n";
        // 固定 CPython separator 合同，不从被测 Format 或常量反造期望。
        string implicitText = contextText
            + "\nDuring handling of the above exception, another exception occurred:\n\n" + currentText;
        string explicitText = causeText
            + "\nThe above exception was the direct cause of the following exception:\n\n" + currentText;
        PyBaseException[] errors = [context, cause, error];
        void Check(string expected)
        {
            var before = errors.Select(e => (Error: e, Frames: e.Frames, Copy: e.Frames.ToArray(),
                e.Cause, e.Context, e.SuppressContextFlag)).ToArray();
            Assert.Equal(expected, PyTraceback.Format(error));
            Assert.Equal(currentText, PyTraceback.FormatSingle(error));
            foreach (var item in before)
            {
                Assert.Same(item.Frames, item.Error.Frames);
                Assert.Equal(item.Copy, item.Error.Frames);
                Assert.Same(item.Cause, item.Error.Cause);
                Assert.Same(item.Context, item.Error.Context);
                Assert.Equal(item.SuppressContextFlag, item.Error.SuppressContextFlag);
            }
        }
        Check(implicitText);
        error.WithCause(cause);
        error.SuppressContextFlag = false; // cause 即使与可显示 context 并存也优先。
        Check(explicitText);
        error.WithCause(null);
        Check(currentText);
        error.SuppressContextFlag = false;
        Check(implicitText);
        error.WithCause(error);
        Check(currentText);
        error.WithCause(null);
        error.SuppressContextFlag = false;
        context.Context = error; // 双对象环只呈现每个实例一次，不多添分隔。
        Check(implicitText);
        error.Context = null;
        Check(currentText);
    }

    [Fact]
    public void ExplicitCauseMutatesOriginalInstanceWithoutLosingSubclassOrAttributes()
    {
        var exception = new PyValueError("same"); var cause = new PyKeyError("cause");
        exception.UserAttributes["marker"] = new object();
        Assert.Same(exception, exception.WithCause(cause));
        Assert.Same(cause, exception.Cause);
        Assert.True(exception.SuppressContextFlag);
        Assert.IsType<PyValueError>(exception.WithCause(null));
        Assert.Null(exception.Cause); Assert.True(exception.SuppressContextFlag);
        Assert.True(exception.UserAttributes.ContainsKey("marker"));
    }

    [Fact]
    public void FromNoneHidesContextOnlyUntilTheFlagIsReset()
    {
        var context = new PyKeyError("hidden context");
        var exception = new PyValueError("visible") { Context = context };
        exception.WithCause(null);
        Assert.Same(context, exception.Context);
        Assert.DoesNotContain("hidden context", PyTraceback.Format(exception));
        PyObject.SetAttr(exception, "__suppress_context__", false);
        Assert.Equal(false, PyObject.GetAttr(exception, "__suppress_context__"));
        Assert.Contains("hidden context", PyTraceback.Format(exception));
        foreach (object? invalid in new object?[] { 1L, 0L, "yes", null, PyNone.Instance })
            Assert.Throws<PyTypeError>(() => PyObject.SetAttr(exception, "__suppress_context__", invalid));
        Assert.False(exception.SuppressContextFlag);
    }

    [Fact]
    public void ExplicitCauseAlwaysWinsAndCyclesDoNotRepeatTheSameInstance()
    {
        var context = new PyValueError("implicit"); var cause = new PyKeyError("explicit");
        var exception = new PyException("Exception", "final", cause: cause) { Context = context };
        Assert.True(exception.SuppressContextFlag);
        exception.SuppressContextFlag = false;
        string report = PyTraceback.Format(exception);
        Assert.Contains("explicit", report); Assert.DoesNotContain("implicit", report);
        exception.WithCause(exception);
        Assert.Equal("Exception: final\n", PyTraceback.Format(exception));
    }

    [Fact]
    public void ContextManagerMissingYieldReportsOnlyTheProtocolFailure()
    {
        var factory = PyNativeFunctions.Create("empty", [], [], 0,
            _ => new PyGenerator(raw => { ((object?[])raw)[0] = -1L; return null; },
                [0L, PyNone.Instance, null, null], "test.py", "empty"));
        // 此合成Python函数的真实限定名已知；不伪造未提供的代码首行。
        PyObject.SetAttr(factory, "__qualname__", "empty");
        object? decorated = PyOps.CallValue(PyContextLib.CreateAttributes()["contextmanager"], [factory], [], []);
        object? manager = PyOps.CallValue(decorated, [], [], []);
        var error = Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallValue(PyObject.GetAttr(manager, "__enter__"), [], [], []));
        Assert.IsType<PyStopIteration>(error.Context);
        Assert.True(error.SuppressContextFlag); Assert.Null(error.Cause);
        Assert.DoesNotContain("StopIteration", PyTraceback.Format(error));
        Assert.Contains("generator didn't yield", PyTraceback.Format(error));
    }
    [Fact]
    public void ExplicitCauseCanAlsoBeTheImplicitCurrentContext()
    {
        var current = new PyKeyError("current");
        var error = new PyTypeError("inner"); error.WithCause(current);
        object? previous = PyErrors.EnterHandler(current);
        try
        {
            Assert.Same(error, PyErrors.PrepareRaise(error));
            Assert.Same(current, error.Cause); Assert.Same(current, error.Context);
            PyErrors.PrepareRaise(current);
            Assert.Null(current.Context); // 同一活动异常重抛不能挂接自身。
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

}
