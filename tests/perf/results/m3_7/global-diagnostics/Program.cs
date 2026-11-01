// 独立诊断器：不向生产代码添加反射；每进程一次初始化，先只读再存读，避免污染scale=7。
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
string path = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string candidate = Path.Combine(Path.GetDirectoryName(path)!, name.Name + ".dll");
    return File.Exists(candidate) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate) : null;
};
var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
asm.EntryPoint!.Invoke(null, asm.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var methods = asm.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
const long count = 1_000_000;
foreach (string name in new[] { "global_sum", "global_store_read" })
{
    var method = methods.SingleOrDefault(item => item.Name == "m0_" + name);
    if (method is null) continue;
    object?[] parameters = [count];
    long expected = name == "global_sum" ? 7 * count : count * (count - 1) / 2;
    for (int warm = 0; warm < 3; warm++)
        if (Convert.ToInt64(method.Invoke(null, parameters)) != expected) throw new Exception("warmup result mismatch");
    for (int repeat = 0; repeat < 7; repeat++)
    {
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        object? result = method.Invoke(null, parameters);
        double seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (Convert.ToInt64(result) != expected) throw new Exception("result mismatch");
        Console.WriteLine($"{name} {seconds:R} {result} allocated={allocated}");
    }
}
