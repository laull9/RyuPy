// 动态 except 的暂停/消费事务。所有更新先校验与构造，最后一次发布同一个 owner 快照。
// 此实现与产品适配共同交付；旧路径不会调用它，不以隔离单测替代真实生成器验收。
using System;

namespace RyuPy.Runtime;

internal sealed partial class PyTraceState
{
    internal TraceResult DispatchPark(TraceBoundary boundary, out TraceDispatchPark? token)
    {
        token = null;
        var old = _state;
        if (old.Park is not null || old.PendingCount is not int end || old.Request is not { } request)
            return TraceResult.Fail(TraceReject.Unfinished);
        if (!ReferenceEquals(boundary.Epoch, old.Epoch)) return TraceResult.Fail(TraceReject.Epoch);
        int start = boundary.Position;
        if (start < 0 || start >= end || end > old.Scene.Count
            || old.Scene[start].Activation != boundary.Activation)
            return TraceResult.Fail(TraceReject.Boundary);
        if (old.Version == long.MaxValue || old.History.Count > int.MaxValue - (end - start))
            return TraceResult.Fail(TraceReject.Overflow);
        var history = new TraceFrame[end - start + old.History.Count];
        for (int i = start; i < end; i++) history[i - start] = old.Scene[i];
        old.History.CopyTo(history, end - start);
        var park = new TraceDispatchPark(request, boundary, old.Scene, start);
        var next = new TraceSnapshot(this, old.Epoch, Array.AsReadOnly(history), old.Scene,
            old.Request, old.BeginKind, null, old.GrantUsed, old.GrantValid,
            old.Version + 1, old.Receipt, old.NextReceive, park, old.Continuation);
        _state = next;
        token = park;
        return TraceResult.Applied;
    }

    // Match 与 selector-failure 都只消费旧 park。新 flight（即便同对象）不受影响。
    internal TraceResult ConsumePark(TraceDispatchPark token)
    {
        var old = _state;
        var valid = ValidatePark(token, old);
        if (valid != TraceReject.None) return TraceResult.Fail(valid);
        if (old.Version == long.MaxValue) return TraceResult.Fail(TraceReject.Overflow);
        _state = new(this, old.Epoch, old.History, old.Scene, old.Request, old.BeginKind,
            old.PendingCount, old.GrantUsed, old.GrantValid, old.Version + 1,
            old.Receipt, old.NextReceive, continuation: old.Continuation);
        return TraceResult.Applied;
    }

    // all-miss 不是源码 raise：只重开原外段，并接到当前最新 H，不能复制旧快照/旧收据。
    internal TraceResult ResumePark(TraceDispatchPark token)
    {
        var old = _state;
        var valid = ValidatePark(token, old);
        if (valid != TraceReject.None) return TraceResult.Fail(valid);
        if (old.PendingCount is not null) return TraceResult.Fail(TraceReject.Unfinished);
        if (old.Version == long.MaxValue || old.Request is not { } previous
            || previous.Number == long.MaxValue) return TraceResult.Fail(TraceReject.Overflow);
        var request = new TraceRequest(this, old.Epoch, previous.Number + 1);
        _state = new(this, old.Epoch, old.History, token.Scene, request, TraceBeginKind.DispatchResume,
            token.Remaining, old.GrantUsed, false, old.Version + 1, null, 1, continuation: token);
        return TraceResult.Applied;
    }

    private TraceReject ValidatePark(TraceDispatchPark token, TraceSnapshot state)
    {
        if (!ReferenceEquals(token.Lineage.Owner, this)) return TraceReject.Owner;
        if (!ReferenceEquals(token.Lineage.Epoch, state.Epoch)) return TraceReject.Epoch;
        return ReferenceEquals(state.Park, token) ? TraceReject.None : TraceReject.Receipt;
    }
}
