namespace RyuPy.Runtime;

/// <summary>提前绑定的内建方法：固定接收者与类型入口，不再执行 Python 属性查找。</summary>
public sealed class PyBuiltinMethod
{
    private readonly object _receiver;
    private readonly string _name;
    private readonly PyBuiltinMethodCall _call;

    // 仅本轮实现的长度槽使用method-wrapper身份，不按所有dunder名称泛化。
    internal bool IsMethodWrapper => _name == "__len__";

    /// <inheritdoc />
    public override string ToString() => IsMethodWrapper
        ? $"<method-wrapper '{_name}' of {PyRepr.TypeName(_receiver)} object at 0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_receiver):x}>"
        : $"<built-in method {_name}>";

    internal PyBuiltinMethod(object receiver, string name, PyBuiltinMethodCall call)
    {
        _receiver = receiver;
        _name = name;
        _call = call;
    }

    /// <summary>一等绑定方法的接收者与名称元数据；不把Python对象暴露为CLR反射对象。</summary>
    internal object? GetMetadata(string name) => name switch
    {
        "__self__" => _receiver, "__name__" => _name,
        "__qualname__" => PyRepr.TypeName(_receiver) + "." + _name,
        "__module__" when !IsMethodWrapper => PyNone.Instance,
        "__objclass__" when IsMethodWrapper => PyObject.TypeOf(_receiver),
        "__class__" when IsMethodWrapper => PyType.Get("method-wrapper"),
        _ => throw new PyAttributeError($"'{(IsMethodWrapper ? "method-wrapper" : "builtin_function_or_method")}' object has no attribute '{name}'"),
    };

    /// <summary>同一接收者上的同一方法相等，不调用接收者自己的相等或hash协议。</summary>
    internal bool SameMethod(PyBuiltinMethod other) =>
        ReferenceEquals(_receiver, other._receiver) && _name == other._name;
    internal long IdentityHash() => System.HashCode.Combine(
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_receiver), _name);

    /// <summary>使用已展开实参调用固定入口；参数错误直到这里才发生。</summary>
    /// <param name="args">位置参数。</param>
    /// <param name="names">关键字名称。</param>
    /// <param name="values">关键字值。</param>
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        PyOps.ValidatePreparedArity(_receiver, _name, args.Length);
        return _call(_receiver, _name, args, names, values);
    }
}

internal delegate object? PyBuiltinMethodCall(
    object receiver, string name, object?[] args, string[] names, object?[] values);

public static partial class PyOps
{
    /// <summary>在实参求值之前读取方法属性；返回值不必可调用。</summary>
    /// <param name="receiver">已经求值的接收者。</param>
    /// <param name="name">方法属性名。</param>
    public static object? PrepareMethod(object? receiver, string name)
    {
        if (receiver is PyNativeType or PyNativeObject or PyInstance or PyClass or PySuper or PyModule or PyBaseException or PyType
            or PyFunction or PyBoundMethod or PyBuiltinFunction or PyProperty)
            return PyObject.GetAttr(receiver, name);

        // 内建类型不可修改方法表。这里的存在性表必须与各类型 XxxMethod 分派同步；
        // 不能用空实参试调用来判断存在性，否则会提前执行副作用或触发参数错误。
        PyBuiltinMethodCall? call = receiver switch
        {
            _ when name == "__len__" && IsBuiltinSized(receiver) => PreparedLength,
            PyList when name is "append" or "extend" or "insert" or "pop" or "remove"
                or "index" or "count" or "clear" or "reverse" or "copy" or "sort" => PreparedList,
            PyTuple when name is "count" or "index" => PreparedTuple,
            PyDict when name is "get" or "keys" or "values" or "items" or "update"
                or "pop" or "setdefault" or "clear" or "copy" => PreparedDict,
            PySet when name is "add" or "remove" or "discard" or "union" or "intersection"
                or "difference" => PreparedSet,
            PyByteArray when name is "append" or "extend" or "decode" or "startswith" or "endswith" => PreparedByteArray,
            PyBytes when name is "decode" or "hex" or "startswith" or "endswith" => PreparedBytes,
            string when name is "encode" or "upper" or "lower" or "strip" or "lstrip"
                or "rstrip" or "split" or "splitlines" or "join" or "replace" or "find" or "rfind"
                or "index" or "rindex" or "count" or "startswith" or "endswith" => PreparedText,
            PyGenerator when name is "__iter__" or "__next__" or "send" or "throw" or "close"
                => PreparedGenerator,
            _ => null,
        };
        if (call is null)
            throw new PyAttributeError($"'{PyRepr.TypeName(receiver)}' object has no attribute '{name}'");
        return new PyBuiltinMethod(receiver!, name, call);
    }

