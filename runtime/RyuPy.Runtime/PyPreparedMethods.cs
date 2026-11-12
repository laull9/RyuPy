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
        "__qualname__" => (_receiver is PyNamedTupleInstance ? "tuple" : PyRepr.TypeName(_receiver)) + "." + _name,
        "__module__" when !IsMethodWrapper => PyNone.Instance,
        "__objclass__" when IsMethodWrapper => _receiver is PyNamedTupleInstance ? PyType.Get("tuple") : PyObject.TypeOf(_receiver),
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
        if (_receiver is string searchText && PyTextSearch.Has(_name))
            return PyTextSearch.Invoke(searchText, _name, args, names);
        // expandtabs独立绑定须先于通用arity与keyword拒绝。
        if (_receiver is string && _name == "expandtabs")
            return PyTextExpandTabs.Invoke(_receiver, args, names, values);
        // translate 的 table/delete 绑定独立于旧方法的通用数量/关键字规则。
        if (_receiver is PyBytes or PyByteArray && _name == "translate")
            return PyBinaryTranslate.Invoke(_receiver, args, names, values);
        // 仅二进制分类族采用CP的kwargs先arity；不改变其它保存方法的既有顺序。
        if (_receiver is PyBytes or PyByteArray && PyBinaryClassification.Has(_name))
            return PyBinaryClassification.Invoke(_receiver, _name, args.Length, names.Length);
        if (_receiver is PyNamedTupleInstance tuple)
            return PyOps.CallNamedTupleInheritedMethod(tuple, _name, args, names, values);
        if (_receiver is string text && _name is "startswith" or "endswith")
            return PyTextAffix.Match(text, _name, args, names, qualified: false);
        if (_receiver is string && _name is "partition" or "rpartition")
            return PyTextPartition.Invoke(_receiver, _name, args, names);
        // 这四项须先拒绝关键字再检查位置数，与直接调用共用窄绑定器。
        if (_receiver is PyList list && _name is "clear" or "pop" or "reverse" or "copy")
        {
            return PyOps.CallListCheckedMethod(list, _name, args, names);
        }
        // set.clear 也须先拒绝关键字，再检查位置数，不进入泛化签名文案。
        if (_receiver is PySet family && PyOps.HasSetFamilyMethod(_name))
            return PyOps.CallSetFamilyMethod(family, _name, args, names);
        if (_receiver is PySet set && _name == "clear")
            return PyOps.CallSetClear(set, args, names);
        if (_receiver is PyDict dict && _name == "update")
            return PyOps.CallDictUpdate(dict, args, names, values);
        PyOps.ValidatePreparedArity(_receiver, _name, args.Length);
        return _call(_receiver, _name, args, names, values);
    }
}

internal delegate object? PyBuiltinMethodCall(
    object receiver, string name, object?[] args, string[] names, object?[] values);

public static partial class PyOps
{
    // 只供生成类的字段/自有属性查找未命中之后使用，避免再进GetAttr造成递归。
    internal static object PrepareNamedTupleInheritedMethod(PyTuple receiver, string name) => name switch
    {
        "count" or "index" => new PyBuiltinMethod(receiver, name, PreparedTuple),
        "__len__" => new PyBuiltinMethod(receiver, name, PreparedLength),
        _ => throw new PyAttributeError($"'{PyRepr.TypeName(receiver)}' object has no attribute '{name}'"),
    };

    // 仅准备本层绑定错误；count/index比较元素时的用户异常不被重包。
    internal static object? CallNamedTupleInheritedMethod(PyTuple receiver, string name,
        object?[] args, string[] names, object?[] values)
    {
        try
        {
            ValidatePreparedArity(receiver, name, args.Length);
            RejectPreparedKeywords(receiver, name, names);
        }
        catch (PyBaseException error) { throw PyNamedTupleLayout.Error(error); }
        return name == "__len__" ? PyInt.Box(receiver.Length) : TupleMethod(receiver, name, args);
    }

    /// <summary>在实参求值之前读取方法属性；返回值不必可调用。</summary>
    /// <param name="receiver">已经求值的接收者。</param>
    /// <param name="name">方法属性名。</param>
    public static object? PrepareMethod(object? receiver, string name)
    {
        if (receiver is PyNamedTupleInstance || receiver is PyClass { NamedTupleLayout: not null })
            return CaptureModuleTarget(receiver, name);
        if (receiver is PyNativeType or PyNativeObject or PyInstance or PyClass or PySuper or PyModule or PyBaseException or PyType
            or PyFunction or PyBoundMethod or PyBuiltinFunction or PyProperty or PySlice or PyStaticMethod or PyClassMethod)
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
            PySet when HasSetFamilyMethod(name) => PreparedSet,
            PySet when name is "add" or "remove" or "discard" or "union" or "intersection"
                or "difference" or "clear" => PreparedSet,
            PyByteArray when name is "append" or "extend" or "decode" or "startswith" or "endswith" or "translate" => PreparedByteArray,
            PyBytes when name is "decode" or "hex" or "startswith" or "endswith" or "translate" => PreparedBytes,
            PyByteArray when PyBinaryClassification.Has(name) => PreparedByteArray,
            PyBytes when PyBinaryClassification.Has(name) => PreparedBytes,
            string when name is "encode" or "upper" or "lower" or "strip" or "lstrip"
                or "rstrip" or "split" or "splitlines" or "join" or "replace" or "find" or "rfind"
                or "index" or "rindex" or "count" or "startswith" or "endswith" or "format"
                or "partition" or "rpartition" or "expandtabs" => PreparedText,
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
            (PyDict, "update") => (0, 1),
            (PySet, "clear") => (0, 0),
            (PySet, "add" or "remove" or "discard" or "union" or "intersection" or "difference") => (1, 1),
            (PyByteArray, "append" or "extend") => (1, 1),
            (PyByteArray or PyBytes, "decode") => (0, 1),
            (PyByteArray or PyBytes, "startswith" or "endswith") => (1, 3),
            (PyBytes, "hex") => (0, 0),
            (string, "format") => (0, int.MaxValue),
            (string, "upper" or "lower") => (0, 0),
            (string, "encode" or "strip" or "lstrip" or "rstrip" or "split" or "splitlines") => (0, 1),
            (string, "join") => (1, 1),
            (string, "startswith" or "endswith") => (1, 3),
            (string, "replace") => (2, 3),
            (string, "count" or "find" or "rfind" or "index" or "rindex") => (1, 3),
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
        if (name == "update") return CallDictUpdate((PyDict)receiver, args, names, values);
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
        if (PyTextSearch.Has(name)) return PyTextSearch.Invoke((string)receiver, name, args, names);
        if (name is "startswith" or "endswith")
            return PyTextAffix.Match((string)receiver, name, args, names, qualified: false);
        // format的关键字是字段数据，不经过其它字符串方法的关键字拒绝器。
        if (name == "format") return PyStringTemplate.Format(receiver, args, names, values);
        RejectPreparedKeywords(receiver, name, names);
        return StringMethod((string)receiver, name, args);
    }

    private static object? PreparedGenerator(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return ((PyGenerator)receiver).CallMethod(name, args);
    }
}
