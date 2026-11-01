using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>换行引擎：源区间保存词块，固定Unicode15分类，只有规范化和输出行生成字符串。</summary>
internal static class PyTextWrapEngine
{
    private readonly record struct Chunk(int Start, int End)
    {
        internal int Length => End - Start;
    }

    internal static string Normalize(string text, bool expand, bool replace, long tabsize)
    {
        if (!expand && !replace) return text;
        var output = new StringBuilder(text.Length);
        long column = 0;
        for (int offset = 0; offset < text.Length;)
        {
            int units = offset + 1 < text.Length && char.IsSurrogatePair(text, offset) ? 2 : 1;
            int cp = units == 2 ? char.ConvertToUtf32(text, offset) : text[offset];
            if (expand && cp == '\t')
            {
                long spaces = tabsize <= 0 ? 0 : tabsize - column % tabsize;
                if (spaces > PyInt.MaxBitShift || output.Length + spaces > int.MaxValue)
                    throw new PyNotImplementedError("textwrap tab expansion exceeds the runtime resource limit");
                output.Append(' ', (int)spaces); column += spaces;
            }
            else
            {
                if (replace && AsciiSpace(cp)) output.Append(' ');
                else output.Append(text, offset, units);
                column = cp is '\n' or '\r' ? 0 : column + 1;
            }
            offset += units;
        }
        return output.ToString();
    }

    internal static List<string> Wrap(string text, long width, string first, string rest,
        bool breakWords, bool dropSpace, bool breakHyphens)
    {
        var points = new List<int>(); var offsets = new List<int>();
        for (int offset = 0; offset < text.Length;)
        {
            offsets.Add(offset);
            bool pair = offset + 1 < text.Length && char.IsSurrogatePair(text, offset);
            points.Add(pair ? char.ConvertToUtf32(text, offset) : text[offset]);
            offset += pair ? 2 : 1;
        }
        offsets.Add(text.Length);
        List<Chunk> chunks = Split(points, breakHyphens);
        var lines = new List<string>();
        int next = 0;
        while (next < chunks.Count)
        {
            string indent = lines.Count == 0 ? first : rest;
            long available = width - PyText.Length(indent);
            if (dropSpace && lines.Count != 0 && Space(chunks[next], points)) next++;
            var line = new List<Chunk>(); long used = 0;
            while (next < chunks.Count && used + chunks[next].Length <= available)
            {
                Chunk chunk = chunks[next++]; line.Add(chunk); used += chunk.Length;
            }
            if (next < chunks.Count && chunks[next].Length > available)
            {
                Chunk chunk = chunks[next];
                if (breakWords)
                {
                    int room = (int)Math.Min(int.MaxValue, available < 1 ? 1 : Math.Max(0, available - used));
                    if (room >= 0)
                    {
                        // 空间恰好耗尽时保留空片段占位，尾空白删除只作用于该片段。
                        int length = Math.Min(room, chunk.Length);
                        if (breakHyphens && chunk.Length > length)
                        {
                            // 剩余空间内最后一个可用连字符；开头全是连字符时仍按长度前进。
                            bool prefixHasOther = false; int cut = 0;
                            for (int at = chunk.Start; at < chunk.Start + length; at++)
                            {
                                if (points[at] == '-' && prefixHasOther) cut = at - chunk.Start + 1;
                                if (points[at] != '-') prefixHasOther = true;
                            }
                            if (cut > 0) length = cut;
                        }
                        line.Add(new Chunk(chunk.Start, chunk.Start + length)); used += length;
                        if (length == chunk.Length) next++;
                        else chunks[next] = new Chunk(chunk.Start + length, chunk.End);
                    }
                }
                else if (line.Count == 0) { line.Add(chunk); next++; }
            }
            if (dropSpace && line.Count != 0 && Space(line[^1], points)) line.RemoveAt(line.Count - 1);
            if (line.Count == 0) continue;
            var output = new StringBuilder(indent);
            foreach (Chunk chunk in line) output.Append(text, offsets[chunk.Start], offsets[chunk.End] - offsets[chunk.Start]);
            lines.Add(output.ToString());
        }
        return lines;
    }

    private static List<Chunk> Split(List<int> points, bool hyphens)
    {
        var chunks = new List<Chunk>(); int start = 0;
        while (start < points.Count)
        {
            bool space = AsciiSpace(points[start]); int end = start + 1;
            while (end < points.Count && AsciiSpace(points[end]) == space) end++;
            if (space || !hyphens) { chunks.Add(new Chunk(start, end)); start = end; continue; }
            int chunkStart = start;
            for (int at = start; at < end; at++)
            {
                if (points[at] != '-') continue;
                int stop = at + 1; while (stop < end && points[stop] == '-') stop++;
                if (stop - at >= 2 && at > start && stop < end && WordPunctuation(points[at - 1]) && Word(points[stop]))
                {
                    if (at > chunkStart) chunks.Add(new Chunk(chunkStart, at));
                    chunks.Add(new Chunk(at, stop)); chunkStart = stop; at = stop - 1; continue;
                }
                // 连续分隔线即使不产生词间分段，也只扫描一次，避免后缀反复扫描成平方复杂度。
                if (stop - at >= 2) { at = stop - 1; continue; }
                bool left = at >= start + 2 && Letter(points[at - 1]) && Letter(points[at - 2]) ||
                    at >= start + 3 && Letter(points[at - 1]) && points[at - 2] == '-' && Letter(points[at - 3]);
                int right = at + 2 < end && points[at + 2] == '-' ? at + 3 : at + 2;
                if (stop == at + 1 && left && at + 1 < end && Letter(points[at + 1]) && right < end && Letter(points[right]))
                {
                    chunks.Add(new Chunk(chunkStart, at + 1)); chunkStart = at + 1;
                }
            }
            if (chunkStart < end) chunks.Add(new Chunk(chunkStart, end));
            start = end;
        }
        return chunks;
    }

    private static bool AsciiSpace(int cp) => cp is ' ' or '\t' or '\n' or '\r' or '\v' or '\f';
    private static bool Word(int cp) => Contains(PyRegexUnicodeData.Word, cp);
    private static bool Letter(int cp) => Word(cp) && !Contains(PyRegexUnicodeData.Digit, cp);
    private static bool WordPunctuation(int cp) => Word(cp) || cp is '!' or '"' or '\'' or '&' or '.' or ',' or '?';
    private static bool Space(Chunk chunk, List<int> points)
    {
        for (int at = chunk.Start; at < chunk.End; at++) if (!Contains(PyRegexUnicodeData.Space, points[at])) return false;
        return true;
    }
    private static bool Contains(int[] pairs, int cp)
    {
        int low = 0, high = pairs.Length / 2 - 1;
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (cp < pairs[2 * mid]) high = mid - 1;
            else if (cp > pairs[2 * mid + 1]) low = mid + 1;
            else return true;
        }
        return false;
    }
}
