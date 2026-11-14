// 实际 S1 内核的有限事务验收；拒绝逐次比较整个不可变状态引用，而非只比 traceback 文本。
using System;
using System.Collections.Generic;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyTraceStateTests
{
    internal static TraceFrame Frame(long id, string name = "same", int line = 7) => new(id, "test.py", name, line);
    internal static TraceRequest Request(PyTraceState state, long number = 1) => new(state, state.Snapshot.Epoch, number);
    internal static TraceReceive At(TraceRequest q, long r, int position, long activation,
        TraceInclusion inclusion = TraceInclusion.Exclusive, TraceReceiveKind kind = TraceReceiveKind.Forward)
        => new(q, r, new TraceBoundary(q.Epoch, position, activation), inclusion, kind);
    internal static TraceReceive Root(TraceRequest q, long r = 1, TraceReceiveKind kind = TraceReceiveKind.Terminal)
        => new(q, r, TraceBoundary.Root(q.Epoch), TraceInclusion.Exclusive, kind);
    internal static void Applied(TraceResult result) => Assert.Equal(TraceResult.Applied, result);
    internal static void Reject(PyTraceState state, TraceReject reason, Func<TraceResult> action)
    {
        var before = state.Snapshot;
        Assert.Equal(TraceResult.Fail(reason), action());
        Assert.Same(before, state.Snapshot);
    }
    internal static void NoOp(PyTraceState state, Func<TraceResult> action)
    {
        var before = state.Snapshot;
        Assert.Equal(TraceResult.NoOp, action());
        Assert.Same(before, state.Snapshot);
    }
    internal static void History(PyTraceState state, params long[] ids)
        => Assert.Equal(ids, state.Snapshot.History.Select(f => f.Activation));

    [Fact]
    public void ExclusiveForwardThenInclusiveHandlePrependsOnlyTraversedFramesToLatestHistory()
    {
        var owner = new PyTraceState(new());
        var old = Request(owner);
        Applied(owner.BeginExplicit(old, [Frame(9, "old")]));
        Applied(owner.Receive(Root(old)));
        var saved = owner.Snapshot;
        var q = Request(owner, 2);
        Applied(owner.BeginExplicit(q, [Frame(1, "M"), Frame(2, "P"), Frame(3, "C")]));
        History(owner, 9);
        Applied(owner.Receive(At(q, 1, 1, 2)));
        History(owner, 3, 9);
        Assert.Equal(2, owner.Snapshot.PendingCount);
        Applied(owner.Receive(At(q, 2, 1, 2, TraceInclusion.Inclusive, TraceReceiveKind.Handle)));
        History(owner, 2, 3, 9);
        Assert.Null(owner.Snapshot.PendingCount);
        Assert.Equal(new long[] { 9 }, saved.History.Select(f => f.Activation));
        Assert.Equal(2, saved.Version);
    }

    [Fact]
    public void FailedFreshBeginDoesNotConsumeBirthGrantAndLegalRetrySucceeds()
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        for (int i = 0; i < 2; i++)
            Reject(owner, TraceReject.Scene, () => owner.BeginNativeFresh(q, owner.BirthGrant, [Frame(1), Frame(1)]));
        Reject(owner, TraceReject.Request, () => owner.BeginNativeFresh(q with { Number = 0 }, owner.BirthGrant, []));
        Assert.False(owner.Snapshot.GrantUsed);
        Assert.True(owner.Snapshot.GrantValid);
        Applied(owner.BeginNativeFresh(q, owner.BirthGrant, []));
        Assert.True(owner.Snapshot.GrantUsed);
        Assert.False(owner.Snapshot.GrantValid);
        NoOp(owner, () => owner.BeginNativeFresh(q, owner.BirthGrant, []));
        Reject(owner, TraceReject.Unfinished, () => owner.BeginNativeFresh(Request(owner, 2), owner.BirthGrant, []));
        Applied(owner.Receive(Root(q)));
        Reject(owner, TraceReject.Grant, () => owner.BeginNativeFresh(Request(owner, 2), owner.BirthGrant, []));
        History(owner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitPermanentlyRevokesBirthEvenWhenCommittedHistoryIsEmpty(bool callback)
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        Applied(owner.BeginExplicit(q, callback ? [Frame(1)] : []));
        Assert.False(owner.Snapshot.GrantValid);
        Assert.False(owner.Snapshot.GrantUsed);
        Reject(owner, TraceReject.Unfinished, () => owner.BeginNativeFresh(Request(owner, 2), owner.BirthGrant, []));
        Applied(owner.Receive(callback
            ? At(q, 1, 0, 1, TraceInclusion.Inclusive, TraceReceiveKind.Handle)
            : Root(q)));
        Reject(owner, TraceReject.Grant, () => owner.BeginNativeFresh(Request(owner, 2), owner.BirthGrant, []));
        History(owner, callback ? [1] : []);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FirstEmptyForwardIsLegalButNewSequenceCannotResubmitTheBoundary(bool root, bool suppress)
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        Applied(owner.BeginNativeFresh(q, owner.BirthGrant, root ? [] : [Frame(1), Frame(2)]));
        var forward = root ? Root(q, kind: TraceReceiveKind.Forward) : At(q, 1, 1, 2);
        Applied(owner.Receive(forward));
        NoOp(owner, () => owner.Receive(forward));
        History(owner);
        Reject(owner, TraceReject.Cursor, () => owner.Receive(forward with { Number = 2 }));
        NoOp(owner, () => owner.Receive(forward));
        var close = suppress
            ? (root ? Root(q, 2, TraceReceiveKind.Suppress) : At(q, 2, 1, 2, kind: TraceReceiveKind.Suppress))
            : (root ? Root(q, 2) : At(q, 2, 1, 2, TraceInclusion.Inclusive, TraceReceiveKind.Handle));
        Applied(owner.Receive(close));
        History(owner, root || suppress ? [] : [2]);
        Assert.Null(owner.Snapshot.PendingCount);
    }

    [Fact]
    public void ReceiveRejectsInvalidModeCombinationsWithoutPublishing()
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        Applied(owner.BeginExplicit(q, [Frame(1), Frame(2)]));
        var invalid = new[]
        {
            At(q, 1, 0, 1, TraceInclusion.Inclusive, TraceReceiveKind.Forward),
            At(q, 1, 0, 1, TraceInclusion.Inclusive, TraceReceiveKind.Suppress),
            At(q, 1, 0, 1, TraceInclusion.Exclusive, TraceReceiveKind.Handle),
            Root(q, kind: TraceReceiveKind.Handle),
        };
        foreach (var input in invalid) Reject(owner, TraceReject.Mode, () => owner.Receive(input));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RootHandlesOnlyRealCallbackFramesAndNeverInfersFreshFromEmptyHistory(bool callback)
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        Applied(owner.BeginNativeFresh(q, owner.BirthGrant, callback ? [Frame(1)] : []));
        Reject(owner, TraceReject.Boundary, () => owner.Receive(Root(q) with { Inclusion = TraceInclusion.Inclusive }));
        Applied(owner.Receive(Root(q)));
        History(owner, callback ? [1] : []);
        NoOp(owner, () => owner.Receive(Root(q)));
        Reject(owner, TraceReject.Grant, () => owner.BeginNativeFresh(Request(owner, 2), owner.BirthGrant, []));
        Applied(owner.BeginExplicit(Request(owner, 2), []));
        History(owner, callback ? [1] : []);
    }

    [Theory]
    [InlineData((int)TraceReceiveKind.Suppress)]
    [InlineData((int)TraceReceiveKind.Handle)]
    public void ClosingReceiveDiscardsOuterPendingWithoutRestoringHistory(int kindValue)
    {
        var kind = (TraceReceiveKind)kindValue;
        var owner = new PyTraceState(new());
        var q = Request(owner);
        Applied(owner.BeginExplicit(q, [Frame(1), Frame(2), Frame(3)]));
        var inclusion = kind == TraceReceiveKind.Handle ? TraceInclusion.Inclusive : TraceInclusion.Exclusive;
        Applied(owner.Receive(At(q, 1, 1, 2, inclusion, kind)));
        History(owner, kind == TraceReceiveKind.Handle ? [2, 3] : [3]);
        Assert.Null(owner.Snapshot.PendingCount);
        var later = Request(owner, 2);
        Applied(owner.BeginExplicit(later, [Frame(3)]));
        Applied(owner.Receive(Root(later)));
        // 同一显示名和 activation 再次显式请求也不跨请求历史去重。
        History(owner, kind == TraceReceiveKind.Handle ? [3, 2, 3] : [3, 3]);
    }

    [Fact]
    public void OnlyLatestReceiveReplaysAndNewRequestInvalidatesTheReceipt()
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        Applied(owner.BeginExplicit(q, [Frame(1), Frame(2), Frame(3)]));
        var first = At(q, 1, 1, 2);
        Applied(owner.Receive(first));
        NoOp(owner, () => owner.Receive(first));
        Reject(owner, TraceReject.Receipt, () => owner.Receive(first with { Kind = TraceReceiveKind.Suppress }));
        Reject(owner, TraceReject.Sequence, () => owner.Receive(Root(q, 3)));
        Reject(owner, TraceReject.Cursor, () => owner.Receive(At(q, 2, 1, 2)));
        NoOp(owner, () => owner.Receive(first));
        var second = At(q, 2, 1, 2, TraceInclusion.Inclusive, TraceReceiveKind.Handle);
        Applied(owner.Receive(second));
        Reject(owner, TraceReject.Sequence, () => owner.Receive(first));
        NoOp(owner, () => owner.Receive(second));
        // 失败 Begin 不能清除终结收据。
        Reject(owner, TraceReject.Scene, () => owner.BeginExplicit(Request(owner, 2), [Frame(1), Frame(1)]));
        NoOp(owner, () => owner.Receive(second));
        NoOp(owner, () => owner.BeginExplicit(q, [Frame(1), Frame(2), Frame(3)]));
        Applied(owner.BeginExplicit(Request(owner, 2), []));
        Reject(owner, TraceReject.Request, () => owner.Receive(second));
        Reject(owner, TraceReject.Request, () => owner.BeginExplicit(q, [Frame(1), Frame(2), Frame(3)]));
    }

    [Fact]
    public void InvalidInputsLeaveEveryStateFieldAndGrantUntouched()
    {
        var authority = new PyTraceContextAuthority();
        var owner = new PyTraceState(authority);
        var other = new PyTraceState(authority);
        var q = Request(owner);
        var wrongEpoch = new PyTraceContextAuthority().Current;
        var beginCases = new (TraceReject Why, Func<TraceResult> Run)[]
        {
            (TraceReject.Owner, () => owner.BeginExplicit(q with { Owner = other }, [])),
            (TraceReject.Epoch, () => owner.BeginExplicit(q with { Epoch = wrongEpoch }, [])),
            (TraceReject.Request, () => owner.BeginExplicit(q with { Number = 0 }, [])),
            (TraceReject.Scene, () => owner.BeginExplicit(q, null!)),
            (TraceReject.Scene, () => owner.BeginExplicit(q, [Frame(0)])),
            (TraceReject.Scene, () => owner.BeginExplicit(q, [Frame(1), Frame(1)])),
            (TraceReject.Scene, () => owner.BeginExplicit(q, [new(1, null!, "f", 1)])),
            (TraceReject.Scene, () => owner.BeginExplicit(q, [Frame(1, line: -1)])),
            (TraceReject.Grant, () => owner.BeginNativeFresh(q, other.BirthGrant, [])),
            (TraceReject.Grant, () => owner.BeginNativeFresh(q, TraceBirthGrant.Create(), [])),
        };
        foreach (var test in beginCases) Reject(owner, test.Why, test.Run);
        Applied(owner.BeginExplicit(q, [Frame(1), Frame(2), Frame(3)]));
        var input = At(q, 1, 1, 2);
        var receiveCases = new (TraceReject Why, TraceReceive Event)[]
        {
            (TraceReject.Owner, input with { Request = q with { Owner = other } }),
            (TraceReject.Epoch, input with { Request = q with { Epoch = wrongEpoch } }),
            (TraceReject.Request, input with { Request = q with { Number = 2 } }),
            (TraceReject.Sequence, input with { Number = 0 }),
            (TraceReject.Sequence, input with { Number = 2 }),
            (TraceReject.Epoch, input with { Boundary = input.Boundary with { Epoch = wrongEpoch } }),
            (TraceReject.Boundary, input with { Boundary = input.Boundary with { Activation = 99 } }),
            (TraceReject.Boundary, input with { Boundary = input.Boundary with { Position = 3 } }),
            (TraceReject.Boundary, input with { Boundary = new(q.Epoch, -1, 2) }),
            (TraceReject.Boundary, input with { Kind = TraceReceiveKind.Terminal }),
            (TraceReject.Mode, input with { Kind = (TraceReceiveKind)999 }),
            (TraceReject.Mode, input with { Inclusion = (TraceInclusion)999 }),
        };
        foreach (var test in receiveCases) Reject(owner, test.Why, () => owner.Receive(test.Event));
        Reject(owner, TraceReject.Request, () => owner.BeginExplicit(q, []));
        Reject(owner, TraceReject.Unfinished, () => owner.BeginExplicit(Request(owner, 2), []));
        Assert.False(owner.Snapshot.GrantUsed);
    }

    [Fact]
    public void SceneAndSnapshotsAreDefensiveAndSameNamedActivationsRemainDistinct()
    {
        var owner = new PyTraceState(new());
        var q = Request(owner);
        TraceFrame[] scene = [Frame(1), Frame(2), Frame(3)];
        Applied(owner.BeginExplicit(q, scene));
        var before = owner.Snapshot;
        scene[1] = Frame(999);
        Assert.Equal(2, before.Scene[1].Activation);
        Assert.Throws<NotSupportedException>(() => ((IList<TraceFrame>)before.Scene)[1] = Frame(999));
        Reject(owner, TraceReject.Boundary, () => owner.Receive(At(q, 1, 1, 999)));
        Applied(owner.Receive(Root(q)));
        History(owner, 1, 2, 3);
        Assert.Empty(before.History);
        Assert.Equal(3, before.PendingCount);
        var saved = owner.Snapshot;
        Assert.Throws<NotSupportedException>(() => ((IList<TraceFrame>)saved.History).Clear());
        var later = Request(owner, 2);
        Applied(owner.BeginExplicit(later, [Frame(4), Frame(5), Frame(6)]));
        Reject(owner, TraceReject.Boundary, () => owner.Receive(At(later, 1, 1, 2)));
        Applied(owner.Receive(Root(later)));
        History(owner, 4, 5, 6, 1, 2, 3);
        Assert.Equal(new long[] { 1, 2, 3 }, saved.History.Select(f => f.Activation));
    }
}
