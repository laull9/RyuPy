using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>slice</c> 对象：<c>a[start:stop:step]</c>。
/// </summary>
/// <remarks>
/// 三个分量在**使用**时才换算成具体下标（CPython 也是这样），因此构造时不校验类型，
/// 只有真的取下标时才可能抛 <c>TypeError</c> / <c>ValueError</c>。
/// </remarks>
public sealed class PySlice
{
    /// <summary>起点；<c>None</c> 表示省略。</summary>
    public object? Start { get; }

    /// <summary>终点；<c>None</c> 表示省略。</summary>
    public object? Stop { get; }

    /// <summary>步长；<c>None</c> 表示 1。</summary>
    public object? Step { get; }

    /// <summary>构造切片。</summary>
    /// <param name="start">起点。</param>
    /// <param name="stop">终点。</param>
    /// <param name="step">步长。</param>
    public PySlice(object? start, object? stop, object? step)
    {
        Start = start;
        Stop = stop;
        Step = step;
    }

    /// <summary>把切片换算成具体的下标三元组（等价 CPython 的 <c>PySlice_GetIndicesEx</c>）。</summary>
    /// <param name="length">序列长度。</param>
    public (long Start, long Stop, long Step, long Count) Indices(long length)
    {
        long step = ToIndex(Step, "slice step cannot be zero", defaultValue: 1);
        if (step == 0)
        {
            throw new PyValueError("slice step cannot be zero");
        }
        long lower = step < 0 ? -1 : 0;
        long upper = step < 0 ? length - 1 : length;

        long start;
        if (Start is null or PyNone)
        {
            start = step < 0 ? upper : lower;
        }
        else
        {
            start = ToIndex(Start, "slice indices must be integers or None", 0);
            if (start < 0)
            {
                start += length;
                if (start < lower)
                {
                    start = lower;
                }
            }
            else if (start > upper)
            {
                start = upper;
            }
        }

        long stop;
        if (Stop is null or PyNone)
        {
            stop = step < 0 ? lower : upper;
        }
        else
        {
            stop = ToIndex(Stop, "slice indices must be integers or None", 0);
            if (stop < 0)
            {
                stop += length;
                if (stop < lower)
                {
                    stop = lower;
                }
            }
            else if (stop > upper)
            {
                stop = upper;
            }
        }

        long count;
        if (step > 0)
        {
            count = stop > start ? (stop - start + step - 1) / step : 0;
        }
        else
        {
            count = start > stop ? (start - stop - step - 1) / (-step) : 0;
        }
        return (start, stop, step, count);
    }

    private static long ToIndex(object? value, string message, long defaultValue) => value switch
    {
        null or PyNone => defaultValue,
        bool b => b ? 1 : 0,
        long l => l,
        int i => i,
        _ => throw new PyTypeError($"{message}; got '{PyRepr.TypeName(value)}'"),
    };

    /// <summary><c>repr</c> 形式。</summary>
    public override string ToString() =>
        $"slice({PyRepr.Repr(Start)}, {PyRepr.Repr(Stop)}, {PyRepr.Repr(Step)})";
}

/// <summary>
/// Python <c>list</c>：可变、有序、允许重复与异构元素。
/// </summary>
/// <remarks>
/// 用 <see cref="List{T}"/> 直接承载 <c>object?</c> 元素——Python 的列表本来就是「任意对象的
/// 引用序列」，这一点与 .NET 泛型容器天然契合，不需要额外包装每个元素。
/// 共享引用语义因此也是自然的：两个变量指向同一个 <see cref="PyList"/> 实例。
/// </remarks>
public sealed class PyList
{
    /// <summary>元素序列。</summary>
    public List<object?> Items { get; }

    /// <summary>用元素序列构造。</summary>
    /// <param name="items">初始元素。</param>
    public PyList(IEnumerable<object?> items) => Items = new List<object?>(items);

    /// <summary>创建空列表。</summary>
    public PyList() => Items = [];

    /// <summary>元素个数。</summary>
    public int Count => Items.Count;

    /// <summary>按 Python 下标规则取元素。</summary>
    /// <param name="index">下标（支持负数）。</param>
    public object? GetItem(long index) => Items[(int)Normalize(index)];

