// typed namedtuple类体成员的有限descriptor域；普通collections和ordinary实例沿原协议。
// 类后mutation仍禁止，字段保持只读；property只委托accessor，不添加tuple实例字典。
namespace RyuPy.Runtime;

internal static class PyTypingNamedTupleMembers
{
    internal static void Validate(object? value)
    {
        switch (value)
        {
            case PyProperty property:
                Accessor(property.Getter); Accessor(property.Setter); Accessor(property.Deleter);
                return;
            case PyStaticMethod method:
                Callable(method.Value); return;
            case PyClassMethod method:
                Callable(method.Value); return;
            case PyFunction:
                return;
            // 冷检查当前没有__get__不足：这些实例的Class以后可新增descriptor协议。
            case PyInstance:
            case PyBaseException { PythonClass: not null }:
            case IPyNativeDescriptor:
                throw PyNamedTupleLayout.Unsupported("NamedTuple class constant with mutable or unsupported descriptor behavior is not supported");
        }
    }

    private static void Accessor(object? value)
    {
        if (value is null or PyNone) return;
        Callable(value);
    }

    private static void Callable(object? value)
    {
        if (value is not PyFunction)
            throw PyNamedTupleLayout.Unsupported("NamedTuple method and property accessors currently require ordinary functions");
    }

    internal static object? ReadProperty(PyNamedTupleInstance instance, PyClass owner,
        PyProperty property, string name)
    {
        if (property.Getter is null)
            throw MissingAccessor(instance, name, "getter");
        return PyObject.InvokeBound(property.Getter, owner, instance, [], [], []);
    }

    internal static bool TryWriteProperty(PyNamedTupleInstance instance, string name,
        object? value, bool deleting)
    {
        PyClass cls = instance.Class;
        PyNamedTupleLayout layout = cls.NamedTupleLayout!;
        if (!layout.IsTyped || layout.TryFieldIndex(name, out _)) return false;
        if (!cls.TryLookup(name, out PyClass owner, out object? raw) || raw is not PyProperty property) return false;
        object? accessor = deleting ? property.Deleter : property.Setter;
        if (accessor is null) throw MissingAccessor(instance, name, deleting ? "deleter" : "setter");
        _ = PyObject.InvokeBound(accessor, owner, instance, deleting ? [] : [value], [], []);
        return true;
    }

    private static PyAttributeError MissingAccessor(PyNamedTupleInstance instance, string name, string accessor) =>
        PyNamedTupleLayout.Error(new PyAttributeError($"property '{name}' of '{instance.Class.Name}' object has no {accessor}"));
}
