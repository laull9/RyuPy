// float 文本先验完整 Python 数字语法再调用 CLR 舍入；固定 Unicode15 规范化复用 int 冷工具。
using System;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>str、bytes、bytearray 的严格十进制浮点文本解析。</summary>
public static class PyFloatText
{
    /// <summary>解析实际文本；自抛语法错误记录调用现场，不增加任意数字长度限制。</summary>
    public static object Parse(object original)
    {
        string text = original switch
        {
            string unicode => PyIntText.NormalizeUnicode(unicode),
            PyBytes bytes => Encoding.Latin1.GetString(bytes.Bytes),
            PyByteArray bytes => Encoding.Latin1.GetString(bytes.Bytes.ToArray()),
            _ => throw new PyRuntimeError("invalid float text parser input"),
        };
        int begin = 0, end = text.Length;
        while (begin < end && Space(text[begin])) begin++;
        while (end > begin && Space(text[end - 1])) end--;
        int index = begin;
        bool negative = index < end && text[index] == '-';
        if (index < end && (text[index] is '+' or '-')) index++;
        ReadOnlySpan<char> unsigned = text.AsSpan(index, end - index);
        if (unsigned.Equals("inf", StringComparison.OrdinalIgnoreCase) || unsigned.Equals("infinity", StringComparison.OrdinalIgnoreCase))
            return negative ? double.NegativeInfinity : double.PositiveInfinity;
        if (unsigned.Equals("nan", StringComparison.OrdinalIgnoreCase))
            return BitConverter.Int64BitsToDouble(negative ? unchecked((long)0xfff8000000000000UL) : 0x7ff8000000000000L);
        bool before = Digits(text, ref index, end);
        bool after = false;
        if (index < end && text[index] == '.') { index++; after = Digits(text, ref index, end); }
        if (!before && !after) throw Invalid(original);
        if (index < end && (text[index] is 'e' or 'E'))
        {
            index++;
            if (index < end && (text[index] is '+' or '-')) index++;
            if (!Digits(text, ref index, end)) throw Invalid(original);
        }
        if (index != end) throw Invalid(original);
        ReadOnlySpan<char> valid = text.AsSpan(begin, end - begin);
        // 正常 ASCII 路径不 Trim/ToLower/复制；只有合法下划线才需要去除副本。
        string? cleaned = null;
        if (valid.IndexOf('_') >= 0)
        {
            var builder = new StringBuilder(valid.Length);
            foreach (char value in valid) if (value != '_') builder.Append(value);
            cleaned = builder.ToString(); valid = cleaned.AsSpan();
        }
        if (double.TryParse(valid, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)) return result;
        // 完整语法已合法，不将后端转换限制谎报为 Python ValueError 或猜测 infinity。
        throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("float(): valid decimal conversion exceeds the runtime parser capability"));
    }

    private static bool Digits(string text, ref int index, int end)
    {
        if (index == end || !Digit(text[index])) return false;
        index++;
        while (index < end)
        {
            if (Digit(text[index])) { index++; continue; }
            if (text[index] == '_' && index + 1 < end && Digit(text[index + 1])) { index += 2; continue; }
            break;
        }
        return true;
    }
    private static bool Digit(char value) => value is >= '0' and <= '9';
    private static bool Space(char value) => value == ' ' || value is >= '\t' and <= '\r';
    private static PyBaseException Invalid(object original) => (PyBaseException)PyErrors.PrepareRaise(
        new PyValueError($"could not convert string to float: {PyRepr.Repr(original)}"));
}