    /// <summary>按 Python 下标规则写元素。</summary>
    /// <param name="index">下标（支持负数）。</param>
    /// <param name="value">新值。</param>
    public void SetItem(long index, object? value) => Items[(int)Normalize(index)] = value;

    /// <summary>按 Python 下标规则删除元素。</summary>
    /// <param name="index">下标（支持负数）。</param>
    public void DelItem(long index) => Items.RemoveAt((int)Normalize(index));

    /// <summary>按下标取元素，自动区分整数下标与切片。</summary>
    /// <param name="key">整数或 <see cref="PySlice"/>。</param>
    public object? GetItem(object key) => key switch
    {
        PySlice slice => GetSlice(slice),
        _ => GetItem(ToIndex(key)),
    };

    /// <summary>写元素，自动区分整数下标与切片。</summary>
    /// <param name="key">整数或切片。</param>
    /// <param name="value">新值或替换序列。</param>
    public void SetItem(object key, object? value)
    {
        if (key is PySlice slice)
        {
            SetSlice(slice, value);
            return;
        }
        SetItem(ToIndex(key), value);
    }

    /// <summary>删除元素，自动区分整数下标与切片。</summary>
    /// <param name="key">整数或切片。</param>
    public void DelItem(object key)
    {
        if (key is PySlice slice)
        {
            DelSlice(slice);
            return;
        }
        DelItem(ToIndex(key));
    }

    /// <summary>取切片，返回新的列表。</summary>
    /// <param name="slice">切片。</param>
    public PyList GetSlice(PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Count);
        var result = new List<object?>((int)count);
        long index = start;
        for (long i = 0; i < count; i++, index += step)
        {
            result.Add(Items[(int)index]);
        }
        return new PyList(result);
    }

    /// <summary>按步长 1 的切片赋值：可改变列表长度。</summary>
    /// <param name="slice">切片。</param>
    /// <param name="value">参与替换的序列。</param>
    public void SetSlice(PySlice slice, object? value)
    {
        object?[] replacement = PyOps.ToSequence(value);
        var (start, stop, step, count) = slice.Indices(Count);
        if (step == 1)
        {
            Items.RemoveRange((int)start, (int)(stop - start));
            Items.InsertRange((int)start, replacement);
            return;
        }
        // 扩展切片：长度必须一致，否则 CPython 抛 ValueError
        if ((long)replacement.Length != count)
        {
            throw new PyValueError(
                $"attempt to assign sequence of size {replacement.Length} to extended slice of size {count}"
            );
        }
        long index = start;
        for (int i = 0; i < replacement.Length; i++, index += step)
        {
            Items[(int)index] = replacement[i];
        }
    }

    /// <summary>删除切片。</summary>
    /// <param name="slice">切片。</param>
    public void DelSlice(PySlice slice)
    {
        var (start, stop, step, count) = slice.Indices(Count);
        if (step == 1)
        {
            Items.RemoveRange((int)start, (int)(stop - start));
            return;
        }
        // 从后往前删，避免下标漂移
        long index = start + (count - 1) * step;
        for (long i = 0; i < count; i++, index -= step)
        {
            Items.RemoveAt((int)index);
        }
    }

    /// <summary><c>append(x)</c>。</summary>
    /// <param name="value">追加的元素。</param>
    public void Append(object? value) => Items.Add(value);

    /// <summary><c>extend(x)</c>。</summary>
    /// <param name="value">可迭代对象。</param>
    public void Extend(object? value) => Items.AddRange(PyOps.ToSequence(value));

    /// <summary><c>insert(i, x)</c>。</summary>
    /// <param name="index">插入位置。</param>
    /// <param name="value">元素。</param>
    public void Insert(long index, object? value)
    {
        int position = index < 0
            ? Math.Max(0, Items.Count + (int)index)
            : (int)Math.Min(index, Items.Count);
        Items.Insert(position, value);
    }

    /// <summary><c>pop([i])</c>。</summary>
    /// <param name="index">下标，默认末尾。</param>
    public object? Pop(long index = -1)
    {
        if (Items.Count == 0)
        {
            throw new PyIndexError("pop from empty list");
        }
        int position = index < 0 ? Items.Count + (int)index : (int)index;
        if (position < 0 || position >= Items.Count)
        {
            throw new PyIndexError("pop index out of range");
        }
        object? value = Items[position];
        Items.RemoveAt(position);
        return value;
    }

    /// <summary><c>remove(x)</c>。</summary>
    /// <param name="value">要删除的元素。</param>
    public void Remove(object? value)
    {
        int index = (int)IndexOf(value);
        if (index < 0)
        {
            throw new PyValueError("list.remove(x): x not in list");
        }
        Items.RemoveAt(index);
    }

    /// <summary><c>index(x)</c>。</summary>
    /// <param name="value">要查找的元素。</param>
    public long IndexOf(object? value)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (PyHash.Equal(Items[i], value))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary><c>x in list</c>。</summary>
    /// <param name="value">要查找的元素。</param>
    public bool Contains(object? value) => IndexOf(value) >= 0;

    /// <summary><c>clear()</c>。</summary>
    public void Clear() => Items.Clear();

    /// <summary><c>reverse()</c>。</summary>
    public void Reverse() => Items.Reverse();

    /// <summary><c>copy()</c>：浅拷贝。</summary>
    public PyList Copy() => new(Items);

    private long Normalize(long index)
    {
        long normalized = index < 0 ? index + Items.Count : index;
        if (normalized < 0 || normalized >= Items.Count)
        {
            throw new PyIndexError("list index out of range");
        }
        return normalized;
    }

    private static long ToIndex(object? key) => key switch
    {
        bool b => b ? 1 : 0,
        long l => l,
        int i => i,
        _ => throw new PyTypeError(
            $"list indices must be integers or slices, not {PyRepr.TypeName(key)}"
        ),
    };
}

