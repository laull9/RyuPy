// dict 真实布局根协议：只访问原始存储；仅缺键下标查询调用类上的 __missing__。
using System;
using System.Numerics;
namespace RyuPy.Runtime;

internal static class PyDictProtocols
{
    // 根比较不经过编译函数帧：保守限制256层，避免两个不同自环触发CLR StackOverflow。
    // 此计数只属于新dict根协议，不改变旧exact比较，也不承诺遵循sys.recursionlimit。
    [ThreadStatic] private static int _comparisonDepth;
    internal static long Len(object self) => PyDictLayout.RequireRaw(self).Count;
    internal static bool Truthy(object self) => Len(self) != 0;
    internal static IPyIterator Iter(object self) => new PyDictLayoutIterator(PyDictLayout.RequireRaw(self), PyDictViewKind.Keys);
    internal static IPyIterator Reversed(object self) => new PyDictLayoutIterator(PyDictLayout.RequireRaw(self), PyDictViewKind.Keys, true);
    internal static void RequireScalarKey(object? key)
    {
        if (key is null or PyNone or string or bool or int or long or BigInteger or double) return;
        if (!PyHash.IsHashable(key))
            throw PyDictLayout.Own(new PyTypeError($"unhashable type: '{PyRepr.TypeName(key)}'"));
        throw PyDictLayout.Own(new PyNotImplementedError("dict layout with non-scalar keys is not supported"));
    }
    internal static void RequireScalarContents(PyDict raw)
    {
        for (int i = 0; i < raw.SlotCount; i++)
            if (raw.TryKeyAt(i, out object? key)) RequireScalarKey(key);
    }
    internal static object? Item(object self, object? key)
    {
        PyDict raw = PyDictLayout.RequireRaw(self);
        RequireScalarKey(key);
        if (self is not PyDictInstance) RequireScalarContents(raw);
        if (raw.TryGet(key ?? PyNone.Instance, out object? value)) return value;
        // bool 区分缺键与查询中异常；不捕获用户代码抛出的 KeyError，也不读实例属性。
        if (self is PyDictInstance user && user.Class.TryLookup("__missing__", out PyClass owner, out object? method))
            return PyObject.InvokeBound(method, owner, self, [key ?? PyNone.Instance], [], []);
        throw PyDictLayout.Own(PyKeyError.ForKey(key));
    }
    internal static void SetItem(object self, object? key, object? value)
    {
        PyDict raw = PyDictLayout.RequireRaw(self);
        RequireScalarKey(key);
        // wrapper payload 的不变量由全部公开写入口维持；精确接收者需要冷扫描。
        if (self is not PyDictInstance) RequireScalarContents(raw);
        raw.Set(key, value);
    }
    internal static void DeleteItem(object self, object? key)
    {
        PyDict raw = PyDictLayout.RequireRaw(self);
        RequireScalarKey(key);
        if (self is not PyDictInstance) RequireScalarContents(raw);
        if (!raw.DeleteIfPresent(key ?? PyNone.Instance)) throw PyDictLayout.Own(PyKeyError.ForKey(key));
    }
    internal static bool Contains(object self, object? key)
    {
        PyDict raw = PyDictLayout.RequireRaw(self);
        RequireScalarKey(key);
        if (self is not PyDictInstance) RequireScalarContents(raw);
        return raw.Contains(key ?? PyNone.Instance);
    }
    internal static object CallUpdate(object receiver, object?[] args, string[] names, object?[] values, bool construct = false)
    {
        if (args.Length > 1)
            throw PyDictLayout.Own(new PyTypeError($"{(construct ? "dict" : "update")} expected at most 1 argument, got {args.Length}"));
        PyDict raw = PyDictLayout.RequireRaw(receiver);
        if (receiver is not PyDictInstance) RequireScalarContents(raw);
        if (args.Length == 1) PyOps.DictLayoutUpdateSource(raw, args[0]);
        if (receiver is not PyDictInstance) RequireScalarContents(raw);
        for (int i = 0; i < names.Length; i++) raw.Set(names[i], values[i]);
        return PyNone.Instance;
    }
    internal static object Compare(string slot, object self, object? other)
    {
        PyDict left = PyDictLayout.RequireRaw(self);
        PyDict? right = other is PyOrderedDict ordered ? ordered.Raw
            : PyDictLayout.TryGet(other, out PyDict payload) ? payload : null;
        if (right is null || slot is not ("__eq__" or "__ne__"))
            return PyNotImplemented.Instance;
        if (ReferenceEquals(left, right)) return slot == "__eq__";
        if (left.Count != right.Count) return slot == "__ne__";
        if (_comparisonDepth >= 256)
            throw PyDictLayout.Own(new PyRecursionError("maximum recursion depth exceeded in comparison"));
        _comparisonDepth++;
        try
        {
            RequireScalarContents(left); RequireScalarContents(right);
            long lv = left.Version, rv = right.Version;
            for (int i = 0; i < left.SlotCount; i++)
            {
                if (!left.TryKeyAt(i, out object? key)) continue;
                if (!right.TryGet(key, out object? value)) return slot == "__ne__";
                bool equal = PyListRootMethods.Equal(left.ValueAt(i), value);
                // value eq 可重入改表；本切不假装提供 CP 的完整重入重试算法。
                if (left.Version != lv || right.Version != rv)
                    throw PyDictLayout.Own(new PyNotImplementedError("dict comparison with structural mutation is not supported"));
                if (!equal) return slot == "__ne__";
            }
            return slot == "__eq__";
        }
        // 所有提前返回及用户eq/键域异常都归还深度；失败后新的比较不继承耗尽预算。
        finally { _comparisonDepth--; }
    }

    internal static object? Invoke(string name, object self, object?[] args)
    {
        switch (name)
        {
            case "__str__": return PyRepr.ReprObject(self);
            case "__repr__": return PyRepr.DictRaw(self, PyDictLayout.RequireRaw(self));
            case "__len__": return Len(self);
            case "__iter__": return Iter(self);
            case "__reversed__": return Reversed(self);
            case "__getitem__": return Item(self, args[0]);
            case "__setitem__": SetItem(self, args[0], args[1]); return PyNone.Instance;
            case "__delitem__": DeleteItem(self, args[0]); return PyNone.Instance;
            case "__contains__": return Contains(self, args[0]);
            case "__ior__":
                // 根签名已由描述符验证；OD union 暂缓必须在目标写入或来源枚举之前拒绝。
                if (args[0] is PyOrderedDict) throw PyOrderedDict.Unsupported("dict.__ior__ source");
                CallUpdate(self, args, [], []); return self;
            case "__or__": case "__ror__":
                if (args[0] is PyOrderedDict) throw PyOrderedDict.Unsupported("dict." + name + " operand");
                if (!PyDictLayout.TryGet(args[0], out PyDict other)) return PyNotImplemented.Instance;
                PyDict first = name == "__or__" ? PyDictLayout.RequireRaw(self) : other;
                PyDict second = name == "__or__" ? other : PyDictLayout.RequireRaw(self);
                RequireScalarContents(first); RequireScalarContents(second);
                PyDict result = first.Copy(); result.Update(second); return result;
            default: return Compare(name, self, args[0]);
        }
    }
}
