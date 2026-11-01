using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PySystemTests
{
    [Fact]
    public void StandardReaderUsesCodePointsAndUniversalNewlines()
    {
        var stream = new PyStandardStream("<stdin>", new StringReader("😀\r\nend\r"), null);
        Assert.Equal("😀", Call(stream, "read", 1L));
        Assert.Equal("\n", Call(stream, "readline"));
        Assert.Equal("end\n", Call(stream, "read"));
        Call(stream, "close");
        Assert.Throws<PyValueError>(() => PyIter.Create(stream));
    }

    [Fact]
    public void SystemExitRetainsObjectWithoutFormattingAtConstruction()
    {
        var cls = new PyClass("Code");
        cls.SetAttr("__str__", PyNativeFunctions.Create("__str__", ["self"], [null], 1,
            _ => throw new PyValueError("formatted")));
        var value = new PyInstance(cls);
        var exception = Assert.IsType<PySystemExit>(PyErrors.Make("SystemExit", value));
        Assert.Same(value, exception.Code);
        Assert.Same(value, PyObject.GetAttr(exception, "code"));
        Assert.Throws<PyValueError>(() => PyRepr.Str(exception));
    }

    [Fact]
    public void NativeFunctionsRejectExcessAndUnexpectedKeywordArguments()
    {
        var function = PyNativeFunctions.Create("f", ["first", "second"], [null, 2L], 1,
            values => new PyTuple(values));
        Assert.Equal("(1, 2)", PyRepr.Repr(function.Invoke([1L], [], [])));
        Assert.Throws<PyTypeError>(() => function.Invoke([1L, 2L, 3L], [], []));
        Assert.Throws<PyTypeError>(() => function.Invoke([1L], ["unknown"], [4L]));
    }

    [Fact]
    public void SplitLinesHandlesUnicodeAndFinalSeparators()
    {
        Assert.Equal("['a', 'b', 'c', 'd']", PyRepr.Repr(PyOps.CallMethod("a\r\nb\u0085c\u2028d\n", "splitlines", [], [], [])));
        Assert.Equal("['a\\r\\n', 'b\\n']", PyRepr.Repr(PyOps.CallMethod("a\r\nb\n", "splitlines", [true], [], [])));
        Assert.Equal("[]", PyRepr.Repr(PyOps.CallMethod("", "splitlines", [], [], [])));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod("x", "splitlines", [true, true], [], []));
    }

    private static object? Call(object stream, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(stream, name), args, [], []);
}
