// OrderedDict 的活视图与独立游标：先判断是否仍有next-key，再检查版本，首次变异错误后永久结束。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;

internal sealed class PyOrderedDictIterator : PyNativeObject, IPyIterator
{
    private readonly PyOrderedDict _owner;
    private readonly PyDictViewKind _kind;
    private readonly bool _reverse;
    private readonly long _version;
    private PyOrderedDict.Node? _next;
    private object? _current;
    internal PyOrderedDictIterator(PyOrderedDict owner, PyDictViewKind kind, bool reverse = false)
    {
        _owner = owner; _kind = kind; _reverse = reverse;
        _version = owner.OrderVersion; _next = reverse ? owner.Last : owner.First;
    }
    public override string TypeName => "odict_iterator";
    public bool MoveNext()
    {
        // 初始空及刚yield最后一项都已经没有next-key；后来的修改不能使其重新活跃或报变异。
        if (_next is null) { _current = null; return false; }
        if (_owner.OrderVersion != _version)
        {
            _next = null; _current = null;
            throw PyOrderedDict.Error(new PyRuntimeError("OrderedDict mutated during iteration"));
        }
        PyOrderedDict.Node node = _next;
        _next = _reverse ? node.Previous : node.Next;
        _current = _kind switch
        {
            PyDictViewKind.Keys => node.Key,
            PyDictViewKind.Values => _owner.Raw.Get(node.Key),
            _ => new PyTuple([node.Key, _owner.Raw.Get(node.Key)]),
        };
        return true;
    }
    public object? Current() => _current;
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => PyType.Get(TypeName),
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => this),
        "__next__" => PyNativeFunctions.Create(name, [], [], 0, _ => MoveNext() ? Current() : throw new PyStopIteration()),
        _ => throw Missing(name),
    };
}

internal sealed class PyOrderedDictView(PyOrderedDict owner, PyDictViewKind kind) : PyNativeObject
{
    internal PyOrderedDict Owner => owner;
    internal PyDictViewKind Kind => kind;
    internal int Count => owner.Count;
    [ThreadStatic] private static HashSet<PyOrderedDictView>? _repr;
    public override string TypeName => kind switch
    { PyDictViewKind.Keys => "odict_keys", PyDictViewKind.Values => "odict_values", _ => "odict_items" };
    internal IPyIterator Iter(bool reverse = false) => new PyOrderedDictIterator(owner, kind, reverse);
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => PyType.Get(TypeName),
        "__len__" => Method(name, 0, _ => (long)Count),
        "__iter__" => Method(name, 0, _ => Iter()),
        "__reversed__" => Method(name, 0, _ => Iter(true)),
        "__contains__" => Method(name, 1, a => Contains(a[0])),
        "__eq__" => Method(name, 1, a => Compare(PyOps.Eq, a[0])),
        "__ne__" => Method(name, 1, a => Compare(PyOps.Ne, a[0])),
        "__hash__" when kind != PyDictViewKind.Values => PyNone.Instance,
        "mapping" or "isdisjoint" or "__and__" or "__or__" or "__sub__" or "__xor__"
            or "__rand__" or "__ror__" or "__rsub__" or "__rxor__" or "__lt__" or "__le__" or "__gt__" or "__ge__"
            => throw PyOrderedDict.Unsupported("view " + name),
        _ => throw Missing(name),
    };
    private static PyFunction Method(string name, int count, Func<object?[], object?> body) => count == 0
        ? PyNativeFunctions.Create(name, [], [], 0, body)
        : PyNativeFunctions.Create(name, ["value"], [null], 1, body, ["posonly"]);
    internal bool Contains(object? item)
    {
        if (kind == PyDictViewKind.Keys) return owner.Contains(item);
        if (kind == PyDictViewKind.Items)
        {
            if (item is not PyTuple pair || pair.Length != 2) return false;
            PyOrderedDict.RequireKey(pair.Items[0]);
            if (!owner.Raw.TryGet(pair.Items[0] ?? PyNone.Instance, out object? value)) return false;
            return EqualChecked(value, pair.Items[1]);
        }
        foreach (var (_, value) in owner.Entries()) if (EqualChecked(value, item)) return true;
        return false;
    }
    private bool EqualChecked(object? left, object? right)
    {
        long version = owner.OrderVersion;
        bool result = PyListRootMethods.Equal(left, right);
        if (owner.OrderVersion != version) throw PyOrderedDict.Unsupported("view comparison with structural mutation");
        return result;
    }
    internal object Compare(int op, object? other)
    {
        if (op is not (PyOps.Eq or PyOps.Ne)) throw PyOrderedDict.Unsupported("view ordering");
        if (kind == PyDictViewKind.Values)
        {
            // object根比较：只有同一视图直接成功，其他值须给对方反射槽机会。
            if (!ReferenceEquals(this, other)) return PyNotImplemented.Instance;
            return op == PyOps.Eq;
        }
        if (other is PySet or PyFrozenSet) throw PyOrderedDict.Unsupported("view comparison with sets");
        if (other is PyDictView) throw PyOrderedDict.Unsupported("view comparison with dict views");
        if (other is not PyOrderedDictView right) return PyNotImplemented.Instance;
        if (right.Kind == PyDictViewKind.Values) return PyNotImplemented.Instance;
        if (kind != right.Kind) throw PyOrderedDict.Unsupported("view comparison across kinds");
        bool equal = Count == right.Count;
        if (equal)
        {
            IPyIterator cursor = Iter();
            long lv = owner.OrderVersion, rv = right.Owner.OrderVersion;
            while (cursor.MoveNext())
            {
                bool found = right.Contains(cursor.Current());
                if (owner.OrderVersion != lv || right.Owner.OrderVersion != rv)
                    throw PyOrderedDict.Unsupported("view comparison with structural mutation");
                if (!found) { equal = false; break; }
            }
        }
        return op == PyOps.Eq ? equal : !equal;
    }
    public override string PythonRepr()
    {
        _repr ??= new();
        if (!_repr.Add(this)) return "...";
        try
        {
            var values = new List<string>(); IPyIterator cursor = Iter();
            while (cursor.MoveNext()) values.Add(PyRepr.Repr(cursor.Current()));
            return TypeName + "([" + string.Join(", ", values) + "])";
        }
        finally { _repr.Remove(this); }
    }
}
