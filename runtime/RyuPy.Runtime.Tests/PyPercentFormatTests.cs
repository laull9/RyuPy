using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>有限百分号格式的单值/tuple 区分、协议副作用、ASCII 及明确拒绝边界。</summary>
public class PyPercentFormatTests
{
    [Fact]
    public void StringFormattingPrecedesRightReverseModulo()
    {
        var cls = new PyClass("ReverseModulo");
        cls.SetAttr("__rmod__", PyNativeFunctions.Create("__rmod__", ["self", "other"], [null, null], 2,
            _ => throw new PyAssertionError("右反射不应执行")));
        cls.SetAttr("__str__", PyNativeFunctions.Create("__str__", ["self"], [null], 1, _ => "text"));
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1, _ => 9L));
        var value = new PyInstance(cls);
        Assert.Equal("text", PyOps.Binary(PyOps.Mod, "%s", value));
        Assert.Equal("9", PyOps.Binary(PyOps.Mod, "%d", value));
        Assert.Throws<PyValueError>(() => PyOps.Binary(PyOps.Mod, "%q", value));
        Assert.Throws<PyTypeError>(() => PyOps.Binary(PyOps.Mod, "plain", value));
    }

    [Fact]
    public void BasicConversionsKeepTupleAndSingleArgumentDistinct()
    {
        Assert.Equal("42/-3/-4/%", PyPercentFormat.Format("%d/%i/%u/%%", new PyTuple([42L, -3L, -4L])));
        Assert.Equal("(1, 2)", PyPercentFormat.Format("%s", new PyTuple([new PyTuple([1L, 2L])])));
        Assert.Equal("1/3/-3", PyPercentFormat.Format("%d/%i/%u", new PyTuple([true, 3.9, -3.9])));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%d", "12"));
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%d", double.NaN));
        Assert.Throws<PyOverflowError>(() => PyPercentFormat.Format("%d", double.PositiveInfinity));
    }

    [Fact]
    public void ConversionSideEffectsPrecedeLaterArgumentErrors()
    {
        int calls = 0;
        var cls = new PyClass("PercentValue");
        cls.SetAttr("__str__", PyNativeFunctions.Create("__str__", ["self"], [null], 1, _ => { calls++; return "ok"; }));
        var value = new PyInstance(cls);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%s/%s", new PyTuple([value])));
        Assert.Equal(1, calls);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%s", new PyTuple([value, 1L])));
        Assert.Equal(2, calls);
        Assert.Equal("ok", PyPercentFormat.Format("%s", value));
        Assert.Equal(3, calls);
    }

    [Fact]
    public void IntegerProtocolDoesNotParseTextAndValidatesReturnValue()
    {
        var cls = new PyClass("IntegerValue");
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1, _ => 8L));
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => throw new PyValueError("wrong order")));
        var value = new PyInstance(cls);
        Assert.Equal("8", PyPercentFormat.Format("%d", value));
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1, _ => "12"));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%d", value));
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1, _ => true));
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format("%d", value));
    }

    [Fact]
    public void AsciiUsesOneReprAndEscapesCodePoints()
    {
        int calls = 0;
        var cls = new PyClass("ReprValue");
        cls.SetAttr("__repr__", PyNativeFunctions.Create("__repr__", ["self"], [null], 1, _ => { calls++; return "é中😀"; }));
        Assert.Equal("\\xe9\\u4e2d\\U0001f600", PyPercentFormat.Format("%a", new PyInstance(cls)));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void UnsupportedSpecificationsFailInsteadOfSilentlyApproximating()
    {
        foreach (string format in new[] { "%5s", "%.*s", "%+d", "%f" })
            Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format(format, 1L));
        Assert.Equal("%", PyPercentFormat.Format("%%", new PyDict()));
        Assert.Equal("plain", PyPercentFormat.Format("plain", new PyList([1L])));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%%", 1L));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%q", new PyTuple([])));
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%q", 1L));
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%", new PyTuple([])));
        var cls = new PyClass("MappingValue");
        cls.SetAttr("__getitem__", PyNativeFunctions.Create("__getitem__", ["self", "key"], [null, null], 2, _ => 1L));
        Assert.Equal("plain", PyPercentFormat.Format("plain", new PyInstance(cls)));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%(key)s", 1L));
    }
}
