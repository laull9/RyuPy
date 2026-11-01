using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyPropertyAccessorTests
{
    [Fact]
    public void AccessorDecoratorsCopyDescriptorAndDeleteUsesDeleter()
    {
        var getter = PyRuntimeDataclassTests.Fn("get", ["self"], args => PyObject.GetAttr(args[0], "stored"));
        var setter = PyRuntimeDataclassTests.Fn("set", ["self", "value"], args =>
        {
            PyObject.SetAttr(args[0], "stored", args[1]);
            return PyNone.Instance;
        });
        var deleter = PyRuntimeDataclassTests.Fn("delete", ["self"], args =>
        {
            PyObject.DelAttr(args[0], "stored");
            return PyNone.Instance;
        });
        var original = new PyProperty(getter);
        object? accessor = PyObject.GetAttr(original, "setter");
        Assert.Equal(true, PyOps.CallBuiltin("callable", [accessor], [], []));
        Assert.Equal("builtin_function_or_method", PyRepr.TypeName(accessor));
        Assert.Null(new PyProperty(PyNone.Instance).Getter);
        var writable = Assert.IsType<PyProperty>(PyOps.CallMethod(original, "setter", [setter], [], []));
        var deletable = Assert.IsType<PyProperty>(PyOps.CallValue(PyOps.PrepareMethod(writable, "deleter"), [deleter], [], []));
        Assert.NotSame(original, writable);
        Assert.Null(original.Setter);
        Assert.Same(setter, PyObject.GetAttr(writable, "fset"));
        Assert.Same(deleter, PyObject.GetAttr(deletable, "fdel"));
        var cls = (PyClass)PyOps.MakeClass("Owner", [], null);
        cls.SetAttr("value", deletable);
        object instance = cls.Invoke([], [], [])!;
        PyObject.SetAttr(instance, "value", 7L);
        Assert.Equal(7L, PyObject.GetAttr(instance, "value"));
        PyObject.DelAttr(instance, "value");
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "stored"));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(original, "setter", [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(original, "getter", [getter, getter], [], []));
    }
    [Fact]
    public void ClassMethodsBindActualClassThroughInstancesAndInheritance()
    {
        var cls = (PyClass)PyOps.MakeClass("Base", [], null);
        var function = PyRuntimeDataclassTests.Fn("label", ["cls"], args => PyObject.GetAttr(args[0], "__name__"));
        cls.SetAttr("label", new PyClassMethod(function));
        var child = (PyClass)PyOps.MakeClass("Child", [cls], null);
        object instance = child.Invoke([], [], [])!;
        Assert.Equal("Base", PyOps.CallMethod(cls, "label", [], [], []));
        Assert.Equal("Child", PyOps.CallMethod(child, "label", [], [], []));
        Assert.Equal("Child", PyOps.CallMethod(instance, "label", [], [], []));
        Assert.Equal("Child", PyOps.CallValue(PyOps.PrepareMethod(instance, "label"), [], [], []));
        Assert.True(PyObject.TryMethod(instance, "label", [], out object? result));
        Assert.Equal("Child", result);
        var bound = Assert.IsType<PyBoundMethod>(PyObject.GetAttr(instance, "label"));
        Assert.Same(child, bound.Self);
    }

}
