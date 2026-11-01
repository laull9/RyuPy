// StreamHandler 在格式化后选择流，一次写入消息与结束符；flush 再读取当前流。
using System;
namespace RyuPy.Runtime;
/// <summary>调用 Python 流协议的日志处理器，不拥有也不关闭用户传入的流。</summary>
public sealed class PyLoggingHandler : PyNativeObject
{
    private object? _stream;
    internal readonly bool DynamicError;
    internal long Level;
    private PyLoggingFormatter? _formatter;
    private string _terminator = "\n";
    /// <summary>默认流捕获当前 stderr；lastResort 可选择每次读取当前 stderr。</summary>
    public PyLoggingHandler(object? stream, bool dynamicError = false)
    { _stream = stream is null or PyNone ? PySystem.Stderr : stream; DynamicError = dynamicError; }
    private object? Stream => DynamicError ? PySystem.Stderr : _stream;
    /// <inheritdoc/>
    public override string TypeName => DynamicError ? "_StderrHandler" : "StreamHandler";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "stream" => Stream ?? PyNone.Instance, "level" => Level, "terminator" => _terminator,
        "formatter" => _formatter ?? (object)PyNone.Instance,
        "setLevel" => PyLogging.One(name, value => { Level = PyLogging.Level(value); return null; }),
        "setFormatter" => PyLogging.One(name, value => { _formatter = value is null or PyNone ? null : value as PyLoggingFormatter
            ?? throw new PyTypeError("formatter must be Formatter or None"); return null; }),
        "flush" => PyLogging.Zero(name, () => { Flush(); return null; }),
        "close" => PyLogging.Zero(name, () => null),
        "setStream" => PyLogging.One(name, value =>
        { if (ReferenceEquals(value, Stream)) return null; object? old = Stream; Flush();
            if (DynamicError) throw new PyAttributeError("property stream has no setter");
            _stream = value; return old; }),
        _ => throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name == "stream" && !DynamicError) _stream = value;
        else if (name == "terminator") _terminator = value as string ?? throw new PyTypeError("terminator must be str");
        else if (name == "level") Level = PyLogging.Level(value);
        else base.SetAttr(name, value);
    }
    private void Flush()
    {
        object? stream = Stream;
        if (stream is null or PyNone) return;
        object? method;
        try { method = PyObject.GetAttr(stream, "flush"); }
        catch (PyAttributeError) { return; }
        PyOps.CallValue(method, [], [], []);
    }
    internal void Emit(PyLoggingRecord record)
    {
        try
        {
            string message = (_formatter ?? PyLogging.DefaultFormatter).Format(record);
            object? stream = Stream;
            PyOps.CallValue(PyObject.GetAttr(stream, "write"), [message + _terminator], [], []);
            Flush();
        }
        catch (PyRecursionError) { throw; }
        catch (PyException error)
        {
            PyErrors.PrepareRaise(error);
            object? previous = PyErrors.EnterHandler(error);
            try
            {
                if (PyOps.Truthy(PyLogging.Option("raiseExceptions", true)))
                {
                    try { PySystem.Write(PySystem.Stderr, "--- Logging error ---\n" + PyTraceback.Format(error)); }
                    catch (PyOSError) { /* 诊断流自身失效时不再递归记录日志。 */ }
                }
            }
            finally { PyErrors.LeaveHandler(previous); }
        }
    }
}