/// <summary>
/// Python <c>dict</c>：保持插入顺序的哈希表。
/// </summary>
/// <remarks>
/// 插入顺序由 <see cref="_keys"/> 单独维护，而不是依赖 .NET 字典的枚举顺序
/// （那属于实现细节）。重复赋值同一个键不改变它原来的位置——这是 Python 的明确行为。
/// <see cref="Version"/> 用于「迭代期间修改」检测：字典视图与迭代器在开始时记下版本号，
/// 发现变化就抛 <c>RuntimeError</c>，与 CPython 一致。
/// </remarks>
public sealed class PyDict
{
    private readonly List<object?> _keys = [];
    private readonly Dictionary<object, object?> _map = new(PyKeyComparer.Instance);

    /// <summary>元素个数。</summary>
    public int Count => _keys.Count;

    /// <summary>结构版本号，每次增删改都会变化。</summary>
    public long Version { get; private set; }

    /// <summary>按插入顺序枚举键值对。</summary>
    public IEnumerable<(object? Key, object? Value)> Entries()
    {
        foreach (object? key in _keys)
        {
            yield return (key, _map[key!]);
        }
    }

    /// <summary>键列表（插入顺序）。</summary>
    public IReadOnlyList<object?> Keys => _keys;

    /// <summary>取值；缺失抛 <c>KeyError</c>。</summary>
    /// <param name="key">键。</param>
    public object? Get(object? key)
    {
        if (key is not null && _map.TryGetValue(key, out object? value))
        {
            return value;
        }
        throw PyKeyError.ForKey(key);
    }

    /// <summary>取值，缺失时返回默认值。</summary>
    /// <param name="key">键。</param>
    /// <param name="fallback">默认值。</param>
    public object? GetOrDefault(object? key, object? fallback) =>
        key is not null && _map.TryGetValue(key, out object? value) ? value : fallback;

    /// <summary>尝试取值。</summary>
    /// <param name="key">键。</param>
    /// <param name="value">取到的值。</param>
    public bool TryGet(object? key, out object? value)
    {
        if (key is null)
        {
            value = null;
            return false;
        }
        return _map.TryGetValue(key, out value);
    }

    /// <summary>赋值（已存在的键保持原位置）。</summary>
    /// <param name="key">键。</param>
    /// <param name="value">值。</param>
    public void Set(object? key, object? value)
    {
        RequireHashable(key);
        object actualKey = key ?? PyNone.Instance;
        if (!_map.ContainsKey(actualKey))
        {
            _keys.Add(key);
        }
        _map[actualKey] = value;
        Version++;
    }

