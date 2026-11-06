// 显式原因不改变异常身份；from None 隐藏报告链但保留可读 Context，标志可恢复。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyExceptionSuppressionTests
{
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
