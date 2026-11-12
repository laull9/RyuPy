// 时区协议只接受真实canonical根的实例；每次调用读取真实方法，不缓存偏移快照。
using System;
namespace RyuPy.Runtime;
internal static class PyDateTimeZoneProtocol
{
    private static string ResultType(object value) => value is PyDateTimeDelta or PyDateTimeValue or PyDateTimeClock or PyDateTimeZone
        ? "datetime." + ((PyNativeObject)value).TypeName : PyRepr.TypeName(value);
    internal static object? Check(object? zone)
    {
        if (zone is null or PyNone) return null;
        if (zone is PyDateTimeZone || zone is PyInstance instance && instance.Class.IsSubclassOf(PyDateTimeTzInfo.Root)) return zone;
        throw new PyTypeError("tzinfo argument must be None or of a tzinfo subclass");
    }
    internal static object? Delta(object? zone, string method, object? datetime)
    {
        if (zone is null) return null;
        object? result = zone is PyDateTimeZone fixedZone
            ? method == "utcoffset" ? new PyDateTimeDelta(fixedZone.Offset) : null
            : PyOps.CallMethod(zone, method, [datetime ?? PyNone.Instance], [], []);
        if (result is null or PyNone) return null;
        if (result is not PyDateTimeDelta delta) throw new PyTypeError($"tzinfo.{method}() must return None or timedelta, not '{ResultType(result)}'");
        if (delta.Microseconds <= -PyDateTimeModule.Day || delta.Microseconds >= PyDateTimeModule.Day)
            throw new PyValueError("offset must be a timedelta strictly between -timedelta(hours=24) and timedelta(hours=24).");
        return delta;
    }
    internal static long? Offset(object? zone, object? datetime) => Delta(zone, "utcoffset", datetime) is PyDateTimeDelta delta ? (long)delta.Microseconds : null;
    internal static object? Name(object? zone, object? datetime)
    {
        if (zone is null) return null;
        object? result = zone is PyDateTimeZone fixedZone ? fixedZone.PythonStr() : PyOps.CallMethod(zone, "tzname", [datetime ?? PyNone.Instance], [], []);
        if (result is null or PyNone) return null;
        return PyStringLayout.TryGet(result, out _) ? result : throw new PyTypeError($"tzinfo.tzname() must return None or a string, not '{ResultType(result)}'");
    }
    internal static string Suffix(object? zone, object? datetime)
    {
        long? offset = Offset(zone, datetime);
        return offset is null ? "" : new PyDateTimeZone(offset.Value, null).Suffix();
    }
    internal static object? FromUtc(object zone, PyDateTimeValue datetime) => PyOps.CallMethod(zone, "fromutc", [datetime], [], []);
    internal static object DefaultFromUtc(object? self, object? value)
    {
        if (value is not PyDateTimeValue { IsDateTime: true } dt) throw new PyTypeError("fromutc: argument must be a datetime");
        if (!ReferenceEquals(dt.Zone, self)) throw new PyValueError("fromutc: dt.tzinfo is not self");
        long offset = Offset(self, dt) ?? throw new PyValueError("fromutc: non-None utcoffset() result required");
        var dst = Delta(self, "dst", dt) as PyDateTimeDelta ?? throw new PyValueError("fromutc: non-None dst() result required");
        var difference = offset - dst.Microseconds;
        if (!difference.IsZero)
        {
            dt = (PyDateTimeValue)dt.Shift(new PyDateTimeDelta(difference), false);
            dst = Delta(self, "dst", dt) as PyDateTimeDelta ?? throw new PyValueError("fromutc(): dt.dst gave inconsistent results; cannot convert");
        }
        return dt.Shift(dst, false);
    }
}
