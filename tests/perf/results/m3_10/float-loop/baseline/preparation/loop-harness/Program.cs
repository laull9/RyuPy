// 真实编译循环的独立资源/JIT诊断；原main以--diagnostic建立完整输入但不计时，结果与回调计数用原check验证。
using System.Reflection;
using System.Runtime;
using System.Text.Json;
using RyuPy.Runtime;

var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
object? exit = assembly.EntryPoint!.Invoke(null, [new string[] { "--diagnostic" }]);
Builtins.Flush();
if (exit is int code && code != 0) throw new Exception("初始化失败");
var type = assembly.GetType("RyuPy.Generated.Module")!;
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
var run = type.GetMethod("m0_run", flags)!.CreateDelegate<Func<object?, object?>>();
var reset = type.GetMethod("m0_reset", flags)!.CreateDelegate<Func<object?>>();
var check = type.GetMethod("m0_check", flags)!.CreateDelegate<Func<object?, object?, object?>>();
var rows = new List<object>();
foreach (string label in new[] { "float", "empty", "bool", "reentry", "custom_first", "custom_middle", "custom_last", "shared_first", "shared_middle", "shared_last", "mixed_restore" })
{
    for (int warm = 0; warm < 3; warm++) { reset(); object? value = run(label); check(label, value); }
    for (int sample = 0; sample < 3; sample++)
    {
        reset();
        long before = GC.GetTotalAllocatedBytes(true), methods = JitInfo.GetCompiledMethodCount();
        var pause = GC.GetTotalPauseDuration(); var jit = JitInfo.GetCompilationTime();
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        object? value = run(label);
        var result = new { label, sample, alloc_bytes = GC.GetTotalAllocatedBytes(true) - before,
            gc0 = GC.CollectionCount(0) - gc0, gc1 = GC.CollectionCount(1) - gc1, gc2 = GC.CollectionCount(2) - gc2,
            gc_pause_ms = (GC.GetTotalPauseDuration() - pause).TotalMilliseconds,
            jit_ms = (JitInfo.GetCompilationTime() - jit).TotalMilliseconds,
            jit_methods = JitInfo.GetCompiledMethodCount() - methods };
        check(label, value); rows.Add(result);
    }
}
// 输出统一放在全部资源采样后，不让JSON方法的后台tier污染下一条资源窗口。
foreach (object row in rows) Console.WriteLine(JsonSerializer.Serialize(row));
