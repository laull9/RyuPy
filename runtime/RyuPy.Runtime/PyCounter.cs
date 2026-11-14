using System;
using System.Collections.Generic;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>Counter 保留零/负计数与插入顺序；更新采用 Python 数值运算而非 CLR 整数截断。</summary>
public sealed class PyCounter : PyCollectionsMapping
{
    /// <inheritdoc />
    public override string TypeName => "Counter";
    /// <inheritdoc />
    protected override object MissingKey(object? key) => 0L;
    /// <inheritdoc />
    protected override PyCollectionsMapping Copy() { var result = new PyCounter(); result.UpdateFrom(Dictionary); return result; }
    /// <summary>来源是映射时累加值，普通可迭代来源逐项累加一；subtract 保留非正计数。</summary>
    internal void Accumulate(object? source, bool subtract)
    {
        if (source is null or PyNone || IsAbsent(source)) return;
        if (source is PyOrderedDict) throw PyOrderedDict.Unsupported("Counter update/subtract");
        if (source is PyMappingProxy proxy) { PyMappingProxyConsumers.Accumulate(Dictionary, proxy, subtract); return; }
        if (source is PyDictInstance derived) { PyMappingProxyConsumers.AccumulateSubclass(Dictionary, derived, subtract); return; }
        PyDict? mapping = source as PyDict ?? (source as IPyNativeMapping)?.Dictionary;
        if (mapping is not null)
        {
            // 空 Counter 的 update 是映射复制，允许任意值，不隐式计算 0 + value。
            if (!subtract && Dictionary.Count == 0) { Dictionary.Update(mapping); return; }
            foreach (var (key, value) in CheckedEntries(mapping))
            {
                object? old = Read(key);
                Dictionary.Set(key, subtract
                    ? PyOps.Binary(PyOps.Sub, old!, value!)
                    : PyOps.Binary(PyOps.Add, value!, old!));
            }
            return;
        }
        RejectCustomMapping(source, "items");
        IPyIterator iterator = PyIter.Create(source);
        while (iterator.MoveNext()) Add(iterator.Current(), 1L, subtract);
    }
    private void Add(object? key, object? count, bool subtract) => Dictionary.Set(key,
        PyOps.Binary(subtract ? PyOps.Sub : PyOps.Add, Read(key)!, count!));
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "update" or "subtract" => UpdateFunction(name, (source, keywords) => { Accumulate(source, name == "subtract"); Accumulate(keywords, name == "subtract"); }),
        "total" => Method(name, [], [], 0, _ => Total()),
        "most_common" => PyNativeFunctions.Create(name, ["n"], [PyNone.Instance], 0, a => MostCommon(a[0])),
        "elements" => Method(name, [], [], 0, _ => new Elements(this)),
        "__eq__" => Method(name, ["other"], [null], 1, a => Equal(a[0])),
        "__add__" or "__sub__" or "__or__" or "__and__" => Method(name, ["other"], [null], 1, a => Combine(a[0], name)),
        "__iadd__" or "__isub__" or "__ior__" or "__iand__" => Method(name, ["other"], [null], 1, a => InPlace(a[0], "__" + name[3..])),
        "__pos__" or "__neg__" => Method(name, [], [], 0, _ => Unary(name == "__neg__")),
        _ => base.GetAttr(name),
    };
    private object Total()
    {
        object total = 0L;
        foreach (var (_, count) in CheckedEntries(Dictionary)) total = PyOps.Binary(PyOps.Add, total, count!);
        return total;
    }
    private PyList MostCommon(object? amount)
    {
        long? count = amount is null or PyNone ? null : PyItertools.Index(amount);
        if (count <= 0) return new PyList([]);
        // 复用稳定 Python 排序，仅调用小于比较，不让 CLR Sort 包装用户异常。
        var rows = CheckedEntries(Dictionary).Select(entry => (object?)new PyTuple([entry.Key, entry.Value])).ToList();
        PySort.SortBy(rows, true, row => ((PyTuple)row!).Items[1]);
        return new PyList(rows.Take(count is null ? rows.Count : (int)Math.Min(count.Value, rows.Count)).ToArray());
    }
    private object Equal(object? other)
    {
        using var recursion = PyCollectionsComparison.Enter();
        if (other is not PyCounter counter) return base.GetAttr("__eq__") is object method ? PyOps.CallValue(method, [other], [], [])! : false;
        foreach (var (key, value) in CheckedEntries(Dictionary)) if (!PyHash.Equal(value, counter.Read(key))) return false;
        foreach (var (key, value) in CheckedEntries(counter.Dictionary)) if (!PyHash.Equal(value, Read(key))) return false;
        return true;
    }
    private object Combine(object? other, string operation)
    {
        if (other is not PyCounter right) return PyNotImplemented.Instance;
        var result = new PyCounter();
        foreach (var (key, value) in CheckedEntries(Dictionary))
        {
            object? b = right.Read(key);
            object? count = operation switch {
                "__add__" => PyOps.Binary(PyOps.Add, value!, b!),
                "__sub__" => PyOps.Binary(PyOps.Sub, value!, b!),
                "__or__" => PyOps.Compare(PyOps.Lt, value!, b!) ? b : value,
                _ => PyOps.Compare(PyOps.Lt, value!, b!) ? value : b,
            };
            if (PyOps.Compare(PyOps.Gt, count!, 0L)) result.Dictionary.Set(key, count);
        }
        foreach (var (key, value) in CheckedEntries(right.Dictionary))
        {
            if (Dictionary.Contains(key)) continue;
            object? count = operation switch {
                "__add__" or "__or__" => value,
                "__sub__" => PyOps.Binary(PyOps.Sub, 0L, value!),
                _ => 0L,
            };
            if (PyOps.Compare(PyOps.Gt, count!, 0L)) result.Dictionary.Set(key, count);
        }
        return result;
    }
    private object InPlace(object? other, string operation)
    {
        if (other is PyOrderedDict) throw PyOrderedDict.Unsupported("Counter inplace operators");
        if (other is PyDictInstance) throw PyDictLayout.Own(new PyNotImplementedError("Counter inplace operators with dict subclasses are not supported"));
        PyDict? mapping = other as PyDict ?? (other as IPyNativeMapping)?.Dictionary;
        if (mapping is null) return PyNotImplemented.Instance;
        // 每项立即写回；中途异常必须保留已经完成的更新，不能用临时结果冒充事务。
        if (operation == "__and__")
        {
            IPyIterator keys = PyIter.Create(Dictionary);
            while (keys.MoveNext())
            {
                object? key = keys.Current();
                object? right = other is PyCounter counter ? counter.Read(key) : mapping.Get(key);
                object? left = Read(key);
                if (PyOps.Compare(PyOps.Lt, right!, left!)) Dictionary.Set(key, right);
            }
        }
        else
        {
            foreach (var (key, value) in CheckedEntries(mapping))
            {
                object? left = Read(key);
                if (operation == "__or__" && !PyOps.Compare(PyOps.Gt, value!, left!)) continue;
                object? result = operation switch {
                    "__add__" => PyOps.Binary(PyOps.Add, left!, value!),
                    "__sub__" => PyOps.Binary(PyOps.Sub, left!, value!),
                    _ => value,
                };
                Dictionary.Set(key, result);
            }
        }
        foreach (object? key in Dictionary.Keys.ToArray())
            if (!PyOps.Compare(PyOps.Gt, Read(key)!, 0L)) Dictionary.Delete(key);
        return this;
    }
    private object Unary(bool negative)
    {
        var result = new PyCounter();
        foreach (var (key, count) in CheckedEntries(Dictionary))
        {
            object? value = negative ? PyOps.Binary(PyOps.Sub, 0L, count!) : count;
            if (PyOps.Compare(PyOps.Gt, value!, 0L)) result.Dictionary.Set(key, value);
        }
        return result;
    }
    /// <inheritdoc />
    public override string PythonRepr() => GuardRepr(() =>
    {
        if (Dictionary.Count == 0) return "Counter()";
        PyDict ordered;
        try { ordered = new PyDict(); ordered.Update(MostCommon(PyNone.Instance)); }
        catch (PyTypeError) { ordered = Dictionary; }
        return $"Counter({PyRepr.Repr(ordered)})";
    });
    private sealed class Elements : PyToolIterator
    {
        private PyDict? _source;
        private readonly long _version;
        private int _slot;
        private object? _key;
        private long _remaining;
        internal Elements(PyCounter counter) { _source = counter.Dictionary; _version = _source.Version; }
        public override string TypeName => "chain";
        public override bool MoveNext()
        {
            if (_source is null) return false;
            try
            {
                while (_remaining <= 0)
                {
                    if (_source.Version != _version) throw new PyRuntimeError("dictionary changed size during iteration");
                    bool found = false;
                    while (_slot < _source.SlotCount)
                    {
                        int slot = _slot++;
                        if (!_source.TryKeyAt(slot, out _key)) continue;
                        // 直接读取既有槽位，不重哈希键，也不为每个计数构造 Python tuple。
                        _remaining = PyItertools.Index(_source.ValueAt(slot));
                        found = true;
                        break;
                    }
                    if (!found) { Finish(); return false; }
                }
                _remaining--; return true;
            }
            catch (PyStopIteration) { Finish(); return false; }
            catch (PyBaseException) { Finish(); throw; }
        }
        private void Finish() { _source = null; _key = null; _remaining = 0; }
        public override object? Current() => _key;
    }
}
