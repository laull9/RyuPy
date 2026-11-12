// dict 派生对象的公开协议分派；根描述符继续使用 PyDictProtocols，不重入用户覆盖。
namespace RyuPy.Runtime;
internal static class PyDictSubclassDispatch
{
    // CPython 的 dict 快复制取决于实际 iter 槽；显式恢复根槽应继续走原始存储。
    internal static bool HasCustomIterator(PyDictInstance value) => value.Class.TryLookup("__iter__", out _, out object? slot)
        && !ReferenceEquals(slot, PyDictDescriptors.Unbound("__iter__"));
    private static bool Invoke(PyDictInstance self, string name, object?[] args, out object? value)
    {
        self.Class.RequireDictReady();
        if (self.Class.HasDictC3Layout)
        {
            if (self.Class.TryResolveDictC3(name, out var resolved))
            { value = resolved.Invoke(self, args, [], []); return true; }
            value = null; return false;
        }
        if (self.Class.TryLookup(name, out PyClass owner, out object? method))
        { value = PyObject.InvokeBound(method, owner, self, args, [], []); return true; }
        value = null; return false;
    }
    internal static object? Item(PyDictInstance self, object? key) => self.Class.HasDictC3Layout
        && Invoke(self, "__getitem__", [key], out object? value) ? value : PyDictProtocols.Item(self, key);
    internal static void SetItem(PyDictInstance self, object? key, object? value)
    { if (!Invoke(self, "__setitem__", [key, value], out _)) PyDictProtocols.SetItem(self, key, value); }
    internal static void DeleteItem(PyDictInstance self, object? key)
    { if (!Invoke(self, "__delitem__", [key], out _)) PyDictProtocols.DeleteItem(self, key); }
    internal static IPyIterator Iter(PyDictInstance self)
    {
        if (!Invoke(self, "__iter__", [], out object? value)) return PyDictProtocols.Iter(self);
        if (value is IPyIterator iterator) return iterator;
        if (value is PyInstance user && user.Class.TryLookup("__next__", out _, out _)) return new PyInstanceIterator(user);
        throw new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(value)}'");
    }
    internal static object? Reversed(PyDictInstance self) => Invoke(self, "__reversed__", [], out object? value)
        ? value : PyDictProtocols.Reversed(self);
    // 严格子类仅在反射槽不同于左类时抢先；NI 不得再回退到忽略覆盖的 payload 合并。
    internal static object Union(object left, object right, bool inplace = false)
    {
        if (inplace && left is PyDictInstance instance)
        { object? changed = Candidate(instance, "__ior__", right); if (changed is not PyNotImplemented) return changed!; }
        bool same = left is PyDictInstance a && right is PyDictInstance b && ReferenceEquals(a.Class, b.Class);
        bool rightFirst = right is PyDictInstance derived
            && (left is PyDict || left is PyDictInstance parent && !same && derived.Class.IsSubclassOf(parent.Class))
            && !ReferenceEquals(Slot(right, "__ror__"), Slot(left, "__ror__"));
        if (rightFirst)
        { object? result = Candidate(right, "__ror__", left); if (result is not PyNotImplemented) return result!; }
        object? forward = Candidate(left, "__or__", right);
        if (forward is not PyNotImplemented) return forward!;
        if (!rightFirst && !same)
        { object? reflected = Candidate(right, "__ror__", left); if (reflected is not PyNotImplemented) return reflected!; }
        throw new PyTypeError($"unsupported operand type(s) for {(inplace ? "|=" : "|")}: '{PyRepr.TypeName(left)}' and '{PyRepr.TypeName(right)}'");
    }
    private static object? Slot(object receiver, string name) => receiver is PyDictInstance instance
        && instance.Class.TryLookup(name, out _, out object? slot) ? slot
        : receiver is PyDict or PyDictInstance ? PyDictDescriptors.Unbound(name) : null;
    private static object? Candidate(object self, string name, object other)
    {
        if (self is PyDictInstance instance)
            return Invoke(instance, name, [other], out object? value) ? value : PyDictProtocols.Invoke(name, self, [other]);
        if (self is PyDict) return PyDictProtocols.Invoke(name, self, [other]);
        return PyObject.TryMethod1(self, name, other, out object? result) ? result : PyNotImplemented.Instance;
    }
}
