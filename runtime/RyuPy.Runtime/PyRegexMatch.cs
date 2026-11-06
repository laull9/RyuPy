using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

/// <summary>Python匹配对象：组号按源码捕获顺序，不采用.NET命名组重编号。</summary>
public sealed class PyRegexMatch : PyNativeObject
{
    private readonly Match? _raw;
    // 旧text/exact bytes消费者保Raw；mutable结果只持marks，不保宿主输入字符串。
    internal Match Raw => _raw ?? throw new InvalidOperationException("mutable regex matches do not retain host Match objects");
    private readonly PyRegexPattern _pattern;
    private readonly PyRegexText? _text;
    private readonly PyBytes? _bytes;
    private readonly PyRegexMutableCaptures? _mutable;
    private bool BytesDomain => _bytes is not null || _mutable is not null;
    private readonly int _start, _end;
    internal PyRegexMatch(PyRegexPattern pattern, PyRegexText text, Match match, int start, int end)
    { _pattern = pattern; _text = text; _raw = match; _start = start; _end = end; }
    internal PyRegexMatch(PyRegexPattern pattern, PyBytes bytes, Match match, int start, int end)
    { _pattern = pattern; _bytes = bytes; _raw = match; _start = start; _end = end; }
    internal PyRegexMatch(PyRegexPattern pattern, PyRegexMutableCaptures mutable, int start, int end)
    { _pattern = pattern; _mutable = mutable; _start = start; _end = end; }
    /// <inheritdoc />
    public override string TypeName => "Match";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "re" => _pattern, "string" => _mutable?.Source ?? (object?)_bytes ?? _text!.Text, "pos" => (long)_start, "endpos" => (long)_end,
        "group" when BytesDomain => PyRegex.Function("group", ["args"], [], 1,
            bound => ((PyTuple)bound[0]!).Items.Length switch
            { 0 => Group(0), 1 => Group(GroupNumber(((PyTuple)bound[0]!).Items[0])),
                _ => new PyTuple(((PyTuple)bound[0]!).Items.Select(value => Group(GroupNumber(value))).ToArray()) }, ["varpos"]),
        "group" => PyNativeFunctions.CreateVariadic("group", args => args.Length switch
        { 0 => Group(0), 1 => Group(GroupNumber(args[0])), _ => new PyTuple(args.Select(value => Group(GroupNumber(value))).ToArray()) }),
        "groups" => PyRegex.Function("groups", ["default"], [PyNone.Instance], 0,
            args => new PyTuple(Enumerable.Range(1, _pattern.Groups).Select(number => GroupWithDefault(number, args[0])).ToArray())),
        "groupdict" => PyRegex.Function("groupdict", ["default"], [PyNone.Instance], 0, args => GroupDict(args[0])),
        "start" or "end" or "span" when BytesDomain => PyRegex.Function(name, ["group"], [0L], 0,
            args => Position(name, GroupNumber(args[0])), ["posonly"]),
        "start" or "end" or "span" => PyNativeFunctions.Create(name, ["group"], [0L], 0,
            args => Position(name, GroupNumber(args[0])), ["posonly"]),
        "expand" when BytesDomain => PyRegex.Function("expand", ["template"], [null], 1, args => ExpandBytes(args[0])),
        "expand" => PyRegex.Function("expand", ["template"], [null], 1, args => new PyRegexReplacement(_pattern, PyRegex.Text(args[0])).Expand(this)),
        _ => throw Missing(name),
    };
    internal int GroupNumber(object? value)
    {
        if (BytesDomain) return PyRegexBytes.GroupNumber(_pattern, value);
        int number;
        if (value is string name)
        {
            if (!_pattern.Names.TryGetValue(name, out number)) throw new PyIndexError("no such group");
        }
        else number = PyRegex.Number(value);
        if (number < 0 || number > _pattern.Groups) throw new PyIndexError("no such group");
        return number;
    }
    private Group Capture(int number) => number == 0 ? Raw.Groups[0] : Raw.Groups["ryu" + number];
    internal object Group(int number)
    {
        if (_mutable is not null) return _mutable.Group(number);
        Group group = Capture(number);
        return !group.Success ? PyNone.Instance : _bytes is null ? group.Value
            : PyRegexBytes.Slice(_bytes, group.Index, group.Length);
    }
    internal object GroupOrEmpty(int number)
    {
        if (_mutable is not null)
        {
            object value = _mutable.Group(number); return value is PyNone ? PyRegexBytes.Empty : value;
        }
        Group group = Capture(number);
        return _bytes is null ? group.Success ? group.Value : ""
            : group.Success ? PyRegexBytes.Slice(_bytes, group.Index, group.Length) : PyRegexBytes.Empty;
    }
    internal object ExpandBytes(object? template) => new PyRegexBytesTemplate(_pattern, template).Expand(this);
    private object? GroupWithDefault(int number, object? missing)
    {
        object value = Group(number); return value is PyNone ? missing : value;
    }
    private object Position(string method, int number)
    {
        if (_mutable is not null)
        {
            PyRegexMutableCaptures.Capture saved = _mutable.Get(number);
            long first = saved.Success ? saved.Start : -1, last = saved.Success ? saved.End : -1;
            return method switch { "start" => first, "end" => last, _ => new PyTuple([first, last]) };
        }
        Group group = Capture(number);
        long start = group.Success ? _bytes is null ? _text!.Point(group.Index) : group.Index : -1;
        long end = group.Success ? _bytes is null ? _text!.Point(group.Index + group.Length) : group.Index + group.Length : -1;
        return method switch { "start" => start, "end" => end, _ => new PyTuple([start, end]) };
    }
    private PyDict GroupDict(object? missing)
    {
        var result = new PyDict();
        foreach (var pair in _pattern.Names) { object value = Group(pair.Value); result.Set(pair.Key, value is PyNone ? missing : value); }
        return result;
    }
    /// <inheritdoc />
    public override string PythonRepr() => "<re.Match object; span=" + PyRepr.Repr(Position("span", 0)) + ", match=" + PyRepr.Repr(BytesDomain ? Group(0) : Raw.Value) + ">";
}
