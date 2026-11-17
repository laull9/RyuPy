// 精确CPython标量的无协议读取：单次owned租约，整数走base16，Unicode直接保留UTF-16孤立代理项。
using System.Globalization;
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.CPython;

internal sealed partial class PythonObject
{
    public bool TryReadExactScalar(out object? value, out string? unicodeCodePoints, bool materialize = true)
    {
        (bool supported, object? scalar, string? points) = Host.PythonCall(() =>
        {
            nint source = Native.rp_incref(Pointer);
            try
            {
                int kind = Native.rp_scalar_kind(source);
                bool none = kind == 0 && Native.rp_is_none(source) != 0;
                if (!materialize) return (kind is >= 1 and <= 4 || none, (object?)null, (string?)null);
                return kind switch
                {
                    1 => (true, (object?)ReadExactInteger(source), (string?)null),
                    2 => (true, Native.rp_truth(source) != 0 ? (object)true : false, (string?)null),
                    3 => (true, (object)Native.rp_double(source), (string?)null),
                    4 => (true, (object)ReadExactUnicode(source, false), ReadExactUnicode(source, true)),
                    _ when none => (true, (object)PyNone.Instance, (string?)null),
                    _ => (false, (object?)null, (string?)null),
                };
            }
            finally { Native.rp_decref(source); }
        });
        value = scalar;
        unicodeCodePoints = points;
        return supported;
    }

    private object ReadExactInteger(nint source)
    {
        string text = Host.ReadTextOwned(Host.Check(Native.rp_integer_hex(source)));
        bool negative = text[0] == '-';
        BigInteger number = BigInteger.Parse("0" + text[(negative ? 3 : 2)..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        if (negative) number = -number;
        return number >= long.MinValue && number <= long.MaxValue ? (object)(long)number : number;
    }

    private unsafe string ReadExactUnicode(nint source, bool points)
    {
        long length = Native.rp_unicode_units(source, null, 0, points ? 1 : 0);
        if (length < 0) Host.ThrowError();
        if (length > 0x3FFFFFDF) throw new PyOverflowError("foreign exact string exceeds managed string capacity");
        return string.Create((int)length, (Host, source, points), static (destination, state) =>
        {
            fixed (char* output = destination)
                if (Native.rp_unicode_units(state.source, output, destination.Length, state.points ? 1 : 0) < 0) state.Host.ThrowError();
        });
    }
}
