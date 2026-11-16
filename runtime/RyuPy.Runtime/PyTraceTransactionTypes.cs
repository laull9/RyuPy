// 未接线的 traceback 事务值类型。身份由调用方显式传入，不读取旧帧栈或异常对象。
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RyuPy.Runtime.Tests")]

namespace RyuPy.Runtime;

internal enum TraceReject
{
    None, Owner, Epoch, Request, Unfinished, Grant, Scene, Boundary,
    Receipt, Sequence, Cursor, Permit, Overflow, Mode
}

internal enum TraceChange { Rejected, Applied, NoOp }
internal readonly record struct TraceResult(TraceChange Change, TraceReject Reject)
{
    internal static TraceResult Applied => new(TraceChange.Applied, TraceReject.None);
    internal static TraceResult NoOp => new(TraceChange.NoOp, TraceReject.None);
    internal static TraceResult Fail(TraceReject why) => new(TraceChange.Rejected, why);
}

internal enum TraceBeginKind { Explicit, NativeFresh, DispatchResume }
internal enum TraceReceiveKind { Forward, Handle, Suppress, Terminal }
internal enum TraceInclusion { Exclusive, Inclusive }

// activation 不按名称、文件、行号或深度去重；同一 scene 中必须唯一。
internal readonly record struct TraceFrame(long Activation, string File, string Function, int Line);
internal readonly record struct TraceRequest(PyTraceState Owner, TraceEpoch Epoch, long Number);
internal readonly record struct TraceBoundary(TraceEpoch Epoch, int Position, long Activation)
{
    internal static TraceBoundary Root(TraceEpoch epoch) => new(epoch, -1, 0);
}
internal readonly record struct TraceReceive(TraceRequest Request, long Number,
    TraceBoundary Boundary, TraceInclusion Inclusion, TraceReceiveKind Kind);
internal sealed record TraceReceipt(TraceReceive Event, long ResultVersion);

// 快照只有不可变值和只读容器。数组从不暴露，也不被接受后的事务再次修改。
internal sealed class TraceSnapshot
{
    internal PyTraceState Owner { get; }
    internal TraceEpoch Epoch { get; }
    internal ReadOnlyCollection<TraceFrame> History { get; }
    internal ReadOnlyCollection<TraceFrame> Scene { get; }
    internal TraceRequest? Request { get; }
    internal TraceBeginKind? BeginKind { get; }
    internal int? PendingCount { get; }
    internal bool GrantUsed { get; }
    internal bool GrantValid { get; }
    internal long Version { get; }
    internal TraceReceipt? Receipt { get; }
    internal long NextReceive { get; }
    // 暂停的动态分派属于同一个 owner 快照；外部 token 无权保存/恢复历史。
    internal TraceDispatchPark? Park { get; }
    // all-miss 重开外段的来源；只用于识别已由 Park 提交的同 activation 分派边界。
    internal TraceDispatchPark? Continuation { get; }

    internal TraceSnapshot(PyTraceState owner, TraceEpoch epoch,
        ReadOnlyCollection<TraceFrame> history, ReadOnlyCollection<TraceFrame> scene,
        TraceRequest? request, TraceBeginKind? beginKind, int? pendingCount,
        bool grantUsed, bool grantValid, long version, TraceReceipt? receipt, long nextReceive,
        TraceDispatchPark? park = null, TraceDispatchPark? continuation = null)
    {
        Owner = owner; Epoch = epoch; History = history; Scene = scene; Request = request;
        BeginKind = beginKind; PendingCount = pendingCount; GrantUsed = grantUsed;
        GrantValid = grantValid; Version = version; Receipt = receipt; NextReceive = nextReceive;
        Park = park;
        Continuation = continuation;
    }
}

// token 只保存已暂停 flight 的外段与真实接收身份；消费位只在 owner.Park。
// 不保存旧 History，因此同对象在 selector 中的新 raise 不会被恢复旧历史覆盖。
internal sealed record TraceDispatchPark(TraceRequest Lineage, TraceBoundary Boundary,
    ReadOnlyCollection<TraceFrame> Scene, int Remaining);

// grant 只有引用身份；有效性和消费位只存在对应 owner 的一次发布状态中。
internal sealed class TraceBirthGrant
{
    private TraceBirthGrant() { }
    internal static TraceBirthGrant Create() => new();
}
