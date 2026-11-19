// 010 合同：epoch 可共享，许可绑定单 owner/from/version，发行新 epoch 不阻止旧 flight 收尾。
using System;
using RyuPy.Runtime;
using Xunit;
using static RyuPy.Runtime.Tests.PyTraceStateTests;

namespace RyuPy.Runtime.Tests;

public class PyTraceStateEpochTests
{
    private static TraceRotatePermit Permit(PyTraceContextAuthority authority, PyTraceState owner)
    {
        Applied(authority.AuthorizeRotate(owner, out var permit));
        return Assert.IsType<TraceRotatePermit>(permit);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoOwnersJoinOneEpochWithoutConsumingItsIdentity(bool reverse)
    {
        var authority = new PyTraceContextAuthority();
        var a = new PyTraceState(authority);
        var b = new PyTraceState(authority);
        foreach (var owner in new[] { a, b })
        {
            var q = Request(owner);
            Applied(owner.BeginExplicit(q, [Frame(ReferenceEquals(owner, a) ? 1 : 2)]));
            Applied(owner.Receive(Root(q)));
        }
        Applied(authority.IssueNextEpoch(out var target));
        var pa = Permit(authority, a);
        var pb = Permit(authority, b);
        Reject(b, TraceReject.Permit, () => b.Rotate(pa));
        var untouched = reverse ? a.Snapshot : b.Snapshot;
        Applied(reverse ? b.Rotate(pb) : a.Rotate(pa));
        Assert.Same(untouched, reverse ? a.Snapshot : b.Snapshot);
        Applied(reverse ? a.Rotate(pa) : b.Rotate(pb));
        Assert.Same(target, a.Snapshot.Epoch);
        Assert.Same(target, b.Snapshot.Epoch);
        Assert.Same(target, authority.Current);
        History(a, 1); History(b, 2);
        var bBefore = b.Snapshot;
        Reject(a, TraceReject.Permit, () => a.Rotate(pa));
        Assert.Same(bBefore, b.Snapshot);
    }

    [Fact]
    public void IssuingNewEpochLeavesOldFlightReceivableAndOtherOwnerCanRotateFirst()
    {
        var authority = new PyTraceContextAuthority();
        var a = new PyTraceState(authority);
        var b = new PyTraceState(authority);
        var q = Request(a);
        Applied(a.BeginExplicit(q, [Frame(1), Frame(2), Frame(3)]));
        var forward = At(q, 1, 1, 2);
        Applied(a.Receive(forward));
        Applied(authority.IssueNextEpoch(out var e2));
        var oldPermit = Permit(authority, a);
        var pb = Permit(authority, b);
        for (int i = 0; i < 2; i++) Reject(a, TraceReject.Unfinished, () => a.Rotate(oldPermit));
        Assert.False(a.Snapshot.GrantUsed);
        NoOp(a, () => a.Receive(forward));
        var aBefore = a.Snapshot;
        Applied(b.Rotate(pb));
        Assert.Same(aBefore, a.Snapshot);
        // 不能拿 authority.Current 校验 Receive，否则 owner A 会永久困在旧 flight。
        var close = At(q, 2, 1, 2, TraceInclusion.Inclusive, TraceReceiveKind.Handle);
        Applied(a.Receive(close));
        Reject(a, TraceReject.Permit, () => a.Rotate(oldPermit));
        NoOp(a, () => a.Receive(close));
        Applied(a.Rotate(Permit(authority, a)));
        Assert.Same(e2, a.Snapshot.Epoch);
        History(a, 2, 3);
        Assert.False(a.Snapshot.GrantUsed);
        Assert.False(a.Snapshot.GrantValid);
        Assert.Null(a.Snapshot.Request);
        Assert.Null(a.Snapshot.Receipt);
        Assert.Equal(0, a.Snapshot.NextReceive);
        Reject(a, TraceReject.Epoch, () => a.Receive(close));
        Reject(a, TraceReject.Epoch, () => a.BeginExplicit(q, []));
        Reject(a, TraceReject.Grant, () => a.BeginNativeFresh(Request(a), a.BirthGrant, []));
        var next = Request(a);
        Applied(a.BeginExplicit(next, [Frame(4)]));
        Reject(a, TraceReject.Epoch, () => a.Receive(Root(next) with { Boundary = TraceBoundary.Root(q.Epoch) }));
        Applied(a.Receive(Root(next)));
        History(a, 4, 2, 3);
    }

    [Fact]
    public void PermitRejectsForgeryWrongOwnerStaleVersionAndSupersededTarget()
    {
        var authority = new PyTraceContextAuthority();
        var owner = new PyTraceState(authority);
        var otherAuthority = new PyTraceContextAuthority();
        var other = new PyTraceState(otherAuthority);
        var original = owner.Snapshot;
        var e1 = authority.Current;
        Assert.Equal(TraceResult.Fail(TraceReject.Epoch), authority.AuthorizeRotate(owner, out var absent));
        Assert.Null(absent);
        Assert.Equal(TraceResult.Fail(TraceReject.Owner), authority.AuthorizeRotate(other, out absent));
        Assert.Null(absent);
        Assert.Same(original, owner.Snapshot);
        Assert.Same(e1, authority.Current);
        Assert.Null(TraceEpoch.Issue(authority, new object(), 999));
        Assert.Null(TraceRotatePermit.Issue(authority, new object(), original, e1));
        Reject(owner, TraceReject.Permit, () => owner.Rotate(null));
        Applied(authority.IssueNextEpoch(out var e2));
        var obsoleteTarget = Permit(authority, owner);
        Applied(authority.IssueNextEpoch(out var e3));
        Reject(owner, TraceReject.Epoch, () => owner.Rotate(obsoleteTarget));
        Assert.Same(e3, authority.Current);
        Assert.Same(e1, owner.Snapshot.Epoch);
        Assert.Equal(2, e2!.Generation);
        var staleVersion = Permit(authority, owner);
        var q = Request(owner);
        Applied(owner.BeginNativeFresh(q, owner.BirthGrant, []));
        Applied(owner.Receive(Root(q)));
        Reject(owner, TraceReject.Permit, () => owner.Rotate(staleVersion));
        Applied(owner.Rotate(Permit(authority, owner)));
        Assert.True(owner.Snapshot.GrantUsed);
        Assert.False(owner.Snapshot.GrantValid);
        Reject(owner, TraceReject.Grant, () => owner.BeginNativeFresh(Request(owner), owner.BirthGrant, []));
        Assert.Equal(TraceResult.Fail(TraceReject.Epoch), authority.AuthorizeRotate(owner, out absent));
        Assert.Null(absent);
    }

    [Fact]
    public void FailedBeginKeepsAnAlreadyIssuedPermitValid()
    {
        var authority = new PyTraceContextAuthority();
        var owner = new PyTraceState(authority);
        Applied(authority.IssueNextEpoch(out var e2));
        var permit = Permit(authority, owner);
        Reject(owner, TraceReject.Scene, () => owner.BeginExplicit(Request(owner), [Frame(1), Frame(1)]));
        Applied(owner.Rotate(permit));
        Assert.Same(e2, owner.Snapshot.Epoch);
    }
}
