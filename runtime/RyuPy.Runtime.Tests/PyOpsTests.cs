using System;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>动态运算内核的差分测试：期望值均以 CPython 3.12 的实际行为为准。</summary>
public class PyOpsTests
{
    private static object I(long value) => value;

    private static object F(double value) => value;

    [Fact]
    public void IntArithmeticStaysLongUntilOverflow()
    {
        object result = PyOps.Binary(PyOps.Add, I(long.MaxValue), I(1));
        Assert.IsType<BigInteger>(result);
        Assert.Equal((BigInteger)long.MaxValue + 1, result);

        object small = PyOps.Binary(PyOps.Mul, I(3), I(4));
        Assert.IsType<long>(small);
        Assert.Equal(12L, small);
    }

    [Fact]
    public void BigIntPowAndShift()
    {
        object power = PyOps.Binary(PyOps.Pow, I(10), I(40));
        Assert.Equal(BigInteger.Pow(10, 40), power);

        object shifted = PyOps.Binary(PyOps.RShift, PyOps.Binary(PyOps.LShift, I(1), I(100)), I(70));
        Assert.Equal(1L << 30, shifted);
    }

    [Fact]
    public void NegativeFloorDivAndModuloFollowDivisorSign()
    {
        Assert.Equal(-4L, PyOps.Binary(PyOps.FloorDiv, I(-7), I(2)));
        Assert.Equal(2L, PyOps.Binary(PyOps.Mod, I(-7), I(3)));
        Assert.Equal(0L, PyOps.Binary(PyOps.Mod, I(-9223372036854775807L), I(-1)));

        object wide = PyOps.Binary(PyOps.FloorDiv, BigInteger.Parse("-1000000000000000000000000000000"), I(7));
        Assert.Equal(BigInteger.Parse("-142857142857142857142857142858"), wide);
    }

    [Fact]
    public void TrueDivisionAlwaysFloatAndRejectsZero()
    {
        Assert.Equal(3.5, PyOps.Binary(PyOps.TrueDiv, I(7), I(2)));
        var error = Assert.Throws<PyZeroDivisionError>(() => PyOps.Binary(PyOps.TrueDiv, I(1), I(0)));
        Assert.Equal("division by zero", error.PyMessage);
        error = Assert.Throws<PyZeroDivisionError>(() => PyOps.Binary(PyOps.TrueDiv, F(1.0), F(0.0)));
        Assert.Equal("float division by zero", error.PyMessage);
    }

    [Fact]
    public void BoolBehavesAsIntInArithmetic()
    {
        Assert.Equal(3L, PyOps.Binary(PyOps.Add, true, I(2)));
        Assert.True(PyOps.Compare(PyOps.Eq, true, I(1)));
        Assert.False(PyOps.Compare(PyOps.Eq, true, F(1.5)));
    }

    [Fact]
    public void IntFloatComparisonIsExact()
    {
        // 2^53 + 1 无法用 double 表示，CPython 判定不相等
        var big = BigInteger.Parse("9007199254740993");
        Assert.False(PyOps.Compare(PyOps.Eq, big, F(9007199254740992.0)));
        Assert.True(PyOps.Compare(PyOps.Gt, big, F(9007199254740992.0)));
        Assert.True(PyOps.Compare(PyOps.Eq, I(2), F(2.0)));
        Assert.True(PyOps.Compare(PyOps.Lt, I(-3), F(-2.5)));
    }

    [Fact]
    public void NanComparisonsMirrorPython()
    {
        object nan = F(double.NaN);
        Assert.False(PyOps.Compare(PyOps.Eq, nan, nan));
        Assert.True(PyOps.Compare(PyOps.Ne, nan, nan));
        Assert.False(PyOps.Compare(PyOps.Lt, nan, I(1)));
        Assert.False(PyOps.Compare(PyOps.Le, nan, I(1)));
        Assert.False(PyOps.Compare(PyOps.Gt, nan, I(1)));
    }

    [Fact]
    public void OrderingBetweenIncomparableTypesRaises()
    {
        var error = Assert.Throws<PyTypeError>(() => PyOps.Compare(PyOps.Lt, I(1), "a"));
        Assert.Contains("'<' not supported between instances of 'int' and 'str'", error.PyMessage);
    }

    [Fact]
    public void ArithmeticTypeErrorsMatchPythonWording()
    {
        var error = Assert.Throws<PyTypeError>(() => PyOps.Binary(PyOps.Add, I(1), "a"));
        Assert.Equal("unsupported operand type(s) for +: 'int' and 'str'", error.PyMessage);
    }

    [Theory]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    [InlineData(-1L, true)]
    public void IntTruthiness(long value, bool expected)
    {
        Assert.Equal(expected, PyOps.Truthy(value));
    }

    [Fact]
    public void TruthinessOfNoneBoolAndString()
    {
        Assert.False(PyOps.Truthy(PyNone.Instance));
        Assert.False(PyOps.Truthy(false));
        Assert.True(PyOps.Truthy(true));
        Assert.False(PyOps.Truthy(""));
        Assert.True(PyOps.Truthy("x"));
        Assert.False(PyOps.Truthy(0.0));
        Assert.True(PyOps.Truthy(double.NaN));
    }

    [Fact]
    public void IntConversionCoversStringsAndErrors()
    {
        Assert.Equal(BigInteger.Parse("123456789012345678901234567890"),
            PyOps.ToIntValue("123456789012345678901234567890"));
        Assert.Equal(7L, PyOps.ToIntValue(F(7.9)));
        Assert.Equal(-7L, PyOps.ToIntValue(F(-7.9)));
        Assert.Throws<PyValueError>(() => PyOps.ToIntValue(double.NaN));
        Assert.Throws<PyOverflowError>(() => PyOps.ToIntValue(double.PositiveInfinity));
        Assert.Throws<PyValueError>(() => PyOps.ToIntValue("12a"));
    }

    [Fact]
    public void AbsAndNegateWorkForBothNumberKinds()
    {
        Assert.Equal(5L, PyOps.AbsValue(I(-5)));
        Assert.Equal(5.5, PyOps.AbsValue(F(-5.5)));
        // -(-2^63) 超出 i64，必须升级而不是溢出
        Assert.Equal(
            (BigInteger)long.MaxValue + 1,
            PyOps.Unary(PyOps.Neg, long.MinValue)
        );
    }

    [Fact]
    public void ReprMatchesPythonForCommonValues()
    {
        Assert.Equal("None", PyRepr.Str(PyNone.Instance));
        Assert.Equal("True", PyRepr.Str(true));
        Assert.Equal("1.0", PyRepr.Str(1.0));
        Assert.Equal("'a\\nb'", PyRepr.Repr("a\nb"));
        Assert.Equal("\"it's\"", PyRepr.Repr("it's"));
        Assert.Equal("'both \\' and \"'", PyRepr.Repr("both ' and \""));
    }

    [Fact]
    public void UnboxingReportsPythonTypes()
    {
        Assert.Equal(3L, PyCast.ToInt64(I(3)));
        Assert.Equal(1L, PyCast.ToInt64(true));
        var error = Assert.Throws<PyTypeError>(() => PyCast.ToInt64("3"));
        Assert.Equal("argument must be 'int', got 'str'", error.PyMessage);
        Assert.Throws<PyOverflowError>(() => PyCast.ToInt64(BigInteger.Pow(2, 200)));
    }
}
