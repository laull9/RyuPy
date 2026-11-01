// 参数声明只支持单值存储与布尔开关；高级动作不以不完整对象伪装支持。
using System;
using System.Collections.Generic;
using System.Linq;
namespace RyuPy.Runtime;
/// <summary>单值命令行参数解析器。</summary>
public sealed partial class PyArgparseParser : PyNativeObject
{
    private readonly List<PyArgparseAction> _actions = new();
    private readonly string _prog;
    private readonly string? _usage, _description, _epilog;
    private readonly bool _abbreviate;
    internal PyArgparseParser(object?[] options)
    {
        _prog = PyArgparse.OptionalString(options[0]) ?? DefaultProgram();
        _usage = PyArgparse.OptionalString(options[1]); _description = PyArgparse.OptionalString(options[2]);
        _epilog = PyArgparse.OptionalString(options[3]); _abbreviate = PyOps.Truthy(options[5]);
        if (PyOps.Truthy(options[4])) _actions.Add(new() { Flags = ["-h", "--help"], Dest = "help", Kind = "help", Help = "show this help message and exit" });
    }
    private static string DefaultProgram()
    {
        object? argv = PyModules.Import("sys").GetAttr("argv");
        object? first = PyOps.GetItem(argv!, 0L);
        return System.IO.Path.GetFileName(PyCast.ToStr(first));
    }
    /// <inheritdoc/>
    public override string TypeName => "ArgumentParser";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "prog" => _prog, "description" => (object?)_description ?? PyNone.Instance, "epilog" => (object?)_epilog ?? PyNone.Instance,
        "add_argument" => AddFunction(),
        "parse_args" => PyNativeFunctions.Create(name, ["args", "namespace"], [null, null], 0, a => Parse(a[0], a[1])),
        "format_usage" => PyNativeFunctions.Create(name, [], [], 0, _ => Usage()),
        "format_help" => PyNativeFunctions.Create(name, [], [], 0, _ => Help()),
        "print_usage" => PyNativeFunctions.Create(name, ["file"], [null], 0, a => { PySystem.Write(a[0] is null or PyNone ? PySystem.Stdout : a[0], Usage()); return null; }),
        "print_help" => PyNativeFunctions.Create(name, ["file"], [null], 0, a => { PySystem.Write(a[0] is null or PyNone ? PySystem.Stdout : a[0], Help()); return null; }),
        "error" => PyNativeFunctions.Create(name, ["message"], [null], 1, a => { Error(PyCast.ToStr(a[0])); return null; }),
        "exit" => PyNativeFunctions.Create(name, ["status", "message"], [0L, null], 0, a =>
        { if (a[1] is not (null or PyNone)) PySystem.Write(PySystem.Stderr, PyCast.ToStr(a[1])); throw new PySystemExit(a[0]); }),
        _ => throw Missing(name),
    };
    private PyFunction AddFunction() => new("add_argument",
        PySignature.Create("add_argument", ["args", "kwargs"], ["varpos", "varkw"], ["0", "0"]),
        (_, args, names, values) => Add(args, names, values), []);
    private object Add(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw new PyTypeError("add_argument() requires a name");
        string[] flags = args.Select(a => a as string ?? throw new PyTypeError("argument name must be str")).ToArray();
        bool positional = !flags[0].StartsWith('-');
        if (positional && flags.Length != 1) throw new PyTypeError("positional argument takes one name");
        if (!positional && flags.Any(f => !f.StartsWith('-') || f.Length < 2)) throw new PyValueError("invalid option string");
        var options = new Dictionary<string, object?>();
        for (int i = 0; i < names.Length; i++)
            if (!options.TryAdd(names[i], values[i])) throw new PyTypeError("duplicate argument keyword: " + names[i]);
        string kind = options.Remove("action", out var action) ? PyCast.ToStr(action) : "store";
        if (kind is not ("store" or "store_true" or "store_false")) throw new PyNotImplementedError("argparse action not supported: " + kind);
        if (positional && kind != "store") throw new PyNotImplementedError("positional boolean actions are not supported");
        string dest = positional ? flags[0] : (flags.FirstOrDefault(f => f.StartsWith("--", StringComparison.Ordinal)) ?? flags[0]).TrimStart('-').Replace('-', '_');
        if (options.Remove("dest", out var explicitDest))
        { if (positional) throw new PyValueError("dest supplied twice for positional argument"); dest = PyCast.ToStr(explicitDest); }
        bool required = positional;
        if (options.Remove("required", out var requiredValue))
        { if (positional) throw new PyTypeError("required is invalid for positionals"); required = PyOps.Truthy(requiredValue); }
        options.Remove("help", out var help); options.Remove("metavar", out var metavar);
        options.Remove("type", out var convert); options.Remove("choices", out var choices);
        if (kind != "store" && (convert is not (null or PyNone) || choices is not (null or PyNone)))
            throw new PyTypeError("boolean action does not accept type or choices");
        if (convert is not (null or PyNone) && !PyOps.Truthy(PyOps.CallBuiltin("callable", [convert], [], [])))
            throw new PyValueError("type is not callable");
        object? defaultValue = options.Remove("default", out var givenDefault) ? givenDefault : kind switch { "store_true" => false, "store_false" => true, _ => null };
        if (options.Count != 0) throw new PyNotImplementedError("argparse argument options not supported: " + string.Join(", ", options.Keys));
        if (!positional && flags.Any(flag => _actions.Any(a => a.Flags.Contains(flag)))) throw new PyValueError("conflicting option string");
        var result = new PyArgparseAction { Flags = positional ? [] : flags, Positional = positional, Dest = dest, Kind = kind,
            Required = required, Default = defaultValue, Convert = convert, Choices = choices,
            Help = PyArgparse.OptionalString(help), Metavar = PyArgparse.OptionalString(metavar) };
        _actions.Add(result); return result;
    }
}
