// Logger 的过滤先于记录创建和格式化；传播只检查各 handler 等级，不重复应用祖先 Logger 等级。
using System;
namespace RyuPy.Runtime;
/// <summary>按名字驻留的原生日志记录器。</summary>
public sealed class PyLogger : PyNativeObject
{
    internal readonly string Name;
    internal readonly bool IsRoot;
    internal PyLogger? Parent;
    private long _level;
    private bool _disabled, _propagate = true;
    private PyList _handlers = new();
    internal PyLogger(string name, long level, bool isRoot = false) { Name = name; _level = level; IsRoot = isRoot; }
    /// <inheritdoc/>
    public override string TypeName => IsRoot ? "RootLogger" : "Logger";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "name" => Name, "level" => _level, "disabled" => _disabled, "propagate" => _propagate,
        "parent" => Parent ?? (object)PyNone.Instance, "handlers" => _handlers,
        "setLevel" => PyLogging.One(name, value => { _level = PyLogging.Level(value); return null; }),
        "getEffectiveLevel" => PyLogging.Zero(name, () => Effective()),
        "isEnabledFor" => PyLogging.One(name, value => Enabled(PyLogging.Level(value))),
        "addHandler" => PyLogging.One(name, value =>
        { if (value is not PyLoggingHandler) throw new PyTypeError("handler must be StreamHandler");
            if (!_handlers.Items.Contains(value)) _handlers.Items.Add(value); return null; }),
        "removeHandler" => PyLogging.One(name, value => { _handlers.Items.Remove(value); return null; }),
        "debug" => LogMethod(name, 10), "info" => LogMethod(name, 20), "warning" or "warn" => LogMethod(name, 30),
        "error" => LogMethod(name, 40), "critical" or "fatal" => LogMethod(name, 50), "log" => LogMethod(name, null),
        _ => throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        switch (name)
        {
            case "level": _level = PyLogging.Level(value); break;
            case "disabled": _disabled = PyOps.Truthy(value); break;
            case "propagate": _propagate = PyOps.Truthy(value); break;
            case "handlers": _handlers = value as PyList ?? throw new PyTypeError("handlers must be list"); break;
            default: base.SetAttr(name, value); break;
        }
    }
    private long Effective()
    { for (PyLogger? item = this; item is not null; item = item.Parent) if (item._level != 0) return item._level; return 0; }
    private bool Enabled(long level) => !_disabled && level > PyLogging.DisabledLevel && level >= Effective();
    private PyFunction LogMethod(string name, long? fixedLevel)
    {
        string[] parameters = fixedLevel.HasValue ? ["msg", "args", "kwargs"] : ["level", "msg", "args", "kwargs"];
        string[] kinds = fixedLevel.HasValue ? ["pos", "varpos", "varkw"] : ["pos", "pos", "varpos", "varkw"];
        var flags = new string[parameters.Length]; Array.Fill(flags, "0");
        var signature = PySignature.Create(name, parameters, kinds, flags);
        return new PyFunction(name, signature, (_, args, names, values) =>
        {
        object?[] bound = PyBinder.Bind(signature, args, names, values, []);
        int offset = fixedLevel.HasValue ? 0 : 1;
        long level = fixedLevel ?? (bound[0] is long or int or bool ? PyLogging.Level(bound[0]) : throw new PyTypeError("level must be an integer"));
        if (!Enabled(level)) return PyNone.Instance;
        if (((PyDict)bound[offset + 2]!).Count != 0) throw new PyNotImplementedError("logging extra/exception/stack options are not supported yet");
        var record = new PyLoggingRecord(Name, level, bound[offset], ((PyTuple)bound[offset + 1]!).Items);
        int found = 0;
        for (PyLogger? item = this; item is not null; item = item._propagate ? item.Parent : null)
        {
            for (int i = 0; i < item._handlers.Count; i++)
            {
                found++; var handler = item._handlers.Items[i] as PyLoggingHandler
                    ?? throw new PyTypeError("handler must be StreamHandler");
                if (level >= handler.Level) handler.Emit(record);
            }
        }
        if (found == 0)
        {
            object? fallback = PyLogging.Option("lastResort", null);
            if (fallback is PyLoggingHandler handler) { if (level >= handler.Level) handler.Emit(record); }
            else if (fallback is not (null or PyNone)) throw new PyNotImplementedError("custom lastResort handler is not supported");
            else if (!PyLogging.WarnedNoHandler && PyOps.Truthy(PyLogging.Option("raiseExceptions", true)))
            {
                PySystem.Write(PySystem.Stderr, "No handlers could be found for logger \"" + Name + "\"\n");
                PyLogging.WarnedNoHandler = true;
            }
        }
        return PyNone.Instance;
        }, []);
    }
}
