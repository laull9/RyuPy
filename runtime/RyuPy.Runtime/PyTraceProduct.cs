// 受控产品 V1 的唯一 trace 适配。只由通过整产物准入的真实 Main 开启，旧产品默认不进入。
// owner 持有 H/Q/Park/receipt；此层只绑定真实异常、执行域与 Push activation，不另写历史。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed record TraceProductBinding(TraceProductScope Scope, PyTraceState Owner);

internal sealed class TraceProductScope
{
    internal readonly PyTraceContextAuthority Authority = new();
    internal readonly Stack<long> Generators = new();
    internal long Activation;
    internal long Flights;
    internal long Parks;
}

/// <summary>受控生成器产品的版本化源码、分派与接收 ABI；不支持外部宿主或并发执行。</summary>
public static class PyTraceProduct
{
    internal static TraceProductScope? Active { get; private set; }
    internal static bool Enabled => Active is not null;
    private static TraceProductScope Scope => Active
        ?? throw new InvalidOperationException("trace V1 requires an admitted execution domain");

    /// <summary>只接受与编译产物一致的 ABI；发生在系统初始化后、用户定义初始化前。</summary>
    public static void Enter(long abi)
    {
        if (abi != 1 || Active is not null || PyTraceback.CaptureDepth() != 0
            || !PySystem.UsesDefaultOutputStreams || PyAtexit.HasCallbacks)
            throw new InvalidOperationException("unsupported trace V1 ABI or execution boundary");
#if TRACE_ADAPTER_TEST
        throw new InvalidOperationException("trace V1 cannot execute with the S2 probe runtime");
#else
        Active = new TraceProductScope();
#endif
    }

    /// <summary>在真实退出刷新完成后关闭；Reset 只清物理栈，不改变本域/epoch。</summary>
    public static void Exit()
    {
        var scope = Scope;
        if (scope.Flights != 0 || scope.Parks != 0 || scope.Generators.Count != 0)
            throw new InvalidOperationException("trace V1 exited with unfinished propagation");
        Active = null;
    }

    /// <summary>唯一源码显式 raise 入口；归一化成功后才开始本次传播。</summary>
    public static object Explicit(object value)
    {
        var error = PyErrors.NormalizeRaise(value);
        var binding = Bind(error);
        var before = binding.Owner.Snapshot;
        Apply(binding, before, binding.Owner.BeginExplicit(
            NextRequest(binding.Owner), PyTraceback.ProductScene()));
        error.TraceProductBinding = binding;
        return PyErrors.Prepare(error);
    }

    // 只有具体“新分配后立即抛出”的审核 producer 调用；catch 与空 H 不授此权限。
    internal static T NativeFresh<T>(T error) where T : PyBaseException
    {
        if (!Enabled) return error;
        if (error.TraceProductBinding is not null)
            throw new InvalidOperationException("native producer reused an existing exception");
        var binding = Bind(error);
        var before = binding.Owner.Snapshot;
        Apply(binding, before, binding.Owner.BeginNativeFresh(NextRequest(binding.Owner),
            binding.Owner.BirthGrant, PyTraceback.ProductScene()));
        error.TraceProductBinding = binding;
        PyErrors.Prepare(error);
        return error;
    }

    /// <summary>动态 selector 求值前提交本帧及内段，暂存原外段；token 不携带旧 H。</summary>
    public static object Park(object value, long depth)
    {
        var binding = Existing(value);
        var before = binding.Owner.Snapshot;
        var boundary = PyTraceback.ProductBoundary(before.Epoch, depth);
        var result = binding.Owner.DispatchPark(boundary, out var token);
        Apply(binding, before, result);
        return token!;
    }

    /// <summary>匹配成功消费旧分派；不能结束同异常对象的新 flight。</summary>
    public static void Match(object token) => Consume(token, false);

    /// <summary>selector 失败消费原分派；替代异常保留它自己的 owner/Q。</summary>
    public static void Abort(object token)
    {
        if (token is not PyNone) Consume(token, false);
    }

    /// <summary>all-miss 恢复原外段和当前 H；绝不调用 Explicit。</summary>
    public static void AllMiss(object token) => Consume(token, true);

    private static void Consume(object value, bool resume)
    {
        if (value is not TraceDispatchPark token
            || !ReferenceEquals(token.Lineage.Epoch.Authority, Scope.Authority))
            throw new InvalidOperationException("trace V1 dispatch token belongs to another domain");
        var owner = token.Lineage.Owner;
        var before = owner.Snapshot;
        Apply(new(Scope, owner), before,
            resume ? owner.ResumePark(token) : owner.ConsumePark(token));
    }

    /// <summary>静态 handler 在弹栈前接收包含本帧的传播段。</summary>
    public static void Handle(object value, long depth)
        => Receive(value, depth, TraceReceiveKind.Handle);

