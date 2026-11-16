// 独立Runtime NativeAOT存活门，不是Python编译执行或进程PSS。
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

internal static class Program
{
    static bool Enabled;
    static void Increment(PyDict d, PyTuple key)
    {
        if (Enabled && PyRecordsIncrement.TryMaterialized(d, key)) return;
        object? old = PyOps.CallMethod(d, "get", [key, PyInt.Box(0L)], [], []);
        object value = PyOps.Binary(PyOps.Add, old!, PyInt.Box(1L));
        PyOps.SetItem(d, key, value);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static List<PyDict> Create(int n)
    {
        var roots = new List<PyDict>(n);
        for (int i = 0; i < n; i++)
        {
            var d = new PyDict(); var first = new PyTuple(["ann", "buy"]);
            Increment(d, first); Increment(d, new PyTuple(["ann", "buy"]));
            if (!ReferenceEquals(d.Keys.Single(), first) || !ReferenceEquals(d.Get(first), PyInt.Box(2L))) throw new Exception("identity");
            roots.Add(d);
        }
        return roots;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference[] ShortLived(int n)
    {
        var roots = Create(n);
        return roots.Select(d => new WeakReference(d)).ToArray();
    }
    static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    static int Alive(WeakReference[] weak) => weak.Count(w => w.IsAlive);
    static void Main(string[] args)
    {
        Enabled = args[0] == "on"; int n = int.Parse(args[1]); string phase = args[2];
        Collect(); long baseline = GC.GetTotalMemory(false);
        List<PyDict>? roots = phase == "released" ? null : Create(n);
        WeakReference[]? released = phase == "released" ? ShortLived(n) : null;
        if (phase == "cleared") foreach (var d in roots!) d.Clear();
        var batches = new List<WeakReference[]>();
        for (int b = 0; b < 4; b++)
        {
            var weak = ShortLived(n); batches.Add(weak);
            for (int attempt = 0; attempt < 12 && Alive(weak) > 0; attempt++) Collect();
            if (Alive(weak) != 0) throw new Exception("weak owner retained");
            Console.WriteLine($"BATCH {b} {GC.GetTotalMemory(false)} {Alive(weak)}");
        }
        Collect();
        if (released is not null && Alive(released) != 0) throw new Exception("released owner retained");
        long live = GC.GetTotalMemory(false); var info = GC.GetGCMemoryInfo();
        long checksum = 0;
        if (roots is not null) foreach (var d in roots)
        {
            if (phase == "held") checksum += (long)d.Get(new PyTuple(["ann", "buy"]))!;
            else if (d.Count != 0) throw new Exception("clear");
        }
        Console.WriteLine($"MEM {args[0]} {n} {phase} {baseline} {live} {info.HeapSizeBytes} {info.FragmentedBytes} {checksum} {batches.Sum(Alive)}");
        Console.WriteLine($"ENV {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        GC.KeepAlive(roots); GC.KeepAlive(batches); GC.KeepAlive(released);
    }
}
