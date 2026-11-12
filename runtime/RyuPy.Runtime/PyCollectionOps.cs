// 容器公共动态入口与方法分派；保留内部查找哨兵，list.index 公共失败单独转 Python 异常。
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
            case PyListInstance list:
                PyListProtocols.SetItem(list, key, value);
                return;
            case PyDictInstance dict:
                PyDictSubclassDispatch.SetItem(dict, key, value);
                return;
            case PyDict dict:
                dict.Set(key, value);
                return;
            case PyByteArray array:
                if (key is PySlice slice) array.SetSlice(slice, value);
                else array.SetItem(ToIndex(key, "bytearray"), value);
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
            case PyListInstance list:
                PyListProtocols.DeleteItem(list, key);
                return;
            case PyDictInstance dict:
                PyDictSubclassDispatch.DeleteItem(dict, key);
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
        PyClass cls when PyEnum.IsEnumClass(cls) => PyEnum.Contains(cls, value),
        PyList list => list.Contains(value),
        PyTuple tuple => tuple.Contains(value),
        PyDict dict => dict.Contains(value),
        PyDictView { UseLayoutProtocol: true } view => PyDictViewProtocols.Contains(view, value),
        PyDictView view => view.Contains(value),
        PySet set => set.Contains(value),
        PyFrozenSet frozen => frozen.Contains(value),
        string text => value is string needle ? text.Contains(needle, StringComparison.Ordinal)
            : value is PyStringInstance sub && text.Contains(sub.Value, StringComparison.Ordinal),
        PyListInstance list => PyListProtocols.Contains(list, value),
        PyDictInstance dict => PyDictProtocols.Contains(dict, value),
        PyStringInstance text => PyStringProtocols.Contains(text, value),
        PyBytes bytes => ContainsBytes(bytes.Span, value),
        PyByteArray array => ContainsBytes([.. array.Bytes], value),
        _ when PyObject.TryMethod1(container, "__contains__", value, out object? found) =>
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
        PyClass cls when PyEnum.IsEnumClass(cls) => PyEnum.Length(cls),
        PyList list => list.Count,
        PyTuple tuple => tuple.Length,
        PyDict dict => dict.Count,
        PyDictView view => view.Count,
        PySet set => set.Count,
        PyFrozenSet frozen => frozen.Count,
        string text => PyText.Length(text),
        PyListInstance list => PyListProtocols.Len(list),
        PyDictInstance dict => PyDictProtocols.Len(dict),
        PyStringInstance text => PyText.Length(text.Value),
        PyBytes bytes => bytes.Count,
        PyByteArray array => array.Count,
        _ when PyObject.TryMethod(value, "__len__", [], out object? length) =>
            LengthResult(length, "len()"),
        _ => throw LengthError(new PyTypeError(
            $"object of type '{PyRepr.TypeName(value)}' has no len()"
        )),
    };

    /// <summary><c>a is b</c>：引用身份比较。</summary>
    /// <param name="a">左值。</param>
    /// <param name="b">右值。</param>
    public static bool Is(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }
        // CPython 里 True / False 是单例、[-5, 256] 的整数被缓存：这些值的 `is` 等价于相等。
        // 装箱值可能来自运行时任意位置（不一定经过 BoxBool / PyInt.Box），这里按值兜底，
        // 保证 `flag is True`、`x is 0` 与 CPython 一致；其它数值仍按装箱对象身份比较。
        return (a, b) switch
        {
            (bool x, bool y) => x == y,
            (long x, long y) => x == y && PyInt.IsSmall(x),
            _ => false,
        };
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
        PyDictView { UseLayoutProtocol: true } view => PyDictViewProtocols.ToArray(view),
        PyDictView view => view.ToArray(),
        PySet set => [.. set.Items],
        PyFrozenSet frozen => [.. frozen.Items],
        string text => Chars(text),
        PyListInstance list => DrainIterator(PyListProtocols.Iter(list)),
        PyDictInstance dict => DrainIterator(PyDictSubclassDispatch.Iter(dict)),
        PyStringInstance text => Chars(text.Value),
        PyBytes bytes => MapBytes(bytes.Span),
        PyByteArray array => MapBytes([.. array.Bytes]),
        PyRange range => DrainIterator(new PyRangeIter(range)),
        IPyIterator iterator => DrainIterator(iterator),
        // proxy 公开返回 raw iterator；不能让旧 fallback 对它重复调用 __iter__。
        PyMappingProxy proxy => PyMappingProxyIteration.Materialize(proxy),
        PyClass cls when PyEnum.IsEnumClass(cls) => DrainIterator(PyEnum.Iterate(cls)),
        // sealed Element 没有公开 __iter__；仅此冷分支复用 for/iter 的活 children 游标。
        PyXmlElement element => DrainIterator(PyIter.Create(element)),
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

    private static object? ListMethod(PyList list, string name, object?[] args) => name switch
    {
        "append" => Append(list, args),
        "extend" => Extend(list, args),
        "insert" => Insert(list, args),
        "remove" => RemoveItem(list, args),
        "index" => ListIndex(list, Single("index", args)),
        "count" => CountItem(list, args),
        "clear" or "pop" or "reverse" or "copy" => CallListCheckedMethod(list, name, args, EmptyNames),
        "sort" => SortList(list, args, [], []),
        _ => throw MissingAttribute("list", name),
    };

    private static long ListIndex(PyList list, object? value)
    {
        long index = list.IndexOf(value);
        if (index >= 0) return index;
        // repr 仍是用户可观察的回调；只为本层新建的缺项错误准备现场。
        throw (PyBaseException)PyErrors.PrepareRaise(new PyValueError($"{PyRepr.Repr(value)} is not in list"));
    }

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
        "update" => CallDictUpdate(dict, args, [], []),
        "pop" => dict.Pop(args[0], args.Length > 1, args.Length > 1 ? args[1] : PyNone.Instance),
        "setdefault" => dict.SetDefault(args[0], args.Length > 1 ? args[1] : PyNone.Instance),
        "clear" => Void(dict.Clear),
        "copy" => dict.Copy(),
        _ => throw MissingAttribute("dict", name),
    };

    private static object? SetMethod(PySet set, string name, object?[] args) => name switch
    {
        "clear" => CallSetClear(set, args, []),
        "add" => AddToSet(set, args),
        "remove" => RemoveFromSet(set, args),
        "discard" => DiscardFromSet(set, args),
        _ when HasSetFamilyMethod(name) => CallSetFamilyMethod(set, name, args, []),
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
            // str 与 bytes/list 的原生错误格式不同；只改失败分支，不动整数和切片路径。
            context == "string" ? $"string indices must be integers, not '{PyRepr.TypeName(value)}'"
                : $"{context} indices must be integers or slices, not {PyRepr.TypeName(value)}"
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
