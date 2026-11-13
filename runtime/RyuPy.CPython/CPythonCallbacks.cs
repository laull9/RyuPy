// 静态反向调用边界：C栈只看到Python对象/异常；注册根随Python代理析构或显式注销释放。
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint Dispatch(nint identity, int op, nint a, nint b, nint c, long number, nint text)
    {
        CPythonHost? host = Instance;
        if (host is null) { Native.rp_error_text("RuntimeError", "RyuPy host is closed"); return 0; }
        if (op == 0)
        {
            if (host.Roots.TryRemove(identity, out object? root)) host.reverse.Remove(root);
            return 0;
        }
        try
        {
            using Lease lease = host.Enter();
            if (identity == -1)
            {
                string moduleName = host.ReadText(a);
                object importResult = op switch
                {
                    20 => host.compiled.Contains(moduleName),
                    21 => PyModules.Import(moduleName),
                    22 => host.compiled.Any(n => n.StartsWith(moduleName + ".", StringComparison.Ordinal)),
                    _ => throw new PyRuntimeError("invalid import hook operation")
                };
                return host.ToPython(importResult);
            }
            if (!host.Roots.TryGetValue(identity, out object? target)) throw new PyRuntimeError("managed proxy has been unregistered");
            string name = text == 0 ? "" : Marshal.PtrToStringUTF8(text)!;
            host.PullContainers();
            object? result;
            switch (op)
            {
                case 1:
                    object?[] args = host.ReadTuple(a);
                    (string[] names, object?[] values) = host.ReadKeywords(b);
                    result = PyOps.CallValue(target, args, names, values); break;
                case 2: result = PyObject.GetAttr(target, name); break;
                case 3: PyObject.SetAttr(target, name, host.FromBorrowed(a)); result = PyNone.Instance; break;
                case 4: PyObject.DelAttr(target, name); result = PyNone.Instance; break;
                case 5: result = PyOps.GetItem(target, host.ProtocolValue(a)); break;
                case 6: PyOps.SetItem(target, host.ProtocolValue(a), host.FromBorrowed(b)); result = PyNone.Instance; break;
                case 7: PyOps.DelItem(target, host.ProtocolValue(a)); result = PyNone.Instance; break;
                case 8: result = PyOps.Len(target); break;
                case 9: result = PyOps.Truthy(target); break;
                case 10: result = PyRepr.Repr(target); break;
                case 11: result = PyRepr.Str(target); break;
                case 12: result = PyIter.Create(target); break;
                case 13:
                    if (target is not IPyIterator iterator) throw new PyTypeError("managed object is not an iterator");
                    if (!iterator.MoveNext()) { Native.rp_error_text("StopIteration", ""); return 0; }
                    result = iterator.Current(); break;
                case 14: result = PyHash.Of(target); break;
                case 15:
                    int comparison = number switch { 0 => PyOps.Lt, 1 => PyOps.Le, 2 => PyOps.Eq, 3 => PyOps.Ne, 4 => PyOps.Gt, 5 => PyOps.Ge, _ => throw new PyRuntimeError("unknown comparison") };
                    object? compared = host.FromBorrowed(a);
                    // 不把Python已返回NotImplemented的混合比较再次送回同一C槽，避免无限互调。
                    if (compared is IPyForeignObject) return Native.rp_notimplemented();
                    result = PyOps.CompareValue(comparison, target, compared!); break;
                default: throw new PyNotImplementedError("unknown managed protocol operation");
            }
            host.PushContainers();
            return host.ToPython(result);
        }
        catch (PyBaseException error)
        {
            try { host.PushContainers(); } catch (Exception sync) { host.synchronizationFailure = sync.Message; }
            try { Native.rp_restore_error(host.ExportException(error)); }
            catch (Exception nested) { Native.rp_error_text("RuntimeError", nested.Message); }
            return 0;
        }
        catch (Exception error) { Native.rp_error_text("RuntimeError", error.Message); return 0; }
    }

    private object? ProtocolValue(nint pointer)
    {
        object? result = FromBorrowed(pointer);
        if (result is PythonObject foreign)
        {
            int kind = Native.rp_scalar_kind(foreign.Pointer);
            if (kind is 1 or 2) return foreign.ReadScalar("int");
            if (kind == 4) return foreign.ReadScalar("str");
        }
        return result;
    }
    private object?[] ReadTuple(nint pointer)
    {
        int length = checked((int)Native.rp_length(pointer)); ThrowIfError();
        var result = new object?[length];
        for (int i = 0; i < length; i++) result[i] = FromOwned(Native.rp_op(18, pointer, n: i));
        return result;
    }
    private (string[], object?[]) ReadKeywords(nint pointer)
    {
        if (pointer == 0) return ([], []);
        nint keys = Check(Native.rp_op(19, pointer));
        nint tuple = 0;
        try
        {
            tuple = Check(Native.rp_op(26, keys));
            int count = checked((int)Native.rp_length(tuple)); ThrowIfError();
            var names = new string[count]; var values = new object?[count];
            for (int i = 0; i < count; i++)
            {
                nint key = Check(Native.rp_op(18, tuple, n: i));
                try { names[i] = ReadText(key); values[i] = FromOwned(Native.rp_op(6, pointer, key)); }
                finally { Native.rp_decref(key); }
            }
            return (names, values);
        }
        finally { Native.rp_decref(keys); Native.rp_decref(tuple); }
    }
    private nint ExportException(PyBaseException error)
    {
        if (error.ForeignObject is PythonObject original) return original.Pointer;
        nint args = MakeTuple(error.Args);
        nint pointer;
        try { pointer = Check(Native.rp_make_error(error.PyTypeName, args)); } finally { Native.rp_decref(args); }
        var origin = new PythonObject(this, pointer); error.ForeignObject = origin; objects[pointer] = new(error);
        if (error.Cause is { } cause) Native.rp_decref(Check(Native.rp_op(3, pointer, ExportException(cause), s: "__cause__")));
        if (error.Context is { } context) Native.rp_decref(Check(Native.rp_op(3, pointer, ExportException(context), s: "__context__")));
        return pointer;
    }
}
