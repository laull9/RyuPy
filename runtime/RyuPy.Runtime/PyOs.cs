// os 的常用目录与文件操作；只适配宿主文件系统，统一转换底层 IO 错误为 Python 异常。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>原生 os 模块的文件系统 API；环境变量由系统模块层合并。</summary>
public static class PyOs
{
    /// <summary>供原生模块注册表使用的属性集合。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["name"] = OperatingSystem.IsWindows() ? "nt" : "posix",
        ["sep"] = System.IO.Path.DirectorySeparatorChar.ToString(),
        ["linesep"] = OperatingSystem.IsWindows() ? "\r\n" : "\n",
        ["pathsep"] = System.IO.Path.PathSeparator.ToString(),
        ["curdir"] = ".", ["pardir"] = "..",
        ["getcwd"] = Function("getcwd", [], [], 0, _ => Guard(() => Directory.GetCurrentDirectory())),
        ["chdir"] = Function("chdir", ["path"], [null], 1, a => Guard(() => { Directory.SetCurrentDirectory(NonEmpty(PyOsPath.Text(a[0]))); return PyNone.Instance; })),
        ["listdir"] = Function("listdir", ["path"], ["."], 0, a => ListDirectory(a[0])),
        ["fspath"] = Function("fspath", ["path"], [null], 1, a => PyOsPath.Text(a[0])),
        ["mkdir"] = Function("mkdir", ["path", "mode"], [null, 511L], 1, a => MakeDirectory(a[0], a[1], false, false)),
        ["makedirs"] = Function("makedirs", ["name", "mode", "exist_ok"], [null, 511L, false], 1,
            a => MakeDirectory(a[0], a[1], true, PyOps.Truthy(a[2]))),
        ["remove"] = Function("remove", ["path"], [null], 1, a => Remove(a[0], false)),
        ["unlink"] = Function("unlink", ["path"], [null], 1, a => Remove(a[0], false)),
        ["rmdir"] = Function("rmdir", ["path"], [null], 1, a => RemoveDirectory(a[0])),
        ["rename"] = Function("rename", ["src", "dst"], [null, null], 2, a => Move(a[0], a[1], false)),
        ["replace"] = Function("replace", ["src", "dst"], [null, null], 2, a => Move(a[0], a[1], true)),
    };

    private static PyFunction Function(string name, string[] names, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, defaults, required, body);

    private static string NonEmpty(string path) => path.Length == 0
        ? throw new PyFileNotFoundError("[Errno 2] No such file or directory: ''") : path;

    /// <summary>文件系统错误边界；Python 异常原样传播，非 IO 程序错误不掩盖。</summary>
    internal static T Guard<T>(Func<T> operation)
    {
        try { return operation(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw PyIO.FileSystemError(error); }
    }

    /// <summary>目录名称列表，保持系统枚举次序而不偷偷排序。</summary>
    public static object ListDirectory(object? path)
    {
        string text = NonEmpty(PyOsPath.Text(path is null or PyNone ? "." : path));
        return Guard<object>(() =>
        {
            if (File.Exists(text)) throw new PyNotADirectoryError("[Errno 20] Not a directory");
            return new PyList(Directory.GetFileSystemEntries(text)
                .Select(p => (object?)System.IO.Path.GetFileName(p)));
        });
    }

    /// <summary>创建目录；非递归 mkdir 必须先有父目录。</summary>
    public static object MakeDirectory(object? path, object? mode, bool parents, bool existOk)
    {
        string text = NonEmpty(PyOsPath.Text(path));
        long permission = mode switch { long number => number, bool flag => flag ? 1 : 0,
            _ => throw new PyTypeError("'mode' must be an integer") };
        if (permission != 511) throw new PyNotImplementedError("custom directory modes are not supported yet");
        return Guard<object>(() =>
        {
            if (Directory.Exists(text))
            {
                if (existOk) return PyNone.Instance;
                throw new PyFileExistsError($"[Errno 17] File exists: {PyRepr.Repr(text)}");
            }
            if (File.Exists(text)) throw new PyFileExistsError($"[Errno 17] File exists: {PyRepr.Repr(text)}");
            string parent = System.IO.Path.GetDirectoryName(text.TrimEnd(System.IO.Path.DirectorySeparatorChar)) ?? "";
            if (parent.Length > 0 && File.Exists(parent))
                throw new PyNotADirectoryError("[Errno 20] Not a directory");
            if (!parents && parent.Length > 0 && !Directory.Exists(parent))
                throw new PyFileNotFoundError($"[Errno 2] No such file or directory: {PyRepr.Repr(text)}");
            Directory.CreateDirectory(text);
            return PyNone.Instance;
        });
    }

    /// <summary>删除文件；File.Delete 对缺失路径默认无操作，需要显式补 Python 错误。</summary>
    public static object Remove(object? path, bool missingOk)
    {
        string text = NonEmpty(PyOsPath.Text(path));
        return Guard<object>(() =>
        {
            bool link = false;
            try { link = (File.GetAttributes(text) & FileAttributes.ReparsePoint) != 0; }
            catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { }
            if (!link && Directory.Exists(text)) throw new PyIsADirectoryError($"[Errno 21] Is a directory: {PyRepr.Repr(text)}");
            if (!link && !File.Exists(text))
            {
                if (missingOk) return PyNone.Instance;
                throw new PyFileNotFoundError($"[Errno 2] No such file or directory: {PyRepr.Repr(text)}");
            }
            File.Delete(text);
            return PyNone.Instance;
        });
    }

    /// <summary>仅删除空目录，不递归删除用户数据。</summary>
    public static object RemoveDirectory(object? path) => Guard<object>(() =>
    {
        string text = NonEmpty(PyOsPath.Text(path));
        if (File.Exists(text)) throw new PyNotADirectoryError("[Errno 20] Not a directory");
        Directory.Delete(text, false);
        return PyNone.Instance;
    });

    /// <summary>移动文件或目录；POSIX rename 覆盖文件，Windows rename 不覆盖。</summary>
    public static object Move(object? source, object? destination, bool replace)
    {
        string src = NonEmpty(PyOsPath.Text(source)), dst = NonEmpty(PyOsPath.Text(destination));
        return Guard<object>(() =>
        {
            if (System.IO.Path.GetFullPath(src) == System.IO.Path.GetFullPath(dst)
                && (File.Exists(src) || Directory.Exists(src))) return PyNone.Instance;
            if (Directory.Exists(src))
            {
                if (File.Exists(dst)) throw new PyNotADirectoryError("[Errno 20] Not a directory");
                if (Directory.Exists(dst))
                    throw new PyNotImplementedError("renaming onto an existing directory is not supported yet");
                Directory.Move(src, dst);
            }
            else
            {
                if (Directory.Exists(dst)) throw new PyIsADirectoryError("[Errno 21] Is a directory");
                File.Move(src, dst, replace || !OperatingSystem.IsWindows());
            }
            return PyNone.Instance;
        });
    }
}
