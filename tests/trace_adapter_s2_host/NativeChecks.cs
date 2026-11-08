// 两个冻结native纵切的精确事件合同；手工adapter反例在独立runtime测试中运行。
using RyuPy.Runtime;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class NativeChecks
{
    internal static bool IsNative(string expected)
        => Path.GetFileName(expected) is "native_local.expected.json" or "native_outer.expected.json";

    // PE静态存在性与下面实际事件身份联合验证，不把方法名存在当成已执行。
    internal static void InspectRuntime(string path, bool probe)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        foreach (var name in new[] { "CreateLocalsPositionalError", "CaptureNativeCaller", "ForwardNative" })
        {
            var methods = metadata.MethodDefinitions
                .Where(h => metadata.GetString(metadata.GetMethodDefinition(h).Name) == name).ToArray();
            if (methods.Length != (probe ? 1 : 0))
                throw new InvalidOperationException("native方法隔离不符: " + name);
            if (probe && pe.GetMethodBody(metadata.GetMethodDefinition(methods[0]).RelativeVirtualAddress).GetILBytes() is not { Length: > 0 })
                throw new InvalidOperationException("native真实方法体缺失: " + name);
        }
    }

    internal static void Check(TraceAdapterScope scope)
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
    }
}
