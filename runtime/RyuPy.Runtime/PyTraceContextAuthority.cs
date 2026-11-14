// 显式串行 context 的 epoch 发行能力。共享 epoch 不消费，Rotate 只发布 owner 局部状态。
using System;

namespace RyuPy.Runtime;

internal sealed class TraceEpoch
{
    internal PyTraceContextAuthority Authority { get; }
    internal long Generation { get; }
    private TraceEpoch(PyTraceContextAuthority authority, long generation)
    { Authority = authority; Generation = generation; }

    // seal 不向调用者泄漏；伪造工厂调用不能得到有效身份。
    internal static TraceEpoch? Issue(PyTraceContextAuthority authority, object seal, long generation)
        => authority.AcceptsSeal(seal) ? new(authority, generation) : null;
}

internal sealed class TraceRotatePermit
{
    internal PyTraceState Owner { get; }
    internal TraceEpoch From { get; }
    internal TraceEpoch Target { get; }
    internal long Version { get; }
    private TraceRotatePermit(PyTraceState owner, TraceEpoch from, TraceEpoch target, long version)
    { Owner = owner; From = from; Target = target; Version = version; }

    internal static TraceRotatePermit? Issue(PyTraceContextAuthority authority, object seal,
        TraceSnapshot state, TraceEpoch target)
        => authority.AcceptsSeal(seal)
            ? new(state.Owner, state.Epoch, target, state.Version) : null;
}

internal sealed class PyTraceContextAuthority
{
    private readonly object _seal = new();
    internal TraceEpoch Current { get; private set; }

    internal PyTraceContextAuthority()
    {
        Current = TraceEpoch.Issue(this, _seal, 1)!;
    }

    internal bool AcceptsSeal(object seal) => ReferenceEquals(_seal, seal);

    internal TraceResult IssueNextEpoch(out TraceEpoch? epoch)
    {
        epoch = null;
        if (Current.Generation == long.MaxValue) return TraceResult.Fail(TraceReject.Overflow);
        // 分配成功后才更换 current；不遍历 owner，也不使旧 flight 无法收尾。
        var next = TraceEpoch.Issue(this, _seal, Current.Generation + 1)!;
        Current = next;
        epoch = next;
        return TraceResult.Applied;
    }

    internal TraceResult AuthorizeRotate(PyTraceState owner, out TraceRotatePermit? permit)
    {
        permit = null;
        var state = owner.Snapshot;
        if (!ReferenceEquals(state.Epoch.Authority, this)) return TraceResult.Fail(TraceReject.Owner);
        if (Current.Generation <= state.Epoch.Generation) return TraceResult.Fail(TraceReject.Epoch);
        permit = TraceRotatePermit.Issue(this, _seal, state, Current);
        return TraceResult.Applied;
    }
}
