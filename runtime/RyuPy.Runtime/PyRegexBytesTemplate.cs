// bytes调用专用的有限Python模板解析器：保模板自己的str/bytes域及首空literal，不改变旧text模板路径。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

internal sealed class PyRegexBytesTemplate
{
    private readonly object _source;
    private readonly bool _bytes;
    private readonly List<object> _parts = [];
    private bool _hasGroups;

    internal PyRegexBytesTemplate(PyRegexPattern pattern, object? source)
    {
        if (source is PyByteArray array) source = PyRegexBytes.SnapshotArrayTemplate(array);
        if (source is PyMemoryView) throw PyRegexBytes.Unsupported("mutable or view regex replacement template");
        if (source is PyStringInstance) throw PyRegexBytes.Unsupported("str subclass regex replacement template");
        if (source is not (string or PyBytes))
            throw PyRegexBytes.Own(new PyTypeError($"decoding to str: need a bytes-like object, {PyRepr.TypeName(source)} found"));
        _source = source; _bytes = source is PyBytes;
        string template = source is PyBytes bytes ? Encoding.Latin1.GetString(bytes.Bytes) : (string)source;
        var literal = new StringBuilder();
        void Flush()
        {
            string text = literal.ToString();
            _parts.Add(_bytes ? text.Length == 0 ? PyRegexBytes.Empty : new PyBytes(Encoding.Latin1.GetBytes(text)) : text);
            literal.Clear();
        }
        void Group(int number, int position)
        {
            if (number > pattern.Groups) throw Error("invalid group reference " + number, position);
            Flush(); _parts.Add(number); _hasGroups = true;
        }
        for (int index = 0; index < template.Length; index++)
        {
            char value = template[index];
            if (value != '\\') { literal.Append(value); continue; }
            int start = index;
            if (++index == template.Length) throw Error("bad escape (end of pattern)", start);
            value = template[index];
            if (value == 'g')
            {
                if (++index == template.Length || template[index] != '<') throw Error("missing <", index);
                int nameStart = index + 1, end = template.IndexOf('>', nameStart);
                if (end < 0) throw Error(nameStart == template.Length ? "missing group name" : "missing >, unterminated name", nameStart);
                string name = template[nameStart..end]; index = end;
                if (name.Length == 0) throw Error("missing group name", nameStart);
                if (AllDigits(name))
                {
                    string canonical = name.TrimStart('0');
                    if (canonical.Length == 0) canonical = "0";
                    if (!int.TryParse(canonical, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                        throw Error("invalid group reference " + canonical, nameStart);
                    Group(number, nameStart);
                }
                else
                {
                    ValidateName(name, nameStart);
                    if (!pattern.Names.TryGetValue(name, out int number))
                        throw PyRegexBytes.Own(new PyIndexError("unknown group name " + PyRepr.Repr(name)));
                    Group(number, nameStart);
                }
            }
            else if (value >= '0' && value <= '9')
            {
                bool octal = value == '0' || index + 2 < template.Length && IsOctal(value)
                    && IsOctal(template[index + 1]) && IsOctal(template[index + 2]);
                int number = value - '0';
                if (octal)
                {
                    for (int count = 1; count < 3 && index + 1 < template.Length && IsOctal(template[index + 1]); count++)
                        number = number * 8 + template[++index] - '0';
                    if (number > 255) throw Error("octal escape value " + template[start..(index + 1)] + " outside of range 0-0o377", start);
                    literal.Append((char)number);
                }
                else
                {
                    if (index + 1 < template.Length && char.IsAsciiDigit(template[index + 1])) number = number * 10 + template[++index] - '0';
                    Group(number, start + 1);
                }
            }
            else
            {
                char replacement = value switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t', 'f' => '\f', 'v' => '\v', 'a' => '\a', 'b' => '\b', '\\' => '\\', _ => '\0',
                };
                if (replacement != '\0') literal.Append(replacement);
                else if (char.IsAsciiLetter(value)) throw Error("bad escape \\" + value, start);
                else literal.Append('\\').Append(value);
            }
        }
        Flush();
    }

    internal object Expand(PyRegexMatch match)
    {
        if (!_hasGroups) return _parts[0];
        var chunks = new List<object?>();
        for (int index = 0; index < _parts.Count; index++)
        {
            object part = _parts[index];
            if (part is int group)
            {
                object value = match.Group(group);
                if (value is not PyNone) chunks.Add(value);
            }
            else if (index == 0 || part is string { Length: > 0 } or PyBytes { Count: > 0 }) chunks.Add(part);
        }
        if (_bytes) return PyRegexBytes.Join(chunks, PyRegexBytes.Empty);
        var result = new StringBuilder();
        for (int index = 0; index < chunks.Count; index++)
        {
            if (chunks[index] is not string text)
                throw PyRegexBytes.Own(new PyTypeError($"sequence item {index}: expected str instance, {PyRepr.TypeName(chunks[index])} found"));
            result.Append(text);
        }
        return result.ToString();
    }

    private void ValidateName(string name, int position)
    {
        foreach (char value in name)
            if (value > 127)
            {
                if (!_bytes) throw PyRegexBytes.Unsupported("non-ASCII group name in str replacement template");
                throw Error("bad character in group name " + PyRepr.Repr(new PyBytes(Encoding.Latin1.GetBytes(name)))[1..], position);
            }
        bool valid = char.IsAsciiLetter(name[0]) || name[0] == '_';
        foreach (char value in name) valid &= char.IsAsciiLetterOrDigit(value) || value == '_';
        if (!valid) throw Error("bad character in group name " + PyRepr.Repr(name), position);
    }

    private PyRegexError Error(string message, int position) => (PyRegexError)PyRegexBytes.Own(_source is PyBytes bytes
        ? new PyRegexError(message, bytes, position) : new PyRegexError(message, (string)_source, position));
    private static bool IsOctal(char value) => value is >= '0' and <= '7';
    private static bool AllDigits(string value)
    {
        foreach (char character in value) if (!char.IsAsciiDigit(character)) return false;
        return true;
    }
}
