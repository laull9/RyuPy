using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// <c>str</c> / <c>bytes</c> / <c>bytearray</c> 的方法分派（<see cref="PyOps.CallMethod"/> 的文本部分）。
/// </summary>
/// <remarks>
/// 字符串方法返回的下标、长度按**码点**计算（见 <see cref="PyText"/>），与 CPython 一致；
/// 未实现的方法抛 AttributeError，而不是返回近似结果。
/// </remarks>
public static partial class PyOps
{
    private static object? ByteArrayMethod(PyByteArray array, string name, object?[] args) => name switch
    {
        "startswith" or "endswith" => PyBinaryAffix.Match(array, name, args),
        "translate" => PyBinaryTranslate.Invoke(array, args, [], []),
        "append" => AppendByte(array, args),
        "extend" => ExtendByteArray(array, args),
        "decode" => PyEncoding.Decode([.. array.Bytes], args.Length > 0 ? AsText(args[0]) : "utf-8"),
        _ when PyBinaryClassification.Has(name) => PyBinaryClassification.Invoke(array, name, args.Length, 0),
        _ => throw MissingAttribute("bytearray", name),
    };

    private static object? AppendByte(PyByteArray array, object?[] args)
    {
        array.Append(Single("append", args));
        return PyNone.Instance;
    }

    private static object? ExtendByteArray(PyByteArray array, object?[] args)
    {
        array.Extend(Single("extend", args));
        return PyNone.Instance;
    }

    private static object? BytesMethod(PyBytes bytes, string name, object?[] args) => name switch
    {
        "decode" => bytes.Decode(args.Length > 0 ? AsText(args[0]) : "utf-8"),
        "hex" => Convert.ToHexString(bytes.Bytes).ToLowerInvariant(),
        "translate" => PyBinaryTranslate.Invoke(bytes, args, [], []),
        "startswith" or "endswith" => PyBinaryAffix.Match(bytes, name, args),
        _ when PyBinaryClassification.Has(name) => PyBinaryClassification.Invoke(bytes, name, args.Length, 0),
        _ => throw MissingAttribute("bytes", name),
    };

    private static object? StringMethod(string text, string name, object?[] args) => name switch
    {
        "encode" => new PyBytes(PyEncoding.Encode(text, args.Length > 0 ? AsText(args[0]) : "utf-8")),
        "upper" => text.ToUpperInvariant(),
        "lower" => text.ToLowerInvariant(),
        "strip" => StripText(text, args, left: true, right: true),
        "lstrip" => StripText(text, args, left: true, right: false),
        "rstrip" => StripText(text, args, left: false, right: true),
        "split" => PyTextSplit.Invoke(text, args, [], []),
        "isspace" => PyUnicodeWhitespace.Invoke(text, args, []),
        "splitlines" => SplitLines(text, args),
        "join" => JoinText(text, Single("join", args)),
        "replace" => ReplaceText(text, args),
        "count" or "find" or "rfind" or "index" or "rindex" => PyTextSearch.Invoke(text, name, args, []),
        "startswith" or "endswith" => PyTextAffix.Match(text, name, args, [], qualified: false),
        _ => throw MissingAttribute("str", name),
    };

    /// <summary>按Python定义的Unicode换行边界切分，末尾分隔符不额外产生空行。</summary>
    private static object SplitLines(string text, object?[] args)
    {
        if (args.Length > 1) throw new PyTypeError("splitlines expected at most 1 argument");
        bool keep = args.Length > 0 && Truthy(args[0]);
        var result = new PyList();
        int start = 0;
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] is not ('\n' or '\r' or '\v' or '\f' or '\u001c' or '\u001d' or '\u001e' or '\u0085' or '\u2028' or '\u2029')) continue;
            int end = index;
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
            result.Append(text[start..(keep ? index + 1 : end)]);
            start = index + 1;
        }
        if (start < text.Length) result.Append(text[start..]);
        return result;
    }

    /// <summary>`str.strip([chars])`：缺省去掉空白，给定时去掉属于 chars 的字符。</summary>
    private static object StripText(string text, object?[] args, bool left, bool right)
    {
        string? chars = args.Length > 0 && args[0] is not PyNone ? AsText(args[0]) : null;
        int start = 0;
        int end = text.Length;
        if (left)
        {
            while (start < end && IsStrippable(text[start], chars))
            {
                start++;
            }
        }
        if (right)
        {
            while (end > start && IsStrippable(text[end - 1], chars))
            {
                end--;
            }
        }
        return text[start..end];
    }

    private static bool IsStrippable(char ch, string? chars) =>
        chars is null ? PyUnicodeWhitespace.Is(ch) : chars.Contains(ch, StringComparison.Ordinal);

    /// <summary>
    /// `str.replace(old, new[, count])`。.NET 的 `Replace` 总是替换全部，
    /// 带 count 时必须自己按出现位置逐个替换。
    /// </summary>
    private static object ReplaceText(string text, object?[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            throw new PyTypeError($"replace expected 2 or 3 arguments, got {args.Length}");
        }
        string oldValue = AsText(args[0]);
        string newValue = AsText(args[1]);
        long count = args.Length == 3 ? ToIndex(args[2], "count") : -1;
        if (count < 0)
        {
            return text.Replace(oldValue, newValue, StringComparison.Ordinal);
        }
        if (oldValue.Length == 0)
        {
            // 空串的替换语义比较特殊；当前子集明确拒绝而不是给出错误结果
            throw new PyValueError("replace() 暂不支持空字符串作为 old");
        }
        var builder = new StringBuilder();
        int start = 0;
        long replaced = 0;
        while (replaced < count)
        {
            int index = text.IndexOf(oldValue, start, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }
            builder.Append(text, start, index - start);
            builder.Append(newValue);
            start = index + oldValue.Length;
            replaced++;
        }
        builder.Append(text, start, text.Length - start);
        return builder.ToString();
    }

    private static object JoinText(string separator, object? value)
    {
        object?[] items = ToSequence(value);
        var parts = new string[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            parts[i] = items[i] is string s
                ? s : items[i] is PyStringInstance subtext ? subtext.Value
                : throw new PyTypeError("sequence item: expected str instance");
        }
        return string.Join(separator, parts);
    }

    private static string AsText(object? value) =>
        value is string text ? text : value is PyStringInstance subtext ? subtext.Value
        : throw new PyTypeError($"expected str, got '{PyRepr.TypeName(value)}'");
}
