// IO 族只暴露真实已知元数据；缺失 Generic 的完整反射明确 NI，不返回假 object 祖先。
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static partial class PyTypingIo
{
    internal static bool TryClassMetadata(PyClass cls, string name, out object? value)
    {
        value = null;
        if (!cls.IsTypingIoFamily) return false;
        switch (name)
        {
            case "__name__": value = cls.Name; return true;
            case "__qualname__":
                value = cls.Attributes.TryGetValue(name, out object? qualified) ? qualified : cls.Name; return true;
            case "__class__": value = PyType.Get("type"); return true;
            case "__module__": value = Module(cls); return true;
            case "__base__":
                if (cls.TypingIoKind == PyTypingIoKind.IO) throw Unsupported("typing IO generic base reflection is not supported");
                value = cls.BaseClass; return true;
            case "__bases__": value = cls.TypingIoBases; return true;
            case "__parameters__":
                if (cls.TypingIoKind is not (PyTypingIoKind.Binary or PyTypingIoKind.Text))
                    throw Unsupported("typing IO generic parameter reflection is not supported");
                value = Empty; return true;
            case "__mro__": case "mro": case "__orig_bases__": case "__dict__":
            case "__init_subclass__": case "__class_getitem__":
                throw Unsupported("typing IO generic class reflection is not supported");
            default: return false;
        }
    }

    internal static bool TryInstanceMetadata(PyInstance instance, string name, out object? value)
    {
        value = null;
        PyClass cls = instance.Class;
        if (!cls.IsTypingIoFamily) return false;
        if (name == "__class__") { value = cls; return true; }
        if (name == "__dict__")
        {
            if (cls.IsTypingIoRoot) throw Error(new PyAttributeError($"'{cls.Name}' object has no attribute '__dict__'"));
            throw Unsupported("typing IO subclass instance dictionary reflection is not supported");
        }
        // 普通派生的同名实例数据可遮蔽这些非数据类成员，不能被反射 NI 抢先吞掉。
        if (name is "__parameters__" or "__orig_bases__" or "__init_subclass__" or "__class_getitem__")
            if (cls.TryGetInstanceSlot(name, out int ownSlot) && instance.IsSlotSet(ownSlot)) return false;
        if (name is "__orig_bases__" or "__init_subclass__" or "__class_getitem__")
            throw Unsupported("typing IO generic instance reflection is not supported");
        if (name == "__parameters__") return TryClassMetadata(cls, name, out value);
        // 实例自身 module 字段仍可遮蔽类值；未知类来源不冒用继承来的 typing。
        if (name == "__module__" && !cls.Attributes.ContainsKey(name)
            && !(cls.TryGetInstanceSlot(name, out int slot) && instance.IsSlotSet(slot))) _ = Module(cls);
        return false;
    }

    internal static void GuardInstanceReservedMutation(PyClass cls, string name)
    {
        if (cls.IsTypingIoFamily && (name == "__class__" || name == "__dict__" && !cls.IsTypingIoRoot))
            throw Unsupported($"changing typing IO instance {name} is not supported");
    }

    internal static void RejectRootInstanceMutation(PyClass cls, string name)
    {
        // property 的 setter/deleter 已先处理；存在的类成员不可被空布局实例遮蔽。
        string message = cls.TryLookup(name, out _, out _)
            ? $"'{cls.Name}' object attribute '{name}' is read-only"
            : $"'{cls.Name}' object has no attribute '{name}'";
        throw Error(new PyAttributeError(message));
    }

    internal static void GuardSuperAttribute(PySuper super, string name)
    {
        if (!super.Owner.IsTypingIoFamily) return;
        if (name is "__dict__" or "__base__" or "__bases__" or "__mro__" or "mro" or "__orig_bases__"
            or "__parameters__" or "__init_subclass__" or "__class_getitem__")
            throw Unsupported("typing IO generic super reflection is not supported");
    }

    private static object? Module(PyClass cls) => cls.Attributes.TryGetValue("__module__", out object? module)
        ? module : throw Unsupported("typing IO class definition module is unknown");

    internal static string Display(PyClass cls)
    {
        object? module = Module(cls);
        return module is string text && text != "builtins" ? text + "." + cls.Name : cls.Name;
    }

    internal static string ClassRepr(PyClass cls) => $"<class '{Display(cls)}'>";
    internal static string InstanceRepr(PyInstance instance) =>
        $"<{Display(instance.Class)} object at 0x{RuntimeHelpers.GetHashCode(instance):x}>";
}
