using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>itertools 的有限惰性工具与构造签名，不预先物化输入。</summary>
public static class PyItertools
{
    private static readonly object Omitted = new();
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>模块导出与类型身份只建立一次。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    private static Dictionary<string, object?> Build()
    {
        var chain = Type("chain", PyNativeFunctions.CreateVariadic("chain", args =>
            new PyChainIterator(PyIter.Create(new PyTuple(args)))));
        chain.SetAttr("from_iterable", PyNativeFunctions.Create("from_iterable", ["iterable"], [null], 1,
            args => new PyChainIterator(PyIter.Create(args[0])), ["posonly"]));
        var repeat = Type("repeat", PyNativeFunctions.Create("repeat", ["object", "times"], [null, Omitted], 1,
            args => new PyRepeatIterator(args[0], ReferenceEquals(args[1], Omitted) ? null : Index(args[1]))));
        var islice = Type("islice", PyNativeFunctions.CreateVariadic("islice", Slice));
        return new(StringComparer.Ordinal)
        {
            ["chain"] = chain, ["repeat"] = repeat, ["islice"] = islice, ["zip_longest"] = PyZipLongest.Type,
            ["count"] = PyCount.Type, ["starmap"] = PyStarMap.Type,
        };
    }
    private static PyNativeType Type(string name, PyFunction factory) => new(name, "itertools",
        (args, keys, values) => PyOps.CallValue(factory, args, keys, values),
        value => value is PyToolIterator iterator && iterator.TypeName == name);
    private static object Slice(object?[] args)
    {
        if (args.Length is < 2 or > 4) throw new PyTypeError("islice expected 2 to 4 arguments");
        long start = args.Length == 2 ? 0 : SliceIndex(args[1], 0);
        object? stopValue = args.Length == 2 ? args[1] : args[2];
        long? stop = stopValue is null or PyNone ? null : SliceIndex(stopValue, 0);
        long step = args.Length == 4 ? SliceIndex(args[3], 1) : 1;
        if (step == 0) throw new PyValueError("Step for islice() must be a positive integer or None.");
        return new PySliceIterator(PyIter.Create(args[0]), start, stop, step);
    }
    private static long SliceIndex(object? value, long fallback)
    {
        if (value is null or PyNone) return fallback;
        long index;
        try { index = Index(value); }
        catch (PyTypeError) { throw new PyValueError("Indices for islice() must be None or an integer"); }
        catch (PyOverflowError) { throw new PyValueError("Indices for islice() exceed sys.maxsize"); }
        if (index < 0) throw new PyValueError("Indices for islice() must be non-negative");
        return index;
    }
    /// <summary>严格整数索引协议；不接受浮点或仅有 __int__ 的对象。</summary>
    internal static long Index(object? value)
    {
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value) || !PyInt.IsInt(value))
                throw new PyTypeError("object cannot be interpreted as an integer");
        }
        if (value is BigInteger big)
        {
            if (big < long.MinValue || big > long.MaxValue) throw new PyOverflowError("Python int too large to convert to C ssize_t");
            return (long)big;
        }
        return PyCast.ToInt64(value);
    }
}

/// <summary>原生惰性迭代器共享 Python 入口；Current 与耗尽状态分离。</summary>
public abstract class PyToolIterator : PyNativeObject, IPyIterator
{
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => this),
        "__next__" => PyNativeFunctions.Create(name, [], [], 0, _ => MoveNext() ? Current() : throw new PyStopIteration()),
        _ => throw Missing(name),
    };
    /// <summary>前进一次，不吞掉 StopIteration 之外的 Python 异常。</summary>
    public abstract bool MoveNext();
    /// <summary>读取最近一次成功推进的元素。</summary>
    public abstract object? Current();
}

/// <summary>chain 保留外层与当前内层游标；只在需要下一项时创建子迭代器。</summary>
public sealed class PyChainIterator : PyToolIterator
{
    private IPyIterator? _outer;
    private IPyIterator? _inner;
    private object? _current;
    internal PyChainIterator(IPyIterator outer) => _outer = outer;
    /// <inheritdoc />
    public override string TypeName => "chain";
    /// <inheritdoc />
    public override bool MoveNext()
    {
        while (_outer is not null)
        {
            if (_inner is not null && _inner.MoveNext()) { _current = _inner.Current(); return true; }
            _inner = null;
            try
            {
                if (!_outer.MoveNext()) { _outer = null; break; }
                _inner = PyIter.Create(_outer.Current());
            }
            catch (PyBaseException)
            {
                // 外层失败或子迭代器构造失败会永久关闭 chain；已有内层的 next 异常
                // 则留在上方传播并保留内层，下一次 next 仍可继续。
                _outer = null; _current = null;
                throw;
            }
        }
        _current = null;
        return false;
    }
    /// <inheritdoc />
    public override object? Current() => _current;
}

/// <summary>repeat 不复制对象；计数为 null 表示省略次数产生无限迭代。</summary>
public sealed class PyRepeatIterator : PyToolIterator
{
    private readonly object? _value;
    private long? _remaining;
    internal PyRepeatIterator(object? value, long? remaining) { _value = value; _remaining = remaining is long n ? Math.Max(0, n) : null; }
    /// <inheritdoc />
    public override string TypeName => "repeat";
    /// <inheritdoc />
    public override bool MoveNext()
    {
        if (_remaining is null) return true;
        if (_remaining <= 0) return false;
        _remaining--;
        return true;
    }
    /// <inheritdoc />
    public override object? Current() => _value;
    /// <inheritdoc />
    public override string PythonRepr() => _remaining is long n ? $"repeat({PyRepr.Repr(_value)}, {n})" : $"repeat({PyRepr.Repr(_value)})";
}

/// <summary>islice 精确推进到 max(start, stop)，步长不会多消费边界外元素。</summary>
public sealed class PySliceIterator : PyToolIterator
{
    private IPyIterator? _source;
    private long _consumed;
    private long _next;
    private readonly long? _stop;
    private readonly long _step;
    private object? _current;
    internal PySliceIterator(IPyIterator source, long start, long? stop, long step)
    { _source = source; _next = start; _stop = stop; _step = step; }
    /// <inheritdoc />
    public override string TypeName => "islice";
    /// <inheritdoc />
    public override bool MoveNext()
    {
        if (_source is null) return false;
        long target = _stop is long stop && _consumed > 0 ? Math.Min(_next, stop) : _next;
        while (_consumed < target)
        {
            if (!_source.MoveNext()) return Finish();
            _consumed++;
        }
        if (_stop is long end && _consumed >= end) return Finish();
        if (!_source.MoveNext()) return Finish();
        _current = _source.Current();
        _consumed++;
        _next = _next > long.MaxValue - _step ? long.MaxValue : _next + _step;
        return true;
    }
    private bool Finish() { _source = null; _current = null; return false; }
    /// <inheritdoc />
    public override object? Current() => _current;
}
