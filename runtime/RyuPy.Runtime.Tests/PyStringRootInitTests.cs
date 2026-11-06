// 真实继承的object.__init__身份和String专用冷分支；不重写普通object构造规则。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringRootInitTests
{
    private static object? Invoke(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static PyClass Class()
    {
        var cls = (PyClass)PyOps.MakeClass("S", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls); return cls;
    }

    [Fact]
    public void InitIsCanonicalObjectDescriptorThroughStrAndSubclass()
    {
        object? root = PyObject.GetAttr(PyBaseObject.Type, "__init__");
        Assert.Same(root, PyObject.GetAttr(PyType.Get("str"), "__init__"));
        Assert.Same(root, PyObject.GetAttr(Class(), "__init__"));
        Assert.Equal("object.__init__", PyObject.GetAttr(root, "__qualname__"));
        Assert.Same(PyBaseObject.Type, PyObject.GetAttr(root, "__objclass__"));
    }

    [Fact]
    public void DefaultStringInitIgnoresArgsButCustomInitSuperRejectsExtras()
    {
        var cls = Class(); object value = cls.Invoke(["raw"], [], [])!;
        object? init = PyObject.GetAttr(value, "__init__");
        Assert.Same(PyNone.Instance, Invoke(init, "ignored")); Assert.Equal("raw", PyRepr.Str(value));
        object? root = PyObject.GetAttr(PyBaseObject.Type, "__init__");
        Assert.Same(PyNone.Instance, Invoke(root, "exact", "ignored"));
        cls.SetAttr("__init__", PyNativeFunctions.CreateVariadic("custom", _ => PyNone.Instance));
        Assert.Same(PyNone.Instance, Invoke(root, value));
        Assert.Throws<PyTypeError>(() => Invoke(root, value, "extra"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [value], ["x"], [1L]));
    }

    [Fact]
    public void OrdinaryObjectInitializationRetainsItsOwnArgumentChecks()
    {
        object bare = PyBaseObject.Create([], []); object? root = PyObject.GetAttr(PyBaseObject.Type, "__init__");
        Assert.Same(PyNone.Instance, Invoke(root, bare));
        Assert.Throws<PyTypeError>(() => Invoke(root, bare, 1L));
        var ordinary = (PyClass)PyOps.MakeClass("Ordinary", [], null); PyOps.FinalizeOrdinaryClass(ordinary);
        object value = ordinary.Invoke([], [], [])!;
        Assert.Same(PyNone.Instance, Invoke(root, value)); Assert.Throws<PyTypeError>(() => Invoke(root, value, 1L));
    }
}
