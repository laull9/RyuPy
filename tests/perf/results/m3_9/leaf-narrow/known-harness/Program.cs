// 已知整数成功/重绑失败同调用点的分配；反射只在函数外配置模块绑定，测量体不建输入。
using System.Reflection;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
assembly.EntryPoint!.Invoke(null, assembly.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var type = assembly.GetType("RyuPy.Generated.Module")!;
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
var getter = type.GetMethod("__mod_get_0", flags)!;
var alias = type.GetField("m0___mv3_alias", flags)!;
object?[] input = [20000L];
foreach (var (label, name, target, expected) in new[] {
    ("index_fast", "repeat_index", "twice", 39998L), ("index_rebind", "repeat_index", "other", 20000L),
    ("constant_fast", "repeat_constant", "twice", 14L), ("constant_rebind", "repeat_constant", "other", 8L) }) {
    alias.SetValue(null, getter.Invoke(null, [target]));
    var method = type.GetMethod("m0_" + name, flags)!;
    void Check(object? result) { if (result is not long x || x != expected) throw new Exception("已知整数回退结果改变"); }
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
