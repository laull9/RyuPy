// os.path 的宿主平台词法路径操作。不得用 GetFullPath 代替 normpath，后者必须保留相对性。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>首批 os.path 操作及 PathLike 到文本路径的统一转换。</summary>
public static class PyOsPath
{
    /// <summary>供原生模块注册表使用的属性集合。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["join"] = PyNativeFunctions.CreateVariadic("join", args => Join(args)),
        ["basename"] = Unary("basename", p => BaseName(p)),
        ["dirname"] = Unary("dirname", p => DirName(p)),
        ["split"] = Unary("split", p => new PyTuple([DirName(p), BaseName(p)])),
        ["splitext"] = Unary("splitext", p => SplitExtension(p)),
        ["normpath"] = Unary("normpath", p => Normalize(p)),
        ["abspath"] = Unary("abspath", p => Absolute(p)),
        ["isabs"] = Unary("isabs", p => System.IO.Path.IsPathRooted(p)),
        ["exists"] = Unary("exists", p => File.Exists(p) || Directory.Exists(p)),
        ["isfile"] = Unary("isfile", p => File.Exists(p)),
        ["isdir"] = Unary("isdir", p => Directory.Exists(p)),
        ["sep"] = System.IO.Path.DirectorySeparatorChar.ToString(),
        ["altsep"] = OperatingSystem.IsWindows() ? "/" : PyNone.Instance,
        ["curdir"] = ".", ["pardir"] = "..", ["extsep"] = ".",
    };

    private static PyFunction Unary(string name, Func<string, object?> operation) =>
        PyNativeFunctions.Create(name, ["path"], [null], 1, args => operation(Text(args[0])));

    /// <summary>平台能力门禁独立于宿主检测，允许测试 Windows 明确拒绝而不伪造成功。</summary>
    /// <param name="windows">是否为 Windows 词法路径规则。</param>
    public static void EnsureLexicalPlatform(bool windows)
    {
        if (windows) throw new PyNotImplementedError("Windows lexical paths are not implemented yet");
    }

    /// <summary>转换 str / Path / __fspath__；字节路径尚未纳入首批 API。</summary>
    public static string Text(object? value)
    {
        if (value is string text) return text;
        if (value is PyPath path) return path.Value;
        if (value is PyBytes) throw new PyNotImplementedError("bytes paths are not supported yet");
        if (PyObject.TryMethod(value, "__fspath__", [], out object? result))
        {
            if (result is string converted) return converted;
            if (result is PyBytes) throw new PyNotImplementedError("bytes paths are not supported yet");
            throw new PyTypeError("expected __fspath__() to return str or bytes");
        }
        throw new PyTypeError($"expected str, bytes or os.PathLike object, not {PyRepr.TypeName(value)}");
    }

    /// <summary>按宿主路径分隔符合并；后续绝对片段替换之前片段，空尾片段保留分隔符。</summary>
    public static string Join(object?[] parts)
    {
        if (OperatingSystem.IsWindows()) return PyWindowsPath.Join(parts);
        if (parts.Length == 0) throw new PyTypeError("join() missing required argument: 'a'");
        string result = Text(parts[0]);
        for (int index = 1; index < parts.Length; index++)
        {
            string next = Text(parts[index]);
            if (next.StartsWith('/')) result = next;
            else if (result.Length == 0 || result.EndsWith('/')) result += next;
            else result += "/" + next;
        }
        return result;
    }

    internal static bool Separator(char c) => c == System.IO.Path.DirectorySeparatorChar
        || OperatingSystem.IsWindows() && c == '/';

    /// <summary>最后一个分隔符之后的文本；尾随分隔符返回空串。</summary>
    public static string BaseName(string path)
    {
        EnsureLexicalPlatform(OperatingSystem.IsWindows());
        int index = path.Length - 1;
        while (index >= 0 && !Separator(path[index])) index--;
        return path[(index + 1)..];
    }

    /// <summary>去掉末端名称与冗余末端分隔符，保留根。</summary>
    public static string DirName(string path)
    {
        if (OperatingSystem.IsWindows()) return PyWindowsPath.DirName(path);
        int index = path.Length - 1;
        while (index >= 0 && !Separator(path[index])) index--;
        string head = path[..(index + 1)];
        if (head.Any(c => !Separator(c))) head = head.TrimEnd(System.IO.Path.DirectorySeparatorChar, '/');
        return head;
    }

    /// <summary>仅最后路径分量参与扩展名，点文件不被视作扩展名。</summary>
    public static PyTuple SplitExtension(string path)
    {
        string name = BaseName(path);
        int dot = name.LastIndexOf('.');
        if (dot <= 0 || name[..dot].All(c => c == '.')) return new PyTuple([path, ""]);
        int position = path.Length - name.Length + dot;
        return new PyTuple([path[..position], path[position..]]);
    }

    /// <summary>词法规范化；POSIX 恰好两个前导斜杠按 Python 保留。</summary>
    public static string Normalize(string path)
    {
        EnsureLexicalPlatform(OperatingSystem.IsWindows());
        if (path.Length == 0) return ".";
        bool absolute = path.StartsWith('/');
        string prefix = path.StartsWith("//", StringComparison.Ordinal) && !path.StartsWith("///", StringComparison.Ordinal)
            ? "//" : absolute ? "/" : "";
        var parts = new List<string>();
        foreach (string part in path.Split('/'))
        {
            if (part.Length == 0 || part == ".") continue;
            if (part == ".." && parts.Count > 0 && parts[^1] != "..") parts.RemoveAt(parts.Count - 1);
            else if (part != ".." || !absolute) parts.Add(part);
        }
        string result = prefix + string.Join("/", parts);
        return result.Length == 0 ? "." : result;
    }

    /// <summary>绝对路径在词法层合并工作目录，不解析符号链接。</summary>
    public static string Absolute(string path) => Normalize(System.IO.Path.IsPathRooted(path)
        ? path : Join([Directory.GetCurrentDirectory(), path]));
}
