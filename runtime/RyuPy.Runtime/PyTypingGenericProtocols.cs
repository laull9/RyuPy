// Generic根的晚改绑必须在后代实际消费槽时拒绝；普通类不扫描祖先，family复用版本化TryLookup。
// PIC命中沿GlobalVersion失效：只要通用读取/写入在新版本成功，缓存无需增加逐次祖先守卫。
namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal void GuardInheritedGenericProtocol(string name)
    {
        if (!IsTypingGenericFamily || !PyTypingGeneric.Root.Attributes.ContainsKey(name)) return;
        if (TryLookup(name, out PyClass owner, out _) && owner.IsTypingGenericRoot)
            throw PyTypingGeneric.Unsupported($"changed inherited Generic protocol {name} is not supported");
    }
}

internal static class PyTypingGenericProtocols
{
    // 仅实际类属性绑定消费未知用户描述符；非数据描述符仍让实例字段先遮蔽。
    // 已知function/staticmethod/classmethod/property保持原绑定，不在定义或赋值时预拒绝。
    internal static void GuardDescriptorRead(PyClass receiver, object? raw, bool dataOnly = false)
    {
        if (!receiver.IsTypingGenericFamily) return;
        PyClass? descriptorClass = raw is PyInstance descriptor ? descriptor.Class
            : raw is PyBaseException exception ? exception.PythonClass : null;
        if (descriptorClass is null || !descriptorClass.TryLookup("__get__", out _, out _)) return;
        if (dataOnly && !descriptorClass.TryLookup("__set__", out _, out _)
            && !descriptorClass.TryLookup("__delete__", out _, out _)) return;
        throw PyTypingGeneric.Unsupported("Generic class attribute descriptor binding is not supported");
    }

    internal static void ReadInstance(PyInstance instance, string name)
    {
        PyClass cls = instance.Class;
        if (cls.GenericDefinition is null) return;
        cls.RequireTypingGenericReady();
        cls.GuardInheritedGenericProtocol("__getattribute__");
        // 显式读取未知根协议也不能暴露一个会绕过隐式守卫的普通函数。
        if (name.StartsWith("__", System.StringComparison.Ordinal)
            && name.EndsWith("__", System.StringComparison.Ordinal))
            cls.GuardInheritedGenericProtocol(name);
        if (name == "__dict__" && !cls.TryLookup(name, out _, out _))
            throw PyTypingGeneric.Unsupported("Generic subclass instance dictionary reflection is not supported");
        if (name is "__init_subclass__" or "__class_getitem__")
        {
            if (cls.TryGetInstanceSlot(name, out int slot) && instance.IsSlotSet(slot)) return;
            throw PyTypingGeneric.Unsupported($"explicit Generic family {name} binding is not supported");
        }
    }

    internal static void GuardSuper(PySuper super, string name)
    {
        if (!super.Owner.IsTypingGenericFamily) return;
        if (name is "__init_subclass__" or "__class_getitem__")
            throw PyTypingGeneric.Unsupported($"explicit Generic super {name} binding is not supported");
        if (!PyTypingGeneric.Root.Attributes.ContainsKey(name)) return;
        for (PyClass? current = super.Owner.BaseClass; current is not null; current = current.BaseClass)
        {
            if (!current.Attributes.ContainsKey(name)) continue;
            if (current.IsTypingGenericRoot && name.StartsWith("__", System.StringComparison.Ordinal))
                throw PyTypingGeneric.Unsupported($"changed Generic super protocol {name} is not supported");
            return;
        }
    }
}
