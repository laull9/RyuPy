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
        if (TryImportInspect(pointer, out object? inspected)) return inspected;
        if (Native.rp_is_none(pointer) != 0) { Native.rp_decref(pointer); return PyNone.Instance; }
        if (Native.rp_scalar_kind(pointer) == 2)
        {
            bool truth = Native.rp_truth(pointer) != 0; Native.rp_decref(pointer); return truth ? PyOps.TrueObject : PyOps.FalseObject;
        }
        if (Native.rp_small_integer(pointer, out long small) != 0) { Native.rp_decref(pointer); return PyOps.BoxInt(small); }
        int superKind = Native.rp_super_kind(pointer);
        if (superKind == 2) { Native.rp_decref(pointer); return PyLexicalSuper.Type; }
        nint id = Native.rp_identity(pointer);
        if (id != 0 && Roots.TryGetValue(id, out object? original)) { Native.rp_decref(pointer); return original; }
        if (objects.TryGetValue(pointer, out WeakReference<object>? weak) && weak.TryGetTarget(out object? existing)
            && existing is not PythonObject { IsDisposed: true }
            && existing is not PyBaseException { ForeignObject: PythonObject { IsDisposed: true } }
            && !(existing is PyInstance staleInstance && PyForeignClassBridge.GetForeignInstance(staleInstance) is PythonObject { IsDisposed: true })
            && !(existing is PySuper { ForeignObject: PythonObject { IsDisposed: true } })
            && !(existing is PyClass staleClass && PyForeignClassBridge.GetForeignClass(staleClass) is PythonObject { IsDisposed: true }))
        { Native.rp_decref(pointer); return existing; }
        if (Native.rp_exception_instance(pointer) != 0) return ExceptionFromOwned(pointer);
        if (Native.rp_is_module(pointer) != 0) return ModuleFromOwned(pointer);
        if (TryResolveForeignDerivedInstance(pointer, out PyInstance? derived)) return derived;
        var result = new PythonObject(this, pointer);
        object converted = superKind == 1 ? PySuper.FromForeign(result, result.GetAttr) : result;
        objects[pointer] = new(converted); return converted;
    }
    internal unsafe nint ToPython(object? value)
    {
        if (value is null or PyNone) return Native.rp_none();
        if (TryExportInspect(value, out nint inspected)) return inspected;
        if (value is PyNativeType superType && ReferenceEquals(superType, PyLexicalSuper.Type)) return Native.rp_super_type();
        if (value is PySuper super)
        {
            if (PyForeignClassBridge.GetForeignSuper(super) is not PythonObject actual || !ReferenceEquals(actual.Host, this))
                throw new PyNotImplementedError("exporting super requires a matching foreign class layout");
            // 本地词法代理首次导出后，往返仍返回原中性对象，而非第二个 CLR super 身份。
            objects[actual.Pointer] = new(super);
            return Native.rp_incref(actual.Pointer);
        }
        if (value is PythonObject foreign)
        {
            if (!ReferenceEquals(foreign.Host, this)) throw new PyTypeError("objects belong to different interpreters");
            return Native.rp_incref(foreign.Pointer);
        }
        if (value is PyInstance instance && PyForeignClassBridge.GetForeignInstance(instance) is PythonObject foreignInstance)
        {
            if (!ReferenceEquals(foreignInstance.Host, this)) throw new PyTypeError("objects belong to different interpreters");
            return Native.rp_incref(foreignInstance.Pointer);
        }
        if (value is PyClass cls && PyForeignClassBridge.GetForeignClass(cls) is PythonObject foreignClass)
        {
            if (!ReferenceEquals(foreignClass.Host, this)) throw new PyTypeError("objects belong to different interpreters");
            return Native.rp_incref(foreignClass.Pointer);
        }
        if (value is PyClass mixin && _mixinMirrors.TryGetValue(mixin, out PythonObject? mirror) && !mirror.IsDisposed)
            return Native.rp_incref(mirror.Pointer);
        if (value is PyBaseException { ForeignObject: PythonObject origin }) return Native.rp_incref(origin.Pointer);
        if (value is PyBaseException error) return Native.rp_incref(ExportException(error));
        if (value is PyClass { IsException: true } exceptionClass) return ExceptionClass(exceptionClass);
        if (reverse.TryGetValue(value, out ManagedReference? reference))
        {
            if (HasSetName(value) && Native.rp_is_managed_descriptor(reference.Pointer) == 0)
                throw new PyNotImplementedError("exported managed proxy cannot add __set_name__ capability while preserving its identity");
            ValidateSetNameDescriptor(value);
            return Native.rp_incref(reference.Pointer);
        }
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
        try
        {
            bool descriptor = HasSetName(value);
            ValidateSetNameDescriptor(value);
            pointer = Check(descriptor ? Native.rp_managed_descriptor(identity) : Native.rp_managed(identity));
        }
        catch { Roots.TryRemove(identity, out _); throw; }
        reverse.Add(value, new ManagedReference(identity, pointer));
        return pointer;
    }
    // 能力只取真实类型链；实例自己的同名字段不能签发特殊槽。已导出普通代理不换身份升级。
    private static bool HasSetName(object? value) => value is PyInstance instance
        && instance.Class.TryLookup("__set_name__", out _, out _);
    private static void ValidateSetNameDescriptor(object? value)
    {
        if (HasSetName(value) && value is PyInstance instance
            && (instance.Class.TryLookup("__get__", out _, out _)
                || instance.Class.TryLookup("__set__", out _, out _)
                || instance.Class.TryLookup("__delete__", out _, out _)))
            throw new PyNotImplementedError("custom descriptor __get__/__set__/__delete__ protocols are not bridged");
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
