// 二进制流直接使用 Stream 的字节位置，BytesIO 与磁盘流共享协议而不提前物化文件。
using System;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>Python I/O 协议的显式运行时实现。</summary>
public sealed class PyIOBinary : PyIOStream
{
    private readonly Stream _stream;
    private readonly string? _name;
    private readonly string _mode;
    private readonly bool _append, _raw;
    /// <summary>按给定配置构造运行时对象。</summary>
    public PyIOBinary(Stream stream, bool readable = true, bool writable = true,
        string? name = null, string mode = "rb+", bool append = false, bool raw = false) : base(readable, writable)
    { _stream = stream; _name = name; _mode = mode; _append = append; _raw = raw; }
    /// <inheritdoc/>
    public override string TypeName => _name is null ? "BytesIO" : _raw ? "FileIO" : CanRead && CanWrite ? "BufferedRandom"
        : CanRead ? "BufferedReader" : "BufferedWriter";
    /// <inheritdoc/>
    protected override bool HasValue => _name is null;
    /// <inheritdoc/>
    protected override object ValueCore() => new PyBytes(((MemoryStream)_stream).ToArray());
    /// <inheritdoc/>
    protected override object? ExtraAttribute(string name) => name switch
    {
        "name" when _name is not null => _name,
        "mode" when _name is not null => _mode,
        _ => base.ExtraAttribute(name),
    };
    /// <inheritdoc/>
    protected override object ReadCore(long size, bool line)
    {
        if (size == 0) return new PyBytes([]);
        using var result = new MemoryStream();
        // 按块读取普通内容；逐行只读到换行，不吞后续字节。
        if (!line)
        {
            var buffer = new byte[8192]; long remaining = size < 0 ? long.MaxValue : size;
            while (remaining > 0)
            {
                int count = _stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (count == 0) break;
                result.Write(buffer, 0, count); remaining -= count;
            }
        }
        else
        {
            while (size < 0 || result.Length < size)
            {
                int value = _stream.ReadByte(); if (value < 0) break;
                result.WriteByte((byte)value); if (value == 10) break;
            }
        }
        return new PyBytes(result.ToArray());
    }
    /// <inheritdoc/>
    protected override bool HintBeforeOpen => _name is not null;
    /// <inheritdoc/>
    protected override void ValidateWriteValue(object? value)
    {
        if (_name is not null && value is not (PyBytes or PyByteArray)) _ = PyIO.Bytes(value);
    }
    /// <inheritdoc/>
    protected override long WriteCore(object? value)
    {
        byte[] bytes = PyIO.Bytes(value);
        if (_append) _stream.Seek(0, SeekOrigin.End);
        _stream.Write(bytes); return bytes.LongLength;
    }
    /// <inheritdoc/>
    protected override long SeekCore(long offset, long whence)
    {
        if (whence is < 0 or > 2) throw new PyValueError("invalid whence");
        if (whence == 0 && offset < 0) throw new PyValueError("negative seek value");
        // BytesIO 相对定位允许越过起点并钳制到零；磁盘流保留 OS 的无效定位错误。
        if (_name is null && whence != 0)
        {
            long basis = whence == 1 ? _stream.Position : _stream.Length;
            offset = Math.Max(0, checked(basis + offset)); whence = 0;
        }
        return _stream.Seek(offset, (SeekOrigin)whence);
    }
    /// <inheritdoc/>
    protected override long TellCore() => _stream.Position;
    /// <inheritdoc/>
    protected override long TruncateCore(long? size)
    {
        long length = size ?? _stream.Position;
        if (length < 0) throw new PyValueError("negative size value");
        // BytesIO 截断到更大位置不扩展内容，磁盘 truncate 扩展零字节。
        if (_name is not null || length < _stream.Length)
        {
            long position = _stream.Position;
            _stream.SetLength(length); _stream.Position = position;
        }
        return length;
    }
    /// <inheritdoc/>
    protected override void FlushCore() => _stream.Flush();
    /// <inheritdoc/>
    protected override void CloseCore() => _stream.Dispose();
}
