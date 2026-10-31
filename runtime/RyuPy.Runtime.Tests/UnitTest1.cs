using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class FloatFormatTests
{
    [Theory]
    [InlineData(1.0, "1.0")]
    [InlineData(1e16, "1e+16")]
    [InlineData(1.25e16, "1.25e+16")]
    [InlineData(1e-5, "1e-05")]
    [InlineData(123456789.0 * 1000.0, "123456789000.0")]
    [InlineData(-2.5, "-2.5")]
    [InlineData(-0.0, "-0.0")]
    [InlineData(0.0, "0.0")]
    [InlineData(1e-4, "0.0001")]
    [InlineData(1e15, "1000000000000000.0")]
    [InlineData(-1e16, "-1e+16")]
    [InlineData(-1e-5, "-1e-05")]
    [InlineData(-0.0001, "-0.0001")]
    [InlineData(double.PositiveInfinity, "inf")]
    [InlineData(double.NegativeInfinity, "-inf")]
    [InlineData(double.NaN, "nan")]
    public void MatchesPythonRepr(double value, string expected)
    {
        string actual = PyFormat.FloatRepr(value);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumPrecisionRepr()
    {
        string actual = PyFormat.FloatRepr(0.1 + 0.2);
        Assert.Equal("0.30000000000000004", actual);
    }

    [Fact]
    public void SqrtRepr()
    {
        string actual = PyFormat.FloatRepr(Math.Pow(2.0, 0.5));
        Assert.Equal("1.4142135623730951", actual);
    }
}

public class PyMathTests
{
    [Theory]
    [InlineData(3.99, 3)]
    [InlineData(-3.99, -3)]
    [InlineData(0.0, 0)]
    public void F64ToI64TruncatesTowardsZero(double val, long expected)
    {
        Assert.Equal(expected, PyMath.F64ToI64(val));
    }

    [Fact]
    public void F64ToI64HandlesNegativeZero()
    {
        Assert.Equal(0, PyMath.F64ToI64(-0.0));
    }

    [Fact]
    public void F64ToI64ThrowsOnNaN()
    {
        Assert.Throws<InvalidOperationException>(() => PyMath.F64ToI64(double.NaN));
    }

    [Fact]
    public void F64ToI64ThrowsOnInfinity()
    {
        Assert.Throws<OverflowException>(() => PyMath.F64ToI64(double.PositiveInfinity));
        Assert.Throws<OverflowException>(() => PyMath.F64ToI64(double.NegativeInfinity));
    }

    [Fact]
    public void AbsBehaviors()
    {
        Assert.Equal(42L, PyMath.AbsI64(-42));
        Assert.Equal(42L, PyMath.AbsI64(42));
        Assert.Equal(3.14, PyMath.AbsF64(-3.14));
        Assert.Equal(3.14, PyMath.AbsF64(3.14));
    }
}