    /// <summary>删除键，缺失抛 <c>KeyError</c>。</summary>
    /// <param name="key">键。</param>
    public void Delete(object? key)
    {
        if (key is null || !_map.Remove(key))
        {
            throw PyKeyError.ForKey(key);
        }
        _keys.RemoveAll(k => PyHash.Equal(k, key));
        Version++;
    }

    /// <summary>包含判断。</summary>
    /// <param name="key">键。</param>
    public bool Contains(object? key) =>
        key is not null && PyHash.IsHashable(key) && _map.ContainsKey(key);

    /// <summary><c>pop(k[, default])</c>。</summary>
    /// <param name="key">键。</param>
    /// <param name="hasDefault">是否提供默认值。</param>
    /// <param name="fallback">默认值。</param>
    public object? Pop(object? key, bool hasDefault, object? fallback)
    {
        if (key is not null && _map.TryGetValue(key, out object? value))
        {
            Delete(key);
            return value;
        }
        if (hasDefault)
        {
            return fallback;
        }
        throw PyKeyError.ForKey(key);
    }

    /// <summary><c>setdefault(k, default)</c>。</summary>
    /// <param name="key">键。</param>
    /// <param name="fallback">默认值。</param>
    public object? SetDefault(object? key, object? fallback)
    {
        if (key is not null && _map.TryGetValue(key, out object? value))
        {
            return value;
        }
        Set(key, fallback);
        return fallback;
    }

    /// <summary><c>update(other)</c>：按另一字典或键值对序列更新。</summary>
    /// <param name="other">另一个字典或可迭代的键值对。</param>
    public void Update(object? other)
    {
        switch (other)
        {
            case PyDict dict:
                foreach (var (key, value) in dict.Entries())
                {
                    Set(key, value);
                }
                break;
            default:
                foreach (object? item in PyOps.ToSequence(other))
                {
                    object?[] pair = PyOps.ToSequence(item);
                    if (pair.Length != 2)
                    {
                        throw new PyValueError(
                            "dictionary update sequence element has length "
                                + $"{pair.Length}; 2 is required"
                        );
                    }
                    Set(pair[0], pair[1]);
                }
                break;
        }
    }

    /// <summary><c>clear()</c>。</summary>
    public void Clear()
    {
        _keys.Clear();
        _map.Clear();
        Version++;
    }

    /// <summary><c>copy()</c>：浅拷贝。</summary>
    public PyDict Copy()
    {
        var copy = new PyDict();
        foreach (var (key, value) in Entries())
        {
            copy.Set(key, value);
        }
        return copy;
    }

    private static void RequireHashable(object? key)
    {
        if (!PyHash.IsHashable(key))
        {
            throw new PyTypeError($"unhashable type: '{PyRepr.TypeName(key)}'");
        }
    }
}

/// <summary>Python <c>set</c>：可变、无序、元素唯一。</summary>
public sealed class PySet
{
    /// <summary>底层集合，键比较走 Python 规则。</summary>
    public HashSet<object> Items { get; }

    /// <summary>用元素序列构造。</summary>
    /// <param name="items">初始元素。</param>
    public PySet(IEnumerable<object?> items) =>
        Items = new HashSet<object>(items.Select(Normalize), PyKeyComparer.Instance);

    /// <summary>创建空集合。</summary>
    public PySet() => Items = new HashSet<object>(PyKeyComparer.Instance);

    /// <summary>元素个数。</summary>
    public int Count => Items.Count;

    /// <summary>加入元素。</summary>
    /// <param name="value">元素。</param>
    public void Add(object? value) => Items.Add(Normalize(value));

    /// <summary>删除元素；缺失抛 <c>KeyError</c>。</summary>
    /// <param name="value">元素。</param>
    public void Remove(object? value)
    {
        if (!Items.Remove(Normalize(value)))
        {
            throw PyKeyError.ForKey(value);
        }
    }

    /// <summary>删除元素；缺失不报错。</summary>
    /// <param name="value">元素。</param>
    public void Discard(object? value) => Items.Remove(Normalize(value));

