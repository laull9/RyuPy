// dict布局完成与late写守卫；只拒绝尚未接通的协议槽，不通配业务dunder名字。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal bool HasDictLayout { get; set; }
    private bool _dictReady;

    private static readonly HashSet<string> UnsupportedDictSlots = CreateDictSlots();
    private static HashSet<string> CreateDictSlots()
    {
        var slots = new HashSet<string>(StringComparer.Ordinal)
        {
            "__new__", "__slots__", "__bases__", "__base__", "__mro__", "__class__", "__dict__",
            "__init_subclass__", "__class_getitem__", "__mro_entries__", "__prepare__",
            "__getattribute__", "__setattr__", "__delattr__", "__get__", "__set__", "__delete__", "__set_name__",
            "__len__", "__bool__", "__iter__", "__getitem__", "__contains__", "__setitem__", "__delitem__",
            "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__", "__hash__",
            "__reversed__", "__format__", "__round__", "__length_hint__",
            "__neg__", "__pos__", "__abs__", "__invert__", "__index__", "__int__", "__float__",
        };
        // 默认字典运算直接访问payload；反射/原位覆盖尚未接通，必须显式拒绝而不是静默忽略。
        foreach (string op in new[] { "add", "mul", "mod", "sub", "truediv", "floordiv", "pow", "and", "or", "xor", "lshift", "rshift", "matmul", "divmod" })
            foreach (string prefix in new[] { "", "r", "i" }) slots.Add($"__{prefix}{op}__");
        foreach (string supported in new[] { "__setitem__", "__delitem__", "__iter__", "__reversed__", "__or__", "__ror__", "__ior__" })
            slots.Remove(supported);
        return slots;
    }

    private static void ValidateDictSlot(string name, object? value)
    {
        if (UnsupportedDictSlots.Contains(name))
            throw new PyNotImplementedError($"dict subclass slot {name} is not supported");
        if (name is "__init__" or "__str__" or "__repr__" or "__missing__"
            or "__setitem__" or "__delitem__" or "__iter__" or "__reversed__" or "__or__" or "__ror__" or "__ior__")
        { if (!PyDictDescriptors.IsUnbound(value)) EnsureSupportedSlot(value, name); }
        if (value is PyProperty or PyStaticMethod or PyClassMethod) return;
        if (value is IPyNativeDescriptor || value is PyInstance instance
            && (instance.Class.TryLookup("__set_name__", out _, out _)
                || instance.Class.TryLookup("__get__", out _, out _)
                || instance.Class.TryLookup("__set__", out _, out _)
                || instance.Class.TryLookup("__delete__", out _, out _)))
            throw new PyNotImplementedError("custom descriptors on dict subclasses are not supported");
    }

    private void FinalizeDictConstruction()
    {
        if (_dictReady) throw new PyNotImplementedError("dict subclass is already finalized");
        if (HasDictC3Layout)
        {
            ValidateDictC3Family(); _dictReady = true; Touch(); return;
        }
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = this; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current)) throw new PyNotImplementedError("cyclic dict layouts are not supported");
            if (!current.HasDictLayout || current.HasStringLayout || current.HasListLayout || current.NamedTupleLayout is not null || current.BuiltinBase is not null || current.ExceptionFactory is not null
                || current.HasEnumMetadata || current.DataclassActive || current.IsNativeRuntimeClass)
                throw new PyNotImplementedError("mixed dict subclass layouts are not supported");
            if (!ReferenceEquals(current, this) && !current._dictReady)
                throw new PyNotImplementedError("dict subclass parent is not finalized");
            foreach (var pair in current.Attributes) ValidateDictSlot(pair.Key, pair.Value);
        }
        _dictReady = true; Touch();
    }

    internal void RequireDictReady()
    {
        if (!HasDictLayout || !_dictReady)
            throw PyDictLayout.Own(new PyNotImplementedError("dict subclass is not finalized"));
    }

    private void GuardDictMutation(string name, object? value, bool deleting)
    {
        if (!HasDictLayout || !_dictReady) return;
        try
        {
            if (name is "__name__" or "__qualname__")
                throw new PyNotImplementedError($"dict subclass metadata {name} mutation is not supported");
            if (HasDictC3Layout && name == "__slots__") return; // 已冻结的布局不随元数据重绑改变。
            if (HasDictC3Layout) ValidateDictC3Slot(name, deleting ? null : value);
            else ValidateDictSlot(name, deleting ? null : value);
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    internal void InitializeDictInstance(PyInstance instance, object?[] args, string[] names, object?[] values)
    {
        if (HasDictC3Layout)
        {
            if (!TryResolveDictC3("__init__", out var init)) throw new PyRuntimeError("dict C3 has no initializer");
            object? returned = init.Invoke(instance, args, names, values);
            if (returned is not null and not PyNone) throw PyDictLayout.Own(new PyTypeError("__init__() should return None"));
            return;
        }
        ConstructionSlots slots = Construction();
        if (!slots.CustomInit) { PyDictLayout.Init(instance, args, names, values); return; }
        // 用户正文在catch之外；只校验错误和非None返回值由原冷构造协议负责。
        EnsureSupportedSlot(slots.Init, "__init__");
        object? result = PyObject.InvokeBound(slots.Init, slots.InitOwner!, instance, args, names, values);
        if (result is not null and not PyNone)
            throw PyDictLayout.Own(new PyTypeError($"__init__() should return None, not '{PyRepr.TypeName(result)}'"));
    }
}
