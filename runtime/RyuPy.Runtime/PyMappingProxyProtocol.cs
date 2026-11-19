namespace RyuPy.Runtime;

/// <summary>
/// mappingproxy 的输入域与只读委托：精确 dict 保直接路径，其他来源不绕过自己的 Python 方法。
/// 验证只查载体或类槽存在性，绝不为了判断可接受而调用用户的 getitem/len/iter/keys。
/// </summary>
internal static class PyMappingProxyProtocol
{
    internal static void Validate(object? mapping)
    {
        switch (mapping)
        {
            case PyDict or PyDictInstance or PyDefaultDict or PyCounter or PyMappingProxy:
            case string or PyStringInstance:
                return;
            case PyRange:
                // CPython 接受 range；本体内容协议未闭，不能谎报为非法 mapping。
                throw PyMappingProxy.Unsupported("over range");
            case PyBaseObject or PyList or PyTuple:
                throw Invalid(mapping);
            case PyInstance instance when instance.Class.TryLookup("__getitem__", out _, out _):
                // __getitem__ = None 也建立槽；实际读取才报告 None 不可调用。
                return;
            case PyNativeObject:
                // 未验收 native 不做 GetAttr 探测，避免构造执行潜在用户操作。
                throw PyMappingProxy.Unsupported("over this native object");
            default:
                throw Invalid(mapping);
        }
    }

    private static PyBaseException Invalid(object? mapping) => PyMappingProxy.Error(new PyTypeError(
        $"mappingproxy() argument must be a mapping, not {PyRepr.TypeName(mapping)}"));

    internal static object? Item(object mapping, object? key) =>
        mapping is PyDict dictionary ? dictionary.Get(key) : PyOps.GetItem(mapping, key);

    internal static object? Get(object mapping, object? key, object? fallback) =>
        mapping is PyDict dictionary ? dictionary.GetOrDefault(key, fallback)
            : PyObject.InvokeMethod(mapping, "get", [key, fallback], [], []);

    internal static object? Method(object mapping, string name)
    {
        if (mapping is not PyDict dictionary)
            return PyObject.InvokeMethod(mapping, name, [], [], []);
        return name switch
        {
            "keys" => new PyDictView(dictionary, PyDictViewKind.Keys),
            "values" => new PyDictView(dictionary, PyDictViewKind.Values),
            "items" => new PyDictView(dictionary, PyDictViewKind.Items),
            "copy" => dictionary.Copy(),
            _ => throw PyMappingProxy.Unsupported(name),
        };
    }
}
