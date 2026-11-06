// dict 命名根方法及其签名：根调用绕业务覆盖，fromkeys 唯独先实际调用类再逐项写入。
namespace RyuPy.Runtime;

internal static class PyDictRootMethods
{
    internal static bool Has(string name) => name is "clear" or "copy" or "get" or "pop" or "popitem"
        or "setdefault" or "update" or "keys" or "items" or "values" or "fromkeys";
    internal static object? Invoke(object self, string name, object?[] args, string[] names, object?[] values)
    {
        if (name == "update") return PyDictProtocols.CallUpdate(self, args, names, values);
        if (names.Length != 0) throw Error($"dict.{name}() takes no keyword arguments");
        int min = name is "get" or "pop" or "setdefault" or "fromkeys" ? 1 : 0;
        int max = min == 1 ? 2 : 0;
        if (args.Length < min) throw Error($"{name} expected at least {min} argument, got {args.Length}");
        if (args.Length > max)
            throw Error(max == 0 ? $"dict.{name}() takes no arguments ({args.Length} given)"
                : $"{name} expected at most {max} arguments, got {args.Length}");
        if (name == "fromkeys") return FromKeys(self, args);
        PyDict raw = PyDictLayout.RequireRaw(self);
        switch (name)
        {
            case "clear": if (raw.Count != 0) raw.Clear(); return PyNone.Instance;
            case "keys": return new PyDictView(raw, PyDictViewKind.Keys, true, self is PyDictInstance);
            case "values": return new PyDictView(raw, PyDictViewKind.Values, true, self is PyDictInstance);
            case "items": return new PyDictView(raw, PyDictViewKind.Items, true, self is PyDictInstance);
            case "copy": PyDictProtocols.RequireScalarContents(raw); return raw.Copy();
            case "popitem":
                if (self is not PyDictInstance) PyDictProtocols.RequireScalarContents(raw);
                if (raw.Count == 0) throw PyDictLayout.Own(new PyKeyError("popitem(): dictionary is empty"));
                return raw.PopLastScalar();
        }
        // CP 空 pop 不哈希、不检查键；其余查询统一 None 归一且不调用 missing。
        object? fallback = args.Length == 2 ? args[1] : PyNone.Instance;
        if (name == "pop" && raw.Count == 0)
        {
            if (args.Length == 2) return fallback;
            throw PyDictLayout.Own(PyKeyError.ForKey(args[0]));
        }
        PyDictProtocols.RequireScalarKey(args[0]);
        if (self is not PyDictInstance) PyDictProtocols.RequireScalarContents(raw);
        object key = args[0] ?? PyNone.Instance;
        if (name == "get") return raw.GetOrDefault(key, fallback);
        if (name == "setdefault") return raw.SetDefault(key, fallback);
        if (raw.TryGet(key, out object? found)) { raw.Delete(key); return found; }
        if (args.Length == 2) return fallback;
        throw PyDictLayout.Own(PyKeyError.ForKey(key));
    }
    private static object FromKeys(object cls, object?[] args)
    {
        // 实际构造允许 custom init 留下默认键，不能直接分配 payload 跳过用户代码。
        object result = PyOps.CallValue(cls, [], [], [])!;
        object? value = args.Length == 2 ? args[1] : PyNone.Instance;
        IPyIterator cursor = PyIter.Create(args[0]);
        while (cursor.MoveNext())
        {
            object? key = cursor.Current();
            PyDictProtocols.RequireScalarKey(key);
            // fromkeys 是逐项公开写入协议，不是 root update。核心覆盖当前在类验证时拒绝。
            PyOps.SetItem(result, key, value);
        }
        return result;
    }
    private static PyTypeError Error(string message) => PyDictLayout.Own(new PyTypeError(message));
}
