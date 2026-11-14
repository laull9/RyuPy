// S1 未接线事务内核：只实现 Explicit、NativeFresh、Receive、Rotate 和不可变快照。
// 每次先校验/复制/构造完整 next，最后单引用发布；没有旧 PyErrors、全局注册表或解释器。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RyuPy.Runtime;

internal sealed partial class PyTraceState
{
    private TraceSnapshot _state;
    internal TraceSnapshot Snapshot => _state;
    internal TraceBirthGrant BirthGrant { get; }

    // owner 即本实例；创建时的 grant 只绑定本实例和出生 epoch，不能由空 H 推导。
    internal PyTraceState(PyTraceContextAuthority authority)
    {
        BirthGrant = TraceBirthGrant.Create();
        _state = new(this, authority.Current, Empty(), Empty(), null, null, null,
            false, true, 0, null, 0);
    }

    private static ReadOnlyCollection<TraceFrame> Empty() => Array.AsReadOnly(Array.Empty<TraceFrame>());

    internal TraceResult BeginExplicit(TraceRequest request, TraceFrame[] scene)
        => Begin(request, scene, TraceBeginKind.Explicit, null);

    internal TraceResult BeginNativeFresh(TraceRequest request, TraceBirthGrant grant, TraceFrame[] scene)
        => Begin(request, scene, TraceBeginKind.NativeFresh, grant);

    private TraceResult Begin(TraceRequest request, TraceFrame[] scene,
        TraceBeginKind kind, TraceBirthGrant? grant)
    {
        var old = _state;
        var identity = ValidateRequest(request, old);
        if (identity != TraceReject.None) return TraceResult.Fail(identity);
        if (scene is null) return TraceResult.Fail(TraceReject.Scene);
        // 先复制再验证，调用方以后改数组不能改变已接受 scene 或旧 snapshot。
        var copy = (TraceFrame[])scene.Clone();
        var seen = new HashSet<long>();
        foreach (var frame in copy)
            if (frame.Activation <= 0 || frame.File is null || frame.Function is null || frame.Line < 0
                || !seen.Add(frame.Activation)) return TraceResult.Fail(TraceReject.Scene);
        if (kind == TraceBeginKind.NativeFresh && !ReferenceEquals(grant, BirthGrant))
            return TraceResult.Fail(TraceReject.Grant);
        if (old.Request is TraceRequest prior && request.Number == prior.Number)
        {
            if (old.BeginKind != kind || !SameScene(old.Scene, copy))
                return TraceResult.Fail(TraceReject.Request);
            // 同一最近 Begin 原样重送不会重开已经结束的 flight，也不会清 receive 收据。
            return TraceResult.NoOp;
        }
        if (old.Request is TraceRequest previous && request.Number <= previous.Number)
            return TraceResult.Fail(TraceReject.Request);
        if (old.PendingCount.HasValue) return TraceResult.Fail(TraceReject.Unfinished);
        if (kind == TraceBeginKind.NativeFresh && (old.GrantUsed || !old.GrantValid))
            return TraceResult.Fail(TraceReject.Grant);
        if (old.Version == long.MaxValue) return TraceResult.Fail(TraceReject.Overflow);
        // 成功 Explicit 也永久撤销出生资格，不能把处理过但 H 为空的对象再当 NativeFresh。
        var next = new TraceSnapshot(this, old.Epoch, old.History, Array.AsReadOnly(copy), request,
            kind, copy.Length, old.GrantUsed || kind == TraceBeginKind.NativeFresh,
            false, old.Version + 1, null, 1, old.Park);
        _state = next;
        return TraceResult.Applied;
    }

