using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

/// <summary>Python匹配对象：组号按源码捕获顺序，不采用.NET命名组重编号。</summary>
public sealed class PyRegexMatch : PyNativeObject
{
    internal Match Raw { get; }
    private readonly PyRegexPattern _pattern;
    private readonly PyRegexText _text;
    private readonly int _start, _end;
    internal PyRegexMatch(PyRegexPattern pattern, PyRegexText text, Match match, int start, int end)
    { _pattern = pattern; _text = text; Raw = match; _start = start; _end = end; }
    /// <inheritdoc />
    public override string TypeName => "Match";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "re" => _pattern, "string" => _text.Text, "pos" => (long)_start, "endpos" => (long)_end,
        "group" => PyNativeFunctions.CreateVariadic("group", args => args.Length switch
        { 0 => Group(0), 1 => Group(GroupNumber(args[0])), _ => new PyTuple(args.Select(value => Group(GroupNumber(value))).ToArray()) }),
        "groups" => PyRegex.Function("groups", ["default"], [PyNone.Instance], 0,
            args => new PyTuple(Enumerable.Range(1, _pattern.Groups).Select(number => Group(number) is PyNone ? args[0] : Group(number)).ToArray())),
        "groupdict" => PyRegex.Function("groupdict", ["default"], [PyNone.Instance], 0, args => GroupDict(args[0])),
        "start" or "end" or "span" => PyNativeFunctions.Create(name, ["group"], [0L], 0,
            args => Position(name, GroupNumber(args[0])), ["posonly"]),
        "expand" => PyRegex.Function("expand", ["template"], [null], 1, args => new PyRegexReplacement(_pattern, PyRegex.Text(args[0])).Expand(this)),
        _ => throw Missing(name),
    };
    internal int GroupNumber(object? value)
    {
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
    internal object Group(int number) { Group group = Capture(number); return group.Success ? group.Value : PyNone.Instance; }
    internal object GroupOrEmpty(int number) { Group group = Capture(number); return group.Success ? group.Value : ""; }
    private object Position(string method, int number)
    {
        Group group = Capture(number);
        long start = group.Success ? _text.Point(group.Index) : -1, end = group.Success ? _text.Point(group.Index + group.Length) : -1;
        return method switch { "start" => start, "end" => end, _ => new PyTuple([start, end]) };
    }
    private PyDict GroupDict(object? missing)
    {
        var result = new PyDict();
        foreach (var pair in _pattern.Names) { object value = Group(pair.Value); result.Set(pair.Key, value is PyNone ? missing : value); }
        return result;
    }
    /// <inheritdoc />
    public override string PythonRepr() => "<re.Match object; span=" + PyRepr.Repr(Position("span", 0)) + ", match=" + PyRepr.Repr(Raw.Value) + ">";
}
