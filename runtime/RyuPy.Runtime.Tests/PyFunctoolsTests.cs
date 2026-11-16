// 包装器测试同时检查绑定、可变关键字、元数据别名与归约的惰性消费。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyFunctoolsTests
{
    internal static readonly Dictionary<string, object?> Module = PyFunctools.CreateAttributes();
    internal static object? Invoke(object? value, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(value, args, names ?? [], values ?? []);
    internal static PyFunction Function(string name, PyCall body)
    {
        var function = new PyFunction(name, PySignature.Create(name, [], [], []), body, []);
        // 本fixture模拟已知名字的Python函数，显式提供限定名；不捏造源码行号。
        PyObject.SetAttr(function, "__qualname__", name);
        return function;
    }

    [Fact]
    public void PartialPreservesArgumentsLiveKeywordsAndDoesNotBindSelf()
    {
        var function = Function("echo", (_, args, names, values) => new PyTuple([new PyTuple(args), new PyTuple(names), new PyTuple(values)]));
        var partial = Assert.IsType<PyPartial>(Invoke(Module["partial"], [function, 1L], ["x"], [2L]));
        var keywords = Assert.IsType<PyDict>(partial.GetAttr("keywords")); keywords.Set("x", 3L);
        Assert.Equal("((1, 4), ('x', 'y'), (5, 6))", PyRepr.Repr(Invoke(partial, [4L], ["x", "y"], [5L, 6L])));
        Assert.Same(function, partial.GetAttr("func"));
        Assert.Throws<PyAttributeError>(() => partial.SetAttr("args", new PyTuple([])));
        var cls = (PyClass)PyOps.MakeClass("Holder", [], null); cls.SetAttr("p", partial);
        var instance = cls.Invoke([], [], []);
        Assert.Same(partial, PyObject.GetAttr(instance, "p"));
        Assert.True((bool)PyOps.CallBuiltin("callable", [partial], [], [])!);
        keywords.Set(1L, 2L);
        Assert.Throws<PyTypeError>(() => Invoke(partial, []));
    }

    [Fact]
    public void NestedPartialFlattensOnlyWithoutCustomAttributes()
    {
        var function = Function("echo", (_, args, _, _) => new PyTuple(args));
        var first = Assert.IsType<PyPartial>(Invoke(Module["partial"], [function, 1L]));
        var flat = Assert.IsType<PyPartial>(Invoke(Module["partial"], [first, 2L]));
        Assert.Same(function, flat.GetAttr("func")); Assert.Equal("(1, 2)", PyRepr.Repr(flat.GetAttr("args")));
        first.SetAttr("custom", true);
        var nested = Assert.IsType<PyPartial>(Invoke(Module["partial"], [first, 3L]));
        Assert.Same(first, nested.GetAttr("func")); Assert.Equal("(1, 3)", PyRepr.Repr(Invoke(nested, [])));
        var touched = Assert.IsType<PyPartial>(Invoke(Module["partial"], [function]));
        _ = touched.GetAttr("__dict__");
        var preserve = Assert.IsType<PyPartial>(Invoke(Module["partial"], [touched]));
        Assert.Same(touched, preserve.GetAttr("func"));
    }

    [Fact]
    public void ReduceDistinguishesMissingInitialFromNoneAndDoesNotSwallowReducerErrors()
    {
        var add = Function("add", (_, args, _, _) => (long)args[0]! + (long)args[1]!);
        Assert.Equal(6L, Invoke(Module["reduce"], [add, new PyList([1L, 2L, 3L])]));
        Assert.IsType<PyNone>(Invoke(Module["reduce"], [add, new PyList(), PyNone.Instance]));
        Assert.Throws<PyTypeError>(() => Invoke(Module["reduce"], [add, new PyList()]));
        var error = new PyStopIteration("user");
        var fail = Function("fail", (_, _, _, _) => throw error);
        Assert.Same(error, Assert.Throws<PyStopIteration>(() => Invoke(Module["reduce"], [fail, new PyList([1L, 2L])])));
        Assert.Throws<PyTypeError>(() => Invoke(Module["reduce"], [add, new PyList()], ["initial"], [0L]));
    }

    [Fact]
    public void WrapsSharesAnnotationsUpdatesAttributesAndSetsWrappedLast()
    {
        var original = Function("original", (_, args, _, _) => args[0]);
        var wrapper = Function("wrapper", (_, args, _, _) => args[0]);
        PyObject.SetAttr(original, "__doc__", "doc");
        PyObject.SetAttr(original, "__wrapped__", "old"); PyObject.SetAttr(original, "custom", 1L);
        var annotations = new PyDict(); annotations.Set("x", PyType.Get("int"));
        PyObject.SetAttr(original, "__annotations__", annotations);
        object? decorator = Invoke(Module["wraps"], [original]);
        Assert.Same(wrapper, Invoke(decorator, [wrapper]));
        Assert.Equal("original", PyObject.GetAttr(wrapper, "__name__"));
        Assert.Equal("doc", PyObject.GetAttr(wrapper, "__doc__"));
        Assert.Equal(1L, PyObject.GetAttr(wrapper, "custom"));
        Assert.Same(annotations, PyObject.GetAttr(wrapper, "__annotations__"));
        Assert.Same(original, PyObject.GetAttr(wrapper, "__wrapped__"));
        Assert.Equal("original", PyObject.GetAttr(wrapper, "__qualname__"));
    }
}
