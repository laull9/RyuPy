using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>Python正则的有限语法解析器：所有字符集合自行翻译，禁止.NET扩展语法穿透。</summary>
internal sealed class PyRegexParser
{
    private readonly string _pattern;
    private int _position;
    private int _depth;
    internal int Flags { get; private set; }
    internal int Groups { get; private set; }
    internal Dictionary<string, int> Names { get; } = new(StringComparer.Ordinal);
    private readonly HashSet<int> _closed = [];
    private bool Ascii => (Flags & 256) != 0;
    private bool Ignore => (Flags & 2) != 0;
    internal PyRegexParser(string pattern, int flags) { _pattern = pattern; Flags = flags; }
    internal string Translate()
    {
        // 初始全局flags在Python语法中必须位于表达式开头。
        while (_pattern.AsSpan(_position).StartsWith("(?", StringComparison.Ordinal))
        {
            int close = _pattern.IndexOf(')', _position + 2);
            if (close < 0) break;
            string flags = _pattern[(_position + 2)..close];
            if (flags.Length == 0 || !AllFlags(flags)) break;
            foreach (char flag in flags) Flags |= Flag(flag);
            _position = close + 1;
        }
        PyRegex.CheckFlags(Flags);
        string result = Sequence(false);
        if (_position != _pattern.Length) throw Error("unbalanced parenthesis");
        return result;
    }
    private string Sequence(bool grouped, int groupStart = 0)
    {
        var output = new StringBuilder(); bool quantified = false, hasAtom = false;
        while (_position < _pattern.Length)
        {
            if (output.Length > 200_000) throw Unsupported("translated pattern size limit");
            char value = _pattern[_position];
            if ((Flags & 64) != 0)
            {
                if (" \t\r\n\v\f".Contains(value)) { _position++; continue; }
                if (value == '#') { while (_position < _pattern.Length && _pattern[_position] != '\n') _position++; continue; }
            }
            if (value == ')') { if (grouped) { _position++; return output.ToString(); } throw new PyRegexError("unbalanced parenthesis", _pattern, _position); }
            _position++;
            if (value == '|') { output.Append('|'); hasAtom = quantified = false; continue; }
            if (value is '*' or '+' or '?' || value == '{' && IsQuantifier(out _))
            {
                if (!hasAtom) throw Error("nothing to repeat");
                if (quantified) throw Error("multiple repeat");
                string quantifier = value.ToString();
                if (value == '{') { IsQuantifier(out int end); string bounds = _pattern[_position..end]; quantifier = "{" + (bounds.StartsWith(',') ? "0" : "") + bounds + "}"; _position = end + 1; }
                output.Append(quantifier); quantified = true;
                if (_position < _pattern.Length && _pattern[_position] == '?') { output.Append('?'); _position++; }
                if (_position < _pattern.Length && _pattern[_position] == '+') throw Unsupported("possessive quantifier");
                continue;
            }
            quantified = false; hasAtom = true;
            switch (value)
            {
                case '(' : output.Append(Group()); break;
                case '[' : output.Append(CharacterClass().Emit()); break;
                case '.' : output.Append(((Flags & 16) != 0 ? new PyRegexSet().Add(0, 0x10ffff) : new PyRegexSet().Add(10, 10).Negate()).Emit()); break;
                case '^': case '$': output.Append(value); hasAtom = false; break;
                case '\\':
                    bool assertion = _position < _pattern.Length && "AZbB".Contains(_pattern[_position]);
                    output.Append(EscapeOutside()); if (assertion) hasAtom = false; break;
                default: output.Append(Literal(ReadCodePoint(value)).Emit()); break;
            }
        }
        if (output.Length > 200_000) throw Unsupported("translated pattern size limit");
        if (grouped) throw new PyRegexError("missing ), unterminated subpattern", _pattern, groupStart);
        return output.ToString();
    }
    private bool IsQuantifier(out int end)
    {
        end = _pattern.IndexOf('}', _position);
        if (end < 0) return false;
        string value = _pattern[_position..end];
        if (value.Length == 0) return false;
        bool comma = false;
        foreach (char c in value) { if (c == ',' && !comma) comma = true; else if (c < '0' || c > '9') return false; }
        return true;
    }
    private string Group()
    {
        int start = _position - 1;
        string prefix = ""; int number = 0;
        if (_position < _pattern.Length && _pattern[_position] == '?')
        {
            _position++;
            if (Consume(':')) prefix = "?:";
            else if (Consume('=')) prefix = "?=";
            else if (Consume('!')) prefix = "?!";
            else if (Consume('P'))
            {
                if (Consume('<'))
                {
                    string name = ReadUntil('>'); ValidateName(name);
                    if (Names.ContainsKey(name)) throw Error("redefinition of group name");
                    number = ++Groups; Names[name] = number; prefix = "?<ryu" + number + ">";
                }
                else if (Consume('='))
                {
                    string name = ReadUntil(')');
                    if (!Names.TryGetValue(name, out int group) || !_closed.Contains(group)) throw Error("unknown group name");
                    if (Ignore) throw Unsupported("case-insensitive backreference");
                    return "\\k<ryu" + group + ">";
                }
                else throw Error("unknown extension ?P");
            }
            else if (_position + 1 < _pattern.Length && _pattern[_position] == '<' && _pattern[_position + 1] is '=' or '!')
                throw Unsupported("lookbehind");
            else if (_position < _pattern.Length && (_pattern[_position] is '>' or '(' or '#' or '-' || AllFlags(_pattern[_position].ToString())))
                throw Unsupported("lookbehind, atomic, conditional, comment or scoped flags");
            else throw Error("unknown extension");
        }
        else { number = ++Groups; prefix = "?<ryu" + number + ">"; }
        if (++_depth > 256) throw new PyRecursionError("re: supported group nesting limit exceeded (256)");
        string inner;
        try { inner = Sequence(true, start); } finally { _depth--; }
        if (number > 0) _closed.Add(number);
        return "(" + prefix + inner + ")";
    }
    private string EscapeOutside()
    {
        if (_position >= _pattern.Length) throw Error("bad escape (end of pattern)");
        char value = _pattern[_position++];
        if (value is 'A' or 'Z') return value == 'A' ? "\\A" : "\\z";
        if (value is 'b' or 'B')
        {
            string word = Category('w').Emit();
            string boundary = "(?:(?<=" + word + ")(?!" + word + ")|(?<!" + word + ")(?=" + word + "))";
            return value == 'b' ? boundary : "(?!\\A\\z)(?!" + boundary + ")";
        }
        if (value >= '1' && value <= '9')
        {
            if (value <= '7' && _position + 1 < _pattern.Length && _pattern[_position] is >= '0' and <= '7' && _pattern[_position + 1] is >= '0' and <= '7')
            {
                int octal = (value - '0') * 64 + (_pattern[_position++] - '0') * 8 + _pattern[_position++] - '0';
                if (octal > 255) throw Error("octal escape outside range 0-0o377");
                return Literal(octal).Emit();
            }
            int group = value - '0';
            if (_position < _pattern.Length && char.IsAsciiDigit(_pattern[_position])) group = group * 10 + _pattern[_position++] - '0';
            if (!_closed.Contains(group)) throw Error("invalid group reference");
            if (Ignore) throw Unsupported("case-insensitive backreference");
            return "\\k<ryu" + group + ">";
        }
        return EscapeSet(value).Emit();
    }
    private PyRegexSet CharacterClass()
    {
        int opening = _position - 1;
        bool negate = Consume('^'), first = true; var result = new PyRegexSet();
        while (_position < _pattern.Length)
        {
            if (_pattern[_position] == ']' && !first) { _position++; return negate ? result.Negate() : result; }
            first = false; PyRegexSet start = ClassAtom(out bool category);
            if (_position + 1 < _pattern.Length && _pattern[_position] == '-' && _pattern[_position + 1] != ']')
            {
                _position++; PyRegexSet end = ClassAtom(out bool endCategory);
                if (category || endCategory || start.Ranges.Count != 1 || end.Ranges.Count != 1 || start.Ranges[0].Low != start.Ranges[0].High || end.Ranges[0].Low != end.Ranges[0].High)
                    throw Error("bad character range");
                int low = start.Ranges[0].Low, high = end.Ranges[0].Low;
                if (low > high) throw Error("bad character range");
                var range = new PyRegexSet().Add(low, high);
                result.Add(Ignore ? range.IgnoreCase(Ascii) : range);
            }
            else result.Add(Ignore && !category ? start.IgnoreCase(Ascii) : start);
        }
        throw new PyRegexError("unterminated character set", _pattern, opening);
    }
    private PyRegexSet ClassAtom(out bool category)
    {
        category = false;
        char value = _pattern[_position++];
        if (value != '\\') { int point = ReadCodePoint(value); return new PyRegexSet().Add(point, point); }
        if (_position == _pattern.Length) throw Error("bad escape (end of pattern)");
        char escape = _pattern[_position++]; category = "wWdDsS".Contains(escape);
        return EscapeSet(escape, inClass: true);
    }
    private PyRegexSet EscapeSet(char value, bool inClass = false)
    {
        if ("wWdDsS".Contains(value)) return Category(value);
        if (value == 'N') throw Unsupported("named Unicode escape");
        if (inClass && value is '8' or '9') throw Error("bad escape \\" + value);
        int point = value switch
        {
            'n' => 10, 'r' => 13, 't' => 9, 'f' => 12, 'v' => 11, 'a' => 7,
            'b' when inClass => 8,
            'x' => Hex(2), 'u' => Hex(4), 'U' => Hex(8),
            '0' => Octal(),
            _ => value,
        };
        if (char.IsAsciiLetter(value) && !"nrtfvabxuU".Contains(value)) throw Error("bad escape \\" + value);
        if (inClass && value >= '1' && value <= '7') throw Unsupported("octal character class escape");
        return inClass ? new PyRegexSet().Add(point, point) : Literal(point);
    }
    private PyRegexSet Category(char code)
    {
        char lower = char.ToLowerInvariant(code);
        PyRegexSet set = lower switch
        {
            'w' => Ascii ? new PyRegexSet().Add('a', 'z').Add('A', 'Z').Add('0', '9').Add('_', '_') : PyRegexSet.Table(PyRegexUnicodeData.Word),
            'd' => Ascii ? new PyRegexSet().Add('0', '9') : PyRegexSet.Table(PyRegexUnicodeData.Digit),
            _ => Ascii ? new PyRegexSet().Add(9, 13).Add(32, 32) : PyRegexSet.Table(PyRegexUnicodeData.Space),
        };
        return char.IsUpper(code) ? set.Negate() : set;
    }
    private PyRegexSet Literal(int point)
    {
        var set = new PyRegexSet().Add(point, point);
        return Ignore ? set.IgnoreCase(Ascii) : set;
    }
    private int ReadCodePoint(char first)
    {
        if (char.IsHighSurrogate(first) && _position < _pattern.Length && char.IsLowSurrogate(_pattern[_position]))
            return char.ConvertToUtf32(first, _pattern[_position++]);
        return first;
    }
    private int Hex(int count)
    {
        if (_position + count > _pattern.Length || !uint.TryParse(_pattern.AsSpan(_position, count), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint point) || point > 0x10ffff)
            throw Error("incomplete or invalid Unicode escape");
        _position += count; return (int)point;
    }
    private int Octal()
    {
        int point = 0;
        for (int count = 0; count < 2 && _position < _pattern.Length && _pattern[_position] is >= '0' and <= '7'; count++) point = point * 8 + _pattern[_position++] - '0';
        return point;
    }
    private bool Consume(char value) { if (_position >= _pattern.Length || _pattern[_position] != value) return false; _position++; return true; }
    private string ReadUntil(char end)
    {
        int start = _position, close = _pattern.IndexOf(end, start);
        if (close < 0) throw Error("missing group delimiter");
        _position = close + 1; return _pattern[start..close];
    }
    private void ValidateName(string name)
    {
        foreach (char c in name) if (c > 127) throw Unsupported("non-ASCII group name");
        if (name.Length == 0 || !(char.IsAsciiLetter(name[0]) || name[0] == '_')) throw Error("bad character in group name");
        foreach (char c in name) if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) throw Unsupported("non-ASCII group name");
    }
    private static bool AllFlags(string value) { foreach (char c in value) if (!"aiLmsux".Contains(c)) return false; return true; }
    private static int Flag(char value) => value switch { 'a' => 256, 'i' => 2, 'L' => 4, 'm' => 8, 's' => 16, 'u' => 32, 'x' => 64, _ => 0 };
    private PyRegexError Error(string message) => new(message, _pattern, Math.Max(0, _position - 1));
    private static PyNotImplementedError Unsupported(string feature) => new("re: unsupported " + feature);
}
