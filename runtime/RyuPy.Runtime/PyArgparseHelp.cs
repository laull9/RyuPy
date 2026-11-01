// 默认帮助布局的短文本路径；与解析共用参数元数据，不硬编码用户的参数名或输出。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace RyuPy.Runtime;
public sealed partial class PyArgparseParser
{
    private static string ChoiceNames(PyArgparseAction action, bool repr)
    {
        var parts = new List<string>(); var iterator = PyIter.Create(action.Choices);
        while (iterator.MoveNext()) parts.Add(repr ? PyRepr.Repr(iterator.Current()) : PyRepr.Str(iterator.Current()));
        return string.Join(repr ? ", " : ",", parts);
    }
    private static string Meta(PyArgparseAction action) => action.Metavar ??
        (action.Choices is null or PyNone ? action.Positional ? action.Dest : action.Dest.ToUpperInvariant() : "{" + ChoiceNames(action, false) + "}");
    private string Usage()
    {
        if (_usage is not null) return "usage: " + _usage.Replace("%(prog)s", _prog, StringComparison.Ordinal) + "\n";
        var parts = new List<string>();
        foreach (var action in _actions.Where(a => !a.Positional))
        {
            string part = action.Flags[0] + (action.Kind == "store" ? " " + Meta(action) : "");
            parts.Add(action.Required ? part : "[" + part + "]");
        }
        parts.AddRange(_actions.Where(a => a.Positional).Select(Meta));
        return "usage: " + _prog + (parts.Count == 0 ? "" : " " + string.Join(" ", parts)) + "\n";
    }
    private static string Invocation(PyArgparseAction action) => action.Positional ? Meta(action) :
        string.Join(", ", action.Flags.Select(flag => flag + (action.Kind == "store" ? " " + Meta(action) : "")));
    private string Help()
    {
        var output = new StringBuilder(Usage());
        if (_description is not null) output.Append('\n').Append(_description).Append('\n');
        int width = Math.Min(24, _actions.Count == 0 ? 2 : _actions.Max(a => Invocation(a).Length + 4));
        foreach (bool positional in new[] { true, false })
        {
            var group = _actions.Where(a => a.Positional == positional).ToArray();
            if (group.Length == 0) continue;
            output.Append('\n').Append(positional ? "positional arguments:\n" : "options:\n");
            foreach (var action in group)
            {
                string invocation = Invocation(action); string? help = action.Help;
                output.Append("  ").Append(invocation);
                if (!string.IsNullOrEmpty(help))
                {
                    if (invocation.Length + 2 > width - 2) output.Append('\n').Append(' ', width);
                    else output.Append(' ', width - invocation.Length - 2);
                    output.Append(help.Replace("%(prog)s", _prog, StringComparison.Ordinal)
                        .Replace("%(default)s", PyRepr.Str(action.Default), StringComparison.Ordinal));
                }
                output.Append('\n');
            }
        }
        if (_epilog is not null) output.Append('\n').Append(_epilog).Append('\n');
        return output.ToString();
    }
}
