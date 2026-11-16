// 四种手写Runtime场景的同形五臂循环；不是编译器输出，也不改变产品默认路径。
// compiler_shape仅模拟捕获→独立参数求值→调用；invalidate_miss包含类写入及失效开销。
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

internal static class ScenarioDrivers
{
    private delegate object? Driver(int site, PyInstance[] receivers, PyClass invalidator, object value, int count);

    internal static void Run(string scenario)
    {
        Driver[] drivers = scenario switch
        {
            "compiler_shape" => [ShapeOriginal, ShapeC0I0, ShapeC0I1, ShapeC1I0, ShapeC1I1],
            "mro_depth8" => [DepthOriginal, DepthC0I0, DepthC0I1, DepthC1I0, DepthC1I1],
            "polymorphic4" => [PolyOriginal, PolyC0I0, PolyC0I1, PolyC1I0, PolyC1I1],
            "invalidate_miss" => [MissOriginal, MissC0I0, MissC0I1, MissC1I0, MissC1I1],
            _ => throw new ArgumentException("未知捕获场景", nameof(scenario)),
        };
        var root = new PyClass("ScenarioRoot");
        var function = new PyFunction("f", PySignature.Create("f", ["self", "x"], ["pos", "pos"], ["0", "0"]),
            static (_, args, _, _) => args[1], [], false);
        PyPic.RegisterExperimentalScalar2(function, static (_, _, _, x) => x);
        root.SetAttr("f", function);
        PyClass leaf = root;
        // 深度明确为8个用户类，包含root；单继承MakeClass保留真实MRO与布局判据。
        if (scenario == "mro_depth8")
            for (int depth = 1; depth < 8; depth++)
                leaf = (PyClass)PyOps.MakeClass("ScenarioDepth" + depth, [leaf], null);
        PyInstance[] receivers = scenario == "polymorphic4"
            ? [new(root), new(NewSibling("ScenarioPoly1", function)),
                new(NewSibling("ScenarioPoly2", function)), new(NewSibling("ScenarioPoly3", function))]
            : [new(leaf)];
        var invalidator = new PyClass("ScenarioInvalidator");
        object value = 7L; // 所有臂复用同一预装箱值、函数及接收实例，不在循环里装箱。
        invalidator.SetAttr("tick", value);
        Validate(scenario, receivers, invalidator, value);
        const int count = 100000;
        string[] names = ["original", "c0i0", "c0i1", "c1i0", "c1i1"];
        long warmup = Stopwatch.GetTimestamp();
        int batch = -1, cycle = 0;
        // 每次调用只运行一个场景；至少3秒预热，随后20批，臂顺序正反交替。
        while (batch < 20)
        {
            if (batch < 0 && Stopwatch.GetElapsedTime(warmup).TotalSeconds >= 3) batch = 0;
            bool reverse = cycle++ % 2 != 0;
            for (int j = 0; j < drivers.Length; j++)
            {
                int arm = reverse ? drivers.Length - 1 - j : j;
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();
                object? result = drivers[arm](3600 + arm, receivers, invalidator, value, count);
                long ticks = Stopwatch.GetTimestamp() - start;
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                if (!ReferenceEquals(result, value)) throw new Exception("场景返回身份变化");
                double ms = ticks * 1000.0 / Stopwatch.Frequency;
                if (batch >= 0)
                    Console.WriteLine(FormattableString.Invariant($"{{\"scenario\":\"{scenario}\",\"batch\":{batch},\"arm\":\"{names[arm]}\",\"reverse\":{(reverse ? "true" : "false")},\"count\":{count},\"ms\":{ms},\"allocation\":{allocated}}}"));
            }
            if (batch >= 0) batch++;
        }
    }

    private static PyClass NewSibling(string name, PyFunction function)
    {
        var cls = new PyClass(name);
        cls.SetAttr("f", function);
        return cls;
    }

