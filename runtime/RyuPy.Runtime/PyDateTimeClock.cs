// 独立时间保存当天微秒和固定时区；比较按偏移校正但不把校正值取模折回当天。
using System;
namespace RyuPy.Runtime;
/// <summary>微秒精确的 time 对象。</summary>
public sealed class PyDateTimeClock : PyNativeObject
{
    internal readonly long Microseconds;
    internal readonly object? Zone;
    internal readonly int Fold;
    private long? _hash;
    internal PyDateTimeClock(long microseconds, object? zone, int fold)
    { Microseconds = microseconds; Zone = zone; Fold = fold; }
    internal static object Create(object?[] args) => new PyDateTimeClock(Parts(args, 0), PyDateTimeModule.Zone(args[4]), FoldValue(args[5]));
    internal static int FoldValue(object? value)
    { int fold = PyDateTimeModule.Index(value); if (fold is < 0 or > 1) throw new PyValueError("fold must be either 0 or 1"); return fold; }
    internal static long Parts(object?[] args, int start)
    {
        int h = PyDateTimeModule.Index(args[start]), m = PyDateTimeModule.Index(args[start + 1]), s = PyDateTimeModule.Index(args[start + 2]), u = PyDateTimeModule.Index(args[start + 3]);
        if (h is < 0 or > 23) throw new PyValueError("hour must be in 0..23");
        if (m is < 0 or > 59) throw new PyValueError("minute must be in 0..59");
        if (s is < 0 or > 59) throw new PyValueError("second must be in 0..59");
        if (u is < 0 or > 999999) throw new PyValueError("microsecond must be in 0..999999");
        return ((h * 60L + m) * 60 + s) * 1000000 + u;
    }
    internal static string Format(long value, string spec)
    {
        long seconds = value / 1000000, us = value % 1000000;
        if (spec == "auto") spec = us == 0 ? "seconds" : "microseconds";
        string result = $"{seconds / 3600:00}";
        if (spec == "hours") return result;
        result += $":{seconds / 60 % 60:00}";
        if (spec == "minutes") return result;
        result += $":{seconds % 60:00}";
        return spec switch { "seconds" => result, "milliseconds" => result + $".{us / 1000:000}",
            "microseconds" => result + $".{us:000000}", _ => throw new PyValueError("Unknown timespec value") };
    }
    /// <inheritdoc/>
    public override string TypeName => "time";
    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (PyDateTimeModule.Comparison(name)) return PyDateTimeModule.One(name, value => Compare(name, value));
        return name switch
        {
            "hour" => Microseconds / 3600000000, "minute" => Microseconds / 60000000 % 60,
            "second" => Microseconds / 1000000 % 60, "microsecond" => Microseconds % 1000000, "tzinfo" => (object?)Zone ?? PyNone.Instance, "fold" => (long)Fold,
            "isoformat" => PyDateTimeModule.Function(name, ["timespec"], ["auto"], 0, a => Format(Microseconds, PyCast.ToStr(a[0])) + PyDateTimeZoneProtocol.Suffix(Zone, null)),
            "utcoffset" => PyDateTimeModule.Zero(name, () => PyDateTimeZoneProtocol.Delta(Zone, "utcoffset", null)),
            "dst" => PyDateTimeModule.Zero(name, () => PyDateTimeZoneProtocol.Delta(Zone, "dst", null)), "tzname" => PyDateTimeModule.Zero(name, () => PyDateTimeZoneProtocol.Name(Zone, null)),
            "replace" => PyNativeFunctions.Create(name, ["hour", "minute", "second", "microsecond", "tzinfo", "fold"],
                [Microseconds / 3600000000, Microseconds / 60000000 % 60, Microseconds / 1000000 % 60, Microseconds % 1000000, Zone, (long)Fold], 0, Create,
                ["pos", "pos", "pos", "pos", "pos", "kwonly"]),
            "__hash__" => PyDateTimeModule.Zero(name, Hash),
            _ => throw Missing(name),
        };
    }
    private object Compare(string name, object? value)
    {
        if (value is not PyDateTimeClock clock) return PyNotImplemented.Instance;
        if (ReferenceEquals(Zone, clock.Zone)) return PyDateTimeModule.Compare(name, Microseconds.CompareTo(clock.Microseconds));
        long? left = PyDateTimeZoneProtocol.Offset(Zone, null), right = PyDateTimeZoneProtocol.Offset(clock.Zone, null);
        if ((left is null) != (right is null))
        { if (name is "__eq__" or "__ne__") return name == "__ne__"; throw new PyTypeError("can't compare offset-naive and offset-aware times"); }
        return PyDateTimeModule.Compare(name, (Microseconds - (left ?? 0)).CompareTo(clock.Microseconds - (right ?? 0)));
    }
    private object Hash()
    {
        if (_hash is { } hash) return hash;
        long offset = PyDateTimeZoneProtocol.Offset(Zone, null) ?? 0;
        // time的零偏移与无偏移哈希相同；偏移校正不按一天取模。
        return (_hash = PyHash.Of(new PyTuple(["time", Microseconds - offset]))).Value;
    }

    /// <inheritdoc/>
    public override string PythonStr() => Format(Microseconds, "auto") + PyDateTimeZoneProtocol.Suffix(Zone, null);
    /// <inheritdoc/>
    public override string PythonRepr()
    {
        string text = $"datetime.time({Microseconds / 3600000000}, {Microseconds / 60000000 % 60}";
        if (Microseconds % 60000000 != 0) text += $", {Microseconds / 1000000 % 60}";
        if (Microseconds % 1000000 != 0) text += $", {Microseconds % 1000000}";
        if (Zone is not null) text += ", tzinfo=" + PyRepr.Repr(Zone); if (Fold != 0) text += ", fold=1";
        return text + ")";
    }
}
