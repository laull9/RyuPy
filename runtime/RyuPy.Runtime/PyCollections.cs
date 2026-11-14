using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>collections 的有限原生导出；类型对象只建立一次，保留类型与实例身份。</summary>
public static class PyCollections
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>模块属性表。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    private static Dictionary<string, object?> Build()
    {
        var dequeFactory = PyNativeFunctions.Create("deque", ["iterable", "maxlen"], [new PyTuple([]), PyNone.Instance], 0,
            args => new PyDeque(args[0], args[1]));
        var deque = new PyNativeType("deque", "collections", (args, names, values) => PyOps.CallValue(dequeFactory, args, names, values), value => value is PyDeque);
        var defaults = new PyNativeType("defaultdict", "collections", DefaultDict, value => value is PyDefaultDict, "dict");
        var counter = new PyNativeType("Counter", "collections", Counter, value => value is PyCounter, "dict");
        return new(StringComparer.Ordinal) { ["OrderedDict"] = PyOrderedDict.Type, ["deque"] = deque, ["defaultdict"] = defaults, ["Counter"] = counter, ["namedtuple"] = PyNamedTupleExport.Instance };
    }
    private static object DefaultDict(object?[] args, string[] names, object?[] values)
    {
        if (args.Length > 2) throw new PyTypeError("defaultdict expected at most 2 arguments");
        var result = new PyDefaultDict(args.Length > 0 ? args[0] : PyNone.Instance);
        if (args.Length == 2) result.UpdateFrom(args[1]);
        for (int i = 0; i < names.Length; i++) result.Dictionary.Set(names[i], values[i]);
        return result;
    }
    private static object Counter(object?[] args, string[] names, object?[] values)
    {
        if (args.Length > 1) throw new PyTypeError("Counter expected at most 1 argument");
        var result = new PyCounter();
        if (args.Length == 1) result.Accumulate(args[0], false);
        var keywords = new PyDict();
        for (int i = 0; i < names.Length; i++) keywords.Set(names[i], values[i]);
        result.Accumulate(keywords, false);
        return result;
    }
}
