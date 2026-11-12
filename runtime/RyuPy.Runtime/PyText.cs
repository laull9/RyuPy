using System;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>str</c> 的码点语义工具。
/// </summary>
/// <remarks>
/// # 为什么需要这一层
///
/// CLR 的 <see cref="string"/> 以 UTF-16 存储，<c>Length</c> / 下标 / 切片都按 **code unit**
/// 计数；Python 的 <c>str</c> 按 **code point** 计数。二者只在 BMP 之外（emoji、部分 CJK
/// 扩展、音乐符号等）有差异：<c>"😀"</c> 在 C# 里是 2 个 <c>char</c>，在 Python 里是 1 个字符。
///
/// 直接沿用 <c>string.Length</c> 会让 <c>len(s)</c>、<c>s[0]</c>、<c>s[1:]</c> 与
/// 迭代同时出错，而且错得很隐蔽（输出乱码而不是异常）。因此所有对用户可见的字符级
/// 操作都经过本文件：
/// <list type="bullet">
/// <item><see cref="Length"/>：码点个数；</item>
/// <item><see cref="At"/>：第 i 个码点（返回新的 <c>str</c>）；</item>
/// <item><see cref="Slice"/>：按码点切片；</item>
/// <item><see cref="Items"/>：按码点拆成迭代序列。</item>
/// </list>
///
/// 孤立代理项（lone surrogate）在 CPython 里是合法的码点，这里也按「1 个码点」处理，
/// 不做替换，保持与内存中的字符串内容一致。
/// </remarks>
public static class PyText
{
    /// <summary>码点个数（<c>len(s)</c>）。</summary>
    /// <param name="text">字符串。</param>
    public static long Length(string text)
    {
        // BMP（含孤立低代理项）一单元即一码点；用BCL向量搜索跳过整段普通文本。
        // 只对真正的高低代理配对减一，不将孤立代理替换成Rune或改变Python语义。
        ReadOnlySpan<char> remaining = text.AsSpan();
        int pairs = 0;
        int high;
        while ((high = remaining.IndexOfAnyInRange('\ud800', '\udbff')) >= 0)
        {
            int consumed = high + 1;
            if (consumed < remaining.Length && char.IsLowSurrogate(remaining[consumed]))
            {
                pairs++;
                consumed++;
            }
            remaining = remaining[consumed..];
        }
        return text.Length - pairs;
    }

    /// <summary>把码点下标换算成 CLR 字符偏移；越界返回 -1。</summary>
    /// <param name="text">字符串。</param>
    /// <param name="index">码点下标（调用方已处理负数）。</param>
    public static int OffsetOf(string text, long index)
    {
        if (index < 0)
        {
            return -1;
        }
        long remaining = index;
        int offset = 0;
        while (offset < text.Length)
        {
            if (remaining == 0)
            {
                return offset;
            }
            offset += CodePointWidth(text, offset);
            remaining--;
        }
        return remaining == 0 ? offset : -1;
    }

    /// <summary>取第 <paramref name="index"/> 个码点；越界抛 <c>IndexError</c>。</summary>
    /// <param name="text">字符串。</param>
    /// <param name="index">码点下标（支持负数）。</param>
    public static string At(string text, long index)
    {
        long length = Length(text);
        long normalized = index < 0 ? index + length : index;
        if (normalized < 0 || normalized >= length)
        {
            throw new PyIndexError("string index out of range");
        }
        int offset = OffsetOf(text, normalized);
        int width = CodePointWidth(text, offset);
        return text.Substring(offset, width);
    }

    /// <summary>按码点切片。</summary>
    /// <param name="text">字符串。</param>
    /// <param name="slice">切片对象。</param>
    public static string Slice(string text, PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Length(text));
        if (count == 0)
        {
            return string.Empty;
        }
        // 先换算成字符偏移，再逐个码点追加；步长为 1 时可以直接一次 Substring。
        int first = OffsetOf(text, start);
        if (step == 1)
        {
            int last = OffsetOf(text, start + count);
            return text.Substring(first, last - first);
        }
        var builder = new StringBuilder((int)Math.Min(count * 2, int.MaxValue));
        long logical = start;
        for (long i = 0; i < count; i++, logical += step)
        {
            int offset = OffsetOf(text, logical);
            builder.Append(text, offset, CodePointWidth(text, offset));
        }
        return builder.ToString();
    }

    /// <summary>按码点拆成对象序列（迭代、<c>list(s)</c> 共用）。</summary>
    /// <param name="text">字符串。</param>
    public static object?[] Items(string text)
    {
        var items = new object?[Length(text)];
        int offset = 0;
        int i = 0;
        while (offset < text.Length)
        {
            int width = CodePointWidth(text, offset);
            items[i++] = text.Substring(offset, width);
            offset += width;
        }
        return items;
    }

    /// <summary>
    /// 从 <paramref name="offset"/> 读一个码点并前进到下一个。
    /// </summary>
    /// <remarks>
    /// 迭代器专用：把「字符偏移」保存在调用方，避免每取一个字符都从头扫描
    /// （否则迭代整个字符串会退化成 O(n²)）。
    /// </remarks>
    /// <param name="text">字符串。</param>
    /// <param name="offset">字符偏移，读完后前进一个码点的宽度。</param>
    public static string ReadAt(string text, ref int offset)
    {
        int width = CodePointWidth(text, offset);
        string value = text.Substring(offset, width);
        offset += width;
        return value;
    }

    /// <summary>
    /// 该位置的码点占几个 UTF-16 code unit。
    /// </summary>
    /// <remarks>
    /// 只有「高代理 + 低代理」这一种组合是 2；孤立代理项（前后不配对）按 1 处理，
    /// 这样切片与下标不会把两个不相干的字符粘在一起。
    /// </remarks>
    private static int CodePointWidth(string text, int offset)
    {
        char ch = text[offset];
        if (char.IsHighSurrogate(ch) && offset + 1 < text.Length && char.IsLowSurrogate(text[offset + 1]))
        {
            return 2;
        }
        return 1;
    }
}
