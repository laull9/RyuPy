// OptionParser 的对象状态与方法绑定；解析、反馈及帮助保持独立冷文件。
using System;
using System.Collections.Generic;
using System.Linq;
namespace RyuPy.Runtime;

/// <summary>有限 optparse 解析器，拥有独立选项映射和可观察状态。</summary>
public sealed partial class PyOptparseParser : PyNativeObject
{
    internal PyDict Defaults = new();
    internal PyList Options = new();
    internal readonly Dictionary<string, PyOptparseOption> Short = new(StringComparer.Ordinal), Long = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, object?> Data = new(StringComparer.Ordinal);
    private static readonly string[] Methods = ["add_option", "add_options", "set_default", "set_defaults", "get_default_values", "set_usage", "get_usage", "print_usage", "format_help", "print_help", "get_version", "print_version", "error", "exit", "parse_args", "enable_interspersed_args", "disable_interspersed_args", "set_process_default_values", "get_prog_name", "set_description", "get_description"];
    private static readonly PyFunction Factory = PyNativeFunctions.Create("OptionParser",
        ["usage", "option_list", "option_class", "version", "conflict_handler", "description", "formatter", "add_help_option", "prog", "epilog"],
        [null, null, PyOptparse.OptionType, null, "error", null, null, true, null, null], 0, a => new PyOptparseParser(a));
    internal static object? Construct(object?[] args, string[] names, object?[] values) => PyOptparse.Call(Factory, args, names, values);
    private PyOptparseParser(object?[] options)
    {
        if (!ReferenceEquals(options[2], PyOptparse.OptionType)) throw PyOptparse.Unsupported("custom option_class");
        if (options[4] is not string conflict || conflict is not ("error" or "resolve"))
            throw PyOptparse.Own(new PyValueError("invalid conflict_resolution value " + PyRepr.Repr(options[4])));
        if (conflict == "resolve") throw PyOptparse.Unsupported("conflict resolution");
        Data["option_class"] = PyOptparse.OptionType; Data["conflict_handler"] = conflict;
        Data["description"] = options[5]; SetUsage(options[0]); Data["prog"] = options[8]; Data["version"] = options[3];
        Data["allow_interspersed_args"] = true; Data["process_default_values"] = true; Data["epilog"] = options[9];
        if (!PyOptparse.None(options[6])) throw PyOptparse.Unsupported("custom formatter");
        // 默认布局只保存内部快照，不公开假formatter对象。
        InitializeDefaultHelp();
        if (PyOps.Truthy(options[1]))
        {
            if (options[1] is not (PyList or PyTuple)) throw PyOptparse.Unsupported("custom option_list iterable");
            foreach (object? option in PyOps.ToSequence(options[1])) Add([option], [], []);
        }
        if (PyOps.Truthy(Data["version"])) Add(["--version"], ["action", "help"], ["version", "show program's version number and exit"]);
        if (PyOps.Truthy(options[7])) Add(["-h", "--help"], ["action", "help"], ["help", "show this help message and exit"]);
        Data["rargs"] = null; Data["largs"] = null; Data["values"] = null;
    }
    private void SetUsage(object? usage)
    {
        if (PyOptparse.None(usage)) { Data["usage"] = "%prog [options]"; return; }
        if (ReferenceEquals(usage, PyOptparse.SuppressUsage)) { Data["usage"] = null; return; }
        string text = PyOptparse.Text(usage);
        Data["usage"] = text.StartsWith("usage: ", StringComparison.OrdinalIgnoreCase) ? text[7..] : text;
    }
    internal PyOptparseOption Add(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw PyOptparse.Own(new PyIndexError("tuple index out of range"));
        PyOptparseOption option;
        if (args[0] is string) option = new PyOptparseOption(args, names, values);
        else if (args.Length == 1 && names.Length == 0)
            option = args[0] as PyOptparseOption ?? throw PyOptparse.Own(new PyTypeError("not an Option instance: " + PyRepr.Repr(args[0])));
        else throw PyOptparse.Own(new PyTypeError("invalid arguments"));
        var conflicts = option.Short.Where(Short.ContainsKey).Concat(option.Long.Where(Long.ContainsKey)).ToArray();
        if (conflicts.Length > 0) throw PyOptparse.Error("OptionConflictError", "conflicting option string(s): " + string.Join(", ", conflicts), option);
        Options.Items.Add(option); option.Container = this;
        foreach (string flag in option.Short) Short[flag] = option;
        foreach (string flag in option.Long) Long[flag] = option;
        if (!PyOptparse.None(option.Dest))
        {
            if (!ReferenceEquals(option.Default, PyOptparse.NoDefault)) Defaults.Set(option.Dest, option.Default);
            else if (!Defaults.TryGet(option.Dest, out _)) Defaults.Set(option.Dest, null);
        }
        return option;
    }
    internal PyOptparseValues DefaultValues()
    {
        var defaults = Defaults.Copy();
        if (PyOps.Truthy(Data["process_default_values"]))
            foreach (object? item in Options.Items)
            {
                if (item is not PyOptparseOption option) throw PyOptparse.Unsupported("non-Option option_list mutation");
                if (defaults.TryGet(option.Dest, out object? value) && value is string)
                    defaults.Set(option.Dest, option.CheckValue(option.Preferred, value));
            }
        return new PyOptparseValues(defaults);
    }
    /// <inheritdoc/>
    public override string TypeName => "OptionParser";
    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (Data.TryGetValue(name, out object? value)) return value ?? PyNone.Instance;
        return name switch
        {
            "__class__" => PyOptparse.ParserType,
            "defaults" => Defaults, "option_list" => Options,
            "formatter" => throw PyOptparse.Unsupported("public formatter object"),
            "add_option" => Raw(name, Add),
            "add_options" => PyNativeFunctions.Create(name, ["option_list"], [null], 1, a =>
            { foreach (object? option in PyOps.ToSequence(a[0])) Add([option], [], []); return null; }),
            "set_default" => PyNativeFunctions.Create(name, ["dest", "value"], [null, null], 2, a => { Defaults.Set(a[0], a[1]); return null; }),
            "set_defaults" => Raw(name, (args, names, values) =>
            {
                if (args.Length != 0) throw PyOptparse.Own(new PyTypeError("set_defaults() takes 1 positional argument but " + (args.Length + 1) + " were given"));
                for (int i = 0; i < names.Length; i++) Defaults.Set(names[i], values[i]); return null;
            }),
            "get_default_values" => PyNativeFunctions.Create(name, [], [], 0, _ => DefaultValues()),
            "set_usage" => PyNativeFunctions.Create(name, ["usage"], [null], 1, a => { SetUsage(a[0]); return null; }),
            "set_description" => PyNativeFunctions.Create(name, ["description"], [null], 1, a => { Data["description"] = a[0]; return null; }),
            "get_description" => PyNativeFunctions.Create(name, [], [], 0, _ => ExpandProg(Data["description"])),
            "get_prog_name" => PyNativeFunctions.Create(name, [], [], 0, _ => ProgramName()),
            "enable_interspersed_args" => PyNativeFunctions.Create(name, [], [], 0, _ => { Data["allow_interspersed_args"] = true; return null; }),
            "disable_interspersed_args" => PyNativeFunctions.Create(name, [], [], 0, _ => { Data["allow_interspersed_args"] = false; return null; }),
            "set_process_default_values" => PyNativeFunctions.Create(name, ["process"], [null], 1, a => { Data["process_default_values"] = a[0]; return null; }),
            "parse_args" => PyNativeFunctions.Create(name, ["args", "values"], [null, null], 0, a => Parse(a[0], a[1])),
            "get_usage" => PyNativeFunctions.Create(name, [], [], 0, _ => GetUsage()),
            "print_usage" => PyNativeFunctions.Create(name, ["file"], [null], 0, a => { PrintUsage(a[0]); return null; }),
            "get_version" => PyNativeFunctions.Create(name, [], [], 0, _ => GetVersion()),
            "print_version" => PyNativeFunctions.Create(name, ["file"], [null], 0, a => { PrintVersion(a[0]); return null; }),
            "error" => PyNativeFunctions.Create(name, ["msg"], [null], 1, a => { Error(a[0]); return null; }),
            "exit" => PyNativeFunctions.Create(name, ["status", "msg"], [0L, null], 0, a => { Exit(a[0], a[1]); return null; }),
            "format_help" => PyNativeFunctions.Create(name, ["formatter"], [null], 0, a =>
                PyOptparse.None(a[0]) ? FormatHelp() : throw PyOptparse.Unsupported("custom formatter")),
            "print_help" => PyNativeFunctions.Create(name, ["file"], [null], 0, a => PrintHelp(a[0])),
            _ => throw Missing(name),
        };
    }
    private static PyFunction Raw(string name, Func<object?[], string[], object?[], object?> body) =>
        new(name, PySignature.Create(name, [], [], []), (_, args, names, values) => body(args, names, values), [], false);
    internal string ProgramName() => PyOptparse.None(Data["prog"])
        ? PyOsPath.Text(PyOps.CallValue(PyOsPath.CreateAttributes()["basename"], [PyOps.GetItem(PyModules.Import("sys").GetAttr("argv")!, 0L)], [], []))
        : PyOptparse.Text(Data["prog"]);
    internal object? ExpandProg(object? value)
    {
        if (PyOptparse.None(value)) throw PyOptparse.Own(new PyAttributeError("'NoneType' object has no attribute 'replace'"));
        return PyOptparse.Text(value).Replace("%prog", ProgramName(), StringComparison.Ordinal);
    }
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name == "defaults") { Defaults = value as PyDict ?? throw PyOptparse.Unsupported("non-dict defaults replacement"); return; }
        if (name == "option_list") { Options = value as PyList ?? throw PyOptparse.Unsupported("non-list option_list replacement"); return; }
        if (name.StartsWith('_') || Methods.Contains(name) || name is "option_class" or "conflict_handler" or "formatter")
            throw PyOptparse.Unsupported("parser method or structural mutation");
        Data[name] = value;
    }
    /// <inheritdoc/>
    public override void DelAttr(string name)
    {
        if (name.StartsWith('_') || Methods.Contains(name) || name is "defaults" or "option_list" or "option_class" or "conflict_handler" or "formatter" || Data.ContainsKey(name))
            throw PyOptparse.Unsupported("parser state deletion");
        throw Missing(name);
    }
}
