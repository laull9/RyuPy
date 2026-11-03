// object 的真实无字典实例及驻留根类型入口；不借普通用户类伪造 object。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>裸 object 实例没有属性槽；其类型始终是唯一驻留 object。</summary>
public sealed class PyBaseObject : PyNativeObject
{
    private PyBaseObject() { }
    /// <inheritdoc />
    public override string TypeName => "object";
    /// <summary>和编译器 TypeObject 入口共用唯一类型身份。</summary>
    public static PyType Type => PyType.Get("object");

    /// <summary>已展开的 object() 调用只允许零参数。</summary>
    public static object Create(object?[] args, string[] names)
    {
        if (args.Length != 0 || names.Length != 0)
            throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError("object() takes no arguments"));
        return new PyBaseObject();
    }

    /// <inheritdoc />
    public override object? GetAttr(string name) => name == "__class__" ? Type
        : PyBaseObjectMethods.IsMethod(name)
            ? name == "__new__" ? PyBaseObjectMethods.Unbound(name) : PyBaseObjectMethods.Bind(this, name)
            : throw (PyBaseException)PyErrors.PrepareRaise(Missing(name));
    /// <inheritdoc />
    public override void SetAttr(string name, object? value)
    {
        if (name == "__class__")
            throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError("__class__ assignment only supported for mutable types or ModuleType subclasses"));
        throw (PyBaseException)PyErrors.PrepareRaise(Missing(name));
    }
    /// <inheritdoc />
    public override void DelAttr(string name)
    {
        if (name == "__class__")
            throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError("can't delete __class__ attribute"));
        throw (PyBaseException)PyErrors.PrepareRaise(Missing(name));
    }
    /// <inheritdoc />
    public override string PythonRepr() => $"<object object at 0x{RuntimeHelpers.GetHashCode(this):x}>";
    /// <inheritdoc />
    public override string PythonStr() => PythonRepr();

    /// <summary>根类型的有限真实元数据，不虚构用户类 MRO。</summary>
    internal static object? TypeAttribute(string name) => name switch
    {
        "__name__" or "__qualname__" => "object",
        "__module__" => "builtins",
        "__class__" => PyType.Get("type"),
        "__base__" => PyNone.Instance,
        "__bases__" => new PyTuple([]),
        "__mro__" => new PyTuple([Type]),
        _ when PyBaseObjectMethods.IsMethod(name) => PyBaseObjectMethods.Unbound(name),
        _ => throw new PyAttributeError($"type object 'object' has no attribute '{name}'"),
    };

    /// <summary>Python 不允许修改驻留不可变 object 类型。</summary>
    internal static void RejectTypeMutation(string name) =>
        throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError($"cannot set '{name}' attribute of immutable type 'object'"));
}
