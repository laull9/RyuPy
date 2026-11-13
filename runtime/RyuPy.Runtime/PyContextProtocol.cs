// with与ContextDecorator共用的类型槽查找：绑定时固定接收者，实例字段不会替换进入/退出逻辑。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>查找并绑定上下文类型方法；绑定发生在进入前，返回值留到调用时校验可调用性。</summary>
    /// <param name="receiver">上下文管理器。</param>
    /// <param name="name">__enter__或__exit__。</param>
    public static object? ContextMethod(object? receiver, string name)
    {
        if (receiver is PyHTTPError httpError) return httpError.Attribute(name);
        if (receiver is PyNativeObject native && PyObject.TryGetNativeSpecial(native, name, out object? method))
            return method;
        if (receiver is PyInstance instance)
        {
            if (instance.Class.HasDictC3Layout && instance.Class.TryResolveDictC3(name, out var resolved))
                return resolved.Bind(instance);
            instance.Class.GuardInheritedGenericProtocol(name);
            if (instance.Class.TryLookup(name, out PyClass owner, out object? raw))
                return BindContext(raw, owner, instance, name);
        }
        if (receiver is PyBaseException { PythonClass: not null } error
            && error.PythonClass.TryLookup(name, out PyClass errorOwner, out object? errorRaw))
            return BindContext(errorRaw, errorOwner, error, name);
        string suffix = name == "__exit__" ? " (missed __exit__ method)" : "";
        throw new PyTypeError($"'{PyRepr.TypeName(receiver)}' object does not support the context manager protocol{suffix}");
    }

    // property的getter也属于类型描述符绑定，必须在进入之前求值，且只执行一次。
    private static object? BindContext(object? raw, PyClass owner, object receiver, string name)
    {
        if (raw is PyProperty property)
        {
            if (property.Getter is null) throw new PyAttributeError($"property '{name}' has no getter");
            return PyObject.InvokeBound(property.Getter, owner, receiver, [], [], []);
        }
        return PyObject.Bind(raw, owner, receiver);
    }
}
