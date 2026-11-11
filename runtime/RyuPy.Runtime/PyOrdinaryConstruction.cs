// 普通单继承的new/init分派；类型槽分类沿用全局版本失效，不建立反向继承网络。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    private long _constructionVersion = -1;
    private ConstructionSlots? _constructionSlots;
    private bool _ordinaryFinalized;

    // 每版本至多构造一次冷分类。普通无new热路径不新建参数数组/绑定包装。
    private sealed record ConstructionSlots(bool Special, bool CustomNew, bool CustomInit,
        PyClass? NewOwner, object? New, PyClass? InitOwner, object? Init);

    private ConstructionSlots Construction()
    {
        RequireLexicalFactoryReady();
        if (_constructionVersion == GlobalVersion && _constructionSlots is { } cached) return cached;
        bool special = false;
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = this; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current)) throw new PyNotImplementedError("cyclic class layouts are not supported");
            special |= current.HasStringLayout || current.HasListLayout || current.HasDictLayout || current.BuiltinBase is not null || current.ExceptionFactory is not null
                || current.HasEnumMetadata || current.DataclassActive || current.IsNativeRuntimeClass;
        }
        bool hasNew = TryLookup("__new__", out PyClass newOwner, out object? newMethod);
        bool hasInit = TryLookup("__init__", out PyClass initOwner, out object? initMethod);
        var slots = new ConstructionSlots(special,
            hasNew && !PyBaseObjectMethods.IsRootNew(newMethod),
            hasInit && !PyBaseObjectMethods.IsRootInit(initMethod),
            hasNew ? newOwner : null, newMethod, hasInit ? initOwner : null, initMethod);
        _constructionSlots = slots; _constructionVersion = GlobalVersion;
        return slots;
    }

    // 装饰器改变布局的时点晚于普通类finalize；全局失效也覆盖此前已创建的派生类缓存。
    internal void ConstructionLayoutChanged() => Touch();

    private ConstructionSlots GuardSpecialNew()
    {
        ConstructionSlots slots = Construction();
        // 实际类调用早于dataclass快计划；晚改绑的根new不能由快计划绕过。
        if (slots.NewOwner?.IsTypingGenericRoot == true) PyTypingGeneric.GuardProtocol("__new__");
        if (slots.Special)
        {
            if (slots.CustomNew)
                throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("custom __new__ for special class layouts is not supported"));
            // 私壳无其它布局早返，必经此special分支；普通调用不新加独立入口判断。
            if (IsNativeRuntimeClass) PyTypingNamedTupleShell.RejectPending(this);
        }
        return slots;
    }

    internal void RequireOrdinaryAllocation()
    {
        if (NamedTupleLayout is not null)
            throw new PyTypeError($"object.__new__({Name}) is not safe, use {Name}.__new__()");
        if (Construction().Special)
            throw new PyNotImplementedError("object allocation currently requires an ordinary user class");
    }

    // 专用类无custom new时保持原兜底分配/初始化语义；其快计划仍在Invoke更前面。
    private object? ConstructLegacySpecial(ConstructionSlots slots, object?[] args, string[] names, object?[] values)
    {
        var instance = new PyInstance(this);
        if (slots.InitOwner is not null) PyObject.InvokeBound(slots.Init, slots.InitOwner, instance, args, names, values);
        else if (args.Length != 0 || names.Length != 0) throw new PyTypeError($"{Name}() takes no arguments");
        return instance;
    }

    private object? ConstructOrdinary(ConstructionSlots slots, object?[] args, string[] names, object?[] values)
    {
        if (!slots.CustomNew)
        {
            // 保留常见直接分配；根new只在多余参数时查询是否有真正自定义init。
            if (!slots.CustomInit && (args.Length != 0 || names.Length != 0))
                throw new PyTypeError($"{Name}() takes no arguments");
            var fresh = new PyInstance(this);
            if (slots.CustomInit) InvokeInit(fresh, slots, args, names, values);
            return fresh;
        }
        RequireOrdinaryAllocation();
        EnsureSupportedSlot(slots.New, "__new__");
        object? created = InvokeNew(slots.New, slots.NewOwner!, args, names, values);
        // foreign和None原样返回；只有实际类型属于requested链才继续init。
        if (created is not PyInstance instance || !instance.Class.IsSubclassOf(this)) return created;
        instance.Class.RequireOrdinaryAllocation();
        ConstructionSlots actual = instance.Class.Construction();
        if (actual.CustomInit) InvokeInit(instance, actual, args, names, values);
        else instance.Class.CheckRootInitArguments(args.Length, names.Length);
        return created;
    }

    private object? InvokeNew(object? raw, PyClass owner, object?[] args, string[] names, object?[] values)
    {
        if (raw is PyStaticMethod or PyClassMethod)
        {
            // staticmethod要显式cls；classmethod还会由InvokeBound再绑定一次实际cls。
            var explicitArgs = new object?[args.Length + 1]; explicitArgs[0] = this;
            Array.Copy(args, 0, explicitArgs, 1, args.Length);
            return PyObject.InvokeBound(raw, owner, this, explicitArgs, names, values);
        }
        return PyObject.InvokeBound(raw, owner, this, args, names, values);
    }

    private static void InvokeInit(PyInstance instance, ConstructionSlots slots,
        object?[] args, string[] names, object?[] values)
    {
        if (slots.InitOwner?.IsTypingGenericRoot == true) PyTypingGeneric.GuardProtocol("__init__");
        EnsureSupportedSlot(slots.Init, "__init__");
        object? result = PyObject.InvokeBound(slots.Init, slots.InitOwner!, instance, args, names, values);
        if (result is not null and not PyNone)
            throw new PyTypeError($"__init__() should return None, not '{PyRepr.TypeName(result)}'");
    }

    internal PyInstance AllocateFromObject(int extra, int keywords)
    {
        if (IsTypingGenericRoot)
        {
            if (extra != 0 || keywords != 0)
                throw PyTypingGeneric.Error(new PyTypeError("typing.Generic() takes no arguments"));
            return new PyInstance(this);
        }
        if (HasStringLayout || HasListLayout || HasDictLayout)
            throw new PyTypeError($"object.__new__({Name}) is not safe, use {Name}.__new__()");
        // 显式 object.__new__ 不经 Invoke，未完成 IO 族仍不能由此提前分配。
        RequireTypingIoReady();
        RequireTypingGenericReady();
        RequireOrdinaryAllocation();
        ConstructionSlots slots = Construction();
        if (extra != 0 || keywords != 0)
        {
            if (slots.CustomNew)
                throw new PyTypeError("object.__new__() takes exactly one argument (the type to instantiate)");
            if (!slots.CustomInit) throw new PyTypeError($"{Name}() takes no arguments");
        }
        return new PyInstance(this);
    }

    internal void CheckRootInitArguments(int extra, int keywords)
    {
        if (extra == 0 && keywords == 0) return;
        RequireOrdinaryAllocation();
        ConstructionSlots slots = Construction();
        if (slots.CustomInit)
            throw new PyTypeError("object.__init__() takes exactly one argument (the instance to initialize)");
        if (!slots.CustomNew)
            throw new PyTypeError($"{Name}.__init__() takes exactly one argument (the instance to initialize)");
    }

    private static void EnsureSupportedSlot(object? raw, string name)
    {
        if (PyBaseObjectMethods.IsRootNew(raw) || PyBaseObjectMethods.IsRootInit(raw)) return;
        if (raw is PyStaticMethod sm) raw = sm.Value;
        else if (raw is PyClassMethod cm) raw = cm.Value;
        if (raw is PyFunction) return;
        // None/数值等非callable仍交正常调用产生TypeError；不把它们误报未支持descriptor。
        if (raw is PyProperty or IPyNativeDescriptor or IPyNativeCallable or PyNativeType or PyClass
            or PyType or PyInstance or PyBuiltinFunction or PyBoundMethod or PyBuiltinMethod or PyStaticMethod or PyClassMethod)
            throw new PyNotImplementedError($"custom descriptor/callable form for {name} is not supported");
    }
}
