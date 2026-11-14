// 通用模板的流式词法与十进制字段解析；每次只完成一个外层字段，路径语义由执行器逐段推进。
// UTF16范围保留原文本；转换字符及Nd数字按Python码点读取，不替换孤立代理。
using System;

namespace RyuPy.Runtime;

internal static class PyStringTemplateParser
{
    internal struct Part
    {
        internal int LiteralStart, LiteralLength;
        internal bool HasField, ExpandSpec, Terminal;
        internal int NameStart, NameEnd, SpecStart, SpecEnd, Conversion;
    }

    internal static bool Next(string text, ref int cursor, int end, out Part part)
    {
        part = default;
        if (cursor >= end) return false;
        int start = cursor;
        while (cursor < end && text[cursor] is not ('{' or '}')) cursor++;
        if (cursor == end)
        {
            part.LiteralStart = start; part.LiteralLength = end - start; part.Terminal = true;
            return true;
        }
        char brace = text[cursor++];
        if (cursor < end && text[cursor] == brace)
        {
            // 返回第一枚转义括号所覆盖的原范围，第二枚仅从输入游标跳过。
            part.LiteralStart = start; part.LiteralLength = cursor - start;
            cursor++; part.Terminal = cursor == end;
            return true;
        }
        if (brace == '}') throw Syntax("Single '}' encountered in format string");
        if (cursor == end) throw Syntax("Single '{' encountered in format string");
        part.LiteralStart = start; part.LiteralLength = cursor - start - 1;
        part.HasField = true;
        ParseField(text, ref cursor, end, ref part);
        part.Terminal = cursor == end;
        return true;
    }

    private static void ParseField(string text, ref int cursor, int end, ref Part part)
    {
        part.NameStart = cursor;
        int delimiter = -1;
        while (cursor < end)
        {
            char c = text[cursor++];
            if (c == '{') throw Syntax("unexpected '{' in field name");
            if (c == '[')
            {
                // []内容不是模板语法，直到第一枚]均保持原样，允许! : { } [。
                while (cursor < end && text[cursor] != ']') cursor++;
                continue;
            }
            if (c is '}' or ':' or '!') { delimiter = c; break; }
        }
        part.NameEnd = cursor - 1;
        if (delimiter == '}') return;
        if (delimiter < 0) throw Syntax("expected '}' before end of string");
        if (delimiter == '!')
        {
            if (cursor >= end) throw Syntax("end of string while looking for conversion specifier");
            part.Conversion = Point(text, ref cursor, end);
            if (cursor < end)
            {
                char following = text[cursor++];
                if (following == '}') return;
                if (following != ':') throw Syntax("expected ':' after conversion specifier");
            }
        }
        part.SpecStart = cursor;
        int depth = 1;
        while (cursor < end)
        {
            char c = text[cursor++];
            if (c == '{') { part.ExpandSpec = true; depth++; }
            else if (c == '}' && --depth == 0)
            {
                part.SpecEnd = cursor - 1;
                return;
            }
        }
        throw Syntax("unmatched '{' in format spec");
    }

    // 返回-1代表非数值或空字段；溢出在发现它的码点立即报错，不预扫描尾部是否有字母。
    internal static long Index(string text, int start, int end)
    {
        if (start == end) return -1;
        long value = 0;
        while (start < end)
        {
            int digit = Decimal(Point(text, ref start, end));
            if (digit < 0) return -1;
            if (value > (long.MaxValue - digit) / 10)
                throw Syntax("Too many decimal digits in format string");
            value = value * 10 + digit;
        }
        return value;
    }

    internal static int Point(string text, ref int index, int end)
    {
        char first = text[index++];
        if (char.IsHighSurrogate(first) && index < end && char.IsLowSurrogate(text[index]))
            return char.ConvertToUtf32(first, text[index++]);
        return first;
    }

    // 复用项目固定Unicode15 Nd数据口径，局部只读表避免扩动已验收值格式器/整数接口。
    private static readonly int[] DecimalZeros = [
        0x30, 0x660, 0x6f0, 0x7c0, 0x966, 0x9e6, 0xa66, 0xae6, 0xb66, 0xbe6,
        0xc66, 0xce6, 0xd66, 0xde6, 0xe50, 0xed0, 0xf20, 0x1040, 0x1090, 0x17e0,
        0x1810, 0x1946, 0x19d0, 0x1a80, 0x1a90, 0x1b50, 0x1bb0, 0x1c40, 0x1c50, 0xa620,
        0xa8d0, 0xa900, 0xa9d0, 0xa9f0, 0xaa50, 0xabf0, 0xff10, 0x104a0, 0x10d30, 0x11066,
        0x110f0, 0x11136, 0x111d0, 0x112f0, 0x11450, 0x114d0, 0x11650, 0x116c0, 0x11730, 0x118e0,
        0x11950, 0x11c50, 0x11d50, 0x11da0, 0x11f50, 0x16a60, 0x16ac0, 0x16b50, 0x1d7ce, 0x1d7d8,
        0x1d7e2, 0x1d7ec, 0x1d7f6, 0x1e140, 0x1e2f0, 0x1e4f0, 0x1e950, 0x1fbf0,
    ];
    private static int Decimal(int point)
    {
        if (point < 128) return point >= '0' && point <= '9' ? point - '0' : -1;
        int low = 0, high = DecimalZeros.Length - 1;
        while (low <= high)
        {
            int mid = low + (high - low) / 2, zero = DecimalZeros[mid];
            if (point < zero) high = mid - 1;
            else if (point >= zero + 10) low = mid + 1;
            else return point - zero;
        }
        return -1;
    }

    internal static PyValueError Syntax(string message) => PyStringTemplate.Error(new PyValueError(message));
}
