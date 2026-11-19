// traceback冷视图的激活身份：普通帧按压栈/弹栈清槽，生成器在多次恢复间保留同一frame。
using System;

namespace RyuPy.Runtime;

public static partial class PyTraceback
{
    private static PyNativeFrame?[]? _nativeFrames;
    // 只有生成器写非null；固定并行数组不产生逐调用对象分配。
    private static PyGenerator?[]? _nativeGenerators;

    // Pop/Reset已经清除退出槽；正常Push不重复清槽，容量只随原帧表冷扩容。
    private static void NativeGrow(int capacity)
    {
        if (_nativeGenerators is not null) Array.Resize(ref _nativeGenerators, capacity);
        if (_nativeFrames is not null) Array.Resize(ref _nativeFrames, capacity);
    }
    private static void NativePop(int start, int count)
    {
        if (count == 1)
        {
            // generator每轮恢复通常只退出自身一槽，避免通用Array.Clear调用。
            if (_nativeGenerators is not null) _nativeGenerators[start] = null;
            if (_nativeFrames is not null && _nativeFrames[start] is { } frame)
            { frame.Line = _lines[start]; _nativeFrames[start] = null; }
            return;
        }
        if (_nativeGenerators is not null) Array.Clear(_nativeGenerators, start, count);
        if (_nativeFrames is not null)
        {
            for (int slot = start; slot < start + count; slot++)
                if (_nativeFrames[slot] is { } frame) frame.Line = _lines[slot];
            Array.Clear(_nativeFrames, start, count);
        }
    }
    internal static int NativeFrameLine(PyNativeFrame frame)
    {
        // f_lineno是冷观察：活帧读真实行号，结束/暂停帧读弹栈时保存的末行。
        if (_nativeFrames is not null)
            for (int slot = _depth - 1; slot >= 0; slot--)
                if (ReferenceEquals(_nativeFrames[slot], frame)) return _lines[slot];
        return frame.Line;
    }
    internal static void BindGeneratorFrame(long depth, PyGenerator generator)
    {
        if (depth < 1 || depth != _depth) throw new PyRuntimeError("generator has no current frame");
        int slot = (int)depth - 1;
        _nativeGenerators ??= new PyGenerator?[_functions.Length];
        _nativeGenerators[slot] = generator;
        if (generator.NativeFrame is { } frame)
        {
            _nativeFrames ??= new PyNativeFrame?[_functions.Length];
            _nativeFrames[slot] = frame;
        }
    }
    private static PyNativeFrame NativeFrameAt(int slot)
    {
        _nativeFrames ??= new PyNativeFrame?[_functions.Length];
        if (_nativeFrames[slot] is { } current) { current.Line = _lines[slot]; return current; }
        PyGenerator? generator = _nativeGenerators?[slot];
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
        if (exception is not PyBaseException error) return;
        // 复用既有legacy Attach边界：V1内明确拒绝，且不越域读任何owner字段。
        Attach(error);
        if (receiverDepth < 1 || receiverDepth > _depth) return;
        var state = error.NativeTraceback ??= new NativeTracebackState();
        state.BeginReceive();
        // 先内后外prepend，得到Python要求的外层到内层链；来源不是全栈快照。
        for (int slot = _depth - 1; slot >= (int)receiverDepth - 1; slot--)
        {
            var frame = NativeFrameAt(slot);
            if (state.MarkSeen(frame)) state.Head = new(frame, _lines[slot], state.Head);
        }
    }
    internal static void ReceiveGeneratorException(object frame, object exception)
    {
        for (int slot = _depth - 1; slot >= 0; slot--)
            if (_nativeGenerators?[slot] is { } generator && generator.OwnsStorage(frame))
            { ReceiveException(exception, slot + 1L); return; }
        throw new PyRuntimeError("generator traceback receiver has no live activation");
    }
}
