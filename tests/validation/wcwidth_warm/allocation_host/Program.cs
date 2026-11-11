// 同一Host在隔离进程装载旧/新Runtime。区间只测真实Invoke暖命中，非完整Python kernel。
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using RyuPy.Runtime;

internal static class Program
{
    private const int Warm = 20000, Count = 100000;
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
    private static long Number(object info, string name) =>
        checked((long)PyInt.ToBigInteger(PyObject.GetAttr(info, name)!));

    private static object Domain(bool typed)
    {
        object stable = new();
        int calls = 0;
        var function = new PyFunction("stable", PySignature.Create("stable", [], [], []),
            (_, _, _, _) => { calls++; return stable; }, [], usesSuper: false);
        // UpdateWrapper读真实函数metadata；不伪造frame或源码位置。
        PyObject.SetAttr(function, "__qualname__", "stable");
        object? decorator = PyOps.CallValue(PyFunctools.CreateAttributes()["lru_cache"], [128L, typed], [], []);
        var cache = (PyLruCache)PyOps.CallValue(decorator, [function], [], [])!;
        object?[] args = ["a", "auto"];
        string[] names = [];
        object?[] values = [];
        Require(ReferenceEquals(cache.Invoke(args, names, values), stable), "fill identity");
        object? last = null;
        for (int i = 0; i < Warm; i++) last = cache.Invoke(args, names, values);
        Require(ReferenceEquals(last, stable), "warm identity");
        var samples = new object[3];
        for (int batch = 0; batch < 3; batch++)
        {
            int thread = Environment.CurrentManagedThreadId;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Count; i++) last = cache.Invoke(args, names, values);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            // 所有验证、cache_info与JSON都在区间外；首批原样保留。
            Require(thread == Environment.CurrentManagedThreadId, "thread changed");
            Require(ReferenceEquals(last, stable) && calls == 1, "hit identity/callback");
            object info = PyOps.CallValue(cache.GetAttr("cache_info"), [], [], [])!;
            long hits = Number(info, "hits"), misses = Number(info, "misses");
            long size = Number(info, "currsize"), maximum = Number(info, "maxsize");
            Require(hits == Warm + (batch + 1L) * Count && misses == 1 && size == 1 && maximum == 128,
                "cache state");
            Require(allocated >= 0, "allocation counter");
            samples[batch] = new { batch, allocated, count = Count, hits, misses, size, maximum,
                calls, identity = true, thread };
        }
        return new { typed, samples };
    }
    private static void Main()
    {
        // 只读程序集身份，不反射调用产品，不用动态装载或动态发射。
        var runtime = typeof(PyLruCache).Assembly;
        var host = typeof(Program).Assembly;
        string runtimePath = Path.GetFullPath(runtime.Location), hostPath = Path.GetFullPath(host.Location);
        string runtimeHash = Hash(runtimePath), hostHash = Hash(hostPath);
        object[] domains = [Domain(false), Domain(true)];
        Require(Hash(runtimePath) == runtimeHash && Hash(hostPath) == hostHash, "loaded files changed");
        Console.WriteLine(JsonSerializer.Serialize(new { schema = 1, pid = Environment.ProcessId,
            runtime_path = runtimePath, runtime_sha256 = runtimeHash, runtime_version = runtime.GetName().Version!.ToString(),
            host_path = hostPath, host_sha256 = hostHash, warm = Warm, count = Count, domains }));
    }
}
