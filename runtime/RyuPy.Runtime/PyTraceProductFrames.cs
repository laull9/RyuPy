// 产品 V1 的真实 activation 采样；仅 Push 分配身份，Retry/Pop/Reset 均不分配或轮换 epoch。
using System;

namespace RyuPy.Runtime;

public static partial class PyTraceback
{
    private static long[] _productActivations = new long[InitialFrameCapacity];

    private static void ProductPush(int slot)
    {
        if (PyTraceProduct.Active is not { } scope) return;
        if (slot == _productActivations.Length)
            Array.Resize(ref _productActivations, checked(slot * 2));
        _productActivations[slot] = checked(++scope.Activation);
    }

    internal static TraceFrame[] ProductScene()
    {
        if (!PyTraceProduct.Enabled) throw new InvalidOperationException("trace V1 has no execution domain");
        var scene = new TraceFrame[_depth];
        for (int i = 0; i < _depth; i++)
            scene[i] = new(_productActivations[i], string.IsNullOrEmpty(_files[i]) ? _file : _files[i],
                _functions[i], _lines[i]);
        return scene;
    }

    internal static TraceBoundary ProductBoundary(TraceEpoch epoch, long depth)
    {
        if (depth < 1 || depth > _depth)
            throw new InvalidOperationException("trace V1 receiver is no longer live");
        int slot = checked((int)depth - 1);
        return new(epoch, slot, _productActivations[slot]);
    }
}
