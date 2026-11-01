// 独立反射分配诊断：运行真实编译函数，输入/实参数组复用，逃逸列表保容量且计数前清理。
using RyuPy.Runtime;
using System.Reflection;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
assembly.EntryPoint!.Invoke(null, assembly.EntryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var methods = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).ToArray();
var left = new PyList([1L]); var right = new PyList([2L]); var saved = new PyList();
const long n = 20001;
foreach (string label in new[] { "swap", "read", "escape" }) {
    var method = methods.Single(m => m.Name == "m0_tuple_" + label);
    object?[] input = label == "escape" ? [n, left, right, saved] : [n, left, right];
    void Check(object? result) {
        if (label == "read") {
            if (!ReferenceEquals(result, left)) throw new Exception("读取身份改变");
        } else {
            if (result is not PyTuple tuple || tuple.Length != 2) throw new Exception("返回tuple改变");
            if (!ReferenceEquals(tuple.Items[0], label == "swap" ? right : left) ||
                !ReferenceEquals(tuple.Items[1], label == "swap" ? left : right)) throw new Exception("元素身份改变");
            if (label == "escape" && (saved.Count != n || !ReferenceEquals(saved.Items[^1], tuple) ||
                ReferenceEquals(saved.Items[0], tuple))) throw new Exception("逃逸身份改变");
        }
    }
    for (int i = 0; i < 20; i++) { saved.Items.Clear(); Check(method.Invoke(null, input)); }
    for (int sample = 0; sample < 5; sample++) {
        saved.Items.Clear(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        object? result = method.Invoke(null, input);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(result);
        Console.WriteLine($"allocation {label} {sample} {bytes} {n} {bytes / (double)n:R}");
    }
}
