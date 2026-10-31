using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 容器与内建的动态入口（M2.3）。
/// </summary>
/// <remarks>
/// 编译产物里，容器字面量、下标、删除、<c>in</c>、内建函数调用、方法调用统一走这里。
/// 为什么集中成一个类而不是给每个容器一种指令：ABI 面越小，Rust 侧的 <c>RuntimeFn</c>
/// 与 C# 侧越不容易漂移；代价是多一次分派，相对于容器操作本身的成本可以忽略。
/// </remarks>
public static partial class PyOps
{
    // ───────────────────────── 构造 ─────────────────────────

    /// <summary><c>[a, b]</c>。</summary>
    /// <param name="items">元素。</param>
    public static object MakeList(object?[] items) => new PyList(items);

    /// <summary><c>(a, b)</c>。</summary>
    /// <param name="items">元素。</param>
    public static object MakeTuple(object?[] items) => new PyTuple(items);

    /// <summary><c>{}</c>。</summary>
    public static object MakeDict() => new PyDict();

    /// <summary><c>{a, b}</c>。</summary>
    /// <param name="items">元素。</param>
    public static object MakeSet(object?[] items) => new PySet(items);

    /// <summary><c>a[start:stop:step]</c> 里的切片对象。</summary>
    /// <param name="start">起点。</param>
    /// <param name="stop">终点。</param>
    /// <param name="step">步长。</param>
    public static object MakeSlice(object? start, object? stop, object? step) =>
        new PySlice(start, stop, step);

    /// <summary><c>b"..."</c>：源码字符串按 latin-1 逐字节还原。</summary>
    /// <param name="text">每个字符是一个字节。</param>
    public static object MakeBytes(string text) =>
        new PyBytes(System.Text.Encoding.Latin1.GetBytes(text));

    // ───────────────────────── 下标 ─────────────────────────

    /// <summary><c>obj[key]</c>。</summary>
    /// <param name="obj">容器。</param>
    /// <param name="key">下标或键。</param>
    public static object? GetItem(object? obj, object? key) => obj switch
    {
        PyList list => list.GetItem(key!),
        PyTuple tuple => tuple.GetItem(key!),
        PyDict dict => dict.Get(key),
        string text => GetString(text, key),
        PyBytes bytes => key is PySlice slice ? bytes.GetSlice(slice) : bytes.GetItem(ToIndex(key, "bytes")),
        PyByteArray array => key is PySlice slice2
            ? array.GetSlice(slice2)
            : array.GetItem(ToIndex(key, "bytearray")),
        _ => throw new PyTypeError($"'{PyRepr.TypeName(obj)}' object is not subscriptable"),
    };

    /// <summary><c>obj[key] = value</c>。</summary>
    /// <param name="obj">容器。</param>
    /// <param name="key">下标或键。</param>
    /// <param name="value">新值。</param>
    public static void SetItem(object? obj, object? key, object? value)
    {
        switch (obj)
        {
            case PyList list:
                list.SetItem(key!, value);
                return;
            case PyDict dict:
                dict.Set(key, value);
                return;
            case PyByteArray array when key is not PySlice:
                array.SetItem(ToIndex(key, "bytearray"), value);
                return;
            case string:
            case PyTuple:
                throw new PyTypeError($"'{PyRepr.TypeName(obj)}' object does not support item assignment");
            default:
                throw new PyTypeError($"'{PyRepr.TypeName(obj)}' object does not support item assignment");
        }
    }

    /// <summary><c>del obj[key]</c>。</summary>
    /// <param name="obj">容器。</param>
    /// <param name="key">下标或键。</param>
    public static void DelItem(object? obj, object? key)
    {
        switch (obj)
        {
            case PyList list:
                list.DelItem(key!);
                return;
            case PyDict dict:
                dict.Delete(key);
                return;
            case PyByteArray buffer:
                buffer.DelItem(key);
                return;
            default:
                throw new PyTypeError(
                    $"'{PyRepr.TypeName(obj)}' object does not support item deletion"
                );
        }
    }

