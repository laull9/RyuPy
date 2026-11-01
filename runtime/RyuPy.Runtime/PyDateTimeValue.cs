// date/datetime 共用公历存储，区别于独立 time；aware 对象仅在比较和相减时换算 UTC 键。
using System;
using System.Globalization;
using System.Numerics;
namespace RyuPy.Runtime;
/// <summary>不可变公历日期或日期时间。</summary>
public sealed class PyDateTimeValue : PyNativeObject
{
    internal readonly long Microseconds;
    internal readonly bool IsDateTime;
    internal readonly PyDateTimeZone? Zone;
    private readonly int _fold;
    private DateTime Calendar => new(Microseconds * 10, DateTimeKind.Unspecified);
    internal PyDateTimeValue(long value, bool datetime, PyDateTimeZone? zone, int fold)
    { Microseconds = value; IsDateTime = datetime; Zone = zone; _fold = fold; }
    internal static object Create(object?[] args, bool datetime)
    {
        int year = PyDateTimeModule.Index(args[0]), month = PyDateTimeModule.Index(args[1]), day = PyDateTimeModule.Index(args[2]);
        if (year is < 1 or > 9999) throw new PyValueError("year must be in 1..9999");
        if (month is < 1 or > 12) throw new PyValueError("month must be in 1..12");
        if (day < 1 || day > DateTime.DaysInMonth(year, month)) throw new PyValueError("day is out of range for month");
        long value = new DateTime(year, month, day).Ticks / 10;
        if (!datetime) return new PyDateTimeValue(value, false, null, 0);
        return new PyDateTimeValue(value + PyDateTimeClock.Parts(args, 3), true, PyDateTimeModule.Zone(args[7]), PyDateTimeClock.FoldValue(args[8]));
    }
    internal static object FromOrdinal(int ordinal, bool datetime)
    {
        if (ordinal < 1 || ordinal > 3652059) throw new PyValueError("ordinal must be in 1..3652059");
        return new PyDateTimeValue((ordinal - 1L) * PyDateTimeModule.Day, datetime, null, 0);
    }
    /// <inheritdoc/>
    public override string TypeName => IsDateTime ? "datetime" : "date";
    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (PyDateTimeModule.Comparison(name)) return PyDateTimeModule.One(name, other => Compare(name, other));
        if (name is "year" or "month" or "day") return name == "year" ? (long)Calendar.Year : name == "month" ? (long)Calendar.Month : (long)Calendar.Day;
        if (name == "isoformat") return IsDateTime
            ? PyDateTimeModule.Function(name, ["sep", "timespec"], ["T", "auto"], 0, a => Iso(a[0], a[1]))
            : PyDateTimeModule.Zero(name, DateString);
        if (IsDateTime)
        {
            switch (name)
            {
                case "hour": return (long)Calendar.Hour;
                case "minute": return (long)Calendar.Minute;
                case "second": return (long)Calendar.Second;
                case "microsecond": return Microseconds % 1000000;
                case "tzinfo": return (object?)Zone ?? PyNone.Instance;
                case "fold": return (long)_fold;
                case "date": return PyDateTimeModule.Zero(name, () => new PyDateTimeValue(Microseconds / PyDateTimeModule.Day * PyDateTimeModule.Day, false, null, 0));
                case "time": return PyDateTimeModule.Zero(name, () => new PyDateTimeClock(Microseconds % PyDateTimeModule.Day, null, _fold));
                case "timetz": return PyDateTimeModule.Zero(name, () => new PyDateTimeClock(Microseconds % PyDateTimeModule.Day, Zone, _fold));
                case "utcoffset": return PyDateTimeModule.Zero(name, () => Zone is null ? null : new PyDateTimeDelta(Zone.Offset));
                case "dst": return PyDateTimeModule.Zero(name, () => null);
                case "tzname": return PyDateTimeModule.Zero(name, () => Zone?.PythonStr());
                case "astimezone": return PyDateTimeModule.Function(name, ["tz"], [null], 0, a => ChangeZone(a[0]));
                case "timestamp": return PyDateTimeModule.Zero(name, () => Zone is null
                    ? throw new PyNotImplementedError("naive timestamp requires an unsupported platform local timezone")
                    : (Microseconds - Zone.Offset - new DateTime(1970, 1, 1).Ticks / 10) / 1000000.0);
            }
        }
        return name switch
        {
            "weekday" => PyDateTimeModule.Zero(name, () => (long)(((int)Calendar.DayOfWeek + 6) % 7)),
            "isoweekday" => PyDateTimeModule.Zero(name, () => (long)(((int)Calendar.DayOfWeek + 6) % 7 + 1)),
            "toordinal" => PyDateTimeModule.Zero(name, () => Microseconds / PyDateTimeModule.Day + 1),
            "replace" => ReplaceFunction(),
            "__add__" or "__radd__" => PyDateTimeModule.One(name, value => value is PyDateTimeDelta delta ? Shift(delta, false) : PyNotImplemented.Instance),
            "__sub__" => PyDateTimeModule.One(name, Subtract),
            "__hash__" => PyDateTimeModule.Zero(name, () => PyHash.Of(new PyTuple([TypeName, Zone is not null, Microseconds - (Zone?.Offset ?? 0)]))),
            _ => throw Missing(name),
        };
    }
    private PyFunction ReplaceFunction() => IsDateTime
        ? PyNativeFunctions.Create("replace", ["year", "month", "day", "hour", "minute", "second", "microsecond", "tzinfo", "fold"],
            [(long)Calendar.Year, (long)Calendar.Month, (long)Calendar.Day, (long)Calendar.Hour, (long)Calendar.Minute, (long)Calendar.Second, Microseconds % 1000000, Zone, (long)_fold],
            0, a => Create(a, true), ["pos", "pos", "pos", "pos", "pos", "pos", "pos", "pos", "kwonly"])
        : PyDateTimeModule.Function("replace", ["year", "month", "day"], [(long)Calendar.Year, (long)Calendar.Month, (long)Calendar.Day], 0, a => Create(a, false));
    internal object Shift(PyDateTimeDelta delta, bool subtract)
    {
        BigInteger change = IsDateTime ? delta.Microseconds : delta.Days * PyDateTimeModule.Day;
        BigInteger next = Microseconds + (subtract ? -change : change);
        if (next < 0 || next > DateTime.MaxValue.Ticks / 10) throw new PyOverflowError("date value out of range");
        return new PyDateTimeValue((long)next, IsDateTime, Zone, 0);
    }
    private object Subtract(object? value)
    {
        if (value is PyDateTimeDelta delta) return Shift(delta, true);
        if (value is not PyDateTimeValue other || other.IsDateTime != IsDateTime) return PyNotImplemented.Instance;
        if ((Zone is null) != (other.Zone is null)) throw new PyTypeError("can't subtract offset-naive and offset-aware datetimes");
        return new PyDateTimeDelta(Microseconds - (Zone?.Offset ?? 0) - other.Microseconds + (other.Zone?.Offset ?? 0));
    }
    private object Compare(string op, object? value)
    {
        if (value is not PyDateTimeValue other || other.IsDateTime != IsDateTime) return PyNotImplemented.Instance;
        if ((Zone is null) != (other.Zone is null))
        { if (op is "__eq__" or "__ne__") return op == "__ne__"; throw new PyTypeError("can't compare offset-naive and offset-aware datetimes"); }
        return PyDateTimeModule.Compare(op, (Microseconds - (Zone?.Offset ?? 0)).CompareTo(other.Microseconds - (other.Zone?.Offset ?? 0)));
    }
    private object ChangeZone(object? value)
    {
        var zone = PyDateTimeModule.Zone(value);
        if (Zone is null || zone is null) throw new PyNotImplementedError("astimezone requires explicit fixed timezones");
        if (ReferenceEquals(zone, Zone)) return this;
        BigInteger next = new BigInteger(Microseconds) - Zone.Offset + zone.Offset;
        if (next < 0 || next > DateTime.MaxValue.Ticks / 10) throw new PyOverflowError("date value out of range");
        return new PyDateTimeValue((long)next, true, zone, 0);
    }
    private string DateString() => Calendar.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string Iso(object? separator, object? specification)
    {
        if (separator is not string text || PyText.Length(text) != 1) throw new PyTypeError("isoformat() separator must be a single character");
        if (specification is not string spec) throw new PyTypeError("timespec must be str");
        return DateString() + text + PyDateTimeClock.Format(Microseconds % PyDateTimeModule.Day, spec) + (Zone?.Suffix() ?? "");
    }
    /// <inheritdoc/>
    public override string PythonStr() => IsDateTime ? Iso(" ", "auto") : DateString();
    /// <inheritdoc/>
    public override string PythonRepr()
    {
        string result = $"datetime.{TypeName}({Calendar.Year}, {Calendar.Month}, {Calendar.Day}";
        if (IsDateTime)
        {
            result += $", {Calendar.Hour}, {Calendar.Minute}";
            if (Calendar.Second != 0 || Microseconds % 1000000 != 0) result += $", {Calendar.Second}";
            if (Microseconds % 1000000 != 0) result += $", {Microseconds % 1000000}";
            if (Zone is not null) result += ", tzinfo=" + Zone.PythonRepr(); if (_fold != 0) result += ", fold=1";
        }
        return result + ")";
    }
}
