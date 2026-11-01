// 根日志基础配置：保留已有处理器时不校验剩余选项；强制重配按移除、关闭、创建顺序执行。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;
public static partial class PyLogging
{
    private static PyFunction BasicConfigFunction() => new("basicConfig",
        PySignature.Create("basicConfig", ["kwargs"], ["varkw"], ["0"]),
        (_, args, names, values) =>
        {
            if (args.Length != 0) throw new PyTypeError("basicConfig() takes 0 positional arguments");
            var options = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++)
                if (!options.TryAdd(names[i], values[i])) throw new PyTypeError("basicConfig() got multiple values for " + names[i]);
            Configure(options); return null;
        }, []);

    private static void Configure(Dictionary<string, object?> options)
    {
        var handlers = (PyList)Root.GetAttr("handlers")!;
        if (options.Remove("force", out var force) && PyOps.Truthy(force))
        {
            foreach (object? handler in handlers.Items.ToArray())
            {
                handlers.Items.Remove(handler);
                PyOps.CallValue(PyObject.GetAttr(handler, "close"), [], [], []);
            }
        }
        if (handlers.Count != 0) return;
        // 高级配置不能悄悄退化为默认流；普通未知键则保留 CPython 安装处理器后报错的顺序。
        foreach (string key in new[] { "filename", "filemode", "handlers", "datefmt", "style", "encoding", "errors" })
            if (options.ContainsKey(key)) throw new PyNotImplementedError("basicConfig option not supported: " + key);
        options.Remove("stream", out var stream);
        options.Remove("format", out var format);
        var formatter = new PyLoggingFormatter(format is null or PyNone ? "%(levelname)s:%(name)s:%(message)s" : PyCast.ToStr(format));
        var installed = new PyLoggingHandler(stream);
        PyOps.CallValue(installed.GetAttr("setFormatter"), [formatter], [], []);
        handlers.Items.Add(installed);
        if (options.Remove("level", out var level) && level is not (null or PyNone)) Root.SetAttr("level", Level(level));
        if (options.Count != 0) throw new PyValueError("Unrecognised argument(s): " + string.Join(", ", options.Keys));
    }

    private static PyFunction RootMethod(string name) => new(name,
        PySignature.Create(name, ["args", "kwargs"], ["varpos", "varkw"], ["0", "0"]),
        (_, args, names, values) =>
        {
            if (((PyList)Root.GetAttr("handlers")!).Count == 0) Configure(new());
            return PyOps.CallValue(Root.GetAttr(name), args, names, values);
        }, []);

    private static readonly PyDict NumberNames = CreateLevelMap(false);
    private static readonly PyDict NameNumbers = CreateLevelMap(true);
    private static PyDict CreateLevelMap(bool reverse)
    {
        var result = new PyDict();
        foreach (long value in new long[] { 0, 10, 20, 30, 40, 50 })
            if (reverse) result.Set(LevelName(value), value); else result.Set(value, LevelName(value));
        if (reverse) { result.Set("WARN", 30L); result.Set("FATAL", 50L); }
        return result;
    }
    private static object? GetLevelName(object? value)
    {
        // 用实际 Python 映射保留数值跨类型相等、用户哈希与相等协议，不另造 CLR 判等分支。
        if (NumberNames.TryGet(value, out var name)) return name;
        if (NameNumbers.TryGet(value, out var number)) return number;
        return "Level " + PyRepr.Str(value);
    }
}
