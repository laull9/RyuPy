// Option 的真实声明对象：先按CP检查错误，再对合法但未实现的动作明确拒绝。
using System;
using System.Collections.Generic;
using System.Linq;
namespace RyuPy.Runtime;

/// <summary>单值选项及布尔动作的可观察元数据。</summary>
public sealed class PyOptparseOption : PyNativeObject
{
    private static readonly string[] Fields = ["action", "type", "dest", "default", "nargs", "const", "choices", "callback", "callback_args", "callback_kwargs", "help", "metavar"];
    internal readonly List<string> Short = [], Long = [];
    internal readonly Dictionary<string, object?> Data = new(StringComparer.Ordinal);
    internal object? Container;
    internal string Action => (string)Data["action"]!;
    internal string? ValueType => Data["type"] as string;
    internal object? Dest => Data["dest"];
    internal object? Default => Data["default"];
    internal string Preferred => Long.Count > 0 ? Long[0] : Short[0];
    internal static object? Construct(object?[] args, string[] names, object?[] values) => new PyOptparseOption(args, names, values);

    internal PyOptparseOption(object?[] args, string[] names, object?[] values)
    {
        foreach (object? arg in args)
        {
            if (!PyOps.Truthy(arg)) continue;
            if (arg is not string flag) throw PyOptparse.Unsupported("custom option string protocol");
            long count = PyText.Length(flag);
            if (count < 2) throw Failure("invalid option string " + PyRepr.Repr(flag) + ": must be at least two characters long");
            if (count == 2)
            {
                if (flag[0] != '-' || flag[1] == '-') throw Failure("invalid short option string " + PyRepr.Repr(flag) + ": must be of the form -x, (x any non-dash char)");
                Short.Add(flag);
            }
            else
            {
                if (!flag.StartsWith("--", StringComparison.Ordinal) || flag[2] == '-') throw Failure("invalid long option string " + PyRepr.Repr(flag) + ": must start with --, followed by non-dash");
                Long.Add(flag);
            }
        }
        if (Short.Count + Long.Count == 0) throw PyOptparse.Own(new PyTypeError("at least one option string must be supplied"));
        foreach (string field in Fields) Data[field] = field == "default" ? PyOptparse.NoDefault : null;
        var unknown = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < names.Length; i++)
        {
            if (!seen.Add(names[i])) throw PyOptparse.Own(new PyTypeError("Option() got multiple values for keyword argument '" + names[i] + "'"));
            if (Data.ContainsKey(names[i])) Data[names[i]] = values[i]; else unknown.Add(names[i]);
        }
        if (unknown.Count > 0) throw Failure("invalid keyword arguments: " + string.Join(", ", unknown.Order(StringComparer.Ordinal)));
        Validate();
    }

    private void Validate()
    {
        if (PyOptparse.None(Data["action"])) Data["action"] = "store";
        if (Data["action"] is not string action || action is not ("store" or "store_const" or "store_true" or "store_false" or "append" or "append_const" or "count" or "callback" or "help" or "version"))
            throw Failure("invalid action: " + PyRepr.Repr(Data["action"]));
        bool typed = action is "store" or "append" or "callback";
        if (PyOptparse.None(Data["type"]))
        { if (action is "store" or "append") Data["type"] = PyOptparse.None(Data["choices"]) ? "string" : "choice"; }
        else
        {
            if (Data["type"] is PyType type) Data["type"] = type.Name;
            if (Equals(Data["type"], "str")) Data["type"] = "string";
            if (Data["type"] is not string typeName || typeName is not ("string" or "int" or "long" or "float" or "complex" or "choice"))
                throw Failure("invalid option type: " + PyRepr.Repr(Data["type"]));
            if (!typed) throw Failure("must not supply a type for action " + PyRepr.Repr(action));
        }
        if (Equals(Data["type"], "choice"))
        {
            if (PyOptparse.None(Data["choices"])) throw Failure("must supply a list of choices for type 'choice'");
            if (Data["choices"] is not (PyList or PyTuple)) throw Failure("choices must be a list of strings ('" + PyRepr.TypeName(Data["choices"]) + "' supplied)");
        }
        else if (!PyOptparse.None(Data["choices"])) throw Failure("must not supply choices for type " + PyRepr.Repr(Data["type"]));
        if (PyOptparse.None(Data["dest"]) && (action is "store" or "store_const" or "store_true" or "store_false" or "append" or "append_const" or "count" || !PyOptparse.None(Data["type"])))
            Data["dest"] = Long.Count > 0 ? Long[0][2..].Replace('-', '_') : PyText.At(Short[0], 1);
        if (action is not ("store_const" or "append_const") && !PyOptparse.None(Data["const"]))
            throw Failure("'const' must not be supplied for action " + PyRepr.Repr(action));
        if (typed)
        { if (PyOptparse.None(Data["nargs"])) Data["nargs"] = 1L; }
        else if (!PyOptparse.None(Data["nargs"])) throw Failure("'nargs' must not be supplied for action " + PyRepr.Repr(action));
        if (action == "callback")
        {
            if (!PyOps.Truthy(PyOps.CallBuiltin("callable", [Data["callback"]], [], []))) throw Failure("callback not callable: " + PyRepr.Repr(Data["callback"]));
            if (!PyOptparse.None(Data["callback_args"]) && Data["callback_args"] is not PyTuple) throw Failure("callback_args, if supplied, must be a tuple: not " + PyRepr.Repr(Data["callback_args"]));
            if (!PyOptparse.None(Data["callback_kwargs"]) && Data["callback_kwargs"] is not PyDict) throw Failure("callback_kwargs, if supplied, must be a dict: not " + PyRepr.Repr(Data["callback_kwargs"]));
        }
        else
        {
            if (!PyOptparse.None(Data["callback"])) throw Failure("callback supplied (" + PyRepr.Repr(Data["callback"]) + ") for non-callback option");
            if (!PyOptparse.None(Data["callback_args"])) throw Failure("callback_args supplied for non-callback option");
            if (!PyOptparse.None(Data["callback_kwargs"])) throw Failure("callback_kwargs supplied for non-callback option");
        }
        if (action is not ("store" or "store_true" or "store_false" or "help" or "version")) throw PyOptparse.Unsupported("action " + action);
        if (ValueType is not (null or "string" or "int" or "choice")) throw PyOptparse.Unsupported("type " + ValueType);
        if (typed && (!PyInt.IsInt(Data["nargs"]) || PyInt.ToBigInteger(Data["nargs"]!) != 1)) throw PyOptparse.Unsupported("multi-value nargs");
    }
    private PyBaseException Failure(string message) => PyOptparse.Error("OptionError", message, this);
    /// <inheritdoc/>
    public override string TypeName => "Option";
    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (Data.TryGetValue(name, out object? value)) return value ?? PyNone.Instance;
        return name switch
        {
            "__class__" => PyOptparse.OptionType,
            "container" when Container is not null => Container,
            "takes_value" => PyNativeFunctions.Create(name, [], [], 0, _ => ValueType is not null),
            "get_opt_string" => PyNativeFunctions.Create(name, [], [], 0, _ => Preferred),
            "check_value" => PyNativeFunctions.Create(name, ["opt", "value"], [null, null], 2, a => CheckValue(PyOptparse.Text(a[0]), a[1])),
            _ => throw Missing(name),
        };
    }
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name is "dest" or "default" or "help" or "metavar" or "choices") { Data[name] = value; return; }
        if (Fields.Contains(name) || name.StartsWith('_') || name is "container" or "takes_value" or "get_opt_string" or "check_value")
            throw PyOptparse.Unsupported("Option structural or method mutation");
        Data[name] = value;
    }
    /// <inheritdoc/>
    public override void DelAttr(string name)
    {
        if (Fields.Contains(name) || name.StartsWith('_') || name is "container" or "takes_value" or "get_opt_string" or "check_value") throw PyOptparse.Unsupported("Option structural deletion");
        if (!Data.Remove(name)) throw Missing(name);
    }
    /// <inheritdoc/>
    public override string PythonStr() => string.Join("/", Short.Concat(Long));
    /// <inheritdoc/>
    public override string PythonRepr() => PyOptparse.ReprIdentity(this, "Option", PythonStr());

    internal object? CheckValue(string opt, object? value)
    {
        if (ValueType == "choice")
        {
            if (PyOps.Contains(Data["choices"], value)) return value;
            var choices = PyOps.ToSequence(Data["choices"]);
            throw PyOptparse.Error("OptionValueError", "option " + opt + ": invalid choice: " + PyRepr.Repr(value) + " (choose from " + string.Join(", ", choices.Select(PyRepr.Repr)) + ")");
        }
        if (ValueType != "int") return value;
        if (value is not string text) throw PyOptparse.Unsupported("non-string integer option conversion");
        int radix = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 16
            : text.StartsWith("0b", StringComparison.OrdinalIgnoreCase) ? 2 : text.StartsWith('0') ? 8 : 10;
        string input = radix == 2 ? text.Length == 2 ? "0" : text[2..] : text;
        long depth = PyTraceback.CaptureDepth();
        try { return PyIntText.Parse(input, radix); }
        catch (PyValueError error)
        {
            PyErrors.PrepareRaise(error); PyTraceback.PopTo(depth + 1);
            object? previous = PyErrors.EnterHandler(error);
            try { throw PyOptparse.Error("OptionValueError", "option " + opt + ": invalid integer value: " + PyRepr.Repr(value)); }
            finally { PyErrors.LeaveHandler(previous); }
        }
    }
}
