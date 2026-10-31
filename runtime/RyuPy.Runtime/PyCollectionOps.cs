using System;
using System.Collections.Generic;
using System.Linq;
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
        _ when PyObject.TryMethod(obj, "__getitem__", [key], out object? item) => item,
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
                if (PyObject.TryMethod(obj, "__setitem__", [key, value], out _))
                {
                    return;
                }
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
                if (PyObject.TryMethod(obj, "__delitem__", [key], out _))
                {
                    return;
                }
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
        PyDictView view => view.Contains(value),
        PySet set => set.Contains(value),
        PyFrozenSet frozen => frozen.Items.Contains(value ?? PyNone.Instance),
        string text => value is string needle && text.Contains(needle, StringComparison.Ordinal),
        PyBytes bytes => ContainsBytes(bytes.Span, value),
        PyByteArray array => ContainsBytes([.. array.Bytes], value),
        _ when PyObject.TryMethod(container, "__contains__", [value], out object? found) =>
            Truthy(found),
        _ when container is PyInstance => ToSequence(container).Any(item => PyHash.Equal(item, value)),
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
        PyDictView view => view.Count,
        PySet set => set.Count,
        PyFrozenSet frozen => frozen.Count,
        string text => PyText.Length(text),
        PyBytes bytes => bytes.Count,
        PyByteArray array => array.Count,
        _ when PyObject.TryMethod(value, "__len__", [], out object? length) =>
            PyCast.ToInt64(length),
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
        PyRange range => DrainIterator(new PyRangeIter(range)),
        IPyIterator iterator => DrainIterator(iterator),
        _ when PyObject.TryMethod(value, "__iter__", [], out object? iterator) =>
            ToSequence(PyIter.Create(iterator)),
        _ => throw new PyTypeError($"'{PyRepr.TypeName(value)}' object is not iterable"),
    };

    // ───────────────────────── 内建函数 ─────────────────────────

    /// <summary>内建函数调用（<c>len</c> / <c>list</c> / <c>sorted</c> …）。</summary>
    /// <param name="name">函数名。</param>
    /// <param name="rawArgs">位置实参（可能含 <c>*iterable</c> 标记）。</param>
    /// <param name="rawNames">关键字名；值为 <c>None</c> 表示 <c>**mapping</c>。</param>
    /// <param name="rawValues">关键字值。</param>
    public static object? CallBuiltin(
        string name,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        var (args, kwNames, kwValues) = FlattenArgs(rawArgs, rawNames, rawValues);
        return BuiltinDispatch(name, args, kwNames, kwValues);
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
        return iterator is IPyIterator it && it.MoveNext();
    }

    /// <summary>取迭代器的当前元素。</summary>
    /// <param name="iterator">迭代器。</param>
    public static object? IterValue(object iterator) =>
        iterator is IPyIterator it
            ? it.Current()
            : throw new PyTypeError("IterValue 的参数不是迭代器");

    /// <summary>解包长度校验：元素个数与目标不符时抛 <c>ValueError</c>。</summary>
    /// <param name="length">序列长度。</param>
    /// <param name="required">除星号目标外的目标数量。</param>
    /// <param name="hasStar">目标里是否有 <c>*rest</c>（有则只要求下限）。</param>
    public static void UnpackCheck(long length, long required, bool hasStar)
    {
        if (length < required)
        {
            // 星号形态只要求「至少」：中间那段可以是空列表
            throw new PyValueError(
                hasStar
                    ? $"not enough values to unpack (expected at least {required}, got {length})"
                    : $"not enough values to unpack (expected {required}, got {length})"
            );
        }
        if (!hasStar && length > required)
        {
            // 没有星号时多出来的值必须报错；漏掉这一检查会让 `a, b = [1, 2, 3]`
            // 静默丢弃第三个元素
            throw new PyValueError($"too many values to unpack (expected {required})");
        }
    }

    // ───────────────────────── 方法调用 ─────────────────────────

    /// <summary>方法调用：<c>obj.name(args)</c>。</summary>
    /// <param name="obj">接收者。</param>
    /// <param name="name">方法名。</param>
    /// <param name="rawArgs">位置实参（可能含 <c>*iterable</c> 标记）。</param>
    /// <param name="rawNames">关键字名；值为 <c>None</c> 表示 <c>**mapping</c>。</param>
    /// <param name="rawValues">关键字值。</param>
    public static object? CallMethod(
        object? obj,
        string name,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        var (args, kwNames, kwValues) = FlattenArgs(rawArgs, rawNames, rawValues);
        // 普通实例 / 类 / super 代理：走属性协议得到绑定方法，再统一调用。
        if (obj is PyInstance or PyClass or PySuper or PyModule
            || (obj is PyBaseException exception && exception.PythonClass is not null))
        {
            object? attribute = PyObject.GetAttr(obj, name);
            return CallValue(attribute, args, kwNames, kwValues);
        }
        // 目前只有 list.sort 接受关键字；其余方法收到关键字立即报 TypeError，
        // 避免“多写了参数却被静默忽略”。
        if (kwNames.Length > 0)
        {
            if (obj is PyList list && name == "sort")
            {
                return SortList(list, args, kwNames, kwValues);
            }
            throw new PyTypeError(
                $"'{PyRepr.TypeName(obj)}'.{name}() got an unexpected keyword argument "
                    + $"'{kwNames[0]}'"
            );
        }
        return obj switch
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
    }

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
        "sort" => SortList(list, args, [], []),
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

    private static object? SortList(
        PyList list,
        object?[] args,
        string[] kwNames,
        object?[] kwValues
    )
    {
        RejectUnknownKeywords("list.sort", kwNames, "key", "reverse");
        object? key = KeywordValue(kwNames, kwValues, "key");
        bool reverse = KeywordBool(kwNames, kwValues, "reverse");
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
        // 降序通过反转比较实现，而不是排完再倒序——后者会破坏相等元素的原始顺序
        PySort.SortBy(
            list.Items,
            reverse,
            key is null or PyNone ? null : item => CallValue(key, [item], [], [])
        );
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
        "strip" => StripText(text, args, left: true, right: true),
        "lstrip" => StripText(text, args, left: true, right: false),
        "rstrip" => StripText(text, args, left: false, right: true),
        "split" => SplitText(text, args),
        "join" => JoinText(text, Single("join", args)),
        "replace" => ReplaceText(text, args),
        "find" => FindText(text, args, fromEnd: false, raiseIfMissing: false),
        "rfind" => FindText(text, args, fromEnd: true, raiseIfMissing: false),
        "index" => FindText(text, args, fromEnd: false, raiseIfMissing: true),
        "rindex" => FindText(text, args, fromEnd: true, raiseIfMissing: true),
        "count" => CountText(text, args),
        "startswith" => text.StartsWith(AsText(Single("startswith", args)), StringComparison.Ordinal),
        "endswith" => text.EndsWith(AsText(Single("endswith", args)), StringComparison.Ordinal),
        _ => throw MissingAttribute("str", name),
    };

    /// <summary>`str.strip([chars])`：缺省去掉空白，给定时去掉属于 chars 的字符。</summary>
    private static object StripText(string text, object?[] args, bool left, bool right)
    {
        string? chars = args.Length > 0 && args[0] is not PyNone ? AsText(args[0]) : null;
        int start = 0;
        int end = text.Length;
        if (left)
        {
            while (start < end && IsStrippable(text[start], chars))
            {
                start++;
            }
        }
        if (right)
        {
            while (end > start && IsStrippable(text[end - 1], chars))
            {
                end--;
            }
        }
        return text[start..end];
    }

    private static bool IsStrippable(char ch, string? chars) =>
        chars is null ? char.IsWhiteSpace(ch) : chars.Contains(ch, StringComparison.Ordinal);

    /// <summary>
    /// `str.replace(old, new[, count])`。.NET 的 `Replace` 总是替换全部，
    /// 带 count 时必须自己按出现位置逐个替换。
    /// </summary>
    private static object ReplaceText(string text, object?[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            throw new PyTypeError($"replace expected 2 or 3 arguments, got {args.Length}");
        }
        string oldValue = AsText(args[0]);
        string newValue = AsText(args[1]);
        long count = args.Length == 3 ? ToIndex(args[2], "count") : -1;
        if (count < 0)
        {
            return text.Replace(oldValue, newValue, StringComparison.Ordinal);
        }
        if (oldValue.Length == 0)
        {
            // 空串的替换语义比较特殊；当前子集明确拒绝而不是给出错误结果
            throw new PyValueError("replace() 暂不支持空字符串作为 old");
        }
        var builder = new StringBuilder();
        int start = 0;
        long replaced = 0;
        while (replaced < count)
        {
            int index = text.IndexOf(oldValue, start, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }
            builder.Append(text, start, index - start);
            builder.Append(newValue);
            start = index + oldValue.Length;
            replaced++;
        }
        builder.Append(text, start, text.Length - start);
        return builder.ToString();
    }

    /// <summary>find / rfind / index / rindex 共用；返回值是**码点**下标。</summary>
    private static object FindText(
        string text,
        object?[] args,
        bool fromEnd,
        bool raiseIfMissing
    )
    {
        if (args.Length is < 1 or > 3)
        {
            throw new PyTypeError($"find expected 1 to 3 arguments, got {args.Length}");
        }
        string needle = AsText(args[0]);
        int start = args.Length > 1 ? (int)ToIndex(args[1], "start") : 0;
        int end = args.Length > 2 ? (int)ToIndex(args[2], "end") : text.Length;
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        string window = text[start..end];
        int index = fromEnd
            ? window.LastIndexOf(needle, StringComparison.Ordinal)
            : window.IndexOf(needle, StringComparison.Ordinal);
        if (index < 0)
        {
            if (raiseIfMissing)
            {
                throw new PyValueError("substring not found");
            }
            return -1L;
        }
        // UTF-16 偏移 → 码点下标（与 Python 的 str.find 一致）
        return PyText.Length(text[..(start + index)]);
    }

    private static object CountText(string text, object?[] args)
    {
        string needle = AsText(Single("count", args));
        if (needle.Length == 0)
        {
            return (long)PyText.Length(text) + 1;
        }
        long count = 0;
        int index = 0;
        while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

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
            return PyText.Slice(text, slice);
        }
        return PyText.At(text, ToIndex(key, "string"));
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
        int index = 0;
        foreach (object? key in dict.Keys)
        {
            keys[index++] = key;
        }
        return keys;
    }

    private static object?[] Chars(string text) => PyText.Items(text);

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
