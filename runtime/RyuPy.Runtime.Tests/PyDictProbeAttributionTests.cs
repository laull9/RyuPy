// 独立host归因：每个Fact可在新testhost进程运行，阶段顺序与tiering由CI显式控制。
// 反射只在计时外读取原索引；不为测试复制字典实现，不把阶段差值直接解释为产品热点占比。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictProbeAttributionTests(ITestOutputHelper output)
{
    private static object? _allocationSink;
    private sealed record Sample(double Ms, long Bytes, int G0, int G1, int G2, double PauseMs, long Sum);

    [Fact] public void SmallTable() => Diagnose(32, false);
    [Fact] public void LargeTable() => Diagnose(4096, false);
    [Fact] public void CollisionTable() => Diagnose(32, true);

    private void Diagnose(int size, bool collision)
    {
        const int iterations = 120000;
        string[] names = Enumerable.Range(0, size).Select(i => collision ? new string('\0', i + 1) : "user" + i).ToArray();
        string[] stages = ["guard", "allocate", "hash-tuple", "hash-pair", "index-tuple", "index-pair-cached", "index-pair", "get", "probe-get", "aggregate-tuple", "aggregate-probe", "aggregate-fused"];
        bool reverse = Environment.GetEnvironmentVariable("RYUPY_RECORDS_DIAG_REVERSE") == "1";
        if (reverse) Array.Reverse(stages);
        output.WriteLine($"attribution-config keys={size} collision={collision} reverse={reverse} tiering={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"} framework={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} cpu={Environment.ProcessorCount} serverGC={System.Runtime.GCSettings.IsServerGC}");
        foreach (string stage in stages) Measure(stage, names, iterations);
        for (int round = 0; round < 6; round++)
        {
            foreach (string stage in round % 2 == 0 ? stages : stages.Reverse())
            {
                Sample s = Measure(stage, names, iterations);
                output.WriteLine($"attribution keys={size} collision={collision} round={round} stage={stage} ms={s.Ms:F3} bytes={s.Bytes} gen0={s.G0} gen1={s.G1} gen2={s.G2} pauseMs={s.PauseMs:F3} sum={s.Sum}");
            }
        }
    }

    private static Sample Measure(string stage, string[] names, int count)
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict); object zero = 0L;
        foreach (string name in names) Assert.True(access.TrySet(name, "buy", zero));
        // 查询tuple和表内tuple不是同一实体，防止原路错误获得身份快判优势。
        PyTuple[] queries = names.Select(name => new PyTuple([name, "buy"])).ToArray();
        var index = (Dictionary<object, int>)typeof(PyDict).GetField("_index", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dict)!;
        var alternate = index.GetAlternateLookup<PyDictStringPair>();
        var comparer = (IAlternateEqualityComparer<PyDictStringPair, object>)PyKeyComparer.Instance;
        long sum = 0; object? last = null;
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        TimeSpan pause = GC.GetTotalPauseDuration(); long bytes = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        // stage选择不在循环内；各阶段原始计时分别报告，不做相减归因。
        switch (stage)
        {
            case "guard":
                for (int i = 0; i < count; i++) if (access.CheckDomain()) sum++;
                break;
            case "allocate":
                for (int i = 0; i < count; i++) System.Threading.Volatile.Write(ref _allocationSink, new PyTuple([names[i % names.Length], "buy"]));
                break;
            case "hash-tuple":
                for (int i = 0; i < count; i++) sum += PyKeyComparer.Instance.GetHashCode(queries[i % names.Length]);
                break;
            case "hash-pair":
                for (int i = 0; i < count; i++) sum += comparer.GetHashCode(new(names[i % names.Length], "buy"));
                break;
            case "index-tuple":
                for (int i = 0; i < count; i++) { index.TryGetValue(queries[i % names.Length], out int slot); sum += slot; }
                break;
            case "index-pair-cached":
                for (int i = 0; i < count; i++) { alternate.TryGetValue(new(names[i % names.Length], "buy"), out int slot); sum += slot; }
                break;
            case "index-pair":
                for (int i = 0; i < count; i++) { index.GetAlternateLookup<PyDictStringPair>().TryGetValue(new(names[i % names.Length], "buy"), out int slot); sum += slot; }
                break;
            case "get":
                for (int i = 0; i < count; i++) { dict.TryGet(queries[i % names.Length], out last); sum += (long)last!; }
                break;
            case "probe-get":
                for (int i = 0; i < count; i++) { if (!access.TryGet(names[i % names.Length], "buy", out bool found, out last) || !found) throw new InvalidOperationException(); sum += (long)last!; }
                break;
            case "aggregate-tuple":
                for (int i = 0; i < count; i++) { var key = new PyTuple([names[i % names.Length], "buy"]); dict.Set(key, (long)dict.GetOrDefault(key, zero)! + 1L); }
                break;
            case "aggregate-probe":
                for (int i = 0; i < count; i++) { string name = names[i % names.Length]; if (!access.TryGet(name, "buy", out _, out last) || !access.TrySet(name, "buy", (long)last! + 1L)) throw new InvalidOperationException(); }
                break;
            case "aggregate-fused":
                for (int i = 0; i < count; i++) if (!access.TryIncrementDiscardedKey(names[i % names.Length], "buy")) throw new InvalidOperationException();
                break;
            default: throw new InvalidOperationException(stage);
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        var result = new Sample(elapsed, allocated, GC.CollectionCount(0) - g0, GC.CollectionCount(1) - g1,
            GC.CollectionCount(2) - g2, (GC.GetTotalPauseDuration() - pause).TotalMilliseconds, sum);
        Assert.Equal(names.Length, dict.Count);
        long total = 0; int position = 0;
        foreach (var (key, value) in dict.Entries())
        {
            Assert.Equal(names[position], ((PyTuple)key!).Items[0]);
            long expected = stage.StartsWith("aggregate-", StringComparison.Ordinal)
                ? count / names.Length + (position < count % names.Length ? 1L : 0L) : 0L;
            Assert.Equal(expected, value); total += (long)value!; position++;
        }
        Assert.Equal(stage.StartsWith("aggregate-", StringComparison.Ordinal) ? (long)count : 0L, total);
        GC.KeepAlive(last); GC.KeepAlive(_allocationSink);
        return result;
    }

    [Fact]
    public void FusedIncrementPreservesStoredKeyAndRejectsOverflowOrNonLong()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        Assert.True(access.TryIncrementDiscardedKey("ann", "buy")); object? key = dict.Keys.Single();
        Assert.True(access.TryIncrementDiscardedKey("ann", "buy")); Assert.Same(key, dict.Keys.Single()); Assert.Equal(2L, dict.Get(key));
        dict.Set(key, long.MaxValue); long version = dict.Version;
        Assert.False(access.TryIncrementDiscardedKey("ann", "buy")); Assert.Equal(long.MaxValue, dict.Get(key)); Assert.Equal(version, dict.Version);
        dict.Set(key, true); Assert.False(access.TryIncrementDiscardedKey("ann", "buy")); Assert.Equal(true, dict.Get(key));
        dict.Set("external", 0L); Assert.False(access.TryIncrementDiscardedKey("ann", "buy")); Assert.Equal(2, dict.Count);
    }
}