    /// <summary>包含判断。</summary>
    /// <param name="value">元素。</param>
    public bool Contains(object? value) => Items.Contains(Normalize(value));

    /// <summary>并集。</summary>
    /// <param name="other">另一个集合。</param>
    public PySet Union(PySet other)
    {
        var result = new PySet(Items);
        result.Items.UnionWith(other.Items);
        return result;
    }

    /// <summary>交集。</summary>
    /// <param name="other">另一个集合。</param>
    public PySet Intersect(PySet other)
    {
        var result = new PySet(Items);
        result.Items.IntersectWith(other.Items);
        return result;
    }

    /// <summary>差集。</summary>
    /// <param name="other">另一个集合。</param>
    public PySet Difference(PySet other)
    {
        var result = new PySet(Items);
        result.Items.ExceptWith(other.Items);
        return result;
    }

    /// <summary>对称差。</summary>
    /// <param name="other">另一个集合。</param>
    public PySet SymmetricDifference(PySet other)
    {
        var result = new PySet(Items);
        result.Items.SymmetricExceptWith(other.Items);
        return result;
    }

    /// <summary>是否为 <paramref name="other"/> 的子集。</summary>
    /// <param name="other">另一个集合。</param>
    public bool IsSubsetOf(PySet other) => Items.IsSubsetOf(other.Items);

    /// <summary>是否为 <paramref name="other"/> 的真子集。</summary>
    /// <param name="other">另一个集合。</param>
    public bool IsProperSubsetOf(PySet other) => Items.IsProperSubsetOf(other.Items);

    /// <summary>是否为 <paramref name="other"/> 的超集。</summary>
    /// <param name="other">另一个集合。</param>
    public bool IsSupersetOf(PySet other) => Items.IsSupersetOf(other.Items);

    /// <summary>元素集合相等的判断（供哈希/相等性使用）。</summary>
    /// <param name="other">另一个集合。</param>
    public bool SetEquals(PySet other) => Items.SetEquals(other.Items);

    /// <summary>集合元素不接受 null：<c>None</c> 用单例表示。</summary>
    private static object Normalize(object? value) => value ?? PyNone.Instance;
}

/// <summary>Python <c>frozenset</c>：不可变集合，可哈希。</summary>
public sealed class PyFrozenSet
{
    /// <summary>元素集合。</summary>
    public HashSet<object> Items { get; }

    /// <summary>用元素序列构造。</summary>
    /// <param name="items">元素。</param>
    public PyFrozenSet(IEnumerable<object?> items) =>
        Items = new HashSet<object>(
            System.Linq.Enumerable.Select(items, value => value ?? PyNone.Instance),
            PyKeyComparer.Instance
        );

    /// <summary>元素个数。</summary>
    public int Count => Items.Count;

    /// <summary>元素集合相等的判断。</summary>
    /// <param name="other">另一个集合。</param>
    public bool SetEquals(PyFrozenSet other) => Items.SetEquals(other.Items);

    /// <summary>并集。</summary>
    /// <param name="other">另一个集合。</param>
    public PyFrozenSet Union(PyFrozenSet other) => new(new HashSet<object>(Items, PyKeyComparer.Instance).Tap(h => h.UnionWith(other.Items)));

    /// <summary>交集。</summary>
    /// <param name="other">另一个集合。</param>
    public PyFrozenSet Intersect(PyFrozenSet other) => new(new HashSet<object>(Items, PyKeyComparer.Instance).Tap(h => h.IntersectWith(other.Items)));

    /// <summary>差集。</summary>
    /// <param name="other">另一个集合。</param>
    public PyFrozenSet Difference(PyFrozenSet other) => new(new HashSet<object>(Items, PyKeyComparer.Instance).Tap(h => h.ExceptWith(other.Items)));

    /// <summary>对称差。</summary>
    /// <param name="other">另一个集合。</param>
    public PyFrozenSet SymmetricDifference(PyFrozenSet other) => new(new HashSet<object>(Items, PyKeyComparer.Instance).Tap(h => h.SymmetricExceptWith(other.Items)));
}

/// <summary>小工具：在表达式里对新建对象做一次就地操作。</summary>
internal static class TapExtensions
{
    /// <summary>对 <paramref name="value"/> 执行 <paramref name="action"/> 并原样返回。</summary>
    public static T Tap<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}

