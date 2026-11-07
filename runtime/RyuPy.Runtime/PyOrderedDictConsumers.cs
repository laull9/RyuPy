// OrderedDict 的消费者边界：公开复制使用重排后的顺序；显式 dict 根只读使用原始插入序。
// 禁止把 OrderedDict 加入通用 raw 布局识别，以免其它消费者无意丢失双顺序语义。
namespace RyuPy.Runtime;

internal static class PyOrderedDictConsumers
{
    internal static void CopyInto(PyDict target, PyOrderedDict source)
    {
        // 先验证目标，避免已有用户键在第一项提交时触发未支持的 hash/eq 回调。
        PyDictProtocols.RequireScalarContents(target);
        foreach (var (key, value) in source.Entries()) target.Set(key, value);
    }

    internal static object? Root(PyOrderedDict self, string name, object?[] args, string[] names, object?[] values)
    {
        Validate(name, args, names);
        switch (name)
        {
            case "copy": return self.PublicCopy();
            case "get": case "keys": case "items": case "values":
                return PyDictRootMethods.Invoke(self.Raw, name, args, names, values);
            case "__repr__": return PyRepr.DictRaw(self, self.Raw);
            case "__str__": return PyRepr.ReprObject(self);
            case "__len__": case "__iter__": case "__reversed__":
            case "__getitem__": case "__contains__": case "__eq__": case "__ne__":
                return PyDictProtocols.Invoke(name, self.Raw, args);
            default: throw PyOrderedDict.Unsupported("dict." + name);
        }
    }

    // 参数错误先于暂缓域错误；只验证签名，不消费来源，不对临时容器执行根写入。
    private static void Validate(string name, object?[] args, string[] names)
    {
        if (name is "update" or "__init__")
        {
            if (args.Length > 1) throw Error($"{(name == "update" ? "update" : "dict")} expected at most 1 argument, got {args.Length}");
            return;
        }
        if (PyDictRootMethods.Has(name))
        {
            if (names.Length != 0) throw Error($"dict.{name}() takes no keyword arguments");
            int min = name is "get" or "pop" or "setdefault" or "fromkeys" ? 1 : 0;
            int max = min == 1 ? 2 : 0;
            if (args.Length < min) throw Error($"{name} expected at least {min} argument, got {args.Length}");
            if (args.Length > max) throw Error(max == 0 ? $"dict.{name}() takes no arguments ({args.Length} given)"
                : $"{name} expected at most {max} arguments, got {args.Length}");
            return;
        }
        if (name is "__getitem__" or "__contains__" or "__reversed__")
        {
            if (names.Length != 0) throw Error($"dict.{name}() takes no keyword arguments");
            int count = name == "__reversed__" ? 0 : 1;
            if (args.Length != count) throw Error(count == 0 ? $"dict.{name}() takes no arguments ({args.Length} given)"
                : $"dict.{name}() takes exactly one argument ({args.Length} given)");
            return;
        }
        if (names.Length != 0) throw Error($"wrapper {name}() takes no keyword arguments");
        int required = name is "__str__" or "__repr__" or "__len__" or "__iter__" ? 0 : name == "__setitem__" ? 2 : 1;
        if (args.Length != required) throw Error($"{(name == "__setitem__" ? " " : "")}expected {required} argument{(required == 1 ? "" : "s")}, got {args.Length}");
    }
    private static PyTypeError Error(string message) => PyOrderedDict.Error(new PyTypeError(message));
}
