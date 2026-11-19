// NoReturn 使用现有特殊形式载体；当前导出仅在真正的 _type_check membership 相位消费。
// 不把可写导出当 canonical，也不在模块初始化、热缓存或不检查类型的参数位置提前拒绝。
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTypingNoReturn
{
    internal static object? ReadExport()
    {
        if (!PyTyping.CreateAttributes().TryGetValue("NoReturn", out object? value))
            throw PyTypingClassVar.Error(new PyNameError("name 'NoReturn' is not defined"));
        return value;
    }

    // 调用方先按原序读取整个全局组；Any 身份短路不能绕过 NoReturn 缺失或后置 helper。
    // 这里只实现无用户比较回调的域。未知替换只有真比较到它时 NI，不污染 Any 提前返回。
    internal static bool Matches(object? value, object? any, object? current, string? annotationField = null)
    {
        if (ReferenceEquals(value, any)) return true;
        if (!SafeOperand(value) || !SafeReplacement(current))
            throw PyTypingClassVar.Error(new PyNotImplementedError(
                (annotationField is null ? "" : "NamedTuple field " + annotationField + ": ")
                + "typing.NoReturn membership comparison protocol is not supported"));
        return ReferenceEquals(value, current) || PyHash.Equal(current, value);
    }

    private static bool SafeReplacement(object? value) => value is null or PyNone or bool or int or long
        or BigInteger or double or string or PyEllipsis or PyType or PyNativeType or PyClass
        or PyTypedDictType or PyTypingTypeVar or PyTypingForm;

    // exact 容器与上述替换跨种类比较不会扫描元素；alias 对非 alias 也不会比较参数树。
    private static bool SafeOperand(object? value) => SafeReplacement(value)
        || value is PyTypingAlias or PyTypingGenericAlias or PyTypingForwardRef
        || value is PyTuple tuple && tuple.GetType() == typeof(PyTuple)
        || value is PyList;

    internal static bool TypedDictMembership(object value)
    {
        var attributes = PyTyping.CreateAttributes();
        if (!attributes.TryGetValue("Any", out object? any)
            || !ReferenceEquals(any, PyTyping.OriginalForm("Any")))
            throw PyTypingClassVar.Error(new PyNotImplementedError("TypedDict lookup after rebinding typing.Any is not supported"));
        PyTyping.GuardHelper("LiteralString");
        object? current = ReadExport();
        PyTyping.GuardHelper("Never"); PyTyping.GuardHelper("Self"); PyTyping.GuardHelper("TypeAlias");
        return Matches(value, any, current);
    }

    internal static PyTypeError CannotInstantiate() =>
        PyTypingClassVar.Error(new PyTypeError("Cannot instantiate typing.NoReturn"));

    internal static bool RejectCheck(bool subclass) => throw PyTypingClassVar.Error(new PyTypeError(
        "typing.NoReturn cannot be used with " + (subclass ? "issubclass()" : "isinstance()")));

    internal static object? GetAttr(PyTypingForm root, string name) => name switch
    {
        "__class__" => PyObject.TypeOf(root),
        "__name__" or "__qualname__" => "NoReturn",
        "__module__" => "typing",
        "__getitem__" => PyNativeFunctions.Create(name, ["parameters"], [null], 1,
            args => PyTyping.Subscribe(root, args[0])),
        "__call__" => new Call(root),
        "__mro_entries__" => PyNativeFunctions.Create(name, ["bases"], [null], 1,
            _ => PyTypingClassVar.RejectBase(root)),
        "__instancecheck__" => PyNativeFunctions.Create(name, ["obj"], [null], 1,
            static _ => RejectCheck(subclass: false)),
        "__subclasscheck__" => PyNativeFunctions.Create(name, ["cls"], [null], 1,
            static _ => RejectCheck(subclass: true)),
        "__or__" => PyNativeFunctions.Create(name, ["other"], [null], 1, args => Union(root, args[0])),
        "__ror__" => PyNativeFunctions.Create(name, ["other"], [null], 1, args => Union(args[0], root)),
        "__reduce__" => PyNativeFunctions.Create(name, [], [], 0, static _ => "NoReturn"),
        "_name" or "_getitem" or "__dict__" or "__doc__" =>
            throw new PyNotImplementedError("typing.NoReturn internal slot reflection is not supported"),
        _ => throw new PyAttributeError(name),
    };

    private static object? Union(object? left, object? right)
    {
        if (!PyTyping.CreateAttributes().TryGetValue("Union", out object? current))
            throw PyTypingClassVar.Error(new PyNameError("name 'Union' is not defined"));
        if (!SafeReplacement(current))
            throw new PyNotImplementedError("rebound typing.Union subscription protocol is not supported");
        return PyOps.GetItem(current, new PyTuple([left, right]));
    }

    private sealed class Call(PyTypingForm owner) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method";
        public override object? GetAttr(string name) => name == "__self__" ? owner : throw Missing(name);
        public object? Invoke(object?[] args, string[] names, object?[] values) => throw CannotInstantiate();
    }
}
