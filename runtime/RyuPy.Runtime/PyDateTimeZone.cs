// 固定时区使用有符号微秒偏移，支持 Python 的严格小于 24 小时范围和秒级偏移。
using System;
namespace RyuPy.Runtime;
/// <summary>不含 DST 或平台时区查找的固定偏移时区。</summary>
public sealed class PyDateTimeZone : PyNativeObject
{
    internal readonly long Offset;
    private readonly string? _name;
    internal static readonly object MissingName = new();
    internal PyDateTimeZone(long offset, string? name)
    {
        if (offset <= -PyDateTimeModule.Day || offset >= PyDateTimeModule.Day) throw new PyValueError("offset must be strictly between -24h and 24h");
        Offset = offset; _name = name;
    }
    internal static object Create(object?[] args)
    {
        if (args[0] is not PyDateTimeDelta delta) throw new PyTypeError("offset must be a timedelta");
        if (delta.Microseconds <= -PyDateTimeModule.Day || delta.Microseconds >= PyDateTimeModule.Day) throw new PyValueError("offset must be strictly between -24h and 24h");
        string? name = ReferenceEquals(args[1], MissingName) ? null : args[1] as string ?? throw new PyTypeError("name must be a string");
        return delta.Microseconds.IsZero && name is null ? PyDateTimeModule.Utc : new PyDateTimeZone((long)delta.Microseconds, name);
    }
    /// <inheritdoc/>
    public override string TypeName => "timezone";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "utcoffset" or "dst" or "tzname" => PyDateTimeModule.Function(name, ["dt"], [null], 1, a =>
        {
            if (a[0] is not (null or PyNone or PyDateTimeValue { IsDateTime: true })) throw new PyTypeError("argument must be a datetime or None");
            return name == "utcoffset" ? new PyDateTimeDelta(Offset) : name == "dst" ? null : PythonStr();
        }),
        "fromutc" => PyDateTimeModule.Function(name, ["dt"], [null], 1, a =>
        {
            if (a[0] is not PyDateTimeValue { IsDateTime: true } value) throw new PyTypeError("fromutc() requires a datetime argument");
            if (!ReferenceEquals(value.Zone, this)) throw new PyValueError("fromutc: dt.tzinfo is not self");
            return value.Shift(new PyDateTimeDelta(Offset), false);
        }),
        "__eq__" or "__ne__" => PyDateTimeModule.One(name, other => other is PyDateTimeZone zone ?
            name == "__eq__" ? Offset == zone.Offset : Offset != zone.Offset : PyNotImplemented.Instance),
        "__hash__" => PyDateTimeModule.Zero(name, () => PyHash.Of(new PyDateTimeDelta(Offset))),
        _ => throw Missing(name),
    };
    internal string Suffix()
    {
        long value = Math.Abs(Offset), seconds = value / 1000000;
        string text = $"{(Offset < 0 ? "-" : "+")}{seconds / 3600:00}:{seconds / 60 % 60:00}";
        if (seconds % 60 != 0 || value % 1000000 != 0) text += $":{seconds % 60:00}";
        if (value % 1000000 != 0) text += $".{value % 1000000:000000}";
        return text;
    }
    /// <inheritdoc/>
    public override string PythonStr() => _name ?? (Offset == 0 ? "UTC" : "UTC" + Suffix());
    /// <inheritdoc/>
    public override string PythonRepr() => ReferenceEquals(this, PyDateTimeModule.Utc) ? "datetime.timezone.utc" :
        "datetime.timezone(" + new PyDateTimeDelta(Offset).PythonRepr() + (_name is null ? "" : ", " + PyRepr.Repr(_name)) + ")";
}