    /// <summary>透明清理在弹栈前提交内段，不开始新 source。</summary>
    public static void Forward(object value, long depth)
        => Receive(value, depth, TraceReceiveKind.Forward);

    /// <summary>真实恢复函数的物理 catch；Retry 始终留在同一次生成器 activation。</summary>
    public static void GeneratorCatch(object value)
        => Forward(value, Scope.Generators.Peek());

    /// <summary>每次恢复重新绑定本次真实 activation 深度，不复用上一次暂停的深度槽。</summary>
    public static long GeneratorDepth() => Scope.Generators.Peek();

    internal static void EnterGenerator(long depth) => Scope.Generators.Push(depth);
    internal static void LeaveGenerator(long depth)
    {
        if (Scope.Generators.Count == 0 || Scope.Generators.Pop() != depth)
            throw new InvalidOperationException("trace V1 generator activation mismatch");
    }

    internal static void EscapeGenerator(object value, long depth) => Forward(value, depth - 1);
    internal static void Terminal(PyBaseException error)
    {
        if (Enabled) Receive(error, 0, TraceReceiveKind.Terminal);
    }

    // 报告/退出刷新会吞掉错误；每个实际 Python 错误都必须结束，不仅保留的首个错误。
    internal static void Suppress(Exception error)
    {
        if (Enabled && error is PyBaseException python)
            Receive(python, 0, TraceReceiveKind.Suppress);
    }

    private static void Receive(object value, long depth, TraceReceiveKind kind)
    {
        var binding = Existing(value);
        var owner = binding.Owner;
        var before = owner.Snapshot;
        if (before.Request is not { } request || before.PendingCount is not int pending)
            throw new InvalidOperationException("trace V1 receive has no active propagation");
        var boundary = depth == 0 ? TraceBoundary.Root(before.Epoch)
            : PyTraceback.ProductBoundary(before.Epoch, depth);
        // 深度仅定位，旧scene必须包含同一真实activation；不能按pending<=depth吞掉非法接收。
        if (depth != 0 && (boundary.Position >= before.Scene.Count
            || before.Scene[boundary.Position].Activation != boundary.Activation))
            throw new InvalidOperationException("trace V1 receiving activation expired");
        if (kind == TraceReceiveKind.Forward)
        {
            // all-miss 的本帧已在 Park 提交；只有该 Q 的明确 continuation 能透明越过此边界。
            if (before.BeginKind == TraceBeginKind.DispatchResume && before.Continuation is { } continuation
                && continuation.Boundary == boundary && pending == continuation.Remaining) return;
            if (before.Receipt is { } receipt && receipt.Event.Request == request
                && receipt.Event.Boundary == boundary && receipt.Event.Kind == TraceReceiveKind.Forward
                && receipt.Event.Inclusion == TraceInclusion.Exclusive && pending == boundary.Position + 1)
            {
                var repeated = owner.Receive(receipt.Event);
                if (repeated.Change != TraceChange.NoOp)
                    throw new InvalidOperationException("trace V1 repeated receive lost its receipt");
                return;
            }
            // 首次空内段仍走 Receive 并得到真实收据，不将它与重复事件混同。
        }
        Apply(binding, before, owner.Receive(new(request, before.NextReceive, boundary,
            kind == TraceReceiveKind.Handle ? TraceInclusion.Inclusive : TraceInclusion.Exclusive, kind)));
    }

    private static TraceRequest NextRequest(PyTraceState owner)
        => new(owner, owner.Snapshot.Epoch, checked((owner.Snapshot.Request?.Number ?? 0) + 1));

    private static TraceProductBinding Bind(PyBaseException error)
    {
        if (error.TraceProductBinding is not null) return Existing(error);
        if (error.Frames.Length != 0)
            throw new InvalidOperationException("trace V1 does not import legacy history");
        return new TraceProductBinding(Scope, new PyTraceState(Scope.Authority));
    }

    private static TraceProductBinding Existing(object value)
    {
        if (value is not PyBaseException error || error.TraceProductBinding is not { } binding
            || !ReferenceEquals(binding.Scope, Scope))
            throw new InvalidOperationException("trace V1 requires this domain's existing source");
        return binding;
    }

    private static void Apply(TraceProductBinding binding, TraceSnapshot before, TraceResult result)
    {
        if (result.Change != TraceChange.Applied)
            throw new InvalidOperationException($"trace V1 transaction rejected: {result.Reject}");
        var after = binding.Owner.Snapshot;
        binding.Scope.Flights += (after.PendingCount.HasValue ? 1 : 0) - (before.PendingCount.HasValue ? 1 : 0);
        binding.Scope.Parks += (after.Park is not null ? 1 : 0) - (before.Park is not null ? 1 : 0);
    }
}
