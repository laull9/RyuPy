// 精确Unicode边界只在两个独立代理码点恰好相邻时建弱旁表；普通CLR字符串保持原表示。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RyuPy.Runtime;

internal static class PyUnicodePoints
{
    private sealed class Boundaries(int[] offsets) { internal readonly int[] Offsets = offsets; }
    private static readonly ConditionalWeakTable<string, Boundaries> SplitPairs = new();
    // 静态码点数据由ldstr保活，同一常量重复求值复用同一Python字符串；动态运算不走此池。
    private static readonly ConditionalWeakTable<string, string> LiteralValues = new();
    internal static string Literal(string points) => LiteralValues.GetValue(points, static value => FromPoints(value));
    // 未出现边界元数据的普通程序只读此标志；既有ldstr/interned实例永远不登记。
    private static int hasMetadata;
    internal static bool IsSplit(string text, int offset) => Volatile.Read(ref hasMetadata) != 0
        && SplitPairs.TryGetValue(text, out Boundaries? value) && Array.BinarySearch(value.Offsets, offset) >= 0;
    internal static bool Has(string text) => Volatile.Read(ref hasMetadata) != 0 && SplitPairs.TryGetValue(text, out _);

    internal static string FromPoints(string points)
    {
        if ((points.Length & 1) != 0) throw new PyValueError("Unicode codepoint units must have even length");
        var text = new StringBuilder(points.Length);
        var splits = new List<int>();
        int previous = -1;
        for (int index = 0; index < points.Length; index += 2)
        {
            int point = (points[index] << 16) | points[index + 1];
            if (point > 0x10ffff) throw new PyValueError("Unicode codepoint out of range");
            if (previous is >= 0xd800 and <= 0xdbff && point is >= 0xdc00 and <= 0xdfff)
                splits.Add(text.Length - 1);
            text.Append(point <= 0xffff ? new string((char)point, 1) : char.ConvertFromUtf32(point));
            previous = point;
        }
        // 显式复制保证不修改CLR intern池或另一个等UTF16字符串的码点身份。
        string result = new(text.ToString().AsSpan());
        if (splits.Count != 0)
        {
            SplitPairs.Add(result, new Boundaries(splits.ToArray()));
            Volatile.Write(ref hasMetadata, 1);
        }
        return result;
    }

    internal static string Points(string text)
    {
        var result = new StringBuilder(text.Length * 2);
        for (int offset = 0; offset < text.Length;)
        {
            int width = PyText.CodePointWidth(text, offset);
            int point = width == 2 ? char.ConvertToUtf32(text, offset) : text[offset];
            result.Append((char)(point >> 16)).Append((char)point);
            offset += width;
        }
        return result.ToString();
    }

    internal static string Substring(string text, int start, int count)
    {
        if (start == 0 && count == text.Length) return text;
        string result = text.Substring(start, count);
        if (!Has(text) || count == 0) return result;
        var points = new StringBuilder(count * 2);
        for (int offset = start; offset < start + count;)
        {
            int width = PyText.CodePointWidth(text, offset);
            int point = width == 2 ? char.ConvertToUtf32(text, offset) : text[offset];
            points.Append((char)(point >> 16)).Append((char)point);
            offset += width;
        }
        return FromPoints(points.ToString());
    }

    internal static string Concat(string left, string right)
    {
        if (left.Length == 0) return right;
        if (right.Length == 0) return left;
        bool boundary = left.Length != 0 && right.Length != 0
            && char.IsHighSurrogate(left[^1]) && char.IsLowSurrogate(right[0]);
        return boundary || Has(left) || Has(right) ? FromPoints(Points(left) + Points(right)) : string.Concat(left, right);
    }

    internal static string Repeat(string text, int count)
    {
        if (count == 0 || text.Length == 0) return string.Empty;
        if (count == 1) return text;
        bool boundary = count > 1 && char.IsHighSurrogate(text[^1]) && char.IsLowSurrogate(text[0]);
        string source = Has(text) || boundary ? Points(text) : text;
        string result = new StringBuilder(source.Length * count).Insert(0, source, count).ToString();
        return ReferenceEquals(source, text) ? result : FromPoints(result);
    }

    internal static string Transfer(string source, string result)
    {
        if (ReferenceEquals(source, result) || Volatile.Read(ref hasMetadata) == 0
            || !SplitPairs.TryGetValue(source, out Boundaries? boundaries)) return result;
        string copy = new(result.AsSpan());
        SplitPairs.Add(copy, boundaries);
        return copy;
    }

    internal static string Join(string separator, string[] pieces)
    {
        if (pieces.Length == 0) return string.Empty;
        if (pieces.Length == 1) return pieces[0];
        bool sensitive = Has(separator) || Edge(separator);
        foreach (string piece in pieces) sensitive |= Has(piece) || Edge(piece);
        if (!sensitive) return string.Join(separator, pieces);
        var result = new StringBuilder();
        string between = Points(separator);
        for (int i = 0; i < pieces.Length; i++)
        { if (i != 0) result.Append(between); result.Append(Points(pieces[i])); }
        return FromPoints(result.ToString());
    }
    private static bool Edge(string text) => text.Length != 0
        && (char.IsLowSurrogate(text[0]) || char.IsHighSurrogate(text[^1]));

    internal static bool Equal(string left, string right) => string.Equals(left, right, StringComparison.Ordinal)
        && (!(Has(left) || Has(right)) || string.Equals(Points(left), Points(right), StringComparison.Ordinal));

    internal static int Compare(string left, string right)
    {
        if (Has(left) || Has(right)) return string.CompareOrdinal(Points(left), Points(right));
        int common = left.AsSpan().CommonPrefixLength(right.AsSpan());
        int start = common;
        if (common > 0 && ((common < left.Length && char.IsSurrogatePair(left, common - 1))
            || (common < right.Length && char.IsSurrogatePair(right, common - 1)))) start--;
        static int Point(string text, int index) => index >= text.Length ? -1
            : index + 1 < text.Length && char.IsSurrogatePair(text, index) ? char.ConvertToUtf32(text, index) : text[index];
        return Point(left, start).CompareTo(Point(right, start));
    }
    internal static string WithSplits(string result, List<int>? offsets)
    {
        if (offsets is null || offsets.Count == 0) return result;
        string copy = new(result.AsSpan());
        SplitPairs.Add(copy, new Boundaries(offsets.ToArray()));
        Volatile.Write(ref hasMetadata, 1);
        return copy;
    }

    internal static void ValidateUtf8(string text)
    {
        int first = text.AsSpan().IndexOfAnyInRange((char)0xd800, (char)0xdfff);
        if (first < 0) return;
        for (int offset = first; offset < text.Length;)
        {
            int width = PyText.CodePointWidth(text, offset);
            if (width == 1 && char.IsSurrogate(text[offset]))
                throw new PyUnicodeEncodeError("'utf-8' codec can't encode surrogate code points");
            offset += width;
        }
    }
}

public static partial class PyOps
{
    /// <summary>静态CIL常量工厂：每个Python码点由两个UTF16单元无损携带。</summary>
    public static string UnicodeFromCodePoints(string points) => PyUnicodePoints.Literal(points);
    /// <summary>外部对象桥使用同一中性码点表示，保持字符串边界。</summary>
    public static string UnicodeCodePoints(string text) => PyUnicodePoints.Points(text);
}
