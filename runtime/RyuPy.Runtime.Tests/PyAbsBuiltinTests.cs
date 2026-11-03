// abs 的绑定、协议身份和冷错误报告；通用星号展开错误仍是既有边界，不声称已补其帧。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyAbsBuiltinTests
{
    private static object? Abs(object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallBuiltin("abs", args, names ?? [], values ?? []);
    private static PyInstance Operand(Func<object?> action)
    {
        var cls = new PyClass("AbsoluteOperand");
        cls.SetAttr("__abs__", PyNativeFunctions.Create("__abs__", ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }
    private static void InCaller(Action<PyValueError> body)
    {
        long depth = PyTraceback.PushFrame("abs_caller.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try { PyTraceback.SetLine(17); body(outer); }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
    private static void CallerError(PyBaseException error, PyBaseException outer)
    {
        Assert.Equal(new PyStackFrame("abs_caller.py", "caller", 17), error.Frames[0]);
        Assert.Same(outer, error.Context); Assert.Same(outer, PyErrors.Current);
    }

    [Fact]
    public void PrimitiveAndProtocolResultsKeepExistingIdentityAndSingleCall()
    {
        Assert.Equal(3L, Abs([-3L])); Assert.Equal(2.5, Abs([-2.5])); Assert.Equal(1L, Abs([true]));
        var result = new PyList([]); int calls = 0;
        var value = Operand(() => { calls++; return result; });
        Assert.Same(result, Abs([value])); Assert.Equal(1, calls);
        Assert.Same(PyNone.Instance, Abs([Operand(() => PyNone.Instance)]));
    }

    [Fact]
    public void ZeroAndMultipleArgumentsPrepareCallerWithoutCallingProtocol()
    {
        int calls = 0; var value = Operand(() => { calls++; return 0L; });
        InCaller(outer =>
        {
            CallerError(Assert.Throws<PyTypeError>(() => Abs([])), outer);
            CallerError(Assert.Throws<PyTypeError>(() => Abs([value, value])), outer);
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public void KeywordsRejectBeforeProtocolAndPrepareCaller()
    {
        int calls = 0; var value = Operand(() => { calls++; return 0L; });
        InCaller(outer =>
        {
            CallerError(Assert.Throws<PyTypeError>(() => Abs([], ["x"], [value])), outer);
            CallerError(Assert.Throws<PyTypeError>(() => Abs([value], ["extra"], [1L])), outer);
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public void SuccessfullyExpandedStarUsesTheSameStrictBinder()
    {
        Assert.Equal(7L, Abs([PyOps.StarArg(new PyList([-7L]))]));
        InCaller(outer =>
        {
            CallerError(Assert.Throws<PyTypeError>(() => Abs([PyOps.StarArg(new PyList([]))])), outer);
            CallerError(Assert.Throws<PyTypeError>(() => Abs([PyOps.StarArg(new PyList([1L, 2L]))])), outer);
        });
    }

    [Fact]
    public void InvalidStarAndIteratorFailureKeepExistingExceptionAndSideEffects()
    {
        Assert.Throws<PyTypeError>(() => Abs([PyOps.StarArg(42L)]));
        var error = new PyLookupError("iterator failed"); int calls = 0;
        var cls = new PyClass("BrokenIterable");
        cls.SetAttr("__iter__", PyNativeFunctions.Create("__iter__", ["self"], [null], 1,
            _ => { calls++; throw error; }));
        Assert.Same(error, Assert.Throws<PyLookupError>(() => Abs([PyOps.StarArg(new PyInstance(cls))])));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void UserBodyExceptionIsNotPreparedAgainOrReplaced()
    {
        int calls = 0; var error = new PyLookupError("body failed");
        var originalContext = new PyValueError("original");
        object? previous = PyErrors.EnterHandler(originalContext);
        long depth = PyTraceback.PushFrame("abs_body.py", "__abs__");
        try { PyTraceback.SetLine(31); PyErrors.PrepareRaise(error); }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        var frames = error.Frames;
        var value = Operand(() => { calls++; throw error; });
        InCaller(_ => Assert.Same(error, Assert.Throws<PyLookupError>(() => Abs([value]))));
        Assert.Equal(1, calls); Assert.Same(frames, error.Frames); Assert.Same(originalContext, error.Context);
    }

    [Fact]
    public void FreshUserBodyFailureRemainsTheOriginalUnwrappedFailure()
    {
        var error = new PyValueError("body raw");
        InCaller(_ => Assert.Same(error, Assert.Throws<PyValueError>(() => Abs([Operand(() => throw error)]))));
        Assert.Empty(error.Frames); Assert.Null(error.Context);
    }

    [Fact]
    public void DirectDynamicBadOperandPreparesCallerAndContext()
    {
        InCaller(outer =>
        {
            CallerError(Assert.Throws<PyTypeError>(() => PyOps.AbsValue("bad")), outer);
            CallerError(Assert.Throws<PyTypeError>(() => Abs([PyNone.Instance])), outer);
        });
    }
}
