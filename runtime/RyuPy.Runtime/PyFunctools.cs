// functools 首批入口与包装元数据：显式浅复制指定属性，最后写 __wrapped__，不伪造未实现元数据。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>函数工具模块的公开注册与共享包装规则。</summary>
public static class PyFunctools
{
    private static readonly object MissingInitial = new();
    internal static readonly PyTuple Assignments = new(["__module__", "__name__", "__qualname__", "__doc__", "__annotations__", "__type_params__"]);
    internal static readonly PyTuple Updates = new(["__dict__"]);
    /// <summary>创建首批 functools API。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var update = PyNativeFunctions.Create("update_wrapper", ["wrapper", "wrapped", "assigned", "updated"],
            [null, null, Assignments, Updates], 2, a => UpdateWrapper(a[0], a[1], a[2], a[3]));
        return new()
        {
            ["partial"] = new PyNativeType("partial", "functools", Partial, x => x is PyPartial),
            ["reduce"] = PyNativeFunctions.Create("reduce", ["function", "iterable", "initial"],
                [null, null, MissingInitial], 2, a => Reduce(a[0], a[1], a[2]), ["posonly", "posonly", "posonly"]),
            ["update_wrapper"] = update,
            ["wraps"] = PyNativeFunctions.Create("wraps", ["wrapped", "assigned", "updated"],
                [null, Assignments, Updates], 1, a => new PyPartial(update, [],
                    ["wrapped", "assigned", "updated"], [a[0], a[1], a[2]])),
            ["WRAPPER_ASSIGNMENTS"] = Assignments, ["WRAPPER_UPDATES"] = Updates,
            ["lru_cache"] = PyNativeFunctions.Create("lru_cache", ["maxsize", "typed"], [128L, false], 0,
                a => PyLruCache.Decorator(a[0], a[1])),
        };
    }
    private static object Partial(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw new PyTypeError("type 'partial' takes at least one argument");
        return new PyPartial(args[0], args[1..], names, values);
    }
    internal static bool IsCallable(object? value) => (bool)PyOps.CallBuiltin("callable", [value], [], [])!;
    private static object? Reduce(object? function, object? source, object? initial)
    {
        var iterator = PyIter.Create(source); object? result = initial;
        if (ReferenceEquals(initial, MissingInitial))
        {
            if (!iterator.MoveNext()) throw new PyTypeError("reduce() of empty iterable with no initial value");
            result = iterator.Current();
        }
        while (iterator.MoveNext()) result = PyOps.CallValue(function, [result, iterator.Current()], [], []);
        return result;
    }
    internal static object? UpdateWrapper(object? wrapper, object? wrapped, object? assigned = null, object? updated = null)
    {
        var attributes = PyIter.Create(assigned ?? Assignments);
        while (attributes.MoveNext())
        {
            string name = attributes.Current() as string ?? throw new PyTypeError("attribute name must be string");
            object? value;
            try { value = PyObject.GetAttr(wrapped, name); }
            catch (PyAttributeError) { continue; }
            PyObject.SetAttr(wrapper, name, value);
        }
        var updates = PyIter.Create(updated ?? Updates);
        while (updates.MoveNext())
        {
            string name = updates.Current() as string ?? throw new PyTypeError("attribute name must be string");
            object? target = PyObject.GetAttr(wrapper, name); object? source;
            try { source = PyObject.GetAttr(wrapped, name); }
            catch (PyAttributeError) { source = new PyDict(); }
            PyOps.CallValue(PyOps.PrepareMethod(target, "update"), [source], [], []);
        }
        PyObject.SetAttr(wrapper, "__wrapped__", wrapped);
        return wrapper;
    }
}
