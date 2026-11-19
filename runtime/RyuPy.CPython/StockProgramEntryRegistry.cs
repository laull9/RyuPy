// 长入口的token弱登记：仅观察真正owned的存活对象，Release精确退账；短callback仍用强scope。
// 表内没有owner/H强回边。全部字典操作由Host.gate保护，枚举弱项时不调用Python API。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private int nextStockIdentityPrune = 1024;

    private sealed class ProgramEntryRegistry(CPythonHost host)
    {
        private sealed class EntryRecord(PythonObject owner)
        {
            internal readonly WeakReference<PythonObject> Owner = new(owner);
            internal WeakReference<StockThinFunctionOwner>? Holder;
        }
        private readonly Dictionary<long, EntryRecord> entries = [];
        internal int PeakCount { get; private set; }
        internal int Count { get { lock (host.gate) return entries.Count; } }

        internal void Add(PythonObject owner, long token)
        {
            lock (host.gate)
            {
                entries.Add(token, new(owner));
                PeakCount = Math.Max(PeakCount, entries.Count);
            }
        }
        internal void AttachHandle(StockThinFunctionOwner holder)
        {
            lock (host.gate)
                if (entries.TryGetValue(holder.Function.OwnershipToken, out var entry))
                    entry.Holder = new(holder);
        }
        // 仅由Release的gate临界区调用；弱项消失本身不能作为移除凭据。
        internal void RemoveReleasedToken(long token) => entries.Remove(token);

        internal StockThinFunctionOwner[] SnapshotHandles()
        {
            lock (host.gate)
            {
                var result = new List<StockThinFunctionOwner>();
                foreach (var entry in entries.Values)
                    if (entry.Holder is { } weak && weak.TryGetTarget(out var holder)) result.Add(holder);
                return [.. result];
            }
        }
        // 下列方法由调用者持gate；强快照冻结仍有合法owned权利的wrapper。
        internal PythonObject[] SnapshotOwners() => entries.Values.Select(entry =>
            entry.Owner.TryGetTarget(out var owner) && !owner.IsDisposed ? owner : null)
            .OfType<PythonObject>().ToArray();
        internal HashSet<long> TokenSnapshot() => entries.Keys.ToHashSet();
        internal bool HasUnresolvedOwner() => entries.Values.Any(entry =>
            !entry.Owner.TryGetTarget(out var owner) || owner.IsDisposed);
    }

    // 只在本profile的GIL冷边界按几何阈值扫描；不在finalizer线程操作普通身份Dictionary。
    private void PruneStockProgramIdentities()
    {
        if (!stockProgramExecuting || objects.Count < nextStockIdentityPrune) return;
        foreach (var pair in objects.ToArray())
        {
            bool dead = !pair.Value.TryGetTarget(out var value)
                || value is PythonObject { IsDisposed: true }
                || value is PyBaseException { ForeignObject: PythonObject { IsDisposed: true } }
                || value is PySuper { ForeignObject: PythonObject { IsDisposed: true } }
                || value is PyModule { ForeignObject: PythonObject { IsDisposed: true } }
                || value is not null && reverse.TryGetValue(value, out var reference) && reference.Owner is { IsDisposed: true };
            if (dead) objects.Remove(pair.Key);
        }
        nextStockIdentityPrune = Math.Max(1024, objects.Count * 2);
    }
}
