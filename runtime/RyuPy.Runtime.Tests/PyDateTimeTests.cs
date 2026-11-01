// 日期专测不用宿主本地时区；所有边界以公历和微秒整数表达。
using System;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyDateTimeTests
{
    private static object New(string type, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyDateTimeModule.CreateAttributes()[type], args, names ?? [], values ?? [])!;
    private static object? Call(object value, string method, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(value, method), args, [], []);
    [Fact]
    public void NegativeDeltaNormalizesAndFractionalConstructionRoundsOnce()
    {
        object delta = New("timedelta", [], ["microseconds"], [-1L]);
        Assert.Equal(-1L, PyObject.GetAttr(delta, "days")); Assert.Equal(86399L, PyObject.GetAttr(delta, "seconds"));
        Assert.Equal(999999L, PyObject.GetAttr(delta, "microseconds")); Assert.Equal("-1 day, 23:59:59.999999", PyRepr.Str(delta));
        Assert.Equal(0L, PyObject.GetAttr(New("timedelta", [], ["microseconds"], [0.5]), "microseconds"));
        Assert.Equal(0L, PyObject.GetAttr(New("timedelta", [], ["milliseconds"], [0.0005]), "microseconds"));
        Assert.Equal(2L, PyObject.GetAttr(New("timedelta", [], ["microseconds"], [1.5]), "microseconds"));
        Assert.Throws<PyOverflowError>(() => New("timedelta", [], ["days"], [1000000000L]));
        object large = New("timedelta", [], ["days", "microseconds"], [999999999L, 1L]);
        Assert.Equal(86399999913600.0, Call(large, "total_seconds"));
    }
    [Fact]
    public void FixedOffsetsBeyondClrLimitAndUtcEqualityAreExact()
    {
        object offset = New("timedelta", [], ["hours"], [20L]); object zone = New("timezone", [offset]);
        Assert.Equal("UTC+20:00", PyRepr.Str(zone));
        object utc = PyObject.GetAttr(PyDateTimeModule.CreateAttributes()["timezone"], "utc")!;
        object first = New("datetime", [2024L, 1L, 2L, 20L], ["tzinfo"], [zone]);
        object second = New("datetime", [2024L, 1L, 2L], ["tzinfo"], [utc]);
        Assert.True(PyHash.Equal(first, second)); Assert.Equal(PyHash.Of(first), PyHash.Of(second));
        Assert.Throws<PyValueError>(() => New("timezone", [New("timedelta", [], ["hours"], [24L])]));
        object naive = New("datetime", [2024L, 1L, 2L]); Assert.False(PyHash.Equal(naive, second));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(naive, "tzinfo"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(New("time", []), "tzinfo"));
        Assert.Throws<PyTypeError>(() => Call(naive, "__lt__", second));
    }
    [Fact]
    public void LeapDayMicrosecondRolloverAndDateDaysOnlyAreCorrect()
    {
        object value = New("datetime", [2024L, 2L, 28L, 23L, 59L, 59L, 999999L]);
        object one = New("timedelta", [], ["microseconds"], [1L]);
        Assert.Equal("2024-02-29 00:00:00", PyRepr.Str(Call(value, "__add__", one)));
        object day = New("date", [2024L, 2L, 28L]);
        Assert.Equal("2024-02-28", PyRepr.Str(Call(day, "__add__", one)));
        Assert.Throws<PyValueError>(() => New("date", [2023L, 2L, 29L]));
        Assert.Throws<PyOverflowError>(() => Call(New("date", [1L, 1L, 1L]), "__sub__", New("timedelta", [1L])));
        Assert.Equal("01:02:03.000456", Call(New("time", [1L, 2L, 3L, 456L]), "isoformat"));
    }
}
