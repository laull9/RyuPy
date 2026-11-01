using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>
/// Python <c>dict</c>：保持插入顺序的哈希表。
/// </summary>
/// <remarks>
/// # 布局
///
/// 三个结构协同工作：
/// <list type="bullet">
/// <item><see cref="_index"/>：键 → 槽位下标，查找 / 覆盖 / 删除都是 O(1)；</item>
/// <item><see cref="_keys"/> / <see cref="_values"/>：按下标平行的槽位数组，下标即插入顺序；</item>
/// <item><c>null</c> 键表示**墓碑**（已删除），迭代时跳过。</item>
/// </list>
///
/// 为什么不用 <c>List.RemoveAll</c> 维护顺序：删除一个键要 O(n) 扫描并搬移后续元素，
/// 而字典删除在真实程序里很常见（<c>pop</c> / <c>del</c>）。墓碑把删除降到 O(1)，
/// 只在墓碑过多时做一次整理（<see cref="Compact"/>），均摊仍是 O(1)。
///
/// # 版本号
///
/// <see cref="Version"/> 只在**结构大小**变化时递增（新增 / 删除 / 清空），
/// 覆盖已有键不递增。这是 CPython 的规则：迭代中给已有键赋新值合法，
/// 增删键才抛 <c>RuntimeError: dictionary changed size during iteration</c>。
/// </remarks>
public sealed class PyDict
{
    /// <summary>墓碑超过这个数量且占比过半时才整理，避免每次删除都搬数组。</summary>
    private const int MinTombstonesBeforeCompact = 32;

    private readonly Dictionary<object, int> _index = new(PyKeyComparer.Instance);
    private readonly List<object?> _keys = [];
    private readonly List<object?> _values = [];
    private int _used;
    private int _tombstones;

    /// <summary>元素个数（不含墓碑）。</summary>
    public int Count => _used;

    /// <summary>结构版本号：只在增删键时变化（覆盖值不变）。</summary>
    public long Version { get; private set; }

    /// <summary>槽位总数（含墓碑）；迭代器用它判断终点。</summary>
    internal int SlotCount => _keys.Count;

    /// <summary>按插入顺序枚举键。</summary>
    public IEnumerable<object?> Keys
    {
        get
        {
            for (int slot = 0; slot < _keys.Count; slot++)
            {
                object? key = _keys[slot];
                if (key is not null)
                {
                    yield return key;
                }
            }
        }
    }

    /// <summary>按插入顺序枚举键值对。</summary>
    public IEnumerable<(object? Key, object? Value)> Entries()
    {
        for (int slot = 0; slot < _keys.Count; slot++)
        {
            object? key = _keys[slot];
            if (key is not null)
            {
                yield return (key, _values[slot]);
            }
        }
    }

    /// <summary>取槽位上的键；墓碑返回 <c>false</c>。</summary>
    internal bool TryKeyAt(int slot, out object? key)
    {
        key = _keys[slot];
        return key is not null;
    }

    /// <summary>取槽位上的值（调用前需确认该槽位不是墓碑）。</summary>
    internal object? ValueAt(int slot) => _values[slot];

    /// <summary>取值；缺失抛 <c>KeyError</c>。</summary>
    /// <param name="key">键。</param>
    public object? Get(object? key)
    {
        if (key is not null && _index.TryGetValue(key, out int slot))
        {
            return _values[slot];
        }
        throw PyKeyError.ForKey(key);
    }

    /// <summary>取值，缺失时返回默认值。</summary>
    /// <param name="key">键。</param>
    /// <param name="fallback">默认值。</param>
    public object? GetOrDefault(object? key, object? fallback) =>
        key is not null && _index.TryGetValue(key, out int slot) ? _values[slot] : fallback;