    // 检查场景确实进入预期缓存/回退路径；只在计时外使用委托选入口。
    private static void Validate(string scenario, PyInstance[] receivers, PyClass invalidator, object value)
    {
        Func<int, object?, string, object?>[] captures = [PyPic.CaptureMethodPlan,
            PyPic.CaptureMethodPlanAblationCold0Inline0, PyPic.CaptureMethodPlanAblationCold0Inline1,
            PyPic.CaptureMethodPlanAblationCold1Inline0, PyPic.CaptureMethodPlanAblationCold1Inline1];
        bool enabled = Environment.GetEnvironmentVariable("RYUPY_PIC") != "0";
        if (scenario == "mro_depth8")
        {
            int depth = 0;
            for (PyClass? cls = receivers[0].Class; cls is not null; cls = cls.BaseClass) depth++;
            if (depth != 8) throw new Exception("MRO深度不符");
        }
        for (int arm = 0; arm < captures.Length; arm++)
        {
            int site = 3600 + arm;
            object?[] plans = new object?[receivers.Length];
            for (int i = 0; i < receivers.Length; i++) plans[i] = captures[arm](site, receivers[i], "f");
            for (int i = 0; i < receivers.Length; i++)
            {
                object? again = captures[arm](site, receivers[i], "f");
                if (enabled ? !ReferenceEquals(plans[i], again) : again is not PyBoundMethod)
                    throw new Exception("场景捕获快路身份变化");
                if (!ReferenceEquals(value, PyPic.CallExperimentalScalar2(site, true, again, receivers[i], "f", value)))
                    throw new Exception("场景调用结果变化");
            }
            if (scenario == "invalidate_miss")
            {
                long version = PyClass.GlobalVersion;
                invalidator.SetAttr("tick", value);
                if (PyClass.GlobalVersion == version) throw new Exception("类写入未失效");
                object? changed = captures[arm](site, receivers[0], "f");
                if (enabled && ReferenceEquals(plans[0], changed)) throw new Exception("失效后复用了旧计划");
                if (!ReferenceEquals(value, PyPic.CallExperimentalScalar2(site, true, plans[0], receivers[0], "f", value)))
                    throw new Exception("失效破坏旧快照");
            }
        }
    }

    // 手写参数求值屏障，无真实Python副作用；不代表编译器已经接线。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object EvaluateArgument(object value) => value;

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ShapeOriginal(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlan(site, receiver, "f");
            object argument = EvaluateArgument(value);
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", argument);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ShapeC0I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline0(site, receiver, "f");
            object argument = EvaluateArgument(value);
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", argument);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ShapeC0I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline1(site, receiver, "f");
            object argument = EvaluateArgument(value);
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", argument);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ShapeC1I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline0(site, receiver, "f");
            object argument = EvaluateArgument(value);
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", argument);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ShapeC1I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline1(site, receiver, "f");
            object argument = EvaluateArgument(value);
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", argument);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? DepthOriginal(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlan(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? DepthC0I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? DepthC0I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? DepthC1I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? DepthC1I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? PolyOriginal(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            PyInstance receiver = receivers[i & 3];
            object? plan = PyPic.CaptureMethodPlan(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? PolyC0I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            PyInstance receiver = receivers[i & 3];
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? PolyC0I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            PyInstance receiver = receivers[i & 3];
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? PolyC1I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            PyInstance receiver = receivers[i & 3];
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? PolyC1I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            PyInstance receiver = receivers[i & 3];
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? MissOriginal(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            // 每次SetAttr都Touch全局版本；本指标包含变更、守卫及计划分配成本。
            invalidator.SetAttr("tick", value);
            object? plan = PyPic.CaptureMethodPlan(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? MissC0I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            // 每次SetAttr都Touch全局版本；本指标包含变更、守卫及计划分配成本。
            invalidator.SetAttr("tick", value);
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? MissC0I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            // 每次SetAttr都Touch全局版本；本指标包含变更、守卫及计划分配成本。
            invalidator.SetAttr("tick", value);
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? MissC1I0(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            // 每次SetAttr都Touch全局版本；本指标包含变更、守卫及计划分配成本。
            invalidator.SetAttr("tick", value);
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同场景的五臂只替换捕获入口；循环内无委托或switch选入口。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? MissC1I1(int site, PyInstance[] receivers, PyClass invalidator, object value, int count)
    {
        object? result = null;
        PyInstance receiver = receivers[0];
        for (int i = 0; i < count; i++)
        {
            // 每次SetAttr都Touch全局版本；本指标包含变更、守卫及计划分配成本。
            invalidator.SetAttr("tick", value);
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

}
