using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>range</c> 对象：惰性、可重复迭代。
/// </summary>
/// <remarks>
/// 编译产物里的 <c>for i in range(...)</c> 会被改写成计数循环，不经过这里；
/// 本类只服务 `range(...)` 作为**值**出现的场合（`list(range(5))`、传给函数、`reversed` 等）。
/// </remarks>
public sealed class PyRange
{
    /// <summary>起点（包含）。</summary>
    public long Start { get; }
    /// <summary>终点（不包含）。</summary>
    public long Stop { get; }
    /// <summary>步长，不能为 0。</summary>
    public long Step { get; }

    /// <summary>构造 range；步长为 0 抛 <c>ValueError</c>。</summary>
    public PyRange(long start, long stop, long step)
    {
        if (step == 0)
        {
            throw new PyValueError("range() arg 3 must not be zero");
        }
        Start = start;
        Stop = stop;
        Step = step;
    }

    /// <summary>元素个数（与 CPython 的公式一致）。</summary>
    public long Length
    {
        get
        {
            long span = Step > 0 ? Stop - Start : Start - Stop;
            if (span <= 0)
            {
                return 0;
            }
            long step = Math.Abs(Step);
            return (span + step - 1) / step;
        }
    }

    /// <summary>与 CPython 一致的 `range(...)` 形式。</summary>
    public override string ToString() =>
        Step == 1 ? $"range({Start}, {Stop})" : $"range({Start}, {Stop}, {Step})";
}

/// <summary>`range` 的迭代器。</summary>
public sealed class PyRangeIter : IPyIterator
{
    private readonly PyRange _range;
    private long _current;
    private bool _started;
    private bool _done;

    /// <summary>为 range 建立迭代器。</summary>
    public PyRangeIter(PyRange range) => _range = range;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_done)
        {
            return false;
        }
        if (!_started)
        {
            _started = true;
            _current = _range.Start;
        }
        else
        {
            _current += _range.Step;
        }
        bool inRange = _range.Step > 0 ? _current < _range.Stop : _current > _range.Stop;
        if (!inRange)
        {
            _done = true;
        }
        return inRange;
    }

    /// <inheritdoc/>
    public object? Current() => _current;
}

/// <summary>`enumerate(iterable, start)` 的迭代器。</summary>
public sealed class PyEnumerateIter : IPyIterator
{
    private readonly IPyIterator _inner;
    private long _index;

    /// <summary>包装一个迭代器并记录起始下标。</summary>
    public PyEnumerateIter(IPyIterator inner, long start)
    {
        _inner = inner;
        _index = start;
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (!_inner.MoveNext())
        {
            return false;
        }
        _index++;
        return true;
    }

    /// <inheritdoc/>
    public object? Current() => new PyTuple([_index - 1, _inner.Current()]);
}

/// <summary>`zip(...)` 的迭代器：任一输入耗尽即结束。</summary>
public sealed class PyZipIter : IPyIterator
{
    private readonly List<IPyIterator> _inputs;
    private object?[] _current = [];
    private bool _done;

    /// <summary>用各输入的迭代器构造 zip。</summary>
    public PyZipIter(List<IPyIterator> inputs) => _inputs = inputs;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        // 任一输入耗尽后迭代器永久结束；`zip()` 没有输入时同样立即结束
        if (_done || _inputs.Count == 0)
        {
            _done = true;
            return false;
        }
        var values = new object?[_inputs.Count];
        foreach (IPyIterator input in _inputs)
        {
            if (!input.MoveNext())
            {
                _done = true;
                return false;
            }
        }
        for (int i = 0; i < _inputs.Count; i++)
        {
            values[i] = _inputs[i].Current();
        }
        _current = values;
        return true;
    }

    /// <inheritdoc/>
    public object? Current() => new PyTuple(_current);
}

/// <summary>`reversed(seq)` 的迭代器：先物化序列，再从后往前。</summary>
public sealed class PyReversedIter : IPyIterator
{
    private readonly object?[] _items;
    private int _index;

    /// <summary>用已经物化的元素构造反向迭代器。</summary>
    public PyReversedIter(object?[] items)
    {
        _items = items;
        _index = items.Length;
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_index <= 0)
        {
            return false;
        }
        _index--;
        return true;
    }

    /// <inheritdoc/>
    public object? Current() => _items[_index];
}

/// <summary>迭代相关的运行时工具。</summary>
public static partial class PyOps
{
    /// <summary>把迭代器剩余元素全部取出（`list(it)` 等容器构造共用）。</summary>
    internal static object?[] DrainIterator(IPyIterator iterator)
    {
        var items = new List<object?>();
        while (iterator.MoveNext())
        {
            items.Add(iterator.Current());
        }
        return [.. items];
    }

    /// <summary>任意可迭代值的迭代器；不可迭代时抛 <c>TypeError</c>。</summary>
    internal static IPyIterator IteratorOf(object? value) => PyIter.Create(value);
}
