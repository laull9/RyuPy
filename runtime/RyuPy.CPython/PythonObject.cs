// Python对象的弱身份代理；所有操作持租约，原始指针永不进入Runtime或生成的CIL。
using RyuPy.Runtime;

namespace RyuPy.CPython;

internal sealed class PythonObject : PyNativeObject, IPyForeignObject, IDisposable
{
    internal CPythonHost Host { get; }
    private readonly nint pointer;
    internal nint Pointer => Volatile.Read(ref disposed) == 0 ? pointer : throw new ObjectDisposedException("PythonObject");
    private readonly long token;
    private int disposed;
    internal bool IsDisposed => Volatile.Read(ref disposed) != 0;
    internal PythonObject(CPythonHost host, nint pointer) { Host = host; this.pointer = pointer; token = host.Track(pointer); }
    ~PythonObject() => Dispose();
    public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) { Host.Release(token); GC.SuppressFinalize(this); } }
    public override string TypeName
    {
        get { using var lease = Host.Enter(); nint type = Host.Check(Native.rp_op(13, Pointer)); try { return Host.TextAttribute(type, "__name__"); } finally { Native.rp_decref(type); } }
    }
    public object ForeignType { get { using var lease = Host.Enter(); return Host.FromOwned(Native.rp_op(13, Pointer))!; } }
    public override object? GetAttr(string name)
    {
        using var lease = Host.Enter(); return Host.FromOwned(Native.rp_op(2, Pointer, s: name));
    }
    public override void SetAttr(string name, object? value)
    {
        using var lease = Host.Enter(); nint argument = Host.ToPython(value);
        try { Native.rp_decref(Host.Check(Native.rp_op(3, Pointer, argument, s: name))); } finally { Native.rp_decref(argument); }
    }
    public override void DelAttr(string name) { using var lease = Host.Enter(); Native.rp_decref(Host.Check(Native.rp_op(4, Pointer, s: name))); }
    public override string PythonRepr() { using var lease = Host.Enter(); return Host.ReadTextOwned(Native.rp_op(11, Pointer)); }
    public override string PythonStr() { using var lease = Host.Enter(); return Host.ReadTextOwned(Native.rp_op(12, Pointer)); }
    public object ReadScalar(string kind)
    {
        using var lease = Host.Enter();
        if (kind == "str") return Host.ReadText(Pointer);
        if (kind == "int")
        {
            nint indexed = Host.Check(Native.rp_op(31, Pointer));
            try
            {
                string digits = Host.ReadTextOwned(Native.rp_op(12, indexed));
                System.Numerics.BigInteger number = System.Numerics.BigInteger.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
                return number >= long.MinValue && number <= long.MaxValue ? (object)(long)number : number;
            }
            finally { Native.rp_decref(indexed); }
        }
        if (kind == "float") { double value = Native.rp_double(Pointer); Host.ThrowIfError(); return value; }
        if (kind == "index") return Host.FromOwned(Native.rp_op(31, Pointer))!;
        throw new PyTypeError("unsupported foreign scalar conversion: " + kind);
    }
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        return Host.PythonCall(() =>
        {
        nint tuple = Host.MakeTuple(args), kwargs = 0;
        try
        {
            if (names.Length != 0)
            {
                kwargs = Host.Check(Native.rp_op(16));
                for (int index = 0; index < names.Length; index++)
                {
                    nint value = Host.ToPython(values[index]);
                    try { Native.rp_decref(Host.Check(Native.rp_op(17, kwargs, value, s: names[index]))); }
                    finally { Native.rp_decref(value); }
                }
            }
            return Host.FromOwned(Native.rp_op(5, Pointer, tuple, kwargs));
        }
        finally { Native.rp_decref(tuple); Native.rp_decref(kwargs); }
        });
    }
    public object? Item(object? key)
    {
        using var lease = Host.Enter(); nint argument = Host.ToPython(key);
        try { return Host.FromOwned(Native.rp_op(6, Pointer, argument)); } finally { Native.rp_decref(argument); }
    }
    public void WriteItem(object? key, object? value, bool deleting)
    {
        using var lease = Host.Enter(); nint k = Host.ToPython(key), v = 0;
        try { if (!deleting) v = Host.ToPython(value); Native.rp_decref(Host.Check(Native.rp_op(deleting ? 8 : 7, Pointer, k, v))); }
        finally { Native.rp_decref(k); Native.rp_decref(v); }
    }
    public IPyIterator Iterate()
    {
        using var lease = Host.Enter(); return new PythonIterator((PythonObject)Host.FromOwned(Native.rp_op(9, Pointer))!);
    }
    public long Length() { using var lease = Host.Enter(); long result = Native.rp_length(Pointer); Host.ThrowIfError(); return result; }
    public bool Truth() { using var lease = Host.Enter(); int result = Native.rp_truth(Pointer); Host.ThrowIfError(); return result != 0; }
    public long Hash() { using var lease = Host.Enter(); return Host.HashForRuntime(this); }
    public object Binary(int operation, object? other, bool reverse)
    {
        using var lease = Host.Enter(); nint argument = Host.ToPython(other);
        try { return Host.FromOwned(Native.rp_op(40, reverse ? argument : Pointer, reverse ? Pointer : argument, n: operation))!; }
        finally { Native.rp_decref(argument); }
    }
    public bool Compare(int operation, object? other, bool reverse) => PyOps.Truthy(RichCompare(operation, other, reverse));
    public object RichCompare(int operation, object? other, bool reverse)
    {
        using var lease = Host.Enter(); nint argument = Host.ToPython(other);
        int opcode = operation switch { PyOps.Eq => 2, PyOps.Ne => 3, PyOps.Lt => 0, PyOps.Le => 1, PyOps.Gt => 4, PyOps.Ge => 5, _ => throw new PyRuntimeError("unknown comparison") };
        try { return Host.FromOwned(Native.rp_op(20, reverse ? argument : Pointer, reverse ? Pointer : argument, n: opcode))!; }
        finally { Native.rp_decref(argument); }
    }
    public bool Accepts(object? value)
    {
        using var lease = Host.Enter(); nint argument = Host.ToPython(value);
        try { nint result = Host.Check(Native.rp_op(27, argument, Pointer)); try { int truth = Native.rp_truth(result); Host.ThrowIfError(); return truth != 0; } finally { Native.rp_decref(result); } }
        finally { Native.rp_decref(argument); }
    }
    public bool IsExceptionClass { get { using var lease = Host.Enter(); return Native.rp_is_exception_type(Pointer) != 0; } }
    public bool IsInstanceOf(object type)
    {
        using var lease = Host.Enter(); nint target = Host.ToPython(type);
        try { nint result = Host.Check(Native.rp_op(27, Pointer, target)); try { int truth = Native.rp_truth(result); Host.ThrowIfError(); return truth != 0; } finally { Native.rp_decref(result); } }
        finally { Native.rp_decref(target); }
    }
    public bool MatchesException(PyBaseException error)
    {
        using var lease = Host.Enter();
        if (error.ForeignObject is PythonObject original) return Native.rp_exception_matches(original.Pointer, Pointer) != 0;
        nint originalType = Host.Check(Native.rp_builtin(error.PyTypeName));
        try { return Native.rp_exception_matches(originalType, Pointer) != 0; } finally { Native.rp_decref(originalType); }
    }
    public object Unary(int operation) { using var lease = Host.Enter(); return Host.FromOwned(Native.rp_op(41, Pointer, n: operation))!; }
}

internal sealed class PythonIterator(PythonObject iterator) : IPyIterator
{
    private object? current;
    public bool MoveNext()
    {
        using var lease = iterator.Host.Enter(); nint pointer = Native.rp_op(10, iterator.Pointer);
        if (pointer == 0) { iterator.Host.ThrowIfError(); current = null; return false; }
        current = iterator.Host.FromOwned(pointer); return true;
    }
    public object? Current() => current;
}
