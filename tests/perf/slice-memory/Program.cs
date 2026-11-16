// 独立Runtime NativeAOT归因驱动，不是Python编译管线，也不是JIT宿主。
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RyuPy.Runtime;

int size = int.Parse(args[0]);
string scenario = args[1], phase = args[2];
var roots = Create(size, scenario);
double checksum = 0;
if (phase == "released") { checksum = Verify(roots, size, scenario); roots = null; }
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
GC.WaitForPendingFinalizers();
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
long liveBytes = GC.GetTotalMemory(false);
var info = GC.GetGCMemoryInfo();
long heap = info.HeapSizeBytes, fragmented = info.FragmentedBytes, committed = info.TotalCommittedBytes;
int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
Console.WriteLine("READY");
if (Console.ReadLine() != "ACK") throw new Exception("missing ACK");
if (roots is not null) checksum = Verify(roots, size, scenario);
Console.WriteLine($"METRIC {liveBytes} {heap} {fragmented} {committed} {gen0} {gen1} {gen2}");
Console.WriteLine($"ENV {System.Runtime.GCSettings.IsServerGC} {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"DONE {size} {scenario} {phase} {checksum:R}");
GC.KeepAlive(roots);

[MethodImpl(MethodImplOptions.NoInlining)]
static List<(PyList Source, List<PyList> Parts)> Create(int size, string scenario)
{
    var roots = new List<(PyList, List<PyList>)>();
    for (int group = 0; group < 4; group++)
    {
        var source = (PyList)PyNumericListOps.Create();
        for (int i = 0; i < size; i++) PyNumericListOps.AppendF64(source, i);
        var parts = new List<PyList>();
        if (scenario == "near") parts.Add(source.GetSlice(new PySlice(0L, (long)size - 1, 1L)));
        else if (scenario == "cumulative")
            for (int start = 0; start < size; start += 256)
                parts.Add(source.GetSlice(new PySlice((long)start, (long)start + 256, 1L)));
        else parts.Add(source.GetSlice(new PySlice(null, null, null)));
        roots.Add((source, parts));
    }
    return roots;
}
[MethodImpl(MethodImplOptions.NoInlining)]
static double Verify(List<(PyList Source, List<PyList> Parts)> roots, int size, string scenario)
{
    double total = 0;
    foreach (var (source, parts) in roots)
    {
        for (int i = 0; i < size; i++) total += (double)source.GetItem(i)!;
        int position = 0;
        foreach (var part in parts)
            for (int i = 0; i < part.Count; i++, position++)
            {
                object? value = part.GetItem(i);
                if (!ReferenceEquals(value, source.GetItem(position))) throw new Exception("identity mismatch");
                total += (double)value!;
            }
        if (position != (scenario == "near" ? size - 1 : size)) throw new Exception("length mismatch");
    }
    return total;
}
