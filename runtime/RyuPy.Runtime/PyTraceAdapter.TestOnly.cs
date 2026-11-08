// S2a隔离适配：仅测试产物包含源/接收入口；scene完全来自真实生成代码维护的帧栈。
#if TRACE_ADAPTER_TEST
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TraceAdapterHost")]
namespace RyuPy.Runtime;

internal sealed record TraceAdapterBinding(object Scope, PyTraceState Owner);
internal sealed record TraceAdapterEvent(string Kind, PyBaseException Error,
    TraceSnapshot Before, TraceSnapshot After, long LiveDepth);

internal sealed class TraceAdapterScope : IDisposable
{
    internal readonly object Seal = new();
    internal readonly PyTraceContextAuthority Authority = new();
    internal readonly List<TraceAdapterEvent> Events = new();
    internal PyBaseException? LastHandled;
    private long nextActivation;
    internal long NextActivation() => checked(++nextActivation);
    public void Dispose() => PyTraceAdapter.Close(this);
}

/// <summary>仅隔离probe产物存在的真实源/接收桥；生产程序集没有此类型。</summary>
public static class PyTraceAdapter
{
    internal static TraceAdapterScope? Active;
    internal static TraceAdapterScope RequireScope() => Active
        ?? throw new InvalidOperationException("S2a requires an active isolated scope");

    /// <summary>宿主建立单次测试执行；不支持嵌套scope或隐式Rotate。</summary>
    public static IDisposable EnterScope()
    {
        if (Active is not null || PyTraceback.CaptureDepth() != 0)
            throw new InvalidOperationException("S2a scope must start at an empty root");
        Active = new TraceAdapterScope();
        return Active;
    }

    internal static void Close(TraceAdapterScope scope)
    {
        if (!ReferenceEquals(Active, scope)) throw new InvalidOperationException("S2a scope mismatch");
        // 只查本scope已发生的事件，不建全局对象registry。异常仍保留自身binding。
        bool unfinished = false;
        foreach (var item in scope.Events)
            unfinished |= item.Error.TraceAdapterBinding?.Owner.Snapshot.PendingCount is not null;
        Active = null;
        PyTraceback.Reset();
        if (unfinished) throw new InvalidOperationException("S2a scope closed with unfinished flights");
    }

    /// <summary>源显式raise独占入口；归一化后、旧Attach之前拒绝历史混用。</summary>
    public static object PrepareExplicit(object value)
    {
        var scope = RequireScope();
        if (PyTraceback.CaptureDepth() == 0)
            throw new InvalidOperationException("S2a explicit source requires a live Python activation");
        PyBaseException error = PyErrors.NormalizeRaise(value);
        var binding = error.TraceAdapterBinding;
        if (binding is not null && !ReferenceEquals(binding.Scope, scope.Seal))
            throw new InvalidOperationException("S2a does not rotate cross-scope owners");
        if (binding is null && error.Frames.Length != 0)
            throw new InvalidOperationException("S2a does not import legacy traceback history");
        var candidate = binding ?? new TraceAdapterBinding(scope.Seal, new PyTraceState(scope.Authority));
        var owner = candidate.Owner;
        var before = owner.Snapshot;
        long number = checked((before.Request?.Number ?? 0) + 1);
        var result = owner.BeginExplicit(new(owner, before.Epoch, number), PyTraceback.AdapterScene());
        RequireApplied(result);
        // 只有Begin成功才发布首binding。空Frames从未用于授予NativeFresh资格。
        error.TraceAdapterBinding = candidate;
        scope.Events.Add(new("Explicit", error, before, owner.Snapshot, PyTraceback.CaptureDepth()));
        PyTraceback.Attach(error);
        return PyErrors.Prepare(error);
    }

    /// <summary>真实Python静态handler先提交Inclusive Handle，再由生成代码PopTo。</summary>
    public static void Handle(object error, long depth) => Receive(error, depth, false);

    /// <summary>自动handler cleanup只转发同一Q，绝不重走Source。</summary>
    public static void Forward(object error, long depth) => Receive(error, depth, true);

    private static void Receive(object value, long depth, bool forward)
    {
        var scope = RequireScope();
        if (value is not PyBaseException error || error.TraceAdapterBinding is not { } binding
            || !ReferenceEquals(binding.Scope, scope.Seal))
            throw new InvalidOperationException("S2a receive requires this scope's existing source binding");
        var owner = binding.Owner;
        var before = owner.Snapshot;
        if (before.Request is not { } request || before.PendingCount is null)
            throw new InvalidOperationException("S2a receive requires an unfinished request");
        var boundary = PyTraceback.AdapterBoundary(before.Epoch, depth);
        var result = owner.Receive(new(request, before.NextReceive, boundary,
            forward ? TraceInclusion.Exclusive : TraceInclusion.Inclusive,
            forward ? TraceReceiveKind.Forward : TraceReceiveKind.Handle));
        RequireApplied(result);
        scope.Events.Add(new(forward ? "Forward" : "Handle", error, before,
            owner.Snapshot, PyTraceback.CaptureDepth()));
        if (!forward) scope.LastHandled = error;
    }

    private static void RequireApplied(TraceResult result)
    {
        if (result.Change != TraceChange.Applied)
            throw new InvalidOperationException($"S2a transaction rejected: {result.Change}/{result.Reject}");
    }
}

public static partial class PyTraceback
{
    // 只存在于probe。深度定位槽，activation来自本scope逐次真实Push，不按名称/来源去重。
    private static long[] _adapterActivations = new long[InitialFrameCapacity];
    private static void AdapterPush(int slot)
    {
        var scope = PyTraceAdapter.RequireScope();
        if (slot == _adapterActivations.Length)
            Array.Resize(ref _adapterActivations, checked(slot * 2));
        _adapterActivations[slot] = scope.NextActivation();
    }
    internal static TraceFrame[] AdapterScene()
    {
        PyTraceAdapter.RequireScope();
        var scene = new TraceFrame[_depth];
        for (int i = 0; i < _depth; i++)
            scene[i] = new(_adapterActivations[i], string.IsNullOrEmpty(_files[i]) ? _file : _files[i],
                _functions[i], _lines[i]);
        return scene;
    }
    internal static TraceBoundary AdapterBoundary(TraceEpoch epoch, long depth)
    {
        if (depth < 1 || depth > _depth) throw new InvalidOperationException("S2a receiver is no longer live");
        int slot = checked((int)depth - 1);
        return new(epoch, slot, _adapterActivations[slot]);
    }
}
#endif
