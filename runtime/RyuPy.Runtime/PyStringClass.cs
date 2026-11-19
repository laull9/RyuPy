// String布局创建/发布前校验及late写守卫；禁止表只覆盖已登记协议边界，不通配普通dunder数据。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal bool HasStringLayout { get; set; }
    private bool _stringReady;

    private static readonly HashSet<string> UnsupportedStringSlots = CreateStringSlots();
    private static HashSet<string> CreateStringSlots()
    {
        var slots = new HashSet<string>(StringComparer.Ordinal)
        {
            "__new__", "__slots__", "__bases__", "__base__", "__mro__", "__class__", "__dict__",
            "__init_subclass__", "__class_getitem__", "__mro_entries__", "__prepare__",
            "__getattribute__", "__setattr__", "__delattr__", "__get__", "__set__", "__delete__", "__set_name__",
            "__len__", "__bool__", "__iter__", "__getitem__", "__contains__", "__setitem__", "__delitem__",
            "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__", "__hash__",
            "__reversed__", "__format__", "__round__", "__length_hint__",
        };
        // add/mul/mod因raw默认协议；其余27槽是既有strict-subclass反射顺序债，不是payload安全要求。
        foreach (string op in new[] { "add", "mul", "mod", "sub", "truediv", "floordiv", "pow", "and", "or", "xor", "lshift", "rshift" })
            foreach (string prefix in new[] { "", "r", "i" }) slots.Add($"__{prefix}{op}__");
        return slots;
    }

    private static void ValidateStringSlot(string name, object? value)
    {
        if (UnsupportedStringSlots.Contains(name))
            throw new PyNotImplementedError($"str subclass slot {name} is not supported");
        if (name is "__init__" or "__str__" or "__repr__") EnsureSupportedSlot(value, name);
        if (value is PyProperty or PyStaticMethod or PyClassMethod) return;
        if (value is IPyNativeDescriptor || value is PyInstance instance
            && (instance.Class.TryLookup("__set_name__", out _, out _)
                || instance.Class.TryLookup("__get__", out _, out _)
                || instance.Class.TryLookup("__set__", out _, out _)
                || instance.Class.TryLookup("__delete__", out _, out _)))
            throw new PyNotImplementedError("custom descriptors on str subclasses are not supported");
    }

    private void FinalizeStringConstruction()
    {
        if (_stringReady) throw new PyNotImplementedError("str subclass is already finalized");
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = this; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current)) throw new PyNotImplementedError("cyclic str layouts are not supported");
            if (!current.HasStringLayout || current.HasListLayout || current.HasDictLayout || current.BuiltinBase is not null || current.ExceptionFactory is not null
                || current.HasEnumMetadata || current.DataclassActive || current.IsNativeRuntimeClass)
                throw new PyNotImplementedError("mixed str subclass layouts are not supported");
            if (!ReferenceEquals(current, this) && !current._stringReady)
                throw new PyNotImplementedError("str subclass parent is not finalized");
            foreach (var pair in current.Attributes) ValidateStringSlot(pair.Key, pair.Value);
        }
        _stringReady = true; Touch();
    }

    internal void RequireStringReady()
    {
        if (!HasStringLayout || !_stringReady)
            throw PyStringLayout.Own(new PyNotImplementedError("str subclass is not finalized"));
    }

    private void GuardStringMutation(string name, object? value, bool deleting)
    {
        if (!HasStringLayout || !_stringReady) return;
        try
        {
            if (name is "__name__" or "__qualname__")
                throw new PyNotImplementedError($"str subclass metadata {name} mutation is not supported");
            ValidateStringSlot(name, deleting ? null : value);
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    internal void CheckStringRootInit(int extra, int keywords)
    {
        if ((extra != 0 || keywords != 0) && Construction().CustomInit)
            throw PyStringLayout.Own(new PyTypeError("object.__init__() takes exactly one argument (the instance to initialize)"));
    }

    internal void InitializeStringInstance(PyInstance instance, object?[] args, string[] names, object?[] values)
    {
        ConstructionSlots slots = Construction();
        if (!slots.CustomInit) return;
        // 用户正文在catch之外；只校验错误和非None返回值由原冷构造协议负责。
        EnsureSupportedSlot(slots.Init, "__init__");
        object? result = PyObject.InvokeBound(slots.Init, slots.InitOwner!, instance, args, names, values);
        if (result is not null and not PyNone)
            throw PyStringLayout.Own(new PyTypeError($"__init__() should return None, not '{PyRepr.TypeName(result)}'"));
    }
}
