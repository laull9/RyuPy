// S2a单元2：只保存真实native调用边界和已授权来源，不从catch或旧Frames推导Fresh。
#if TRACE_ADAPTER_TEST
using System;

namespace RyuPy.Runtime;

internal sealed record TraceNativeSource(PyBaseException Error, PyTraceState Owner, TraceRequest Request);

internal sealed class TraceNativeTicket
{
    internal readonly TraceAdapterScope Scope;
    internal readonly TraceBoundary Boundary;
    internal TraceNativeSource? Source;
    internal TraceNativeTicket(TraceAdapterScope scope, TraceBoundary boundary)
    { Scope = scope; Boundary = boundary; }
}

public static partial class PyTraceAdapter
{
    // 调用入口只授予边界身份，不是出生授权；无source的票据不能转发任意异常。
    internal static TraceNativeTicket CaptureNativeCaller()
    {
        var scope = RequireScope();
        return new(scope, PyTraceback.AdapterBoundary(scope.Authority.Current, PyTraceback.CaptureDepth()));
    }

    private static TraceAdapterScope ValidateNativeBoundary(TraceNativeTicket ticket)
    {
        var scope = RequireScope();
        if (!ReferenceEquals(ticket.Scope, scope))
            throw new InvalidOperationException("S2a native ticket belongs to another scope");
        var live = PyTraceback.AdapterBoundary(ticket.Boundary.Epoch, ticket.Boundary.Position + 1L);
        if (live != ticket.Boundary)
            throw new InvalidOperationException("S2a native caller activation expired");
        return scope;
    }

    internal static TraceAdapterScope ValidateNativeSource(TraceNativeTicket ticket)
    {
        var scope = ValidateNativeBoundary(ticket);
        if (ticket.Source is not null || !ReferenceEquals(ticket.Boundary.Epoch, scope.Authority.Current))
            throw new InvalidOperationException("S2a native birth ticket expired or already used");
        return scope;
    }

    // 已有flight按自己的epoch收尾；authority.Current前进不使旧flight失效。
    internal static void ForwardNative(PyBaseException error, TraceNativeTicket ticket)
    {
        var scope = ValidateNativeBoundary(ticket);
        var source = ticket.Source;
        var binding = error.TraceAdapterBinding;
        if (source is null || binding is null || !ReferenceEquals(source.Error, error)
            || !ReferenceEquals(source.Owner, binding.Owner) || !ReferenceEquals(binding.Scope, scope.Seal))
            throw new InvalidOperationException("S2a native receive requires this allocation's source");
        var owner = source.Owner;
        var before = owner.Snapshot;
        if (before.Request != source.Request || before.PendingCount is null
            || !ReferenceEquals(source.Request.Owner, owner)
            || !ReferenceEquals(source.Request.Epoch, before.Epoch)
            || !ReferenceEquals(ticket.Boundary.Epoch, before.Epoch))
            throw new InvalidOperationException("S2a native request or epoch mismatch");
        RequireApplied(owner.Receive(new(source.Request, before.NextReceive, ticket.Boundary,
            TraceInclusion.Exclusive, TraceReceiveKind.Forward)));
        scope.Events.Add(new("Forward", error, before, owner.Snapshot,
            PyTraceback.CaptureDepth(), error.Frames.Length, error.Context));
    }
}
#endif
