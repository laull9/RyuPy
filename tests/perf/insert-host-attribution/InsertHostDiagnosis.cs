// 仅host归因诊断；raw Pair16是布局代理，不是PyNumericSlot或产品AOT。
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RyuPy.Runtime.Tests;

public class InsertHostDiagnosis
{
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Pair16(double number, object reference)
    {
        public readonly double Number = number;
        public readonly object Reference = reference;
    }
    private sealed record Row(int Size, string Operation, string Mode, int Sample, bool Warmup,
        int Order, int Insertions, long Ticks, long Allocated, int Gen0, int Gen1, int Gen2,
        string StorageBefore, string StorageAfter, int CapacityBefore, int CapacityAfter);

    [Fact]
    public void CaptureInsertionMatrix()
    {
        string output = Environment.GetEnvironmentVariable("INSERT_HOST_OUTPUT")
            ?? throw new InvalidOperationException("需要隔离证据目录");
        int round = int.Parse(Environment.GetEnvironmentVariable("INSERT_HOST_ROUND")!);
        string arm = Environment.GetEnvironmentVariable("INSERT_HOST_ARM")!;
        Assert.Contains(arm, new[] { "A", "B" });
        Assert.InRange(round, 0, 3);
        Assert.Equal(16, Unsafe.SizeOf<Pair16>());
        Assert.Equal(8, sizeof(double));
        string[] modes = ["py-fresh", "py-materialized", "raw-double", "raw-pair16"];
        var rows = new List<Row>();
        int sizeOrdinal = 0;
        foreach (int size in new[] { 512, 5000, 50000 })
        {
            int operationOrdinal = 0;
            foreach (string operation in new[] { "once", "many" })
            {
                int insertions = operation == "once" ? 1 : 2000;
                // 每个cell预热3次、记录7次；每样本轮换表示的顺序，两臂相同。
                for (int sample = -3; sample < 7; sample++)
                for (int order = 0; order < modes.Length; order++)
                {
                    int offset = (round + sizeOrdinal + operationOrdinal + sample + 3) % modes.Length;
                    string mode = modes[(order + offset) % modes.Length];
                    rows.Add(Measure(size, operation, mode, sample, order, insertions));
                    Save(); // 保留逐样本原始数据，即使后续失败。
                }
                operationOrdinal++;
            }
            sizeOrdinal++;
        }
        Assert.Equal(240, rows.Count);
        Assert.Equal(168, rows.Count(row => !row.Warmup));
        File.WriteAllText(Path.Combine(output, "COMPLETE"), "host only; no product acceptance\n");

        void Save()
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "samples.json"), JsonSerializer.Serialize(new
            {
                arm, round, process = Environment.ProcessId, frequency = Stopwatch.Frequency,
                runtime = RuntimeInformation.FrameworkDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                serverGc = System.Runtime.GCSettings.IsServerGC,
                latencyMode = System.Runtime.GCSettings.LatencyMode.ToString(),
                pairSize = Unsafe.SizeOf<Pair16>(), doubleSize = sizeof(double),
                tiered = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
                tieredPgo = Environment.GetEnvironmentVariable("DOTNET_TieredPGO"), rows
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Row Measure(int size, string operation, string mode, int sample, int order, int insertions)
    {
        // 构造、委托创建和oracle均在操作计时外；同一cell所有插入复用同一原盒。
        object shared = 7.5;
        object freshMarker = new();
        PyList? py = null;
        List<double>? rawDouble = null;
        List<Pair16>? rawPair = null;
        Action insert;
        if (mode.StartsWith("py-"))
        {
            py = (PyList)PyNumericListOps.Create();
            for (int i = 0; i < size; i++) PyNumericListOps.AppendF64(py, i + 0.25);
            if (mode == "py-materialized") _ = py.Items;
            insert = () => { for (int i = 0; i < insertions; i++) py.Insert(0, shared); };
        }
        else if (mode == "raw-double")
        {
            rawDouble = new List<double>();
            for (int i = 0; i < size; i++) rawDouble.Add(i + 0.25);
            double number = (double)shared; // 与pair构造一致，在计时外解包。
            insert = () => { for (int i = 0; i < insertions; i++) rawDouble.Insert(0, number); };
        }
        else
        {
            rawPair = new List<Pair16>();
            for (int i = 0; i < size; i++) rawPair.Add(new Pair16(i + 0.25, freshMarker));
            var pair = new Pair16((double)shared, shared);
            insert = () => { for (int i = 0; i < insertions; i++) rawPair.Insert(0, pair); };
        }
        var beforeStorage = Storage();
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        insert();
        long ticks = Stopwatch.GetTimestamp() - start;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
        int gen0 = GC.CollectionCount(0) - gc0, gen1 = GC.CollectionCount(1) - gc1, gen2 = GC.CollectionCount(2) - gc2;
        var afterStorage = Storage();
        int count = py?.Count ?? rawDouble?.Count ?? rawPair!.Count;
        Assert.Equal(size + insertions, count);
        for (int i = 0; i < count; i++)
        {
            double expected = i < insertions ? (double)shared : i - insertions + 0.25;
            if (py is not null)
            {
                object? value = py.GetItem(i);
                Assert.Equal(expected, Assert.IsType<double>(value));
                if (i < insertions) Assert.Same(shared, value);
            }
            else if (rawDouble is not null) Assert.Equal(expected, rawDouble[i]);
            else
            {
                Assert.Equal(expected, rawPair![i].Number);
                Assert.Same(i < insertions ? shared : freshMarker, rawPair[i].Reference);
            }
        }
        return new Row(size, operation, mode, sample, sample < 0, order, insertions,
            ticks, allocated, gen0, gen1, gen2, beforeStorage.Name, afterStorage.Name,
            beforeStorage.Capacity, afterStorage.Capacity);

        (string Name, int Capacity) Storage()
        {
            if (rawDouble is not null) return ("raw-double", rawDouble.Capacity);
            if (rawPair is not null) return ("raw-pair16", rawPair.Capacity);
            foreach (string field in new[] { "_numeric", "_items" })
            {
                object? value = typeof(PyList).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(py);
                if (value is not null) return (field, (int)value.GetType().GetProperty("Capacity")!.GetValue(value)!);
            }
            throw new InvalidOperationException("未知背板");
        }
    }
}
