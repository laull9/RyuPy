using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>Python替换模板独立解析，绝不把用户模板交给.NET的美元替换方言。</summary>
internal sealed class PyRegexReplacement
{
    private readonly List<object> _parts = [];
    internal PyRegexReplacement(PyRegexPattern pattern, string template)
    {
        var text = new StringBuilder();
        void Flush() { if (text.Length > 0) { _parts.Add(text.ToString()); text.Clear(); } }
        void Group(int number, int position)
        {
            if (number > pattern.Groups) throw new PyRegexError("invalid group reference", template, position);
            Flush(); _parts.Add(number);
        }
        for (int index = 0; index < template.Length; index++)
        {
            char value = template[index];
            if (value != '\\') { text.Append(value); continue; }
            int start = index;
            if (++index == template.Length) throw new PyRegexError("bad escape (end of pattern)", template, start);
            value = template[index];
            if (value == 'g')
            {
                if (++index == template.Length || template[index] != '<') throw new PyRegexError("missing <", template, index);
                int end = template.IndexOf('>', index + 1);
                if (end < 0) throw new PyRegexError("missing >", template, index);
                string name = template[(index + 1)..end]; index = end;
                if (name.Length > 0 && AllDigits(name))
                {
                    if (!int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out int number)) throw new PyRegexError("invalid group reference", template, start);
                    Group(number, start);
                }
                else if (pattern.Names.TryGetValue(name, out int number)) Group(number, start);
                else throw new PyIndexError("unknown group name '" + name + "'");
            }
            else if (value >= '0' && value <= '9')
            {
                bool octal = value == '0' || index + 2 < template.Length && IsOctal(value) && IsOctal(template[index + 1]) && IsOctal(template[index + 2]);
                int number = value - '0';
                if (octal)
                {
                    for (int count = 1; count < 3 && index + 1 < template.Length && IsOctal(template[index + 1]); count++) number = number * 8 + template[++index] - '0';
                    if (number > 255) throw new PyRegexError("octal escape value outside of range 0-0o377", template, start);
                    text.Append((char)number);
                }
                else
                {
                    if (index + 1 < template.Length && char.IsAsciiDigit(template[index + 1])) number = number * 10 + template[++index] - '0';
                    Group(number, start);
                }
            }
            else
            {
                char replacement = value switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'f' => '\f', 'v' => '\v', 'a' => '\a', 'b' => '\b', '\\' => '\\', _ => '\0' };
                if (replacement != '\0') text.Append(replacement);
                else if (char.IsAsciiLetter(value)) throw new PyRegexError("bad escape \\" + value, template, start);
                else text.Append('\\').Append(value);
            }
        }
        Flush();
    }
    internal string Expand(PyRegexMatch match)
    {
        var output = new StringBuilder();
        foreach (object part in _parts) output.Append(part is int number ? match.GroupOrEmpty(number) : part);
        return output.ToString();
    }
    private static bool IsOctal(char value) => value is >= '0' and <= '7';
    private static bool AllDigits(string value) { foreach (char c in value) if (!char.IsAsciiDigit(c)) return false; return true; }
}
