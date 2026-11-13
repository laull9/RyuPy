// 单主解释器状态机、租约/GIL与引用释放队列；finalizer仅登记释放，绝不调用Python。
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

/// <summary>可观察生命周期计数，各项用于定位跨堆持有与迟到释放。</summary>
public sealed record HostStatistics(string State, long BridgeCalls, int ActiveLeases, int Handles, int ManagedRoots, int PendingReleases, long LateReleases);

/// <summary>固定CPython3.12.13、单次初始化的随包宿主。</summary>
public sealed partial class CPythonHost : IDisposable, IPyModuleProvider
{
    private static int processStarted;
    internal static CPythonHost? Instance;
    private readonly object gate = new();
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly HashSet<string> modules;
    private readonly Dictionary<nint, WeakReference<object>> objects = new();
    private readonly Dictionary<string, PyModule> imported = new(StringComparer.Ordinal);
    internal readonly ConcurrentDictionary<nint, object> Roots = new();
    private readonly ConditionalWeakTable<object, ManagedReference> reverse = new();
    private readonly ConcurrentQueue<nint> releases = new();
    private readonly Dictionary<long, nint> handles = new();
    private long nextHandle;
    private int state;
    private int active;
    private long calls, late, nextRoot;
    [ThreadStatic] private static int leaseDepth;
    // 原生库保留到进程退出，扩展仍可能持有函数地址。
    private nint pythonLibrary, shimLibrary;
    private string pythonPath = "", shimPath = "";
    [DllImport("libSystem.B.dylib", EntryPoint = "dlopen")] private static extern nint MacLoad([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);
    [DllImport("libdl.so.2", EntryPoint = "dlopen")] private static extern nint LinuxLoad([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    private CPythonHost(HostConfiguration configuration)
    {
        modules = new(configuration.Modules, StringComparer.Ordinal); synchronizeContainers = configuration.SynchronizeContainers;
    }

    internal static unsafe CPythonHost Start(string path, string[] argv)
    {
        HostConfiguration configuration = HostConfiguration.Read(path);
        if (Interlocked.Exchange(ref processStarted, 1) != 0) throw new InvalidOperationException("每进程只允许一次CPython初始化");
        var host = new CPythonHost(configuration); Instance = host;
        try
        {
            host.pythonPath = configuration.Library; host.shimPath = configuration.Shim;
            // C扩展从进程全局符号表解析Py*；固定绝对路径加载，绝不探测系统Python。
            host.pythonLibrary = OperatingSystem.IsMacOS() ? MacLoad(configuration.Library, 0xA)
                : OperatingSystem.IsLinux() ? LinuxLoad(configuration.Library, 0x102) : NativeLibrary.Load(configuration.Library);
            if (host.pythonLibrary == 0) throw new DllNotFoundException("无法加载固定libpython：" + configuration.Library);
            host.shimLibrary = NativeLibrary.Load(configuration.Shim);
            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (name, _, _) => name == Native.Library ? host.shimLibrary : 0);
            nint[] paths = configuration.SearchPaths.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
            nint[] args = argv.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
            try
            {
                fixed (nint* pathPointer = paths, argPointer = args)
                    if (Native.rp_start(configuration.Home, pathPointer, paths.Length, argPointer, args.Length, &Dispatch) != 0)
                        throw new InvalidOperationException("CPython初始化失败：" + Marshal.PtrToStringUTF8(Native.rp_init_error()));
            }
            finally { foreach (nint item in paths) Marshal.FreeCoTaskMem(item); foreach (nint item in args) Marshal.FreeCoTaskMem(item); }
            host.state = 1;
            host.InstallImports();
            return host;
        }
        catch (Exception error)
        {
            if (host.state == 1) { try { host.Dispose(); } catch { } }
            host.state = 3; Instance = null;
            throw new InvalidOperationException($"CPython启动失败 [stage=load/init config={path} library={configuration.Library} shim={configuration.Shim}]：{error.Message}", error);
        }
    }

    /// <summary>即时计数，不触发GC或隐式回收。</summary>
    public HostStatistics Statistics
    {
        get { lock (gate) return new(state == 1 ? "running" : state == 2 ? "stopping" : "closed", calls, active, handles.Count, Roots.Count, releases.Count, late); }
    }

    internal Lease Enter()
    {
        lock (gate)
        {
            if (state != 1 && !(state == 2 && Environment.CurrentManagedThreadId == ownerThread))
                throw new ObjectDisposedException("CPython", "解释器关闭中或已关闭");
            active++; calls++;
        }
        // 取得GIL时不持有托管锁，Python可以重入同一宿主。
        int gil = 0; bool acquired = false;
        try { gil = Native.rp_gil(); acquired = true; Drain(); leaseDepth++; return new Lease(this, gil); }
        catch
        {
            if (acquired) Native.rp_gil_release(gil);
            lock (gate) { active--; Monitor.PulseAll(gate); }
            throw;
        }
    }

    internal readonly struct Lease(CPythonHost host, int gil) : IDisposable
    {
        public void Dispose()
        {
            host.Drain(); leaseDepth--; Native.rp_gil_release(gil);
            lock (host.gate) { host.active--; Monitor.PulseAll(host.gate); }
        }
    }

    internal long Track(nint pointer) { lock (gate) { long token = ++nextHandle; handles.Add(token, pointer); return token; } }
    internal void Release(long token)
    {
        lock (gate)
        {
            if (!handles.Remove(token, out nint pointer)) { if (state == 3) late++; return; }
            if (state == 3) { late++; return; }
            releases.Enqueue(pointer);
        }
    }
    private void Drain() { while (releases.TryDequeue(out nint pointer)) Native.rp_decref(pointer); }

    /// <summary>在合法租约内排空释放队列；可用来观察稳态。</summary>
    public void DrainReferences() { using Lease lease = Enter(); }

    /// <summary>显式注销反向代理，应用应先令Python端删除其引用以断开跨堆环。</summary>
    public void Unregister(object value)
    {
        using Lease lease = Enter();
        if (reverse.TryGetValue(value, out ManagedReference? reference))
        {
            Roots.TryRemove(reference.Identity, out _); reverse.Remove(value);
            if (transfers.TryGetValue(value, out ContainerTransfer? transfer)) { transfers.Remove(value); transfer.Owner.Dispose(); }
        }
    }

    /// <summary>停止外部租约，等待已进入的调用，所属线程终止解释器一次。</summary>
    public void Dispose()
    {
        if (leaseDepth != 0) throw new InvalidOperationException("持有CPython租约时禁止关闭宿主");
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("CPython必须在初始化线程关闭");
        lock (gate)
        {
            if (state == 3) return;
            if (state != 1) throw new InvalidOperationException("CPython关闭重入");
            state = 2;
            // Wait释放gate；现存外部调用和回调可以完成并通知。
            while (active != 0) Monitor.Wait(gate);
        }
        using (Lease lease = Enter())
        {
            CaptureModulePaths();
            imported.Clear(); objects.Clear(); Drain();
            lock (gate) liveHandlesAtShutdown = handles.Count;
            // 活代理可能被atexit回调再次使用，保留其owned引用到Finalize结束。
            // 此处只排空已不可达代理的释放队列；显式作用域应在关闭前注销跨堆环。
        }
        int result = Native.rp_stop();
        // Finalize期间的C代理析构/atexit仍可访问Roots；完成后才清除托管根。
        Roots.Clear(); transfers.Clear(); transferIndex.Clear(); lock (gate) { state = 3; handles.Clear(); } Instance = null;
        WriteReport();
        if (result != 0) throw new InvalidOperationException("CPython关闭时标准流刷新失败");
    }

    /// <inheritdoc />
    public bool Owns(string name) => modules.Contains(name) || modules.Any(m => name.StartsWith(m + ".", StringComparison.Ordinal));
    /// <summary>确定释放一个opaque对象；调用者应保证后续不再使用其任何别名。</summary>
    public void ReleaseObject(object value)
    {
        if (value is PythonObject foreign) foreign.Dispose();
        else if (value is PyBaseException { ForeignObject: PythonObject origin }) origin.Dispose();
        else if (reverse.TryGetValue(value, out ManagedReference? reference))
        {
            reference.Owner?.Dispose(); reverse.Remove(value);
            transfers.Remove(value);
        }
    }
    internal sealed record ManagedReference(nint Identity, nint Pointer, PythonObject? Owner = null);
}
