// 尚未构建的独立区间分配诊断模板：输入/反射参数复用，正式计时另由compare.py执行。
using System.Reflection;
using RyuPy.Runtime;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
assembly.EntryPoint!.Invoke(null, assembly.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var type = assembly.GetType("RyuPy.Generated.Module")!;
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
foreach (var (label, name, argument, expected) in new (string, string, object, string)[] {
    ("call_true", "call_diamond", true, "100000"), ("call_false", "call_diamond", false, "180000"),
    ("loop_true", "loop_diamond", true, "100000"), ("loop_false", "loop_diamond", false, "180000"),
    ("unknown_int", "unknown_loop", 11L, "260000"), ("unknown_bool", "unknown_loop", true, "60000"),
    ("unknown_overflow", "unknown_loop", long.MaxValue, "184467440737095516180000") }) {
    var method = type.GetMethod("m0_" + name, flags)!;
    object?[] input = [20000L, argument];
    void Check(object? result) {
        if (PyRepr.Str(result) != expected) throw new Exception("区间负载结果改变");
    }
    for (int warm = 0; warm < 20; warm++) Check(method.Invoke(null, input));
    for (int sample = 0; sample < 5; sample++) {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        object? result = method.Invoke(null, input);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(result);
        Console.WriteLine($"allocation {label} {sample} {bytes} 20000 {bytes / 20000.0:R}");
    }
}
