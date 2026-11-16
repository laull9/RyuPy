// 捕获计划2×2消融：只改变miss冷拆分与capture/布局helper的内联策略。
// 四入口保留完整相同守卫、缓存与失效语义，复用同一有界缓存；不接入产品默认入口。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

public static partial class PyPic
{
    // C=0：本体miss；I=0：capture及布局helper均NoInlining。
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static object? CaptureMethodPlanAblationCold0Inline0(int site, object? receiver, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || receiver is not PyInstance instance
            || instance.GetType() != typeof(PyInstance) || instance.ForeignInstance is not null)
            return PyOps.CaptureMethodTarget(receiver, name);
        PyClass cls = instance.Class;
        cls.RequireLexicalFactoryReady();
        if (!IsPlainCaptureClassAblationI0(cls)) return PyOps.CaptureMethodTarget(receiver, name);
        long version = PyClass.GlobalVersion;
        for (int index = site * EntriesPerSite; index < (site + 1) * EntriesPerSite; index++)
        {
            ref CapturedPlanEntry entry = ref CapturedPlans[index];
            if (entry.Plan is not null && ReferenceEquals(entry.Class, cls) && entry.Version == version
                && string.Equals(entry.Name, name, StringComparison.Ordinal)
                && (entry.ShadowSlot < 0 || !instance.IsSlotSet(entry.ShadowSlot)))
                return entry.Plan;
        }
        if (instance.TryOwnAttribute(name, out _) || !cls.TryLookup(name, out PyClass owner, out object? raw)
            || raw is not PyFunction function)
            return PyOps.CaptureMethodTarget(instance, name);
        var plan = new CapturedClassPlan(owner, function);
        CapturedPlans[NextVictim(CapturedPlanVictims, site)] = new CapturedPlanEntry
        {
            Class = cls, Name = name, Version = version, Plan = plan,
            ShadowSlot = cls.TryGetInstanceSlot(name, out int slot) ? slot : -1,
        };
        return plan;
    }

    // C=0：本体miss；I=1：capture及布局helper均AggressiveInlining。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? CaptureMethodPlanAblationCold0Inline1(int site, object? receiver, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || receiver is not PyInstance instance
            || instance.GetType() != typeof(PyInstance) || instance.ForeignInstance is not null)
            return PyOps.CaptureMethodTarget(receiver, name);
        PyClass cls = instance.Class;
        cls.RequireLexicalFactoryReady();
        if (!IsPlainCaptureClassAblationI1(cls)) return PyOps.CaptureMethodTarget(receiver, name);
        long version = PyClass.GlobalVersion;
        for (int index = site * EntriesPerSite; index < (site + 1) * EntriesPerSite; index++)
        {
            ref CapturedPlanEntry entry = ref CapturedPlans[index];
            if (entry.Plan is not null && ReferenceEquals(entry.Class, cls) && entry.Version == version
                && string.Equals(entry.Name, name, StringComparison.Ordinal)
                && (entry.ShadowSlot < 0 || !instance.IsSlotSet(entry.ShadowSlot)))
                return entry.Plan;
        }
        if (instance.TryOwnAttribute(name, out _) || !cls.TryLookup(name, out PyClass owner, out object? raw)
            || raw is not PyFunction function)
            return PyOps.CaptureMethodTarget(instance, name);
        var plan = new CapturedClassPlan(owner, function);
        CapturedPlans[NextVictim(CapturedPlanVictims, site)] = new CapturedPlanEntry
        {
            Class = cls, Name = name, Version = version, Plan = plan,
            ShadowSlot = cls.TryGetInstanceSlot(name, out int slot) ? slot : -1,
        };
        return plan;
    }

    // C=1：冷函数miss；I=0：capture及布局helper均NoInlining。
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static object? CaptureMethodPlanAblationCold1Inline0(int site, object? receiver, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || receiver is not PyInstance instance
            || instance.GetType() != typeof(PyInstance) || instance.ForeignInstance is not null)
            return PyOps.CaptureMethodTarget(receiver, name);
        PyClass cls = instance.Class;
        cls.RequireLexicalFactoryReady();
        if (!IsPlainCaptureClassAblationI0(cls)) return PyOps.CaptureMethodTarget(receiver, name);
        long version = PyClass.GlobalVersion;
        for (int index = site * EntriesPerSite; index < (site + 1) * EntriesPerSite; index++)
        {
            ref CapturedPlanEntry entry = ref CapturedPlans[index];
            if (entry.Plan is not null && ReferenceEquals(entry.Class, cls) && entry.Version == version
                && string.Equals(entry.Name, name, StringComparison.Ordinal)
                && (entry.ShadowSlot < 0 || !instance.IsSlotSet(entry.ShadowSlot)))
                return entry.Plan;
        }
        return CaptureMethodPlanMiss(site, instance, name, cls, version);
    }

    // C=1：冷函数miss；I=1：capture及布局helper均AggressiveInlining。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? CaptureMethodPlanAblationCold1Inline1(int site, object? receiver, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || receiver is not PyInstance instance
            || instance.GetType() != typeof(PyInstance) || instance.ForeignInstance is not null)
            return PyOps.CaptureMethodTarget(receiver, name);
        PyClass cls = instance.Class;
        cls.RequireLexicalFactoryReady();
        if (!IsPlainCaptureClassAblationI1(cls)) return PyOps.CaptureMethodTarget(receiver, name);
        long version = PyClass.GlobalVersion;
        for (int index = site * EntriesPerSite; index < (site + 1) * EntriesPerSite; index++)
        {
            ref CapturedPlanEntry entry = ref CapturedPlans[index];
            if (entry.Plan is not null && ReferenceEquals(entry.Class, cls) && entry.Version == version
                && string.Equals(entry.Name, name, StringComparison.Ordinal)
                && (entry.ShadowSlot < 0 || !instance.IsSlotSet(entry.ShadowSlot)))
                return entry.Plan;
        }
        return CaptureMethodPlanMiss(site, instance, name, cls, version);
    }

    // 每次仍遍历完整MRO；布局setter不全受GlobalVersion保护，不能缓存准入资格。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsPlainCaptureClassAblationI0(PyClass cls)
    {
        foreach (PyClass item in cls.MroClasses())
            if (item.HasSlotLayout || item.HasDictC3Layout || item.HasStringLayout || item.HasListLayout
                || item.HasDictLayout || item.NamedTupleLayout is not null || item.IsTypingGenericFamily
                || item.IsTypingIoFamily || item.HasEnumMetadata || item.IsNativeRuntimeClass
                || item.HasForeignHierarchy || item.BuiltinBase is not null || item.ExceptionFactory is not null)
                return false;
        return true;
    }

    // 每次仍遍历完整MRO；布局setter不全受GlobalVersion保护，不能缓存准入资格。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPlainCaptureClassAblationI1(PyClass cls)
    {
        foreach (PyClass item in cls.MroClasses())
            if (item.HasSlotLayout || item.HasDictC3Layout || item.HasStringLayout || item.HasListLayout
                || item.HasDictLayout || item.NamedTupleLayout is not null || item.IsTypingGenericFamily
                || item.IsTypingIoFamily || item.HasEnumMetadata || item.IsNativeRuntimeClass
                || item.HasForeignHierarchy || item.BuiltinBase is not null || item.ExceptionFactory is not null)
                return false;
        return true;
    }

}
