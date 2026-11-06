// 模板转换先保真实对象，再交值级format：ASCII的!a保repr对象，非ASCII才产生精确转义字符串。
// 不复用旧f-string的UTF16-char转义；转换只执行一次用户槽，不改已有%或repr接口。
using System;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyStringTemplateConversions
{
    internal static object? Convert(object? value, int conversion) => conversion switch
    {
        0 => value,
        's' => PyRepr.StrObject(value),
        'r' => PyRepr.ReprObject(value),
        'a' => Ascii(value),
        _ => throw PyStringTemplateParser.Syntax("Unknown conversion specifier " +
            (conversion <= char.MaxValue ? ((char)conversion).ToString() : char.ConvertFromUtf32(conversion))),
    };

    private static object Ascii(object? value)
    {
        object representation = PyRepr.ReprObject(value);
        string text = PyStringLayout.RequireRaw(representation);
        bool nonAscii = false;
        long length = 0;
        int cursor = 0;
        // 先检查实际容量，避免builder扩容途中才发现理论结果超出CLR表示上限。
        while (cursor < text.Length)
        {
            int point = PyStringTemplateParser.Point(text, ref cursor, text.Length);
            if (point < 128) length++;
            else
            {
                nonAscii = true;
                length += point <= 255 ? 4 : point <= 65535 ? 6 : 10;
            }
            PyStringTemplateWriter.CheckCapacity(length);
        }
        if (!nonAscii) return representation;
        var builder = new StringBuilder((int)length);
        cursor = 0;
        while (cursor < text.Length)
        {
            int point = PyStringTemplateParser.Point(text, ref cursor, text.Length);
            if (point < 128) { builder.Append((char)point); continue; }
            builder.Append(point <= 255 ? "\\x" : point <= 65535 ? "\\u" : "\\U");
            builder.Append(point.ToString(point <= 255 ? "x2" : point <= 65535 ? "x4" : "x8", CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }
}
