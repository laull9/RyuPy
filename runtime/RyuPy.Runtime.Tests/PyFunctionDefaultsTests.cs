// 函数默认值反射和真实binder共用活状态，覆盖别名修改、默认值扩张及共享签名隔离。
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyFunctionDefaultsTests
{
    private static PyFunction Function(PySignature signature, object?[] defaults) => new("value", signature,
        (self, args, names, values) => new PyTuple(PyBinder.Bind(signature, args, names, values, self.Defaults)), defaults);

    private static PySignature Signature() => PySignature.Create("value", ["a", "b", "required", "option"],
        ["posonly", "pos", "kwonly", "kwonly"], ["0", "1", "0", "1"]);
    private static PyTuple Call(PyFunction function, object?[] args, string[]? names = null, object?[]? values = null) =>
        Assert.IsType<PyTuple>(function.Invoke(args, names ?? [], values ?? []));

    [Fact]
    public void PublicContainersRetainIdentityAndBindingReadsEveryAliasMutation()
    {
        var function = Function(Signature(), [null, 2L, null, 4L]);
        object first = PyObject.GetAttr(function, "__defaults__")!;
        Assert.Same(first, PyObject.GetAttr(function, "__defaults__"));
        var keywords = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__kwdefaults__"));
        Assert.Same(keywords, PyObject.GetAttr(function, "__kwdefaults__"));
        var shared = new PyList();
        var positional = new PyTuple([shared]);
        PyObject.SetAttr(function, "__defaults__", positional);
        Assert.Same(positional, PyObject.GetAttr(function, "__defaults__"));
        keywords.Set("required", shared);
        PyTuple result = Call(function, [1L]);
        Assert.Same(shared, result.Items[1]); Assert.Same(shared, result.Items[2]);
        keywords.Set("option", 8L);
        Assert.Equal(8L, Call(function, [1L]).Items[3]);
        keywords.Delete("required");
        Assert.Equal("value() missing 1 required keyword-only argument: 'required'",
            Assert.Throws<PyTypeError>(() => Call(function, [1L])).Message);
        PyObject.SetAttr(function, "__defaults__", new PyTuple([99L, 6L, 7L]));
        Assert.Equal(new object?[] { 6L, 7L, 3L, 8L }, Call(function, [], ["required"], [3L]).Items);
    }

    [Fact]
    public void DeletionNoneEmptyAndInvalidWritesKeepDistinctStates()
    {
        var function = Function(Signature(), [null, 2L, null, 4L]);
        var positional = new PyTuple([]); var keywords = new PyDict();
        PyObject.SetAttr(function, "__defaults__", positional);
        PyObject.SetAttr(function, "__kwdefaults__", keywords);
        Assert.Same(positional, PyObject.GetAttr(function, "__defaults__"));
        Assert.Same(keywords, PyObject.GetAttr(function, "__kwdefaults__"));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(function, "__defaults__", new PyList()));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(function, "__kwdefaults__", positional));
        Assert.Same(positional, PyObject.GetAttr(function, "__defaults__"));
        Assert.Same(keywords, PyObject.GetAttr(function, "__kwdefaults__"));
        foreach (string name in new[] { "__defaults__", "__kwdefaults__" })
        {
            PyObject.DelAttr(function, name); PyObject.DelAttr(function, name);
            Assert.Same(PyNone.Instance, PyObject.GetAttr(function, name));
        }
        Assert.Equal("value() missing 2 required positional arguments: 'a' and 'b'",
            Assert.Throws<PyTypeError>(() => Call(function, [])).Message);
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__"));
        attributes.Set("__defaults__", new PyTuple([99L]));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__defaults__"));
        PyObject.SetAttr(function, "__defaults__", PyNone.Instance);
        PyObject.SetAttr(function, "__kwdefaults__", PyNone.Instance);
    }

    [Fact]
    public void SharedSignaturesAndSharedInputArraysHaveIndependentDynamicState()
    {
        PySignature signature = Signature(); object?[] shared = [null, 2L, null, 4L];
        PyFunction first = Function(signature, shared), second = Function(signature, shared);
        PyObject.SetAttr(first, "__defaults__", new PyTuple([6L, 7L]));
        Assert.Equal(7L, Call(first, [], ["required"], [3L]).Items[1]);
        Assert.Equal(2L, Call(second, [1L], ["required"], [3L]).Items[1]);
        Assert.Same(signature, first.Signature); Assert.Same(signature, second.Signature);
        Assert.Equal(new[] { false, true, false, true }, signature.HasDefault);
        var emptySignature = PySignature.Create("empty", [], [], []);
        PyFunction empty1 = Function(emptySignature, []), empty2 = Function(emptySignature, []);
        var tuple = new PyTuple([1L]);
        PyObject.SetAttr(empty1, "__defaults__", tuple);
        Assert.Same(tuple, PyObject.GetAttr(empty1, "__defaults__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(empty2, "__defaults__"));
    }
}
