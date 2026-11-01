// 独立稳态吞吐诊断：20次20万轮热身后测7次200万轮，不读分配且不开JIT日志。
using System.Diagnostics;
using System.Reflection;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
assembly.EntryPoint!.Invoke(null, assembly.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var method = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).Single(m => m.Name == "m0_field_reads");
var value = method.DeclaringType!.GetField("m0___mv3_first", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
object?[] invokeArgs = [200000L, value];
for (int i = 0; i < 20; i++) {
    if (method.Invoke(null, invokeArgs) is not long warm || warm != 1400000) throw new Exception("预热结果改变");
}
invokeArgs[0] = 2000000L;
for (int sample = 0; sample < 7; sample++) {
    var watch = Stopwatch.StartNew();
    object? result = method.Invoke(null, invokeArgs);
    watch.Stop();
    if (result is not long actual || actual != 14000000) throw new Exception("稳态结果改变");
    Console.WriteLine($"steady {sample} {watch.Elapsed.TotalSeconds:R} {result}");
}
