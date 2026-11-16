using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>常用内建容器浅/深复制；身份 memo 保环与共享引用，不执行反射或未实现的 pickle 协议。</summary>
public static class PyCopy
{
    /// <summary>公开已实现签名；外部 memo 与用户重建协议单独拒绝，不静默丢失状态。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["copy"] = PyNativeFunctions.Create("copy", ["x"], [null], 1, args => Shallow(args[0])),
        ["deepcopy"] = PyNativeFunctions.Create("deepcopy", ["x", "memo"], [null, PyNone.Instance], 1, args =>
        {
            if (args[1] is not (null or PyNone))
                throw new PyNotImplementedError("deepcopy with an external memo is not supported");
            return Deep(args[0], new Dictionary<object, object?>(ReferenceEqualityComparer.Instance), 0);
        }),
    };

    private static bool Atomic(object? value) => value is null or PyNone or PyNotImplemented or bool or int or long or BigInteger
        or double or string or PyBytes or PyFunction or PyBuiltinFunction or PyType or PyClass or PyNativeType;

    private static object? Shallow(object? value)
    {
        if (value is PyNamedTupleInstance) throw PyNamedTupleLayout.Error(Unsupported(value));
        if (value is PyListInstance) throw PyListLayout.Own(Unsupported(value));
        if (value is PyDictInstance) throw PyDictLayout.Own(Unsupported(value));
        if (Atomic(value) || value is PyTuple) return value;
        return value switch
        {
            PyList list => new PyList(list.Items),
            PyDict dictionary => CopyDictionary(dictionary),
            _ => throw Unsupported(value),
        };
    }

    private static PyDict CopyDictionary(PyDict dictionary)
    {
        // 现有 dict.Copy 会重新 hash；可执行用户 hash 的键必须先拒绝，避免复制引入回调。
        foreach (object? key in dictionary.Keys)
            if (!SafeKey(key)) throw new PyNotImplementedError("copy of dict with user hash keys is not supported");
        return dictionary.Copy();
    }

    private static bool SafeKey(object? key, int depth = 0)
    {
        if (key is null or PyNone or bool or int or long or BigInteger or double or string or PyBytes) return true;
        if (key is not PyTuple tuple) return false;
        if (depth >= 256) throw new PyRecursionError("maximum recursion depth exceeded while copying an object");
        foreach (object? item in tuple.Items) if (!SafeKey(item, depth + 1)) return false;
        return true;
    }

    private static object? Deep(object? value, Dictionary<object, object?> memo, int depth)
    {
        // 必须在tuple快路及其元素的递归复制之前拒绝，不能先浅化或剥掉生成类身份。
        if (value is PyNamedTupleInstance) throw PyNamedTupleLayout.Error(Unsupported(value));
        if (value is PyListInstance) throw PyListLayout.Own(Unsupported(value));
        if (value is PyDictInstance) throw PyDictLayout.Own(Unsupported(value));
        if (Atomic(value)) return value;
        if (memo.TryGetValue(value!, out object? previous)) return previous;
        if (depth >= 256) throw new PyRecursionError("maximum recursion depth exceeded while copying an object");
        switch (value)
        {
            case PyList list:
                var result = new PyList();
                memo[value] = result;
                foreach (object? item in list.Items) result.Items.Add(Deep(item, memo, depth + 1));
                return result;
            case PyDict dictionary:
                var copy = new PyDict();
                memo[value] = copy;
                foreach (var entry in dictionary.Entries())
                {
                    // Python 赋值语句先求右值，再求下标键；保留这个回调顺序。
                    object? copiedValue = Deep(entry.Value, memo, depth + 1);
                    copy.Set(Deep(entry.Key, memo, depth + 1), copiedValue);
                }
                return copy;
            case PyTuple tuple:
                var items = new object?[tuple.Length];
                bool changed = false;
                for (int index = 0; index < items.Length; index++)
                {
                    items[index] = Deep(tuple.Items[index], memo, depth + 1);
                    changed |= !ReferenceEquals(items[index], tuple.Items[index]);
                }
                // tuple 可经可变子容器回到自己；递归内层已构建的结果优先，不能造第二份环。
                if (memo.TryGetValue(value, out previous)) return previous;
                object frozen = changed ? new PyTuple(items) : tuple;
                memo[value] = frozen;
                return frozen;
            default:
                throw Unsupported(value);
        }
    }

    private static PyNotImplementedError Unsupported(object? value) =>
        new($"copy protocol for '{PyRepr.TypeName(value)}' is not supported");
}
