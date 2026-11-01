// 日志模块身份注册与等级表；原生实现独立编写，不复制 CPython 源码。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;
/// <summary>logging 首批模块入口，明确保留可配置的 lastResort 与 raiseExceptions。</summary>
public static partial class PyLogging
{
    private static readonly Dictionary<string, PyLogger> Loggers = new(StringComparer.Ordinal);
    internal static long DisabledLevel;
    internal static bool WarnedNoHandler;
    internal static readonly PyLoggingFormatter DefaultFormatter = new(null);
    internal static readonly PyLogger Root = new("root", 30, true);
    private static readonly PyLoggingHandler Fallback = new(null, true) { Level = 30 };
    /// <summary>创建原生日志模块属性。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["NOTSET"] = 0L, ["DEBUG"] = 10L, ["INFO"] = 20L, ["WARNING"] = 30L, ["WARN"] = 30L,
        ["ERROR"] = 40L, ["CRITICAL"] = 50L, ["FATAL"] = 50L,
        ["basicConfig"] = BasicConfigFunction(),
        ["getLevelName"] = PyNativeFunctions.Create("getLevelName", ["level"], [null], 1, a => GetLevelName(a[0])),
        ["debug"] = RootMethod("debug"), ["info"] = RootMethod("info"),
        ["warning"] = RootMethod("warning"), ["warn"] = RootMethod("warn"),
        ["error"] = RootMethod("error"), ["critical"] = RootMethod("critical"),
        ["fatal"] = RootMethod("fatal"), ["log"] = RootMethod("log"),
        ["disable"] = PyNativeFunctions.Create("disable", ["level"], [50L], 0,
            a => { DisabledLevel = Level(a[0]); return null; }),
        ["Logger"] = new PyNativeType("Logger", "logging", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("Logger", ["name", "level"], [null, 0L], 1,
                a => new PyLogger(PyCast.ToStr(a[0]), Level(a[1]))), args, names, values), value => value is PyLogger),
        ["RootLogger"] = new PyNativeType("RootLogger", "logging", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("RootLogger", ["level"], [null], 1,
                a => new PyLogger("root", Level(a[0]), true)), args, names, values), value => value is PyLogger { IsRoot: true }, "Logger"),
        ["_StderrHandler"] = new PyNativeType("_StderrHandler", "logging", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("_StderrHandler", ["level"], [0L], 0,
                a => new PyLoggingHandler(null, true) { Level = Level(a[0]) }), args, names, values),
            value => value is PyLoggingHandler { DynamicError: true }, "StreamHandler"),
        ["root"] = Root, ["lastResort"] = Fallback, ["raiseExceptions"] = true,
        ["getLogger"] = PyNativeFunctions.Create("getLogger", ["name"], [null], 0, a => GetLogger(a[0])),
        ["StreamHandler"] = new PyNativeType("StreamHandler", "logging", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("StreamHandler", ["stream"], [null], 0, a => new PyLoggingHandler(a[0])), args, names, values),
            value => value is PyLoggingHandler),
        ["Formatter"] = new PyNativeType("Formatter", "logging", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("Formatter", ["fmt", "datefmt", "style", "validate", "defaults"],
                [null, null, "%", true, null], 0, a =>
                {
                    if (a[1] is not (null or PyNone) || !Equals(a[2], "%") || !Equals(a[3], true) || a[4] is not (null or PyNone))
                        throw new PyNotImplementedError("logging supports only default percent Formatter options");
                    return new PyLoggingFormatter(a[0] is null or PyNone ? null : PyCast.ToStr(a[0]));
                }, ["pos", "pos", "pos", "pos", "kwonly"]), args, names, values), value => value is PyLoggingFormatter),
    };
    internal static object? Option(string name, object? fallback)
    {
        try { return PyModules.Import("logging").GetAttr(name); }
        catch (PyImportError) { return fallback; }
    }
    internal static PyLogger GetLogger(object? name)
    {
        if (name is null or PyNone || name is "" or "root") return Root;
        string text = name as string ?? throw new PyTypeError("A logger name must be a string");
        lock (Loggers)
        {
            if (Loggers.TryGetValue(text, out var existing)) return existing;
            var logger = new PyLogger(text, 0); Loggers[text] = logger;
            foreach (var item in Loggers.Values)
            {
                item.Parent = Root; string prefix = item.Name;
                while (prefix.LastIndexOf('.') is int at && at >= 0)
                {
                    prefix = prefix[..at];
                    if (Loggers.TryGetValue(prefix, out var parent)) { item.Parent = parent; break; }
                }
            }
            return logger;
        }
    }
    internal static long Level(object? value) => value switch
    {
        long number => number, int number => number, bool flag => flag ? 1 : 0,
        string name => name switch { "NOTSET" => 0, "DEBUG" => 10, "INFO" => 20, "WARN" or "WARNING" => 30,
            "ERROR" => 40, "FATAL" or "CRITICAL" => 50, _ => throw new PyValueError("Unknown level: " + name) },
        _ => throw new PyTypeError("Level not an integer or a valid string"),
    };
    internal static string LevelName(long level) => level switch
    { 0 => "NOTSET", 10 => "DEBUG", 20 => "INFO", 30 => "WARNING", 40 => "ERROR", 50 => "CRITICAL", _ => "Level " + level };
    internal static PyFunction One(string name, Func<object?, object?> body) =>
        PyNativeFunctions.Create(name, [name switch { "setLevel" or "isEnabledFor" => "level", "setFormatter" => "fmt",
            "addHandler" or "removeHandler" => "hdlr", "setStream" => "stream", _ => "value" }], [null], 1, a => body(a[0]));
    internal static PyFunction Zero(string name, Func<object?> body) => PyNativeFunctions.Create(name, [], [], 0, _ => body());
}
