// 有界 LRU 使用缓存哈希桶和双向链表；函数体不在锁内执行，用户比较重入后重新验证缓存版本。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>functools.lru_cache 的可调用、非数据描述符包装对象。</summary>
public sealed class PyLruCache : PyNativeObject, IPyNativeCallable, IPyNativeDescriptor
{
    private sealed class Entry
    {
        internal readonly PyLruKey Key;
        internal readonly object? Result;
        internal LinkedListNode<Entry>? Link;
        internal Entry(PyLruKey key, object? result) { Key = key; Result = result; }
    }
    private readonly object _gate = new();
    private readonly object? _function, _maximum, _parameterMaximum, _typedOption;
    private readonly long _capacity;
    private readonly bool _typed;
    private readonly Dictionary<long, List<Entry>> _buckets = new();
    private readonly LinkedList<Entry> _recent = new();
    private PyDict _attributes = new();
    private long _hits, _misses, _version;

    private PyLruCache(object? function, object? maximum, object? typed)
    {
        if (!PyFunctools.IsCallable(function)) throw new PyTypeError("the first argument must be callable");
        _function = function; _typedOption = typed; _typed = PyOps.Truthy(typed);
        if (maximum is null or PyNone) { _maximum = _parameterMaximum = PyNone.Instance; _capacity = long.MaxValue; }
        else
        {
            BigInteger count = PyInt.ToBigInteger(maximum);
            if (count > long.MaxValue) throw new PyOverflowError("Python int too large to convert to C ssize_t");
            _parameterMaximum = count < 0 ? 0L : maximum;
            if (count < 0) count = 0;
            _maximum = PyInt.Normalize(count); _capacity = (long)count;
        }
        _attributes.Set("cache_parameters", PyNativeFunctions.Create("cache_parameters", [], [], 0, _ => Parameters()));
        PyFunctools.UpdateWrapper(this, function);
    }
    internal static object Decorator(object? maximum, object? typed)
    {
        if (PyFunctools.IsCallable(maximum) && typed is bool boolean)
            return new PyLruCache(maximum, 128L, boolean);
        if (maximum is not (null or PyNone) && !PyInt.IsInt(maximum))
            throw new PyTypeError("Expected first argument to be an integer, a callable, or None");
        return PyNativeFunctions.Create("decorating_function", ["user_function"], [null], 1,
            a => new PyLruCache(a[0], maximum, typed));
    }
    /// <inheritdoc/>
    public override string TypeName => "_lru_cache_wrapper";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name != "__dict__" && _attributes.TryGet(name, out object? attribute)
        ? attribute : name switch
    {
        "__dict__" => _attributes,
        "__call__" => this,
        "cache_info" => PyNativeFunctions.Create(name, [], [], 0, _ => Info()),
        "cache_clear" => PyNativeFunctions.Create(name, [], [], 0, _ => { Clear(); return null; }),
        "cache_parameters" => PyNativeFunctions.Create(name, [], [], 0, _ => Parameters()),
        _ => _attributes.TryGet(name, out object? value) ? value : throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name == "__dict__") _attributes = value as PyDict ?? throw new PyTypeError("__dict__ must be a dictionary");
        else _attributes.Set(name, value);
    }
    /// <inheritdoc/>
    public override void DelAttr(string name)
    {
        if (name == "__dict__") throw new PyTypeError("cannot delete __dict__");
        if (!_attributes.TryGet(name, out _)) throw Missing(name);
        _attributes.Delete(name);
    }
    /// <inheritdoc/>
    public object? Bind(PyClass owner, object? self) => self is null ? this : new PyBoundMethod(owner, self, this);
    /// <inheritdoc/>
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        // maxsize=0 完全不建立键：不可哈希参数仍能调用，hash也没有额外副作用。
        if (_capacity == 0)
        {
            lock (_gate) _misses++;
            return PyOps.CallValue(_function, args, names, values);
        }
        var key = new PyLruKey(args, names, values, _typed);
        lock (_gate)
        {
            Entry? found = Find(key);
            if (found is not null) { _hits++; Touch(found); return found.Result; }
            _misses++;
        }
        object? result = PyOps.CallValue(_function, args, names, values);
        lock (_gate)
        {
            // 函数体可能递归调用同一个键；已有结果不被外层再次插入或覆盖。
            if (Find(key) is not null) return result;
            if (_recent.Count >= _capacity) Remove(_recent.First!.Value);
            var entry = new Entry(key, result);
            if (!_buckets.TryGetValue(key.Hash, out var bucket)) _buckets[key.Hash] = bucket = [];
            bucket.Add(entry); entry.Link = _recent.AddLast(entry); _version++;
        }
        return result;
    }
    private Entry? Find(PyLruKey key)
    {
        while (true)
        {
            long version = _version;
            if (!_buckets.TryGetValue(key.Hash, out var bucket)) return null;
            bool changed = false;
            for (int i = 0; i < bucket.Count; i++)
            {
                Entry candidate = bucket[i]; bool equal = candidate.Key.Equal(key);
                // __eq__ 可清缓存或递归插入；不能返回已经淘汰的条目或沿用被改动的桶下标。
                if (version != _version) { changed = true; break; }
                if (equal) return candidate;
            }
            if (!changed) return null;
        }
    }
    private void Touch(Entry entry)
    { _recent.Remove(entry.Link!); _recent.AddLast(entry.Link!); }
    private void Remove(Entry entry)
    {
        var bucket = _buckets[entry.Key.Hash]; bucket.Remove(entry);
        if (bucket.Count == 0) _buckets.Remove(entry.Key.Hash);
        _recent.Remove(entry.Link!); _version++;
    }
    private void Clear()
    {
        lock (_gate) { _buckets.Clear(); _recent.Clear(); _hits = _misses = 0; _version++; }
    }
    private object Info()
    { lock (_gate) return new PyLruInfo(_hits, _misses, _maximum, _recent.Count); }
    private PyDict Parameters()
    { var result = new PyDict(); result.Set("maxsize", _parameterMaximum); result.Set("typed", _typedOption); return result; }
}
