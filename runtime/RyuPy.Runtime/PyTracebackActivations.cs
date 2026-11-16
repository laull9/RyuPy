// traceback冷视图的激活身份：普通帧按压栈/弹栈清槽，生成器在多次恢复间保留同一frame。
using System;

namespace RyuPy.Runtime;

public static partial class PyTraceback
{
    private static PyNativeFrame?[]? _nativeFrames;
    // 只有生成器写非null；固定并行数组不产生逐调用对象分配。
    private static PyGenerator?[] _nativeGenerators = new PyGenerator?[InitialFrameCapacity];

    private static void NativePush(int slot)
    {
        if (_nativeGenerators.Length < _functions.Length)
            Array.Resize(ref _nativeGenerators, _functions.Length);
        _nativeGenerators[slot] = null;
        if (_nativeFrames is null) return;
        if (_nativeFrames.Length < _functions.Length)
            Array.Resize(ref _nativeFrames, _functions.Length);
        _nativeFrames[slot] = null;
    }
    private static void NativePop(int start, int count)
    {
        Array.Clear(_nativeGenerators, start, count);
        if (_nativeFrames is not null) Array.Clear(_nativeFrames, start, count);
    }
    private static void NativeLine(int slot, int line)
    {
        if (_nativeFrames is not null && _nativeFrames[slot] is { } frame) frame.Line = line;
    }
    internal static long PushGeneratorFrame(string file, string name, object? origin, PyGenerator generator)
    {
        long depth = PushFrameCore(file, name, origin);
        int slot = (int)depth - 1;
        _nativeGenerators[slot] = generator;
        if (generator.NativeFrame is { } frame)
        {
            _nativeFrames ??= new PyNativeFrame?[_functions.Length];
            _nativeFrames[slot] = frame;
        }
        return depth;
    }
    private static PyNativeFrame NativeFrameAt(int slot)
    {
        _nativeFrames ??= new PyNativeFrame?[_functions.Length];
        if (_nativeFrames[slot] is { } current) return current;
        PyGenerator? generator = _nativeGenerators[slot];
        var frame = generator?.NativeFrame
            ?? new PyNativeFrame(_files[slot], _functions[slot], _lines[slot], _origins[slot]);
        _nativeFrames[slot] = frame;
        if (generator is not null) generator.NativeFrame = frame;
        return frame;
    }
    internal static PyNativeFrame? CurrentNativeFrame() => _depth == 0 ? null : NativeFrameAt(_depth - 1);

    /// <summary>在真实catch弹帧前接收，链头只包含接收者至当前失败点的真实激活。</summary>
    public static void ReceiveException(object exception, long receiverDepth)
    {
        if (exception is not PyBaseException error || error.TraceProductBinding is not null) return;
        if (receiverDepth < 1 || receiverDepth > _depth) return;
        var state = error.NativeTraceback ??= new NativeTracebackState();
        state.BeginReceive();
        // 先内后外prepend，得到Python要求的外层到内层链；来源不是全栈快照。
        for (int slot = _depth - 1; slot >= (int)receiverDepth - 1; slot--)
        {
            var frame = NativeFrameAt(slot);
            if (state.Seen.Add(frame)) state.Head = new(frame, _lines[slot], state.Head);
        }
    }
    internal static void ReceiveGeneratorException(object frame, object exception)
    {
        for (int slot = _depth - 1; slot >= 0; slot--)
            if (_nativeGenerators[slot] is { } generator && generator.OwnsStorage(frame))
            { ReceiveException(exception, slot + 1L); return; }
        throw new PyRuntimeError("generator traceback receiver has no live activation");
    }
}
