// CP侧模块级CIL函数的元数据写入：精确tuple/dict转成同身份原生载体，之后复用同步容器协议。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    internal void SetInspectAttribute(object? target, string name, nint pointer)
    {
        object? value;
        if (target is PyFunction && name == "__defaults__" && Native.rp_exact_tuple(pointer) != 0)
        {
            if (objects.TryGetValue(pointer, out WeakReference<object>? weak) && weak.TryGetTarget(out object? cached) && cached is PyTuple)
                value = cached;
            else
            {
                value = new PyTuple(ReadTuple(pointer));
                CacheInspectValue(value, Native.rp_incref(pointer));
            }
        }
        else if (target is PyFunction && name is "__kwdefaults__" or "__annotations__" or "__dict__"
            && Native.rp_container_kind(pointer) == 2)
            value = ImportInspectDictionary(pointer);
        else value = FromBorrowed(pointer);
        PyObject.SetAttr(target, name, value);
    }
    private void CacheInspectValue(object value, nint owned)
    {
        var owner = new PythonObject(this, owned);
        reverse.Add(value, new ManagedReference(0, owned, owner)); objects[owned] = new(value);
    }
    private PyDict ImportInspectDictionary(nint pointer)
    {
        if (!synchronizeContainers) throw new PyNotImplementedError("cross-domain function metadata dictionaries require synchronized containers");
        if (objects.TryGetValue(pointer, out WeakReference<object>? weak) && weak.TryGetTarget(out object? prior) && prior is PyDict dict)
            return dict;
        var result = new PyDict();
        var owner = new PythonObject(this, Native.rp_incref(pointer));
        var transfer = new ContainerTransfer(result, owner);
        // 提前登记支持容器自引用；失败时成对撤销weak索引与owned引用。
        reverse.Add(result, new ManagedReference(0, pointer, owner)); objects[pointer] = new(result);
        transfers.Add(result, transfer); transferIndex.Add(new(transfer));
        try
        {
            long index = 0;
            while (Native.rp_dict_next(pointer, ref index, out nint key, out nint item) != 0)
            {
                try { result.Set(FromBorrowed(key), FromBorrowed(item)); }
                finally { Native.rp_decref(key); Native.rp_decref(item); }
            }
            ThrowIfError(); transfer.DictSnapshot = [.. result.Entries()]; return result;
        }
        catch
        { reverse.Remove(result); objects.Remove(pointer); transfers.Remove(result); owner.Dispose(); throw; }
    }
}
