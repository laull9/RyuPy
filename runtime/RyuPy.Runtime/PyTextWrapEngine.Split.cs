// TextWrapper 可观察分段入口：复用原分词状态机，旧 Wrap/optparse 源区间路径不变。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyTextWrapEngine
{
    internal static string[] SplitText(string text, bool hyphens)
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
        List<Chunk> chunks = Split(points, hyphens);
        var result = new string[chunks.Count];
        for (int index = 0; index < chunks.Count; index++)
        {
            Chunk chunk = chunks[index];
            result[index] = text.Substring(offsets[chunk.Start], offsets[chunk.End] - offsets[chunk.Start]);
        }
        return result;
    }

    // strip 判空与 ASCII 分词不同：NBSP 等 Unicode 空白也属于纯空白块。
    internal static bool WhitespaceOnly(string text)
    {
        for (int offset = 0; offset < text.Length;)
        {
            bool pair = offset + 1 < text.Length && char.IsSurrogatePair(text, offset);
            int point = pair ? char.ConvertToUtf32(text, offset) : text[offset];
            if (!Contains(PyRegexUnicodeData.Space, point)) return false;
            offset += pair ? 2 : 1;
        }
        return true;
    }
}
