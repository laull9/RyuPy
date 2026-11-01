// 独立诊断加载真实编译产物，不是生产执行协议。先完整初始化模块再访问同一Record对象。
using RyuPy.Runtime;
using System.Diagnostics;
using System.Reflection;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
assembly.EntryPoint!.Invoke(null, assembly.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var method = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).Single(m => m.Name == "m0_field_reads");
var value = method.DeclaringType!.GetField("m0___mv3_first", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
object?[] invokeArgs = [200000L, value];
for (int i = 0; i < 20; i++) method.Invoke(null, invokeArgs);
for (int batch = 0; batch < 5; batch++) {
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    long bytes = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew();
    object? result = null;
    for (int i = 0; i < 5; i++) result = method.Invoke(null, invokeArgs);
    watch.Stop(); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
    if (result is not long actual || actual != 1400000) throw new Exception("字段累加结果改变");
    Console.WriteLine($"compiled_field_reads batch={batch} ns_per_iteration={watch.Elapsed.TotalNanoseconds / 1000000:F3} bytes_per_iteration={bytes / 1000000.0:F6} total_bytes={bytes} calls=5 iterations=1000000 result={result}");
}
