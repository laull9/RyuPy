// pathlib 目录枚举的惰性迭代器：首次推进才读取目录，快照不持有未关闭的 OS 枚举句柄。
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>单目录 iterdir/glob 的快照迭代器，文件系统副作用与异常延迟到首次 next。</summary>
public sealed class PyPathIterator : PyNativeObject, IPyIterator
{
    private readonly string _directory;
    private readonly string? _pattern;
    private readonly Action? _validate;
    private string[]? _entries;
    private int _position;
    private object? _current;
    private bool _finished;

    internal PyPathIterator(string directory, string? pattern, Action? validate = null)
    {
        _directory = directory;
        _pattern = pattern;
        _validate = validate;
    }

    /// <inheritdoc />
    public override string TypeName => "generator";

    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => this),
        "__next__" => PyNativeFunctions.Create(name, [], [], 0, _ => MoveNext()
            ? Current() : throw new PyStopIteration()),
        "close" => PyNativeFunctions.Create(name, [], [], 0, _ => { _finished = true; _entries = null; _current = null; return PyNone.Instance; }),
        _ => throw Missing(name),
    };

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_finished) return false;
        try
        {
            if (_entries is null)
            {
                _validate?.Invoke();
                if (_pattern is null && File.Exists(_directory))
                    throw new PyNotADirectoryError("[Errno 20] Not a directory");
                if (_pattern is not null && !Directory.Exists(_directory))
                { _finished = true; return false; }
                _entries = PyOs.Guard(() => Directory.GetFileSystemEntries(_directory));
            }
            while (_position < _entries.Length)
            {
                string entry = _entries[_position++];
                if (_pattern is not null && !Match(System.IO.Path.GetFileName(entry), _pattern)) continue;
                _current = new PyPath(entry);
                return true;
            }
            _finished = true;
            _entries = null;
            _current = null;
            return false;
        }
        catch { _finished = true; _entries = null; _current = null; throw; }
    }

    /// <inheritdoc />
    public object? Current() => _current;

    // 有限状态星号回溯的 * / ? 匹配，不依赖.NET通配符兼容性（例如 *.* 不匹配无点文件）。
    private static bool Match(string name, string pattern)
    {
        Rune[] letters = name.EnumerateRunes().ToArray(), tokens = pattern.EnumerateRunes().ToArray();
        int n = 0, p = 0, star = -1, retry = 0;
        while (n < letters.Length)
        {
            if (p < tokens.Length && (tokens[p].Value == '?' || Same(tokens[p], letters[n]))) { p++; n++; }
            else if (p < tokens.Length && tokens[p].Value == '*') { star = p++; retry = n; }
            else if (star >= 0) { p = star + 1; n = ++retry; }
            else return false;
        }
        while (p < tokens.Length && tokens[p].Value == '*') p++;
        return p == tokens.Length;
    }

    private static bool Same(Rune left, Rune right) => OperatingSystem.IsWindows()
        ? Rune.ToUpperInvariant(left) == Rune.ToUpperInvariant(right) : left == right;
}