    // ───────────────────────── 判定与转换 ─────────────────────────

    /// <summary><c>x in obj</c>。</summary>
    /// <param name="container">容器。</param>
    /// <param name="value">被查找的值。</param>
    public static bool Contains(object? container, object? value) => container switch
    {
        PyList list => list.Contains(value),
        PyTuple tuple => tuple.Contains(value),
        PyDict dict => dict.Contains(value),
        PySet set => set.Contains(value),
        PyFrozenSet frozen => frozen.Items.Contains(value ?? PyNone.Instance),
        string text => value is string needle && text.Contains(needle, StringComparison.Ordinal),
        PyBytes bytes => ContainsBytes(bytes.Span, value),
        PyByteArray array => ContainsBytes([.. array.Bytes], value),
        _ => throw new PyTypeError(
            $"argument of type '{PyRepr.TypeName(container)}' is not iterable"
        ),
    };

    /// <summary><c>len(obj)</c>。</summary>
    /// <param name="value">容器或字符串。</param>
    public static long Len(object? value) => value switch
    {
        PyList list => list.Count,
        PyTuple tuple => tuple.Length,
        PyDict dict => dict.Count,
        PySet set => set.Count,
        PyFrozenSet frozen => frozen.Count,
        string text => text.Length,
        PyBytes bytes => bytes.Count,
        PyByteArray array => array.Count,
        _ => throw new PyTypeError(
            $"object of type '{PyRepr.TypeName(value)}' has no len()"
        ),
    };

    /// <summary><c>a is b</c>：引用身份比较。</summary>
    /// <param name="a">左值。</param>
    /// <param name="b">右值。</param>
    public static bool Is(object? a, object? b)
    {
        // None 与单例值按身份比较即可；数值类型在这个动态表示下是装箱值，
        // 因此 `1 is 1` 为 false——这是与 CPython 的已知差异（记在 semantics.md）。
        return ReferenceEquals(a, b);
    }

    /// <summary><c>repr(obj)</c> 的编译期入口。</summary>
    /// <param name="value">任意值。</param>
    public static string ReprValue(object? value) => PyRepr.Repr(value);

    /// <summary>把可迭代对象摊平成数组（容器构造、解包、<c>extend</c> 共用）。</summary>
    /// <param name="value">可迭代对象。</param>
    public static object?[] ToSequence(object? value) => value switch
    {
        object?[] array => array,
        PyList list => [.. list.Items],
        PyTuple tuple => tuple.Items,
        PyDict dict => KeysOf(dict),
        PyDictView view => view.ToArray(),
        PySet set => [.. set.Items],
        PyFrozenSet frozen => [.. frozen.Items],
        string text => Chars(text),
        PyBytes bytes => MapBytes(bytes.Span),
        PyByteArray array => MapBytes([.. array.Bytes]),
        _ => throw new PyTypeError($"'{PyRepr.TypeName(value)}' object is not iterable"),
    };

    // ───────────────────────── 内建函数 ─────────────────────────

    /// <summary>内建函数调用（<c>len</c> / <c>list</c> / <c>sorted</c> …）。</summary>
    /// <param name="name">函数名。</param>
    /// <param name="args">实参。</param>
    public static object? CallBuiltin(string name, object?[] args) => name switch
    {
        "len" => Len(Single(name, args)),
        "list" => new PyList(args.Length == 0 ? [] : ToSequence(args[0])),
        "tuple" => new PyTuple(args.Length == 0 ? [] : ToSequence(args[0])),
        "dict" => BuiltinDict(args),
        "set" => new PySet(args.Length == 0 ? [] : ToSequence(args[0])),
        "frozenset" => new PyFrozenSet(args.Length == 0 ? [] : ToSequence(args[0])),
        "bytes" => args.Length == 0
            ? new PyBytes([])
            : new PyBytes(PyEncoding.ToBytes(args[0])),
        "bytearray" => args.Length == 0
            ? new PyByteArray([])
            : new PyByteArray(PyEncoding.ToBytes(args[0])),
        "sorted" => BuiltinSorted(args),
        "hash" => PyHash.Of(Single(name, args)),
        "repr" => PyRepr.Repr(Single(name, args)),
        "bool" => Truthy(args.Length == 0 ? PyNone.Instance : args[0]),
        "iter" => ToSequence(Single(name, args)),
        _ => throw new PyNameError($"name '{name}' is not defined"),
    };

