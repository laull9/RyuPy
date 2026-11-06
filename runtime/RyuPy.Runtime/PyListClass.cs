// list布局完成与late写守卫；只拒绝尚未接通的协议槽，不通配业务dunder名字。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal bool HasListLayout { get; set; }
    private bool _listReady;

    private static readonly HashSet<string> UnsupportedListSlots = CreateListSlots();
    private static HashSet<string> CreateListSlots()
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
        // 默认序列运算直接访问payload；反射/原位覆盖尚未接通，必须显式拒绝而不是静默忽略。
        foreach (string op in new[] { "add", "mul", "mod", "sub", "truediv", "floordiv", "pow", "and", "or", "xor", "lshift", "rshift" })
            foreach (string prefix in new[] { "", "r", "i" }) slots.Add($"__{prefix}{op}__");
        return slots;
    }

    private static void ValidateListSlot(string name, object? value)
    {
        if (UnsupportedListSlots.Contains(name))
            throw new PyNotImplementedError($"list subclass slot {name} is not supported");
        if (name is "__init__" or "__str__" or "__repr__") EnsureSupportedSlot(value, name);
        if (value is PyProperty or PyStaticMethod or PyClassMethod) return;
        if (value is IPyNativeDescriptor || value is PyInstance instance
            && (instance.Class.TryLookup("__set_name__", out _, out _)
                || instance.Class.TryLookup("__get__", out _, out _)
                || instance.Class.TryLookup("__set__", out _, out _)
                || instance.Class.TryLookup("__delete__", out _, out _)))
            throw new PyNotImplementedError("custom descriptors on list subclasses are not supported");
    }

    private void FinalizeListConstruction()
    {
        if (_listReady) throw new PyNotImplementedError("list subclass is already finalized");
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = this; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current)) throw new PyNotImplementedError("cyclic list layouts are not supported");
            if (!current.HasListLayout || current.HasStringLayout || current.HasDictLayout || current.NamedTupleLayout is not null || current.BuiltinBase is not null || current.ExceptionFactory is not null
                || current.HasEnumMetadata || current.DataclassActive || current.IsNativeRuntimeClass)
                throw new PyNotImplementedError("mixed list subclass layouts are not supported");
            if (!ReferenceEquals(current, this) && !current._listReady)
                throw new PyNotImplementedError("list subclass parent is not finalized");
            foreach (var pair in current.Attributes) ValidateListSlot(pair.Key, pair.Value);
        }
        _listReady = true; Touch();
    }

    internal void RequireListReady()
    {
        if (!HasListLayout || !_listReady)
            throw PyListLayout.Own(new PyNotImplementedError("list subclass is not finalized"));
    }

    private void GuardListMutation(string name, object? value, bool deleting)
    {
        if (!HasListLayout || !_listReady) return;
        try
        {
            if (name is "__name__" or "__qualname__")
                throw new PyNotImplementedError($"list subclass metadata {name} mutation is not supported");
            ValidateListSlot(name, deleting ? null : value);
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    internal void InitializeListInstance(PyInstance instance, object?[] args, string[] names, object?[] values)
    {
        ConstructionSlots slots = Construction();
        if (!slots.CustomInit) { PyListLayout.Init(instance, args, names, values); return; }
        // 用户正文在catch之外；只校验错误和非None返回值由原冷构造协议负责。
        EnsureSupportedSlot(slots.Init, "__init__");
        object? result = PyObject.InvokeBound(slots.Init, slots.InitOwner!, instance, args, names, values);
        if (result is not null and not PyNone)
            throw PyListLayout.Own(new PyTypeError($"__init__() should return None, not '{PyRepr.TypeName(result)}'"));
    }
}
