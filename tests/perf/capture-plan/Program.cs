// Runtime 原型诊断：完整调用与分解诊断交错批次，参数预装箱，循环内测时间和线程分配。
using System.Diagnostics;
using RyuPy.Runtime;

var cls = new PyClass("PlanCounter");
var receiver = new PyInstance(cls);
cls.SetAttr("f", new PyFunction("f", PySignature.Create("f", ["self", "x"], ["pos", "pos"], ["0", "0"]),
    (_, args, _, _) => args[1], [], false));
// 防止超界站点或未来判据变化导致把回退路径误记为计划快路。
object? probe = PyPic.CaptureMethodPlan(3512, receiver, "f");
if (Environment.GetEnvironmentVariable("RYUPY_PIC") != "0"
    && !ReferenceEquals(probe, PyPic.CaptureMethodPlan(3512, receiver, "f")))
    throw new Exception("计划快路未命中");
// 内部手写标量委托仅验证机制成本，不代表编译器已生成此入口。
var scalarClass = new PyClass("ScalarCounter");
var scalarReceiver = new PyInstance(scalarClass);
var scalarFunction = new PyFunction("f", PySignature.Create("f", ["self", "x"], ["pos", "pos"], ["0", "0"]),
    static (_, args, _, _) => args[1], [], false);
PyPic.RegisterExperimentalScalar(scalarFunction, 2, static (_, _, _, x, _, _) => x);
scalarClass.SetAttr("f", scalarFunction);
object? scalarProbe = PyPic.CaptureMethodPlan(3513, scalarReceiver, "f");
if (Environment.GetEnvironmentVariable("RYUPY_PIC") != "0"
    && !ReferenceEquals(scalarProbe, PyPic.CaptureMethodPlan(3513, scalarReceiver, "f")))
    throw new Exception("标量计划缓存未命中");
var narrowClass = new PyClass("NarrowCounter");
var narrowReceiver = new PyInstance(narrowClass);
var narrowFunction = new PyFunction("f", PySignature.Create("f", ["self", "x"], ["pos", "pos"], ["0", "0"]),
    static (_, args, _, _) => args[1], [], false);
PyPic.RegisterExperimentalScalar2(narrowFunction, static (_, _, _, x) => x);
narrowClass.SetAttr("f", narrowFunction);
// 初始化类变更会让之前缓存过期，在所有定义完成后固定分解臂的计划。
scalarProbe = PyPic.CaptureMethodPlan(3513, scalarReceiver, "f");
object? splitProbe = PyPic.CaptureMethodPlanSplit(3515, narrowReceiver, "f");
bool picEnabled = Environment.GetEnvironmentVariable("RYUPY_PIC") != "0";
const int count = 100000;
object value = 7L;
// 按时间预热同一调用站点，避免短批次结束时仍处于分层编译过渡期。
// 不把这视为稳态证明，报告保留逐批数据供检查漂移。
long warmupStart = Stopwatch.GetTimestamp();
int batch = -1;
int cycle = 0;
while (batch < 20)
{
    if (batch < 0 && Stopwatch.GetElapsedTime(warmupStart).TotalSeconds >= 3) batch = 0;
    string[] arms = cycle++ % 2 == 0
        ? ["late", "bound", "plan", "scalar", "scalar_split", "scalar_narrow", "capture_only", "invoke_only", "scalar_capture_split", "capture_split_only"]
        : ["capture_split_only", "scalar_capture_split", "invoke_only", "capture_only", "scalar_narrow", "scalar_split", "scalar", "plan", "bound", "late"];
    foreach (string arm in arms)
    {
        object? result = null;
        long allocation = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        switch (arm)
        {
            case "late":
                for (int i = 0; i < count; i++) result = PyPic.CallMethod1(3510, receiver, "f", value);
                break;
            case "bound":
                for (int i = 0; i < count; i++)
                {
                    object? target = PyOps.CaptureMethodTarget(receiver, "f");
                    result = PyPic.CallCapturedMethod1(3511, true, target, "f", value);
                }
                break;
            case "capture_only":
                for (int i = 0; i < count; i++) result = PyPic.CaptureMethodPlan(3513, scalarReceiver, "f");
                break;
            case "invoke_only":
                for (int i = 0; i < count; i++)
                    result = PyPic.CallCapturedPlan1(3513, true, scalarProbe, scalarReceiver, "f", value);
                break;
            case "scalar_split":
                for (int i = 0; i < count; i++)
                {
                    object? plan = PyPic.CaptureMethodPlan(3513, scalarReceiver, "f");
                    result = PyPic.CallExperimentalScalarNoSuper(3513, true, plan, scalarReceiver, "f", value);
                }
                break;
            case "scalar_narrow":
                for (int i = 0; i < count; i++)
                {
                    object? plan = PyPic.CaptureMethodPlan(3514, narrowReceiver, "f");
                    result = PyPic.CallExperimentalScalar2(3514, true, plan, narrowReceiver, "f", value);
                }
                break;
            case "capture_split_only":
                for (int i = 0; i < count; i++) result = PyPic.CaptureMethodPlanSplit(3515, narrowReceiver, "f");
                break;
            case "scalar_capture_split":
                for (int i = 0; i < count; i++)
                {
                    object? plan = PyPic.CaptureMethodPlanSplit(3515, narrowReceiver, "f");
                    result = PyPic.CallExperimentalScalar2(3515, true, plan, narrowReceiver, "f", value);
                }
                break;
            case "scalar":
                for (int i = 0; i < count; i++)
                {
                    object? plan = PyPic.CaptureMethodPlan(3513, scalarReceiver, "f");
                    result = PyPic.CallCapturedPlan1(3513, true, plan, scalarReceiver, "f", value);
                }
                break;
            case "plan":
                for (int i = 0; i < count; i++)
                {
                    object? plan = PyPic.CaptureMethodPlan(3512, receiver, "f");
                    result = PyPic.CallCapturedPlan1(3512, true, plan, receiver, "f", value);
                }
                break;
        }
        long ticks = Stopwatch.GetTimestamp() - start;
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
        if (arm is "capture_only" or "capture_split_only")
        {
            object? expected = arm == "capture_only" ? scalarProbe : splitProbe;
            if (picEnabled ? !ReferenceEquals(result, expected) : result is not PyBoundMethod)
                throw new Exception("分解捕获身份变化");
        }
        else if (!ReferenceEquals(result, value)) throw new Exception("返回身份变化");
        double ms = ticks * 1000.0 / Stopwatch.Frequency;
        if (batch >= 0)
            Console.WriteLine(FormattableString.Invariant($"{{\"batch\":{batch},\"arm\":\"{arm}\",\"count\":{count},\"ms\":{ms},\"allocation\":{allocation}}}"));
    }
    if (batch >= 0) batch++;
}
