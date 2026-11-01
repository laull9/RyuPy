// 逐项解析保留转换/帮助/错误发生次序；默认字符串只对未显式指定的参数转换。
using System;
using System.Collections.Generic;
using System.Linq;
namespace RyuPy.Runtime;
public sealed partial class PyArgparseParser
{
    private object Parse(object? arguments, object? target)
    {
        var result = target is null or PyNone ? new PyArgparseNamespace() : target as PyArgparseNamespace
            ?? throw new PyTypeError("namespace must be argparse.Namespace");
        var tokens = new List<string>();
        object? source = arguments;
        bool useArgv = source is null or PyNone;
        if (useArgv) source = PyModules.Import("sys").GetAttr("argv");
        var iterator = PyIter.Create(source); int index = 0;
        while (iterator.MoveNext())
        {
            if (useArgv && index++ == 0) continue;
            tokens.Add(iterator.Current() as string ?? throw new PyTypeError("argument must be str"));
        }
        foreach (var action in _actions)
            if (action.Kind != "help" && !result.Contains(action.Dest)) result.SetAttr(action.Dest, action.Default);
        var seen = new HashSet<PyArgparseAction>();
        var positional = _actions.Where(a => a.Positional).ToArray(); int position = 0;
        var extras = new List<string>(); bool options = true;
        for (int i = 0; i < tokens.Count; i++)
        {
            string token = tokens[i];
            if (options && token == "--") { options = false; continue; }
            PyArgparseAction? action;
            string? inline = null;
            if (options && LooksOptional(token))
            {
                string key = token;
                int equals = token.IndexOf('=');
                if (equals >= 0) { key = token[..equals]; inline = token[(equals + 1)..]; }
                action = _actions.FirstOrDefault(a => a.Flags.Contains(key));
                if (action is null && key.Length > 2 && key[1] != '-')
                {
                    action = _actions.FirstOrDefault(a => a.Flags.Contains(key[..2]));
                    if (action is not null)
                    {
                        if (action.Kind == "store") inline = token[2..];
                        else tokens.Insert(i + 1, "-" + token[2..]);
                    }
                }
                if (action is null && _abbreviate && key.StartsWith("--", StringComparison.Ordinal))
                {
                    var matches = _actions.SelectMany(a => a.Flags.Select(f => (Action: a, Flag: f)))
                        .Where(p => p.Flag.StartsWith(key, StringComparison.Ordinal)).ToArray();
                    if (matches.Length > 1) Error("ambiguous option: " + key + " could match " + string.Join(", ", matches.Select(p => p.Flag)));
                    if (matches.Length == 1) action = matches[0].Action;
                }
                if (action is null)
                {
                    extras.Add(token); continue;
                }
                if (action.Kind != "store" && inline is not null) Error("argument " + Label(action) + ": ignored explicit argument " + PyRepr.Repr(inline));
                if (action.Kind == "help") { PySystem.Write(PySystem.Stdout, Help()); throw new PySystemExit(0L); }
                if (action.Kind == "store")
                {
                    if (inline is null)
                    {
                        if (i + 1 >= tokens.Count || LooksOptional(tokens[i + 1]) || tokens[i + 1] == "--")
                            Error("argument " + Label(action) + ": expected one argument");
                        inline = tokens[++i];
                    }
                    result.SetAttr(action.Dest, Convert(action, inline));
                }
                else result.SetAttr(action.Dest, action.Kind == "store_true");
            }
            else
            {
                if (position >= positional.Length) { extras.Add(token); continue; }
                action = positional[position++]; result.SetAttr(action.Dest, Convert(action, token));
            }
            seen.Add(action);
        }
        string[] missing = _actions.Where(a => a.Required && !seen.Contains(a)).Select(Label).ToArray();
        if (missing.Length != 0) Error("the following arguments are required: " + string.Join(", ", missing));
        foreach (var action in _actions)
            if (!seen.Contains(action) && action.Default is string text && ReferenceEquals(result.GetAttr(action.Dest), action.Default))
                result.SetAttr(action.Dest, Convert(action, text, false));
        if (extras.Count != 0) Error("unrecognized arguments: " + string.Join(" ", extras));
        return result;
    }
    private bool LooksOptional(string value)
    {
        if (value.Length < 2 || value[0] != '-') return false;
        if (NegativeNumber(value) &&
            !_actions.Any(a => a.Flags.Any(f => NegativeNumber(f)))) return false;
        return true;
    }
    private static bool NegativeNumber(string text) =>
        System.Text.RegularExpressions.Regex.IsMatch(text, @"^-([0-9]+|[0-9]*\.[0-9]+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private object? Convert(PyArgparseAction action, string text, bool checkChoices = true)
    {
        object? value = text;
        if (action.Convert is not (null or PyNone))
        {
            try { value = PyOps.CallValue(action.Convert, [text], [], []); }
            catch (PyException exception) when (exception is PyValueError or PyTypeError)
            {
                string type;
                try { type = PyCast.ToStr(PyObject.GetAttr(action.Convert, "__name__")); }
                catch (PyAttributeError) { type = PyRepr.Repr(action.Convert); }
                Error("argument " + Label(action) + ": invalid " + type + " value: " + PyRepr.Repr(text));
            }
        }
        if (checkChoices && action.Choices is not (null or PyNone))
        {
            bool found = PyOps.Contains(action.Choices, value);
            if (!found) Error("argument " + Label(action) + ": invalid choice: " + PyRepr.Repr(value) + " (choose from " + ChoiceNames(action, true) + ")");
        }
        return value;
    }
    private static string Label(PyArgparseAction action) => action.Positional ? action.Dest : string.Join('/', action.Flags);
    private void Error(string message)
    {
        PySystem.Write(PySystem.Stderr, Usage());
        PySystem.Write(PySystem.Stderr, _prog + ": error: " + message + "\n");
        throw new PySystemExit(2L);
    }
}
