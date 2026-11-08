// 两个冻结native纵切的精确事件合同；手工adapter反例在独立runtime测试中运行。
using RyuPy.Runtime;

internal static class NativeChecks
{
    internal static bool IsNative(string name)
        => name is "native_local" or "native_outer";

    internal static void Check(TraceAdapterScope scope, string name)
    {
        var events = scope.Events;
        if (events.Count != 3 || events[0].Kind != "NativeFresh"
            || events[1].Kind != "Forward" || events[2].Kind != "Handle")
            throw new InvalidOperationException("native必须精确Source/Forward/Handle三个真实事件");
        var error = scope.LastHandled ?? throw new InvalidOperationException("缺少native接收对象");
        var source = events[0];
        var forward = events[1];
        var handle = events[2];
        var forwarded = forward.After.Receipt ?? throw new InvalidOperationException("缺少Forward收据");
        var handled = handle.After.Receipt ?? throw new InvalidOperationException("缺少Handle收据");
        foreach (var item in events)
        {
            if (!ReferenceEquals(item.Error, error) || !ReferenceEquals(item.After.Owner, source.After.Owner)
                || item.After.Request != source.After.Request || item.After.Request?.Number != 1)
                throw new InvalidOperationException("native对象/owner/Q身份改变");
        }
        if (source.LegacyFrameCount != 0 || source.LegacyContext is not null
            || forward.LegacyFrameCount != 0 || forward.LegacyContext is not null
            || handle.LegacyFrameCount is not > 0 || handle.LegacyContext is not null || error.Context is not null)
            throw new InvalidOperationException("native Forward必须先于旧Attach/Prepare，最终Context应为空");
        if (forward.After.History.Count != 0 || forward.Before.PendingCount != forward.After.PendingCount
            || forwarded.Event.Number != 1 || handled.Event.Number != 2
            || forwarded.Event.Inclusion != TraceInclusion.Exclusive
            || handled.Event.Inclusion != TraceInclusion.Inclusive
            || handle.After.PendingCount is not null)
            throw new InvalidOperationException("native空Forward或最终Handle边界错误");
        if (forward.LiveDepth != forwarded.Event.Boundary.Position + 1L
            || handle.LiveDepth != source.After.Scene.Count)
            throw new InvalidOperationException("native接收丢失尚活callee或使用了错误caller");
        var owner = source.After.Owner;
        var epoch = source.After.Epoch;
        if (!ReferenceEquals(error.TraceAdapterBinding!.Scope, scope.Seal)
            || !ReferenceEquals(error.TraceAdapterBinding.Owner, owner)
            || !ReferenceEquals(source.After, forward.Before) || !ReferenceEquals(forward.After, handle.Before)
            || !ReferenceEquals(owner.Snapshot, handle.After))
            throw new InvalidOperationException("native binding/snapshot链身份错误");
        foreach (var item in events)
        {
            var request = item.After.Request ?? throw new InvalidOperationException("native缺Q");
            if (!ReferenceEquals(item.Before.Owner, owner) || !ReferenceEquals(item.After.Owner, owner)
                || !ReferenceEquals(item.Before.Epoch, epoch) || !ReferenceEquals(item.After.Epoch, epoch)
                || !ReferenceEquals(request.Owner, owner) || !ReferenceEquals(request.Epoch, epoch)
                || !item.After.GrantUsed || item.After.GrantValid)
                throw new InvalidOperationException("native Owner/Epoch/grant错误");
        }
        bool local = name == "native_local";
        int depth = local ? 2 : 3;
        if (source.Before.GrantUsed || !source.Before.GrantValid || source.Before.PendingCount is not null
            || source.Before.Scene.Count != 0 || source.After.History.Count != 0
            || source.LiveDepth != depth || handle.LegacyFrameCount != depth
            || forwarded.Event.Kind != TraceReceiveKind.Forward || handled.Event.Kind != TraceReceiveKind.Handle)
            throw new InvalidOperationException("native冷源/grant/接收kind错误");
        var scene = source.After.Scene;
        if (scene.Count != depth || scene[0].Function != "<module>" || scene[0].Line != (local ? 9 : 12)
            || scene[1].Function != (local ? "native_local" : "native_outer") || scene[1].Line != (local ? 4 : 7)
            || (!local && (scene[2].Function != "native_callee" || scene[2].Line != 3))
            || !scene.SequenceEqual(forward.After.Scene) || !scene.SequenceEqual(handle.After.Scene))
            throw new InvalidOperationException("native精确scene发生变化");
        if (source.After.Scene.Count != depth || source.After.PendingCount != depth
            || source.After.Scene.Where((f, i) => f.Activation != i + 1L).Any()
            || forwarded.Event.Boundary.Position != depth - 1 || forwarded.Event.Boundary.Activation != depth
            || handled.Event.Boundary.Position != 1 || handled.Event.Boundary.Activation != 2
            || !ReferenceEquals(forwarded.Event.Boundary.Epoch, epoch)
            || !ReferenceEquals(handled.Event.Boundary.Epoch, epoch))
            throw new InvalidOperationException("native精确scene/caller/receiver错误");
        var history = handle.After.History;
        if (history.Count != (local ? 1 : 2)
            || history[0].Function != (local ? "native_local" : "native_outer")
            || history[0].Line != (local ? 4 : 7) || history[0].Activation != 2
            || (!local && (history[1].Function != "native_callee" || history[1].Line != 3 || history[1].Activation != 3)))
            throw new InvalidOperationException("native精确最终history错误");
    }
}
