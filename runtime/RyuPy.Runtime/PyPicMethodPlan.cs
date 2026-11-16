// 调用前捕获普通类函数的不可变计划；缓存不持有接收实例，原方法ABI和编译器尚未接线。
using System;

namespace RyuPy.Runtime;

public static partial class PyPic
{
    // 计划不可变且不存receiver；站点替换不能改写已保存的调用局部快照。
    // 类、定义owner和函数仍被有界缓存强持有，不承诺这些元数据立即回收。
    private sealed class CapturedClassPlan(PyClass owner, PyFunction function)
    {
        internal readonly PyClass Owner = owner;
        internal readonly PyFunction Function = function;
    }

    private struct CapturedPlanEntry
    {
        internal PyClass? Class;
        internal string? Name;
        internal long Version;
        internal int ShadowSlot;
        internal CapturedClassPlan? Plan;
    }

    private static readonly PagedEntries<CapturedPlanEntry> CapturedPlans = new(MaxSites * EntriesPerSite);
    private static readonly byte[] CapturedPlanVictims = new byte[MaxSites];

    /// <summary>实参求值前捕获目标；仅精确普通实例的直接类函数可形成站点计划。</summary>
    public static object? CaptureMethodPlan(int site, object? receiver, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || receiver is not PyInstance instance
            || instance.GetType() != typeof(PyInstance) || instance.ForeignInstance is not null)
            return PyOps.CaptureMethodTarget(receiver, name);
        PyClass cls = instance.Class;
        cls.RequireLexicalFactoryReady();
        // 特殊族可能在类查找之前拦截属性；不能仅凭返回绑定方法的外观认定来源。
        // MroClasses是结构体枚举器；尚未证明所有布局字段受版本保护，命中前也检查。
        if (!IsPlainCaptureClass(cls)) return PyOps.CaptureMethodTarget(receiver, name);
        long version = PyClass.GlobalVersion;
        for (int index = site * EntriesPerSite; index < (site + 1) * EntriesPerSite; index++)
        {
            ref CapturedPlanEntry entry = ref CapturedPlans[index];
            if (entry.Plan is not null && ReferenceEquals(entry.Class, cls) && entry.Version == version
                && string.Equals(entry.Name, name, StringComparison.Ordinal)
                && (entry.ShadowSlot < 0 || !instance.IsSlotSet(entry.ShadowSlot)))
                return entry.Plan;
        }
        // property、__getattr__、实例值即使返回绑定方法也不缓存；首切不准入classmethod。
        if (instance.TryOwnAttribute(name, out _) || !cls.TryLookup(name, out PyClass owner, out object? raw)
            || raw is not PyFunction function)
            return PyOps.CaptureMethodTarget(receiver, name);
        var plan = new CapturedClassPlan(owner, function);
        CapturedPlans[NextVictim(CapturedPlanVictims, site)] = new CapturedPlanEntry
        {
            Class = cls, Name = name, Version = version, Plan = plan,
            ShadowSlot = cls.TryGetInstanceSlot(name, out int slot) ? slot : -1,
        };
        return plan;
    }

    private static bool IsPlainCaptureClass(PyClass cls)
    {
        foreach (PyClass item in cls.MroClasses())
            if (item.HasSlotLayout || item.HasDictC3Layout || item.HasStringLayout || item.HasListLayout
                || item.HasDictLayout || item.NamedTupleLayout is not null || item.IsTypingGenericFamily
                || item.IsTypingIoFamily || item.HasEnumMetadata || item.IsNativeRuntimeClass
                || item.HasForeignHierarchy || item.BuiltinBase is not null || item.ExceptionFactory is not null)
                return false;
        return true;
    }

    /// <summary>零参计划调用；只使用捕获快照，每次传入独立的含self参数数组。</summary>
    public static object? CallCapturedPlan0(int site, bool captured, object? plan, object? receiver, string name)
    {
        if (captured && plan is CapturedClassPlan bound)
            return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver], [], []);
        return CallCapturedMethod0(site, captured, plan, name);
    }

    /// <summary>单参计划调用；星号仍按原协议展开，不把计划暴露为Python可调用值。</summary>
    public static object? CallCapturedPlan1(int site, bool captured, object? plan, object? receiver, string name, object? a0)
    {
        if (captured && plan is CapturedClassPlan bound)
        {
            if (!PyOps.IsStarredArgument(a0))
                return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver, a0], [], []);
            plan = new PyBoundMethod(bound.Owner, receiver, bound.Function);
        }
        return CallCapturedMethod1(site, captured, plan, name, a0);
    }

    /// <summary>双参计划调用；参数重绑、同站点重入后均不得重新查找方法。</summary>
    public static object? CallCapturedPlan2(int site, bool captured, object? plan, object? receiver, string name, object? a0, object? a1)
    {
        if (captured && plan is CapturedClassPlan bound)
        {
            if (!PyOps.IsStarredArgument(a0) && !PyOps.IsStarredArgument(a1))
                return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver, a0, a1], [], []);
            plan = new PyBoundMethod(bound.Owner, receiver, bound.Function);
        }
        return CallCapturedMethod2(site, captured, plan, name, a0, a1);
    }

    /// <summary>三参计划调用；任意函数正文可保留数组，禁止复用站点scratch或池。</summary>
    public static object? CallCapturedPlan3(int site, bool captured, object? plan, object? receiver, string name, object? a0, object? a1, object? a2)
    {
        if (captured && plan is CapturedClassPlan bound)
        {
            if (!PyOps.IsStarredArgument(a0) && !PyOps.IsStarredArgument(a1) && !PyOps.IsStarredArgument(a2))
                return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver, a0, a1, a2], [], []);
            plan = new PyBoundMethod(bound.Owner, receiver, bound.Function);
        }
        return CallCapturedMethod3(site, captured, plan, name, a0, a1, a2);
    }
}
