// I/O 构造入口与边界校验：只开放已验收的编码、模式和路径，不把 CLR 异常带到 Python。
using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>Python I/O 协议的显式运行时实现。</summary>
public static class PyIO
{
    private static readonly PyFunction OpenFunction = PyNativeFunctions.Create("open",
        ["file", "mode", "buffering", "encoding", "errors", "newline", "closefd", "opener"],
        [null, "r", -1L, null, null, null, true, null], 1, OpenBound);
    private static readonly PyFunction StringFunction = PyNativeFunctions.Create("StringIO",
        ["initial_value", "newline"], ["", "\n"], 0, a =>
            new PyIOTextMemory(IsNone(a[0]) ? "" : Text(a[0]), Newline(a[1])));
    private static readonly PyFunction BytesFunction = PyNativeFunctions.Create("BytesIO",
        ["initial_bytes"], [null], 0, a =>
        {
            var memory = new MemoryStream();
            if (!IsNone(a[0])) { byte[] bytes = Bytes(a[0]); memory.Write(bytes); memory.Position = 0; }
            return new PyIOBinary(memory);
        });

    /// <summary>绑定 open 参数并创建文本或二进制文件流。</summary>
    public static object? Open(object?[] args, string[] names, object?[] values) =>
        Guard(() => OpenFunction.Invoke(args, names, values));
    /// <summary>绑定 StringIO 构造参数并创建文本内存流。</summary>
    public static object? CreateStringIO(object?[] args, string[] names, object?[] values) =>
        Guard(() => StringFunction.Invoke(args, names, values));
    /// <summary>绑定 BytesIO 构造参数并创建字节内存流。</summary>
    public static object? CreateBytesIO(object?[] args, string[] names, object?[] values) =>
        Guard(() => BytesFunction.Invoke(args, names, values));

    private static object OpenBound(object?[] a)
    {
        string path = PyOsPath.Text(a[0]);
        string mode = Text(a[1]);
        bool binary = mode.Contains('b'), plus = mode.Contains('+');
        int operations = 0;
        foreach (char operation in "rwax") if (mode.Contains(operation)) operations++;
        if (operations != 1 || mode.Length == 0 || (binary && mode.Contains('t')))
            throw new PyValueError($"invalid mode: '{mode}'");
        var seen = new System.Collections.Generic.HashSet<char>();
        foreach (char character in mode)
            if (!"rwaxbt+".Contains(character) || !seen.Add(character))
                throw new PyValueError($"invalid mode: '{mode}'");
        long buffering = Index(a[2]);
        if (buffering is not (-1 or 0 or 1))
            throw new PyNotImplementedError("custom file buffering is not supported yet");
        if (binary && buffering == 1) throw new PyNotImplementedError("line buffering in binary mode is not supported");
        if (!binary && buffering == 0) throw new PyValueError("can't have unbuffered text I/O");
        if (IsNone(a[6]) || !PyOps.Truthy(a[6]))
            throw new PyValueError("Cannot use closefd=False with file name");
        if (!IsNone(a[7])) throw new PyNotImplementedError("custom opener is not supported yet");
        if (binary && (!IsNone(a[3]) || !IsNone(a[4]) || !IsNone(a[5])))
            throw new PyValueError("binary mode doesn't take an encoding, errors or newline argument");
        if (!IsNone(a[4]) && Text(a[4]) != "strict")
            throw new PyNotImplementedError("only strict encoding errors are supported");
        string encoding = IsNone(a[3]) ? "utf-8" : Text(a[3]);
        Encoding codec = Codec(encoding); string? newline = Newline(a[5]);
        bool read = mode.Contains('r') || plus, write = !mode.Contains('r') || plus;
        bool append = mode.Contains('a');
        FileMode fileMode = mode.Contains('w') ? FileMode.Create : mode.Contains('x') ? FileMode.CreateNew
            : append ? FileMode.OpenOrCreate : FileMode.Open;
        if (path.Contains('\0')) throw new PyValueError("embedded null byte");
        if (Directory.Exists(path)) throw new PyIsADirectoryError(path);
        // CLR 将父路径为普通文件和真正缺失目录都报告为 DirectoryNotFound，先保留 Python 子类。
        string? parent = Path.GetDirectoryName(Path.GetFullPath(path));
        while (!string.IsNullOrEmpty(parent))
        {
            if (File.Exists(parent)) throw new PyNotADirectoryError(path);
            string? next = Path.GetDirectoryName(parent);
            if (next == parent) break;
            parent = next;
        }
        if (fileMode == FileMode.CreateNew && File.Exists(path)) throw new PyFileExistsError(path);
        var stream = new FileStream(path, fileMode, read && write ? FileAccess.ReadWrite
            : read ? FileAccess.Read : FileAccess.Write, FileShare.ReadWrite, buffering == 0 ? 1 : 4096);
        try
        {
            if (append) stream.Seek(0, SeekOrigin.End);
            return binary ? new PyIOBinary(stream, read, write, path, mode, append, buffering == 0)
                : new PyIOTextFile(stream, codec, encoding, newline, read, write, path, mode, append, buffering == 1);
        }
        catch
        {
            // 所有权仅在包装对象成功返回后转移；初始化定位失败也必须关闭句柄。
            stream.Dispose(); throw;
        }
    }

