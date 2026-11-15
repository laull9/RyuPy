// 隔离的观察边界诊断：操作后的整体物化不得被当作有益优化。
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace RyuPy.Runtime.Tests;

public class PyNumericObservationDiagnosticTests
{
    [Fact]
    public void CaptureBaselineAllocationAndRepresentation()
    {
        string output = Environment.GetEnvironmentVariable("NUMERIC_OBSERVATION_EVIDENCE")
            ?? throw new InvalidOperationException("诊断必须提供证据目录");
        var rows = new List<object>();
        foreach (int count in new[] { 16, 1024, 65536 })
        foreach (string mode in new[] { "fresh", "observed", "object" })
        foreach (string operation in new[] { "insert", "delete", "index-first", "index-last", "index-miss", "copy" })
        for (int repeat = -3; repeat < 5; repeat++)
        {
            PyList list = mode == "object" ? new PyList() : (PyList)PyNumericListOps.Create();
            for (int i = 0; i < count; i++)
                if (mode == "object") list.Append((double)i);
                else PyNumericListOps.AppendF64(list, i);
            object? observed = mode != "fresh" ? list.GetItem(0) : null;
            Assert.Equal(mode != "object", Numeric(list));
            object inserted = 7.5;
            object target = operation == "index-last" ? (double)(count - 1) : operation == "index-miss" ? -2.0 : 0.0;
            PyList? copy = null;
            long result = -99;
            long before = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            switch (operation)
            {
                case "insert": list.Insert(0, inserted); break;
                case "delete": list.DelItem(-1L); break;
                case "copy": copy = list.Copy(); break;
                default: result = list.IndexOf(target); break;
            }
            long ticks = Stopwatch.GetTimestamp() - start;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            bool numeric = Numeric(list);
            Assert.False(numeric); // 固定当前基线，不预判候选表示。
            Assert.Equal(count + (operation == "insert" ? 1 : operation == "delete" ? -1 : 0), list.Count);
            if (operation == "insert") Assert.Same(inserted, list.GetItem(0));
            if (operation.StartsWith("index")) Assert.Equal(operation == "index-miss" ? -1 : operation == "index-last" ? count - 1 : 0, result);
            if (mode != "fresh") Assert.Same(observed, list.GetItem(operation == "insert" ? 1 : 0));
            if (copy is not null) { Assert.Equal(count, copy.Count); Assert.Same(list.GetItem(0), copy.GetItem(0)); }
            if (repeat >= 0) rows.Add(new { count, mode, operation, repeat, allocated, ticks, numeric });
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "host-observation.json"), JsonSerializer.Serialize(
            new { frequency = Stopwatch.Frequency, rows }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal(270, rows.Count);
    }
    private static bool Numeric(PyList list) => typeof(PyList)
        .GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list) is not null;
}
