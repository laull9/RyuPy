using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>Python正则的有限语法解析器：所有字符集合自行翻译，禁止.NET扩展语法穿透。</summary>
internal sealed class PyRegexParser
{
    private readonly string _pattern;
    private readonly PyBytes? _bytesPattern;
    private int _position;
    private int _depth;
    internal int Flags { get; private set; }
    internal int Groups { get; private set; }
    internal Dictionary<string, int> Names { get; } = new(StringComparer.Ordinal);
    private readonly HashSet<int> _closed = [];
    // 只在实际出现数字前向条件时分配；顺序保留首次无效引用的原位置。
    private List<(int Group, int Position)>? _forwardConditionals;
    private bool Ascii => _bytesPattern is not null || (Flags & 256) != 0;
    private bool Ignore => (Flags & 2) != 0;
    internal PyRegexParser(string pattern, int flags, PyBytes? bytesPattern = null)
    { _pattern = pattern; Flags = flags; _bytesPattern = bytesPattern; }
    internal string Translate()
    {
        // 初始全局flags在Python语法中必须位于表达式开头。
        while (_pattern.AsSpan(_position).StartsWith("(?", StringComparison.Ordinal))
        {
            int close = _pattern.IndexOf(')', _position + 2);
            if (close < 0) break;
            string flags = _pattern[(_position + 2)..close];
            if (flags.Length == 0 || !AllFlags(flags)) break;
            if (_bytesPattern is not null) CheckBytesInlineFlags(flags, _position + 2);
            foreach (char flag in flags) Flags |= Flag(flag);
            _position = close + 1;
        }
        if (_bytesPattern is null) PyRegex.CheckFlags(Flags);
        string result = Sequence(false);
        if (_position != _pattern.Length) throw Error("unbalanced parenthesis");
        // CP在语法解析完成后验证外部type flags，合法LOCALE的NI不能吞掉先发生的语法错。
        if (_bytesPattern is not null) PyRegexBytes.CheckFlags(Flags);
        if (_forwardConditionals is not null)
            foreach (var reference in _forwardConditionals)
                if (reference.Group > Groups)
                    throw ErrorAt("invalid group reference " + reference.Group, reference.Position);
        return result;
    }
    private string Sequence(bool grouped, int groupStart = 0, bool conditionalBranch = false)
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
            if (value == ')')
            {
                if (!grouped) throw ErrorAt("unbalanced parenthesis", _position);
                // 条件分支由外层消费分隔符；普通组的原有消费方式不变。
                if (!conditionalBranch) _position++;
                return output.ToString();
            }
            if (value == '|' && conditionalBranch) return output.ToString();
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
        if (grouped) throw ErrorAt("missing ), unterminated subpattern", groupStart);
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
            else if (Consume('(')) return ConditionalGroup(start);
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
            else if (_position < _pattern.Length && (_pattern[_position] is '>' or '#' or '-' || AllFlags(_pattern[_position].ToString())))
            {
                if (_bytesPattern is not null && AllFlags(_pattern[_position].ToString()))
                {
                    int stop = _position;
                    while (stop < _pattern.Length && AllFlags(_pattern[stop].ToString())) stop++;
                    CheckBytesInlineFlags(_pattern[_position..stop], _position);
                }
                throw Unsupported("lookbehind, atomic, conditional, comment or scoped flags");
            }
            else throw Error("unknown extension");
        }
        else { number = ++Groups; prefix = "?<ryu" + number + ">"; }
        if (++_depth > 256) throw new PyRecursionError("re: supported group nesting limit exceeded (256)");
        string inner;
        try { inner = Sequence(true, start); } finally { _depth--; }
        if (number > 0) _closed.Add(number);
        return "(" + prefix + inner + ")";
    }
    // 条件只测试捕获参与，不是反向引用；故I不拒绝本节点，分支仍走原有字符翻译。
    private string ConditionalGroup(int start)
    {
        int nameStart = _position;
        string name = ReadConditionalName();
        int group = ConditionalReference(name, nameStart);
        // CP重入开放捕获会更新start，.NET仍看旧完成capture，不能直接采用后端条件。
        if (group <= Groups && !_closed.Contains(group))
            throw Unsupported("conditional reference to an open group");
        if (group > Groups) (_forwardConditionals ??= []).Add((group, nameStart));
        if (++_depth > 256) throw new PyRecursionError("re: supported group nesting limit exceeded (256)");
        try
        {
            string yes = Sequence(true, start, conditionalBranch: true);
            string? no = null;
            if (Consume('|'))
            {
                no = Sequence(true, start, conditionalBranch: true);
                if (_position < _pattern.Length && _pattern[_position] == '|')
                    throw ErrorAt("conditional backref with more than two branches", _position);
            }
            if (!Consume(')')) throw ErrorAt("missing ), unterminated subpattern", start);
            string result = "(?(ryu" + group + ")" + yes + (no is null ? "" : "|" + no) + ")";
            if (result.Length > 200_000) throw Unsupported("translated pattern size limit");
            return result;
        }
        finally { _depth--; }
    }
    // 名字中不应用VERBOSE跳空白；反斜线及其后码元是一个token，不能错认转义的右括号。
    private string ReadConditionalName()
    {
        int start = _position;
        while (_position < _pattern.Length)
        {
            char value = _pattern[_position];
            if (value == ')')
            {
                string name = _pattern[start.._position]; _position++;
                if (name.Length == 0) throw ErrorAt("missing group name", start);
                return name;
            }
            if (value == '\\')
            {
                if (_position + 1 == _pattern.Length) throw ErrorAt("bad escape (end of pattern)", _position);
                _position += 2;
            }
            else _position++;
        }
        throw ErrorAt(_position == start ? "missing group name" : "missing ), unterminated name", start);
    }
    private int ConditionalReference(string name, int start)
    {
        bool decimalNumber = true;
        foreach (char value in name) decimalNumber &= char.IsAsciiDigit(value);
        if (decimalNumber)
        {
            // 超长数字的CP int转换限制另留边界；先确认全数字，不能把混合非法名误报NI。
            if (name.Length > 4300) throw Unsupported("conditional group number digit limit");
            int first = 0;
            while (first < name.Length && name[first] == '0') first++;
            if (first == name.Length) throw ErrorAt("bad group number", start);
            ReadOnlySpan<char> digits = name.AsSpan(first);
            // 固定64位CP MAXGROUPS为1073741823；按规范十进制比较，不溢出或截断错误数字。
            if (digits.Length > 10 || digits.Length == 10 && digits.SequenceCompareTo("1073741823") >= 0)
                throw ErrorAt("invalid group reference " + digits.ToString(), start);
            int group = 0;
            foreach (char value in digits) group = group * 10 + value - '0';
            return group;
        }
        if (_bytesPattern is null)
            foreach (char value in name)
                if (value > 127) throw Unsupported("non-ASCII group name");
        bool valid = char.IsAsciiLetter(name[0]) || name[0] == '_';
        foreach (char value in name) valid &= char.IsAsciiLetterOrDigit(value) || value == '_';
        if (!valid) throw ErrorAt("bad character in group name " + ConditionalNameRepr(name), start);
        if (!Names.TryGetValue(name, out int number))
            throw ErrorAt("unknown group name " + ConditionalNameRepr(name), start);
        return number;
    }
    private string ConditionalNameRepr(string name) => _bytesPattern is null ? PyRepr.Repr(name)
        : PyRepr.Repr(new PyBytes(Encoding.Latin1.GetBytes(name)))[1..];
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
        throw ErrorAt("unterminated character set", opening);
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
        if (_bytesPattern is not null && value is 'u' or 'U' or 'N')
            throw ErrorAt("bad escape \\" + value, _position - 2);
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
        if (char.IsAsciiLetter(value) && !"nrtfvabxuU".Contains(value))
            throw _bytesPattern is null ? Error("bad escape \\" + value) : ErrorAt("bad escape \\" + value, _position - 2);
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
            throw _bytesPattern is null ? Error("incomplete or invalid Unicode escape") : ErrorAt("incomplete escape \\x", _position - 2);
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
        if (_bytesPattern is not null)
        {
            bool valid = name.Length > 0 && (char.IsAsciiLetter(name[0]) || name[0] == '_');
            foreach (char c in name) valid &= char.IsAsciiLetterOrDigit(c) || c == '_';
            if (!valid) throw ErrorAt("bad character in group name " + PyRepr.Repr(new PyBytes(Encoding.Latin1.GetBytes(name)))[1..], _position - name.Length - 1);
            return;
        }
        foreach (char c in name) if (c > 127) throw Unsupported("non-ASCII group name");
        if (name.Length == 0 || !(char.IsAsciiLetter(name[0]) || name[0] == '_')) throw Error("bad character in group name");
        foreach (char c in name) if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) throw Unsupported("non-ASCII group name");
    }
    private static bool AllFlags(string value) { foreach (char c in value) if (!"aiLmsux".Contains(c)) return false; return true; }
    private static int Flag(char value) => value switch { 'a' => 256, 'i' => 2, 'L' => 4, 'm' => 8, 's' => 16, 'u' => 32, 'x' => 64, _ => 0 };
    private void CheckBytesInlineFlags(string flags, int start)
    {
        int types = 0;
        for (int index = 0; index < flags.Length; index++)
        {
            char flag = flags[index];
            if (flag == 'u') throw ErrorAt("bad inline flags: cannot use 'u' flag with a bytes pattern", start + index + 1);
            if (flag is 'a' or 'L')
            {
                types |= Flag(flag);
                if (types == (256 | 4)) throw ErrorAt("bad inline flags: flags 'a', 'u' and 'L' are incompatible", start + index + 1);
            }
        }
    }
    private PyRegexError Error(string message) => ErrorAt(message, Math.Max(0, _position - 1));
    private PyRegexError ErrorAt(string message, int position) => _bytesPattern is null
        ? new(message, _pattern, position) : new(message, _bytesPattern, position);
    // 只准备本解析器新造的拒绝错误；不扩大到用户协议或成功解析路径。
    private static PyNotImplementedError Unsupported(string feature) => (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError("re: unsupported " + feature));
}
