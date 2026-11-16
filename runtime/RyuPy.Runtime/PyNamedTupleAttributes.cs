// namedtuple身份、只读字段和类元数据；公开字典内容与私有构造schema严格分离。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed partial class PyNamedTupleLayout
{
    // 类固有 tuple 元数据在发布前固定；不得挂普通 Attributes 而让实例误读。
    private PyTuple _bases = null!;
    private PyTuple _mro = null!;

    private Dictionary<string, object?> PrepareAttributes(PyClass cls, object? module)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        _bases = new PyTuple([PyType.Get("tuple")]);
        _mro = new PyTuple([cls, PyType.Get("tuple"), PyBaseObject.Type]);
        var fields = new PyTuple(Array.ConvertAll(_fields, static name => (object?)name));
        var defaults = new PyDict();
        for (int index = 0; index < _fields.Length; index++)
        {
            if (_constructor.HasDefault[index + 1]) defaults.Set(_fields[index], _defaults[index + 1]);
            attributes[_fields[index]] = new PyNamedTupleField(index);
        }
        string arguments = string.Join(", ", _fields) + (_fields.Length == 1 ? "," : "");
        // 准备独立最终字典；异常不得破坏仍在执行类体的namespace。
        attributes["__doc__"] = cls.Name + "(" + arguments + ")";
        attributes["__module__"] = module ?? PyNone.Instance;
        attributes["__slots__"] = new PyTuple([]);
        attributes["_fields"] = fields;
        attributes["__match_args__"] = fields;
        attributes["_field_defaults"] = defaults;
        PyNamedTupleHelpers.Prepare(cls, this, attributes);
        return attributes;
    }

    internal static bool TryClassMetadata(PyClass cls, string name, out object? value)
    {
        value = null;
        if (cls.NamedTupleLayout is null) return false;
        switch (name)
        {
            case "__name__":
                value = cls.Name; return true;
            case "__qualname__":
                value = cls.Attributes.TryGetValue(name, out object? qualified) ? qualified : cls.Name; return true;
            case "__class__": value = PyType.Get("type"); return true;
            case "__module__": value = cls.Attributes["__module__"]; return true;
            case "__base__": value = PyType.Get("tuple"); return true;
            case "__bases__": value = cls.NamedTupleLayout._bases; return true;
            case "__mro__": value = cls.NamedTupleLayout._mro; return true;
            case "__dict__": throw Unsupported("namedtuple class __dict__ is not supported");
            default:
                if (!cls.Attributes.ContainsKey(name) && PyNamedTupleTupleMethods.Has(name))
                {
                    value = PyNamedTupleTupleMethods.Unbound(name);
                    return true;
                }
                return false;
        }
    }

    internal static object? GetInstanceAttribute(PyNamedTupleInstance instance, string name)
    {
        PyClass cls = instance.Class;
        PyNamedTupleLayout layout = cls.NamedTupleLayout!;
        if (name == "__class__") return cls;
        if (layout.TryFieldIndex(name, out int index)) return instance.Items[index];
        if (cls.TryLookup(name, out PyClass owner, out object? raw))
        {
            if (layout.IsTyped && raw is PyProperty property)
                return PyTypingNamedTupleMembers.ReadProperty(instance, owner, property, name);
            return PyObject.Bind(raw, owner, instance);
        }
        // 没有实例dict；只按已实现tuple方法表回退，不凭属性名伪造callable。
        try { return PyOps.PrepareNamedTupleInheritedMethod(instance, name); }
        catch (PyAttributeError error) { throw Error(error); }
    }

    internal static void RejectInstanceMutation(PyNamedTupleInstance instance, string name, bool deleting)
    {
        if (instance.Class.NamedTupleLayout!.TryFieldIndex(name, out _))
            throw Error(new PyAttributeError(deleting ? "can't delete attribute" : "can't set attribute"));
        throw Error(new PyAttributeError($"'{instance.Class.Name}' object has no attribute '{name}'"));
    }

    internal static void GuardMutation(PyClass cls)
    {
        if (cls.NamedTupleLayout is not null)
            throw Unsupported("changing namedtuple class attributes is not supported");
    }

    internal static void GuardBase(PyClass cls)
    {
        if (cls.IsNativeRuntimeClass) PyTypingNamedTupleShell.RejectPending(cls);
        if (cls.NamedTupleLayout is not null)
            throw Unsupported("subclassing a generated namedtuple is not supported");
    }

    internal static string ClassRepr(PyClass cls)
    {
        object? module = cls.Attributes["__module__"];
        return module is string name && name != "builtins"
            ? $"<class '{name}.{cls.Name}'>" : $"<class '{cls.Name}'>";
    }

    internal static string Repr(PyNamedTupleInstance instance) => instance.Class.NamedTupleLayout!.FormatRepr(instance);

    internal static PyNotImplementedError Unsupported(string message) =>
        (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError(message));

    internal static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