    private static object BuiltinDict(object?[] args)
    {
        var dict = new PyDict();
        if (args.Length > 0)
        {
            dict.Update(args[0]);
        }
        return dict;
    }

    private static object BuiltinSorted(object?[] args)
    {
        object?[] items = ToSequence(Single("sorted", args));
        var list = new List<object?>(items);
        list.Sort(CompareForSort);
        return new PyList(list);
    }

    private static int CompareForSort(object? a, object? b)
    {
        if (PyHash.Equal(a, b))
        {
            return 0;
        }
        object left = a ?? PyNone.Instance;
        object right = b ?? PyNone.Instance;
        return Compare(Lt, left, right) ? -1 : 1;
    }

    /// <summary>执行一个无返回值的方法并返回 <c>None</c>。</summary>
    private static object? Void(Action action)
    {
        action();
        return PyNone.Instance;
    }

    private static object? Single(string name, object?[] args)
    {
        if (args.Length != 1)
        {
            throw new PyTypeError($"{name}() takes exactly one argument ({args.Length} given)");
        }
        return args[0];
    }

    /// <summary><c>list.append(x)</c>：星号字面量的增量构建入口。</summary>
    /// <param name="target">目标列表。</param>
    /// <param name="value">追加的元素。</param>
    public static void ListAppend(object target, object? value)
    {
        if (target is not PyList list)
        {
            throw new PyTypeError("ListAppend 目标不是 list");
        }
        list.Append(value);
    }

    /// <summary><c>list.extend(seq)</c>：星号字面量的展开入口。</summary>
    /// <param name="target">目标列表。</param>
    /// <param name="value">可迭代对象。</param>
    public static void ListExtend(object target, object? value)
    {
        if (target is not PyList list)
        {
            throw new PyTypeError("ListExtend 目标不是 list");
        }
        list.Extend(value);
    }

    /// <summary>为 <c>for x in obj</c> 建立迭代器。</summary>
    /// <param name="source">可迭代对象。</param>
    public static object IterStart(object? source) => PyIter.Create(source);

    /// <summary>推进迭代器；返回是否还有元素。</summary>
    /// <param name="iterator">迭代器。</param>
    public static bool IterNextOk(object iterator)
    {
        return iterator is PyIter it && it.MoveNext();
    }

    /// <summary>取迭代器的当前元素。</summary>
    /// <param name="iterator">迭代器。</param>
    public static object? IterValue(object iterator) =>
        iterator is PyIter it
            ? it.Current()
            : throw new PyTypeError("IterValue 的参数不是迭代器");

    /// <summary>解包长度校验：元素不够时抛 <c>ValueError</c>。</summary>
    /// <param name="length">序列长度。</param>
    /// <param name="required">非星号目标的数量。</param>
    public static void UnpackCheck(long length, long required)
    {
        if (length < required)
        {
            throw new PyValueError(
                length == 0
                    ? "not enough values to unpack (expected at least "
                        + $"{required}, got 0)"
                    : $"not enough values to unpack (expected at least {required}, got {length})"
            );
        }
    }

    // ───────────────────────── 方法调用 ─────────────────────────

