using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// 通用迭代器：<c>for x in obj</c> 的运行时候选。
/// </summary>
/// <remarks>
/// # 为什么不用 .NET 的 IEnumerator
///
/// Python 的迭代协议要处理「迭代期间修改容器」这类语义：字典在迭代中增删必须抛
/// <c>RuntimeError</c>，列表则允许追加（新元素会在本轮看到）。.NET 的 IEnumerator
/// 会直接失效并抛 <c>InvalidOperationException</c>，语义与错误类型都不对。
///
/// 因此这里自己维护游标：
/// <list type="bullet">
/// <item>列表 / 字符串 / 字节按下标推进（能自然看到追加的元素）；</item>
/// <item>字典与视图直接在槽位数组上推进，并带版本号（增删即报错）；</item>
/// <item>集合用快照（CPython 对集合的迭代期修改是未定义行为，这里选择确定性快照，
/// 已在 semantics.md 登记）。</item>
/// </list>
///
/// 编译产物用两个入口推进迭代：<see cref="PyOps.IterNextOk"/> 判断是否还有元素，
/// <see cref="PyOps.IterValue"/> 取当前元素。分开两步是为了让 MIR 用普通条件分支
/// 表达循环，不需要为 <c>StopIteration</c> 建立异常区域。
/// </remarks>
/// <summary>
/// 迭代器的最小协议：推进与取当前值。
/// </summary>
/// <remarks>
/// 编译产物只通过 <c>PyOps.IterNextOk</c> / <c>PyOps.IterValue</c> 接触迭代器，
/// 因此 enumerate / zip / range 等运行时迭代器只要实现这个接口即可参与 <c>for</c>。
/// </remarks>
public interface IPyIterator
{
    /// <summary>推进一个位置；返回是否还有元素。</summary>
    bool MoveNext();

    /// <summary>取当前元素（<see cref="MoveNext"/> 返回 <c>true</c> 之后调用）。</summary>
    object? Current();
}

/// <summary>
/// 编译产物使用的通用迭代器：按容器类型选择游标语义（列表按下标、字典按槽位、集合用快照）。
/// </summary>
public sealed class PyIter : IPyIterator
{
    private readonly object _source;
    private readonly long _version;
    /// <summary>集合 / frozenset 的确定快照；其它来源为 <c>null</c>。</summary>
    private readonly object?[]? _snapshot;
    /// <summary>字典视图的种类；直接迭代字典时为 <c>null</c>（等价 keys）。</summary>
    private readonly PyDictViewKind? _viewKind;
    /// <summary>列表 / 字节的逻辑下标，或字典 / 视图的**槽位**下标。</summary>
    private long _cursor;
    /// <summary>字典 / 视图当前槽位。</summary>
    private int _currentSlot;
    /// <summary>当前元素（字符串 / 字节在推进时已算好，避免重复换算码点偏移）。</summary>
    private object? _current;
    /// <summary>字符串的字符偏移游标（码点可能占两个 code unit）。</summary>
    private int _charOffset;

    private PyIter(
        object source,
        long version,
        object?[]? snapshot,
        PyDictViewKind? viewKind
    )
    {
        _source = source;
        _version = version;
        _snapshot = snapshot;
        _viewKind = viewKind;
    }

    /// <summary>为任意可迭代对象建立迭代器。</summary>
    /// <param name="source">可迭代对象。</param>
    public static IPyIterator Create(object? source) => source switch
    {
        null or PyNone => throw NotIterable(source),
        PyList list => new PyIter(list, 0, null, null),
        PyTuple tuple => new PyIter(tuple, 0, null, null),
        PyDict dict => new PyIter(dict, dict.Version, null, null),
        // 视图不复制：直接在字典的槽位上迭代，版本校验保证增删会报错
        PyDictView view => new PyIter(view.Dict, view.Dict.Version, null, view.Kind),
        PySet set => new PyIter(set, 0, [.. set.Items], null),
        PyFrozenSet frozen => new PyIter(frozen, 0, [.. frozen.Items], null),
        string text => new PyIter(text, 0, null, null),
        PyBytes bytes => new PyIter(bytes, 0, null, null),
        PyByteArray array => new PyIter(array, 0, null, null),
        PyRange range => new PyRangeIter(range),
        PyClass cls when PyEnum.IsEnumClass(cls) => PyEnum.Iterate(cls),
        // 原生流的__iter__仍需检查关闭状态，不能被IPyIterator快路径绕过。
        PyNativeObject native => CreateFromObject(native),
        // 迭代器本身可迭代：`iter(it)` 返回自身
        IPyIterator iterator => iterator,
        // 普通实例：调用一次 `__iter__`，验证返回对象的迭代器协议。
        PyInstance instance => CreateFromObject(instance),
        _ => throw NotIterable(source),
    };

