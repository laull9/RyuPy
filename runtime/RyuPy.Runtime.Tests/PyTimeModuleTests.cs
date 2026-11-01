// 时钟只验单调和秒数范围，sleep 使用零/毫秒级等待，不依赖 CI 调度的精确上界。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyTimeModuleTests
{
    private static readonly Dictionary<string, object?> Attributes = PyTimeModule.CreateAttributes();
    private static object? Call(string name, params object?[] args) => PyOps.CallValue(Attributes[name], args, [], []);

    [Fact]
    public void WallClockUsesEpochSecondsAndCountersAreMonotonic()
    {
        double before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        double timestamp = Assert.IsType<double>(Call("time"));
        double after = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1;
        Assert.InRange(timestamp, before, after);
        foreach (string name in new[] { "monotonic", "perf_counter" })
        {
            double first = Assert.IsType<double>(Call(name));
            double second = Assert.IsType<double>(Call(name));
            Assert.True(second >= first);
        }
    }

    [Fact]
    public void ShortSleepDoesNotReturnBeforeTheRequestedDuration()
    {
        Assert.IsType<PyNone>(Call("sleep", 0L));
        Assert.IsType<PyNone>(Call("sleep", -0.0));
        long start = Stopwatch.GetTimestamp();
        Assert.IsType<PyNone>(Call("sleep", 0.001));
        Assert.True(Stopwatch.GetElapsedTime(start).TotalSeconds >= 0.001);
    }

    [Fact]
    public void SleepValidatesDomainOverflowAndStrictIndexProtocol()
    {
        Assert.Throws<PyValueError>(() => Call("sleep", -1L));
        Assert.Throws<PyValueError>(() => Call("sleep", double.NaN));
        Assert.Throws<PyOverflowError>(() => Call("sleep", double.PositiveInfinity));
        Assert.Throws<PyOverflowError>(() => Call("sleep", double.NegativeInfinity));
        Assert.Throws<PyOverflowError>(() => Call("sleep", BigInteger.One << 100));
        Assert.Throws<PyOverflowError>(() => Call("sleep", -(BigInteger.One << 100)));
        Assert.Throws<PyTypeError>(() => Call("sleep", "0"));
        Assert.Throws<PyTypeError>(() => Call("sleep", PyMathModuleTests.Protocol("__float__", () => 0.0)));
        Assert.Throws<PyTypeError>(() => Call("sleep", PyMathModuleTests.Protocol("__index__", () => 0.0)));
        int calls = 0;
        Assert.IsType<PyNone>(Call("sleep", PyMathModuleTests.Protocol("__index__", () => { calls++; return 0L; })));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ClockAndSleepSignaturesRejectArgumentsAndKeywords()
    {
        foreach (string name in new[] { "time", "monotonic", "perf_counter" })
        {
            Assert.Throws<PyTypeError>(() => Call(name, 0L));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(Attributes[name], [], ["value"], [0L]));
        }
        Assert.Throws<PyTypeError>(() => Call("sleep"));
        Assert.Throws<PyTypeError>(() => Call("sleep", 0L, 0L));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Attributes["sleep"], [], ["secs"], [0L]));
    }
}
