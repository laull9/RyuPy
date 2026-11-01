using System;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>内部16B浮点槽：引用与数值不重叠；fresh值用哨兵，观察后缓存唯一原盒。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PyNumericSlot
{
    private static readonly object FreshFloat = new();
    private object _reference;
    private readonly long _bits;

    private PyNumericSlot(object reference, double value)
    {
        _reference = reference;
        _bits = BitConverter.DoubleToInt64Bits(value);
    }

    internal static PyNumericSlot Fresh(double value) => new(FreshFloat, value);
    internal static PyNumericSlot Existing(object box, double value) => new(box, value);
    internal double Number => _reference is null
        ? throw new PyRuntimeError("uninitialized numeric list slot")
        : BitConverter.Int64BitsToDouble(_bits);

    internal object Materialize()
    {
        if (_reference is null) throw new PyRuntimeError("uninitialized numeric list slot");
        if (ReferenceEquals(_reference, FreshFloat)) _reference = Number;
        return _reference;
    }
}
