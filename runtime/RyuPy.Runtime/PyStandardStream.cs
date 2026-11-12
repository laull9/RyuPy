using System;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>标准文本流的原生包装；只包装进程已拥有的流，不额外打开用户路径。</summary>
public sealed class PyStandardStream : PyNativeObject, IPyIterator
{
    private readonly TextReader? _reader;
    private readonly TextWriter? _writer;
    private readonly string _name;
    private bool _closed;
    private bool _detached;
    internal bool Detached => _detached;
    internal bool BufferClosed => _buffer?.Closed == true;
    private readonly PyStandardBinaryOutput? _buffer;
    private bool _skipLineFeed;
    private object? _current;

    /// <summary>创建输入或输出包装。</summary>
    public PyStandardStream(string name, TextReader? reader, TextWriter? writer)
    { _name = name; _reader = reader; _writer = writer; }

    /// <summary>真实标准输出包装与稳定raw载体共享所有权状态。</summary>
    internal PyStandardStream(string name, TextWriter writer, PyStandardBinaryOutput buffer)
    { _name = name; _writer = writer; _buffer = buffer; }

    /// <inheritdoc />
    public override string TypeName => "TextIOWrapper";

    /// <summary>静态print共用此入口，避免为每个值重新创建绑定函数。</summary>
    public void Write(string text)
    {
        CheckOpen();
        if (_writer is null) throw new PyUnsupportedOperation("not writable");
        PyIO.Guard(() => { _writer.Write(TranslateOutput(text, OperatingSystem.IsWindows())); return PyNone.Instance; });
    }

    /// <summary>标准输出 newline=None 的宿主策略；纯函数参数允许在 Unix 验证 Windows 分支。</summary>
    /// <remarks>每个 LF 都翻译，包括已有 CRLF；不读取 TextWriter.NewLine，也不改用户替换的输出对象。</remarks>
    public static string TranslateOutput(string text, bool windows)
    {
        // Unix 常见路径与 Windows 无 LF 路径原样返回，不分配新文本。
        if (!windows || text.IndexOf('\n') < 0) return text;
        return text.Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    /// <summary>显式刷新输出。</summary>
    public void Flush() { CheckOpen(); PyIO.Guard(() => { _writer?.Flush(); return PyNone.Instance; }); }

    private void CheckAttached() { if (_detached) throw new PyValueError("underlying buffer has been detached"); }
    private bool Closed { get { CheckAttached(); return _closed || _buffer?.Closed == true; } }
    private void CheckOpen() { if (Closed) throw new PyValueError("I/O operation on closed file."); }
    private object Detach()
    {
        CheckOpen();
        if (_buffer is null) throw new PyNotImplementedError("detach requires a real standard output binary buffer");
        Flush(); // 刷新失败时必须保留附着状态。
        _detached = true;
        return _buffer;
    }
    private void Close()
    {
        CheckAttached();
        if (Closed) return;
        try { Flush(); }
        finally { _closed = true; _buffer?.Close(); }
    }

    private string Read(long size, bool line)
    {
        CheckOpen();
        if (_reader is null) throw new PyUnsupportedOperation("not readable");
        if (size < -1) size = -1;
        var output = new System.Text.StringBuilder();
        while (size != 0)
        {
            int next = _reader.Read();
            // CR 已足以结束当前行；仅下次确需读取时吞一个可选 LF，避免 Peek 等待下一行。
            // Read 抛错时保留未决状态；非 LF 后继仍在本轮处理，不能误吞。
            if (_skipLineFeed)
            {
                _skipLineFeed = false;
                if (next == '\n') continue;
            }
            if (next < 0) break;
            // 标准输入按通用换行和Unicode码点计数，不暴露UTF-16半个代理项。
            if (next == '\r') { _skipLineFeed = true; next = '\n'; }
            output.Append((char)next);
            if (char.IsHighSurrogate((char)next) && _reader.Peek() >= 0 && char.IsLowSurrogate((char)_reader.Peek()))
                output.Append((char)_reader.Read());
            if (size > 0) size--;
            if (line && next == '\n') break;
        }
        return output.ToString();
    }

    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "name" => StreamName(), "mode" => _reader is null ? "w" : "r", "encoding" => "utf-8",
        "errors" => "strict", "closed" => Closed,
        "buffer" => _detached ? PyNone.Instance : _buffer ?? throw new PyNotImplementedError("binary buffer requires a real standard output stream"),
        "detach" => PyNativeFunctions.Create(name, [], [], 0, _ => Detach()),
        "write" => PyNativeFunctions.Create("write", ["text"], [null], 1, args =>
        { string text = PyCast.ToStr(args[0]); Write(text); return PyText.Length(text); }),
        "read" or "readline" => PyNativeFunctions.Create(name, ["size"], [-1L], 0,
            args => PyIO.Guard(() => Read(PyCast.ToInt64(args[0]), name == "readline"))),
        "flush" => PyNativeFunctions.Create(name, [], [], 0, _ => { Flush(); return PyNone.Instance; }),
        "close" => PyNativeFunctions.Create(name, [], [], 0, _ =>
        { Close(); return PyNone.Instance; }),
        "readable" => PyNativeFunctions.Create(name, [], [], 0, _ => { CheckOpen(); return _reader is not null; }),
        "writable" => PyNativeFunctions.Create(name, [], [], 0, _ => { CheckOpen(); return _writer is not null; }),
        "seekable" => PyNativeFunctions.Create(name, [], [], 0, _ => { CheckOpen(); return false; }),
        "isatty" => PyNativeFunctions.Create(name, [], [], 0, _ =>
        { CheckOpen(); return _reader is not null ? !Console.IsInputRedirected : _name == "<stderr>" ? !Console.IsErrorRedirected : !Console.IsOutputRedirected; }),
        "__enter__" => PyNativeFunctions.Create(name, [], [], 0, _ => { CheckOpen(); return this; }),
        "__exit__" => PyNativeFunctions.Create(name, ["type", "value", "traceback"], [null, null, null], 3,
            _ => { Close(); return false; }),
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => { CheckOpen(); return this; }),
        "__next__" => PyNativeFunctions.Create(name, [], [], 0, _ => MoveNext() ? Current() : throw new PyStopIteration()),
        _ => throw Missing(name),
    };

    private string StreamName() { CheckAttached(); return _name; }

    /// <inheritdoc />
    public bool MoveNext() { string line = Read(-1, true); _current = line; return line.Length > 0; }
    /// <inheritdoc />
    public object? Current() => _current;
}
