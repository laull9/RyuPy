// 可变 Element 的原值字段、真实字典别名和可共享 children；所有遍历均保节点引用。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>没有唯一 parent 指针的 Element 图节点，不通过文本往返或 .NET XML 树重建。</summary>
public sealed class PyXmlElement : PyNativeObject
{
    internal object? Tag;
    internal object? Text = PyNone.Instance;
    internal object? Tail = PyNone.Instance;
    internal PyDict Attributes;
    private List<PyXmlElement>? _children;
    internal int Count => _children?.Count ?? 0;
    internal PyXmlElement ChildAt(int index) => _children![index];
    internal void Append(PyXmlElement child) => (_children ??= []).Add(child);
    internal PyXmlElement(object? tag, PyDict attributes) { Tag = tag; Attributes = attributes; }
    /// <inheritdoc />
    public override string TypeName => "Element";
    /// <inheritdoc />
    public override string PythonRepr() => throw PyXmlElementTree.Unsupported("Element representation");
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "tag" => Tag, "text" => Text, "tail" => Tail, "attrib" => Attributes,
        "__class__" => PyXmlElementTree.ElementType,
        "__len__" => PyXmlElementTree.Wrapper("__len__", 0, _ => (long)Count),
        "__bool__" => PyXmlElementTree.Wrapper("__bool__", 0, _ => PyWarnings.BooleanElement(Count)),
        "__getitem__" => PyXmlElementTree.Wrapper("__getitem__", 1, a => GetItem(a[0])),
        "__repr__" or "__str__" or "__format__" or "__setitem__" or "__delitem__" or "clear" or "extend" or "remove" or "itertext" or "find" or "findall"
            or "findtext" or "iterfind" or "makeelement" or "__copy__" or "__deepcopy__" or "__getstate__"
            or "__setstate__" or "__reduce__" or "__reduce_ex__" => throw PyXmlElementTree.Unsupported("Element." + name),
        "append" or "insert" or "get" or "set" or "keys" or "items" or "iter" =>
            PyXmlElementTree.Raw(name, (a, n, v) => Method(name, a, n, v)),
        _ => throw MissingElement(name),
    };
    /// <inheritdoc />
    public override void SetAttr(string name, object? value)
    {
        switch (name)
        {
            case "tag": Tag = value; return;
            case "text": Text = value; return;
            case "tail": Tail = value; return;
            case "attrib":
                var dictionary = PyXmlElementTree.RequireAttributes(value);
                PyXmlElementTree.GuardKeys(dictionary);
                Attributes = dictionary; return;
            default:
                if (IsMethod(name)) throw PyXmlElementTree.Own(new PyAttributeError($"'xml.etree.ElementTree.Element' object attribute '{name}' is read-only"));
                throw MissingElement(name);
        }
    }
    /// <inheritdoc />
    public override void DelAttr(string name)
    {
        if (name is "tag" or "text" or "tail" or "attrib")
            throw PyXmlElementTree.Own(new PyAttributeError("can't delete element attribute"));
        if (IsMethod(name)) throw PyXmlElementTree.Own(new PyAttributeError($"'xml.etree.ElementTree.Element' object attribute '{name}' is read-only"));
        throw MissingElement(name);
    }
    private static bool IsMethod(string name) => name is "append" or "insert" or "get" or "set" or "keys" or "items" or "iter"
        or "clear" or "extend" or "remove" or "itertext" or "find" or "findall" or "findtext" or "iterfind" or "makeelement";
    private static PyAttributeError MissingElement(string name) => PyXmlElementTree.Own(
        new PyAttributeError($"'xml.etree.ElementTree.Element' object has no attribute '{name}'"));

    private object? Method(string name, object?[] args, string[] names, object?[] values)
    {
        switch (name)
        {
            case "append":
                if (args.Length + names.Length > 1) throw Error($"append() takes at most 1 argument ({args.Length + names.Length} given)");
                if (args.Length != 1) throw Error($"append() takes exactly 1 positional argument ({args.Length} given)");
                Append(PyXmlElementTree.RequireElement(args[0], "append() argument 1")); return null;
            case "insert":
                Positional(name, args, names, 2);
                long position = Index(args[0], false);
                var child = PyXmlElementTree.RequireElement(args[1], "insert() argument 2");
                // C ssize_t 转换已先完成；负数相对当前长度，夹到两端，而非搬移旧父节点。
                long normalized = position < 0 ? Math.Max(0, position + Count) : Math.Min(position, Count);
                (_children ??= []).Insert((int)normalized, child); return null;
            case "get":
                object?[] get = Keyword(name, args, names, values, ["key", "default"], [null, PyNone.Instance], 1);
                PyXmlElementTree.GuardKeys(Attributes);
                return Attributes.GetOrDefault(PyXmlElementTree.Key(get[0]), get[1]);
            case "set":
                Positional(name, args, names, 2);
                PyXmlElementTree.GuardKeys(Attributes);
                Attributes.Set(PyXmlElementTree.Key(args[0]), args[1]); return null;
            case "keys":
            case "items":
                Positional(name, args, names, 0);
                PyXmlElementTree.GuardKeys(Attributes);
                var result = new PyList();
                foreach (var (key, value) in Attributes.Entries()) result.Append(name == "keys" ? key : new PyTuple([key, value]));
                return result;
            default:
                object? filter = Keyword(name, args, names, values, ["tag"], [PyNone.Instance], 0)[0];
                if (filter is not (null or PyNone or string)) throw PyXmlElementTree.Unsupported("Element.iter filter other than None or exact str");
                return new PyXmlPreorderIterator(this, filter is string text && text != "*" ? text : null);
        }
    }

    private static void Positional(string name, object?[] args, string[] names, int count)
    {
        if (names.Length > 0) throw Error($"Element.{name}() takes no keyword arguments");
        if (args.Length != count) throw Error(count == 0 ? $"Element.{name}() takes no arguments ({args.Length} given)"
            : $"{name} expected {count} arguments, got {args.Length}");
    }
    private static object?[] Keyword(string name, object?[] args, string[] names, object?[] values,
        string[] parameters, object?[] defaults, int required)
    {
        int total = args.Length + names.Length;
        if (total > parameters.Length) throw Error($"{name}() takes at most {parameters.Length} argument{(parameters.Length == 1 ? "" : "s")} ({total} given)");
        var bound = (object?[])defaults.Clone();
        var assigned = new bool[parameters.Length];
        for (int i = 0; i < args.Length; i++) { bound[i] = args[i]; assigned[i] = true; }
        for (int i = 0; i < names.Length; i++)
        {
            int index = Array.IndexOf(parameters, names[i]);
            if (index < 0) continue;
            if (assigned[index]) throw Error($"argument for {name}() given by name ('{names[i]}') and position ({index + 1})");
            assigned[index] = true; bound[index] = values[i];
        }
        for (int i = 0; i < required; i++) if (!assigned[i]) throw Error($"{name}() missing required argument '{parameters[i]}' (pos {i + 1})");
        foreach (string keyword in names) if (Array.IndexOf(parameters, keyword) < 0) throw Error($"'{keyword}' is an invalid keyword argument for {name}()");
        return bound;
    }
    private object? GetItem(object? key)
    {
        if (key is PySlice slice)
        {
            GuardSliceField(slice.Step);
            if (slice.Step is not (null or PyNone) && PyInt.ToBigInteger(slice.Step!).IsZero)
                throw PyXmlElementTree.Own(new PyValueError("slice step cannot be zero"));
            GuardSliceField(slice.Start); GuardSliceField(slice.Stop);
            var parts = slice.PublicIndices((long)Count).Items;
            BigInteger start = PyInt.ToBigInteger(parts[0]!), stop = PyInt.ToBigInteger(parts[1]!), step = PyInt.ToBigInteger(parts[2]!);
            var result = new PyList();
            for (BigInteger cursor = start; step.Sign > 0 ? cursor < stop : cursor > stop; cursor += step)
                result.Append(ChildAt((int)cursor));
            return result;
        }
        long index = Index(key, true);
        if (index < 0) index += Count;
        if (index < 0 || index >= Count) throw PyXmlElementTree.Own(new PyIndexError("child index out of range"));
        return ChildAt((int)index);
    }
    private static void GuardSliceField(object? value)
    {
        if (value is null or PyNone || PyInt.IsInt(value)) return;
        if (PyObject.MayHaveMethods(value)) throw PyXmlElementTree.Unsupported("Element slice user __index__");
        throw Error("slice indices must be integers or None or have an __index__ method");
    }
    private static long Index(object? value, bool item)
    {
        if (!PyInt.IsInt(value))
        {
            if (PyObject.MayHaveMethods(value)) throw PyXmlElementTree.Unsupported("Element user __index__");
            throw Error(item ? "element indices must be integers" : $"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer");
        }
        if (PyInt.TryI64(value!, out long result)) return result;
        throw PyXmlElementTree.Own(item ? (PyBaseException)new PyIndexError("cannot fit 'int' into an index-sized integer")
            : new PyOverflowError("Python int too large to convert to C ssize_t"));
    }
    private static PyTypeError Error(string text) => PyXmlElementTree.Own(new PyTypeError(text));
}
