// 六个同形独立循环隔离巨大Main的布局；仍为手写Runtime诊断，不代表编译器产品。
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

internal static class AblationDrivers
{
    internal static void Run()
    {
        var cls = new PyClass("AblationCounter");
        var receiver = new PyInstance(cls);
        var function = new PyFunction("f", PySignature.Create("f", ["self", "x"], ["pos", "pos"], ["0", "0"]),
            static (_, args, _, _) => args[1], [], false);
        PyPic.RegisterExperimentalScalar2(function, static (_, _, _, x) => x);
        cls.SetAttr("f", function);
        object value = 7L;
        const int count = 100000;
        string[] names = ["original", "legacy_split", "c0i0", "c0i1", "c1i0", "c1i1"];
        Func<int, object?, object?, int, object?>[] drivers = [Original, LegacySplit, C0I0, C0I1, C1I0, C1I1];
        Func<int, object?, string, object?>[] captures = [PyPic.CaptureMethodPlan, PyPic.CaptureMethodPlanSplit,
            PyPic.CaptureMethodPlanAblationCold0Inline0, PyPic.CaptureMethodPlanAblationCold0Inline1,
            PyPic.CaptureMethodPlanAblationCold1Inline0, PyPic.CaptureMethodPlanAblationCold1Inline1];
        bool enabled = Environment.GetEnvironmentVariable("RYUPY_PIC") != "0";
        for (int arm = 0; arm < captures.Length; arm++)
        {
            object? first = captures[arm](3520 + arm, receiver, "f");
            object? second = captures[arm](3520 + arm, receiver, "f");
            if (enabled ? !ReferenceEquals(first, second) : second is not PyBoundMethod)
                throw new Exception("消融捕获快路身份变化");
        }
        // 委托只在循环外调一次；每臂循环直接调自身捕获入口，无逐次间接选择成本。
        long warmup = Stopwatch.GetTimestamp();
        int batch = -1, cycle = 0;
        while (batch < 20)
        {
            if (batch < 0 && Stopwatch.GetElapsedTime(warmup).TotalSeconds >= 3) batch = 0;
            bool reverse = cycle++ % 2 != 0;
            for (int j = 0; j < drivers.Length; j++)
            {
                int arm = reverse ? drivers.Length - 1 - j : j;
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();
                object? result = drivers[arm](3520 + arm, receiver, value, count);
                long ticks = Stopwatch.GetTimestamp() - start;
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                if (!ReferenceEquals(result, value)) throw new Exception("消融返回身份变化");
                double ms = ticks * 1000.0 / Stopwatch.Frequency;
                if (batch >= 0)
                    Console.WriteLine(FormattableString.Invariant($"{{\"batch\":{batch},\"arm\":\"{names[arm]}\",\"count\":{count},\"ms\":{ms},\"allocation\":{allocated}}}"));
            }
            if (batch >= 0) batch++;
        }
    }

    // 同形参数与循环边界；入口只改变指定消融因子。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? Original(int site, object? receiver, object? value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlan(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同形参数与循环边界；入口只改变指定消融因子。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? LegacySplit(int site, object? receiver, object? value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanSplit(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同形参数与循环边界；入口只改变指定消融因子。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? C0I0(int site, object? receiver, object? value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同形参数与循环边界；入口只改变指定消融因子。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? C0I1(int site, object? receiver, object? value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold0Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同形参数与循环边界；入口只改变指定消融因子。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? C1I0(int site, object? receiver, object? value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline0(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }

    // 同形参数与循环边界；入口只改变指定消融因子。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? C1I1(int site, object? receiver, object? value, int count)
    {
        object? result = null;
        for (int i = 0; i < count; i++)
        {
            object? plan = PyPic.CaptureMethodPlanAblationCold1Inline1(site, receiver, "f");
            result = PyPic.CallExperimentalScalar2(site, true, plan, receiver, "f", value);
        }
        return result;
    }
}
