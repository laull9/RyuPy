// URL 百分号编码与查询多值映射；按字节处理转义，保留畸形百分号和重复键顺序。
using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyUrlCodec
{
    private static Encoding Codec(object? encoding, object? errors, string fallback)
    {
        string name = PyUrlParse.None(encoding) ? "utf-8" : PyUrlParse.Text(encoding);
        string policy = PyUrlParse.None(errors) ? fallback : PyUrlParse.Text(errors);
        if (policy is not ("strict" or "replace" or "ignore")) throw new PyNotImplementedError("URL codec errors supports strict, replace, ignore only");
        int codepage = name.ToLowerInvariant().Replace("_", "-") switch
        { "utf-8" or "utf8" => 65001, "ascii" or "us-ascii" => 20127, "latin-1" or "latin1" or "iso-8859-1" => 28591,
            _ => throw new PyLookupError("unknown encoding: " + name) };
        return Encoding.GetEncoding(codepage,
            policy == "strict" ? EncoderFallback.ExceptionFallback : new EncoderReplacementFallback(policy == "ignore" ? "" : "?"),
            policy == "strict" ? DecoderFallback.ExceptionFallback : new DecoderReplacementFallback(policy == "ignore" ? "" : "\ufffd"));
    }

    internal static string Quote(object? value, object? safe, object? encoding, object? errors, bool plus)
    {
        byte[] bytes;
        try
        {
            if (value is PyBytes binary)
            {
                if (!PyUrlParse.None(encoding) || !PyUrlParse.None(errors)) throw new PyTypeError("quote() doesn't support 'encoding' for bytes");
                bytes = binary.Bytes;
            }
            else bytes = Codec(encoding, errors, "strict").GetBytes(PyUrlParse.Text(value));
        }
        catch (EncoderFallbackException) { throw new PyUnicodeEncodeError("URL encoding failed"); }
        string allowed = safe is PyBytes safeBytes ? Encoding.Latin1.GetString(safeBytes.Bytes) : PyUrlParse.Text(safe);
        var result = new StringBuilder();
        foreach (byte item in bytes)
        {
            char character = (char)item;
            if (item < 128 && (char.IsAsciiLetterOrDigit(character) || "_.-~".Contains(character) || allowed.Contains(character))) result.Append(character);
            else if (plus && item == 32) result.Append('+');
            else result.Append('%').Append(item.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        }
        return result.ToString();
    }

    internal static string Unquote(object? value, object? encoding, object? errors, bool plus)
    {
        if (plus && value is not string) throw new PyTypeError("unquote_plus requires str");
        string text = value is PyBytes binary ? Encoding.Latin1.GetString(binary.Bytes) : PyUrlParse.Text(value);
        if (plus) text = text.Replace('+', ' ');
        var result = new StringBuilder(); var bytes = new List<byte>();
        // 非 ASCII 字符保持原文；仅连续 ASCII/转义字节进入解码器。
        void Flush()
        {
            if (bytes.Count == 0) return;
            try { result.Append(Codec(encoding, errors, "replace").GetString(bytes.ToArray())); }
            catch (DecoderFallbackException) { throw new PyUnicodeDecodeError("URL decoding failed"); }
            bytes.Clear();
        }
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] == '%' && index + 2 < text.Length && Hex(text[index + 1]) >= 0 && Hex(text[index + 2]) >= 0)
            { bytes.Add((byte)(Hex(text[index + 1]) * 16 + Hex(text[index + 2]))); index += 2; }
            else if (text[index] < 128 || value is PyBytes) bytes.Add((byte)text[index]);
            else { Flush(); result.Append(text[index]); }
        }
        Flush(); return result.ToString();
    }
    private static int Hex(char value) => value is >= '0' and <= '9' ? value - '0'
        : value is >= 'a' and <= 'f' ? value - 'a' + 10 : value is >= 'A' and <= 'F' ? value - 'A' + 10 : -1;

    internal static object UrlEncode(object?[] args)
    {
        if (!PyUrlParse.None(args[5])) throw new PyNotImplementedError("custom quote_via is not implemented");
        var pairs = new List<(object?, object?)>();
        if (args[0] is PyDict mapping) foreach (var entry in mapping.Entries()) pairs.Add((entry.Key, entry.Value));
        else
        {
            if (args[0] is string or PyBytes) throw new PyTypeError("not a valid non-string sequence or mapping object");
            var iterator = PyIter.Create(args[0]);
            while (iterator.MoveNext())
            {
                var pair = PyIter.Create(iterator.Current());
                if (!pair.MoveNext()) throw new PyTypeError("not a valid non-string sequence or mapping object");
                object? key = pair.Current();
                if (!pair.MoveNext()) throw new PyTypeError("not a valid non-string sequence or mapping object");
                object? value = pair.Current();
                if (pair.MoveNext()) throw new PyTypeError("not a valid non-string sequence or mapping object");
                pairs.Add((key, value));
            }
        }
        var output = new List<string>();
        string Encode(object? value) => Quote(value is string or PyBytes ? value : PyRepr.Str(value), args[2], args[3], args[4], true);
        foreach (var (key, value) in pairs)
        {
            string encodedKey = Encode(key);
            if (PyOps.Truthy(args[1]) && value is PyList or PyTuple)
            {
                var iterator = PyIter.Create(value);
                while (iterator.MoveNext()) output.Add(encodedKey + "=" + Encode(iterator.Current()));
            }
            else output.Add(encodedKey + "=" + Encode(value));
        }
        return string.Join('&', output);
    }

    internal static object ParseQuery(object?[] args)
    {
        string text = PyUrlParse.Text(args[0]), separator = PyUrlParse.Text(args[6]);
        if (separator.Length == 0) throw new PyValueError("Separator must be of type string or bytes.");
        string[] fields = text.Length == 0 ? [] : text.Split(separator, StringSplitOptions.None);
        if (!PyUrlParse.None(args[5]) && fields.Length > PyIO.Index(args[5])) throw new PyValueError("Max number of fields exceeded");
        var result = new PyDict();
        foreach (string field in fields)
        {
            if (field.Length == 0 && !PyOps.Truthy(args[2])) continue;
            int equals = field.IndexOf('=');
            if (equals < 0 && PyOps.Truthy(args[2])) throw new PyValueError($"bad query field: {PyRepr.Repr(field)}");
            string key = equals < 0 ? field : field[..equals], value = equals < 0 ? "" : field[(equals + 1)..];
            if (value.Length == 0 && !PyOps.Truthy(args[1])) continue;
            key = Unquote(key, args[3], args[4], true); value = Unquote(value, args[3], args[4], true);
            if (result.GetOrDefault(key, null) is PyList existing) existing.Items.Add(value);
            else result.Set(key, new PyList([value]));
        }
        return result;
    }
}