    /// <summary>方法调用：<c>obj.name(args)</c>。</summary>
    /// <param name="obj">接收者。</param>
    /// <param name="name">方法名。</param>
    /// <param name="args">实参。</param>
    public static object? CallMethod(object? obj, string name, object?[] args) => obj switch
    {
        PyList list => ListMethod(list, name, args),
        PyTuple tuple => TupleMethod(tuple, name, args),
        PyDict dict => DictMethod(dict, name, args),
        PySet set => SetMethod(set, name, args),
        PyByteArray array => ByteArrayMethod(array, name, args),
        string text => StringMethod(text, name, args),
        PyBytes bytes => BytesMethod(bytes, name, args),
        _ => throw new PyAttributeError(
            $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
        ),
    };

    private static object? ListMethod(PyList list, string name, object?[] args) => name switch
    {
        "append" => Append(list, args),
        "extend" => Extend(list, args),
        "insert" => Insert(list, args),
        "pop" => args.Length == 0 ? list.Pop() : list.Pop(ToIndex(args[0], "pop")),
        "remove" => RemoveItem(list, args),
        "index" => list.IndexOf(Single("index", args)),
        "count" => CountItem(list, args),
        "clear" => Void(list.Clear),
        "reverse" => Void(list.Reverse),
        "copy" => list.Copy(),
        "sort" => SortList(list, args),
        _ => throw MissingAttribute("list", name),
    };

    private static object? Append(PyList list, object?[] args)
    {
        list.Append(Single("append", args));
        return PyNone.Instance;
    }

    private static object? Extend(PyList list, object?[] args)
    {
        list.Extend(Single("extend", args));
        return PyNone.Instance;
    }

    private static object? Insert(PyList list, object?[] args)
    {
        if (args.Length != 2)
        {
            throw new PyTypeError("insert() takes exactly 2 arguments");
        }
        list.Insert(ToIndex(args[0], "insert"), args[1]);
        return PyNone.Instance;
    }

    private static object? RemoveItem(PyList list, object?[] args)
    {
        list.Remove(Single("remove", args));
        return PyNone.Instance;
    }

    private static object CountItem(PyList list, object?[] args)
    {
        object? needle = Single("count", args);
        long count = 0;
        foreach (object? item in list.Items)
        {
            if (PyHash.Equal(item, needle))
            {
                count++;
            }
        }
        return count;
    }

    private static object? SortList(PyList list, object?[] args)
    {
        bool reverse = false;
        foreach (object? arg in args)
        {
            if (arg is bool flag)
            {
                reverse = flag;
            }
            else
            {
                throw new PyTypeError("sort() 目前只支持 reverse 参数");
            }
        }
        list.Items.Sort(CompareForSort);
        if (reverse)
        {
            list.Items.Reverse();
        }
        return PyNone.Instance;
    }

    private static object? TupleMethod(PyTuple tuple, string name, object?[] args) => name switch
    {
        "count" => CountItem(tuple.Items, args),
        "index" => IndexOf(tuple.Items, Single("index", args)),
        _ => throw MissingAttribute("tuple", name),
    };

    private static object CountItem(object?[] items, object?[] args)
    {
        object? needle = Single("count", args);
        long count = 0;
        foreach (object? item in items)
        {
            if (PyHash.Equal(item, needle))
            {
                count++;
            }
        }
        return count;
    }