/// <summary>Python <c>bytes</c>：不可变字节序列。</summary>
public sealed class PyBytes
{
    /// <summary>字节内容。</summary>
    public byte[] Bytes { get; }

    /// <summary>用字节数组构造。</summary>
    /// <param name="bytes">字节。</param>
    public PyBytes(byte[] bytes) => Bytes = bytes;

    /// <summary>只读视图。</summary>
    public ReadOnlySpan<byte> Span => Bytes;

    /// <summary>长度。</summary>
    public int Count => Bytes.Length;

    /// <summary>按下标取字节（返回 <c>int</c>），支持负数。</summary>
    /// <param name="index">下标。</param>
    public object GetItem(long index)
    {
        long normalized = index < 0 ? index + Bytes.Length : index;
        if (normalized < 0 || normalized >= Bytes.Length)
        {
            throw new PyIndexError("index out of range");
        }
        return (long)Bytes[normalized];
    }

    /// <summary>取切片。</summary>
    /// <param name="slice">切片。</param>
    public PyBytes GetSlice(PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Count);
        var result = new byte[count];
        long index = start;
        for (long i = 0; i < count; i++, index += step)
        {
            result[i] = Bytes[index];
        }
        return new PyBytes(result);
    }

    /// <summary>按 UTF-8 解码（默认编码）。</summary>
    /// <param name="encoding">编码名。</param>
    public string Decode(string encoding) => PyEncoding.Decode(Bytes, encoding);
}

/// <summary>Python <c>bytearray</c>：可变字节序列。</summary>
public sealed class PyByteArray
{
    /// <summary>字节内容。</summary>
    public List<byte> Bytes { get; }

    /// <summary>用字节序列构造。</summary>
    /// <param name="bytes">字节。</param>
    public PyByteArray(IEnumerable<byte> bytes) => Bytes = new List<byte>(bytes);

    /// <summary>长度。</summary>
    public int Count => Bytes.Count;

    /// <summary>按下标取字节（返回 <c>int</c>）。</summary>
    /// <param name="index">下标。</param>
    public object GetItem(long index)
    {
        long normalized = index < 0 ? index + Bytes.Count : index;
        if (normalized < 0 || normalized >= Bytes.Count)
        {
            throw new PyIndexError("bytearray index out of range");
        }
        return (long)Bytes[(int)normalized];
    }

    /// <summary>按下标写字节。</summary>
    /// <param name="index">下标。</param>
    /// <param name="value">0~255 的整数。</param>
    public void SetItem(long index, object? value)
    {
        long normalized = index < 0 ? index + Bytes.Count : index;
        if (normalized < 0 || normalized >= Bytes.Count)
        {
            throw new PyIndexError("bytearray index out of range");
        }
        Bytes[(int)normalized] = PyEncoding.ToByte(value);
    }