    internal TraceResult Receive(TraceReceive input)
    {
        var old = _state;
        var identity = ValidateRequest(input.Request, old);
        if (identity != TraceReject.None) return TraceResult.Fail(identity);
        if (old.Request != input.Request) return TraceResult.Fail(TraceReject.Request);
        if (old.Receipt is TraceReceipt receipt && receipt.Event.Number == input.Number)
            return receipt.Event == input ? TraceResult.NoOp : TraceResult.Fail(TraceReject.Receipt);
        if (input.Number != old.NextReceive || input.Number <= 0)
            return TraceResult.Fail(TraceReject.Sequence);
        if (!old.PendingCount.HasValue) return TraceResult.Fail(TraceReject.Request);
        var boundary = input.Boundary;
        if (!ReferenceEquals(boundary.Epoch, old.Epoch)) return TraceResult.Fail(TraceReject.Epoch);
        bool root = boundary.Position == -1 && boundary.Activation == 0;
        if (!root && (boundary.Position < 0 || boundary.Position >= old.Scene.Count
            || old.Scene[boundary.Position].Activation != boundary.Activation))
            return TraceResult.Fail(TraceReject.Boundary);
        if (input.Inclusion != TraceInclusion.Exclusive && input.Inclusion != TraceInclusion.Inclusive)
            return TraceResult.Fail(TraceReject.Mode);
        if (input.Kind != TraceReceiveKind.Forward && input.Kind != TraceReceiveKind.Handle
            && input.Kind != TraceReceiveKind.Suppress && input.Kind != TraceReceiveKind.Terminal)
            return TraceResult.Fail(TraceReject.Mode);
        if ((root && input.Inclusion == TraceInclusion.Inclusive)
            || (input.Kind == TraceReceiveKind.Terminal && !root))
            return TraceResult.Fail(TraceReject.Boundary);
        if ((input.Kind == TraceReceiveKind.Handle && (root || input.Inclusion != TraceInclusion.Inclusive))
            || (input.Kind != TraceReceiveKind.Handle && input.Inclusion != TraceInclusion.Exclusive))
            return TraceResult.Fail(TraceReject.Mode);
        int end = old.PendingCount.Value;
        int start = root ? 0 : boundary.Position + (input.Inclusion == TraceInclusion.Exclusive ? 1 : 0);
        // 首次空内段 Forward 合法（native 本地错误）；已有最近 Forward 后不能用新 R 重包同一边界。
        // 有 F 的成功 Receive 只可能是 Forward，因此最近收据足以识别游标未推进的重复。
        if (start > end || (!root && boundary.Position >= end)
            || (input.Kind == TraceReceiveKind.Forward && start == end && old.Receipt is not null))
            return TraceResult.Fail(TraceReject.Cursor);
        if (old.Version == long.MaxValue || old.NextReceive == long.MaxValue
            || old.History.Count > int.MaxValue - (end - start))
            return TraceResult.Fail(TraceReject.Overflow);
        var history = new TraceFrame[end - start + old.History.Count];
        for (int i = start; i < end; i++) history[i - start] = old.Scene[i];
        old.History.CopyTo(history, end - start);
        long version = old.Version + 1;
        var next = new TraceSnapshot(this, old.Epoch, Array.AsReadOnly(history), old.Scene,
            old.Request, old.BeginKind, input.Kind == TraceReceiveKind.Forward ? start : null,
            old.GrantUsed, old.GrantValid, version, new TraceReceipt(input, version), old.NextReceive + 1,
            old.Park, old.Continuation);
        _state = next;
        return TraceResult.Applied;
    }

    internal TraceResult Rotate(TraceRotatePermit? permit)
    {
        var old = _state;
        if (permit is null || !ReferenceEquals(permit.Owner, this)) return TraceResult.Fail(TraceReject.Permit);
        if (!ReferenceEquals(permit.From, old.Epoch) || permit.Version != old.Version)
            return TraceResult.Fail(TraceReject.Permit);
        if (!ReferenceEquals(permit.Target.Authority, old.Epoch.Authority)
            || !ReferenceEquals(permit.Target, old.Epoch.Authority.Current)
            || permit.Target.Generation <= old.Epoch.Generation) return TraceResult.Fail(TraceReject.Epoch);
        if (old.PendingCount.HasValue || old.Park is not null)
            return TraceResult.Fail(TraceReject.Unfinished);
        if (old.Version == long.MaxValue) return TraceResult.Fail(TraceReject.Overflow);
        var next = new TraceSnapshot(this, permit.Target, old.History, Empty(), null, null, null,
            old.GrantUsed, false, old.Version + 1, null, 0);
        _state = next;
        return TraceResult.Applied;
    }

    private TraceReject ValidateRequest(TraceRequest request, TraceSnapshot state)
    {
        if (!ReferenceEquals(request.Owner, this)) return TraceReject.Owner;
        if (!ReferenceEquals(request.Epoch, state.Epoch)) return TraceReject.Epoch;
        return request.Number <= 0 ? TraceReject.Request : TraceReject.None;
    }

    private static bool SameScene(ReadOnlyCollection<TraceFrame> left, TraceFrame[] right)
    {
        if (left.Count != right.Length) return false;
        for (int i = 0; i < right.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }
}
