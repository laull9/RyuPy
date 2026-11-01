using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>deque 用环形数组保持两端均摊 O(1)；有界淘汰立即清除存储引用。</summary>
public sealed class PyDeque : PyNativeObject
{
    private object?[] _items = new object?[8];
    private int _head;
    private int _count;
    private long _version;
    private readonly long? _maxlen;
    [ThreadStatic] private static HashSet<PyDeque>? _repr;
    internal PyDeque(object? source, object? maxlen)
    {
        _maxlen = maxlen is null or PyNone ? null : PyItertools.Index(maxlen);
        if (_maxlen < 0) throw new PyValueError("maxlen must be non-negative");
        Extend(source, false);
    }
    /// <inheritdoc />
    public override string TypeName => "deque";
    /// <summary>逻辑元素数量。</summary>
    public int Count => _count;
    private int Slot(int index) => (_head + index) % _items.Length;
    private object? At(int index) => _items[Slot(index)];
    private PyFunction Method(string name, string[] parameters, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, defaults, required, body, Array.ConvertAll(parameters, _ => "posonly"));
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__hash__" => PyNone.Instance,
        "maxlen" => _maxlen is long bound ? bound : PyNone.Instance,
        "__len__" => Method(name, [], [], 0, _ => (long)_count),
        "__iter__" => Method(name, [], [], 0, _ => new Cursor(this, false)),
        "__reversed__" => Method(name, [], [], 0, _ => new Cursor(this, true)),
        "__getitem__" => Method(name, ["index"], [null], 1, a => At(Index(a[0]))),
        "__setitem__" => Method(name, ["index", "value"], [null, null], 2, a => { _items[Slot(Index(a[0]))] = a[1]; return null; }),
        "__delitem__" => Method(name, ["index"], [null], 1, a => { Delete(Index(a[0])); return null; }),
        "append" => Method(name, ["value"], [null], 1, a => { Append(a[0], false); return null; }),
        "appendleft" => Method(name, ["value"], [null], 1, a => { Append(a[0], true); return null; }),
        "pop" => Method(name, [], [], 0, _ => Pop(false)),
        "popleft" => Method(name, [], [], 0, _ => Pop(true)),
        "extend" => Method(name, ["iterable"], [null], 1, a => { Extend(a[0], false); return null; }),
        "extendleft" => Method(name, ["iterable"], [null], 1, a => { Extend(a[0], true); return null; }),
        "clear" => Method(name, [], [], 0, _ => { Array.Clear(_items); if (_count != 0) _version++; _count = 0; _head = 0; return null; }),
        "rotate" => Method(name, ["n"], [1L], 0, a => { Rotate(PyItertools.Index(a[0])); return null; }),
        "reverse" => Method(name, [], [], 0, _ => { for (int i = 0; i < _count / 2; i++) Swap(i, _count - 1 - i); return null; }),
        "copy" or "__copy__" => Method(name, [], [], 0, _ => new PyDeque(this, _maxlen is long n ? n : PyNone.Instance)),
        "index" => Method(name, ["value", "start", "stop"], [null, 0L, long.MaxValue], 1, a => (long)FindIndex(a[0], PyItertools.Index(a[1]), PyItertools.Index(a[2]))),
        "insert" => Method(name, ["index", "value"], [null, null], 2, a => { Insert(PyItertools.Index(a[0]), a[1]); return null; }),
        "count" => Method(name, ["value"], [null], 1, a => (long)Find(a[0], false)),
        "__contains__" => Method(name, ["value"], [null], 1, a => Find(a[0], true) >= 0),
        "remove" => Method(name, ["value"], [null], 1, a => { int index = Find(a[0], true); if (index < 0) throw new PyValueError("deque.remove(x): x not in deque"); Delete(index); return null; }),
        "__eq__" => Method(name, ["other"], [null], 1, a => Equal(a[0])),
        _ => throw Missing(name),
    };
    private int Index(object? value)
    {
        long index = PyItertools.Index(value);
        if (index < 0) index += _count;
        if (index < 0 || index >= _count) throw new PyIndexError("deque index out of range");
        return (int)index;
    }
    private void Grow()
    {
        if (_count < _items.Length) return;
        if (_items.Length > Array.MaxLength / 2) throw new PyOverflowError("deque capacity exceeds runtime limit");
        var next = new object?[_items.Length * 2];
        for (int i = 0; i < _count; i++) next[i] = At(i);
        _items = next; _head = 0;
    }
    private void Append(object? value, bool left)
    {
        _version++;
        if (_maxlen == 0) return;
        if (_maxlen is long bound && _count == bound) Pop(!left);
        Grow();
        if (left) _head = (_head + _items.Length - 1) % _items.Length;
        _items[Slot(left ? 0 : _count)] = value;
        _count++;
    }
    private object? Pop(bool left)
    {
        if (_count == 0) throw new PyIndexError("pop from an empty deque");
        int slot = Slot(left ? 0 : _count - 1);
        object? result = _items[slot]; _items[slot] = null;
        if (left) _head = (_head + 1) % _items.Length;
        _count--; _version++;
        return result;
    }
    private void Extend(object? source, bool left)
    {
        // 自身扩展只读开始时的元素，否则会因自己写入失效或无限循环。
        if (ReferenceEquals(source, this)) source = new PyTuple(Snapshot());
        IPyIterator iterator = PyIter.Create(source);
        while (iterator.MoveNext())
            if (_maxlen != 0) Append(iterator.Current(), left);
    }
    private object?[] Snapshot()
    {
        var result = new object?[_count];
        for (int i = 0; i < _count; i++) result[i] = At(i);
        return result;
    }
    private void Rotate(long count)
    {
        if (_count <= 1) return;
        _version++;
        count %= _count;
        if (count > _count / 2) count -= _count;
        if (count < -_count / 2) count += _count;
        while (count > 0) { object? value = Pop(false); Append(value, true); count--; }
        while (count < 0) { object? value = Pop(true); Append(value, false); count++; }
    }
    private void Swap(int left, int right) => (_items[Slot(left)], _items[Slot(right)]) = (_items[Slot(right)], _items[Slot(left)]);
    private void Delete(int index)
    {
        for (int i = index; i < _count - 1; i++) _items[Slot(i)] = At(i + 1);
        Pop(false);
    }
    private int Find(object? value, bool first)
    {
        long version = _version;
        int count = 0;
        for (int i = 0; i < _count; i++)
        {
            bool equal = ReferenceEquals(At(i), value) || PyHash.Equal(At(i), value);
            if (_version != version) throw new PyRuntimeError("deque mutated during iteration");
            if (equal) { if (first) return i; count++; }
        }
        return first ? -1 : count;
    }
    private int FindIndex(object? value, long start, long stop)
    {
        if (start < 0) start += _count;
        if (stop < 0) stop += _count;
        start = Math.Clamp(start, 0, _count); stop = Math.Clamp(stop, 0, _count);
        long version = _version;
        for (int i = (int)start; i < stop; i++)
        {
            bool equal = ReferenceEquals(At(i), value) || PyHash.Equal(At(i), value);
            if (_version != version) throw new PyRuntimeError("deque mutated during iteration");
            if (equal) return i;
        }
        throw new PyValueError("value is not in deque");
    }
    private void Insert(long index, object? value)
    {
        if (_maxlen is long bound && _count >= bound) throw new PyIndexError("deque already at its maximum size");
        if (index < 0) index += _count;
        index = Math.Clamp(index, 0, _count);
        Grow();
        for (int i = _count; i > index; i--) _items[Slot(i)] = At(i - 1);
        _items[Slot((int)index)] = value; _count++; _version++;
    }
    private object Equal(object? other)
    {
        using var recursion = PyCollectionsComparison.Enter();
        if (other is not PyDeque deque) return PyNotImplemented.Instance;
        if (_count != deque._count) return false;
        long first = _version, second = deque._version;
        for (int i = 0; i < _count; i++)
        {
            bool equal = ReferenceEquals(At(i), deque.At(i)) || PyHash.Equal(At(i), deque.At(i));
            if (_version != first || deque._version != second) throw new PyRuntimeError("deque mutated during iteration");
            if (!equal) return false;
        }
        return true;
    }
    /// <inheritdoc />
    public override string PythonRepr()
    {
        _repr ??= new();
        if (!_repr.Add(this)) return "[...]";
        try { return "deque(" + PyRepr.Repr(new PyList(Snapshot())) + (_maxlen is long n ? $", maxlen={n})" : ")"); }
        finally { _repr.Remove(this); }
    }
    private sealed class Cursor : PyToolIterator
    {
        private readonly PyDeque _deque;
        private readonly long _version;
        private readonly bool _reverse;
        private int _position;
        private object? _current;
        internal Cursor(PyDeque deque, bool reverse) { _deque = deque; _version = deque._version; _reverse = reverse; }
        public override string TypeName => _reverse ? "_deque_reverse_iterator" : "_deque_iterator";
        public override bool MoveNext()
        {
            if (_deque._version != _version) throw new PyRuntimeError("deque mutated during iteration");
            if (_position >= _deque._count) { _current = null; return false; }
            _current = _deque.At(_reverse ? _deque._count - 1 - _position : _position); _position++; return true;
        }
        public override object? Current() => _current;
    }
}
