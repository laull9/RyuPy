// 单元2定向适配反例；手工帧只用于协议反例，不冒充真实Python纵切。
#if TRACE_ADAPTER_TEST
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[CollectionDefinition("TraceNativeSerial", DisableParallelization = true)]
public sealed class TraceNativeSerialCollection { }

[Collection("TraceNativeSerial")]
public sealed class PyTraceNativeAdapterTests
{
    private static PyTypeError Native()
        => Assert.Throws<PyTypeError>(() => PyOps.FunctionLocalsValidate([1L], [], []));

    private static TraceNativeTicket ExistingTicket(PyBaseException error)
    {
        var ticket = PyTraceAdapter.CaptureNativeCaller();
        var owner = error.TraceAdapterBinding!.Owner;
        ticket.Source = new(error, owner, owner.Snapshot.Request!.Value);
        return ticket;
    }

    [Fact]
    public void NativeSourcePreservesIdentityOrderAndContext()
    {
        using var scope = (TraceAdapterScope)PyTraceAdapter.EnterScope();
        long depth = PyTraceback.PushFrame("native-tests", "caller");
        var original = new PyValueError("context");
        PyTraceAdapter.PrepareExplicit(original);
        PyTraceAdapter.Handle(original, depth);
        object? previous = PyErrors.EnterHandler(original);
        try
        {
            var error = Native();
            Assert.Same(original, error.Context);
            var events = scope.Events.Where(e => ReferenceEquals(e.Error, error)).ToArray();
            Assert.Equal(2, events.Length);
            Assert.Equal("NativeFresh", events[0].Kind);
            Assert.Equal("Forward", events[1].Kind);
            Assert.All(events, e => { Assert.Equal(0, e.LegacyFrameCount); Assert.Null(e.LegacyContext); });
            Assert.NotEmpty(error.Frames);
            Assert.Same(events[0].After.Owner, events[1].After.Owner);
            Assert.Equal(events[0].After.Request, events[1].After.Request);
            Assert.True(events[0].After.GrantUsed);
            Assert.False(events[0].After.GrantValid);
            PyTraceAdapter.Handle(error, depth);
            Assert.Same(error, scope.LastHandled);
        }
        finally { PyErrors.LeaveHandler(previous); }
        PyTraceback.PopTo(depth);
    }

