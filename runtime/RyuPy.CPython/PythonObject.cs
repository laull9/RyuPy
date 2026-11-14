// Python对象的弱身份代理；每个可执行Python协议的入口都经过域权同步、租约与GIL边界。
using RyuPy.Runtime;

namespace RyuPy.CPython;

internal sealed class PythonObject : PyNativeObject, IPyForeignObject, IDisposable
{
    internal CPythonHost Host { get; }
    private readonly nint pointer;
    internal nint Pointer => !IsDisposed ? pointer : throw new ObjectDisposedException("PythonObject");
    private readonly long token;
    private int disposed;
    internal bool IsDisposed => Volatile.Read(ref disposed) != 0;
    internal PythonObject(CPythonHost host, nint pointer) { Host = host; this.pointer = pointer; token = host.Track(pointer); }
    ~PythonObject() => Dispose();
    public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) { Host.Release(token); GC.SuppressFinalize(this); } }
    public override string TypeName => Host.PythonCall(() =>
    {
        nint type = Host.Check(Native.rp_op(13, Pointer));
        try { return Host.TextAttribute(type, "__name__"); } finally { Native.rp_decref(type); }
    });
    public object ForeignType => Host.PythonCall(() => Host.FromOwned(Native.rp_op(13, Pointer))!);
    public override object? GetAttr(string name) => Host.PythonCall(() => Host.FromOwned(Native.rp_op(2, Pointer, s: name)));
    public override void SetAttr(string name, object? value) => Host.PythonCall(() =>
    {
        nint argument = Host.ToPython(value);
        try { Native.rp_decref(Host.Check(Native.rp_op(3, Pointer, argument, s: name))); } finally { Native.rp_decref(argument); }
        return 0;
    });
    public override void DelAttr(string name) => Host.PythonCall(() => { Native.rp_decref(Host.Check(Native.rp_op(4, Pointer, s: name))); return 0; });
    public override string PythonRepr() => Host.PythonCall(() => Host.ReadTextOwned(Native.rp_op(11, Pointer)));
    public override string PythonStr() => Host.PythonCall(() => Host.ReadTextOwned(Native.rp_op(12, Pointer)));
    public object ReadScalar(string kind) => Host.PythonCall<object>(() =>
    {
        if (kind.StartsWith("exact_", StringComparison.Ordinal))
        {
            int required = kind switch { "exact_int" => 1, "exact_bool" => 2, "exact_float" => 3, "exact_str" => 4, "exact_bytes" => 5, _ => 0 };
            if (required == 0 || Native.rp_scalar_kind(Pointer) != required)
                throw new PyTypeError("foreign scalar requires " + kind + " without conversion protocols");
            if (required == 2) return Native.rp_truth(Pointer) != 0;
            if (required == 5)
            {
                nint bytes = Host.Check(Native.rp_bytes_view(Pointer, out long length));
                var contents = new byte[checked((int)length)];
                System.Runtime.InteropServices.Marshal.Copy(bytes, contents, 0, contents.Length);
                return new PyBytes(contents);
            }
            kind = required switch { 1 => "int", 3 => "float", _ => "str" };
        }
        if (kind == "str") return Host.ReadText(Pointer);
        if (kind is "int" or "integer")
        {
            nint indexed = Host.Check(Native.rp_op(kind == "integer" ? 24 : 31, Pointer));
            try
            {
                // 十六进制公开API不受十进制字符串位数上限影响；巨大整数仍由消费者执行自身range检查。
                string digits = Host.ReadTextOwned(Native.rp_integer_hex(indexed));
                bool negative = digits[0] == '-';
                System.Numerics.BigInteger number = System.Numerics.BigInteger.Parse("0" + digits[(negative ? 3 : 2)..],
                    System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture);
                if (negative) number = -number;
                return number >= long.MinValue && number <= long.MaxValue ? (object)(long)number : number;
            }
            finally { Native.rp_decref(indexed); }
        }
        if (kind is "float" or "floating")
        {
            nint number = kind == "floating" ? Host.Check(Native.rp_op(25, Pointer)) : Native.rp_incref(Pointer);
            try { double value = Native.rp_double(number); Host.ThrowIfError(); return value; }
            finally { Native.rp_decref(number); }
        }
        if (kind == "index") return Host.FromOwned(Native.rp_op(31, Pointer))!;
        throw new PyTypeError("unsupported foreign scalar conversion: " + kind);
    });
    public object? Invoke(object?[] args, string[] names, object?[] values) => Host.PythonCall(() =>
    {
        nint tuple = Host.MakeTuple(args), kwargs = 0;
        try
        {
            if (names.Length != 0)
            {
                kwargs = Host.Check(Native.rp_op(16));
                for (int i = 0; i < names.Length; i++)
                {
                    nint value = Host.ToPython(values[i]);
                    try { Native.rp_decref(Host.Check(Native.rp_op(17, kwargs, value, s: names[i]))); }
                    finally { Native.rp_decref(value); }
                }
            }
            return Host.FromOwned(Native.rp_op(5, Pointer, tuple, kwargs));
        }
        finally { Native.rp_decref(tuple); Native.rp_decref(kwargs); }
    });
    public object? Item(object? key) => Host.PythonCall(() =>
    {
        nint argument = Host.ToPython(key);
        try { return Host.FromOwned(Native.rp_op(6, Pointer, argument)); } finally { Native.rp_decref(argument); }
    });
    public void WriteItem(object? key, object? value, bool deleting) => Host.PythonCall(() =>
    {
        nint k = Host.ToPython(key), v = 0;
        try { if (!deleting) v = Host.ToPython(value); Native.rp_decref(Host.Check(Native.rp_op(deleting ? 8 : 7, Pointer, k, v))); }
        finally { Native.rp_decref(k); Native.rp_decref(v); }
        return 0;
    });
    public IPyIterator Iterate() => Host.PythonCall<IPyIterator>(() => new PythonIterator((PythonObject)Host.FromOwned(Native.rp_op(9, Pointer))!));
    public long Length() => Host.PythonCall(() => { long result = Native.rp_length(Pointer); Host.ThrowIfError(); return result; });
    public bool Truth() => Host.PythonCall(() => { int result = Native.rp_truth(Pointer); Host.ThrowIfError(); return result != 0; });
    public long Hash() => Host.PythonCall(() => Host.HashForRuntime(this));
    public object Binary(int operation, object? other, bool reverse) => Host.PythonCall(() =>
    {
        nint argument = Host.ToPython(other);
        try { return Host.FromOwned(Native.rp_op(40, reverse ? argument : Pointer, reverse ? Pointer : argument, n: operation))!; }
        finally { Native.rp_decref(argument); }
    });
    public bool Compare(int operation, object? other, bool reverse) => PyOps.Truthy(RichCompare(operation, other, reverse));
    public object RichCompare(int operation, object? other, bool reverse) => Host.PythonCall(() =>
    {
        if (Host.TryProxyComparison(this, operation, other, reverse, out object compared)) return compared;
        nint argument = Host.ToPython(other);
        int opcode = operation switch { PyOps.Eq => 2, PyOps.Ne => 3, PyOps.Lt => 0, PyOps.Le => 1, PyOps.Gt => 4, PyOps.Ge => 5, _ => throw new PyRuntimeError("unknown comparison") };
        try { return Host.FromOwned(Native.rp_op(20, reverse ? argument : Pointer, reverse ? Pointer : argument, n: opcode))!; }
        finally { Native.rp_decref(argument); }
    });
    public bool Accepts(object? value) => Host.PythonCall(() =>
    {
        nint argument = Host.ToPython(value);
        try { return InstanceCheck(argument, Pointer); } finally { Native.rp_decref(argument); }
    });
    public bool IsExceptionClass => Host.PythonCall(() => Native.rp_is_exception_type(Pointer) != 0);
    public bool IsInstanceOf(object type) => Host.PythonCall(() =>
    {
        nint target = Host.ToPython(type);
        try { return InstanceCheck(Pointer, target); } finally { Native.rp_decref(target); }
    });
    private bool InstanceCheck(nint value, nint type)
    {
        nint result = Host.Check(Native.rp_op(27, value, type));
        try { int truth = Native.rp_truth(result); Host.ThrowIfError(); return truth != 0; } finally { Native.rp_decref(result); }
    }
    public bool MatchesException(PyBaseException error) => Host.PythonCall(() =>
    {
        if (error.ForeignObject is PythonObject original) return Native.rp_exception_matches(original.Pointer, Pointer) != 0;
        nint originalType = Host.Check(Native.rp_builtin(error.PyTypeName));
        try { return Native.rp_exception_matches(originalType, Pointer) != 0; } finally { Native.rp_decref(originalType); }
    });
    public PyStackFrame[] TracebackFrames() => Host.PythonCall(() =>
    {
        var frames = new List<PyStackFrame>();
        for (int i = 0; ; i++)
        {
            int found = Native.rp_traceback_frame(Pointer, i, out nint file, out nint function, out int line);
            if (found < 0) Host.ThrowError();
            if (found == 0) break;
            frames.Add(new(System.Runtime.InteropServices.Marshal.PtrToStringUTF8(file)!, System.Runtime.InteropServices.Marshal.PtrToStringUTF8(function)!, line));
        }
        return frames.ToArray();
    });
    public void SynchronizeException(PyBaseException error, bool write) => Host.SynchronizeException(this, error, write);
    public object Unary(int operation) => Host.PythonCall(() => Host.FromOwned(Native.rp_op(41, Pointer, n: operation))!);
}

internal sealed class PythonIterator(PythonObject iterator) : IPyIterator
{
    private object? current;
    public bool MoveNext() => iterator.Host.PythonCall(() =>
    {
        nint pointer = Native.rp_op(10, iterator.Pointer);
        if (pointer == 0) { iterator.Host.ThrowIfError(); current = null; return false; }
        current = iterator.Host.FromOwned(pointer); return true;
    });
    public object? Current() => current;
}
