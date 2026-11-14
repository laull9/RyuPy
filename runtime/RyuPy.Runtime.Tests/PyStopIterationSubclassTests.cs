// 锁定生成异常的三参数基类 ABI、真实 CLR 子类与原参数延迟格式化；不改变生成器内部完成入口。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStopIterationSubclassTests
{
    // 与现 codegen 一致：先请求基类消息策略，再把完整原始参数传入 protected 构造。
    private class Child(object?[] args) : PyStopIteration("Child",
        PyErrors.UserExceptionMessageForBase("StopIteration", args), args);
    private sealed class Grandchild(object?[] args) : Child(args);

    private static PyInstance Payload(Func<string> text, Func<string> representation)
    {
        var cls = (PyClass)PyOps.MakeClass("Payload", [], null);
        cls.SetAttr("__str__", PyNativeFunctions.CreateVariadic("str", _ => text()));
        cls.SetAttr("__repr__", PyNativeFunctions.CreateVariadic("repr", _ => representation()));
        return Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }

    [Fact]
    public void ProtectedConstructorPreservesZeroNoneOneAndManyArguments()
    {
        var empty = new Child([]);
        Assert.Empty(empty.Args); Assert.Null(empty.Value); Assert.Equal("", empty.PyMessage);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(empty, "value"));
        var none = new Child([PyNone.Instance]);
        Assert.Same(PyNone.Instance, Assert.Single(none.Args));
        Assert.Same(PyNone.Instance, none.Value); Assert.Equal("None", none.PyMessage);
        object first = new PyList(); object second = new PyDict();
        var one = new Child([first]); var many = new Child([first, second]);
        Assert.Same(first, Assert.Single(one.Args)); Assert.Same(first, one.Value);
        Assert.Equal(2, many.Args.Length); Assert.Same(first, many.Args[0]);
        Assert.Same(second, many.Args[1]); Assert.Same(first, many.Value);
        var tuple = new PyTuple([1L, 2L]); var tupleValue = new Child([tuple]);
        Assert.Same(tuple, Assert.Single(tupleValue.Args)); Assert.Same(tuple, tupleValue.Value);
        Assert.Equal("(1, 2)", tupleValue.PyMessage);
    }

    [Fact]
    public void ConstructorOwnsTheArgumentArrayButSharesElementsAndSavesValue()
    {
        object first = new PyList(); object second = new PyDict();
        object?[] input = [first, second]; var child = new Child(input);
        Assert.NotSame(input, child.Args);
        input[0] = 42L; input[1] = 43L;
        Assert.Same(first, child.Args[0]); Assert.Same(second, child.Args[1]);
        Assert.Same(first, child.Value);
        // .NET 直接操作数组也不能让 value 变成动态 Args[0]；Python args 重绑不在本切范围。
        child.Args[0] = "changed";
        Assert.Same(first, child.Value);
    }

    [Fact]
    public void SingleArgumentFormattingIsLazyAndObservesLaterChanges()
    {
        var events = new List<string>(); string value = "first";
        PyInstance payload = Payload(() => { events.Add("str"); return value; },
            () => { events.Add("repr"); return "representation"; });
        var child = new Child([payload]); var grandchild = new Grandchild([payload]);
        Assert.Empty(events); Assert.Same(payload, child.Value); Assert.Same(payload, grandchild.Value);
        Assert.Equal("first", child.PyMessage); value = "second";
        Assert.Equal("second", child.Message); Assert.Equal("second", PyRepr.Str(grandchild));
        Assert.Equal<string>(["str", "str", "str"], events);
    }

    [Fact]
    public void MultipleArgumentsUseReprInOrderOnlyWhenTheMessageIsRead()
    {
        var events = new List<string>();
        PyInstance left = Payload(() => { events.Add("str:left"); return "wrong"; },
            () => { events.Add("repr:left"); return "L"; });
        PyInstance right = Payload(() => { events.Add("str:right"); return "wrong"; },
            () => { events.Add("repr:right"); return "R"; });
        var child = new Child([left, right]);
        Assert.Empty(events); Assert.Same(left, child.Value);
        Assert.Equal("(L, R)", child.PyMessage);
        Assert.Equal<string>(["repr:left", "repr:right"], events);
        events.Clear(); Assert.Equal("(L, R)", child.Message);
        Assert.Equal<string>(["repr:left", "repr:right"], events);
    }

    [Fact]
    public void ExplicitFormattingPreservesTheOriginalUserException()
    {
        var error = new PyRuntimeError("payload failure"); int calls = 0;
        PyInstance payload = Payload(() => { calls++; throw error; },
            () => { calls++; throw error; });
        var one = new Child([payload]); var many = new Child([payload, 2L]);
        Assert.Equal(0, calls); Assert.Same(payload, one.Value); Assert.Same(payload, many.Value);
        Assert.Same(error, Assert.Throws<PyRuntimeError>(() => one.PyMessage));
        Assert.Equal(1, calls);
        Assert.Same(error, Assert.Throws<PyRuntimeError>(() => many.Message));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void RealClrCatchAndTheExistingInternalCompletionConstructorRemainIntact()
    {
        var child = new Grandchild([7L]); PyStopIteration? caught = null;
        try { throw child; } catch (PyStopIteration error) { caught = error; }
        Assert.Same(child, caught); Assert.Equal("Child", child.PyTypeName);
        Assert.False(new PyException("StopIteration", "fake") is PyStopIteration);
        var cls = (PyClass)PyOps.MakeClass("Child", [], null); child.BindPythonClass(cls);
        Assert.Same(cls, PyObject.TypeOf(child));
        foreach (object? value in new object?[] { null, PyNone.Instance })
        {
            var completed = new PyStopIteration(value);
            Assert.Null(completed.Value); Assert.Empty(completed.Args); Assert.Equal("", completed.Message);
        }
        int calls = 0;
        PyInstance result = Payload(() => { calls++; return "return value"; }, () => "repr");
        var stop = new PyStopIteration(result);
        Assert.Equal(0, calls); Assert.Same(result, stop.Value); Assert.Same(result, Assert.Single(stop.Args));
        Assert.Equal("return value", stop.Message); Assert.Equal(1, calls);
        var tuple = new PyTuple([1L, 2L]); var tupleStop = new PyStopIteration(tuple);
        Assert.Same(tuple, tupleStop.Value); Assert.Same(tuple, Assert.Single(tupleStop.Args));
    }
}
