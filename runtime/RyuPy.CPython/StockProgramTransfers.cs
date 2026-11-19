// stock入口中的同步容器可把callback结果和opaque项保存在CLR图中；按准确owner转移登记。
// 运行期转交入口弱表，终态嵌套调用转交父scope；外部暂停登记的owned不能取得内部权限。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private readonly HashSet<nint> stockProgramProxyRoots = [];
    private void ObserveStockProgramProxy(nint identity)
    {
        if (Environment.CurrentManagedThreadId == ownerThread && (stockProgramEntry is not null || stockProgramCallback is not null))
            lock (gate) stockProgramProxyRoots.Add(identity);
    }
    private void ReleaseStockProgramProxy(nint identity)
    { lock (gate) stockProgramProxyRoots.Remove(identity); }

    private void RetainStockProgramContainer(object value)
    {
        if (stockProgramCallback is null) return;
        var pending = new Stack<object>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Push(value);
        while (pending.TryPop(out object? item))
        {
            if (!visited.Add(item)) continue;
            PythonObject? owner = item switch
            {
                PythonObject direct => direct,
                StockThinFunctionHandle handle => handle.ForeignFunction,
                PyBaseException { ForeignObject: PythonObject error } => error,
                PyModule { ForeignObject: PythonObject module } => module,
                PySuper { ForeignObject: PythonObject super } => super,
                PyInstance instance => PyForeignClassBridge.GetForeignInstance(instance) as PythonObject,
                PyClass type => PyForeignClassBridge.GetForeignClass(type) as PythonObject,
                _ => reverse.TryGetValue(item, out var reference) ? reference.Owner : null,
            };
            if (owner is not null) TransferStockProgramOwner(owner);
            switch (item)
            {
                case PyList list:
                    foreach (object? child in list.Items) if (child is not null) pending.Push(child);
                    break;
                case PyTuple tuple:
                    foreach (object? child in tuple.Items) if (child is not null) pending.Push(child);
                    break;
                case PyDict dictionary:
                    foreach ((object? key, object? child) in dictionary.Entries())
                    { if (key is not null) pending.Push(key); if (child is not null) pending.Push(child); }
                    break;
                case PyBaseException error:
                    foreach (object? child in error.Args) if (child is not null) pending.Push(child);
                    if (error.Cause is { } cause) pending.Push(cause);
                    if (error.Context is { } context) pending.Push(context);
                    break;
            }
        }
    }

    private void TransferStockProgramOwner(PythonObject owner)
    {
        ProgramOwnedScope? scope = stockProgramCallback;
        if (scope is null || (!stockProgramExecuting && scope.Previous is null)) return;
        // 只转移当前scope真正持有的owner；既有外部wrapper与Pause期间新建wrapper均保持外部归属。
        int index = scope.Owners.FindIndex(candidate => ReferenceEquals(candidate, owner));
        if (index < 0) return;
        scope.Owners.RemoveAt(index);
        StockThinFunctionOwner? holder = scope.Handles.Find(candidate => ReferenceEquals(candidate.Function, owner));
        if (holder is not null) scope.Handles.Remove(holder);
        if (stockProgramExecuting)
        {
            stockProgramEntryRegistry!.Add(owner, owner.OwnershipToken);
            if (holder is not null) stockProgramEntryRegistry.AttachHandle(holder);
        }
        else
        {
            scope.Previous!.Add(owner);
            if (holder is not null) scope.Previous.Handles.Add(holder);
        }
    }
}
