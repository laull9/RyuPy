// 显式g/G有限数值域：既有CP参考加边界候选；真正CP-driver验证由父任务唯一执行。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBuiltinFormatFloatTests
{
    private static string Format(double value, string spec) =>
        Assert.IsType<string>(PyOps.CallBuiltin("format", [value, spec], [], []));

    [Theory]
    [InlineData(1.0, "g", "1")]
    [InlineData(1.23456789, "g", "1.23457")]
    [InlineData(1.23456789, ".0g", "1")]
    [InlineData(0.0001, "g", "0.0001")]
    [InlineData(0.00001, "g", "1e-05")]
    [InlineData(999999.5, "g", "1e+06")]
    [InlineData(999999.5, "G", "1E+06")]
    [InlineData(1.25, ".2g", "1.2")]
    [InlineData(1.75, ".2g", "1.8")]
    [InlineData(2.5, ".1g", "2")]
    [InlineData(3.5, ".1g", "4")]
    [InlineData(-1.25, ".2g", "-1.2")]
    [InlineData(-0.0, "g", "-0")]
    [InlineData(-0.0, "+010g", "-000000000")]
    public void DefaultPrecisionRoundingAndScientificThreshold(double value, string spec, string expected) =>
        Assert.Equal(expected, Format(value, spec));

    [Fact]
    public void ExtremesUseTheGeneralBackendWithoutFloatReprFallback()
    {
        Assert.Equal("1.79769e+308", Format(double.MaxValue, "g"));
        Assert.Equal("4.94066e-324", Format(double.Epsilon, "g"));
        Assert.Equal("2.22507e-308", Format(BitConverter.Int64BitsToDouble(0x0010000000000000), "g"));
        Assert.Equal("0.10000000000000001", Format(0.1, ".17g"));
        Assert.Equal("0.100000000000000006", Format(0.1, ".18g"));
        Assert.Equal("0.1000000000000000055511151231257827021181583404541", Format(0.1, ".50g"));
        Assert.Equal("0.1000000000000000055511151231257827021181583404541015625", Format(0.1, ".100g"));
    }

    [Fact]
    public void SpecialValuesUsePythonTextAndIgnoreNaNSignBit()
    {
        Assert.Equal("inf", Format(double.PositiveInfinity, "g"));
        Assert.Equal("-INF", Format(double.NegativeInfinity, "G"));
        Assert.Equal("+000000inf", Format(double.PositiveInfinity, "+010g"));
        Assert.Equal("-000000inf", Format(double.NegativeInfinity, "+010g"));
        foreach (double value in new[] { BitConverter.Int64BitsToDouble(0x7ff8000000000000L),
            BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000000000000UL)) })
        {
            Assert.Equal("nan", Format(value, "g")); Assert.Equal("NAN", Format(value, "G"));
            Assert.Equal("+000000nan", Format(value, "+010g"));
            Assert.Equal(" NAN", Format(value, " G"));
        }
    }

    [Theory]
    [InlineData(1.25, "*=+10g", "+*****1.25")]
    [InlineData(1.25, "*<010g", "1.25******")]
    [InlineData(1.25, "^9g", "  1.25   ")]
    [InlineData(1.25, "😀^7g", "😀1.25😀😀")]
    [InlineData(-1.25, "010g", "-000001.25")]
    public void NumericPaddingRespectsExplicitFillAndCodepointWidth(double value, string spec, string expected) =>
        Assert.Equal(expected, Format(value, spec));

    [Fact]
    public void UnsupportedAndInvalidDomainsRemainDistinctForFiniteAndSpecialValues()
    {
        foreach (double value in new[] { 1.0, double.NaN, double.PositiveInfinity })
        {
            foreach (string spec in new[] { "f", "e", "%", ".2", ">10", "#g", "zg", ",g", "_g", "n" })
                Assert.Throws<PyNotImplementedError>(() => Format(value, spec));
            foreach (string spec in new[] { "x", "s", ",n" })
                Assert.Throws<PyValueError>(() => Format(value, spec));
            Assert.Equal("precision too big", Assert.Throws<PyValueError>(() => Format(value, ".2147483648g")).Message);
        }
        // finite只测真实后端guard；特殊数不调用后端，大precision仍安全输出短字符串。
        Assert.Throws<PyNotImplementedError>(() => Format(1.0, ".1000000000g"));
        Assert.Equal("nan", Format(double.NaN, ".1000000000g"));
        Assert.Equal("+000000NAN", Format(double.NaN, "+010.2147483647G"));
        Assert.Equal("inf", Format(double.PositiveInfinity, ".1000000000g"));
        Assert.Equal("-000000INF", Format(double.NegativeInfinity, "+010.2147483647G"));
        Assert.Equal("0", Format(0.0, ".1000000000g"));
        Assert.Equal("-000000000", Format(-0.0, "+010.2147483647G"));
        Assert.Throws<PyNotImplementedError>(() => Format(1.0, "😀>536870897g"));
    }
}
