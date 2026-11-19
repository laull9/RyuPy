// gzip.open 的真实磁盘流：二进制与 UTF-8/ASCII/Latin-1 文本共享现有 Python I/O 上下文协议。
// 读取按需解压且累计限额，写入立即压缩；异常退出与 close 均通过 finally 释放文件句柄。
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>gzip 的可关闭、可逐行迭代文件对象。</summary>
internal sealed class PyGzipFile : PyIOStream
{
    private readonly Stream stream;
    private readonly StreamReader? reader;
    private readonly Encoding? codec;
    private readonly string path, mode, encoding;
    private readonly string? newline;
    private long position;

    private PyGzipFile(Stream stream, string path, string mode, Encoding? codec, string encoding, string? newline)
        : base(mode[0] == 'r', mode[0] != 'r')
    {
        this.stream = stream; this.path = path; this.mode = mode; this.codec = codec;
        this.encoding = encoding; this.newline = newline;
        if (codec is not null && CanRead) reader = new StreamReader(stream, codec, false, 1024, true);
    }

    /// <summary>先验证全部选项再打开路径，避免失败构造留下文件或句柄。</summary>
    public static object? Open(object?[] args) => PyIO.Guard(() =>
    {
        string path = PyOsPath.Text(args[0]), mode = PyIO.Text(args[1]);
        if (mode is not ("r" or "rb" or "rt" or "w" or "wb" or "wt" or "a" or "ab" or "at" or "x" or "xb" or "xt"))
            throw new PyValueError("Invalid mode: " + mode);
        bool text = mode.EndsWith('t'), read = mode[0] == 'r';
        int level = PyCompression.Level(args[2]);
        if (!text && (!PyIO.IsNone(args[3]) || !PyIO.IsNone(args[4]) || !PyIO.IsNone(args[5])))
            throw new PyValueError("encoding, errors and newline are only supported in text mode");
        if (text && !PyIO.IsNone(args[4]) && PyIO.Text(args[4]) != "strict")
            throw new PyNotImplementedError("gzip text streams currently support errors='strict' only");
        string encoding = PyIO.IsNone(args[3]) ? "utf-8" : PyIO.Text(args[3]);
        Encoding? codec = text ? PyIO.Codec(encoding) : null;
        string? newline = text ? PyIO.Newline(args[5]) : null;
        if (path.Contains('\0')) throw new PyValueError("embedded null byte");
        if (Directory.Exists(path)) throw new PyIsADirectoryError(path);
        // CLR 合并了缺目录与父路径为普通文件的错误，打开前保留 Python 的精确子类。
        string? parent = Path.GetDirectoryName(Path.GetFullPath(path));
        while (!string.IsNullOrEmpty(parent))
        {
            if (File.Exists(parent)) throw new PyNotADirectoryError(path);
            string? next = Path.GetDirectoryName(parent);
            if (next == parent) break;
            parent = next;
        }
        if (mode[0] == 'x' && File.Exists(path)) throw new PyFileExistsError(path);
        var file = new FileStream(path, read ? FileMode.Open : mode[0] == 'a' ? FileMode.Append
            : mode[0] == 'x' ? FileMode.CreateNew : FileMode.Create, read ? FileAccess.Read : FileAccess.Write);
        Stream? wrapper = null;
        try
        {
            wrapper = read ? new PyGzipDecoder(file)
                : new GZipStream(new PyGzipHeader(file, checked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds())),
                    new ZLibCompressionOptions { CompressionLevel = level });
            if (!read) wrapper.Write(ReadOnlySpan<byte>.Empty);
            return new PyGzipFile(wrapper, path, mode, codec, encoding, newline);
        }
        catch { try { wrapper?.Dispose(); } finally { file.Dispose(); } throw; }
    });

    /// <inheritdoc/>
    public override string TypeName => codec is null ? "GzipFile" : "TextIOWrapper";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name == "seekable"
        ? PyNativeFunctions.Create(name, [], [], 0, _ => { CheckOpen(); return false; }) : base.GetAttr(name);
    /// <inheritdoc/>
    protected override object? ExtraAttribute(string name) => name switch
    {
        "name" => path,
        "mode" => codec is null ? CanRead ? 1L : 2L : mode,
        "encoding" when codec is not null => encoding,
        "errors" when codec is not null => "strict",
        _ => base.ExtraAttribute(name),
    };
    /// <inheritdoc/>
    protected override object ReadCore(long size, bool line)
    {
        if (codec is not null) return ReadText(size, line);
        using var output = new MemoryStream();
        if (line)
        {
            while (size < 0 || output.Length < size)
            {
                int value = stream.ReadByte();
                if (value < 0) break;
                output.WriteByte((byte)value);
                if (value == 10) break;
            }
        }
        else
        {
            var buffer = new byte[8192];
            while (size < 0 || output.Length < size)
            {
                int count = stream.Read(buffer, 0, size < 0 ? buffer.Length : (int)Math.Min(buffer.Length, size - output.Length));
                if (count == 0) break;
                output.Write(buffer, 0, count);
            }
        }
        position += output.Length;
        return new PyBytes(output.ToArray());
    }

    private string ReadText(long size, bool line)
    {
        var result = new StringBuilder(); long count = 0;
        while (size < 0 || count < size)
        {
            int value = reader!.Read();
            if (value < 0) break;
            char character = (char)value;
            if (character == '\r' && newline is null)
            {
                if (reader.Peek() == '\n') reader.Read();
                character = '\n';
            }
            result.Append(character); count++;
            if (char.IsHighSurrogate(character) && reader.Peek() is >= 0xdc00 and <= 0xdfff)
                result.Append((char)reader.Read());
            if (!line) continue;
            if (character == '\r' && newline == "")
            {
                if ((size < 0 || count < size) && reader.Peek() == '\n') { result.Append((char)reader.Read()); count++; }
                break;
            }
            if (character == '\n' && newline is null or "" or "\n"
                || character == '\r' && newline == "\r"
                || character == '\n' && newline == "\r\n" && result.Length > 1 && result[^2] == '\r') break;
        }
        position += count;
        return result.ToString();
    }
    /// <inheritdoc/>
    protected override void ValidateWriteValue(object? value)
    { if (codec is null) _ = PyCompression.Bytes(value); else _ = PyIO.Text(value); }
    /// <inheritdoc/>
    protected override long WriteCore(object? value)
    {
        if (codec is null) { byte[] bytes = PyCompression.Bytes(value); stream.Write(bytes); position += bytes.Length; return bytes.Length; }
        string text = PyIO.Text(value);
        string translated = newline switch
        {
            null => text.Replace("\n", Environment.NewLine),
            "\r" or "\r\n" => text.Replace("\n", newline),
            _ => text,
        };
        stream.Write(codec.GetBytes(translated)); long count = PyText.Length(text); position += count; return count;
    }
    /// <inheritdoc/>
    protected override long SeekCore(long offset, long whence) => throw new PyUnsupportedOperation("gzip seek is not supported");
    /// <inheritdoc/>
    protected override long TellCore() => codec is null ? position
        : throw new PyUnsupportedOperation("gzip text position cookies are not supported");
    /// <inheritdoc/>
    protected override long TruncateCore(long? size) => throw new PyUnsupportedOperation("truncate");
    /// <inheritdoc/>
    protected override void FlushCore() { if (CanWrite) stream.Flush(); }
    /// <inheritdoc/>
    protected override void CloseCore() { try { reader?.Dispose(); } finally { stream.Dispose(); } }
}
