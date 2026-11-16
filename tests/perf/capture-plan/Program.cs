// Runtime 原型诊断：三臂交错批次，参数预装箱，循环内测时间和线程分配。
using System.Diagnostics;
using RyuPy.Runtime;

var cls = new PyClass("PlanCounter");
var receiver = new PyInstance(cls);
cls.SetAttr("f", new PyFunction("f", PySignature.Create("f", [], [], []),
    (_, args, _, _) => args[1], [], false));
// 防止超界站点或未来判据变化导致把回退路径误记为计划快路。
object? probe = PyPic.CaptureMethodPlan(3512, receiver, "f");
if (Environment.GetEnvironmentVariable("RYUPY_PIC") != "0"
    && !ReferenceEquals(probe, PyPic.CaptureMethodPlan(3512, receiver, "f")))
    throw new Exception("计划快路未命中");
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
    string[] arms = cycle++ % 2 == 0 ? ["late", "bound", "plan"] : ["plan", "bound", "late"];
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
        if (!ReferenceEquals(result, value)) throw new Exception("返回身份变化");
        double ms = ticks * 1000.0 / Stopwatch.Frequency;
        if (batch >= 0)
            Console.WriteLine(FormattableString.Invariant($"{{\"batch\":{batch},\"arm\":\"{arm}\",\"count\":{count},\"ms\":{ms},\"allocation\":{allocation}}}"));
    }
    if (batch >= 0) batch++;
}
