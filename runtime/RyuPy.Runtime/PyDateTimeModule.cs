// 日期模块以公历 ordinal 和整数微秒为语义核心，不使用受 ±14 小时约束的 DateTimeOffset。
using System;
using System.Collections.Generic;
using System.Numerics;
namespace RyuPy.Runtime;
/// <summary>日期、时间、时间差和固定偏移时区的有限标准库入口。</summary>
public static class PyDateTimeModule
{
    internal const long Day = 86400000000L;
    internal static readonly PyDateTimeZone Utc = new(0, null);
    /// <summary>创建原生类型以及明确实现的类方法和常量。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var date = Type("date", ["year", "month", "day"], [null, null, null], 3, a => PyDateTimeValue.Create(a, false), v => v is PyDateTimeValue);
        var datetime = Type("datetime", ["year", "month", "day", "hour", "minute", "second", "microsecond", "tzinfo", "fold"],
            [null, null, null, 0L, 0L, 0L, 0L, null, 0L], 3, a => PyDateTimeValue.Create(a, true), v => v is PyDateTimeValue { IsDateTime: true }, ["date"],
            ["pos", "pos", "pos", "pos", "pos", "pos", "pos", "pos", "kwonly"]);
        var time = Type("time", ["hour", "minute", "second", "microsecond", "tzinfo", "fold"], [0L, 0L, 0L, 0L, null, 0L], 0,
            a => PyDateTimeClock.Create(a), v => v is PyDateTimeClock, [], ["pos", "pos", "pos", "pos", "pos", "kwonly"]);
        var delta = Type("timedelta", ["days", "seconds", "microseconds", "milliseconds", "minutes", "hours", "weeks"],
            [0L, 0L, 0L, 0L, 0L, 0L, 0L], 0, PyDateTimeDelta.Create, v => v is PyDateTimeDelta);
        var zone = Type("timezone", ["offset", "name"], [null, PyDateTimeZone.MissingName], 1, a => PyDateTimeZone.Create(a), v => v is PyDateTimeZone, ["tzinfo"]);
        var tzinfo = new PyNativeType("tzinfo", "datetime", null, v => v is PyDateTimeZone);
        zone.SetAttr("utc", Utc);
        zone.SetAttr("min", new PyDateTimeZone(-Day + 60000000L, null)); zone.SetAttr("max", new PyDateTimeZone(Day - 60000000L, null));
        date.SetAttr("fromordinal", Function("fromordinal", ["n"], [null], 1, a => PyDateTimeValue.FromOrdinal(Index(a[0]), false)));
        datetime.SetAttr("fromordinal", Function("fromordinal", ["n"], [null], 1, a => PyDateTimeValue.FromOrdinal(Index(a[0]), true)));
        date.SetAttr("min", new PyDateTimeValue(0, false, null, 0)); date.SetAttr("max", new PyDateTimeValue(new DateTime(9999, 12, 31).Ticks / 10, false, null, 0));
        datetime.SetAttr("min", new PyDateTimeValue(0, true, null, 0)); datetime.SetAttr("max", new PyDateTimeValue(DateTime.MaxValue.Ticks / 10, true, null, 0));
        date.SetAttr("resolution", new PyDateTimeDelta(Day)); datetime.SetAttr("resolution", new PyDateTimeDelta(1));
        time.SetAttr("min", new PyDateTimeClock(0, null, 0)); time.SetAttr("max", new PyDateTimeClock(Day - 1, null, 0)); time.SetAttr("resolution", new PyDateTimeDelta(1));
        delta.SetAttr("min", new PyDateTimeDelta(-new BigInteger(999999999) * Day));
        delta.SetAttr("max", new PyDateTimeDelta(new BigInteger(1000000000) * Day - 1)); delta.SetAttr("resolution", new PyDateTimeDelta(1));
        return new() { ["date"] = date, ["datetime"] = datetime, ["time"] = time, ["timedelta"] = delta, ["timezone"] = zone, ["tzinfo"] = tzinfo, ["MINYEAR"] = 1L, ["MAXYEAR"] = 9999L };
    }
    private static PyNativeType Type(string name, string[] names, object?[] defaults, int required, Func<object?[], object?> body,
        Func<object?, bool> accepts, string[]? bases = null, string[]? kinds = null)
    {
        var function = PyNativeFunctions.Create(name, names, defaults, required, body, kinds);
        return new(name, "datetime", (a, k, v) => PyOps.CallValue(function, a, k, v), accepts, bases ?? []);
    }
    internal static PyFunction Function(string name, string[] names, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, defaults, required, body);
    internal static PyFunction Zero(string name, Func<object?> body) => Function(name, [], [], 0, _ => body());
    internal static PyFunction One(string name, Func<object?, object?> body) => Function(name, ["other"], [null], 1, a => body(a[0]));
    internal static BigInteger Integer(object? value) => value switch
    { long n => n, int n => n, bool b => b ? 1 : 0, BigInteger n => n, _ => throw new PyTypeError("integer argument expected") };
    internal static int Index(object? value)
    {
        if (value is not (long or int or bool or BigInteger))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value)) throw new PyTypeError("integer argument expected");
        }
        BigInteger n = Integer(value);
        if (n < int.MinValue || n > int.MaxValue) throw new PyOverflowError("Python int too large to convert to C long");
        return (int)n;
    }
    internal static BigInteger Floor(BigInteger n, BigInteger d)
    { BigInteger q = BigInteger.DivRem(n, d, out var r); return r.Sign < 0 ? q - 1 : q; }
    internal static BigInteger Round(BigInteger n, BigInteger d)
    {
        if (d.Sign < 0) { n = -n; d = -d; }
        BigInteger q = Floor(n, d), r = n - q * d;
        return r * 2 > d || r * 2 == d && !q.IsEven ? q + 1 : q;
    }
    internal static (BigInteger Numerator, BigInteger Denominator) Ratio(object? value)
    {
        if (value is not double number) return (Integer(value), BigInteger.One);
        if (double.IsNaN(number)) throw new PyValueError("cannot convert float NaN to integer");
        if (double.IsInfinity(number)) throw new PyOverflowError("cannot convert float infinity to integer");
        long bits = BitConverter.DoubleToInt64Bits(number); int exponent = (int)((bits >> 52) & 2047);
        BigInteger mantissa = bits & 0xfffffffffffffL;
        if (exponent != 0) mantissa += BigInteger.One << 52;
        int shift = exponent == 0 ? -1074 : exponent - 1075;
        if (bits < 0) mantissa = -mantissa;
        return shift >= 0 ? (mantissa << shift, BigInteger.One) : (mantissa, BigInteger.One << -shift);
    }
    // 先确定二进制指数、舍入有效位，再缩放；不能分别把大分子/分母转 double 导致双舍入。
    internal static double FractionDouble(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero) throw new PyZeroDivisionError("division by zero");
        bool negative = numerator.Sign < 0 ^ denominator.Sign < 0;
        BigInteger n = BigInteger.Abs(numerator), d = BigInteger.Abs(denominator);
        if (n.IsZero) return negative ? -0.0 : 0.0;
        int exponent = checked((int)(n.GetBitLength() - d.GetBitLength()));
        if (exponent >= 0 ? n < (d << exponent) : (n << -exponent) < d) exponent--;
        int shift = exponent < -1022 ? 1074 : 52 - exponent;
        BigInteger rounded = shift >= 0 ? Round(n << shift, d) : Round(n, d << -shift);
        double result = Math.ScaleB((double)rounded, -shift);
        return negative ? -result : result;
    }
    internal static object Number(BigInteger value) => value >= long.MinValue && value <= long.MaxValue ? (object)(long)value : value;
    internal static object Compare(string op, int? compare) => compare is null ? PyNotImplemented.Instance : op switch
    { "__eq__" => compare == 0, "__ne__" => compare != 0, "__lt__" => compare < 0, "__le__" => compare <= 0, "__gt__" => compare > 0, _ => compare >= 0 };
    internal static bool Comparison(string name) => name is "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__";
    internal static PyDateTimeZone? Zone(object? value) => value is null or PyNone ? null : value as PyDateTimeZone
        ?? throw new PyNotImplementedError("only datetime.timezone fixed offsets are supported");
}
