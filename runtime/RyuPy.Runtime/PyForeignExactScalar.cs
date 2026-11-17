// foreign精确标量的只读事实与字典内部canonical hash/equality；原key对象始终保留，不参与用户转换协议。
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>provider按真实精确类型读取不可变标量；不得调用用户__index__/__str__/__eq__等协议。</summary>
public interface IPyForeignExactScalar
{
    /// <summary>支持None/bool/int/float/str的精确原生布局；其余类型及子类返回false。</summary>
    bool TryReadExactScalar(out object? value, out string? unicodeCodePoints, bool materialize = true);
}

internal static class PyForeignScalarKey
{
    private sealed class Snapshot(object value, string? points)
    { internal readonly object Value = value; internal readonly string? Points = points; }
    // value不持有reader或外部句柄；只缓存不可变值，不能构成provider双堆强环。
    private static readonly ConditionalWeakTable<IPyForeignExactScalar, Snapshot> Values = new();

    private static bool TrySnapshot(object? key, out Snapshot snapshot)
    {
        IPyForeignExactScalar? reader = key as IPyForeignExactScalar
            ?? (key as PyInstance)?.ForeignInstance as IPyForeignExactScalar;
        if (reader is null) { snapshot = null!; return false; }
        if (Values.TryGetValue(reader, out Snapshot? cached))
        {
            // 缓存不绕过provider存活/域权检查；仅跳过重复的不可变值复制。
            if (!reader.TryReadExactScalar(out _, out _, materialize: false)) { snapshot = null!; return false; }
            snapshot = cached;
            return true;
        }
        if (!reader.TryReadExactScalar(out object? scalar, out string? points)
            || scalar is not (null or PyNone or string or long or int or BigInteger or bool or double))
        { snapshot = null!; return false; }
        object exact = scalar ?? PyNone.Instance;
        snapshot = Values.GetValue(reader, _ => new Snapshot(exact, points));
        return true;
    }

    internal static bool TryRead(object? key, out object value)
    {
        if (TrySnapshot(key, out Snapshot snapshot)) { value = snapshot.Value; return true; }
        value = null!; return false;
    }

    // UTF16 hash允许碰撞；相等性保留CPython实际码点，避免emoji与两个surrogate码点被合并。
    internal static bool Equal(object? left, object? right)
    {
        bool foreignLeft = TrySnapshot(left, out Snapshot a);
        bool foreignRight = TrySnapshot(right, out Snapshot b);
        object? x = foreignLeft ? a.Value : left;
        object? y = foreignRight ? b.Value : right;
        if (x is string xs && y is string ys && (foreignLeft || foreignRight))
            return string.Equals(foreignLeft ? a.Points : UnicodePoints(xs),
                foreignRight ? b.Points : UnicodePoints(ys), System.StringComparison.Ordinal);
        return PyHash.Equal(x, y);
    }

    internal static string UnicodePoints(string text)
    {
        var points = new System.Text.StringBuilder(text.Length * 2);
        for (int index = 0; index < text.Length; index++)
        {
            int scalar = text[index];
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                scalar = char.ConvertToUtf32(text[index], text[++index]);
            points.Append((char)(scalar >> 16)).Append((char)scalar);
        }
        return points.ToString();
    }

    internal static object? Canonical(object? key) => TryRead(key, out object scalar) ? scalar : key;
}
