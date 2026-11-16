// 调用前捕获普通类函数的不可变计划；缓存不持有接收实例，原方法ABI和编译器尚未接线。
using System;
using System.Runtime.CompilerServices;

// 仅隔离诊断host可消费内部实验入口，不新增产品公共ABI。
[assembly: InternalsVisibleTo("RyuPy.CapturedPlanPrototypeHost")]

namespace RyuPy.Runtime;

public static partial class PyPic
{
    // 此委托必须由明确创建者与原Body共同实现同一语义，不能从任意Body推断。
    // 参数是context、实际function与最多四个Python位置值（首个通常为receiver）。
    internal delegate object? ExperimentalScalarCall(object? context, PyFunction function,
        object? p0, object? p1, object? p2, object? p3);

    // 两位置值专用委托，隔离测试统一宽委托的空槽传参成本。
    internal delegate object? ExperimentalScalar2Call(object? context, PyFunction function, object? p0, object? p1);

    private sealed class ScalarCapability(int arity, object? context, ExperimentalScalarCall call,
        ExperimentalScalar2Call? call2 = null)
    {
        internal readonly int Arity = arity;
        internal readonly object? Context = context;
        internal readonly ExperimentalScalarCall Call = call;
        internal readonly ExperimentalScalar2Call? Call2 = call2;
    }

    // 弱键防止独立函数永久驻留；值可合法引用键，依赖CWT的ephemeron语义。
    // 能力一次登记不可替换；计划在miss读取，热调用不查弱表。
    private static readonly ConditionalWeakTable<PyFunction, ScalarCapability> ScalarCapabilities = new();

    internal static void RegisterExperimentalScalar(PyFunction function, int arity, ExperimentalScalarCall call) =>
        RegisterScalarCore(function, arity, call, null);

    internal static void RegisterExperimentalScalar2(PyFunction function, ExperimentalScalar2Call call) =>
        RegisterScalarCore(function, 2, (ctx, fn, p0, p1, _, _) => call(ctx, fn, p0, p1), call);

    private static void RegisterScalarCore(PyFunction function, int arity, ExperimentalScalarCall call,
        ExperimentalScalar2Call? call2)
    {
        if (arity < 1 || arity > 4 || !function.Signature.SimplePositional
            || function.Signature.Names.Length != arity)
            throw new PyRuntimeError("invalid experimental scalar contract");
        // 不替换Body.Target，元数据和来源检查继续看到原PyFunctionOwner。
        object? context = function.Body.Target is PyFunctionOwner ? PyFunctionOwner.GetContext(function) : null;
        try { ScalarCapabilities.Add(function, new ScalarCapability(arity, context, call, call2)); }
        catch (ArgumentException) { throw new PyRuntimeError("scalar capability already registered"); }
    }

    // 独立实验入口：不改原CallCapturedPlan1，保留同次完整baseline。
    // 无super热路没有EH，带super、star、无能力和非计划一律回原合同路径。
    internal static object? CallExperimentalScalarNoSuper(int site, bool captured, object? plan,
        object? receiver, string name, object? argument)
    {
        if (captured && plan is CapturedClassPlan bound && bound.Scalar is { Arity: 2 } capability
            && !bound.Function.UsesSuper && !PyOps.IsStarredArgument(argument))
        {
            PyLexicalClassGuard.Receiver(receiver);
            return capability.Call(capability.Context, bound.Function, receiver, argument, null, null);
        }
        return CallCapturedPlan1(site, captured, plan, receiver, name, argument);
    }

    // 窄委托另查Call2能力；不删除任何判据，不省略receiver late guard。
    internal static object? CallExperimentalScalar2(int site, bool captured, object? plan,
        object? receiver, string name, object? argument)
    {
        if (captured && plan is CapturedClassPlan bound && bound.Scalar is { Arity: 2, Call2: not null } capability
            && !bound.Function.UsesSuper && !PyOps.IsStarredArgument(argument))
        {
            PyLexicalClassGuard.Receiver(receiver);
            return capability.Call2(capability.Context, bound.Function, receiver, argument);
        }
        return CallCapturedPlan1(site, captured, plan, receiver, name, argument);
    }

    private static object? InvokeScalar(CapturedClassPlan plan, object? receiver,
        object? a0 = null, object? a1 = null, object? a2 = null)
    {
        ScalarCapability capability = plan.Scalar!;
        PyLexicalClassGuard.Receiver(receiver);
        if (!plan.Function.UsesSuper)
            return capability.Call(capability.Context, plan.Function, receiver, a0, a1, a2);
        PyClass? oldOwner = PyMethodContext.CurrentOwner;
        object? oldSelf = PyMethodContext.CurrentSelf;
        PyMethodContext.SetCurrent(plan.Owner, receiver);
        try { return capability.Call(capability.Context, plan.Function, receiver, a0, a1, a2); }
        finally { PyMethodContext.SetCurrent(oldOwner, oldSelf); }
    }

    // 计划不可变且不存receiver；站点替换不能改写已保存的调用局部快照。
    // 类、定义owner和函数仍被有界缓存强持有，不承诺这些元数据立即回收。
    private sealed class CapturedClassPlan(PyClass owner, PyFunction function)
    {
        internal readonly PyClass Owner = owner;
        internal readonly PyFunction Function = function;
        internal readonly ScalarCapability? Scalar = ScalarCapabilities.TryGetValue(function, out var value) ? value : null;
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

    /// <summary>零参计划调用；只使用捕获快照，无标量能力时传入独立参数数组。</summary>
    public static object? CallCapturedPlan0(int site, bool captured, object? plan, object? receiver, string name)
    {
        if (captured && plan is CapturedClassPlan bound)
        {
            if (bound.Scalar?.Arity == 1) return InvokeScalar(bound, receiver);
            return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver], [], []);
        }
        return CallCapturedMethod0(site, captured, plan, name);
    }

    /// <summary>单参计划调用；星号仍按原协议展开，不把计划暴露为Python可调用值。</summary>
    public static object? CallCapturedPlan1(int site, bool captured, object? plan, object? receiver, string name, object? a0)
    {
        if (captured && plan is CapturedClassPlan bound)
        {
            if (!PyOps.IsStarredArgument(a0))
            {
                if (bound.Scalar?.Arity == 2) return InvokeScalar(bound, receiver, a0);
                return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver, a0], [], []);
            }
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
            {
                if (bound.Scalar?.Arity == 3) return InvokeScalar(bound, receiver, a0, a1);
                return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver, a0, a1], [], []);
            }
            plan = new PyBoundMethod(bound.Owner, receiver, bound.Function);
        }
        return CallCapturedMethod2(site, captured, plan, name, a0, a1);
    }

    /// <summary>三参计划调用；任意Body仍用独立数组，只有显式标量能力可省略。</summary>
    public static object? CallCapturedPlan3(int site, bool captured, object? plan, object? receiver, string name, object? a0, object? a1, object? a2)
    {
        if (captured && plan is CapturedClassPlan bound)
        {
            if (!PyOps.IsStarredArgument(a0) && !PyOps.IsStarredArgument(a1) && !PyOps.IsStarredArgument(a2))
            {
                if (bound.Scalar?.Arity == 4) return InvokeScalar(bound, receiver, a0, a1, a2);
                return PyObject.InvokePrepared(bound.Function, bound.Owner, receiver, [receiver, a0, a1, a2], [], []);
            }
            plan = new PyBoundMethod(bound.Owner, receiver, bound.Function);
        }
        return CallCapturedMethod3(site, captured, plan, name, a0, a1, a2);
    }
}
