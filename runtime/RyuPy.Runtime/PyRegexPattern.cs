using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

/// <summary>已验证的Python模式；.NET仅执行翻译结果，匹配迭代及公开索引保持Python语义。</summary>
public partial class PyRegexPattern : PyNativeObject
{
    private readonly Regex _search;
    // 仅在需要全匹配或空匹配重试时构造辅助执行器，避免每次compile无条件解析三份。
    private readonly Lazy<Regex> _nonempty, _full;
    internal string? Pattern { get; }
    internal PyBytes? BytePattern { get; }
    internal int Flags { get; }
    internal int Groups { get; }
    internal Dictionary<string, int> Names { get; }
    /// <inheritdoc />
    public sealed override string TypeName => "Pattern";
    internal PyRegexPattern(string pattern, int flags) : this(pattern, flags, null) { }
    private PyRegexPattern(string pattern, int flags, PyBytes? bytePattern)
    {
        Pattern = pattern; BytePattern = bytePattern;
        if (pattern.Length > 16384) throw new PyNotImplementedError("re: pattern length exceeds supported resource limit");
        var parser = new PyRegexParser(pattern, flags, bytePattern);
        string boundary = "(?!(?<=[\\uD800-\\uDBFF])[\\uDC00-\\uDFFF])";
        string translated = boundary + "(?:" + parser.Translate() + ")" + boundary;
        Flags = parser.Flags | (bytePattern is null && (parser.Flags & 256) == 0 ? 32 : 0); Groups = parser.Groups; Names = parser.Names;
        RegexOptions options = RegexOptions.CultureInvariant | ((Flags & 8) != 0 ? RegexOptions.Multiline : RegexOptions.None);
        try
        {
            _search = new Regex(translated, options, TimeSpan.FromSeconds(2));
            _nonempty = new Lazy<Regex>(() => new Regex("\\G(?:" + translated + ")(?!\\G)", options, TimeSpan.FromSeconds(2)));
            _full = new Lazy<Regex>(() => new Regex("\\G(?:" + translated + ")\\z", options, TimeSpan.FromSeconds(2)));
        }
        catch (ArgumentException)
        {
            throw bytePattern is null ? new PyRegexError("invalid regular expression", pattern, 0)
                : new PyRegexError("invalid regular expression", bytePattern, 0);
        }
    }
    // 复合Scanner的同程序集冷构造；已有字段均为真实可用执行器，不制造空模式占位。
    // 普通Pattern实例的字段集合与旧构造/匹配正文不变；辅助Lazy只属于复合对象冷成本。
    private protected PyRegexPattern(Regex executor, int flags, int groups)
    {
        Pattern = null; BytePattern = null; Flags = flags; Groups = groups;
        Names = new(StringComparer.Ordinal); _search = executor;
        _nonempty = new Lazy<Regex>(() => executor); _full = new Lazy<Regex>(() => executor);
    }
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "pattern" => (object?)BytePattern ?? Pattern, "flags" => (long)Flags, "groups" => (long)Groups,
        "split" => PyRegexSplit.Pattern(this),
        "groupindex" => throw new PyNotImplementedError("re.Pattern.groupindex read-only mapping is not supported"),
        "match" or "search" or "fullmatch" or "findall" or "finditer" when BytePattern is not null =>
            PyRegex.Function(name, ["string", "pos", "endpos"], [null, 0L, long.MaxValue], 1,
                args => ApplyBytesInput(name, args[0], args[1], args[2])),
        "sub" or "subn" when BytePattern is not null =>
            PyRegex.Function(name, ["repl", "string", "count"], [null, null, 0L], 2,
                args => SubstituteBytesInput(args[0], args[1], args[2], name == "subn")),
        "match" or "search" or "fullmatch" or "findall" or "finditer" =>
            PyRegex.Function(name, ["string", "pos", "endpos"], [null, 0L, (long)int.MaxValue], 1,
                args => Apply(name, PyRegex.Text(args[0]), PyRegex.Number(args[1]), PyRegex.Number(args[2]))),
        "sub" or "subn" => PyRegex.Function(name, ["repl", "string", "count"], [null, null, 0L], 2,
            args => Substitute(args[0], PyRegex.Text(args[1]), PyRegex.Number(args[2]), name == "subn")),
        _ => throw Missing(name),
    };
    internal object? Apply(string method, string value, int start, int end)
    {
        var text = new PyRegexText(value); start = Math.Clamp(start, 0, text.Length); end = Math.Clamp(end, 0, text.Length);
        if (method == "finditer") return new PyRegexIterator(Enumerate(text, start, end).GetEnumerator());
        if (method == "findall")
        {
            var values = new List<object?>();
            foreach (var match in Enumerate(text, start, end))
                values.Add(Groups == 0 ? match.Group(0) : Groups == 1 ? match.GroupOrEmpty(1) : new PyTuple(Enumerable.Range(1, Groups).Select(match.GroupOrEmpty).ToArray()));
            return new PyList(values);
        }
        if (start > end) return PyNone.Instance;
        Match result = Run(method == "fullmatch" ? _full.Value : _search, value[..text.Offsets[end]], text.Offsets[start]);
        if (!result.Success || method == "match" && result.Index != text.Offsets[start]) return PyNone.Instance;
        return new PyRegexMatch(this, text, result, start, end);
    }
    internal IEnumerable<PyRegexMatch> Enumerate(PyRegexText text, int start, int end)
    {
        if (start > end) yield break;
        string subject = text.Text[..text.Offsets[end]]; int position = text.Offsets[start]; bool retry = false;
        while (position <= subject.Length)
        {
            Match result = Run(retry ? _nonempty.Value : _search, subject, position);
            if (!result.Success)
            {
                if (!retry || position == subject.Length) yield break;
                position = text.Offsets[text.Point(position) + 1]; retry = false; continue;
            }
            yield return new PyRegexMatch(this, text, result, start, end);
            position = result.Index + result.Length; retry = result.Length == 0;
        }
    }
    private static Match Run(Regex regex, string subject, int position)
    {
        try { return regex.Match(subject, position); }
        catch (RegexMatchTimeoutException) { throw new PyRuntimeError("re: supported matching time budget exceeded (2 seconds)"); }
    }
    internal object Substitute(object? replacement, string value, int count, bool tuple)
    {
        PyRegexReplacement? template = replacement is string text ? new PyRegexReplacement(this, text) : null;
        if (template is null && (PyOps.CallBuiltin("callable", [replacement], [], []) is not true
            || replacement is PyInstance instance && !instance.Class.TryLookup("__call__", out _, out _)))
            throw new PyTypeError("replacement must be a string or callable");
        if (replacement is PyBytes or PyByteArray) throw new PyTypeError("cannot use a bytes replacement on a string pattern");
        var input = new PyRegexText(value); var output = new StringBuilder(); int end = 0, changed = 0;
        if (count >= 0) foreach (var match in Enumerate(input, 0, input.Length))
        {
            output.Append(value, end, match.Raw.Index - end);
            object? inserted = template is not null ? template.Expand(match) : PyOps.CallValue(replacement, [match], [], []);
            // Python把回调返回None视作空替换，仍计入subn次数；其它非str返回值拒绝。
            if (inserted is null or PyNone) inserted = "";
            if (inserted is not string fragment) throw new PyTypeError("replacement must return a string");
            output.Append(fragment); end = match.Raw.Index + match.Raw.Length; changed++;
            // 限额到达后立即停止，不能再MoveNext扫描尾部（可能含昂贵回溯）。
            if (count > 0 && changed >= count) break;
        }
        output.Append(value, end, value.Length - end);
        return tuple ? new PyTuple([output.ToString(), (long)changed]) : output.ToString();
    }
    /// <inheritdoc />
    public override string PythonRepr() => BytePattern is null ? "re.compile(" + PyRepr.Repr(Pattern) + ")" : BytesRepr();
}

/// <summary>惰性finditer；每次MoveNext只推进到一个匹配，复用共享输入码点表。</summary>
internal sealed class PyRegexIterator(IEnumerator<PyRegexMatch> matches) : IPyIterator
{
    private bool _finished;
    public bool MoveNext()
    {
        if (_finished) return false;
        if (matches.MoveNext()) return true;
        _finished = true; matches.Dispose(); return false;
    }
    public object? Current() => _finished ? PyNone.Instance : matches.Current;
}
