// 时间差用大整数总微秒；浮点构造保留 CPython 的分量缩放双精度舍入，最后统一 ties-to-even。
using System;
using System.Numerics;
namespace RyuPy.Runtime;
/// <summary>微秒精确的不可变时间差。</summary>
public sealed class PyDateTimeDelta : PyNativeObject
{
    internal readonly BigInteger Microseconds;
    internal BigInteger Days => PyDateTimeModule.Floor(Microseconds, PyDateTimeModule.Day);
    private long Rest => (long)(Microseconds - Days * PyDateTimeModule.Day);
    internal PyDateTimeDelta(BigInteger value)
    {
        Microseconds = value;
        if (Days < -999999999 || Days > 999999999) throw new PyOverflowError("timedelta days out of range");
    }
    internal static object Create(object?[] args)
    {
        long[] multipliers = [PyDateTimeModule.Day, 1000000, 1, 1000, 60000000, 3600000000, PyDateTimeModule.Day * 7];
        BigInteger whole = 0; double fraction = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is not double number) { whole += PyDateTimeModule.Integer(args[i]) * multipliers[i]; continue; }
            if (double.IsNaN(number)) throw new PyValueError("cannot convert float NaN to integer");
            if (double.IsInfinity(number)) throw new PyOverflowError("cannot convert float infinity to integer");
            double integral = Math.Truncate(number);
            whole += new BigInteger(integral) * multipliers[i];
            double scaled = (number - integral) * multipliers[i];
            double scaledIntegral = Math.Truncate(scaled);
            whole += new BigInteger(scaledIntegral); fraction += scaled - scaledIntegral;
        }
        double floor = Math.Floor(fraction); whole += new BigInteger(floor);
        double remaining = fraction - floor;
        if (remaining > 0.5 || remaining == 0.5 && !whole.IsEven) whole++;
        return new PyDateTimeDelta(whole);
    }

    /// <inheritdoc/>
    public override string TypeName => "timedelta";
    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (PyDateTimeModule.Comparison(name)) return PyDateTimeModule.One(name, other =>
            PyDateTimeModule.Compare(name, other is PyDateTimeDelta d ? Microseconds.CompareTo(d.Microseconds) : null));
        return name switch
        {
            "days" => (long)Days, "seconds" => Rest / 1000000, "microseconds" => Rest % 1000000,
            "total_seconds" => PyDateTimeModule.Zero(name, () => PyDateTimeModule.FractionDouble(Microseconds, 1000000)),
            "__bool__" => PyDateTimeModule.Zero(name, () => !Microseconds.IsZero),
            "__hash__" => PyDateTimeModule.Zero(name, () => PyHash.Of(new PyTuple([(long)Days, Rest / 1000000, Rest % 1000000]))),
            "__neg__" => PyDateTimeModule.Zero(name, () => new PyDateTimeDelta(-Microseconds)),
            "__pos__" => PyDateTimeModule.Zero(name, () => this),
            "__abs__" => PyDateTimeModule.Zero(name, () => new PyDateTimeDelta(BigInteger.Abs(Microseconds))),
            "__add__" or "__radd__" => PyDateTimeModule.One(name, Add),
            "__sub__" => PyDateTimeModule.One(name, value => value is PyDateTimeDelta delta ? new PyDateTimeDelta(Microseconds - delta.Microseconds) : PyNotImplemented.Instance),
            "__mul__" or "__rmul__" => PyDateTimeModule.One(name, value => Scale(value, false)),
            "__truediv__" => PyDateTimeModule.One(name, value => Scale(value, true)),
            _ => throw Missing(name),
        };
    }
    private object Add(object? value) => value switch
    {
        PyDateTimeDelta delta => new PyDateTimeDelta(Microseconds + delta.Microseconds),
        PyDateTimeValue date => date.Shift(this, false), _ => PyNotImplemented.Instance,
    };
    private object Scale(object? value, bool divide)
    {
        if (divide && value is PyDateTimeDelta delta)
        { if (delta.Microseconds.IsZero) throw new PyZeroDivisionError("division by zero"); return PyDateTimeModule.FractionDouble(Microseconds, delta.Microseconds); }
        if (value is not (long or int or BigInteger or bool or double)) return PyNotImplemented.Instance;
        var ratio = PyDateTimeModule.Ratio(value);
        if (divide && ratio.Numerator.IsZero) throw new PyZeroDivisionError("division by zero");
        return new PyDateTimeDelta(divide ? PyDateTimeModule.Round(Microseconds * ratio.Denominator, ratio.Numerator) :
            PyDateTimeModule.Round(Microseconds * ratio.Numerator, ratio.Denominator));
    }
    /// <inheritdoc/>
    public override string PythonStr()
    {
        long rest = Rest, seconds = rest / 1000000;
        string result = $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";
        if (rest % 1000000 != 0) result += $".{rest % 1000000:000000}";
        return Days.IsZero ? result : $"{Days} day{(Days == 1 || Days == -1 ? "" : "s")}, " + result;
    }
    /// <inheritdoc/>
    public override string PythonRepr()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!Days.IsZero) parts.Add("days=" + Days); if (Rest / 1000000 != 0) parts.Add("seconds=" + Rest / 1000000);
        if (Rest % 1000000 != 0) parts.Add("microseconds=" + Rest % 1000000);
        return "datetime.timedelta(" + (parts.Count == 0 ? "0" : string.Join(", ", parts)) + ")";
    }
}
