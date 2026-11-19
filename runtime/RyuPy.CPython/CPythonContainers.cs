// 显式synchronized容器合同：同一对象图按跨域边界转交写权，保存真实CPython容器与托管别名。
using RyuPy.Runtime;
using System.Runtime.CompilerServices;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private readonly ConditionalWeakTable<object, ContainerTransfer> transfers = new();
    private readonly List<WeakReference<ContainerTransfer>> transferIndex = new();
    private bool synchronizing;
    private readonly bool synchronizeContainers;
    private int transferThread;
    private int transferDepth;
    [ThreadStatic] private static int callbackDepth;
    internal readonly struct CallbackScope : IDisposable
    {
        internal CallbackScope(bool _) { callbackDepth++; }
        public void Dispose() { callbackDepth--; }
    }
    private string? synchronizationFailure;
    private sealed class ContainerTransfer(object value, PythonObject owner)
    {
        internal readonly object Value = value;
        internal readonly PythonObject Owner = owner;
        internal object?[] ListSnapshot = [];
        internal (object? Key, object? Value)[] DictSnapshot = [];
    }

    private ContainerTransfer[] ActiveTransfers()
    {
        var result = new List<ContainerTransfer>();
        transferIndex.RemoveAll(weak => !weak.TryGetTarget(out var transfer) || transfer.Owner.IsDisposed);
        foreach (WeakReference<ContainerTransfer> weak in transferIndex)
            if (weak.TryGetTarget(out ContainerTransfer? transfer)) result.Add(transfer);
        return [.. result];
    }
    internal nint TransferContainer(object value)
    {
        ValidateGraph(value, new(ReferenceEqualityComparer.Instance));
        nint pointer = value is PyList ? Check(Native.rp_list()) : Check(Native.rp_op(16));
        var owner = new PythonObject(this, pointer);
        var transfer = new ContainerTransfer(value, owner);
        transfers.Add(value, transfer); transferIndex.Add(new(transfer)); reverse.Add(value, new ManagedReference(0, pointer, owner)); objects[pointer] = new(value);
        // 先登记身份后填充，嵌套对象和自引用会命中同一指针。
        try { PushContainer(transfer); RetainStockProgramContainer(value); }
        catch { transfers.Remove(value); reverse.Remove(value); objects.Remove(pointer); owner.Dispose(); throw; }
        return Native.rp_incref(pointer);
    }

    internal T PythonCall<T>(Func<T> operation)
    {
        try
        {
            using Lease lease = Enter();
            if (TryCallCarrier(operation, out T carried)) return carried;
            return ExecutePythonCall(operation);
        }
        // delegate强持协议receiver；CLR不能在C API消费borrowed指针或同步/异常清理期间提前终结它。
        // 外层finally覆盖全部using清理和嵌套重入，finalizer始终只负责纯托管Release入队。
        finally { GC.KeepAlive(operation); }
    }
    private T ExecutePythonCall<T>(Func<T> operation)
    {
        using var handled = new PythonHandledScope(this);
        EnterPythonCall();
        T result;
        try { result = operation(); }
        catch
        {
            try { LeavePythonCall(); }
            catch (Exception error) { synchronizationFailure = error.Message; }
            throw;
        }
        LeavePythonCall(); return result;
    }
    internal void EnterPythonCall()
    {
        int thread = Environment.CurrentManagedThreadId;
        if (transferDepth != 0 && transferThread != thread && callbackDepth == 0)
            throw new PyRuntimeError("synchronized containers cannot be shared by concurrent Python/managed calls");
        if (transferDepth == 0) transferThread = thread;
        transferDepth++;
        try { PushContainers(); }
        catch { transferDepth--; throw; }
    }
    internal void LeavePythonCall()
    {
        try { PullContainers(); }
        finally { if (--transferDepth == 0) transferThread = 0; }
    }
    private void PushContainers()
    {
        if (!synchronizeContainers || synchronizing) return;
        // 完整预检先于任何CP对象更新，防止域外键导致部分物化。
        foreach (ContainerTransfer transfer in ActiveTransfers()) ValidateGraph(transfer.Value, new(ReferenceEqualityComparer.Instance));
        synchronizing = true;
        try { foreach (ContainerTransfer transfer in ActiveTransfers()) PushContainer(transfer); }
        finally { synchronizing = false; }
    }
    private void PullContainers()
    {
        if (!synchronizeContainers || synchronizing) return;
        synchronizing = true;
        try { foreach (ContainerTransfer transfer in ActiveTransfers()) PullContainer(transfer); }
        finally { synchronizing = false; }
    }
    private void PushContainer(ContainerTransfer transfer)
    {
        if (transfer.Value is PyList list)
        {
            object?[] values = [.. list.Items];
            if (SameReferences(values, transfer.ListSnapshot)) return;
            nint source = Check(Native.rp_list());
            try
            {
                foreach (object? value in values)
                {
                    nint item = ToPython(value);
                    try { if (Native.rp_list_append(source, item) < 0) ThrowError(); }
                    finally { Native.rp_decref(item); }
                }
                if (Native.rp_list_replace(transfer.Owner.Pointer, source) < 0) ThrowError();
                transfer.ListSnapshot = values;
            }
            finally { Native.rp_decref(source); }
            return;
        }
        var dict = (PyDict)transfer.Value;
        (object? Key, object? Value)[] entries = [.. dict.Entries()];
        var previous = transfer.DictSnapshot;
        if (SameEntries(previous, entries))
        {
            RetainStockProgramContainer(transfer.Value);
            return;
        }
        foreach ((object? key, _) in entries) RequireTransferKey(key);
        var positions = IndexKeys(entries);
        bool orderChanged = !KeepsSurvivorOrder(previous, positions);
        foreach ((object? key, _) in previous)
        {
            if (!orderChanged && positions.ContainsKey(key!)) continue;
            nint nativeKey = ToPython(key);
            try { if (Native.rp_dict_delete(transfer.Owner.Pointer, nativeKey) < 0) ThrowError(); }
            finally { Native.rp_decref(nativeKey); }
        }
        var priorPositions = orderChanged ? null : IndexKeys(previous);
        foreach ((object? key, object? value) in entries)
        {
            if (priorPositions is not null && priorPositions.TryGetValue(key!, out int prior)
                && ReferenceEquals(previous[prior].Value, value)) continue;
            nint nativeKey = ToPython(key), nativeValue = 0;
            try
            {
                nativeValue = ToPython(value);
                Native.rp_decref(Check(Native.rp_op(7, transfer.Owner.Pointer, nativeKey, nativeValue)));
            }
            finally { Native.rp_decref(nativeKey); Native.rp_decref(nativeValue); }
        }
        transfer.DictSnapshot = entries;
        RetainStockProgramContainer(transfer.Value);
    }
    private void PullContainer(ContainerTransfer transfer)
    {
        if (transfer.Value is PyList list)
        {
            int length = checked((int)Native.rp_length(transfer.Owner.Pointer)); ThrowIfError();
            var values = new object?[length];
            for (int i = 0; i < length; i++)
            {
                nint key = Native.rp_int(i);
                try { values[i] = FromOwned(Native.rp_op(6, transfer.Owner.Pointer, key)); }
                finally { Native.rp_decref(key); }
            }
            if (!SameReferences(values, transfer.ListSnapshot)) { list.Items.Clear(); list.Items.AddRange(values); }
            transfer.ListSnapshot = values;
            RetainStockProgramContainer(transfer.Value);
            return;
        }
        var dict = (PyDict)transfer.Value;
        nint keys = Check(Native.rp_op(19, transfer.Owner.Pointer)); nint tuple = 0;
        (object? Key, object? Value)[] entries;
        try
        {
            tuple = Check(Native.rp_op(26, keys));
            int count = checked((int)Native.rp_length(tuple)); ThrowIfError();
            entries = new (object?, object?)[count];
            for (int i = 0; i < count; i++)
            {
                nint key = Check(Native.rp_op(18, tuple, n: i));
                try
                {
                    object? managedKey = FromBorrowed(key); RequireTransferKey(managedKey);
                    entries[i] = (managedKey, FromOwned(Native.rp_op(6, transfer.Owner.Pointer, key)));
                }
                finally { Native.rp_decref(key); }
            }
        }
        finally { Native.rp_decref(tuple); Native.rp_decref(keys); }
        (object? Key, object? Value)[] current = [.. dict.Entries()];
        if (!SameEntries(current, entries))
        {
            var positions = IndexKeys(entries);
            if (!KeepsSurvivorOrder(current, positions)) dict.Clear();
            else foreach ((object? key, _) in current) if (!positions.ContainsKey(key!)) dict.Delete(key);
            foreach ((object? key, object? value) in entries)
                if (!dict.TryGet(key, out object? prior) || !ReferenceEquals(prior, value)) dict.Set(key, value);
        }
        transfer.DictSnapshot = entries;
        RetainStockProgramContainer(transfer.Value);
    }

    // 未变字典走一次顺序身份比较；连覆盖值也算变化，不误用只跟踪结构的 PyDict.Version。
    private static bool SameEntries(ReadOnlySpan<(object? Key, object? Value)> before,
        ReadOnlySpan<(object? Key, object? Value)> after)
    {
        if (before.Length != after.Length) return false;
        for (int i = 0; i < before.Length; i++)
            if (!ReferenceEquals(before[i].Key, after[i].Key) || !ReferenceEquals(before[i].Value, after[i].Value))
                return false;
        return true;
    }

    // 只按真实引用建立临时索引，不调用 Python 哈希或相等协议。None 键是 PyNone，不是 CLR null。
    private static Dictionary<object, int> IndexKeys(ReadOnlySpan<(object? Key, object? Value)> entries)
    {
        var positions = new Dictionary<object, int>(entries.Length, ReferenceEqualityComparer.Instance);
        for (int i = 0; i < entries.Length; i++) positions.Add(entries[i].Key!, i);
        return positions;
    }

    // 先删旧键、再写新值和追加新键只能保留旧存活键的前缀顺序；否则沿用整表重排。
    private static bool KeepsSurvivorOrder(ReadOnlySpan<(object? Key, object? Value)> previous,
        Dictionary<object, int> positions)
    {
        int expected = 0;
        foreach ((object? key, _) in previous)
            if (positions.TryGetValue(key!, out int position) && position != expected++) return false;
        return true;
    }

    private static void ValidateGraph(object? value, HashSet<object> seen)
    {
        if (value is null || !seen.Add(value)) return;
        if (value is PyDict dict)
        {
            foreach ((object? key, object? item) in dict.Entries()) { RequireTransferKey(key); ValidateGraph(item, seen); }
        }
        else if (value is PyList list) foreach (object? item in list.Items) ValidateGraph(item, seen);
        else if (value is PyTuple tuple) foreach (object? item in tuple.Items) ValidateGraph(item, seen);
    }
    private static bool SameReferences(object?[] left, object?[] right)
    {
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (!ReferenceEquals(left[i], right[i])) return false;
        return true;
    }
    private static void RequireTransferKey(object? key)
    {
        if (key is null or PyNone or bool or int or long or double or System.Numerics.BigInteger or string or PyBytes) return;
        if (key is PyTuple tuple && key.GetType() == typeof(PyTuple)) { foreach (object? member in tuple.Items) RequireTransferKey(member); return; }
        if (key is PythonObject foreign)
        {
            if (Native.rp_scalar_kind(foreign.Pointer) != 0) return;
            if (Native.rp_exact_tuple(foreign.Pointer) != 0)
            {
                long count = Native.rp_length(foreign.Pointer); foreign.Host.ThrowIfError();
                for (long i = 0; i < count; i++) RequireTransferKey(foreign.Host.FromOwned(Native.rp_op(18, foreign.Pointer, n: i)));
                return;
            }
        }
        throw new PyNotImplementedError("synchronized container key protocol unsupported; exact immutable scalar keys or scalar tuples are required");
    }
}
