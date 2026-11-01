// pathlib 首批宿主 Path 对象：词法分量、路径组合及文件读写，复用 os 与 io 的错误/编码边界。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>pathlib 的原生模块属性。</summary>
public static class PyPaths
{
    // 模块可重复注册，但类型身份不能随注册改变；Lazy默认同步保证并行首次访问只建一次。
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(BuildAttributes);

    /// <summary>首批 Path 工厂；纯 Windows/POSIX 异平台路径类尚未开放。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;

    private static Dictionary<string, object?> BuildAttributes()
    {
        PyFunction constructor = PyNativeFunctions.CreateVariadic("Path", args => new PyPath(
            args.Length == 0 ? "." : PyOsPath.Join(args)));
        var path = new PyNativeType("Path", "pathlib", constructor.Invoke, value => value is PyPath);
        var posix = new PyNativeType("PosixPath", "pathlib", constructor.Invoke,
            value => value is PyPath && !OperatingSystem.IsWindows(), "Path");
        var windows = new PyNativeType("WindowsPath", "pathlib", null, _ => false, "Path");
        foreach (PyNativeType type in new[] { path, posix, windows })
            type.SetAttr("cwd", PyNativeFunctions.Create("cwd", [], [], 0,
                _ => type.Invoke([Directory.GetCurrentDirectory()], [], [])));
        return new(StringComparer.Ordinal) { ["Path"] = path, ["PosixPath"] = posix, ["WindowsPath"] = windows };
    }

}

/// <summary>宿主平台路径。构造不触碰文件系统，文件操作明确复用受控 IO 入口。</summary>
public sealed class PyPath : PyNativeObject
{
    /// <summary>按 pathlib 规则压缩冗余分隔符与点分量，但不消除父目录分量。</summary>
    public string Value { get; }

    /// <summary>构造路径，不要求它存在。</summary>
    public PyPath(string value) => Value = Clean(value);

    /// <inheritdoc />
    public override string TypeName => OperatingSystem.IsWindows() ? "WindowsPath" : "PosixPath";

    /// <inheritdoc />
    public override string PythonStr() => Value;