    [Fact]
    public void NativeReceiveRejectsUnknownSourceAndDuplicateBoundary()
    {
        using var scope = PyTraceAdapter.EnterScope();
        long depth = PyTraceback.PushFrame("native-tests", "caller");
        var unknown = new PyTypeError("unknown");
        var frames = unknown.Frames;
        Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ForwardNative(unknown, PyTraceAdapter.CaptureNativeCaller()));
        Assert.Null(unknown.TraceAdapterBinding);
        Assert.Same(frames, unknown.Frames);
        Assert.Null(unknown.Context);
        // 第二类原native错误仅作拒绝反例；不得在catch获得出生资格。
        Assert.Throws<InvalidOperationException>(() => PyOps.FunctionLocalsValidate([], ["x"], [1L]));
        var error = Native();
        var owner = error.TraceAdapterBinding!.Owner;
        var state = owner.Snapshot;
        Assert.Equal(TraceChange.NoOp, owner.Receive(state.Receipt!.Event).Change);
        Assert.Same(state, owner.Snapshot);
        Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ForwardNative(error, ExistingTicket(error)));
        Assert.Same(state, owner.Snapshot);
        Assert.Equal(TraceReject.Receipt, owner.Receive(state.Receipt.Event with { Kind = TraceReceiveKind.Handle }).Reject);
        Assert.Same(state, owner.Snapshot);
        Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ForwardNative(unknown, ExistingTicket(error)));
        PyTraceAdapter.Handle(error, depth);
        PyTraceback.PopTo(depth);
    }

    [Fact]
    public void NativeBirthRejectsWrongConsumedAndExplicitRevokedGrants()
    {
        using var scope = (TraceAdapterScope)PyTraceAdapter.EnterScope();
        long depth = PyTraceback.PushFrame("native-tests", "caller");
        var scene = PyTraceback.AdapterScene();
        var owner = new PyTraceState(scope.Authority);
        var other = new PyTraceState(scope.Authority);
        var initial = owner.Snapshot;
        Assert.Equal(TraceReject.Grant, owner.BeginNativeFresh(new(owner, initial.Epoch, 1), other.BirthGrant, scene).Reject);
        Assert.Same(initial, owner.Snapshot);
        var error = Native();
        var native = error.TraceAdapterBinding!.Owner;
        PyTraceAdapter.Handle(error, depth);
        var completed = native.Snapshot;
        Assert.Equal(TraceReject.Grant, native.BeginNativeFresh(new(native, completed.Epoch, 2), native.BirthGrant, scene).Reject);
        Assert.Same(completed, native.Snapshot);
        var explicitError = new PyValueError("explicit");
        PyTraceAdapter.PrepareExplicit(explicitError);
        PyTraceAdapter.Handle(explicitError, depth);
        var explicitOwner = explicitError.TraceAdapterBinding!.Owner;
        var explicitState = explicitOwner.Snapshot;
        Assert.Equal(TraceReject.Grant, explicitOwner.BeginNativeFresh(new(explicitOwner, explicitState.Epoch, 2), explicitOwner.BirthGrant, scene).Reject);
        Assert.Same(explicitState, explicitOwner.Snapshot);
        PyTraceback.PopTo(depth);
    }

    [Fact]
    public void NativeTicketsRejectStaleActivationScopeAndColdEpoch()
    {
        TraceNativeTicket old;
        using (var scope = (TraceAdapterScope)PyTraceAdapter.EnterScope())
        {
            long depth = PyTraceback.PushFrame("native-tests", "caller");
            old = PyTraceAdapter.CaptureNativeCaller();
            PyTraceback.PopTo(depth);
            long replacement = PyTraceback.PushFrame("native-tests", "caller");
            Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ValidateNativeSource(old));
            var cold = PyTraceAdapter.CaptureNativeCaller();
            Assert.Equal(TraceChange.Applied, scope.Authority.IssueNextEpoch(out _).Change);
            Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ValidateNativeSource(cold));
            PyTraceback.PopTo(replacement);
        }
        using (PyTraceAdapter.EnterScope())
        {
            long depth = PyTraceback.PushFrame("native-tests", "caller");
            Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ValidateNativeSource(old));
            PyTraceback.PopTo(depth);
        }
    }

    [Fact]
    public void NativeOldFlightFinishesAndUnfinishedCloseReleasesScope()
    {
        using (var scope = (TraceAdapterScope)PyTraceAdapter.EnterScope())
        {
            long outer = PyTraceback.PushFrame("native-tests", "outer");
            var ticket = PyTraceAdapter.CaptureNativeCaller();
            long inner = PyTraceback.PushFrame("native-tests", "inner");
            var error = Native();
            var owner = error.TraceAdapterBinding!.Owner;
            ticket.Source = new(error, owner, owner.Snapshot.Request!.Value);
            Assert.Equal(TraceChange.Applied, scope.Authority.IssueNextEpoch(out _).Change);
            var before = owner.Snapshot;
            var wrong = PyTraceAdapter.CaptureNativeCaller();
            wrong.Source = new(error, owner, before.Request!.Value);
            Assert.Throws<InvalidOperationException>(() => PyTraceAdapter.ForwardNative(error, wrong));
            Assert.Same(before, owner.Snapshot);
            // 定向权限测试：旧flight向更外层合法推进，不是再次空Forward。
            PyTraceAdapter.ForwardNative(error, ticket);
            PyTraceback.PopTo(inner);
            PyTraceAdapter.Handle(error, outer);
            PyTraceback.PopTo(outer);
        }
        var unfinished = PyTraceAdapter.EnterScope();
        PyTraceback.PushFrame("native-tests", "unfinished");
        var pending = Native();
        var state = pending.TraceAdapterBinding!.Owner.Snapshot;
        Assert.Throws<InvalidOperationException>(unfinished.Dispose);
        Assert.Null(PyTraceAdapter.Active);
        Assert.Equal(0L, PyTraceback.CaptureDepth());
        Assert.Same(state, pending.TraceAdapterBinding.Owner.Snapshot);
    }
}
#endif
