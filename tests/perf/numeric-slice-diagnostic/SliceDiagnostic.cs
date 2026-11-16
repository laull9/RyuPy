// 仅固定基线诊断：观察范围和生命周期分配，不是产品AOT收益。
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace RyuPy.Runtime.Tests;

public class SliceDiagnostic
{
    private static object? Field(object value, string name) => value.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value);
    private static (string Storage, int Observed) State(PyList list)
    {
        if (Field(list, "_numeric") is not IList slots) return ("object", list.Count);
        int observed = 0;
        for (int i = 0; i < slots.Count; i++)
            if (Field(slots[i]!, "_reference") is double) observed++;
        return ("numeric", observed);
    }
    private static (long Bytes, long Ticks) Measure(Action action)
    {
        long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
        action();
        return (GC.GetAllocatedBytesForCurrentThread() - bytes, Stopwatch.GetTimestamp() - start);
    }
    [Fact]
    public void CaptureSliceAndLifecycle()
    {
        string output = Environment.GetEnvironmentVariable("SLICE_HOST_OUTPUT")
            ?? throw new InvalidOperationException("需要证据目录");
        var rows = new List<object>();
        foreach (int size in new[] { 512, 5000, 50000 })
        foreach (string mode in new[] { "fresh", "partial", "materialized", "copied" })
        foreach (string shape in new[] { "empty", "one", "eight", "reverse-eight", "full" })
        for (int repeat = -3; repeat < 5; repeat++)
        {
            var source = (PyList)PyNumericListOps.Create();
            for (int i = 0; i < size; i++) PyNumericListOps.AppendF64(source, i + 0.25);
            object? observed = mode == "partial" ? source.GetItem(2) : null;
            List<object?>? alias = mode == "materialized" ? source.Items : null;
            PyList? previousCopy = mode == "copied" ? source.Copy() : null;
            int[] positions = shape switch
            {
                "empty" => [], "one" => [2], "eight" => Enumerable.Range(2, 8).ToArray(),
                "reverse-eight" => Enumerable.Range(2, 8).Reverse().ToArray(),
                _ => Enumerable.Range(0, size).ToArray()
            };
            PySlice slice = shape switch
            {
                "empty" => new(2L, 2L, 1L), "one" => new(2L, 3L, 1L),
                "eight" => new(2L, 10L, 1L), "reverse-eight" => new(9L, 1L, -1L),
                _ => new(0L, (long)size, 1L)
            };
            var before = State(source);
            PyList? result = null;
            var slicing = Measure(() => result = source.GetSlice(slice));
            var after = State(source);
            Assert.Equal(positions.Length, result!.Count);
            Assert.Equal(mode == "fresh" ? 0 : mode == "partial" ? 1 : size, before.Observed);
            if (positions.Length != 0) { Assert.Equal("object", after.Storage); Assert.Equal(size, after.Observed); }
            else Assert.Equal(before, after);
            for (int i = 0; i < positions.Length; i++)
            {
                Assert.Equal(positions[i] + 0.25, result.GetItem(i));
                Assert.Same(source.GetItem(positions[i]), result.GetItem(i));
                if (observed is not null && positions[i] == 2) Assert.Same(observed, result.GetItem(i));
                if (previousCopy is not null) Assert.Same(previousCopy.GetItem(positions[i]), result.GetItem(i));
            }
            // 后续阶段有固定顺序；计时外oracle及反射仍会影响后续GC，单列不归因。
            double total = 0;
            var iteration = Measure(() => { var it = PyIter.Create(source); while (it.MoveNext()) total += (double)it.Current()!; });
            Assert.Equal((double)size * (size - 1) / 2.0 + size * 0.25, total);
            PyList? laterCopy = null;
            var copying = Measure(() => laterCopy = source.Copy());
            List<object?>? published = null;
            var publishing = Measure(() => published = source.Items);
            if (alias is not null) Assert.Same(alias, published);
            Assert.Same(source.GetItem(2), laterCopy!.GetItem(2));
            if (positions.Length != 0)
            {
                int position = positions[0];
                object? old = result.GetItem(0);
                source.SetItem((long)position, 999.5);
                Assert.Same(old, result.GetItem(0));
                result.SetItem(0L, -999.5);
                Assert.Equal(999.5, source.GetItem(position));
                Assert.Same(old, laterCopy.GetItem(position));
            }
            if (repeat >= 0) rows.Add(new { size, mode, shape, repeat,
                storageBefore = before.Storage, observedBefore = before.Observed,
                storageAfter = after.Storage, observedAfter = after.Observed,
                sliceBytes = slicing.Bytes, sliceTicks = slicing.Ticks,
                iterateBytes = iteration.Bytes, iterateTicks = iteration.Ticks,
                copyBytes = copying.Bytes, copyTicks = copying.Ticks,
                publishBytes = publishing.Bytes, publishTicks = publishing.Ticks });
        }
        Assert.Equal(300, rows.Count);
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "host.json"), JsonSerializer.Serialize(new { frequency = Stopwatch.Frequency, rows }));
    }
}
