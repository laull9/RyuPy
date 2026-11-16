// Generic 空根的冷反射与字段守卫；只对根标记分派，不为普通实例分配字典或元数据。
using System;

namespace RyuPy.Runtime;

internal static class PyTypingGenericMetadata
{
    // 延迟求值避免 Root 创建时 SetAttr 的守卫触发 canonical 初始化递归。
    private static readonly Lazy<PyTuple> Bases = new(() => new PyTuple([PyBaseObject.Type]));
    private static readonly Lazy<PyTuple> Mro = new(() => new PyTuple([PyTypingGeneric.Root, PyBaseObject.Type]));
    internal static bool ClassAttribute(PyClass cls, string name, out object? value)
    {
        value = null;
        if (!cls.IsTypingGenericRoot) return PyTypingGenericClass.Attribute(cls, name, out value);
        switch (name)
        {
            case "__bases__": value = Bases.Value; return true;
            case "__mro__": value = Mro.Value; return true;
            case "mro":
                if (cls.Attributes.ContainsKey(name)) return false;
                value = PyNativeFunctions.Create("mro", [], [], 0, _ => new PyList([cls, PyBaseObject.Type])); return true;
            case "__hash__": case "__eq__": case "__ne__":
                if (cls.Attributes.ContainsKey(name)) return false;
                value = PyBaseObjectMethods.Unbound(name); return true;
            case "__dict__": throw PyTypingGeneric.Unsupported("Generic class mappingproxy reflection is not supported");
            case "__class_getitem__": case "__init_subclass__":
                if (cls.Attributes.ContainsKey(name)) return false;
                throw PyTypingGeneric.Unsupported($"explicit Generic {name} binding is not supported");
            case "__new__": case "__init__":
                if (cls.Attributes.ContainsKey(name)) return false;
                throw PyTypingGeneric.Unsupported($"explicit Generic {name} descriptor binding is not supported");
        }
        return false;
    }

    internal static bool InstanceAttribute(PyInstance instance, string name, out object? value)
    {
        value = null;
        if (!instance.Class.IsTypingGenericRoot)
        {
            PyTypingGenericProtocols.ReadInstance(instance, name);
            return false;
        }
        PyTypingGeneric.GuardProtocol("__getattribute__");
        if (!instance.Class.Attributes.ContainsKey(name)) PyTypingGeneric.GuardProtocol("__getattr__");
        if (name is "__new__" or "__init__" && !instance.Class.Attributes.ContainsKey(name))
            throw PyTypingGeneric.Unsupported($"explicit Generic instance {name} descriptor binding is not supported");
        if (name is "__hash__" or "__eq__" or "__ne__" && !instance.Class.Attributes.ContainsKey(name))
        { value = PyBaseObjectMethods.BindConstruction(instance, name); return true; }
        return false;
    }

    internal static void MutateInstance(PyClass cls, string name, bool deleting)
    {
        if (!cls.IsTypingGenericRoot) return;
        PyTypingGeneric.GuardProtocol(deleting ? "__delattr__" : "__setattr__");
        if (name == "__class__") throw PyTypingGeneric.Error(new PyTypeError(deleting
            ? "can't delete __class__ attribute"
            : "__class__ assignment only supported for mutable types or ModuleType subclasses"));
        if (cls.Attributes.TryGetValue(name, out object? attribute) && attribute is PyProperty or IPyNativeDescriptor or PyInstance)
            throw PyTypingGeneric.Unsupported("Generic instance descriptor mutation is not supported");
        throw PyTypingGeneric.Error(new PyAttributeError($"'typing.Generic' object has no attribute '{name}'"));
    }

    internal static void MutateClass(PyClass cls, string name)
    {
        if (!cls.IsTypingGenericRoot) { PyTypingGenericClass.Mutate(cls, name); return; }
        if (name is "__name__" or "__qualname__" or "__bases__" or "__base__" or "__mro__" or "__dict__" or "__class__")
            throw PyTypingGeneric.Unsupported($"changing Generic class metadata {name} is not supported");
    }

    internal static bool InstanceOf(object? value, PyClass cls)
    {
        if (value is PyInstance instance)
        {
            if (instance.Class.IsTypingIoFamily)
                throw PyTypingGeneric.Unsupported("typing IO Generic ancestry is not supported yet");
            return instance.Class.IsSubclassOf(cls);
        }
        return false;
    }

    // 非字符串 module 的 CP 回退保留原生限定名；alias 的 repr 则另按字符串化处理。
    private static string DisplayName(PyClass cls)
    {
        object? value = cls.NativeModuleValue;
        if (value is PyStringInstance)
            throw PyTypingGeneric.Unsupported("Generic root module string subclass reflection is not supported");
        return value is string module
            ? module == "builtins" ? "typing.Generic" : module + "." + cls.Name : "typing.Generic";
    }
    internal static string ClassRepr(PyClass cls) => $"<class '{DisplayName(cls)}'>";
    internal static string InstanceRepr(PyInstance instance) =>
        $"<{DisplayName(instance.Class)} object at 0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(instance):x}>";
}
