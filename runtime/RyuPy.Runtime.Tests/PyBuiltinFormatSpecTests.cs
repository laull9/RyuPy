// 严格规格与有界后端：非法ValueError、合法未实现NI；UTF-16不能改变Python码点文本语义。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBuiltinFormatSpecTests
{
    private static object? Format(object? value, object? spec) => PyOps.CallBuiltin("format", [value, spec], [], []);

    [Theory]
    [InlineData("", "😀x")]
    [InlineData("5", "😀x   ")]
    [InlineData("05", "😀x000")]
    [InlineData(".1s", "😀")]
    [InlineData("５s", "😀x   ")]
    [InlineData(".１s", "😀")]
    [InlineData("😀^5", "😀😀x😀😀")]
    [InlineData("^5", " 😀x  ")]
    public void TextUsesCodepointsAndDefaultLeftAlignment(string spec, string expected) =>
        Assert.Equal(expected, Format("😀x", spec));

    [Fact]
    public void LoneSurrogatesArePreservedAndUnchangedSubclassesKeepIdentity()
    {
        Assert.Equal("\ud800", Format("\ud800x", ".1s"));
        Assert.Equal("\ud800😀x\ud800\ud800", Format("😀x", "\ud800^5s"));
        var cls = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls); object value = cls.Invoke(["raw"], [], [])!;
        int calls = 0; cls.SetAttr("__str__", PyNativeFunctions.CreateVariadic("__str__", _ => { calls++; return "OVERRIDE"; }));
        Assert.Equal("OVERRIDE", Format(value, "")); Assert.Equal(1, calls);
        // 这是候选实现契约；相应CP is断言待driver执行，不冒充已有参考测过identity。
        Assert.Same(value, Format(value, "s")); Assert.Same(value, Format(value, ".9s"));
        Assert.Equal("  raw", Format(value, ">5s")); Assert.Equal(1, calls);
        object spec = cls.Invoke([">5s"], [], [])!;
        Assert.Equal("  raw", Format("raw", spec)); Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("", "-42")]
    [InlineData("08d", "-0000042")]
    [InlineData("#08x", "-0x0002a")]
    [InlineData("0=8x", "-000002a")]
    [InlineData("*<08x", "-2a*****")]
    [InlineData("+8", "     -42")]
    [InlineData("#b", "-0b101010")]
    public void IntegerFormattingIsExact(string spec, string expected) => Assert.Equal(expected, Format(-42L, spec));

    [Fact]
    public void BigIntegerAndBoolUseCorrectNumericDomain()
    {
        Assert.Equal("100000000000000000000", Format(BigInteger.One << 80, "x"));
        Assert.Equal("True", Format(true, "")); Assert.Equal("    1", Format(true, "5"));
        Assert.Equal("00001", Format(true, "05")); Assert.Equal("+1", Format(true, "+"));
        Assert.Equal("Unknown format code 's' for object of type 'bool'", Assert.Throws<PyValueError>(() => Format(true, "s")).Message);
    }

    [Fact]
    public void IllegalTypeRulesPrecedeUnimplementedFeatureRejection()
    {
        foreach (string spec in new[] { ".2d", "z", ",x", "+c", "#c", "q" })
            Assert.Throws<PyValueError>(() => Format(42L, spec));
        foreach (string spec in new[] { ".2f", "c", "n", ",d", "_x" })
            Assert.Throws<PyNotImplementedError>(() => Format(42L, spec));
        foreach (string spec in new[] { "+5", "#5", "=5", ",s", "_s", "zs", "d" })
            Assert.Throws<PyValueError>(() => Format("abc", spec));
        foreach (double value in new[] { 1.0, double.NaN, double.PositiveInfinity })
        {
            Assert.Throws<PyValueError>(() => Format(value, "x"));
            Assert.Equal("precision too big", Assert.Throws<PyValueError>(() => Format(value, ".2147483648g")).Message);
            Assert.Throws<PyNotImplementedError>(() => Format(value, "f"));
        }
        Assert.Equal("1.0", Format(1.0, "")); Assert.Equal("-0.0", Format(-0.0, ""));
    }

    [Fact]
    public void DecimalDigitPolicyDoesNotLimitPowerOfTwoPresentations()
    {
        BigInteger boundary = BigInteger.Pow(10, 4300);
        Assert.Equal(4300, Assert.IsType<string>(Format(boundary - 1, "d")).Length);
        Assert.Throws<PyValueError>(() => Format(boundary, "d"));
        Assert.Throws<PyValueError>(() => Format(-boundary, ""));
        Assert.NotEmpty(Assert.IsType<string>(Format(boundary, "x")));
        Assert.NotEmpty(Assert.IsType<string>(Format(BigInteger.One << 20000, "b")));
    }

    [Fact]
    public void WidthLimitsAreExplicitAndPrecisionDoesNotAllocateByRequestedLength()
    {
        Assert.Throws<PyNotImplementedError>(() => Format("x", "1073741792s"));
        Assert.Throws<PyNotImplementedError>(() => Format(1L, "1073741792d"));
        Assert.Throws<PyNotImplementedError>(() => Format("x", "😀>536870897s"));
        Assert.Equal("Too many decimal digits in format string", Assert.Throws<PyValueError>(() => Format("x", "9223372036854775808s")).Message);
        Assert.Equal("x", Format("x", ".9223372036854775807s"));
        Assert.Throws<PyValueError>(() => Format("x", ".s"));
        Assert.Throws<PyValueError>(() => Format("x", "12ss"));
    }
}
