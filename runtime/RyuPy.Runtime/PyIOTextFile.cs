// 文本磁盘流按完整码点解码，保留真实字节游标，不将 UTF-16 下标冒充文件位置。
// 首批只支持无状态 UTF-8/ASCII/Latin-1；read 不跨读取缓冲器预取，tell cookie 可安全恢复。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>Python I/O 协议的显式运行时实现。</summary>
public sealed class PyIOTextFile : PyIOStream
{
    private readonly FileStream _stream;
    private readonly Encoding _codec;
    private readonly string _encoding, _name, _mode;
    private readonly string? _newline;
    private readonly bool _append, _lineBuffering;
    private readonly HashSet<long> _cookies = [0];
    /// <summary>按给定配置构造运行时对象。</summary>
    public PyIOTextFile(FileStream stream, Encoding codec, string encoding, string? newline,
        bool readable, bool writable, string name, string mode, bool append, bool lineBuffering = false) : base(readable, writable)
    {
        _stream = stream; _codec = codec; _encoding = encoding; _newline = newline;
        _name = name; _mode = mode; _append = append; _lineBuffering = lineBuffering;
    }
    /// <inheritdoc/>
    public override string TypeName => "TextIOWrapper";
    /// <inheritdoc/>
    protected override object? ExtraAttribute(string name) => name switch
    {
        "name" => _name, "mode" => _mode, "encoding" => _encoding,
        "errors" => "strict", "line_buffering" => _lineBuffering,
        _ => base.ExtraAttribute(name),
    };
    private string? RawCharacter()
    {
        int first = _stream.ReadByte(); if (first < 0) return null;
        Span<byte> bytes = stackalloc byte[4]; bytes[0] = (byte)first;
        int count = 1;
        if (_codec.CodePage == 65001)
        {
            int width = first < 0x80 ? 1 : first is >= 0xc2 and <= 0xdf ? 2
                : first is >= 0xe0 and <= 0xef ? 3 : first is >= 0xf0 and <= 0xf4 ? 4 : 1;
            for (; count < width; count++)
            {
                int next = _stream.ReadByte(); if (next < 0) break;
                bytes[count] = (byte)next;
            }
        }
        return _codec.GetString(bytes[..count]);
    }
    private string? ReadCharacter()
    {
        string? character = RawCharacter();
        if (_newline is null && character == "\r")
        {
            long afterCr = _stream.Position;
            if (RawCharacter() != "\n") _stream.Position = afterCr;
            return "\n";
        }
        return character;
    }
    /// <inheritdoc/>
    protected override object ReadCore(long size, bool line)
    {
        var result = new StringBuilder(); long count = 0;
        while (size < 0 || count < size)
        {
            string? character = ReadCharacter(); if (character is null) break;
            result.Append(character); count++;
            if (!line) continue;
            if (_newline == "" && character == "\r")
            {
                if (size < 0 || count < size)
                {
                    long afterCr = _stream.Position;
                    if (RawCharacter() == "\n") result.Append('\n');
                    else _stream.Position = afterCr;
                }
                break;
            }
            if ((_newline is null or "" or "\n") && character == "\n") break;
            if (_newline == "\r" && character == "\r") break;
            if (_newline == "\r\n" && character == "\n" && result.Length > 1 && result[^2] == '\r') break;
        }
        return result.ToString();
    }
    /// <inheritdoc/>
    protected override void ValidateWriteValue(object? value) => _ = PyIO.Text(value);
    /// <inheritdoc/>
    protected override long WriteCore(object? value)
    {
        string text = PyIO.Text(value);
        string translated = _newline switch
        {
            null => text.Replace("\n", Environment.NewLine),
            "\r" or "\r\n" => text.Replace("\n", _newline),
            _ => text,
        };
        byte[] bytes = _codec.GetBytes(translated);
        if (_append) _stream.Seek(0, SeekOrigin.End);
        _stream.Write(bytes);
        if (_lineBuffering && (text.Contains('\n') || text.Contains('\r'))) _stream.Flush();
        return PyText.Length(text);
    }
    /// <inheritdoc/>
    protected override long SeekCore(long offset, long whence)
    {
        if (whence is < 0 or > 2) throw new PyValueError("invalid whence");
        if (whence != 0 && offset != 0) throw new PyOSError("can't do nonzero cur-relative seeks");
        if (offset < 0) throw new PyValueError("negative seek position");
        if (whence == 0 && !_cookies.Contains(offset))
            throw new PyNotImplementedError("text seek only accepts zero or a cookie returned by tell()");
        _stream.Seek(offset, (SeekOrigin)whence); return TellCore();
    }
    /// <inheritdoc/>
    protected override long TellCore()
    { long position = _stream.Position; _cookies.Add(position); return position; }
    /// <inheritdoc/>
    protected override long TruncateCore(long? size)
    {
        long length = size ?? _stream.Position;
        if (length < 0) throw new PyValueError("negative size value");
        long position = _stream.Position;
        _stream.SetLength(length); _stream.Position = position; return length;
    }
    /// <inheritdoc/>
    protected override void FlushCore() => _stream.Flush();
    /// <inheritdoc/>
    protected override void CloseCore() => _stream.Dispose();
}
