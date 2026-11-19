using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyFunctionMetadataTests
{
    private static PyFunction Function(string name = "original") => new(name,
        PySignature.Create(name, [], [], []), (_, _, _, _) => PyNone.Instance, []);

    [Fact]
    public void DefinitionMetadataKeepsRealObjectsAndDoesNotCopyToWrapper()
    {
        var function = Function();
        Assert.Equal("original", PyObject.GetAttr(function, "__name__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__doc__"));
        Assert.Equal(0, Assert.IsType<PyDict>(PyObject.GetAttr(function, "__annotations__")).Count);
        object annotation = PyType.Get("int");
        PyOps.FunctionMetadata(function, "documentation", ["value", "return"], [annotation, PyNone.Instance]);
        Assert.Equal("documentation", PyObject.GetAttr(function, "__doc__"));
        var annotations = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__annotations__"));
        Assert.Same(annotation, annotations.Get("value"));
        Assert.Same(PyNone.Instance, annotations.Get("return"));
        var wrapper = Function("wrapper");
        Assert.Same(PyNone.Instance, PyObject.GetAttr(wrapper, "__doc__"));
        Assert.Equal(0, Assert.IsType<PyDict>(PyObject.GetAttr(wrapper, "__annotations__")).Count);
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__qualname__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__module__"));
    }

    [Fact]
    public void FunctionDictionaryIsLiveAndSpecialAttributesStaySeparate()
    {
        var function = Function();
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__"));
        PyObject.SetAttr(function, "label", 42L);
        Assert.Equal(42L, attributes.Get("label"));
        attributes.Set("label", 99L);
        Assert.Equal(99L, PyObject.GetAttr(function, "label"));
        attributes.Set("__name__", "not the descriptor");
        Assert.Equal("original", PyObject.GetAttr(function, "__name__"));
        PyObject.DelAttr(function, "label");
        Assert.False(attributes.TryGet("label", out _));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(function, "label"));
        var replacement = new PyDict();
        PyObject.SetAttr(function, "__dict__", replacement);
        Assert.Same(replacement, PyObject.GetAttr(function, "__dict__"));
        PyObject.SetAttr(function, "callback", PyOps.BuiltinFunction("abs"));
        Assert.Equal(3L, PyOps.CallMethod(function, "callback", [-3L], [], []));
        Assert.Equal(4L, PyOps.CallValue(PyOps.PrepareMethod(function, "callback"), [-4L], [], []));
    }

    [Fact]
    public void MetadataAssignmentAndDeletionUsePythonRules()
    {
        var function = Function();
        PyObject.SetAttr(function, "__name__", "changed");
        Assert.Equal("changed", function.Name);
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(function, "__name__", 42L));
        Assert.Throws<PyTypeError>(() => PyObject.DelAttr(function, "__name__"));
        PyObject.SetAttr(function, "__doc__", 42L);
        Assert.Equal(42L, PyObject.GetAttr(function, "__doc__"));
        PyObject.DelAttr(function, "__doc__");
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__doc__"));
        var annotations = new PyDict();
        PyObject.SetAttr(function, "__annotations__", annotations);
        Assert.Same(annotations, PyObject.GetAttr(function, "__annotations__"));
        PyObject.SetAttr(function, "__annotations__", PyNone.Instance);
        Assert.NotSame(annotations, PyObject.GetAttr(function, "__annotations__"));
        PyObject.DelAttr(function, "__annotations__");
        Assert.Equal(0, Assert.IsType<PyDict>(PyObject.GetAttr(function, "__annotations__")).Count);
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(function, "__annotations__", new PyList()));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(function, "__dict__", PyNone.Instance));
        Assert.Throws<PyTypeError>(() => PyObject.DelAttr(function, "__dict__"));
    }

    [Fact]
    public void BoundMethodsReadThroughWithoutBecomingWritable()
    {
        var function = Function();
        var cls = (PyClass)PyOps.MakeClass("Owner", [], null);
        object instance = cls.Invoke([], [], [])!;
        var bound = new PyBoundMethod(cls, instance, function);
        PyObject.SetAttr(function, "__name__", "renamed");
        PyObject.SetAttr(function, "label", 7L);
        Assert.Equal("renamed", PyObject.GetAttr(bound, "__name__"));
        Assert.Equal(7L, PyObject.GetAttr(bound, "label"));
        Assert.Same(PyObject.GetAttr(function, "__dict__"), PyObject.GetAttr(bound, "__dict__"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(bound, "label", 8L));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(bound, "label"));
        Assert.Equal("function", PyRepr.TypeName(function));
        Assert.Same(PyType.Get("function"), PyObject.TypeOf(function));
    }

    [Fact]
    public void MetadataAbiRejectsMalformedInputsWithoutClrErrors()
    {
        Assert.Throws<PyTypeError>(() => PyOps.FunctionMetadata(42L, null, [], []));
        Assert.Throws<PyRuntimeError>(() => PyOps.FunctionMetadata(Function(), null, ["x"], []));
        Assert.Throws<PyTypeError>(() => PyOps.FunctionMetadata(Function(), null, [42L], [PyType.Get("int")]));
    }
}
