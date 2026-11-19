// 托管与Native共享真实执行后的对象/事务断言，辅助手工反例不冒充Python纵切。
using RyuPy.Runtime;

internal static class ExecutionChecks
{
    internal static void Require(bool condition, string why)
    {
        if (!condition) throw new InvalidOperationException(why);
    }

    internal static void Check(TraceAdapterScope scope, string name, int code)
    {
        Require(code == 0, "真实生成模块执行失败");
        Require(PyTraceback.CaptureDepth() == 0, "正常返回未恢复真实帧栈");
        var exported = PyModules.Import("__main__").GetAttr("result");
        Require(ReferenceEquals(exported, scope.LastHandled), "host捕获不是同源程序导出的真实result");
        var last = scope.LastHandled ?? throw new InvalidOperationException("没有真实Python接收");
        for (PyBaseException? error = last; error is not null; error = error.Context)
        {
            var binding = error.TraceAdapterBinding ?? throw new InvalidOperationException("缺少真实source binding");
            Require(ReferenceEquals(binding.Scope, scope.Seal), "错误scope身份");
            Require(binding.Owner.Snapshot.PendingCount is null, "最终Handle没有关闭flight");
        }
        Require(scope.Events.Count > 0, "没有真实事务事件");
        // 不同owner的事件可交错；每个owner分别追踪快照引用，不能全局邻接比较。
        var latest = new Dictionary<PyTraceState, TraceSnapshot>(ReferenceEqualityComparer.Instance);
        foreach (var item in scope.Events)
        {
            var binding = item.Error.TraceAdapterBinding ?? throw new InvalidOperationException("事件缺少真实binding");
            var owner = binding.Owner;
            Require(ReferenceEquals(binding.Scope, scope.Seal)
                && ReferenceEquals(item.Before.Owner, owner) && ReferenceEquals(item.After.Owner, owner),
                "事件binding/Before/After不是同一Owner");
            Require(ReferenceEquals(item.Before.Epoch, item.After.Epoch), "事件擅自切换Epoch");
            if (latest.TryGetValue(owner, out var prior))
                Require(ReferenceEquals(prior, item.Before), "同owner事件快照链断裂");
            latest[owner] = item.After;
            var request = item.After.Request ?? throw new InvalidOperationException("事件缺少After Q");
            Require(ReferenceEquals(request.Owner, owner) && ReferenceEquals(request.Epoch, item.After.Epoch),
                "After Request的Owner/Epoch引用错误");
            if (item.Before.Request is { } beforeRequest)
                Require(ReferenceEquals(beforeRequest.Owner, owner)
                    && ReferenceEquals(beforeRequest.Epoch, item.Before.Epoch), "Before Request的Owner/Epoch引用错误");
            Require(item.After.Version == item.Before.Version + 1, "真实事件不是一次快照发布");
            CheckSnapshotReceipt(item.Before);
            CheckSnapshotReceipt(item.After);
            if (item.Kind is "Explicit" or "NativeFresh")
                Require(item.After.Receipt is null && item.After.NextReceive == 1
                    && item.Before.PendingCount is null && request.Number == (item.Before.Request?.Number ?? 0) + 1,
                    "Source必须新Q且重置接收序列");
            if (item.Kind == "Explicit")
                Require(item.After.BeginKind == TraceBeginKind.Explicit && !item.After.GrantValid, "Source身份错误");
            else if (item.Kind == "NativeFresh")
            {
                Require(item.Before.Request is null && item.Before.History.Count == 0, "native必须真实冷源");
                Require(item.After.BeginKind == TraceBeginKind.NativeFresh && item.After.GrantUsed
                && !item.After.GrantValid && item.After.Request?.Number == 1
                && item.After.NextReceive == 1 && item.After.Receipt is null, "native出生授权错误");
            }
            else if (item.Kind is "Forward" or "Handle")
            {
                Require(item.Before.Request == item.After.Request, "接收错误地重新Begin");
                Require(item.Before.NextReceive + 1 == item.After.NextReceive, "接收R不连续");
                var receipt = item.After.Receipt ?? throw new InvalidOperationException("接收缺少真实receipt");
                Require(receipt.Event.Request == request
                    && ReferenceEquals(receipt.Event.Request.Owner, owner)
                    && ReferenceEquals(receipt.Event.Request.Epoch, item.After.Epoch)
                    && ReferenceEquals(receipt.Event.Boundary.Epoch, item.After.Epoch)
                    && receipt.Event.Number == item.Before.NextReceive && receipt.ResultVersion == item.After.Version,
                    "receipt与Q/Epoch/版本不一致");
                Require(receipt.Event.Kind == (item.Kind == "Forward" ? TraceReceiveKind.Forward : TraceReceiveKind.Handle)
                    && receipt.Event.Inclusion == (item.Kind == "Forward" ? TraceInclusion.Exclusive : TraceInclusion.Inclusive),
                    "receipt接收种类/边界包含错误");
                Require(item.LiveDepth >= receipt.Event.Boundary.Position + 1, "Receive晚于PopTo");
            }
            else throw new InvalidOperationException("未知适配事件");
        }
        foreach (var pair in latest)
            Require(ReferenceEquals(pair.Key.Snapshot, pair.Value), "owner真实最终Snapshot不是最后事件After");
        if (NativeChecks.IsNative(name)) NativeChecks.Check(scope, name);
        if (name == "explicit_inner")
        {
            Require(scope.Events.Count(e => e.Kind == "Explicit") == 2, "replacement需要两个独立源");
            Require(scope.Events.Count(e => e.Kind == "Forward") == 1, "replacement必须经过一次真实cleanup Forward");
            Require(scope.Events.Count(e => e.Kind == "Handle") == 2, "original和replacement各一次Handle");
            Require(scope.LastHandled!.Context is not null, "replacement丢失context身份");
        }
        if (name == "recursive")
        {
            var frames = scope.LastHandled!.TraceAdapterBinding!.Owner.Snapshot.History;
            Require(frames.Select(f => f.Activation).Distinct().Count() == frames.Count, "递归activation被合并");
        }
        if (name == "reused")
        {
            var sources = scope.Events.Where(e => e.Kind == "Explicit").ToArray();
            Require(sources.Length == 2, "reused必须两个Source");
            Require(ReferenceEquals(sources[0].Error, sources[1].Error)
                && ReferenceEquals(sources[0].After.Owner, sources[1].After.Owner), "reused更换对象/owner");
            Require(sources[0].After.Request?.Number == 1 && sources[1].After.Request?.Number == 2,
                "reused必须新Q且不新建owner");
            Require(sources[1].Before.PendingCount is null && sources[1].Before.History.Count > 0
                && sources[1].After.History.SequenceEqual(sources[1].Before.History), "reused丢失旧历史");
        }
        if (name == "explicit_inner")
        {
            var sources = scope.Events.Where(e => e.Kind == "Explicit").ToArray();
            Require(!ReferenceEquals(sources[0].Error, sources[1].Error)
                && !ReferenceEquals(sources[0].After.Owner, sources[1].After.Owner)
                && ReferenceEquals(last, sources[1].Error) && ReferenceEquals(last.Context, sources[0].Error),
                "replacement/context不是两个真实源的身份");
        }
    }

