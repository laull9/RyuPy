// super读取使用真实接收者完整MRO，native节点不退到末尾兜底。
namespace RyuPy.Runtime;
public static partial class PyObject
{
    /// <summary>读取 `super()` 代理的属性：从定义类的下一个基类开始。</summary>
    private static object? GetSuperAttr(PySuper super, string name)
    {
        PyLexicalClassGuard.Receiver(super.Self);
        if (name == "__class__") return PyLexicalSuper.Type;
        PyLexicalSuper.GuardAttribute(name);
        PyTypingGenericProtocols.GuardSuper(super, name);
        PyTypingIo.GuardSuperAttribute(super, name);
        PyClass? actual = super.Self is PyInstance instance ? instance.Class : super.Self as PyClass;
        if (actual is { HasDictC3Layout: true }) return GetDictC3Super(super, actual, name);
        foreach (PyClass current in PyClass.SuperClasses(super.Owner, super.Self))
        {
            if (current.Attributes.TryGetValue(name, out object? raw))
            {
                PyLexicalSuper.GuardDescriptor(raw);
                PyTypingGenericProtocols.GuardDescriptorRead(super.Owner, raw);
                // class接收的普通函数/property不绑定；classmethod仍绑定真实派生类。
                if (super.Self is PyClass && raw is not (PyClassMethod or PyStaticMethod)) return raw;
                if (raw is PyProperty property)
                {
                    return InvokeBound(property.Getter, current, super.Self, [], [], []);
                }
                return Bind(raw, current, super.Self);
            }
        }
        if (super.Owner.TryRootConstructionAttribute(name, super.Self, out object? rootMethod)) return rootMethod;
        throw new PyAttributeError(
            $"'super' object has no attribute '{name}'"
        );
    }

    private static object? GetDictC3Super(PySuper super, PyClass actual, string name)
    {
        bool after = false;
        foreach (object node in actual.DictC3Nodes)
        {
            if (!after) { after = ReferenceEquals(node, super.Owner); continue; }
            object? raw;
            bool found = node is PyClass cls ? cls.Attributes.TryGetValue(name, out raw) : PyClass.NativeC3Own(node, name, out raw);
            if (!found) continue;
            if (node is PyClass owner && raw is PyProperty property && super.Self is not PyClass)
                return InvokeBound(property.Getter, owner, super.Self, [], [], []);
            return new PyC3Resolution(node, raw, name).Bind(super.Self, classAccess: super.Self is PyClass);
        }
        throw new PyAttributeError($"'super' object has no attribute '{name}'");
    }

}
