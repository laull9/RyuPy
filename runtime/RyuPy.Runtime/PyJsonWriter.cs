using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>JSON编码器：显式控制Python转义、顺序、键规则与循环检测，避免宿主JSON格式差异。</summary>
internal sealed class PyJsonWriter
{
    private readonly bool _ascii, _skipKeys, _checkCircular, _allowNan, _sortKeys;
    private readonly string? _indent;
    private readonly string _itemSep, _keySep;
    private readonly object? _default;
    private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);

    internal PyJsonWriter(bool ascii, bool skipKeys, bool checkCircular, bool allowNan,
        bool sortKeys, object? indent, object? separators, object? defaultHandler)
    {
        _ascii = ascii; _skipKeys = skipKeys; _checkCircular = checkCircular;
        _allowNan = allowNan; _sortKeys = sortKeys; _default = defaultHandler;
        _indent = indent switch
        {
            null or PyNone => null,
            string text => text,
            long or int or bool or BigInteger => Indent(indent),
            _ => throw new PyTypeError($"can't multiply sequence by non-int of type '{PyRepr.TypeName(indent)}'"),
        };
        _itemSep = _indent is null ? ", " : ","; _keySep = ": ";
        if (separators is not null and not PyNone)
        {
            object?[] parts = separators switch
            {
                PyTuple tuple => tuple.Items,
                PyList list => list.Items.ToArray(),
                PyListInstance list => PyOps.ToSequence(list),
                _ => throw new PyTypeError("separators must be a two-item sequence"),
            };
            if (parts.Length != 2) throw new PyValueError("separators must contain exactly two items");
            _itemSep = parts[0] as string ?? throw new PyTypeError("item separator must be str");
            _keySep = parts[1] as string ?? throw new PyTypeError("key separator must be str");
        }
    }

    internal string Write(object? value) => string.Concat(Chunks(value, 0));

    /// <summary>dump逐块调用write，错误前缀与用户写入副作用不被整串缓冲吞掉。</summary>
    internal void WriteTo(object? value, object? stream)
    {
        foreach (string chunk in Chunks(value, 0))
            PyOps.CallValue(PyObject.GetAttr(stream, "write"), [chunk], [], []);
    }

    private bool Scalar(object? value, out string encoded)
    {
        switch (value)
        {
            case null or PyNone: encoded = "null"; return true;
            case bool flag: encoded = flag ? "true" : "false"; return true;
            case string text: encoded = Quote(text); return true;
            case long or int or BigInteger:
                encoded = Integer(value); return true;
            case double number: encoded = Float(number); return true;
            default: encoded = string.Empty; return false;
        }
    }

    private IEnumerable<string> Chunks(object? value, int depth)
    {
        if (depth > 256) throw new PyRecursionError("maximum recursion depth exceeded while encoding a JSON object");
        if (Scalar(value, out string scalar)) { yield return scalar; yield break; }
        bool marked = _checkCircular && value is not null;
        if (marked && !_active.Add(value!)) throw new PyValueError("Circular reference detected");
        try
        {
            IEnumerable<string> result = value switch
            {
                PyList list => Sequence(list.Items, depth),
                // 循环检测仍使用公开owner，默认槽不可覆盖时可逐项读取真实背板。
                PyListInstance list => Sequence(list.Payload.Items, depth),
                PyTuple tuple => Sequence(tuple.Items, depth),
                PyOrderedDict => throw PyOrderedDict.Unsupported("JSON encoding"),
                PyDict dict => Object(dict, depth),
                PyDictInstance dict => SubclassObject(dict, depth),
                IPyNativeMapping mapping => Object(mapping.Dictionary, depth),
                _ => Default(value, depth),
            };
            foreach (string chunk in result) yield return chunk;
        }
        finally { if (marked) _active.Remove(value!); }
    }

    private IEnumerable<string> Default(object? value, int depth)
    {
        if (_default is null or PyNone)
            throw new PyTypeError($"Object of type {PyRepr.TypeName(value)} is not JSON serializable");
        return Chunks(PyOps.CallValue(_default, [value], [], []), depth + 1);
    }

    private IEnumerable<string> Sequence(IReadOnlyList<object?> items, int depth)
    {
        if (items.Count == 0) { yield return "[]"; yield break; }
        string prefix = "[" + NewLine(depth + 1);
        // list的当前长度逐项读取，保留default/write修改列表后可见的迭代行为。
        for (int index = 0; index < items.Count; index++)
        {
            if (index > 0) prefix = _itemSep + NewLine(depth + 1);
            object? value = items[index];
            if (Scalar(value, out string encoded)) yield return prefix + encoded;
            else
            {
                yield return prefix;
                foreach (string chunk in Chunks(value, depth + 1)) yield return chunk;
            }
        }
        if (_indent is not null) yield return NewLine(depth);
        yield return "]";
    }

    private IEnumerable<string> Object(PyDict dict, int depth)
    {
        if (dict.Count == 0) { yield return "{}"; yield break; }
        yield return "{";
        if (_indent is not null) yield return NewLine(depth + 1);
        // 排序先于字符串化，复用稳定排序且不让.NET Sort包装Python异常。
        IEnumerable<(object? Key, object? Value)> entries = dict.Entries();
        if (_sortKeys)
        {
            var sorted = new List<object?>();
            foreach (var (key, value) in entries) sorted.Add(new PyTuple([key, value]));
            bool stringKeys = true;
            foreach (object? entry in sorted) stringKeys &= ((PyTuple)entry!).Items[0] is string;
            PySort.SortBy(sorted, false, entry => stringKeys
                ? CodePointKey((string)((PyTuple)entry!).Items[0]!) : ((PyTuple)entry!).Items[0]);
            var pairs = new List<(object?, object?)>();
            foreach (object? entry in sorted) pairs.Add((((PyTuple)entry!).Items[0], ((PyTuple)entry).Items[1]));
            entries = pairs;
        }
        bool first = true; long version = dict.Version;
        foreach (var (rawKey, value) in entries)
        {
            if (!_sortKeys && version != dict.Version) throw new PyRuntimeError("dictionary changed size during iteration");
            string? key = rawKey switch
            {
                string text => text, null or PyNone => "null", bool flag => flag ? "true" : "false",
                long or int or BigInteger => Integer(rawKey),
                double number => Float(number),
                _ => null,
            };
            if (key is null)
            {
                if (_skipKeys) continue;
                throw new PyTypeError($"keys must be str, int, float, bool or None, not {PyRepr.TypeName(rawKey)}");
            }
            if (!first) yield return _itemSep + NewLine(depth + 1);
            first = false; yield return Quote(key); yield return _keySep;
            foreach (string chunk in Chunks(value, depth + 1)) yield return chunk;
        }
        if (!_sortKeys && version != dict.Version) throw new PyRuntimeError("dictionary changed size during iteration");
        if (_indent is not null) yield return NewLine(depth);
        yield return "}";
    }

    // dict 派生的 JSON 使用公开 items（不同于 dict(source) 的根存储快路）。
    // 空字典先返回，不调用覆盖；循环集合一直以 Chunks 中的公开 wrapper 为 owner。
    private IEnumerable<string> SubclassObject(PyDictInstance dict, int depth)
    {
        if (dict.Payload.Count == 0) { yield return "{}"; yield break; }
        yield return "{";
        if (_indent is not null) yield return NewLine(depth + 1);
        IEnumerable<(object? Key, object? Value)> entries = SubclassEntries(dict);
        if (_sortKeys)
        {
            var sorted = new List<object?>();
            foreach (var (key, value) in entries) sorted.Add(new PyTuple([key, value]));
            bool stringKeys = true;
            foreach (object? entry in sorted) stringKeys &= ((PyTuple)entry!).Items[0] is string;
            PySort.SortBy(sorted, false, entry => stringKeys
                ? CodePointKey((string)((PyTuple)entry!).Items[0]!) : ((PyTuple)entry!).Items[0]);
            var pairs = new List<(object?, object?)>();
            foreach (object? entry in sorted) pairs.Add((((PyTuple)entry!).Items[0], ((PyTuple)entry).Items[1]));
            entries = pairs;
        }
        bool first = true;
        foreach (var (rawKey, value) in entries)
        {
            string? key = rawKey switch
            {
                string text => text, null or PyNone => "null", bool flag => flag ? "true" : "false",
                long or int or BigInteger => Integer(rawKey), double number => Float(number), _ => null,
            };
            if (key is null)
            {
                if (_skipKeys) continue;
                throw new PyTypeError($"keys must be str, int, float, bool or None, not {PyRepr.TypeName(rawKey)}");
            }
            if (!first) yield return _itemSep + NewLine(depth + 1);
            first = false; yield return Quote(key); yield return _keySep;
            foreach (string chunk in Chunks(value, depth + 1)) yield return chunk;
        }
        if (_indent is not null) yield return NewLine(depth);
        yield return "}";
    }

    private static IEnumerable<(object? Key, object? Value)> SubclassEntries(PyDictInstance dict)
    {
        object? items = PyObject.InvokeMethod(dict, "items", [], [], []);
        IPyIterator iterator = PyMappingProxyIteration.Start(items);
        while (PyMappingProxyIteration.ReadNext(iterator, out object? item))
        {
            var pair = (PyTuple)PyOps.UnpackSequence(item, 2);
            yield return (pair.Items[0], pair.Items[1]);
        }
    }

    // JSON字符串键按码点排序；不能直接用UTF16 ordinal把补充平面排到BMP末尾字符前。
    private static PyTuple CodePointKey(string text)
    {
        var points = new List<object?>();
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            { points.Add((long)char.ConvertToUtf32(text[i], text[i + 1])); i++; }
            else points.Add((long)text[i]);
        }
        return new PyTuple(points.ToArray());
    }

    private static string Integer(object value)
    {
        string result = Convert.ToString(value, CultureInfo.InvariantCulture)!;
        if (result.Length - (result[0] == '-' ? 1 : 0) > 4300)
            throw new PyValueError("Exceeds the limit (4300 digits) for integer string conversion");
        return result;
    }

    private string Float(double value)
    {
        if (double.IsFinite(value)) return PyFormat.FloatRepr(value);
        if (!_allowNan) throw new PyValueError("Out of range float values are not JSON compliant");
        return double.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity";
    }

    private string Quote(string text)
    {
        var _output = new StringBuilder();
        _output.Append('"');
        foreach (char c in text)
        {
            switch (c)
            {
                case '"': _output.Append("\\\""); break;
                case '\\': _output.Append("\\\\"); break;
                case '\b': _output.Append("\\b"); break;
                case '\f': _output.Append("\\f"); break;
                case '\n': _output.Append("\\n"); break;
                case '\r': _output.Append("\\r"); break;
                case '\t': _output.Append("\\t"); break;
                default:
                    if (c < 32 || (_ascii && c >= 127))
                        _output.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else _output.Append(c);
                    break;
            }
        }
        _output.Append('"');
        return _output.ToString();
    }

    private string NewLine(int depth)
    {
        if (_indent is null) return string.Empty;
        var result = new StringBuilder("\n");
        for (int i = 0; i < depth; i++) result.Append(_indent);
        return result.ToString();
    }

    private static string Indent(object value)
    {
        BigInteger count = PyInt.ToBigInteger(value);
        if (count <= 0) return string.Empty;
        if (count > int.MaxValue) throw new PyOverflowError("cannot fit 'int' into an index-sized integer");
        return new string(' ', (int)count);
    }
}