    internal static bool IsNone(object? value) => value is null or PyNone;
    internal static string Text(object? value) => value as string
        ?? throw new PyTypeError($"expected str, not {PyRepr.TypeName(value)}");
    internal static byte[] Bytes(object? value) => value switch
    {
        PyBytes bytes => bytes.Bytes,
        PyByteArray bytes => [.. bytes.Bytes],
        _ => throw new PyTypeError($"a bytes-like object is required, not '{PyRepr.TypeName(value)}'"),
    };
    internal static long Index(object? value)
    {
        if (value is not (bool or int or long or BigInteger))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("an integer is required");
        }
        return value switch
        {
            bool b => b ? 1 : 0, int i => i, long l => l,
            BigInteger b when b >= long.MinValue && b <= long.MaxValue => (long)b,
            BigInteger => throw new PyOverflowError("Python int too large to convert to C ssize_t"),
            _ => throw new PyTypeError("__index__ returned non-int"),
        };
    }
    internal static string? Newline(object? value)
    {
        if (IsNone(value)) return null;
        string text = Text(value);
        if (text is not ("" or "\n" or "\r" or "\r\n")) throw new PyValueError("illegal newline value");
        return text;
    }
    internal static Encoding Codec(string name)
    {
        string normalized = name.ToLowerInvariant().Replace("-", "").Replace("_", "");
        return normalized switch
        {
            "utf8" or "u8" => new UTF8Encoding(false, true),
            "ascii" or "usascii" => Encoding.GetEncoding(20127, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
            "latin1" or "iso88591" => Encoding.GetEncoding(28591, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
            _ => throw new PyLookupError($"unsupported encoding: {name}"),
        };
    }
    internal static object? Guard(Func<object?> action)
    {
        try { return action(); }
        catch (PyBaseException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or ObjectDisposedException or OverflowException)
        {
            // 宿主异常刚转换时尚无 Python 帧；在离开真实调用现场前补报告帧及活动异常上下文。
            // 此 catch 已排除 PyBaseException；映射分支确实创建新对象，不把已有错误授为 Fresh。
            var mapped = FileSystemError(error);
            throw PyTraceProduct.Enabled ? PyTraceProduct.NativeFresh(mapped)
                : (PyBaseException)PyErrors.PrepareRaise(mapped);
        }
    }
    /// <summary>将宿主文件系统异常映射为 Python 异常，供路径模块共享。</summary>
    public static PyBaseException FileSystemError(Exception error) => error switch
    {
        PyBaseException python => python,
        EncoderFallbackException => new PyUnicodeEncodeError("encoding failed in strict mode"),
        DecoderFallbackException => new PyUnicodeDecodeError("decoding failed in strict mode"),
        FileNotFoundException or DirectoryNotFoundException => new PyFileNotFoundError(error.Message),
        UnauthorizedAccessException => new PyPermissionError(error.Message),
        ObjectDisposedException => new PyValueError("I/O operation on closed file."),
        OverflowException => new PyOverflowError("I/O size or offset is too large"),
        ArgumentException => new PyValueError(error.Message),
        _ => new PyOSError(error.Message),
    };
}