    /// <inheritdoc />
    public override string PythonRepr() => TypeName + "(" + PyRepr.Repr(OperatingSystem.IsWindows() ? Value.Replace('\\', '/') : Value) + ")";

    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "name" => Name,
        "suffix" => Suffix,
        "stem" => Name[..(Name.Length - Suffix.Length)],
        "parent" => Parent,
        "parts" => Parts(),
        "anchor" => Anchor,
        "__fspath__" => Method(name, [], [], 0, _ => Value),
        "__str__" => Method(name, [], [], 0, _ => Value),
        "__eq__" => Method(name, ["other"], [null], 1, a => a[0] is PyPath other
            ? string.Equals(Value, other.Value, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
            : PyNotImplemented.Instance),
        "__hash__" => Method(name, [], [], 0, _ => (long)(OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value) : StringComparer.Ordinal.GetHashCode(Value))),
        "__truediv__" => Method(name, ["other"], [null], 1, a => new PyPath(PyOsPath.Join([Value, a[0]]))),
        "__rtruediv__" => Method(name, ["other"], [null], 1, a => new PyPath(PyOsPath.Join([a[0], Value]))),
        "joinpath" => PyNativeFunctions.CreateVariadic(name, args => new PyPath(PyOsPath.Join([Value, .. args]))),
        "is_absolute" => Method(name, [], [], 0, _ => System.IO.Path.IsPathRooted(Value)),
        "absolute" => Method(name, [], [], 0, _ => new PyPath(System.IO.Path.IsPathRooted(Value) ? Value : PyOsPath.Join([Directory.GetCurrentDirectory(), Value]))),
        "as_posix" => Method(name, [], [], 0, _ => OperatingSystem.IsWindows() ? Value.Replace('\\', '/') : Value),
        "exists" => Method(name, [], [], 0, _ => PyPathQueries.Probe(Value) is not null),
        "is_file" => Method(name, [], [], 0, _ => PyPathQueries.Probe(Value) is FileAttributes attributes && (attributes & FileAttributes.Directory) == 0),
        "is_dir" => Method(name, [], [], 0, _ => PyPathQueries.Probe(Value) is FileAttributes attributes && (attributes & FileAttributes.Directory) != 0),
        "mkdir" => Method(name, ["mode", "parents", "exist_ok"], [511L, false, false], 0,
            a => PyOs.MakeDirectory(Value, a[0], PyOps.Truthy(a[1]), PyOps.Truthy(a[2]))),
        "unlink" => Method(name, ["missing_ok"], [false], 0, a => PyOs.Remove(Value, PyOps.Truthy(a[0]))),
        "rmdir" => Method(name, [], [], 0, _ => PyOs.RemoveDirectory(Value)),
        "rename" => Method(name, ["target"], [null], 1, a => Move(a[0], false)),
        "replace" => Method(name, ["target"], [null], 1, a => Move(a[0], true)),
        "iterdir" => Method(name, [], [], 0, _ => IterateDirectory()),
        "glob" => Method(name, ["pattern", "case_sensitive"], [null, PyNone.Instance], 1,
            a => Glob(a[0], a[1]), ["pos", "kwonly"]),
        "read_text" => Method(name, ["encoding", "errors"], [PyNone.Instance, PyNone.Instance], 0,
            a => Read("r", a[0], a[1])),
        "read_bytes" => Method(name, [], [], 0, _ => Read("rb", PyNone.Instance, PyNone.Instance)),
        "write_text" => Method(name, ["data", "encoding", "errors", "newline"],
            [null, PyNone.Instance, PyNone.Instance, PyNone.Instance], 1,
            a => Write(a[0], "w", a[1], a[2], a[3])),
        "write_bytes" => Method(name, ["data"], [null], 1,
            a => Write(a[0], "wb", PyNone.Instance, PyNone.Instance, PyNone.Instance)),
        "open" => Method(name, ["mode", "buffering", "encoding", "errors", "newline"],
            ["r", -1L, PyNone.Instance, PyNone.Instance, PyNone.Instance], 0,
            a => PyIO.Open([Value, a[0]], ["buffering", "encoding", "errors", "newline"], [a[1], a[2], a[3], a[4]])),
        _ => throw Missing(name),
    };

    private PyFunction Method(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body, string[]? kinds = null) =>
        PyNativeFunctions.Create(name, names, defaults, required, body, kinds);

    private string Anchor => !OperatingSystem.IsWindows() && Value.StartsWith("//", StringComparison.Ordinal)
        ? "//" : System.IO.Path.GetPathRoot(Value) ?? "";
    private string Name => Value == "." || Value == Anchor ? "" : PyOsPath.BaseName(Value);
    private string Suffix
    {
        get
        {
            int dot = Name.LastIndexOf('.');
            return dot > 0 && dot < Name.Length - 1 ? Name[dot..] : "";
        }
    }
    private PyPath Parent => Value == Anchor || Value == "." ? new PyPath(Value)
        : new PyPath(PyOsPath.DirName(Value));

    private PyTuple Parts()
    {
        var parts = new List<object?>();
        if (Anchor.Length > 0) parts.Add(Anchor);
        foreach (string part in Value[Anchor.Length..].Split(System.IO.Path.DirectorySeparatorChar))
            if (part.Length > 0 && part != ".") parts.Add(part);
        return new PyTuple(parts.ToArray());
    }

    private PyPath Move(object? target, bool replace)
    {
        string destination = PyOsPath.Text(target);
        PyOs.Move(Value, destination, replace);
        return new PyPath(destination);
    }

    // iterdir/glob 的具体枚举在 iterator 第一次推进才发生；既不能提前抛 IO 异常，
    // 也不能把目录结果当作列表返回，误导 next() 与惰性消费的调用者。
    private object IterateDirectory() => new PyPathIterator(Value, null);

    private object Glob(object? pattern, object? caseSensitive) => new PyPathIterator(Value,
        pattern as string, () =>
        {
            if (pattern is not string text) throw new PyTypeError("pattern must be a string");
            if (text.Length == 0) throw new PyValueError("Unacceptable pattern: ''");
            if (caseSensitive is not (null or PyNone)) throw new PyNotImplementedError("explicit glob case_sensitive is not supported yet");
            if (text.Contains('/') || text.Contains('\\') || text.Contains('[') || text.Contains(']') || text.Contains("**", StringComparison.Ordinal))
                throw new PyNotImplementedError("only single-directory glob with '*' and '?' is supported yet");
        });

    private object? Read(string mode, object? encoding, object? errors)
    {
        object? stream = PyIO.Open([Value, mode], ["encoding", "errors"], [encoding, errors]);
        try { return Call(stream, "read", []); }
        finally { Call(stream, "close", []); }
    }

    private object? Write(object? data, string mode, object? encoding, object? errors, object? newline)
    {
        // pathlib 在打开文件前检查数据类型，不能因非法数据先截断现存文件。
        if (mode == "w" && data is not string) throw new PyTypeError("data must be str");
        if (mode == "wb" && data is not (PyBytes or PyByteArray)) throw new PyTypeError("a bytes-like object is required");
        object? stream = PyIO.Open([Value, mode], ["encoding", "errors", "newline"], [encoding, errors, newline]);
        try { return Call(stream, "write", [data]); }
        finally { Call(stream, "close", []); }
    }

    private static object? Call(object? value, string name, object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);

    private static string Clean(string value)
    {
        PyOsPath.EnsureLexicalPlatform(OperatingSystem.IsWindows());
        if (OperatingSystem.IsWindows()) value = value.Replace('/', '\\');
        char separator = System.IO.Path.DirectorySeparatorChar;
        string anchor = System.IO.Path.GetPathRoot(value) ?? "";
        if (!OperatingSystem.IsWindows() && value.StartsWith("//", StringComparison.Ordinal)
            && !value.StartsWith("///", StringComparison.Ordinal)) anchor = "//";
        string rest = string.Join(separator, value[anchor.Length..].Split(separator)
            .Where(part => part.Length > 0 && part != "."));
        string result = anchor + rest;
        return result.Length == 0 ? "." : result;
    }
}
