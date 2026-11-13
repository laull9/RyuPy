using System;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>借用进程标准输出句柄的稳定二进制载体；不解码、不关闭宿主句柄。</summary>
internal sealed class PyStandardBinaryOutput : PyNativeObject
{
    private readonly Stream stream;
    private readonly string name;
    private readonly Func<bool> isatty;
    internal bool Closed { get; private set; }
    /// <summary>构造借用的输出载体；close只转移Python逻辑状态。</summary>
    internal PyStandardBinaryOutput(Stream stream, string name, Func<bool> isatty)
    { this.stream = stream; this.name = name; this.isatty = isatty; }
    /// <inheritdoc />
    public override string TypeName => "BufferedWriter";
    private void Check(string message = "I/O operation on closed file") { if (Closed) throw new PyValueError(message); }
    internal void Flush() { Check("flush of closed file"); PyIO.Guard(() => { stream.Flush(); return PyNone.Instance; }); }
    internal void Close() { if (Closed) return; try { Flush(); } finally { Closed = true; } }
    private object? Write(object? value)
    {
        byte[] bytes = PyIO.Bytes(value);
        Check("write to closed file");
        return PyIO.Guard(() => { stream.Write(bytes); return bytes.LongLength; });
    }
    /// <inheritdoc />
    public override object? GetAttr(string attribute) => attribute switch
    {
        "closed" => Closed, "name" => name, "mode" => "wb",
        "write" => new WriteMethod(this),
        "flush" => PyNativeFunctions.Create("flush", [], [], 0, _ => { Flush(); return PyNone.Instance; }),
        "close" => PyNativeFunctions.Create("close", [], [], 0, _ => { Close(); return PyNone.Instance; }),
        "readable" => PyNativeFunctions.Create(attribute, [], [], 0, _ => false),
        "writable" => PyNativeFunctions.Create(attribute, [], [], 0, _ => { Check(); return true; }),
        "seekable" => PyNativeFunctions.Create(attribute, [], [], 0, _ => { Check(); return false; }),
        "isatty" => PyNativeFunctions.Create(attribute, [], [], 0, _ => { Check(); return isatty(); }),
        "__enter__" => PyNativeFunctions.Create(attribute, [], [], 0, _ => { Check(); return this; }),
        "__exit__" => PyNativeFunctions.Create(attribute, ["type", "value", "traceback"], [null, null, null], 3,
            _ => { Close(); return false; }),
        _ => throw Missing(attribute),
    };
    private sealed class WriteMethod(PyStandardBinaryOutput owner) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "builtin_function_or_method";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__self__" => owner, "__name__" => "write", "__qualname__" => "BufferedWriter.write",
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (names.Length != 0) throw new PyTypeError("BufferedWriter.write() takes no keyword arguments");
            if (args.Length != 1) throw new PyTypeError($"BufferedWriter.write() takes exactly one argument ({args.Length} given)");
            return owner.Write(args[0]);
        }
    }
}
