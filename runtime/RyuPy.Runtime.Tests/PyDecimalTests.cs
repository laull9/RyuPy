// 有限Decimal真实native协议及共享数值hash；CP3.12原件见独立Decimal合同，不测试占位类型。
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyDecimalTests
{
    private static PyDecimal D(object value) => Assert.IsType<PyDecimal>(PyDecimal.Type.Invoke([value], [], []));

    [Theory]
    [InlineData("1.2300", "1.2300")]
    [InlineData("-0.00", "-0.00")]
    [InlineData("1E-6", "0.000001")]
    [InlineData("1E-7", "1E-7")]
    [InlineData("1E+3", "1E+3")]
    [InlineData("_1_", "1")]
    [InlineData("1__2", "12")]
    [InlineData("\u00a0１２.٣０\u00a0", "12.30")]
    [InlineData("𝟙.𝟚", "1.2")]
    [InlineData("123456789012345678901234567890.123456789", "123456789012345678901234567890.123456789")]
    public void ExactConstructionRepresentationAndType(string input, string expected)
    {
        var value = D(input);
        Assert.Equal(expected, PyRepr.Str(value));
        Assert.Equal("Decimal('" + expected + "')", PyRepr.Repr(value));
        Assert.Equal(expected, PyBuiltinFormat.Format(value, "", ""));
        Assert.Same(PyDecimal.Type, PyObject.TypeOf(value));
        Assert.True(PyDecimal.Type.Accepts(value));
        Assert.Same(value, D(value));
    }

    [Fact]
    public void HugeExponentsDoNotExpandAndUseExactModuloHash()
    {
        var high = D("1E+1000000000"); var low = D("1E-1000000000");
        Assert.Equal("1E+1000000000", PyRepr.Str(high));
        Assert.Equal("1E-1000000000", PyRepr.Str(low));
        Assert.True(PyOps.Compare(PyOps.Gt, high, BigInteger.Pow(10, 100)));
        Assert.True(PyOps.Compare(PyOps.Gt, low, 0L));
        Assert.True(PyOps.Compare(PyOps.Lt, low, D("2E-1000000000")));
        Assert.Equal(661415105712233318L, PyHash.Of(high));
        Assert.Equal(614850342967879236L, PyHash.Of(low));
        foreach (string input in new[] { "1E+999999999999999999", "1E-1999999999999999997", "0E+999999999999999999" })
            Assert.Equal(input, PyRepr.Str(D(input)));
    }

    [Fact]
    public void EqualIntegerBoolAndFloatKeysShareOneValueHash()
    {
        object[][] groups = [ [D("1.00"), 1L, 1.0, true], [D("-0.00"), 0L, -0.0, false],
            [D("0.5"), 0.5], [D("-0.5"), -0.5],
            [D("0.1000000000000000055511151231257827021181583404541015625"), 0.1] ];
        foreach (var group in groups)
        {
            var dict = new PyDict(); var set = new PySet();
            for (int i = 0; i < group.Length; i++)
            {
                Assert.True(PyHash.Equal(group[0], group[i]));
                Assert.True(PyHash.Equal(group[i], group[0]));
                Assert.Equal(PyHash.Of(group[0]), PyHash.Of(group[i]));
                dict.Set(group[i], (long)i); set.Add(group[i]);
            }
            Assert.Equal(1L, dict.Count); Assert.Single(set.Items);
            foreach (var item in group) Assert.Equal((long)group.Length - 1, dict.Get(item));
        }
        Assert.False(PyHash.Equal(D("0.1"), 0.1));
        Assert.True(PyOps.Compare(PyOps.Lt, D("0.1"), 0.1));
        Assert.True(PyOps.Compare(PyOps.Gt, 0.1, D("0.1")));
        Assert.True(PyOps.Compare(PyOps.Gt, D("9007199254740993"), 9007199254740992.0));
        Assert.False(PyOps.Truthy(D("-0.00"))); Assert.True(PyOps.Truthy(D("1E-1000000000")));
        Assert.Equal("1", PyRepr.Str(D(true))); Assert.Equal("0", PyRepr.Str(D(false)));
    }

    [Fact]
    public void IntegerHashModulusAndSentinelRemainShared()
    {
        BigInteger p = (BigInteger.One << 61) - 1;
        foreach (BigInteger integer in new[] { p, p*p, p+1, -(p+1), BigInteger.Pow(10, 80) })
        {
            var value = D(integer);
            Assert.Equal(PyHash.Of(integer), PyHash.Of(value));
            Assert.True(PyHash.Equal(value, integer));
        }
        Assert.Equal(-2L, PyHash.Of(D(-(p+1))));
    }

    [Theory]
    [InlineData(0.5, 1152921504606846976L)]
    [InlineData(-0.5, -1152921504606846976L)]
    [InlineData(0.1, 230584300921369408L)]
    [InlineData(-0.1, -230584300921369408L)]
    [InlineData(1.5, 1152921504606846977L)]
    [InlineData(1e100, 1822893315824342674L)]
    [InlineData(5e-324, 16777216L)]
    [InlineData(-5e-324, -16777216L)]
    public void FiniteFloatHashMatchesExactCpRational(double value, long expected) => Assert.Equal(expected, PyHash.Of(value));

    [Fact]
    public void FloatSpecialDomainIsUnchangedAndDecimalNanOrderingIsExplicit()
    {
        object nan = double.NaN;
        Assert.False(PyHash.Equal(nan, nan)); Assert.Equal(0L, PyHash.Of(nan));
        Assert.Equal(314159L, PyHash.Of(double.PositiveInfinity));
        Assert.Equal(-314159L, PyHash.Of(double.NegativeInfinity));
        var dict = new PyDict(); dict.Set(nan, 7L); Assert.Equal(7L, dict.Get(nan));
        Assert.True(PyOps.Compare(PyOps.Lt, D("1E+1000000000"), double.PositiveInfinity));
        Assert.False(PyHash.Equal(D("1"), nan));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Lt, D("1"), nan));
    }

    [Fact]
    public void ContextArithmeticParsingSignalsAndConversionsAreExplicitBoundaries()
    {
        foreach (string input in new[] { "NaN", "sNaN", "+Inf", "", ".", "1e", "1\0", "1e9223372036854775807", "1e-9223372036854775808", "12E+999999999999999999" })
            Assert.Throws<PyNotImplementedError>(() => D(input));
        Assert.Throws<PyNotImplementedError>(() => D(0.1));
        var value = D("1.25");
        Assert.Throws<PyNotImplementedError>(() => PyOps.Binary(PyOps.Add, value, 1L));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Binary(PyOps.Add, 1L, value));
        Assert.Throws<PyNotImplementedError>(() => PyBuiltinFormat.Format(value, ".1f", ".1f"));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(value.GetAttr("__float__"), [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyDecimal.Type.Invoke(["1", PyNone.Instance], [], []));
    }
}
