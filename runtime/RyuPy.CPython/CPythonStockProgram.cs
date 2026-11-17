// 编译入口专用的一次性程序生命期：内部owned按创建事务登记，外部根精确拒绝。
// 终态允许初始化线程同步回调；所有引用仍走真实Release/Drain，最终保留ledger验零。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private bool stockProgramAuthorized, stockProgramExecuting;
    private ProgramOwnedScope? stockProgramEntry, stockProgramCallback;
    private PythonObject[] stockProgramOwners = [];
    private HashSet<long> stockProgramTokens = [];
    private StockProgramResult? stockProgramResult;

    internal sealed record StockProgramResult(int FinalizeResult, long NativeLive, long Destroyed, long NativeCalls,
        int CoreRoots, int OwnedHandles, int Ledger, int Pending, long Late, long PostFinalizeDecrefs);
    internal bool HasStockProgramPermission => stockProgramAuthorized;
    internal StockProgramResult? ProgramResult => stockProgramResult;

    internal void RunStockProgramMain()
    {
        if (!stockThinEnabled || stockProgramAuthorized || stockProgramExecuting)
            throw new InvalidOperationException("stock编译入口缺少能力或已经执行");
        if (Environment.CurrentManagedThreadId != ownerThread || leaseDepth != 0)
            throw new InvalidOperationException("stock编译入口必须在初始化线程无租约执行");
        stockProgramExecuting = true;
        var scope = new ProgramOwnedScope(this);
        stockProgramEntry = scope;
        try { PyModules.RunMain(); }
        finally
        {
            using Lease lease = Enter();
            // 只交还入口内部H；真实F继续由原D持有，随后外部getter获取独立的新H。
            scope.ReleaseHandles(); Drain();
            stockProgramOwners = scope.Owners.Where(owner => !owner.IsDisposed).ToArray();
            stockProgramTokens = stockProgramOwners.Select(owner => scope.Tokens[owner]).ToHashSet();
            stockProgramEntry = null; stockProgramExecuting = false; stockProgramAuthorized = true;
        }
    }

    internal int FinishStockProgram(Func<int> finishRuntime)
    {
        if (Environment.CurrentManagedThreadId != ownerThread || leaseDepth != 0)
            throw new InvalidOperationException("调用中或错误线程禁止结束stock编译程序");
        lock (gate)
        {
            if (!stockProgramAuthorized || state != 1 || active != 0 || stockProgramExecuting)
                throw new InvalidOperationException("stock程序未授权、正在调用或已经关闭");
            // 5在预检开始就挡住所有外线程；和普通Dispose的等待态2分开。
            state = 5;
        }
        try
        {
            using Lease lease = Enter();
            if (StockThinOwnedHandles != 0 || Roots.Count != 0
                || !handles.Keys.ToHashSet().SetEquals(stockProgramTokens))
                throw new InvalidOperationException("stock程序仍有外部owned引用，释放后重试结束");
            if (Native.rp_thin_program_check() < 0) ThrowError();
        }
        catch { lock (gate) state = 1; throw; }

        // 提交后先执行Runtime退出钩子；module/D包装此时尚可用，异常报告保留原入口退出码。
        int code = 0, nativeResult = -1;
        Exception? error = null;
        try
        {
            using (Lease lease = Enter())
            using (var scope = BeginStockProgramCallback())
                code = finishRuntime();
        }
        catch (Exception failure) { error = failure; }
        try
        {
            using (Lease lease = Enter())
            {
                if (executionAuditEnabled) Native.rp_execution_audit_phase(2);
                try { CaptureModulePaths(); } catch (Exception failure) { error ??= failure; }
                ReleaseClassBridgeResources();
                if (Native.rp_thin_program_prepare() < 0) ThrowError();
                foreach (PythonObject owner in stockProgramOwners) owner.RetireScoped();
                Drain(); liveHandlesAtShutdown = handles.Count;
            }
            nativeResult = Native.rp_stop();
        }
        finally
        {
            // 此处仅读取纯C计数与托管计数；解释器终止后绝不补做DECREF。
            Native.rp_thin_stats(out long live, out long destroyed, out long nativeCalls);
            Native.rp_reference_stats(out long post);
            lock (gate)
            {
                state = 3;
                stockProgramResult = new(nativeResult, live, destroyed, nativeCalls, stockThinRoots,
                    StockThinOwnedHandles, handles.Count, releases.Count, late, post);
            }
            RemoveInspectBridge(); imported.Clear(); objects.Clear();
            transfers.Clear(); transferIndex.Clear(); Instance = null;
            tclTkEnvironment?.TryRestore();
        }
        WriteReport();
        StockProgramResult result = stockProgramResult!;
        if (result.NativeLive != 0 || result.CoreRoots != 0 || result.OwnedHandles != 0
            || result.Ledger != 0 || result.Pending != 0 || result.Late != 0 || result.PostFinalizeDecrefs != 0 || Roots.Count != 0)
            throw new InvalidOperationException("stock程序终态owned/native引用未归零：" + result);
        if (error is not null) throw new InvalidOperationException("stock程序退出处理失败", error);
        if (nativeResult != 0) throw new InvalidOperationException("CPython关闭时标准流刷新失败");
        return code;
    }

    // 构造时记录强owner与准确token，不从弱缓存、指针类型或总数量推测归属。
    internal void ObserveStockProgramOwner(PythonObject owner, long token)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) return;
        (stockProgramCallback ?? stockProgramEntry)?.Add(owner, token);
    }
    internal void ObserveStockProgramHandle(StockThinFunctionOwner owner)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) return;
        (stockProgramCallback ?? stockProgramEntry)?.Handles.Add(owner);
    }
    private ProgramOwnedScope? BeginStockProgramCallback()
    {
        if ((state != 5 && !stockProgramExecuting) || Environment.CurrentManagedThreadId != ownerThread) return null;
        var scope = new ProgramOwnedScope(this, stockProgramCallback);
        stockProgramCallback = scope;
        return scope;
    }

    // 第三方managed业务没有入口/终态内部登记权限；准入静态body另建独立callback能力。
    private ProgramRegistrationPause PauseStockProgramRegistration() => new(this);
    private readonly struct ProgramRegistrationPause : IDisposable
    {
        private readonly CPythonHost host;
        private readonly ProgramOwnedScope? entry, callback;
        internal ProgramRegistrationPause(CPythonHost host)
        {
            this.host = host; entry = host.stockProgramEntry; callback = host.stockProgramCallback;
            host.stockProgramEntry = null; host.stockProgramCallback = null;
        }
        public void Dispose() { host.stockProgramEntry = entry; host.stockProgramCallback = callback; }
    }

    private sealed class ProgramOwnedScope(CPythonHost host, ProgramOwnedScope? previous = null) : IDisposable
    {
        internal readonly List<PythonObject> Owners = [];
        internal readonly Dictionary<PythonObject, long> Tokens = [];
        internal readonly List<StockThinFunctionOwner> Handles = [];
        internal void Add(PythonObject owner, long token) { Owners.Add(owner); Tokens.Add(owner, token); }
        internal void ReleaseHandles()
        { foreach (var handle in Handles) { handle.Function.RetireScoped(); handle.Dispose(); } Handles.Clear(); }
        public void Dispose()
        {
            // 原异常和返回值已由调用边界独立保活；DECREF中的析构重入使用嵌套scope。
            nint raised = Native.rp_take_error();
            try
            {
                while (Handles.Count != 0 || Owners.Count != 0)
                {
                    ReleaseHandles();
                    while (Owners.Count != 0)
                    {
                        int last = Owners.Count - 1;
                        PythonObject owner = Owners[last]; Owners.RemoveAt(last); owner.RetireScoped();
                    }
                    host.Drain();
                }
            }
            finally
            {
                host.stockProgramCallback = previous;
                if (raised != 0) { Native.rp_restore_error(raised); Native.rp_decref(raised); }
            }
        }
    }
}
