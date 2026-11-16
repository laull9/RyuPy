// 新 dict 根视图的活协议；values 保身份相等，keys/items 的有限相等使用 RichCompareBool。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;

internal static class PyDictViewProtocols
{
    internal static IPyIterator Iter(PyDictView view, bool reverse = false) => new PyDictLayoutIterator(view.Dict, view.Kind, reverse);
    internal static object?[] ToArray(PyDictView view)
    {
        var items = new List<object?>(view.Count);
        IPyIterator iterator = Iter(view);
        while (iterator.MoveNext()) items.Add(iterator.Current());
        return items.ToArray();
    }
    internal static bool Contains(PyDictView view, object? value)
    {
        PyDict raw = view.Dict;
        if (view.Kind == PyDictViewKind.Values)
        {
            // CP values membership 不是外部迭代器：eq 回调后重读存储，但结构重入暂缓。
            long version = raw.Version;
            for (int i = 0; i < raw.SlotCount; i++)
            {
                if (!raw.TryKeyAt(i, out _)) continue;
                bool equal = PyListRootMethods.Equal(raw.ValueAt(i), value);
                if (version != raw.Version)
                    throw PyDictLayout.Own(new PyNotImplementedError("dict view membership with structural mutation is not supported"));
                if (equal) return true;
            }
            return false;
        }
        if (view.Kind == PyDictViewKind.Items)
        {
            if (value is not PyTuple pair || pair.Length != 2) return false;
            PyDictProtocols.RequireScalarKey(pair.Items[0]);
            if (!view.ScalarInvariant) PyDictProtocols.RequireScalarContents(raw);
            if (!raw.TryGet(pair.Items[0] ?? PyNone.Instance, out object? found)) return false;
            long version = raw.Version;
            bool equal = PyListRootMethods.Equal(found, pair.Items[1]);
            if (version != raw.Version)
                throw PyDictLayout.Own(new PyNotImplementedError("dict view membership with structural mutation is not supported"));
            return equal;
        }
        PyDictProtocols.RequireScalarKey(value);
        if (!view.ScalarInvariant) PyDictProtocols.RequireScalarContents(raw);
        return raw.Contains(value ?? PyNone.Instance);
    }
    internal static object Compare(int op, PyDictView left, object? other)
    {
        if (op is not (PyOps.Eq or PyOps.Ne))
            throw PyDictLayout.Own(new PyNotImplementedError("dict view ordering and set operations are not supported"));
        if (left.Kind == PyDictViewKind.Values)
        {
            if (other is not PyDictView) return PyNotImplemented.Instance;
            bool same = ReferenceEquals(left, other);
            return op == PyOps.Eq ? same : !same;
        }
        if (other is PySet or PyFrozenSet)
            throw PyDictLayout.Own(new PyNotImplementedError("dict view comparison with sets is not supported"));
        if (other is not PyDictView right || right.Kind == PyDictViewKind.Values)
            return PyNotImplemented.Instance;
        if (left.Kind != right.Kind)
            throw PyDictLayout.Own(new PyNotImplementedError("dict view comparison across different kinds is not supported"));
        bool equal = left.Count == right.Count;
        if (equal)
        {
            IPyIterator cursor = Iter(left);
            while (cursor.MoveNext())
                if (!Contains(right, cursor.Current())) { equal = false; break; }
        }
        return op == PyOps.Eq ? equal : !equal;
    }
}