    // 这里只承诺现有方法实现接受的参数子集，不能把额外参数静默丢弃。
    // 调用前统一检查，避免字典缺参访问数组越界，以及 clear/pop 等先修改再报错。
    internal static void ValidatePreparedArity(object receiver, string name, int count)
    {
        (int min, int max) = (receiver, name) switch
        {
            (_, "__len__") when IsBuiltinSized(receiver) => (0, 0),
            (PyList, "clear" or "reverse" or "copy" or "sort") => (0, 0),
            (PyList, "pop") => (0, 1),
            (PyList, "insert") => (2, 2),
            (PyList, "append" or "extend" or "remove" or "index" or "count") => (1, 1),
            (PyTuple, "index" or "count") => (1, 1),
            (PyDict, "keys" or "values" or "items" or "clear" or "copy") => (0, 0),
            (PyDict, "get" or "pop" or "setdefault") => (1, 2),
            (PyDict, "update") => (1, 1),
            (PySet, "add" or "remove" or "discard" or "union" or "intersection" or "difference") => (1, 1),
            (PyByteArray, "append" or "extend") => (1, 1),
            (PyByteArray or PyBytes, "decode") => (0, 1),
            (PyByteArray or PyBytes, "startswith" or "endswith") => (1, 3),
            (PyBytes, "hex") => (0, 0),
            (string, "upper" or "lower") => (0, 0),
            (string, "encode" or "strip" or "lstrip" or "rstrip" or "split" or "splitlines") => (0, 1),
            (string, "join" or "count" or "startswith" or "endswith") => (1, 1),
            (string, "replace") => (2, 3),
            (string, "find" or "rfind" or "index" or "rindex") => (1, 3),
            (PyGenerator, "__iter__" or "__next__" or "close") => (0, 0),
            (PyGenerator, "send" or "throw") => (1, 1),
            (PyProperty, "getter" or "setter" or "deleter") => (1, 1),
            _ => throw new PyRuntimeError("missing prepared method signature"),
        };
        if (count < min || count > max)
            throw new PyTypeError($"{name}() expected {min} to {max} arguments, got {count}");
    }

    private static void RejectPreparedKeywords(object receiver, string name, string[] names)
    {
        if (names.Length > 0)
            throw new PyTypeError($"'{PyRepr.TypeName(receiver)}'.{name}() got an unexpected keyword argument '{names[0]}'");
    }

    private static object? PreparedList(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        if (name == "sort") return SortList((PyList)receiver, args, names, values);
        RejectPreparedKeywords(receiver, name, names);
        return ListMethod((PyList)receiver, name, args);
    }

    private static object? PreparedTuple(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return TupleMethod((PyTuple)receiver, name, args);
    }

    private static object? PreparedDict(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return DictMethod((PyDict)receiver, name, args);
    }

    private static object? PreparedSet(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return SetMethod((PySet)receiver, name, args);
    }

    private static object? PreparedByteArray(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return ByteArrayMethod((PyByteArray)receiver, name, args);
    }

    private static object? PreparedBytes(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return BytesMethod((PyBytes)receiver, name, args);
    }

    private static object? PreparedText(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return StringMethod((string)receiver, name, args);
    }

    private static object? PreparedGenerator(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return ((PyGenerator)receiver).CallMethod(name, args);
    }
}
