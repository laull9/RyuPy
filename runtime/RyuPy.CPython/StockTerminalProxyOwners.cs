// 终态内部proxy的owned按token弱登记：真实CLR保存者保活，临时值自然退账，原生dealloc确定退休。
// token可关联多个内部root以保留共享别名；清单没有CLR owner强回边，也不人工清Root表。
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private sealed class TerminalProxyOwner(PythonObject owner, StockThinFunctionOwner? holder)
    {
        internal readonly WeakReference<PythonObject> Owner = new(owner);
        internal readonly WeakReference<StockThinFunctionOwner>? Holder = holder is null ? null : new(holder);
    }
    private readonly Dictionary<nint, Dictionary<long, TerminalProxyOwner>> terminalProxyOwners = [];
    private int terminalProxyPeakRecords;
    private bool terminalPoolDraining;
    private readonly Dictionary<long, TerminalProxyOwner> terminalOwnerPool = [];
    private readonly Dictionary<long, HashSet<nint>> terminalOwnerRoots = [];

    private ProgramOwnedScope? BeginTerminalProxyCallback(nint identity)
    {
        if (state != 5 || Environment.CurrentManagedThreadId != ownerThread) return null;
        lock (gate) if (!stockProgramProxyRoots.Contains(identity)) return null;
        ProgramOwnedScope scope = BeginStockProgramCallback()!;
        scope.ProxyIdentity = identity;
        return scope;
    }
    private void RegisterTerminalProxyOwner(nint identity, PythonObject owner, StockThinFunctionOwner? holder)
    {
        lock (gate)
        {
            if (!terminalProxyOwners.TryGetValue(identity, out var owners)) terminalProxyOwners.Add(identity, owners = []);
            if (holder is null && terminalOwnerRoots.TryGetValue(owner.OwnershipToken, out var existingRoots))
                foreach (nint root in existingRoots)
                    if (terminalProxyOwners.TryGetValue(root, out var existing) && existing.TryGetValue(owner.OwnershipToken, out var record)
                        && record.Holder is { } weak && weak.TryGetTarget(out var found)) { holder = found; break; }
            if (holder is null && terminalOwnerPool.TryGetValue(owner.OwnershipToken, out var previous)
                && previous.Holder is { } prior && prior.TryGetTarget(out var saved)) holder = saved;
            owners[owner.OwnershipToken] = new(owner, holder);
            terminalOwnerPool[owner.OwnershipToken] = new(owner, holder);
            if (!terminalOwnerRoots.TryGetValue(owner.OwnershipToken, out var roots)) terminalOwnerRoots.Add(owner.OwnershipToken, roots = []);
            roots.Add(identity);
            terminalProxyPeakRecords = Math.Max(terminalProxyPeakRecords, terminalOwnerRoots.Count);
        }
    }
    // Release已持gate；只由真实token退账删除弱项。
    private void ReleaseTerminalOwnerToken(long token)
    {
        terminalOwnerPool.Remove(token);
        if (!terminalOwnerRoots.Remove(token, out var roots)) return;
        foreach (nint root in roots)
            if (terminalProxyOwners.TryGetValue(root, out var owners))
            { owners.Remove(token); if (owners.Count == 0) terminalProxyOwners.Remove(root); }
    }
    private void ObserveTerminalProxyValue(object? value)
    {
        if (state != 5 || value is null) return;
        ProgramOwnedScope? rootScope = stockProgramCallback;
        while (rootScope is not null && rootScope.ProxyIdentity == 0) rootScope = rootScope.Previous;
        if (rootScope is null) return;
        PythonObject? owner = StockProgramValueOwner(value);
        if (owner is null || owner.IsDisposed) return;
        StockThinFunctionOwner? holder = null;
        bool registered;
        lock (gate) registered = terminalOwnerRoots.ContainsKey(owner.OwnershipToken);
        // 只有原终态内部owner或本调用链明确拥有的owner可转移，外部Pause创建者没有这些凭据。
        if (!registered)
        {
            ProgramOwnedScope? scope = stockProgramCallback;
            while (scope is not null)
            {
                int index = scope.Owners.FindIndex(candidate => ReferenceEquals(candidate, owner));
                if (index >= 0)
                {
                    scope.Owners.RemoveAt(index);
                    holder = scope.Handles.Find(candidate => ReferenceEquals(candidate.Function, owner));
                    if (holder is not null) scope.Handles.Remove(holder);
                    registered = true; break;
                }
                scope = scope.Previous;
            }
        }
        if (registered) RegisterTerminalProxyOwner(rootScope.ProxyIdentity, owner, holder);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DetachTerminalProxyScope(ProgramOwnedScope scope)
    {
        foreach (PythonObject owner in scope.Owners)
        {
            StockThinFunctionOwner? holder = scope.Handles.Find(candidate => ReferenceEquals(candidate.Function, owner));
            RegisterTerminalProxyOwner(scope.ProxyIdentity, owner, holder);
        }
        scope.Owners.Clear(); scope.Handles.Clear(); scope.Detach();
    }
    private void CompleteTerminalProxyCallback(ProgramOwnedScope scope)
    {
        DetachTerminalProxyScope(scope);
        // helper已返回，清单只有弱项；GC finalizer只执行纯托管Release，GIL内随后真实Drain。
        nint raised = Native.rp_take_error();
        try { GC.Collect(); GC.WaitForPendingFinalizers(); Drain(); TryReleaseTerminalOwnerPool(); }
        finally { if (raised != 0) { Native.rp_restore_error(raised); Native.rp_decref(raised); } }
    }
    private void ReleaseTerminalProxyOwners(nint identity)
    {
        lock (gate)
        {
            if (terminalProxyOwners.Remove(identity, out var records))
                foreach (long token in records.Keys)
                    if (terminalOwnerRoots.TryGetValue(token, out var roots)) roots.Remove(identity);
        }
        // root间可以经CLR共享容器传递别名，不一定再次FromOwned；单一root死亡不撤销live wrapper。
        TryReleaseTerminalOwnerPool();
    }
    private void TryReleaseTerminalOwnerPool()
    {
        if (state != 5 || terminalPoolDraining || stockProgramCallback is not null
            || Roots.Count != 0 || Volatile.Read(ref stockThinRoots) != 0) return;
        lock (gate) if (terminalOwnerPool.Count == 0) return;
        // finalizer可能已置disposed、尚未取得gate退token；不持gate等待纯托管Release后再冻结快照。
        terminalPoolDraining = true;
        nint raised = Native.rp_take_error();
        try
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); Drain();
            if (Roots.Count != 0 || Volatile.Read(ref stockThinRoots) != 0) return;
            PythonObject[] owners;
            StockThinFunctionOwner[] holders;
            lock (gate)
            {
                owners = terminalOwnerPool.Values.Select(record => record.Owner.TryGetTarget(out var owner) && !owner.IsDisposed ? owner : null)
                    .OfType<PythonObject>().ToArray();
                holders = terminalOwnerPool.Values.Select(record => record.Holder is { } weak && weak.TryGetTarget(out var holder) ? holder : null)
                    .OfType<StockThinFunctionOwner>().ToArray();
            }
            // 所有CP→CLR proxy和薄函数入口均已真实析构，且没有活动owned scope，才撤销终态余下权利。
            ProgramOwnedScope scope = BeginStockProgramCallback()!;
            scope.Owners.AddRange(owners); scope.Handles.AddRange(holders); scope.Dispose();
        }
        finally
        {
            terminalPoolDraining = false;
            if (raised != 0) { Native.rp_restore_error(raised); Native.rp_decref(raised); }
        }
    }
    private PythonObject? StockProgramValueOwner(object value) => value switch
    {
        PythonObject direct => direct,
        StockThinFunctionHandle handle => handle.ForeignFunction,
        PyBaseException { ForeignObject: PythonObject error } => error,
        PyModule { ForeignObject: PythonObject module } => module,
        PySuper { ForeignObject: PythonObject super } => super,
        PyInstance instance => PyForeignClassBridge.GetForeignInstance(instance) as PythonObject,
        PyClass type => PyForeignClassBridge.GetForeignClass(type) as PythonObject,
        _ => reverse.TryGetValue(value, out var reference) ? reference.Owner : null,
    };
}
