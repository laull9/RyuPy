using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>dir 的真实用户类目录与自定义 __dir__ 协议；不虚构尚未实现的 object 属性。</summary>
public static class PyDir
{
    /// <summary>单对象位置调用；零参帧目录与未实现对象类别明确拒绝。</summary>
    public static object Call(object?[] args, string[] names, object?[] values)
    {
        if (names.Length != 0) throw new PyTypeError("dir() takes no keyword arguments");
        if (args.Length == 0) throw new PyNotImplementedError("dir() without an object is not supported");
        if (args.Length != 1) throw new PyTypeError($"dir expected at most 1 argument, got {args.Length}");
        if (args[0] is PyInstance instance)
        {
            // 特殊方法从类型查找，不能用实例上同名属性替代，也不为枚举读取property。
            if (instance.Class.TryLookup("__dir__", out PyClass owner, out object? raw))
            {
                if (raw is PyProperty property)
                {
                    if (property.Getter is null or PyNone) throw new PyAttributeError("property '__dir__' has no getter");
                    object? callable = PyObject.InvokeBound(property.Getter, owner, instance, [], [], []);
                    return Sort(PyOps.CallValue(callable, [], [], []));
                }
                if (PyObject.TryMethod(instance, "__dir__", [], out object? supplied)) return Sort(supplied);
            }
            var found = ClassNames(instance.Class);
            foreach (string name in instance.Class.AssignedInstanceNames(instance)) found.Add(name);
            return Sort(new PyList(found));
        }
        if (args[0] is PyClass cls) return Sort(new PyList(ClassNames(cls)));
        throw new PyNotImplementedError($"dir() for {PyRepr.TypeName(args[0])} is not supported");
    }

    private static HashSet<string> ClassNames(PyClass cls)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (PyClass? current = cls; current is not null; current = current.BaseClass)
            foreach (string name in current.Attributes.Keys) names.Add(name);
        return names;
    }
    private static object Sort(object? value)
    {
        object?[] items = PyOps.ToSequence(value);
        bool strings = true;
        foreach (object? item in items) if (item is not string) { strings = false; break; }
        if (!strings) return PyOps.CallBuiltin("sorted", [new PyList(items)], [], [])!;
        // CLR ordinal 按 UTF-16 单元排序；目录字符串必须按 Python 码点顺序。
        // 原序号作平局键，保持相等字符串对象的原顺序，不依赖 CLR sort 的稳定性。
        var ordered = new (string Value, int Order)[items.Length];
        for (int i = 0; i < items.Length; i++) ordered[i] = ((string)items[i]!, i);
        Array.Sort(ordered, static (a, b) =>
        {
            int comparison = CompareNames(a.Value, b.Value);
            return comparison == 0 ? a.Order.CompareTo(b.Order) : comparison;
        });
        var result = new PyList();
        foreach (var item in ordered) result.Append(item.Value);
        return result;
    }

    private static int CompareNames(string left, string right)
    {
        int a = 0, b = 0;
        while (a < left.Length && b < right.Length)
        {
            int x = Point(left, ref a), y = Point(right, ref b);
            if (x != y) return x.CompareTo(y);
        }
        return (left.Length - a).CompareTo(right.Length - b);
    }
    private static int Point(string text, ref int index)
    {
        char first = text[index++];
        if (char.IsHighSurrogate(first) && index < text.Length && char.IsLowSurrogate(text[index]))
            return char.ConvertToUtf32(first, text[index++]);
        return first;
    }
}
