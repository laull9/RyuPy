// 独立分配诊断：预热真实调用循环，复用反射参数，避免把PIC首次建缓存误算逐轮分配。
using System.Reflection;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
assembly.EntryPoint!.Invoke(null, assembly.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var methods = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).ToArray();
object?[] input = [20000L];
foreach (string label in new[] { "alias", "direct" }) {
    var method = methods.Single(m => m.Name == "m0_call_" + label);
    void Check(object? result) {
        if (result is not long value || value != 399980000) throw new Exception("调用结果改变");
    }
    for (int i = 0; i < 20; i++) Check(method.Invoke(null, input));
    for (int sample = 0; sample < 5; sample++) {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        object? result = method.Invoke(null, input);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(result);
        Console.WriteLine($"allocation {label} {sample} {bytes} 20000 {bytes / 20000.0:R}");
    }
}
