using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>
/// 普通类与属性协议（M2.5）的运行时单元测试。
/// </summary>
/// <remarks>
/// 这些行为在 e2e 里也有差分用例；这里单独覆盖查找优先级与缓存失效，
/// 因为二者的 bug 很容易只在「先读缓存、再修改、再读」的顺序下出现。
/// </remarks>
public class PyClassTests
{
    private static PyFunction Fn(string name, Func<object?[], object?> body, params string[] paramsNames)
    {
        var names = paramsNames.Length == 0 ? new[] { "self" } : paramsNames;
        var sig = PySignature.Create(
            name,
            names,
            names.Select(_ => "pos").ToArray(),
            names.Select(_ => "0").ToArray()
        );
        return new PyFunction(name, sig, (self, args, _, _) => body(args), []);
    }

    private static PyClass NewClass(string name) => (PyClass)PyOps.MakeClass(name, [], null);

    [Fact]
    public void InstanceAttributeShadowsClassAttributeUntilDeleted()
    {
        PyClass cls = NewClass("Box");
        cls.SetAttr("value", 1L);
        var instance = (PyInstance)cls.Invoke([], [], [])!;

        Assert.Equal(1L, PyObject.GetAttr(instance, "value"));
        PyObject.SetAttr(instance, "value", 2L);
        Assert.Equal(2L, PyObject.GetAttr(instance, "value"));
        Assert.Equal(1L, PyObject.GetAttr(cls, "value"));

        PyObject.DelAttr(instance, "value");
        Assert.Equal(1L, PyObject.GetAttr(instance, "value"));
    }

    [Fact]
    public void ClassAttributeReplacementInvalidatesLookupCache()
    {
        PyClass cls = NewClass("Item");
        cls.SetAttr("value", 1L);
        var instance = (PyInstance)cls.Invoke([], [], [])!;

        // 预热缓存
        Assert.Equal(1L, PyObject.GetAttr(instance, "value"));
        Assert.Equal(1L, PyObject.GetAttr(instance, "value"));

        cls.SetAttr("value", 9L);
        Assert.Equal(9L, PyObject.GetAttr(instance, "value"));
        cls.SetAttr("value", 4L);
        Assert.Equal(4L, PyObject.GetAttr(instance, "value"));
    }

    [Fact]
    public void PropertyWinsOverInstanceDictionary()
    {
        PyClass cls = NewClass("Box");
        var getter = Fn("get_x", args => PyObject.GetAttr(args[0], "x"));
        cls.SetAttr("x", 1L);
        cls.SetAttr("doubled", new PyProperty(getter));

        var instance = (PyInstance)cls.Invoke([], [], [])!;
        PyObject.SetAttr(instance, "x", 5L);
        Assert.Equal(5L, PyObject.GetAttr(instance, "doubled"));

        // 数据描述符优先：Python 层赋值会走 property setter，没有 setter 时直接报错，
        // 不会把值塞进实例槽位。
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "doubled", 100L));
        Assert.Equal(5L, PyObject.GetAttr(instance, "doubled"));
    }

    [Fact]
    public void BoundMethodAndClassMethodReceiveCorrectReceiver()
    {
        PyClass cls = NewClass("Box");
        cls.SetAttr("category", "box");
        cls.SetAttr("who", Fn("who", args => args[0]));
        cls.SetAttr(
            "named",
            new PyClassMethod(Fn("named", args => PyObject.GetAttr(args[0], "category")))
        );
        var instance = (PyInstance)cls.Invoke([], [], [])!;

        object? bound = PyObject.GetAttr(instance, "who");
        Assert.IsType<PyBoundMethod>(bound);
        Assert.Same(instance, PyOps.CallValue(bound, [], [], []));
        Assert.Equal("box", PyOps.CallValue(PyObject.GetAttr(cls, "named"), [], [], []));
    }

    [Fact]
    public void MagicMethodsDriveEqualityHashTruthAndLength()
    {
        PyClass cls = NewClass("Vec");
        cls.SetAttr("__eq__", new PyFunction("__eq__", PySignature.Create(
            "__eq__",
            ["self", "other"],
            ["pos", "pos"],
            ["0", "0"]
        ), (self, args, _, _) =>
            PyHash.Equal(PyObject.GetAttr(args[0], "x"), PyObject.GetAttr(args[1], "x")), []));
        cls.SetAttr("__hash__", Fn("__hash__", args => PyHash.Of(PyObject.GetAttr(args[0], "x"))));
        cls.SetAttr("__bool__", Fn("__bool__", args => PyOps.Truthy(PyObject.GetAttr(args[0], "x"))));
        cls.SetAttr("__len__", Fn("__len__", args => 3L));

        var a = (PyInstance)cls.Invoke([], [], [])!;
        var b = (PyInstance)cls.Invoke([], [], [])!;
        PyObject.SetAttr(a, "x", 2L);
        PyObject.SetAttr(b, "x", 2L);

        Assert.True(PyHash.Equal(a, b));
        Assert.Equal(PyHash.Of(2L), PyHash.Of(a));
        Assert.True(PyOps.Truthy(a));
        Assert.Equal(3L, PyOps.Len(a));
    }
}
