// 默认builtins冷读取的共享存储、真实绑定和既有异常合同；不代替编译产品验收。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyBuiltinLookupTests
{
    private static PyModule Module(PyModuleExecute execute)
    {
        string name = "builtin_lookup_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "lookup.py", 1, "/virtual/lookup.py", false, execute);
        return PyModuleInstances.Create(name);
    }

    private static PyDict Default() => Assert.IsType<PyDict>(Module(_ => { }).GetAttr("__builtins__"));

    [Fact]
    public void GenericLookupReturnsTheSharedCurrentBindingIncludingNull()
    {
        var builtins = Default();
        Assert.Same(builtins, Default());
        foreach (string name in new[] { "KeyError", "len" })
        {
            object? original = builtins.Get(name);
            try
            {
                Assert.Same(original, PyOps.LoadDefaultBuiltin(name));
                var first = new PyList(); var second = new PyDict();
                builtins.Set(name, first);
                object? saved = PyOps.LoadDefaultBuiltin(name);
                builtins.Set(name, second);
                Assert.Same(first, saved); Assert.Same(second, PyOps.LoadDefaultBuiltin(name));
                builtins.Set(name, PyNone.Instance); Assert.Same(PyNone.Instance, PyOps.LoadDefaultBuiltin(name));
                builtins.Set(name, null); Assert.Null(PyOps.LoadDefaultBuiltin(name));
            }
            finally { builtins.Set(name, original); }
        }
    }

    [Fact]
    public void MissingSupportedBuiltinIsNameErrorAndUnsupportedClassificationIsSeparate()
    {
        var builtins = Default(); object? original = builtins.Get("KeyError");
        try
        {
            builtins.Delete("KeyError");
            Assert.Equal("name 'KeyError' is not defined", Assert.Throws<PyNameError>(() => PyOps.LoadDefaultBuiltin("KeyError")).PyMessage);
            Assert.Throws<PyNameError>(() => PyOps.LoadDefaultBuiltin("definitely_not_builtin"));
            Assert.Throws<PyNotImplementedError>(() => PyOps.LoadDefaultBuiltin("exec"));
        }
        finally { builtins.Set("KeyError", original); }
    }

    [Fact]
    public void InstanceOwnerAndGlobalsStillUseTheCapturedDictionary()
    {
        object? actual = null;
        var module = Module(context => actual = PyModuleInstances.LoadName(context, "KeyError"));
        object? canonical = PyOps.LoadDefaultBuiltin("KeyError");
        var custom = new PyDict(); var first = new PyList(); var second = new PyList();
        custom.Set("KeyError", first); module.SetAttr("__builtins__", custom);
        PyModuleInstances.Execute(module); Assert.Same(first, actual);
        custom.Set("KeyError", second); PyModuleInstances.Execute(module); Assert.Same(second, actual);
        module.SetAttr("KeyError", PyNone.Instance); PyModuleInstances.Execute(module); Assert.Same(PyNone.Instance, actual);
        Assert.Same(canonical, PyOps.LoadDefaultBuiltin("KeyError"));
    }

    [Fact]
    public void LoadedKeyErrorUsesFullArgumentsWithoutChangingInternalMessages()
    {
        object? type = PyOps.LoadDefaultBuiltin("KeyError");
        var empty = Assert.IsType<PyKeyError>(PyOps.CallValue(type, [], [], []));
        Assert.Empty(empty.Args); Assert.Equal("", PyRepr.Str(empty));
        foreach (object? key in new object?[] { PyNone.Instance, "", new PyList(), new PyTuple([1L, "two"]) })
        {
            var raw = new object?[] { key };
            var error = Assert.IsType<PyKeyError>(PyOps.CallValue(type, raw, [], []));
            Assert.NotSame(raw, error.Args); Assert.Same(key, Assert.Single(error.Args));
            Assert.Equal(PyRepr.Repr(key), PyRepr.Str(error));
        }
        var left = new PyList(); var right = new PyDict();
        var many = Assert.IsType<PyKeyError>(PyOps.CallValue(type, [left, right, PyNone.Instance, "last"], [], []));
        Assert.Equal(4, many.Args.Length); Assert.Same(left, many.Args[0]); Assert.Same(right, many.Args[1]);
        Assert.Equal("pre-rendered", PyRepr.Str(new PyKeyError("pre-rendered")));
        var missing = PyKeyError.ForKey(left);
        Assert.Same(left, Assert.Single(missing.Args)); Assert.Equal("[]", PyRepr.Str(missing));
    }

    [Fact]
    public void KeyRepresentationIsDeferredAndExplicitFailuresKeepTheirIdentity()
    {
        int calls = 0; var marker = new PyValueError("repr failure"); var cls = new PyClass("Key");
        cls.SetAttr("__str__", PyNativeFunctions.Create("__str__", ["self"], [null], 1,
            _ => throw new PyAssertionError("KeyError construction called str")));
        cls.SetAttr("__repr__", PyNativeFunctions.Create("__repr__", ["self"], [null], 1,
            _ => { calls++; throw marker; }));
        var key = new PyInstance(cls);
        var error = Assert.IsType<PyKeyError>(PyOps.CallValue(PyOps.LoadDefaultBuiltin("KeyError"), [key], [], []));
        Assert.Equal(0, calls); Assert.Same(key, Assert.Single(error.Args));
        Assert.Same(marker, Assert.Throws<PyValueError>(() => PyRepr.Str(error)));
        Assert.Equal(1, calls);
    }
}
