namespace RyuPy.Runtime;

/// <summary>下标读取入口：容器协议与有限泛型类型订阅共用MIR入口，写入/删除仍在集合操作模块。</summary>
public static partial class PyOps
{
    /// <summary><c>obj[key]</c>。</summary>
    /// <param name="obj">容器。</param>
    /// <param name="key">下标或键。</param>
    public static object? GetItem(object? obj, object? key) => obj switch
    {
        PyClass { IsTypingGenericRoot: true } => PyTypingGeneric.Subscribe(key),
        PyClass { IsTypingGenericFamily: true } => throw PyTypingGeneric.Unsupported("user Generic class subscription is not supported yet"),
        PyClass cls when PyEnum.IsEnumClass(cls) => PyEnum.GetItem(cls, key),
        PyClass cls when cls.IsTypingIoFamily => PyTypingIo.Subscribe(cls),
        PyType type when type.Name is "list" or "dict" or "tuple" or "set" or "frozenset" => PyTyping.SubscribeNative(type, key),
        PyNativeType type when ReferenceEquals(type, PyOrderedDict.Type) => throw PyOrderedDict.Unsupported("type subscription"),
        PyClass type when PyAbc.IsAbc(type) => PyTyping.SubscribeNative(type, key),
        PyList list => list.GetItem(key!),
        PyTuple tuple => tuple.GetItem(key!),
        PyDict dict => dict.Get(key),
        string text => GetString(text, key),
        PyListInstance list => PyListProtocols.Item(list, key),
        PyDictInstance dict => PyDictSubclassDispatch.Item(dict, key),
        PyStringInstance text => PyStringProtocols.Item(text, key),
        PyBytes bytes => key is PySlice slice ? bytes.GetSlice(slice) : bytes.GetItem(ToIndex(key, "bytes")),
        PyByteArray array => key is PySlice slice2
            ? array.GetSlice(slice2)
            : array.GetItem(ToIndex(key, "bytearray")),
        _ when PyObject.TryMethod1(obj, "__getitem__", key, out object? item) => item,
        _ => throw new PyTypeError($"'{PyRepr.TypeName(obj)}' object is not subscriptable"),
    };

}