    /// <summary>推进一个位置；返回是否还有元素。</summary>
    public bool MoveNext()
    {
        switch (_source)
        {
            case PyDict dict:
                CheckVersion(dict.Version);
                while (_cursor < dict.SlotCount)
                {
                    int slot = (int)_cursor++;
                    if (dict.TryKeyAt(slot, out _))
                    {
                        _currentSlot = slot;
                        return true;
                    }
                }
                return false;
            case string text:
                if (_charOffset >= text.Length)
                {
                    return false;
                }
                _current = PyText.ReadAt(text, ref _charOffset);
                return true;
            case PyBytes bytes:
                if (_cursor >= bytes.Count)
                {
                    return false;
                }
                _current = (long)bytes.Bytes[(int)_cursor];
                _cursor++;
                return true;
            case PyByteArray array:
                if (_cursor >= array.Count)
                {
                    return false;
                }
                _current = (long)array.Bytes[(int)_cursor];
                _cursor++;
                return true;
            default:
                {
                    long length = Length();
                    if (_cursor >= length)
                    {
                        return false;
                    }
                    _cursor++;
                    return true;
                }
        }
    }

    /// <summary>取当前元素（<see cref="MoveNext"/> 返回 <c>true</c> 之后调用）。</summary>
    public object? Current()
    {
        switch (_source)
        {
            case PyDict dict:
                {
                    // 直接迭代字典等价于 keys()；视图则按 _viewKind 取对应分量
                    CheckVersion(dict.Version);
                    object? key = KeyOf(dict, _currentSlot);
                    if (_viewKind is null)
                    {
                        return key;
                    }
                    return _viewKind switch
                    {
                        PyDictViewKind.Keys => key,
                        PyDictViewKind.Values => dict.ValueAt(_currentSlot),
                        _ => new PyTuple([key, dict.ValueAt(_currentSlot)]),
                    };
                }
            case PyList list:
                return list.Items[(int)_cursor - 1];
            case PyTuple tuple:
                return tuple.Items[(int)_cursor - 1];
            case PySet or PyFrozenSet:
                return _snapshot![(int)_cursor - 1];
            case string or PyBytes or PyByteArray:
                return _current;
            default:
                throw NotIterable(_source);
        }
    }

    private object? KeyOf(PyDict dict, int slot) =>
        dict.TryKeyAt(slot, out object? key) ? key : throw NotIterable(dict);

    private long Length() => _source switch
    {
        PyList list => list.Count,
        PyTuple tuple => tuple.Length,
        // 集合按快照迭代：长度必须取快照长度。用集合的实时 Count 会在
        // 「迭代中新增元素」时越界（快照没有新元素），破坏确定性快照的承诺。
        PySet or PyFrozenSet => _snapshot!.Length,
        string text => PyText.Length(text),
        PyBytes bytes => bytes.Count,
        PyByteArray array => array.Count,
        _ => _snapshot?.Length ?? throw NotIterable(_source),
    };

    private void CheckVersion(long version)
    {
        if (version != _version)
        {
            throw new PyRuntimeError("dictionary changed size during iteration");
        }
    }

    private static PyTypeError NotIterable(object? value) =>
        new($"'{PyRepr.TypeName(value)}' object is not iterable");
    private static IPyIterator CreateFromObject(object instance)
    {
        if (!PyObject.TryMethod(instance, "__iter__", [], out object? iterator))
        {
            throw NotIterable(instance);
        }
        // __iter__ 的结果必须已经是迭代器；不能再对结果调用 __iter__，
        // 否则会重复用户副作用，甚至把返回列表的非法实现误当成合法。
        if (iterator is IPyIterator ready)
        {
            return ready;
        }
        if (iterator is PyInstance next && next.Class.TryLookup("__next__", out _, out _))
        {
            return new PyInstanceIterator(next);
        }
        throw new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(iterator)}'");
    }

}


/// <summary>用户对象的 `__next__` 迭代器适配（不再次调用 `__iter__`）。</summary>
public sealed class PyInstanceIterator : IPyIterator
{
    private readonly PyInstance _instance;

    // yield from 必须直接使用真实 __next__，保留适配器 MoveNext 会吞掉的终值。
    internal PyInstance Instance => _instance;
    private object? _current;
    private bool _finished;

    /// <summary>包装一个定义了 `__next__` 的实例。</summary>
    public PyInstanceIterator(PyInstance instance) => _instance = instance;

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_finished)
        {
            return false;
        }
        try
        {
            if (!PyObject.TryMethod(_instance, "__next__", [], out object? value))
            {
                throw new PyTypeError(
                    $"'{_instance.Class.Name}' object is not an iterator"
                );
            }
            _current = value;
            return true;
        }
        catch (PyStopIteration)
        {
            _finished = true;
            return false;
        }
    }

    /// <inheritdoc />
    public object? Current() => _current;
}
