// 实例字段写入与绑定参数辅助；从对象协议partial拆出，保留原调用和数据描述符优先级。
using System;

namespace RyuPy.Runtime;

public static partial class PyObject
{
    /// <summary>写实例属性；property setter 优先。</summary>
    private static void SetInstanceAttr(PyInstance instance, string name, object? value)
    {
        instance.Class.RequireLexicalFactoryReady();
        if (instance.Class.GenericDefinition is not null
            && (name is "__dict__" or "__class__" || instance.Class.TryLookup("__setattr__", out _, out _)))
            throw PyTypingGeneric.Unsupported("Generic subclass instance mutation protocol is not supported");
        instance.Class.GuardInheritedGenericProtocol("__setattr__");
        if (instance.Class.IsTypingGenericRoot) PyTypingGenericMetadata.MutateInstance(instance.Class, name, deleting: false);
        PyStringLayout.GuardInstanceMutation(instance, name);
        PyListLayout.GuardInstanceMutation(instance, name);
        PyDictLayout.GuardInstanceMutation(instance, name);
        PyEnum.GuardMemberMutation(instance, name);
        instance.Class.CheckDataclassWrite(name, deleting: false);
        InitializeDataclassField(instance, name, value);
    }

    // 相当于生成 frozen __init__ 内的 object.__setattr__：只跳过冻结检查，仍保留 property 协议。
    internal static void InitializeDataclassField(PyInstance instance, string name, object? value)
    {
        if (instance.Class.HasProperties
            && instance.Class.TryLookup(name, out PyClass owner, out object? raw)
            && raw is PyProperty property)
        {
            if (property.Setter is null)
            {
                throw new PyAttributeError(
                    $"property '{name}' of '{instance.Class.Name}' object has no setter"
                );
            }
            InvokeBound(property.Setter, owner, instance, [value], [], []);
            return;
        }
        int slot = instance.Class.GetOrAddInstanceSlot(name);
        instance.SetSlot(slot, value);
    }

    private static object?[] Prepend(object? self, object?[] args)
    {
        var result = new object?[args.Length + 1];
        result[0] = self;
        Array.Copy(args, 0, result, 1, args.Length);
        return result;
    }
}