    private static void CheckSnapshotReceipt(TraceSnapshot state)
    {
        if (state.Receipt is not { } receipt) return;
        Require(state.Request == receipt.Event.Request
            && ReferenceEquals(receipt.Event.Request.Owner, state.Owner)
            && ReferenceEquals(receipt.Event.Request.Epoch, state.Epoch)
            && ReferenceEquals(receipt.Event.Boundary.Epoch, state.Epoch)
            && receipt.ResultVersion == state.Version && state.NextReceive == receipt.Event.Number + 1,
            "快照receipt与当前Owner/Epoch/Q/版本不一致");
    }

    private static string boundaryEvidence = "";
    private static readonly List<string> boundaryPassed = new();
    private static void Reject(string name, Action action)
    {
        try { action(); }
        catch (InvalidOperationException)
        {
            boundaryPassed.Add(name);
            ObservationWriter.Boundaries(boundaryEvidence, boundaryPassed);
            return;
        }
        throw new InvalidOperationException("应该拒绝的适配调用成功了: " + name);
    }

    private static void InScope(Action action)
    {
        var scope = PyTraceAdapter.EnterScope();
        Exception? failure = null;
        try { action(); }
        catch (Exception error) { failure = error; }
        try { scope.Dispose(); }
        catch (Exception error)
        {
            ObservationWriter.Status(boundaryEvidence, "manual-boundary-dispose", failure?.ToString(), error.ToString());
            if (failure is not null) Console.Error.WriteLine("Boundary Dispose: " + error);
            failure ??= error;
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    // 定向边界反例不冒充真实Python纵切；这里才允许手工建立单个帧。
    internal static void Boundaries(string evidence)
    {
        boundaryEvidence = evidence;
        boundaryPassed.Clear();
        ObservationWriter.Boundaries(evidence, boundaryPassed);
        var external = new PyValueError("external");
        Require(external.TraceAdapterBinding is null, "构造不应读取scope");
        Reject("outside-source", () => PyTraceAdapter.PrepareExplicit(external));
        Reject("outside-handle", () => PyTraceAdapter.Handle(external, 1));
        Require(external.TraceAdapterBinding is null, "scope外失败发布了binding");
        TraceSnapshot? completed = null;
        InScope(() =>
        {
            Reject("no-live-frame", () => PyTraceAdapter.PrepareExplicit(external));
            Require(external.TraceAdapterBinding is null, "首次失败不应发布binding");
            var depth = PyTraceback.PushFrame("host-boundary", "boundary");
            Reject("unbound-handle", () => PyTraceAdapter.Handle(external, depth));
            Require(external.TraceAdapterBinding is null, "catch代劳绑定");
            var legacy = new PyValueError("legacy");
            PyErrors.PrepareRaise(legacy);
            var oldFrames = legacy.Frames;
            Reject("legacy-history", () => PyTraceAdapter.PrepareExplicit(legacy));
            Require(legacy.TraceAdapterBinding is null && ReferenceEquals(oldFrames, legacy.Frames), "旧历史被导入或改写");
            PyTraceAdapter.PrepareExplicit(external);
            Require(!external.TraceAdapterBinding!.Owner.Snapshot.GrantValid, "Explicit必须撤销birth");
            var unfinished = external.TraceAdapterBinding.Owner.Snapshot;
            Reject("unfinished-source", () => PyTraceAdapter.PrepareExplicit(external));
            Require(ReferenceEquals(unfinished, external.TraceAdapterBinding.Owner.Snapshot), "重复未终结Source改变快照");
            PyTraceAdapter.Handle(external, depth);
            completed = external.TraceAdapterBinding.Owner.Snapshot;
            PyTraceback.PopTo(depth);
        });
        InScope(() =>
        {
            Reject("cross-scope-source", () => PyTraceAdapter.PrepareExplicit(external));
            Reject("cross-scope-handle", () => PyTraceAdapter.Handle(external, 1));
            Require(ReferenceEquals(completed, external.TraceAdapterBinding!.Owner.Snapshot), "跨scope改变历史");
        });
        var unfinishedScope = PyTraceAdapter.EnterScope();
        PyTraceback.PushFrame("host-boundary", "unfinished");
        PyTraceAdapter.PrepareExplicit(new PyValueError("unfinished"));
        Reject("unfinished-close", unfinishedScope.Dispose);
        Require(PyTraceAdapter.Active is null, "失败Close必须释放本次ambient scope");
    }

}
