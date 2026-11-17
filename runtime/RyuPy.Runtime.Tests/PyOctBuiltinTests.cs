// oct通用协议的冷错误现场、单次用户槽和异常身份；源码差分另覆盖大整数、警告、绑定和遮蔽。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyOctBuiltinTests
{
    private static object? Oct(object?[] args, object?[]? names = null) =>
        PyOps.CallBuiltin("oct", args, names ?? [], names is null ? [] : [1L]);

    private static PyInstance Index(Func<object?> action)
    {
        var cls = new PyClass("OctalOperand");
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }

    [Fact]
    public void BindingRejectsBeforeCallingIndexAndBuiltinIdentityIsStable()
    {
        int calls = 0;
        var value = Index(() => { calls++; return 8L; });
        Assert.Equal("oct() takes exactly one argument (0 given)", Assert.Throws<PyTypeError>(() => Oct([])).Message);
        Assert.Equal("oct() takes exactly one argument (2 given)", Assert.Throws<PyTypeError>(() => Oct([value, value])).Message);
        Assert.Equal("oct() takes no keyword arguments", Assert.Throws<PyTypeError>(() => Oct([value], ["value"])).Message);
        Assert.Equal(0, calls);
        Assert.Same(PyOps.BuiltinFunction("oct"), PyOps.LoadDefaultBuiltin("oct"));
    }

    [Fact]
    public void IntegerProtocolIsCalledOnceAndDoesNotInvokeOriginalFormat()
    {
        int calls = 0;
        var value = Index(() => { calls++; return -511L; });
        value.Class.SetAttr("__format__", PyNativeFunctions.Create("__format__", ["self", "spec"], [null, null], 2,
            _ => throw new PyRuntimeError("must not format original")));
        Assert.Equal("-0o777", Oct([value]));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ExistingUserFailureKeepsIdentityFramesAndContext()
    {
        var error = new PyLookupError("index failed");
        var context = new PyValueError("context");
        object? previous = PyErrors.EnterHandler(context);
        long depth = PyTraceback.PushFrame("index_body.py", "__index__");
        try { PyTraceback.SetLine(13); PyErrors.PrepareRaise(error); }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        var frames = error.Frames;
        int calls = 0;
        Assert.Same(error, Assert.Throws<PyLookupError>(() => Oct([Index(() => { calls++; throw error; })])));
        Assert.Same(frames, error.Frames);
        Assert.Same(context, error.Context);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FreshBindingAndBadResultErrorsKeepCallerContext()
    {
        long depth = PyTraceback.PushFrame("oct_caller.py", "caller");
        var context = new PyValueError("outer");
        object? previous = PyErrors.EnterHandler(context);
        try
        {
            PyTraceback.SetLine(19);
            foreach (Action action in new Action[] { () => Oct([]), () => Oct(["bad"]), () => Oct([Index(() => "bad")]) })
            {
                var error = Assert.Throws<PyTypeError>(action);
                Assert.Equal(new PyStackFrame("oct_caller.py", "caller", 19), error.Frames[0]);
                Assert.Same(context, error.Context);
                Assert.Same(context, PyErrors.Current);
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
}
