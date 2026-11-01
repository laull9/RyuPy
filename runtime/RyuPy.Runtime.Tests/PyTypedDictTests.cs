using Xunit;
namespace RyuPy.Runtime.Tests;

/// <summary>TypedDict元数据、总量规则与真实dict构造与共享类型检查协议分开验收的独立专测。</summary>
public class PyTypedDictTests
{
    private static PyTypedDictType Define(string name, object parent, bool total, params (string, object?)[] fields)
    {
        object shell = PyTypedDict.Begin(name, parent, total, "example");
        var annotations = Assert.IsType<PyDict>(PyObject.GetAttr(shell, "__annotations__"));
        foreach ((string key, object? value) in fields) annotations.Set(key, value);
        return Assert.IsType<PyTypedDictType>(PyTypedDict.Finish(shell));
    }

    [Fact]
    public void ChildTotalOnlyChangesNewOrOverriddenFields()
    {
        var parent = Define("Parent", PyTypedDict.Factory, true, ("x", PyType.Get("int")), ("shared", PyType.Get("str")));
        var child = Define("Child", parent, false, ("shared", PyType.Get("int")), ("y", PyType.Get("str")));
        Assert.Equal(new object?[] { "x", "shared", "y" }, Assert.IsType<PyDict>(child.GetAttr("__annotations__")).Keys);
        Assert.True(Assert.IsType<PyFrozenSet>(child.GetAttr("__required_keys__")).SetEquals(new PyFrozenSet(["x"])));
        Assert.True(Assert.IsType<PyFrozenSet>(child.GetAttr("__optional_keys__")).SetEquals(new PyFrozenSet(["shared", "y"])));
        Assert.Equal(false, child.GetAttr("__total__"));
        Assert.NotSame(parent.GetAttr("__annotations__"), child.GetAttr("__annotations__"));
        Assert.Equal("_TypedDictMeta", child.TypeName);
        Assert.True(PyTypedDict.IsTypedDict(child));
        Assert.False(PyTypedDict.IsTypedDict(PyType.Get("dict")));
    }

    [Fact]
    public void ConstructorIsExactlyDictionaryBindingNotFieldValidation()
    {
        var type = Define("Record", PyTypedDict.Factory, true, ("required", PyType.Get("int")));
        Assert.Equal(0, Assert.IsType<PyDict>(type.Invoke([], [], [])).Count);
        var input = new PyDict(); input.Set("required", "wrong type is accepted");
        var output = Assert.IsType<PyDict>(type.Invoke([input], ["extra"], [7L]));
        Assert.NotSame(input, output);
        Assert.Equal("wrong type is accepted", output.Get("required"));
        Assert.Equal(7L, output.Get("extra"));
        Assert.Throws<PyTypeError>(() => type.Invoke([input, input], [], []));
        Assert.Throws<PyTypeError>(() => PyTypedDictType.RejectRuntimeCheck());
    }

    [Fact]
    public void AnnotationMutationDoesNotRecomputeKeySnapshots()
    {
        var type = Define("Record", PyTypedDict.Factory, true, ("x", PyType.Get("int")));
        Assert.IsType<PyDict>(type.GetAttr("__annotations__")).Set("later", PyType.Get("str"));
        Assert.True(Assert.IsType<PyFrozenSet>(type.GetAttr("__required_keys__")).SetEquals(new PyFrozenSet(["x"])));
        Assert.Throws<PyNotImplementedError>(() => PyTypedDict.Begin("Fake", new PyClass("TypedDict"), true, "example"));
        Assert.Throws<PyNotImplementedError>(() => Define("Forward", PyTypedDict.Factory, true, ("x", "Other")));
        var wrapper = new PyTypingAlias("Required", PyType.Get("int"), [PyType.Get("int")]);
        Assert.Throws<PyNotImplementedError>(() => Define("Qualified", PyTypedDict.Factory, false, ("x", wrapper)));
    }

    [Fact]
    public void RootFactoryUsesRealFunctionBindingAndCopiesFieldMap()
    {
        var fields = new PyDict(); fields.Set("key", PyType.Get("str"));
        var type = Assert.IsType<PyTypedDictType>(PyOps.CallValue(PyTypedDict.Factory, ["Functional", fields], ["total"], [false]));
        fields.Set("later", PyType.Get("int"));
        Assert.Equal(1, Assert.IsType<PyDict>(type.GetAttr("__annotations__")).Count);
        Assert.Equal("Functional", type.GetAttr("__name__"));
        Assert.Equal(false, type.GetAttr("__total__"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyTypedDict.Factory, ["Bad", fields, false], [], []));
    }
}
