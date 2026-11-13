// new/borrowed引用转换、弱身份缓存和显式标量复制；可变托管对象只暴露活代理。
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    internal nint Check(nint pointer) { if (pointer == 0) ThrowError(); return pointer; }
    internal void ThrowIfError() { if (Native.rp_error() != 0) ThrowError(); }
    internal void ThrowError()
    {
        nint error = Native.rp_take_error();
        if (error == 0) throw new PyRuntimeError("CPython bridge returned NULL without an exception");
        throw PyErrors.ReceiveForeign(ExceptionFromOwned(error));
    }
    internal object? FromBorrowed(nint pointer) => pointer == 0 ? null : FromOwned(Native.rp_incref(pointer));
    internal object? FromOwned(nint pointer)
    {
        Check(pointer);
        if (Native.rp_is_none(pointer) != 0) { Native.rp_decref(pointer); return PyNone.Instance; }
        if (Native.rp_scalar_kind(pointer) == 2)
        {
            bool truth = Native.rp_truth(pointer) != 0; Native.rp_decref(pointer); return truth ? PyOps.TrueObject : PyOps.FalseObject;
        }
        if (Native.rp_small_integer(pointer, out long small) != 0) { Native.rp_decref(pointer); return PyOps.BoxInt(small); }
        nint id = Native.rp_identity(pointer);
        if (id != 0 && Roots.TryGetValue(id, out object? original)) { Native.rp_decref(pointer); return original; }
        if (objects.TryGetValue(pointer, out WeakReference<object>? weak) && weak.TryGetTarget(out object? existing) && existing is not PythonObject { IsDisposed: true } && existing is not PyBaseException { ForeignObject: PythonObject { IsDisposed: true } })
        { Native.rp_decref(pointer); return existing; }
        if (Native.rp_exception_instance(pointer) != 0) return ExceptionFromOwned(pointer);
        if (Native.rp_is_module(pointer) != 0) return ModuleFromOwned(pointer);
        var result = new PythonObject(this, pointer); objects[pointer] = new(result); return result;
    }
    internal unsafe nint ToPython(object? value)
    {
        if (value is null or PyNone) return Native.rp_none();
        if (value is PythonObject foreign)
        {
            if (!ReferenceEquals(foreign.Host, this)) throw new PyTypeError("objects belong to different interpreters");
            return Native.rp_incref(foreign.Pointer);
        }
        if (value is PyBaseException { ForeignObject: PythonObject origin }) return Native.rp_incref(origin.Pointer);
        if (value is PyBaseException error) return Native.rp_incref(ExportException(error));
        if (value is PyClass { IsException: true } exceptionClass) return ExceptionClass(exceptionClass);
        if (reverse.TryGetValue(value, out ManagedReference? reference)) return Native.rp_incref(reference.Pointer);
        nint CacheScalar(nint scalar)
        {
            Check(scalar); var owner = new PythonObject(this, scalar);
            reverse.Add(value, new ManagedReference(0, scalar, owner)); objects[scalar] = new(value);
            return Native.rp_incref(scalar);
        }
        switch (value)
        {
            case bool flag: return Native.rp_bool(flag ? 1 : 0);
            case long number: return CacheScalar(Native.rp_int(number));
            case int number: return CacheScalar(Native.rp_int(number));
            case double number: return CacheScalar(Native.rp_float(number));
            case BigInteger number: return CacheScalar(Native.rp_op(21, s: number.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            case string text:
                byte[] utf8 = Encoding.UTF8.GetBytes(text);
                fixed (byte* bytes = utf8) return CacheScalar(Native.rp_text(bytes, utf8.Length));
            case PyBytes data:
                fixed (byte* bytes = data.Bytes) return CacheScalar(Native.rp_bytes(bytes, data.Bytes.Length));
            case PyType builtin: return Check(Native.rp_builtin(builtin.Name));
            case PySlice slice:
                nint start = ToPython(PyObject.GetAttr(slice, "start")), stop = 0, step = 0;
                try { stop = ToPython(PyObject.GetAttr(slice, "stop")); step = ToPython(PyObject.GetAttr(slice, "step")); return Check(Native.rp_op(28, start, stop, step)); }
                finally { Native.rp_decref(start); Native.rp_decref(stop); Native.rp_decref(step); }
        }
        if (value.GetType() == typeof(PyTuple))
        {
            var tuple = (PyTuple)value;
            nint nativeTuple = Check(Native.rp_op(14, n: tuple.Length));
            var owner = new PythonObject(this, nativeTuple);
            reverse.Add(value, new ManagedReference(0, nativeTuple, owner)); objects[nativeTuple] = new(value);
            try
            {
                for (int i = 0; i < tuple.Length; i++)
                {
                    nint item = ToPython(tuple.Items[i]);
                    try { Native.rp_decref(Check(Native.rp_op(15, nativeTuple, item, n: i))); } finally { Native.rp_decref(item); }
                }
                return Native.rp_incref(nativeTuple);
            }
            catch { reverse.Remove(value); owner.Dispose(); throw; }
        }
        if (synchronizeContainers && value is PyList or PyDict) return TransferContainer(value);
        // 弱反向索引不钉住托管对象；C端代理存活期间Roots才是明确的强根。
        nint identity = (nint)Interlocked.Increment(ref nextRoot);
        Roots[identity] = value;
        nint pointer;
        try { pointer = Check(Native.rp_managed(identity)); } catch { Roots.TryRemove(identity, out _); throw; }
        reverse.Add(value, new ManagedReference(identity, pointer));
        return pointer;
    }
    internal nint MakeTuple(object?[] values)
    {
        nint tuple = Check(Native.rp_op(14, n: values.Length));
        try
        {
            for (int i = 0; i < values.Length; i++)
            {
                nint item = ToPython(values[i]);
                try { Native.rp_decref(Check(Native.rp_op(15, tuple, item, n: i))); } finally { Native.rp_decref(item); }
            }
            return tuple;
        }
        catch { Native.rp_decref(tuple); throw; }
    }
    internal string ReadText(nint pointer)
    {
        nint text = Native.rp_utf8(pointer, out long length); Check(text);
        return Marshal.PtrToStringUTF8(text, checked((int)length))!;
    }
    internal string ReadTextOwned(nint pointer)
    {
        Check(pointer); try { return ReadText(pointer); } finally { Native.rp_decref(pointer); }
    }
    internal string TextAttribute(nint pointer, string name) => ReadTextOwned(Native.rp_op(2, pointer, s: name));

    internal void SynchronizeException(PythonObject origin, PyBaseException error, bool write) => PythonCall(() =>
    {
        if (write) ExportException(error);
        else ExceptionFromOwned(Native.rp_incref(origin.Pointer));
        return 0;
    });

    private PyBaseException ExceptionFromOwned(nint pointer, HashSet<nint>? visiting = null)
    {
        PyBaseException error;
        PythonObject origin;
        if (objects.TryGetValue(pointer, out WeakReference<object>? weak) && weak.TryGetTarget(out object? prior)
            && prior is PyBaseException { ForeignObject: PythonObject { IsDisposed: false } existing } same)
        { origin = existing; error = same; Native.rp_decref(pointer); }
        else
        {
            // 先建立实例身份再读取cause/context，允许异常链形成环。
            origin = new PythonObject(this, pointer);
            string builtin = Marshal.PtrToStringUTF8(Native.rp_builtin_exception(pointer))!;
            error = CreateImportedException(pointer, builtin, Marshal.PtrToStringUTF8(Native.rp_exception_name(pointer))!);
            error.ForeignObject = origin; objects[pointer] = new(error);
        }
        visiting ??= [];
        if (!visiting.Add(pointer)) return error;
        // CPython可以在同一实例再次传播前修改链；真实传播入口每次刷新，且不调用用户str。
        string name = Marshal.PtrToStringUTF8(Native.rp_exception_name(pointer))!;
        nint arguments = Check(Native.rp_exception_args(pointer));
        try { error.BindForeignException(origin, ReadTuple(arguments), name); }
        finally { Native.rp_decref(arguments); }
        PyBaseException? ReadLink(int cause)
        {
            nint link = Check(Native.rp_exception_link(pointer, cause));
            if (Native.rp_is_none(link) != 0) { Native.rp_decref(link); return null; }
            return ExceptionFromOwned(link, visiting);
        }
        error.BindForeignChain(ReadLink(1), ReadLink(0), Native.rp_exception_suppress(pointer) != 0);
        return error;
    }
}
