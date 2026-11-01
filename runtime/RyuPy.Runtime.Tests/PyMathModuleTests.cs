// math 每个首批 API 的数值、参数与 Python 异常边界；不把平台 libm 末位差异当语义错误。
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyMathModuleTests
{
    private static readonly Dictionary<string, object?> Attributes = PyMathModule.CreateAttributes();
    private static object? Call(string name, params object?[] args) => PyOps.CallValue(Attributes[name], args, [], []);
    private static double Float(string name, params object?[] args) => Assert.IsType<double>(Call(name, args));

    [Fact]
    public void ScalarFunctionsAndConstantsUseRealNumbers()
    {
        Assert.Equal(3.0, Float("sqrt", 9L));
        Assert.Equal(2.5, Float("fabs", -2.5));
        Assert.Equal(1.0, Float("exp", 0L));
        Assert.Equal(0.0, Float("sin", 0L));
        Assert.Equal(1.0, Float("cos", 0L));
        Assert.Equal(0.0, Float("tan", 0L));
        Assert.Equal(8.0, Float("pow", 2L, 3L));
        Assert.InRange(Math.Abs(Float("log", 8L, 2L) - 3), 0, 1e-12);
        Assert.Equal(2.0, Float("log10", 100L));
        Assert.Equal(Math.PI, Attributes["pi"]); Assert.Equal(Math.E, Attributes["e"]);
        Assert.Equal(2 * Math.PI, Attributes["tau"]);
        Assert.Equal(true, Call("isfinite", 1L)); Assert.Equal(false, Call("isfinite", double.NaN));
        Assert.Equal(true, Call("isinf", double.NegativeInfinity)); Assert.Equal(false, Call("isinf", double.NaN));
        Assert.Equal(true, Call("isnan", double.NaN)); Assert.Equal(false, Call("isnan", 0L));
    }

    [Fact]
    public void DomainAndRangeErrorsDoNotLeakClrNanOrInfinity()
    {
        foreach (string name in new[] { "sqrt", "log", "log10" })
            Assert.Throws<PyValueError>(() => Call(name, -1.0));
        foreach (string name in new[] { "sin", "cos", "tan" })
            Assert.Throws<PyValueError>(() => Call(name, double.PositiveInfinity));
        Assert.Throws<PyOverflowError>(() => Call("exp", 1000.0));
        Assert.Throws<PyOverflowError>(() => Call("pow", 1e308, 2.0));
        Assert.Throws<PyValueError>(() => Call("pow", -2.0, 0.5));
        Assert.Throws<PyValueError>(() => Call("pow", 0.0, -1.0));
        Assert.Throws<PyValueError>(() => Call("log", 2L, 0L));
        Assert.Throws<PyZeroDivisionError>(() => Call("log", double.NaN, 1L));
        Assert.Throws<PyTypeError>(() => Call("log", 2L, PyNone.Instance));
        Assert.Equal(0.0, Float("exp", -1000.0));
    }

    [Fact]
    public void NanInfinityAndSignedZeroFollowEachApi()
    {
        foreach (string name in new[] { "sqrt", "fabs", "exp", "sin", "cos", "tan", "log", "log10" })
            Assert.True(double.IsNaN(Float(name, double.NaN)));
        Assert.Equal(double.PositiveInfinity, Float("sqrt", double.PositiveInfinity));
        Assert.Equal(double.PositiveInfinity, Float("exp", double.PositiveInfinity));
        Assert.Equal(0.0, Float("exp", double.NegativeInfinity));
        Assert.Equal(1.0, Float("pow", double.NaN, 0L));
        Assert.Equal(1.0, Float("pow", 1L, double.NaN));
        Assert.Equal(double.PositiveInfinity, Float("pow", -0.0, double.NegativeInfinity));
        Assert.Equal(double.PositiveInfinity, Float("pow", double.NegativeInfinity, 0.5));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Float("sqrt", -0.0)));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Float("sin", -0.0)));
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(Float("fabs", -0.0)));
    }

    [Fact]
    public void IntegralFunctionsKeepBigIntegersAndRejectNonFiniteFloats()
    {
        BigInteger large = BigInteger.Pow(10, 1000);
        foreach (string name in new[] { "floor", "ceil", "trunc" })
        {
            Assert.Equal(large, Call(name, large));
            Assert.Equal(1L, Call(name, true));
            Assert.Throws<PyValueError>(() => Call(name, double.NaN));
            Assert.Throws<PyOverflowError>(() => Call(name, double.PositiveInfinity));
            Assert.Equal(new BigInteger(1e100), Call(name, 1e100));
        }
        Assert.Equal(-2L, Call("floor", -1.2)); Assert.Equal(-1L, Call("ceil", -1.2));
        Assert.Equal(-1L, Call("trunc", -1.2));
        Assert.InRange(Math.Abs(Float("log10", large) - 1000), 0, 1e-10);
        Assert.InRange(Math.Abs(Float("log", large) - 1000 * Math.Log(10)), 0, 1e-10);
        foreach (string name in new[] { "sqrt", "fabs", "isfinite", "isinf", "isnan" })
            Assert.Throws<PyOverflowError>(() => Call(name, large));
    }

    [Fact]
    public void NumericProtocolsAreSingleCallStrictAndOrdered()
    {
        int calls = 0;
        var floating = Protocol("__float__", () => { calls++; return 4.0; });
        Assert.Equal(2.0, Float("sqrt", floating)); Assert.Equal(1, calls);
        Assert.Equal(2.0, Float("sqrt", Protocol("__index__", () => 4L)));
        Assert.Equal(2L, Call("floor", Protocol("__float__", () => 2.5)));
        Assert.Equal(3L, Call("ceil", Protocol("__index__", () => 3L)));
        Assert.Throws<PyTypeError>(() => Call("sqrt", Protocol("__float__", () => 4L)));
        Assert.Throws<PyTypeError>(() => Call("sqrt", Protocol("__index__", () => 4.0)));
        Assert.Throws<PyTypeError>(() => Call("trunc", floating));
        foreach (string name in new[] { "floor", "ceil", "trunc" })
            Assert.Equal("custom", Call(name, Protocol("__" + name + "__", () => "custom")));
        Assert.Throws<PyValueError>(() => Call("sqrt", Protocol("__float__", () => throw new PyValueError("user"))));
    }

    [Fact]
    public void AllFunctionsRejectStringsKeywordsAndBadArity()
    {
        foreach (var pair in Attributes)
        {
            if (pair.Value is not PyFunction) continue;
            Assert.Throws<PyTypeError>(() => Call(pair.Key));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(pair.Value, [], ["x"], [1L]));
            Assert.Throws<PyTypeError>(() => Call(pair.Key, 1L, 2L, 3L));
            Assert.Throws<PyTypeError>(() => Call(pair.Key, "2"));
        }
    }

    internal static object Protocol(string name, Func<object?> body)
    {
        var cls = (PyClass)PyOps.MakeClass("Number", [], null);
        cls.SetAttr(name, new PyFunction(name, PySignature.Create(name, ["self"], ["pos"], ["0"]),
            (_, _, _, _) => body(), []));
        return cls.Invoke([], [], [])!;
    }
}
