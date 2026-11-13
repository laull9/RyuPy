// 默认活代理的精确容器比较：逐索引/逐键读取当前值，保留元素协议和修改副作用。
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    [ThreadStatic] private static int proxyComparisonDepth;

    internal bool TryProxyComparison(PythonObject foreign, int operation, object? other, bool reverse, out object result)
    {
        result = false;
        int kind = Native.rp_container_kind(foreign.Pointer);
        bool list = kind == 1 && other is PyList;
        bool dict = kind == 2 && other is PyDict;
        if ((kind is 3 or 4) && (other is PySet or PyFrozenSet))
            throw new PyNotImplementedError("proxy set/frozenset comparison is not supported");
        if (synchronizeContainers) return false;
        if (!list && !dict) return false;
        if (++proxyComparisonDepth > 128)
        {
            proxyComparisonDepth--;
            throw new PyRecursionError("maximum recursion depth exceeded in container comparison");
        }
        try
        {
            result = list ? CompareProxyList(foreign.Pointer, (PyList)other!, operation, reverse)
                : CompareProxyDictionary(foreign.Pointer, (PyDict)other!, operation, reverse);
            return true;
        }
        finally { proxyComparisonDepth--; }
    }

    private long ContainerLength(nint value)
    {
        long length = Native.rp_length(value); ThrowIfError(); return length;
    }

    private object CompareProxyList(nint python, PyList native, int operation, bool reverse)
    {
        if (operation is PyOps.Eq or PyOps.Ne && ContainerLength(python) != native.Count)
            return operation == PyOps.Ne;
        long index = 0;
        while (index < ContainerLength(python) && index < native.Count)
        {
            // 先持有两侧当前元素，再调用用户比较；删除/扩容不会使本轮元素悬空。
            object? foreignItem = FromOwned(Native.rp_list_item(python, index));
            object? nativeItem = native.GetItem(index);
            object? left = reverse ? nativeItem : foreignItem, right = reverse ? foreignItem : nativeItem;
            if (!ReferenceEquals(left, right) && !PyHash.Equal(left, right))
                return operation switch
                {
                    PyOps.Eq => false, PyOps.Ne => true,
                    _ => PyOps.CompareValue(operation, left!, right!),
                };
            index++;
        }
        long foreignLength = ContainerLength(python), nativeLength = native.Count;
        return PyOps.Compare(operation, reverse ? nativeLength : foreignLength, reverse ? foreignLength : nativeLength);
    }

    private bool SafeNativeKey(object? value, int depth = 0) => depth < 128 && (value switch
    {
        null or PyNone or bool or int or long or BigInteger or double or string or PyBytes => true,
        PyTuple tuple when value.GetType() == typeof(PyTuple) => tuple.Items.All(item => SafeNativeKey(item, depth + 1)),
        PythonObject foreign when ReferenceEquals(foreign.Host, this) => SafePythonKey(foreign.Pointer, depth + 1),
        _ => false,
    });

    private bool SafePythonKey(nint key, int depth = 0)
    {
        if (depth >= 128) return false;
        if (Native.rp_is_none(key) != 0 || Native.rp_scalar_kind(key) != 0) return true;
        if (Native.rp_exact_tuple(key) == 0) return false;
        long count = ContainerLength(key);
        for (long index = 0; index < count; index++)
        {
            nint item = Check(Native.rp_op(18, key, n: index));
            try { if (!SafePythonKey(item, depth + 1)) return false; }
            finally { Native.rp_decref(item); }
        }
        return true;
    }

    private void ValidateProxyDictionaryKeys(nint python, PyDict native)
    {
        foreach (object? key in native.Keys)
            if (!SafeNativeKey(key)) throw new PyNotImplementedError("proxy dictionary comparison requires exact immutable keys");
        long position = 0;
        while (Native.rp_dict_next(python, ref position, out nint key, out nint value) != 0)
        {
            try
            {
                if (!SafePythonKey(key)) throw new PyNotImplementedError("proxy dictionary comparison requires exact immutable keys");
            }
            finally { Native.rp_decref(key); Native.rp_decref(value); }
        }
    }

    private static bool NextDictionaryEntry(nint python, ref long position, bool rescan, out nint key, out nint value)
    {
        if (!rescan) return Native.rp_dict_next(python, ref position, out key, out value) != 0;
        // PyDict_Next不允许跨键结构修改复用游标；冷路从0扫描当前表到先前槽位之后。
        long current = 0;
        while (Native.rp_dict_next(python, ref current, out key, out value) != 0)
        {
            if (current > position) { position = current; return true; }
            Native.rp_decref(key); Native.rp_decref(value);
        }
        key = value = 0;
        return false;
    }

    private object CompareProxyDictionary(nint python, PyDict native, int operation, bool reverse)
    {
        if (operation is not (PyOps.Eq or PyOps.Ne))
            throw new PyTypeError("ordering comparison is not supported between dict instances");
        if (ContainerLength(python) != native.Count) return operation == PyOps.Ne;
        // 自定义键域先拒绝；不通过临时字典重hash，不增加用户hash/eq调用次数。
        ValidateProxyDictionaryKeys(python, native);
        bool rescan = false;
        bool Intrinsic(object? value) => SafeNativeKey(value)
            || value is PythonObject foreign && SafePythonKey(foreign.Pointer);
        bool EqualValues(object? left, object? right)
        {
            bool equal = ReferenceEquals(left, right) || PyHash.Equal(left, right);
            // 用户值比较可增删任一字典的键；下一次lookup前重核域，常用标量值不走此冷路。
            if (equal && (!Intrinsic(left) || !Intrinsic(right)))
            { ValidateProxyDictionaryKeys(python, native); rescan = true; }
            return equal;
        }
        bool equal = true;
        if (reverse)
        {
            foreach ((object? key, object? value) in native.Entries())
            {
                if (!SafeNativeKey(key)) throw new PyNotImplementedError("proxy dictionary comparison requires exact immutable keys");
                nint argument = ToPython(key);
                int found; nint item;
                try { found = Native.rp_dict_lookup(python, argument, out item); }
                finally { Native.rp_decref(argument); }
                if (found < 0) ThrowError();
                if (found == 0) { equal = false; break; }
                if (!EqualValues(value, FromOwned(item))) { equal = false; break; }
            }
        }
        else
        {
            long position = 0;
            while (NextDictionaryEntry(python, ref position, rescan, out nint key, out nint value))
            {
                rescan = false;
                try
                {
                    if (!SafePythonKey(key)) throw new PyNotImplementedError("proxy dictionary comparison requires exact immutable keys");
                    object? managedKey = FromBorrowed(key), managedValue = FromBorrowed(value);
                    if (!native.TryGet(managedKey, out object? item) || !EqualValues(managedValue, item))
                    { equal = false; break; }
                }
                finally { Native.rp_decref(key); Native.rp_decref(value); }
            }
        }
        return operation == PyOps.Eq ? equal : !equal;
    }
}
