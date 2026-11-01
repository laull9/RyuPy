// StringIO 使用码点槽位而非 UTF-16 code unit 作为位置；孤立代理项仍是可保存的字符。
using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>Python I/O 协议的显式运行时实现。</summary>
public sealed class PyIOTextMemory : PyIOStream
{
    private readonly List<string> _characters = [];
    private readonly string? _newline;
    private long _position;
    /// <summary>按给定配置构造运行时对象。</summary>
    public PyIOTextMemory(string initial, string? newline)
    {
        _newline = newline;
        _characters.AddRange(Characters(Translate(initial)));
    }
    /// <inheritdoc/>
    public override string TypeName => "StringIO";
    /// <inheritdoc/>
    protected override bool HasValue => true;
    /// <inheritdoc/>
    protected override object ValueCore() => string.Concat(_characters);
    /// <inheritdoc/>
    protected override object? ExtraAttribute(string name) => name switch
    {
        "line_buffering" => false,
        _ => base.ExtraAttribute(name),
    };
    internal static IEnumerable<string> Characters(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length && char.IsSurrogatePair(text, i))
            { yield return text.Substring(i, 2); i++; }
            else yield return text[i].ToString();
        }
    }
    private string Translate(string text) => _newline switch
    {
        null => text.Replace("\r\n", "\n").Replace("\r", "\n"),
        "\r" or "\r\n" => text.Replace("\n", _newline),
        _ => text,
    };
    /// <inheritdoc/>
    protected override object ReadCore(long size, bool line)
    {
        var result = new StringBuilder(); long count = 0;
        while (_position < _characters.Count && (size < 0 || count < size))
        {
            string character = _characters[(int)_position++]; result.Append(character); count++;
            if (!line) continue;
            if (_newline is null or "" && character == "\r")
            {
                if (_position < _characters.Count && _characters[(int)_position] == "\n"
                    && (size < 0 || count < size))
                { _position++; result.Append('\n'); }
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
        string text = PyIO.Text(value); string translated = Translate(text);
        if (_position > int.MaxValue) throw new PyOverflowError("StringIO position is too large");
        // Python 的空 write 不填充 seek 形成的空洞。
        if (translated.Length == 0) return 0L;
        while (_characters.Count < _position) _characters.Add("\0");
        foreach (string character in Characters(translated))
        {
            if (_position < _characters.Count) _characters[(int)_position] = character;
            else _characters.Add(character);
            _position++;
        }
        return PyText.Length(text);
    }
    /// <inheritdoc/>
    protected override long SeekCore(long offset, long whence)
    {
        if (whence is < 0 or > 2) throw new PyValueError("invalid whence");
        if (whence != 0 && offset != 0) throw new PyOSError("Can't do nonzero cur-relative seeks");
        if (whence == 0 && offset < 0) throw new PyValueError("Negative seek position");
        _position = whence == 0 ? offset : whence == 2 ? _characters.Count : _position;
        return _position;
    }
    /// <inheritdoc/>
    protected override long TellCore() => _position;
    /// <inheritdoc/>
    protected override long TruncateCore(long? size)
    {
        long length = size ?? _position;
        if (length < 0) throw new PyValueError("Negative size value");
        if (length < _characters.Count) _characters.RemoveRange((int)length, _characters.Count - (int)length);
        return length;
    }
    /// <inheritdoc/>
    protected override void CloseCore() => _characters.Clear();
}