    /// <summary>尝试取值。</summary>
    /// <param name="key">键。</param>
    /// <param name="value">取到的值。</param>
    public bool TryGet(object? key, out object? value)
    {
        if (key is not null && _index.TryGetValue(key, out int slot))
        {
            value = _values[slot];
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>赋值（已存在的键保持原位置，且不改变版本号）。</summary>
    /// <param name="key">键。</param>
    /// <param name="value">值。</param>
    public void Set(object? key, object? value)
    {
        RequireHashable(key);
        object actualKey = key ?? PyNone.Instance;
        // 一次查找同时完成「查找或登记」：先 TryGetValue 再写索引器会对新键
        // 重复调用用户 __hash__ / __eq__，既多做工作也重复可观察副作用。
        ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_index, actualKey, out bool exists);
        if (exists)
        {
            // 覆盖已有键：位置不变、大小不变，因此不算「迭代期间修改」
            _values[slot] = value;
            return;
        }
        slot = _keys.Count;
        _keys.Add(actualKey);
        _values.Add(value);
        _used++;
        Version++;
        if (_tombstones >= MinTombstonesBeforeCompact && _tombstones * 2 > _keys.Count)
        {
            Compact();
        }
    }

    /// <summary>删除键，缺失抛 <c>KeyError</c>。删除是 O(1)（留墓碑）。</summary>
    /// <param name="key">键。</param>
    public void Delete(object? key)
    {
        if (key is null || !_index.Remove(key, out int slot))
        {
            throw PyKeyError.ForKey(key);
        }
        _keys[slot] = null;
        _values[slot] = null;
        _used--;
        _tombstones++;
        Version++;
    }

    /// <summary>包含判断；不可哈希的键抛 <c>TypeError</c>（与 CPython 一致）。</summary>
    /// <param name="key">键。</param>
    public bool Contains(object? key)
    {
        RequireHashable(key);
        return key is not null && _index.ContainsKey(key);
    }

    /// <summary><c>pop(k[, default])</c>。</summary>
    /// <param name="key">键。</param>
    /// <param name="hasDefault">是否提供默认值。</param>
    /// <param name="fallback">默认值。</param>
    public object? Pop(object? key, bool hasDefault, object? fallback)
    {
        if (key is not null && _index.TryGetValue(key, out int slot))
        {
            object? value = _values[slot];
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
        if (key is not null && _index.TryGetValue(key, out int slot))
        {
            return _values[slot];
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
            case PyEnumMembers proxy:
                Update(proxy.Dictionary);
                break;
            case IPyNativeMapping mapping:
                Update(mapping.Dictionary);
                break;
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
        _values.Clear();
        _index.Clear();
        _used = 0;
        _tombstones = 0;
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

    /// <summary>
    /// 整理墓碑：把存活槽位压紧，保持插入顺序，并重建键 → 槽位映射。
    /// </summary>
    /// <remarks>
    /// 只在插入时调用，而插入已经使 <see cref="Version"/> 递增；任何正在迭代的
    /// 迭代器都会在下一次取值时看到版本变化并抛 <c>RuntimeError</c>，
    /// 因此整理不会让「迭代到一半的槽位下标」被悄悄错位使用。
    /// </remarks>
    private void Compact()
    {
        int write = 0;
        for (int read = 0; read < _keys.Count; read++)
        {
            object? key = _keys[read];
            if (key is null)
            {
                continue;
            }
            if (write != read)
            {
                _keys[write] = key;
                _values[write] = _values[read];
                _index[key] = write;
            }
            write++;
        }
        if (write < _keys.Count)
        {
            _keys.RemoveRange(write, _keys.Count - write);
            _values.RemoveRange(write, _values.Count - write);
        }
        _tombstones = 0;
    }

    private static void RequireHashable(object? key)
    {
        if (!PyHash.IsHashable(key))
        {
            throw new PyTypeError($"unhashable type: '{PyRepr.TypeName(key)}'");
        }
    }
}

/// <summary>字典视图的种类。</summary>
public enum PyDictViewKind
{
    /// <summary><c>dict.keys()</c>。</summary>
    Keys,

    /// <summary><c>dict.values()</c>。</summary>
    Values,

    /// <summary><c>dict.items()</c>。</summary>
    Items,
}

/// <summary>
/// 字典视图：<c>keys()</c> / <c>values()</c> / <c>items()</c> 的返回值。
/// </summary>
/// <remarks>
/// 视图是**活**的：构造后字典发生增删，视图立刻反映出来（CPython 的行为）。
/// 视图本身不保存游标——每次 <c>for</c> 循环由 <see cref="PyIter"/> 创建一个独立迭代器，
/// 并各自记录版本号。这样既符合「同一视图可以并行迭代多次」，也避免视图对象被复用后
/// 游标串味。
/// </remarks>
public sealed class PyDictView
{
    /// <summary>被观察的字典。</summary>
    internal PyDict Dict { get; }

    /// <summary>视图种类。</summary>
    public PyDictViewKind Kind { get; }

    /// <summary>构造视图。</summary>
    /// <param name="dict">被观察的字典。</param>
    /// <param name="kind">视图种类。</param>
    public PyDictView(PyDict dict, PyDictViewKind kind)
    {
        Dict = dict;
        Kind = kind;
    }

    /// <summary>当前元素个数（活的）。</summary>
    public int Count => Dict.Count;

    /// <summary>
    /// <c>value in view</c>：keys 看键、values 看值、items 看 <c>(key, value)</c> 二元组。
    /// </summary>
    /// <param name="value">被查找的值。</param>
    /// <remarks>
    /// 视图是活的，所以每次判断都直接读字典的当前内容，不缓存任何快照。
    /// items 视图的成员判断要求一个长度恰好为 2 的 tuple，与 CPython 一致。
    /// </remarks>
    public bool Contains(object? value)
    {
        switch (Kind)
        {
            case PyDictViewKind.Keys:
                return Dict.Contains(value);
            case PyDictViewKind.Values:
                foreach (var (_, item) in Dict.Entries())
                {
                    if (PyHash.Equal(item, value))
                    {
                        return true;
                    }
                }
                return false;
            default:
                if (value is not PyTuple pair || pair.Length != 2)
                {
                    return false;
                }
                return Dict.TryGet(pair.Items[0], out object? found)
                    && PyHash.Equal(found, pair.Items[1]);
        }
    }

    /// <summary>
    /// 把视图摊平成数组（<c>list(view)</c> / <c>sorted(view)</c> 用）。
    /// </summary>
    /// <remarks>
    /// 这里**不做**版本校验：视图是活的，新建一次遍历看到的是当前内容
    /// （<c>keys = d.keys(); d["x"] = 1; list(keys)</c> 在 CPython 里是合法的）。
    /// 版本校验只发生在同一个迭代器内部（见 <see cref="PyIter"/>）。
    /// </remarks>
    public object?[] ToArray()
    {
        var items = new object?[Dict.Count];
        int index = 0;
        foreach (var (key, value) in Dict.Entries())
        {
            items[index++] = Kind switch
            {
                PyDictViewKind.Keys => key,
                PyDictViewKind.Values => value,
                _ => new PyTuple([key, value]),
            };
        }
        return items;
    }
}
