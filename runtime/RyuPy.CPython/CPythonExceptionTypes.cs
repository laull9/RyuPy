// 用户异常类按Runtime类身份生成真实CPython异常类型；基类与实例往返共用同一映射。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private static void RequireExportableException(PyClass type)
    {
        foreach (string attribute in type.Attributes.Keys)
            if (attribute is not ("__module__" or "__qualname__" or "__doc__"))
                throw new PyNotImplementedError($"CPython exception class '{type.Name}' attribute '{attribute}' is not supported");
        if (PyObject.GetAttr(type, "__bases__") is not PyTuple { Length: 1 })
            throw new PyNotImplementedError("CPython exception classes require one exception base");
        if (type.ExceptionFactory is null)
            throw new PyNotImplementedError("CPython exception class requires its compiled Runtime factory");
        if (PyObject.GetAttr(type, "__module__") is not string || PyObject.GetAttr(type, "__qualname__") is not string
            || type.Attributes.TryGetValue("__doc__", out object? doc) && doc is not (null or PyNone or string))
            throw new PyNotImplementedError("CPython exception class metadata must be strings or a None docstring");
        if (type.BaseClass is { IsException: true } ancestor) RequireExportableException(ancestor);
    }

    private nint ExceptionClass(PyClass type)
    {
        // 整条继承链先预检，任何未实现布局都在创建CP类型/冻结Runtime类之前失败。
        RequireExportableException(type);
        if (reverse.TryGetValue(type, out ManagedReference? prior) && prior.Owner is { IsDisposed: false })
            return Native.rp_incref(prior.Pointer);
        reverse.Remove(type);
        nint parent = type.BaseClass is { IsException: true } ancestor
            ? ExceptionClass(ancestor) : Check(Native.rp_builtin(type.BuiltinBase ?? "Exception"));
        nint pointer;
        // 模块限定名只控制显示；缓存键始终是实际PyClass引用，允许同名独立定义。
        string module = (string)PyObject.GetAttr(type, "__module__")!;
        nint metadata = 0;
        try
        {
            metadata = Check(Native.rp_op(16));
            foreach (string key in new[] { "__qualname__", "__doc__" })
            {
                object? value = key == "__qualname__" ? PyObject.GetAttr(type, key)
                    : type.Attributes.GetValueOrDefault(key);
                nint item = ToPython(value);
                try { Native.rp_decref(Check(Native.rp_op(17, metadata, item, s: key))); }
                finally { Native.rp_decref(item); }
            }
            pointer = Check(Native.rp_exception_type(module + "." + type.Name, parent, metadata));
        }
        finally { Native.rp_decref(metadata); Native.rp_decref(parent); }
        var owner = new PythonObject(this, pointer);
        reverse.Add(type, new ManagedReference(0, pointer, owner)); objects[pointer] = new(type);
        type.FreezeExportedException();
        return Native.rp_incref(pointer);
    }

    private static void RequireExportableException(PyBaseException error)
    {
        if (error.UserAttributes.Count != 0)
            throw new PyNotImplementedError("CPython exception instance attributes are not supported");
        if (error.PythonClass is { } type) RequireExportableException(type);
    }

    private nint ExceptionClass(PyBaseException error)
    {
        RequireExportableException(error);
        return error.PythonClass is { } type ? ExceptionClass(type) : Check(Native.rp_builtin(error.PyTypeName));
    }

    // CP侧直接调用导出的异常类构造实例时，也恢复原Runtime类供类型化匹配使用。
    private PyBaseException CreateImportedException(nint pointer, string builtin, string name)
    {
        nint type = Check(Native.rp_op(13, pointer)), mro = 0;
        try
        {
            mro = Check(Native.rp_op(2, type, s: "__mro__"));
            long count = Native.rp_length(mro); ThrowIfError();
            // CP侧可继续派生；最近已映射祖先决定CLR catch壳，ForeignObject保留实际CP子类。
            for (long index = 0; index < count; index++)
            {
                nint ancestor = Check(Native.rp_op(18, mro, n: index));
                try
                {
                    if (!objects.TryGetValue(ancestor, out WeakReference<object>? weak)
                        || !weak.TryGetTarget(out object? value) || value is not PyClass original) continue;
                    if (original.ExceptionFactory is null)
                        throw new PyNotImplementedError("CPython exception class requires its compiled Runtime factory");
                    PyBaseException result = original.ExceptionFactory(original, []);
                    result.BindPythonClass(original);
                    return result;
                }
                finally { Native.rp_decref(ancestor); }
            }
        }
        finally { Native.rp_decref(mro); Native.rp_decref(type); }
        return PyExceptionTypes.IsKnown(builtin) ? PyExceptionTypes.Create(builtin, "") : new PyBaseException(name, "");
    }
}
