using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>验证新元数据对象经过共享调用/类型分派，而非只测试独立构造helper。</summary>
public class PyTypeMetadataRoutingTests
{
    [Fact]
    public void EllipsisRoutesAsSingletonAndItsTypeRemainsCallable()
    {
        object value = PyOps.BuiltinFunction("Ellipsis");
        Assert.Same(PyEllipsis.Instance, value);
        object type = PyObject.TypeOf(value);
        Assert.Same(type, PyObject.GetAttr(value, "__class__"));
        Assert.Equal("builtins", PyObject.GetAttr(type, "__module__"));
        Assert.Same(value, PyOps.CallValue(type, [], [], []));
        Assert.Equal(false, PyOps.CallBuiltin("callable", [value], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("callable", [type], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [value, PyType.Get("object")], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(value, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("Ellipsis", [], [], []));
        var typing = PyTyping.CreateAttributes();
        object? tuple = PyOps.GetItem(typing["Tuple"], new PyTuple([PyType.Get("int"), value]));
        Assert.Equal("typing.Tuple[int, ...]", PyRepr.Repr(tuple));
        var args = Assert.IsType<PyTuple>(PyOps.CallValue(typing["get_args"], [tuple], [], []));
        Assert.Same(value, args.Items[1]);
    }

    [Fact]
    public void TypedDictIsATypeButCannotBeTheRuntimeCheckTarget()
    {
        var fields = new PyDict(); fields.Set("x", PyType.Get("int"));
        object? type = PyOps.CallValue(PyTypedDict.Factory, ["Routing", fields], ["total"], [false]);
        Assert.IsType<PyTypedDictType>(type);
        Assert.Equal("typing", PyObject.GetAttr(PyObject.TypeOf(type), "__module__"));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [type, PyType.Get("type")], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("issubclass", [type, PyType.Get("dict")], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("issubclass", [type, PyType.Get("object")], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("isinstance", [new PyDict(), type], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("issubclass", [type, type], [], []));
        var instance = Assert.IsType<PyDict>(PyOps.CallValue(type, [], ["extra"], [7L]));
        Assert.Equal(7L, instance.Get("extra"));
        var typing = PyTyping.CreateAttributes();
        Assert.Equal(true, PyOps.CallValue(typing["is_typeddict"], [type], [], []));
        Assert.Equal(false, PyOps.CallValue(typing["is_typeddict"], [PyTypedDict.Factory], [], []));
    }
}
