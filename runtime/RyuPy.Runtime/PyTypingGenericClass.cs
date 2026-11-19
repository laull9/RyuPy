// Generic 单父链的冷建类状态：保留原始基类身份，类体结束后才安装元数据和执行有限终结协议。
// 普通实例仍使用原有字段及构造槽；仅泛型后代分配本文件的状态与稳定反射 tuple。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed class PyTypingGenericDefinition(PyTuple? originalBases)
{
    internal enum Phase : byte { Pending, Ready, Failed }
    internal Phase State;
    internal readonly PyTuple? OriginalBases = originalBases;
    internal PyTuple? Bases;
    internal PyTuple? Mro;
}

public sealed partial class PyClass
{
    internal PyTypingGenericDefinition? GenericDefinition { get; set; }
    internal bool IsTypingGenericFamily => IsTypingGenericRoot || GenericDefinition is not null;

    internal void RequireTypingGenericReady()
    {
        if (GenericDefinition is { State: not PyTypingGenericDefinition.Phase.Ready })
            throw PyTypingGeneric.Unsupported("Generic class is not finalized");
    }

    // 类体自己的hook不是本次消费对象；只查实际父链，且必须晚于set_name阶段。
    internal void FinalizeTypingGenericDefinition()
    {
        if (GenericDefinition is not { } definition) return;
        if (definition.State != PyTypingGenericDefinition.Phase.Pending)
            throw PyTypingGeneric.Unsupported("Generic class is already finalized");
        if (definition.OriginalBases is { } original) SetAttr("__orig_bases__", original);
        if (Attributes.ContainsKey("__slots__"))
            throw PyTypingGeneric.Unsupported("Generic subclass slots are not supported");
        foreach (object? value in Attributes.Values)
        {
            PyClass? descriptorClass = value is PyInstance instance ? instance.Class
                : value is PyBaseException error ? error.PythonClass : null;
            if (descriptorClass is not null && descriptorClass.TryLookup("__set_name__", out _, out _)
                || value is IPyNativeDescriptor)
                throw PyTypingGeneric.Unsupported("Generic class descriptor __set_name__ is not supported");
        }
        if (BaseClass is not null && BaseClass.TryLookup("__init_subclass__", out _, out _))
            throw PyTypingGeneric.Unsupported("custom Generic __init_subclass__ is not supported");
        PyTypingGenericParameters.Finish(this);
    }

    internal void CompleteTypingGenericDefinition()
    {
        if (GenericDefinition is not { } definition) return;
        definition.Bases = new PyTuple([BaseClass!]);
        var mro = new List<object?>();
        for (PyClass? current = this; current is not null; current = current.BaseClass) mro.Add(current);
        mro.Add(PyBaseObject.Type);
        definition.Mro = new PyTuple(mro.ToArray());
        definition.State = PyTypingGenericDefinition.Phase.Ready;
    }

    internal void FailTypingGenericDefinition()
    {
        if (GenericDefinition is { } definition) definition.State = PyTypingGenericDefinition.Phase.Failed;
    }
}

internal static class PyTypingGenericClass
{
    // Validate不擦除alias；此处才执行一次mro_entries，返回值与原alias都不对用户暴露临时token。
    internal static object? ResolveBase(object? value, out PyTuple? original)
    {
        original = null;
        if (value is not PyTypingGenericAlias alias) return value;
        original = new PyTuple([alias]);
        _ = alias.MroEntries(original);
        return PyTypingGeneric.Root;
    }

    internal static bool Attribute(PyClass cls, string name, out object? value)
    {
        value = null;
        if (cls.GenericDefinition is not { } definition) return false;
        cls.RequireTypingGenericReady();
        switch (name)
        {
            case "__bases__": value = definition.Bases!; return true;
            case "__mro__": value = definition.Mro!; return true;
            case "mro":
                if (cls.TryLookup(name, out _, out _)) return false;
                value = PyNativeFunctions.Create("mro", [], [], 0,
                    _ => new PyList(definition.Mro!.Items)); return true;
            case "__init_subclass__": case "__class_getitem__":
                throw PyTypingGeneric.Unsupported($"explicit Generic family {name} binding is not supported");
        }
        return false;
    }

    internal static void Mutate(PyClass cls, string name)
    {
        if (cls.GenericDefinition is not { State: PyTypingGenericDefinition.Phase.Ready }) return;
        if (name is "__bases__" or "__base__" or "__mro__" or "__class__" or "__dict__")
            throw PyTypingGeneric.Unsupported($"changing Generic family {name} is not supported");
    }
}