    private static object IndexOf(object?[] items, object? needle)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (PyHash.Equal(items[i], needle))
            {
                return (long)i;
            }
        }
        throw new PyValueError("tuple.index(x): x not in tuple");
    }

    private static object? DictMethod(PyDict dict, string name, object?[] args) => name switch
    {
        "get" => dict.GetOrDefault(args[0], args.Length > 1 ? args[1] : PyNone.Instance),
        "keys" => new PyDictView(dict, PyDictViewKind.Keys),
        "values" => new PyDictView(dict, PyDictViewKind.Values),
        "items" => new PyDictView(dict, PyDictViewKind.Items),
        "update" => Update(dict, args),
        "pop" => dict.Pop(args[0], args.Length > 1, args.Length > 1 ? args[1] : PyNone.Instance),
        "setdefault" => dict.SetDefault(args[0], args.Length > 1 ? args[1] : PyNone.Instance),
        "clear" => Void(dict.Clear),
        "copy" => dict.Copy(),
        _ => throw MissingAttribute("dict", name),
    };

    private static object? Update(PyDict dict, object?[] args)
    {
        dict.Update(Single("update", args));
        return PyNone.Instance;
    }

    private static object? SetMethod(PySet set, string name, object?[] args) => name switch
    {
        "add" => AddToSet(set, args),
        "remove" => RemoveFromSet(set, args),
        "discard" => DiscardFromSet(set, args),
        "union" => set.Union(AsSet(Single("union", args))),
        "intersection" => set.Intersect(AsSet(Single("intersection", args))),
        "difference" => set.Difference(AsSet(Single("difference", args))),
        _ => throw MissingAttribute("set", name),
    };

    private static object? AddToSet(PySet set, object?[] args)
    {
        set.Add(Single("add", args));
        return PyNone.Instance;
    }

    private static object? RemoveFromSet(PySet set, object?[] args)
    {
        set.Remove(Single("remove", args));
        return PyNone.Instance;
    }

    private static object? DiscardFromSet(PySet set, object?[] args)
    {
        set.Discard(Single("discard", args));
        return PyNone.Instance;
    }

    private static PySet AsSet(object? value) => value switch
    {
        PySet set => set,
        PyFrozenSet frozen => new PySet(frozen.Items),
        _ => new PySet(ToSequence(value)),
    };

    private static object? ByteArrayMethod(PyByteArray array, string name, object?[] args) => name switch
    {
        "append" => AppendByte(array, args),
        "extend" => ExtendByteArray(array, args),
        "decode" => PyEncoding.Decode([.. array.Bytes], args.Length > 0 ? AsText(args[0]) : "utf-8"),
        _ => throw MissingAttribute("bytearray", name),
    };

    private static object? AppendByte(PyByteArray array, object?[] args)
    {
        array.Append(Single("append", args));
        return PyNone.Instance;
    }

    private static object? ExtendByteArray(PyByteArray array, object?[] args)
    {
        array.Extend(Single("extend", args));
        return PyNone.Instance;
    }

    private static object? BytesMethod(PyBytes bytes, string name, object?[] args) => name switch
    {
        "decode" => bytes.Decode(args.Length > 0 ? AsText(args[0]) : "utf-8"),
        "hex" => Convert.ToHexString(bytes.Bytes).ToLowerInvariant(),
        _ => throw MissingAttribute("bytes", name),
    };

    private static object? StringMethod(string text, string name, object?[] args) => name switch
    {
        "encode" => new PyBytes(PyEncoding.Encode(text, args.Length > 0 ? AsText(args[0]) : "utf-8")),
        "upper" => text.ToUpperInvariant(),
        "lower" => text.ToLowerInvariant(),
        "strip" => text.Trim(),
        "split" => SplitText(text, args),
        "join" => JoinText(text, Single("join", args)),
        "startswith" => text.StartsWith(AsText(Single("startswith", args)), StringComparison.Ordinal),
        "endswith" => text.EndsWith(AsText(Single("endswith", args)), StringComparison.Ordinal),
        _ => throw MissingAttribute("str", name),
    };

    private static object SplitText(string text, object?[] args)
    {
        if (args.Length == 0)
        {
            return new PyList(Array.ConvertAll(
                text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries),
                part => (object?)part
            ));
        }
        string separator = AsText(args[0]);
        return new PyList(Array.ConvertAll(
            text.Split(separator, StringSplitOptions.None),
            part => (object?)part
        ));
    }

    private static object JoinText(string separator, object? value)
    {
        object?[] items = ToSequence(value);
        var parts = new string[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            parts[i] = items[i] is string s
                ? s
                : throw new PyTypeError("sequence item: expected str instance");
        }
        return string.Join(separator, parts);
    }

    private static string AsText(object? value) =>
        value as string
        ?? throw new PyTypeError($"expected str, got '{PyRepr.TypeName(value)}'");

    private static PyAttributeError MissingAttribute(string type, string name) =>
        new($"'{type}' object has no attribute '{name}'");

    // ───────────────────────── 内部工具 ─────────────────────────

    private static object? GetString(string text, object? key)
    {
        if (key is PySlice slice)
        {
            var (start, _, step, count) = slice.Indices(text.Length);
            var builder = new StringBuilder((int)count);
            long index = start;
            for (long i = 0; i < count; i++, index += step)
            {
                builder.Append(text[(int)index]);
            }
            return builder.ToString();
        }
        long position = ToIndex(key, "string");
        long normalized = position < 0 ? position + text.Length : position;
        if (normalized < 0 || normalized >= text.Length)
        {
            throw new PyIndexError("string index out of range");
        }
        return text[(int)normalized].ToString();
    }

    private static long ToIndex(object? value, string context) => value switch
    {
        bool b => b ? 1 : 0,
        long l => l,
        int i => i,
        BigInteger big when big >= long.MinValue && big <= long.MaxValue => (long)big,
        _ => throw new PyTypeError(
            $"{context} indices must be integers or slices, not {PyRepr.TypeName(value)}"
        ),
    };

    private static bool ContainsBytes(ReadOnlySpan<byte> haystack, object? needle)
    {
        if (needle is long or int)
        {
            byte target = PyEncoding.ToByte(needle);
            return haystack.IndexOf(target) >= 0;
        }
        ReadOnlySpan<byte> pattern = PyEncoding.ToBytes(needle);
        return haystack.IndexOf(pattern) >= 0;
    }

    private static object?[] KeysOf(PyDict dict)
    {
        var keys = new object?[dict.Count];
        for (int i = 0; i < dict.Count; i++)
        {
            keys[i] = dict.Keys[i];
        }
        return keys;
    }

    private static object?[] Chars(string text)
    {
        var items = new object?[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            items[i] = text[i].ToString();
        }
        return items;
    }

    private static object?[] MapBytes(ReadOnlySpan<byte> bytes)
    {
        var items = new object?[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            items[i] = (long)bytes[i];
        }
        return items;
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
/// 版本号用于「迭代期间修改」检测——遍历开始时记下版本，取下一个元素时发现变化就抛
/// <c>RuntimeError</c>，而不是给出不确定的遍历结果。
/// </remarks>
public sealed class PyDictView
{
    private readonly PyDict _dict;
    private readonly long _version;
    private int _cursor;

    /// <summary>视图种类。</summary>
    public PyDictViewKind Kind { get; }

    /// <summary>构造视图。</summary>
    /// <param name="dict">被观察的字典。</param>
    /// <param name="kind">视图种类。</param>
    public PyDictView(PyDict dict, PyDictViewKind kind)
    {
        _dict = dict;
        Kind = kind;
        _version = dict.Version;
    }

    /// <summary>当前元素个数（活的）。</summary>
    public int Count => _dict.Count;

    /// <summary>取下一个元素；到达末尾返回 <c>false</c>。</summary>
    /// <param name="value">取到的元素。</param>
    public bool TryNext(out object? value)
    {
        if (_dict.Version != _version)
        {
            throw new PyRuntimeError("dictionary changed size during iteration");
        }
        if (_cursor >= _dict.Count)
        {
            value = null;
            return false;
        }
        object? key = _dict.Keys[_cursor++];
        value = Kind switch
        {
            PyDictViewKind.Keys => key,
            PyDictViewKind.Values => _dict.Get(key),
            _ => new PyTuple([key, _dict.Get(key)]),
        };
        return true;
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
        var items = new object?[_dict.Count];
        int index = 0;
        foreach (var (key, value) in _dict.Entries())
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
