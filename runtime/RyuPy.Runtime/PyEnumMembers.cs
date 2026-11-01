using System;

namespace RyuPy.Runtime;

/// <summary>Enum.__members__ 的只读活映射；不实现 dict 派生标记，避免误接受 JSON 或可变映射协议。</summary>
public sealed class PyEnumMembers : PyNativeObject
{
    /// <summary>仅运行时复制入口可访问底层字典；Python 属性不暴露可变对象。</summary>
    internal PyDict Dictionary { get; }
    internal PyEnumMembers(PyDict members) => Dictionary = members;
    /// <inheritdoc />
    public override string TypeName => "mappingproxy";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__iter__" => Method(name, [], [], 0, _ => PyIter.Create(Dictionary)),
        "__len__" => Method(name, [], [], 0, _ => (long)Dictionary.Count),
        "__getitem__" => Method(name, ["key"], [null], 1, a => Dictionary.Get(a[0])),
        "__contains__" => Method(name, ["key"], [null], 1, a => Dictionary.Contains(a[0])),
        "get" => Method(name, ["key", "default"], [null, PyNone.Instance], 1,
            a => Dictionary.TryGet(a[0], out object? found) ? found : a[1]),
        "keys" => Method(name, [], [], 0, _ => new PyDictView(Dictionary, PyDictViewKind.Keys)),
        "values" => Method(name, [], [], 0, _ => new PyDictView(Dictionary, PyDictViewKind.Values)),
        "items" => Method(name, [], [], 0, _ => new PyDictView(Dictionary, PyDictViewKind.Items)),
        "copy" => Method(name, [], [], 0, _ => Copy()),
        "__eq__" => Method(name, ["other"], [null], 1, a => a[0] switch
        {
            PyEnumMembers other => PyHash.Equal(Dictionary, other.Dictionary),
            PyDict dict => PyHash.Equal(Dictionary, dict),
            _ => PyNotImplemented.Instance,
        }),
        "__hash__" => PyNone.Instance,
        _ => throw Missing(name),
    };
    private static PyFunction Method(string name, string[] args, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, args, defaults, required, body, Array.ConvertAll(args, _ => "posonly"));
    private PyDict Copy()
    {
        var result = new PyDict();
        foreach (var (key, value) in Dictionary.Entries()) result.Set(key, value);
        return result;
    }
    /// <inheritdoc />
    public override string PythonRepr() => "mappingproxy(" + PyRepr.Repr(Dictionary) + ")";
    /// <inheritdoc />
    public override string PythonStr() => PyRepr.Str(Dictionary);
}
