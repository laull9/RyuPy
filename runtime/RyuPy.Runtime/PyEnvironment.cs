using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>os.environ映射；显式写入同步进程环境，putenv不会反向更新此Python视图。</summary>
public sealed class PyEnvironment : PyNativeObject
{
    private static readonly Dictionary<string, string> Values = ReadInitial();
    /// <inheritdoc />
    public override string TypeName => "_Environ";

    private static Dictionary<string, string> ReadInitial()
    {
        var result = new Dictionary<string, string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (DictionaryEntry pair in Environment.GetEnvironmentVariables()) result[(string)pair.Key] = (string)pair.Value!;
        return result;
    }

    /// <summary>严格环境变量字符串，不隐式str转换。</summary>
    public static string Text(object? value) => value is string text ? text : throw new PyTypeError("str expected");
    /// <summary>读取环境映射，不存在返回指定默认值。</summary>
    public static object? Get(object? key, object? fallback) => Values.TryGetValue(Text(key), out string? value) ? value : fallback;
    /// <summary>仅写进程环境，供os.putenv；不反向修改已缓存的environ映射。</summary>
    public static void Set(object? key, object? value)
    {
        string name = Text(key), text = Text(value);
        if (name.Contains('=') || name.Contains('\0') || text.Contains('\0'))
            throw new PyValueError("illegal environment variable name or value");
        if (name.Length == 0) throw new PyOSError("[Errno 22] Invalid argument");
        PyIO.Guard(() => { Environment.SetEnvironmentVariable(name, text); return PyNone.Instance; });
    }

    private static object? Put(object? key, object? value)
    {
        Set(key, value); Values[Text(key)] = Text(value); return PyNone.Instance;
    }
    private static object? Delete(object? key)
    {
        string name = Text(key);
        if (!Values.Remove(name)) throw new PyKeyError(PyRepr.Repr(name));
        Unset(name); return PyNone.Instance;
    }

    /// <summary>仅删除进程环境；参数校验与宿主异常映射不泄漏CLR错误。</summary>
    public static void Unset(object? key)
    {
        string name = Text(key);
        if (name.Contains('\0')) throw new PyValueError("embedded null byte");
        if (name.Length == 0 || name.Contains('=')) throw new PyOSError("[Errno 22] Invalid argument");
        PyIO.Guard(() => { Environment.SetEnvironmentVariable(name, null); return PyNone.Instance; });
    }

    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__getitem__" => PyNativeFunctions.Create(name, ["key"], [null], 1, args =>
            Values.TryGetValue(Text(args[0]), out string? value) ? value : throw new PyKeyError(PyRepr.Repr(args[0]))),
        "__setitem__" => PyNativeFunctions.Create(name, ["key", "value"], [null, null], 2, args => Put(args[0], args[1])),
        "__delitem__" => PyNativeFunctions.Create(name, ["key"], [null], 1, args => Delete(args[0])),
        "__contains__" => PyNativeFunctions.Create(name, ["key"], [null], 1, args => Values.ContainsKey(Text(args[0]))),
        "__len__" => PyNativeFunctions.Create(name, [], [], 0, _ => (long)Values.Count),
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyIter.Create(new PyList(Values.Keys))),
        "get" => PyNativeFunctions.Create(name, ["key", "default"], [null, PyNone.Instance], 1, args => Get(args[0], args[1])),
        "keys" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(Values.Keys)),
        "values" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(Values.Values)),
        "items" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(Values.Select(pair => (object?)new PyTuple([pair.Key, pair.Value])))),
        "copy" => PyNativeFunctions.Create(name, [], [], 0, _ => Snapshot()),
        _ => throw Missing(name),
    };

    private static PyDict Snapshot()
    {
        var result = new PyDict();
        foreach (var pair in Values) result.Set(pair.Key, pair.Value);
        return result;
    }
}
