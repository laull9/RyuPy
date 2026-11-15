// 仅隔离诊断：测量现有Pop的增量分配，不把宿主暖进程结果当源码AOT性能。
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace RyuPy.Runtime.Tests;

public class PyNumericPopDiagnosticTests
{
    [Fact]
    public void CaptureBaselineAllocationAndRepresentation()
    {
        string output = Environment.GetEnvironmentVariable("NUMERIC_POP_EVIDENCE")
            ?? throw new InvalidOperationException("诊断必须提供证据目录");
        var rows = new List<object>();
        foreach (int count in new[] { 16, 1024, 65536 })
        foreach (string mode in new[] { "fresh", "observed", "object" })
        for (int repeat = -3; repeat < 5; repeat++)
        {
            PyList list = mode == "object" ? new PyList() : (PyList)PyNumericListOps.Create();
            for (int i = 0; i < count; i++)
            {
                if (mode == "object") list.Append((double)i);
                else PyNumericListOps.AppendF64(list, i);
            }
            object? observed = mode != "fresh" ? list.GetItem(-1) : null;
            bool beforeNumeric = typeof(PyList).GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list) is not null;
            Assert.Equal(mode != "object", beforeNumeric);
            long before = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            object? value = list.Pop();
            long ticks = Stopwatch.GetTimestamp() - start;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal((double)(count - 1), Assert.IsType<double>(value));
            Assert.Equal(count - 1, list.Count);
            if (mode != "fresh") Assert.Same(observed, value);
            bool numeric = typeof(PyList).GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list) is not null;
            Assert.False(numeric); // 精确当前基线；未来候选需独立合同，不能改本基线证明。
            if (repeat >= 0) rows.Add(new { count, mode, repeat, allocated, ticks, numeric });
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "host-pop.json"), JsonSerializer.Serialize(
            new { frequency = Stopwatch.Frequency, rows }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal(45, rows.Count);
    }
}
