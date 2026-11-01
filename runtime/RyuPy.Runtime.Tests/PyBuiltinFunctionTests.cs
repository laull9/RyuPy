using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyBuiltinFunctionTests
{
    [Fact]
    public void BuiltinValuesHaveStableIdentityAndReadOnlyNames()
    {
        object value = PyOps.BuiltinFunction("abs");
        Assert.Same(value, PyOps.BuiltinFunction("abs"));
        Assert.NotSame(value, PyOps.BuiltinFunction("len"));
        Assert.Equal("abs", PyObject.GetAttr(value, "__name__"));
        Assert.Equal("builtin_function_or_method", PyRepr.TypeName(value));
        Assert.Equal("<built-in function abs>", PyRepr.Repr(value));
        Assert.Equal(true, PyOps.CallBuiltin("callable", [value], [], []));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, "__name__", "renamed"));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(value, "__name__"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "__dict__"));
    }

    [Fact]
    public void BuiltinInvocationUsesExistingArgumentValidationAndPicFallback()
    {
        object abs = PyOps.BuiltinFunction("abs");
        Assert.Equal(3L, PyOps.CallValue(abs, [-3L], [], []));
        Assert.Equal(4L, PyPic.CallValue(123, abs, [-4L], [], []));
        Assert.Equal(1L, PyOps.CallValue(PyOps.BuiltinFunction("len"), [new PyTuple([42L])], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(abs, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(abs, [], ["x"], [1L]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyOps.BuiltinFunction("print"), [], ["bad"], [1L]));
    }
}
