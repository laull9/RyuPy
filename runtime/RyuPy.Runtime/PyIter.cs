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
/// 因此这里自己维护游标：列表 / 字符串 / 字节按下标推进（能自然看到追加的元素），
/// 字典与视图带版本号（改动即报错），集合用快照（CPython 对集合也是「行为未定义即报错」，
/// 这里选择确定性的快照并在 semantics.md 登记）。
///
/// 编译产物用两个入口推进迭代：<see cref="PyOps.IterNextOk"/> 判断是否还有元素，
/// <see cref="PyOps.IterValue"/> 取当前元素。分开两步是为了让 MIR 用普通条件分支
/// 表达循环，不需要为 <c>StopIteration</c> 建立异常区域。
/// </remarks>
public sealed class PyIter
{
    private readonly object _source;
    private readonly long _version;
    private readonly object?[]? _snapshot;
    private long _cursor;

    private PyIter(object source, long version, object?[]? snapshot)
    {
        _source = source;
        _version = version;
        _snapshot = snapshot;
    }

    /// <summary>为任意可迭代对象建立迭代器。</summary>
    /// <param name="source">可迭代对象。</param>
    public static PyIter Create(object? source) => source switch
    {
        null or PyNone => throw NotIterable(source),
        PyList list => new PyIter(list, 0, null),
        PyTuple tuple => new PyIter(tuple, 0, null),
        PyDict dict => new PyIter(dict, dict.Version, null),
        PyDictView view => new PyIter(view, 0, view.ToArray()),
        PySet set => new PyIter(set, 0, [.. set.Items]),
        PyFrozenSet frozen => new PyIter(frozen, 0, [.. frozen.Items]),
        string text => new PyIter(text, 0, null),
        PyBytes bytes => new PyIter(bytes, 0, null),
        PyByteArray array => new PyIter(array, 0, null),
        _ => throw NotIterable(source),
    };

    /// <summary>推进一个位置；返回是否还有元素。</summary>
    public bool MoveNext()
    {
        long length = Length();
        if (_cursor >= length)
        {
            return false;
        }
        _cursor++;
        return true;
    }

    /// <summary>取当前元素（<see cref="MoveNext"/> 返回 <c>true</c> 之后调用）。</summary>
    public object? Current()
    {
        CheckVersion();
        long index = _cursor - 1;
        return _source switch
        {
            PyList list => list.Items[(int)index],
            PyTuple tuple => tuple.Items[(int)index],
            PyDict dict => dict.Keys[(int)index],
            PyDictView => _snapshot![(int)index],
            PySet or PyFrozenSet => _snapshot![(int)index],
            string text => text[(int)index].ToString(),
            PyBytes bytes => (long)bytes.Bytes[(int)index],
            PyByteArray array => (long)array.Bytes[(int)index],
            _ => throw NotIterable(_source),
        };
    }

    private long Length() => _source switch
    {
        PyList list => list.Count,
        PyTuple tuple => tuple.Length,
        PyDict dict => dict.Count,
        PyDictView => _snapshot!.Length,
        PySet set => set.Count,
        PyFrozenSet frozen => frozen.Count,
        string text => text.Length,
        PyBytes bytes => bytes.Count,
        PyByteArray array => array.Count,
        _ => throw NotIterable(_source),
    };

    private void CheckVersion()
    {
        if (_source is PyDict dict && dict.Version != _version)
        {
            throw new PyRuntimeError("dictionary changed size during iteration");
        }
    }

    private static PyTypeError NotIterable(object? value) =>
        new($"'{PyRepr.TypeName(value)}' object is not iterable");
}
