// count 数值状态：MoveNext/Current 与公开 next 共用状态，锁定任意精度、bool 身份及逐次浮点加法。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCountStateTests
{
    private static IPyIterator Make(params object?[] args) => Assert.IsAssignableFrom<IPyIterator>(
        PyOps.CallValue(PyItertools.CreateAttributes()["count"], args, [], []));
    private static object? Next(IPyIterator value) => PyOps.CallBuiltin("next", [value], [], []);
    private static object?[] Take(IPyIterator value, int size)
    {
        var result = new object?[size];
        for (int index = 0; index < size; index++) result[index] = Next(value);
        return result;
    }

    [Fact]
    public void CurrentHoldsTheYieldedValueAndInstancesRemainIndependent()
    {
        IPyIterator first = Make(10L, 2L), second = Make(10L, 2L);
        Assert.Null(first.Current()); Assert.True(first.MoveNext());
        Assert.Equal(10L, first.Current()); Assert.Same(first.Current(), first.Current());
        Assert.Equal("count(12, 2)", PyRepr.Repr(first)); Assert.Equal("count(10, 2)", PyRepr.Repr(second));
        Assert.Equal(12L, Next(first)); Assert.Equal(12L, first.Current()); Assert.Equal(10L, Next(second));
    }

    [Fact]
    public void BoolFastModeNormalizesOnlyIntegerUnitSteps()
    {
        foreach ((bool start, object step) in new (bool, object)[] { (true, 1L), (false, true), (true, true) })
        {
            IPyIterator value = Make(start, step);
            Assert.Equal($"count({(start ? 1 : 0)})", PyRepr.Repr(value));
            Assert.Equal(start ? 1L : 0L, Assert.IsType<long>(Next(value)));
            Assert.Equal(start ? 2L : 1L, Assert.IsType<long>(Next(value)));
        }
        Assert.Equal("count(5)", PyRepr.Repr(Make(5L, true)));
        Assert.Equal("count(1.0)", PyRepr.Repr(Make(1.0, true)));
        Assert.Equal("count(1.0, 1.0)", PyRepr.Repr(Make(1.0, 1.0)));
    }

    [Fact]
    public void BoolSlowModeKeepsOriginalFirstObjectAndThenUsesIntegerOrFloatAddition()
    {
        object start = true;
        foreach (object step in new object[] { false, 2L, -1L, 1.0 })
        {
            IPyIterator value = Make(start, step);
            Assert.Same(start, Next(value));
            object? second = Next(value);
            Assert.Equal(PyOps.Binary(PyOps.Add, start, step), second);
            if (step is double) Assert.IsType<double>(second); else Assert.IsType<long>(second);
        }
        Assert.Equal("count(True, False)", PyRepr.Repr(Make(start, false)));
        IPyIterator zero = Make(start, false); Assert.Same(start, Next(zero));
        Assert.Equal("count(1, False)", PyRepr.Repr(zero));
    }

    [Fact]
    public void UnitIncrementCrossesInt64WithoutWrappingOrStopping()
    {
        long upper = long.MaxValue;
        Assert.Equal<object?>([upper - 1, upper, new BigInteger(upper) + 1, new BigInteger(upper) + 2],
            Take(Make(upper - 1), 4));
        object start = upper;
        IPyIterator boundary = Make(start);
        Assert.Equal(upper, Next(boundary)); Assert.NotSame(start, boundary.Current());
        Assert.Equal($"count({new BigInteger(upper) + 1})", PyRepr.Repr(boundary));
        Assert.Equal(new BigInteger(upper) + 1, Next(boundary));
        Assert.Equal<object?>([long.MinValue, long.MinValue + 1, long.MinValue + 2], Take(Make(long.MinValue), 3));
    }

    [Fact]
    public void SignedLargeAndZeroStepsKeepArbitraryPrecisionAndCanReturnToInt64()
    {
        BigInteger upper = new(long.MaxValue), lower = new(long.MinValue), huge = BigInteger.Pow(10, 80);
        Assert.Equal<object?>([long.MaxValue - 1, upper + 2, upper + 5], Take(Make(long.MaxValue - 1, 3L), 3));
        Assert.Equal<object?>([long.MinValue, lower - 2, lower - 4], Take(Make(long.MinValue, -2L), 3));
        Assert.Equal<object?>([upper + 3, upper + 1, long.MaxValue - 1, long.MaxValue - 3], Take(Make(upper + 3, -2L), 4));
        BigInteger step = BigInteger.Pow(10, 70);
        Assert.Equal<object?>([huge, huge + step, huge + 2 * step], Take(Make(huge, step), 3));
        Assert.Equal<object?>([-huge, -huge - step, -huge - 2 * step], Take(Make(-huge, -step), 3));
        Assert.Equal<object?>([3L, huge + 3, 2 * huge + 3], Take(Make(3L, huge), 3));
        object original = huge; IPyIterator zero = Make(original, 0L);
        Assert.Same(original, Next(zero));
        for (int index = 0; index < 16; index++) Assert.Equal(huge, Next(zero));
        Assert.Equal<object?>([5L, 3L, 1L, -1L], Take(Make(5L, -2L), 4));
    }

    [Fact]
    public void FloatIsSuccessiveAdditionRatherThanAnIndexProduct()
    {
        object?[] values = Take(Make(0.0, 0.1), 12); double expected = 0.0;
        foreach (object? value in values) { Assert.Equal(expected, Assert.IsType<double>(value)); expected += 0.1; }
        Assert.Equal(0.6, values[6]); Assert.NotEqual(6 * 0.1, values[6]);
        Assert.Equal(0.9999999999999999, values[10]); Assert.NotEqual(10 * 0.1, values[10]);
        const double stalled = 9007199254740992.0;
        Assert.Equal<object?>([stalled, stalled, stalled], Take(Make(stalled, 1.0), 3));
    }

    [Fact]
    public void MixedInputsKeepOriginalFirstValueBeforeFloatPromotion()
    {
        object start = 1000L; IPyIterator value = Make(start, 0.5);
        Assert.Same(start, Next(value)); Assert.Equal(1000.5, Assert.IsType<double>(Next(value)));
        Assert.Equal("count(1001.0, 0.5)", PyRepr.Repr(value));
        object big = BigInteger.One << 100; value = Make(big, 0.5);
        Assert.Same(big, Next(value)); Assert.Equal((double)(BigInteger)big, Assert.IsType<double>(Next(value)));
        Assert.Equal<object?>([1.5, 3.5, 5.5], Take(Make(1.5, 2L), 3));
    }

    [Fact]
    public void NanInfinityAndFloatOverflowAreValuesRatherThanExhaustion()
    {
        object nan = double.NaN; IPyIterator value = Make(nan, 1L);
        Assert.Same(nan, Next(value)); Assert.True(double.IsNaN(Assert.IsType<double>(Next(value))));
        value = Make(3L, double.NaN); Assert.Equal(3L, Next(value));
        Assert.True(double.IsNaN(Assert.IsType<double>(Next(value))));
        object inf = double.PositiveInfinity; value = Make(inf, double.NegativeInfinity);
        Assert.Same(inf, Next(value)); Assert.True(double.IsNaN(Assert.IsType<double>(Next(value))));
        Assert.Equal<object?>([double.PositiveInfinity, double.PositiveInfinity], Take(Make(double.PositiveInfinity), 2));
        Assert.Equal<object?>([1e308, double.PositiveInfinity, double.PositiveInfinity], Take(Make(1e308, 1e308), 3));
    }

    [Fact]
    public void SignedZeroBitsFollowEachActualAddition()
    {
        object negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
        IPyIterator integerZero = Make(negativeZero, 0L), negativeStep = Make(negativeZero, negativeZero);
        Assert.Same(negativeZero, Next(integerZero)); Assert.Equal(0L, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(Next(integerZero))));
        foreach (object? value in Take(negativeStep, 3))
            Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(value)));
        foreach (object? value in Take(Make(0.0, negativeZero), 3))
            Assert.Equal(0L, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(value)));
    }

    [Fact]
    public void IntegerFloatConversionOverflowDoesNotPublishOrAdvanceAndDefaultCannotHideIt()
    {
        BigInteger huge = BigInteger.Pow(10, 400);
        foreach (IPyIterator value in new[] { Make(huge, 1.0), Make(1.0, huge) })
        {
            string before = PyRepr.Repr(value);
            for (int index = 0; index < 2; index++)
            {
                Assert.Equal("int too large to convert to float", Assert.Throws<PyOverflowError>(() => value.MoveNext()).PyMessage);
                Assert.Null(value.Current()); Assert.Equal(before, PyRepr.Repr(value));
                Assert.Throws<PyOverflowError>(() => PyOps.CallBuiltin("next", [value, "fallback"], [], []));
                Assert.Null(value.Current()); Assert.Equal(before, PyRepr.Repr(value));
            }
        }
    }
}
