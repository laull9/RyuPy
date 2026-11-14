// 十进制百分号 flags/width 的值、ASCII语法和逐项取值/转换合同；不扩其它specifier。
using System;
using System.Collections.Generic;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyPercentDecimalWidthTests
{
    private static PyFunction Function(string name, string[] names, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, new object?[names.Length], names.Length, body);

    [Fact]
    public void FlagsAndWidthApplyToAllThreeDecimalConversions()
    {
        foreach (char conversion in "diu")
        {
            foreach (var (specification, positive, negative, zero) in new[]
            {
                ("6", "    12", "   -12", "     0"),
                ("06", "000012", "-00012", "000000"),
                ("+06", "+00012", "-00012", "+00000"),
                (" 06", " 00012", "-00012", " 00000"),
                ("-06", "12    ", "-12   ", "0     "),
                ("0-6", "12    ", "-12   ", "0     "),
                ("+ 6", "   +12", "   -12", "    +0"),
                (" +6", "   +12", "   -12", "    +0"),
                ("#06", "000012", "-00012", "000000"),
                ("00++##--  6", "+12   ", "-12   ", "+0    "),
                ("1", "12", "-12", "0"),
                ("0", "12", "-12", "0"),
                ("0000000000000000000000004", "0012", "-012", "0000"),
            })
            {
                string format = "%" + specification + conversion;
                Assert.Equal(positive, PyPercentFormat.Format(format, 12L));
                Assert.Equal(negative, PyPercentFormat.Format(format, -12L));
                Assert.Equal(zero, PyPercentFormat.Format(format, 0L));
            }
        }
        Assert.Equal(new string(' ', 1023) + "1", PyPercentFormat.Format("%1024d", 1L));
    }

    [Fact]
    public void ExistingNumericValuesAndUnicodeLiteralsArePreserved()
    {
        Assert.Equal("+001", PyPercentFormat.Format("%+04d", true));
        Assert.Equal("+000", PyPercentFormat.Format("%+04i", -0.0));
        Assert.Equal("-003", PyPercentFormat.Format("%04u", -3.9));
        Assert.Equal("-09223372036854775808", PyPercentFormat.Format("%021d", long.MinValue));
        Assert.Equal("-000000001267650600228229401496703205376",
            PyPercentFormat.Format("%+040d", -(BigInteger.One << 100)));
        Assert.Equal("😀-012中%/é", PyPercentFormat.Format("😀%04d中%%/%s", new PyTuple([-12L, "é"])));
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%04d", double.NaN));
        Assert.Throws<PyOverflowError>(() => PyPercentFormat.Format("%04d", double.PositiveInfinity));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%04d", "12"));
    }

    [Fact]
    public void CurrentFieldSyntaxAndMissingArgumentsKeepErrorOrder()
    {
        foreach (string format in new[] { "%6", "%+", "%04" })
            Assert.Equal("incomplete format", Assert.Throws<PyValueError>(() =>
                PyPercentFormat.Format(format, new PyTuple([]))).Message);
        foreach (string format in new[] { "%4q", "%4-d", "%4+d", "%4 d", "%4#d", "%0%", "%٤d", "%０4d" })
        {
            Assert.Throws<PyValueError>(() => PyPercentFormat.Format(format, 1L));
            Assert.Equal("not enough arguments for format string", Assert.Throws<PyTypeError>(() =>
                PyPercentFormat.Format(format, new PyTuple([]))).Message);
        }
        Assert.Equal("unsupported format character 'q' (0x71) at index 3", Assert.Throws<PyValueError>(() =>
            PyPercentFormat.Format("😀%4q", 1L)).Message);
        Assert.Equal("unsupported format character '?' (0x1f600) at index 2", Assert.Throws<PyValueError>(() =>
            PyPercentFormat.Format("%4😀", 1L)).Message);
        foreach (string width in new[] { "2147483648", "9223372036854775807" })
            Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%" + width + "d", new PyTuple([])));
        Assert.Equal("width too big", Assert.Throws<PyValueError>(() =>
            PyPercentFormat.Format("%9223372036854775808d", new PyTuple([]))).Message);
    }

    [Fact]
    public void IntegerProtocolsRunOnceAndDoNotFallbackAfterBadReturn()
    {
        var events = new List<string>();
        var cls = new PyClass("Number");
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => { events.Add("int"); return -12L; }));
        cls.SetAttr("__index__", Function("__index__", ["self"], _ => { events.Add("index"); return 8L; }));
        var value = new PyInstance(cls);
        Assert.Equal("-012", PyPercentFormat.Format("%04d", value));
        Assert.Equal(new[] { "int" }, events);
        events.Clear();
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => { events.Add("bad"); return "12"; }));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%04d", value));
        Assert.Equal(new[] { "bad" }, events);
        var index = new PyClass("IndexOnly");
        index.SetAttr("__index__", Function("__index__", ["self"], _ => { events.Add("index"); return 8L; }));
        Assert.Equal("0008", PyPercentFormat.Format("%04i", new PyInstance(index)));
        Assert.Equal(new[] { "bad", "index" }, events);
        var original = new PyValueError("original");
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => throw original));
        Assert.Same(original, Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%04d", value)));
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => throw new PyTypeError("original type")));
        Assert.Contains("real number is required", Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%04d", value)).Message);
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => true));
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format("%04d", value));
    }

    [Fact]
    public void MappingKeysPrecedeSpecificationAndReloadAfterConversion()
    {
        var events = new List<string>();
        var number = new PyClass("Number");
        long next = 1;
        number.SetAttr("__int__", Function("__int__", ["self"], _ => { events.Add("int"); next = 9; return 12L; }));
        var mapping = new PyClass("Mapping");
        mapping.SetAttr("__getitem__", Function("__getitem__", ["self", "key"], args =>
        {
            string key = Assert.IsType<string>(args[1]); events.Add(key);
            if (key == "missing") throw new PyKeyError(key);
            return key == "first" ? new PyInstance(number) : next;
        }));
        var value = new PyInstance(mapping);
        Assert.Equal("0012/0009/0009", PyPercentFormat.Format("%(first)04d/%(next)04i/%(next)04u", value));
        Assert.Equal(new[] { "first", "int", "next", "next" }, events);
        events.Clear();
        Assert.Throws<PyKeyError>(() => PyPercentFormat.Format("%(missing)9223372036854775808d", value));
        Assert.Equal(new[] { "missing" }, events);
        events.Clear();
        Assert.Equal("width too big", Assert.Throws<PyValueError>(() =>
            PyPercentFormat.Format("%(first)9223372036854775808d", value)).Message);
        Assert.Equal(new[] { "first" }, events);
        events.Clear();
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%(first)04", value));
        Assert.Equal(new[] { "first" }, events);
        Assert.Equal("0009/0009", PyPercentFormat.Format("%((nested))04d/%()04d", value));
    }

    [Fact]
    public void LaterErrorsDoNotEraseOrRepeatEarlierConversions()
    {
        int calls = 0;
        var cls = new PyClass("Number");
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => { calls++; return 12L; }));
        var value = new PyInstance(cls);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%04d/%04d", new PyTuple([value])));
        Assert.Equal(1, calls);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%04d", new PyTuple([value, 1L])));
        Assert.Equal(2, calls);
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%04d/%", new PyTuple([value])));
        Assert.Equal(3, calls);
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format("%04d/%.*d", new PyTuple([value, 1L, 2L])));
        Assert.Equal(4, calls);
    }

    [Fact]
    public void UnsupportedDomainsRemainExplicitWithoutConvertingValues()
    {
        var cls = new PyClass("Untouched");
        cls.SetAttr("__int__", Function("__int__", ["self"], _ => throw new PyAssertionError("不能转换")));
        foreach (string format in new[] { "%5s", "%0s", "%04r", "%04a", "%.3d", "%04.3d", "%*d", "%.*d", "%04x", "%04f", "%04c", "%04ld" })
            Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format(format, new PyInstance(cls)));
    }
}