    /// <summary>取切片。</summary>
    /// <param name="slice">切片。</param>
    public PyByteArray GetSlice(PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Count);
        var result = new List<byte>((int)count);
        long index = start;
        for (long i = 0; i < count; i++, index += step)
        {
            result.Add(Bytes[(int)index]);
        }
        return new PyByteArray(result);
    }

    /// <summary><c>del ba[i]</c> / <c>del ba[i:j]</c>。</summary>
    /// <param name="index">下标或切片。</param>
    public void DelItem(object? index)
    {
        switch (index)
        {
            case PySlice slice:
                {
                    var (start, _, step, count) = slice.Indices(Count);
                    if (count == 0)
                    {
                        return;
                    }
                    if (step > 0)
                    {
                        // 从后往前删，前面的下标才不会因为移除而失效
                        long position = start + (count - 1) * step;
                        for (long i = 0; i < count; i++, position -= step)
                        {
                            Bytes.RemoveAt((int)position);
                        }
                    }
                    else
                    {
                        long position = start;
                        for (long i = 0; i < count; i++, position += step)
                        {
                            Bytes.RemoveAt((int)position);
                        }
                    }
                    return;
                }
            case long raw:
                {
                    long normalized = raw < 0 ? raw + Count : raw;
                    if (normalized < 0 || normalized >= Count)
                    {
                        throw new PyIndexError("bytearray index out of range");
                    }
                    Bytes.RemoveAt((int)normalized);
                    return;
                }
            case int or bool or short or byte or System.Numerics.BigInteger:
                DelItem(ToIndex(index));
                return;
            default:
                throw new PyTypeError(
                    $"bytearray indices must be integers or slices, not {PyRepr.TypeName(index)}"
                );
        }
    }

    /// <summary><c>extend(x)</c>。</summary>
    /// <param name="value">字节序列。</param>
    public void Extend(object? value) => Bytes.AddRange(PyEncoding.ToBytes(value));

    /// <summary><c>append(x)</c>。</summary>
    /// <param name="value">0~255 的整数。</param>
    public void Append(object? value) => Bytes.Add(PyEncoding.ToByte(value));

    /// <summary>复制成 <c>bytes</c>。</summary>
    public PyBytes ToBytes() => new([.. Bytes]);

    private static long ToIndex(object? key) => key switch
    {
        bool b => b ? 1 : 0,
        long l => l,
        int i => i,
        short s => s,
        byte b2 => b2,
        System.Numerics.BigInteger big when big >= long.MinValue && big <= long.MaxValue =>
            (long)big,
        System.Numerics.BigInteger => throw new PyIndexError(
            "cannot fit 'int' into an index-sized integer"
        ),
        _ => throw new PyTypeError(
            $"bytearray indices must be integers or slices, not {PyRepr.TypeName(key)}"
        ),
    };
}

/// <summary>编码与字节转换。</summary>
public static class PyEncoding
{
    /// <summary>按名字解码字节串（支持 utf-8 / ascii / latin-1）。</summary>
    /// <param name="bytes">字节。</param>
    /// <param name="encoding">编码名。</param>
    public static string Decode(ReadOnlySpan<byte> bytes, string encoding)
    {
        string normalized = encoding.ToLowerInvariant().Replace("-", "").Replace("_", "");
        try
        {
            return normalized switch
            {
                "utf8" or "u8" or "" => new UTF8Encoding(false, true).GetString(bytes),
                "ascii" or "usascii" => Encoding.ASCII.GetString(bytes),
                "latin1" or "iso88591" => Encoding.Latin1.GetString(bytes),
                _ => throw new PyLookupError($"unknown encoding: {encoding}"),
            };
        }
        catch (DecoderFallbackException)
        {
            throw new PyUnicodeDecodeError(
                $"'utf-8' codec can't decode bytes: invalid start byte"
            );
        }
    }

    /// <summary>按名字编码字符串。</summary>
    /// <param name="text">字符串。</param>
    /// <param name="encoding">编码名。</param>
    public static byte[] Encode(string text, string encoding)
    {
        string normalized = encoding.ToLowerInvariant().Replace("-", "").Replace("_", "");
        return normalized switch
        {
            "utf8" or "u8" or "" => new UTF8Encoding(false, true).GetBytes(text),
            "ascii" or "usascii" => Encoding.ASCII.GetBytes(text),
            "latin1" or "iso88591" => Encoding.Latin1.GetBytes(text),
            _ => throw new PyLookupError($"unknown encoding: {encoding}"),
        };
    }

    /// <summary>把任意序列转成字节数组。</summary>
    /// <param name="value">bytes / bytearray / 整数序列。</param>
    public static byte[] ToBytes(object? value) => value switch
    {
        PyBytes bytes => bytes.Bytes,
        PyByteArray array => [.. array.Bytes],
        string text => Encoding.Latin1.GetBytes(text),
        _ => Array.ConvertAll(PyOps.ToSequence(value), ToByte),
    };

    /// <summary>把整数转成单字节，越界抛 <c>ValueError</c>。</summary>
    /// <param name="value">整数值。</param>
    public static byte ToByte(object? value)
    {
        long number = value switch
        {
            bool b => b ? 1 : 0,
            long l => l,
            int i => i,
            _ => throw new PyTypeError(
                $"'{
                    PyRepr.TypeName(value)
                }' object cannot be interpreted as an integer"
            ),
        };
        if (number < 0 || number > 255)
        {
            throw new PyValueError("byte must be in range(0, 256)");
        }
        return (byte)number;
    }
}
