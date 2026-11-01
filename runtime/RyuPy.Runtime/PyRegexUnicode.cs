using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>标量字符集合翻译为UTF16原子，避免点号/量词拆开补充字符；分类来自固定Python表。</summary>
internal sealed class PyRegexSet
{
    internal readonly List<(int Low, int High)> Ranges = [];
    private static readonly int[][] CaseGroups = BuildCases();
    internal PyRegexSet Add(int low, int high) { Ranges.Add((low, high)); return this; }
    internal PyRegexSet Add(PyRegexSet other) { Ranges.AddRange(other.Ranges); return this; }
    internal static PyRegexSet Table(int[] pairs)
    {
        var result = new PyRegexSet();
        for (int index = 0; index < pairs.Length; index += 2) result.Add(pairs[index], pairs[index + 1]);
        return result;
    }
    internal bool Contains(int value) => Ranges.Any(range => value >= range.Low && value <= range.High);
    internal PyRegexSet Normalize()
    {
        var sorted = Ranges.OrderBy(range => range.Low).ToArray(); Ranges.Clear();
        foreach (var range in sorted)
        {
            if (Ranges.Count > 0 && range.Low <= Ranges[^1].High + 1)
                Ranges[^1] = (Ranges[^1].Low, Math.Max(range.High, Ranges[^1].High));
            else Ranges.Add(range);
        }
        return this;
    }
    internal PyRegexSet Negate()
    {
        Normalize(); var result = new PyRegexSet(); int start = 0;
        foreach (var range in Ranges) { if (start < range.Low) result.Add(start, range.Low - 1); start = range.High + 1; }
        if (start <= 0x10ffff) result.Add(start, 0x10ffff);
        return result;
    }
    internal PyRegexSet IgnoreCase(bool ascii)
    {
        var extra = new List<int>();
        foreach (int[] group in CaseGroups)
            if (group.Any(value => (!ascii || value < 128) && Contains(value)))
                extra.AddRange(group.Where(value => !ascii || value < 128));
        foreach (int value in extra) Add(value, value);
        return Normalize();
    }
    internal string Emit()
    {
        Normalize(); var branches = new List<string>();
        foreach (var range in Ranges)
        {
            if (range.Low <= 0xffff) branches.Add(UnitRange(range.Low, Math.Min(range.High, 0xffff)));
            int low = Math.Max(range.Low, 0x10000), high = range.High;
            if (low > high) continue;
            int firstHigh = 0xd800 + ((low - 0x10000) >> 10), lastHigh = 0xd800 + ((high - 0x10000) >> 10);
            int firstLow = 0xdc00 + ((low - 0x10000) & 1023), lastLow = 0xdc00 + ((high - 0x10000) & 1023);
            if (firstHigh == lastHigh) branches.Add(UnitRange(firstHigh, firstHigh) + UnitRange(firstLow, lastLow));
            else
            {
                branches.Add(UnitRange(firstHigh, firstHigh) + UnitRange(firstLow, 0xdfff));
                if (firstHigh + 1 <= lastHigh - 1) branches.Add(UnitRange(firstHigh + 1, lastHigh - 1) + "[\\uDC00-\\uDFFF]");
                branches.Add(UnitRange(lastHigh, lastHigh) + UnitRange(0xdc00, lastLow));
            }
        }
        // 匹配起止都必须在码点边界；保留孤立代理码元，但不让有效代理对被拆开。
        string boundary = "(?!(?<=[\\uD800-\\uDBFF])[\\uDC00-\\uDFFF])";
        return "(?:" + boundary + "(?:" + (branches.Count == 0 ? "(?!)" : string.Join('|', branches)) + ")" + boundary + ")";
    }
    private static string UnitRange(int low, int high) => low == high ? $"\\u{low:X4}" : $"[\\u{low:X4}-\\u{high:X4}]";
    private static int[][] BuildCases()
    {
        var parents = new Dictionary<int, int>();
        int Root(int value)
        {
            if (!parents.TryGetValue(value, out int parent)) { parents[value] = value; return value; }
            return parent == value ? value : parents[value] = Root(parent);
        }
        int[] pairs = PyRegexUnicodeData.CasePairs;
        for (int index = 0; index < pairs.Length; index += 2) parents[Root(pairs[index])] = Root(pairs[index + 1]);
        var groups = new Dictionary<int, List<int>>();
        foreach (int value in parents.Keys.ToArray())
        {
            int root = Root(value);
            if (!groups.TryGetValue(root, out var group)) groups[root] = group = [];
            group.Add(value);
        }
        return groups.Values.Select(group => group.ToArray()).ToArray();
    }
}

/// <summary>每次匹配操作一次建立码点边界；所有公开pos/span使用Python码点，而非UTF16索引。</summary>
internal sealed class PyRegexText
{
    internal string Text { get; }
    internal int[] Offsets { get; }
    internal int Length => Offsets.Length - 1;
    internal PyRegexText(string text)
    {
        Text = text; var offsets = new List<int>();
        for (int index = 0; index < text.Length;)
        {
            offsets.Add(index);
            index += char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
        }
        offsets.Add(text.Length); Offsets = offsets.ToArray();
    }
    internal int Point(int offset)
    {
        int point = Array.BinarySearch(Offsets, offset);
        if (point < 0) throw new PyRuntimeError("regular expression split a Unicode code point");
        return point;
    }
}
