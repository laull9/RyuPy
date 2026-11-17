// 真实双堆所有权验证：core 无 CP 强引用，handle owned F，终结器只入队，主线程持 GIL drain。
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

sealed class Core
{
    // 缓存只弱持公开 handle；core 绝不捕获 owner、globals 或 CP 函数。
    internal WeakReference<Handle>? Cache;
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static long Leaf(long x, long offset) => checked(x + offset);
}

sealed class Handle
{
    internal readonly nint Function;
    internal Handle(nint f) { Function = f; Native.ch_inc(f); Program.Handles++; }
    ~Handle() => Program.Pending.Enqueue(Function);
    internal long Call(long x)
    {
        Program.Require(Calls.ch_call(Function, x, out long value) == 0, "CLR handle 调用");
        GC.KeepAlive(this);
        return value;
    }
}

static unsafe class Program
{
    internal static readonly ConcurrentQueue<nint> Pending = new();
    internal static int Roots, Handles;
    private static long invocations;
    private static readonly List<WeakReference<Core>> Cores = new();

    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    internal static void Run(string source) => Require(Native.ch_run(source) == 0, source);
    private static nint Eval(string expression)
    {
        nint value = Native.ch_eval(expression);
        Require(value != 0, expression);
        return value;
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int Invoke(nint token, long x, long offset, long* result)
    {
        try
        {
            Require(GCHandle.FromIntPtr(token).Target is Core, "原生强根 core 存活");
            *result = Core.Leaf(x, offset);
            invocations++;
            return 0;
        }
        catch { return -1; } // 托管异常不能穿越原生 ABI。
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Release(nint token)
    {
        GCHandle.FromIntPtr(token).Free();
        Roots--;
    }
    private static nint NewRoot()
    {
        var core = new Core();
        Cores.Add(new(core));
        Roots++;
        return GCHandle.ToIntPtr(GCHandle.Alloc(core));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Handle Acquire(nint f)
    {
        nint token = Native.ch_token(f);
        Require(token != 0, "未知捕获或默认值必须拒绝");
        var core = (Core)GCHandle.FromIntPtr(token).Target!;
        if (core.Cache is not null && core.Cache.TryGetTarget(out Handle? cached)) return cached;
        var handle = new Handle(f);
        core.Cache = new(handle);
        return handle;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Handle Create(int offset)
    {
        Run("begin_creation_trace()");
        Run($"prepare({offset})");
        nint d = Eval("d");
        nint f = Native.ch_make(d, NewRoot());
        Require(f != 0, "创建薄函数");
        Require(Native.ch_set(d, f) == 0, "F -> D -> F 环");
        Handle handle = Acquire(f);
        Native.ch_dec(f);
        Native.ch_dec(d);
        Run("observe(); publish(); del d, m; end_creation_trace()");
        return handle;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Handle> DropToPython()
    {
        Handle handle = Create(10);
        Require(handle.Call(2) == 12, "CIL 叶子结果");
        return new(handle);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Handle> Reflow()
    {
        nint f = Eval("sys.modules['proof'].f");
        Run("sys.modules['proof'].f.__defaults__ = (1,)");
        Require(Native.ch_token(f) == 0, "拒绝新增默认值");
        Run("sys.modules['proof'].f.__defaults__ = None");
        Run("saved_code = sys.modules['proof'].f.__code__; " +
            "exec('def other(p):\\n def g(x): return p(x)\\n return g'); " +
            "sys.modules['proof'].f.__code__ = other(None).__code__");
        Require(Native.ch_token(f) == 0, "拒绝替换 glue code");
        Run("sys.modules['proof'].f.__code__ = saved_code; del saved_code, other");
        Handle handle = Acquire(f);
        Require(ReferenceEquals(handle, Acquire(f)), "同代 handle 身份");
        Require(handle.Call(3) == 13, "CP 回流重建 handle");
        Run("check_conversion_reentry()");
        Native.ch_dec(f);
        return new(handle);
    }
    private static void ClrCollect()
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    }
    private static void Drain()
    {
        // 只有此主线程持 GIL，可安全运行真实 DECREF 与 native 析构回调。
        while (Pending.TryDequeue(out nint f)) { Native.ch_dec(f); Handles--; }
    }
    private static void Cycle() { ClrCollect(); Drain(); Run("gc.collect()"); }
    private static void Baseline(string label, long expectedDestroyed)
    {
        ClrCollect(); Drain();
        Require(Native.ch_live() > 0, "CP 强环仍待循环 GC，不能靠引用计数提前消失");
        Run("assert any(f() is not None for f,s in watches)");
        for (int i = 0; i < 8; i++) Cycle();
        Run("assert all(f() is None and s() is None for f,s in watches)");
        Require(Roots == 0 && Handles == 0 && Pending.IsEmpty, "CLR 根/handle/队列回基线");
        Require(Native.ch_live() == 0 && Native.ch_destroyed() == expectedDestroyed, "真实 capsule 析构计数");
        Require(Cores.All(w => !w.TryGetTarget(out _)), "core 弱引用已死");
        Console.WriteLine($"PASS {label} roots={Roots} handles={Handles} native_live={Native.ch_live()} dealloc={Native.ch_destroyed()} weak_dead={Cores.Count}");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClrOnlyAndReload()
    {
        Handle old = Create(20);
        Run("old_dict_id = id(sys.modules['proof'].__dict__); del sys.modules['proof']; gc.collect()");
        Cycle();
        Require(old.Call(2) == 22, "仅 CLR 保存 H 时旧 globals 可读");
        Handle fresh = Create(100);
        Run("assert id(sys.modules['proof'].__dict__) != old_dict_id");
        Require(old.Call(2) == 22 && fresh.Call(2) == 102, "重导后新旧字典身份及值独立");
        Run("sys.modules['proof'].offset = 200");
        Require(old.Call(2) == 22 && fresh.Call(2) == 202, "live globals 单一权威");
        Run("del sys.modules['proof']");
        GC.KeepAlive(old); GC.KeepAlive(fresh);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailedImport()
    {
        Handle handle = Create(40);
        Run("fail_import()");
        // 外逃 H 在失败清理后仍持旧环境，随后两端全部释放。
        Require(handle.Call(2) == 42, "失败事务外逃函数仍可调用");
    }
    private static void RejectUnknown()
    {
        Run("def unknown(x=1): return x\n");
        nint f = Eval("unknown");
        Require(Native.ch_token(f) == 0, "拒绝未知函数");
        Native.ch_dec(f);
        Run("del unknown");
        Run("exec('def outer(y):\\n def captured(x): return x+y\\n return captured'); unknown = outer(1)");
        f = Eval("unknown");
        Require(Native.ch_token(f) == 0, "拒绝未知业务捕获");
        Native.ch_dec(f);
        Run("del unknown, outer");
    }
    public static int Main(string[] args)
    {
        Require(RuntimeFeature.IsDynamicCodeSupported == (args[2] == "jit"), "真实 JIT/NativeAOT 运行模式");
        Require(Native.ch_init(args[0], &Invoke, &Release) == 0, "stock 初始化");
        Run(File.ReadAllText(args[1]));
        RejectUnknown();
        for (int i = 0; i < 12; i++)
        {
            var weak = DropToPython();
            // 先观察终结器入队且 owned F 尚未 drain，再验证 CP 独持可调用。
            ClrCollect();
            Require(!weak.TryGetTarget(out _) && !Pending.IsEmpty, "公开 handle 已收集并排队");
            Drain(); Run("gc.collect(); assert sys.modules['proof'].f(5) == 15");
            Require(Roots == 1 && Handles == 0, "CP 单独保存 F 可保持 core");
            weak = Reflow();
            Cycle();
            Require(!weak.TryGetTarget(out _), "回流 handle 再次收集");
            Run("del sys.modules['proof']");
            Baseline($"cp_only_reflow_{i}", i * 4 + 1);
            ClrOnlyAndReload();
            Baseline($"clr_only_reload_{i}", i * 4 + 3);
            FailedImport();
            Baseline($"failed_import_{i}", i * 4 + 4);
        }
        Require(invocations >= 12 * 8, "真实 CIL 回调次数");
        Console.WriteLine($"RESULT cycles=12 functions=48 cil_calls={invocations} roots=0 handles=0 native_live=0");
        Require(Native.ch_finish() == 0, "stock finalize");
        return 0;
    }
}
