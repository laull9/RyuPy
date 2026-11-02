// 固定factory只复用现binder；不在测试中加载C库。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNativeBindingFactoryTests
{
    [Fact]
    public void RequiredPositionalBindingCapturesArityAndKeywordFailuresBeforeBody()
    {
        int called = 0;
        PyFunction function = PyNativeBindings.CreateI32Binary("sum_i32", _ => { called++; return 0L; });
        var outer = new PyValueError("outer"); object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("binding_use.py", "caller");
        try
        {
            PyTraceback.SetLine(12);
            (object?[] Args, string[] Names, object?[] Values)[] failures =
            [
                ([], [], []), ([1L], [], []), ([1L, 2L, 3L], [], []),
                ([], ["left", "right"], [1L, 2L]), ([1L, 2L], ["left"], [3L]),
                ([1L, 2L], ["unknown"], [3L]),
            ];
            foreach (var call in failures)
            {
                var error = Assert.Throws<PyTypeError>(() => function.Invoke(call.Args, call.Names, call.Values));
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("binding_use.py", "caller", 12), error.Frames[0]);
                Assert.Equal(0, called);
            }
            Assert.Same(outer, PyErrors.Current);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void ValidBindingPassesOriginalObjectsAndBodyExceptionsAreNotCaughtByFactory()
    {
        var left = new PyList([]); var right = new PyList([]); var result = new PyList([]);
        var function = PyNativeBindings.CreateI32Binary("identity_probe", bound =>
        {
            Assert.Same(left, bound[0]); Assert.Same(right, bound[1]); return result;
        });
        Assert.Same(result, function.Invoke([left, right], [], []));
        var original = new PyValueError("body error");
        var throwing = PyNativeBindings.CreateI32Binary("throwing", _ => throw original);
        var outer = new PyTypeError("outer"); object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("body.py", "caller");
        try
        {
            PyTraceback.SetLine(5);
            Assert.Same(original, Assert.Throws<PyValueError>(() => throwing.Invoke([1L, 2L], [], [])));
            Assert.Empty(original.Frames); Assert.Null(original.Context);
            Assert.Same(outer, PyErrors.Current);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void FixedBodyValidatesLeftThenRightAndBoxesResultUsingExistingIntegerPolicy()
    {
        int leftChecked = 0, rightChecked = 0, nativeCalled = 0;
        var function = PyNativeBindings.CreateI32Binary("add", bound =>
        {
            leftChecked++;
            int left = PyNativeBindings.CheckedI32(bound[0], "add", "left");
            rightChecked++;
            int right = PyNativeBindings.CheckedI32(bound[1], "add", "right");
            nativeCalled++;
            return PyOps.BoxInt((long)left + right);
        });
        Assert.Throws<PyTypeError>(() => function.Invoke([true, long.MaxValue], [], []));
        Assert.Equal(1, leftChecked); Assert.Equal(0, rightChecked); Assert.Equal(0, nativeCalled);
        Assert.Throws<PyOverflowError>(() => function.Invoke([1L, long.MaxValue], [], []));
        Assert.Equal(2, leftChecked); Assert.Equal(1, rightChecked); Assert.Equal(0, nativeCalled);
        Assert.Equal(4294967294L, function.Invoke([(long)int.MaxValue, (long)int.MaxValue], [], []));
        Assert.Equal(1, nativeCalled);
        Assert.Same(PyOps.BoxInt(3), function.Invoke([1L, 2L], [], []));
    }
}
