// 缓存键只在每次调用构建时计算一次 Python hash；桶索引用缓存 hash，淘汰不重新调用用户代码。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed class PyLruKey
{
    private static readonly object KeywordMarker = new();
    private readonly object?[] _parts;
    private readonly bool _fast;
    internal long Hash { get; }
    internal PyLruKey(object?[] args, string[] names, object?[] values, bool typed)
    {
        // CPython 的单一精确 int/str 快键与一般元组键分开，不能用 1 == 1.0 合并所有调用。
        _fast = !typed && names.Length == 0 && args.Length == 1 && args[0] is long or int or System.Numerics.BigInteger or string;
        if (_fast) { _parts = [args[0]]; Hash = PyHash.Of(args[0]); return; }
        var parts = new List<object?>(args);
        if (names.Length > 0)
        {
            parts.Add(KeywordMarker);
            for (int i = 0; i < names.Length; i++) { parts.Add(names[i]); parts.Add(values[i]); }
        }
        if (typed)
        {
            foreach (object? value in args) parts.Add(PyObject.TypeOf(value));
            foreach (object? value in values) parts.Add(PyObject.TypeOf(value));
        }
        _parts = parts.ToArray(); Hash = PyHash.Of(new PyTuple(_parts));
    }
    internal bool Equal(PyLruKey other)
    {
        if (_fast != other._fast || _parts.Length != other._parts.Length) return false;
        for (int i = 0; i < _parts.Length; i++)
            if (!ReferenceEquals(_parts[i], other._parts[i]) && !PyHash.Equal(_parts[i], other._parts[i])) return false;
        return true;
    }
}
