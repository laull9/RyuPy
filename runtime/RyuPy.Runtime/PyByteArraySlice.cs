// bytearray 切片赋值的有限公共路径：保留 CP 的一轮/两轮下标转换与别名快照。
// 连续写只移动一次尾段；扩展空赋值线性压紧，不重建整个目标或逐项 RemoveAt。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

public sealed partial class PyByteArray
{
    /// <summary>用已有 buffer 或精确整数序列替换切片，保留对象身份与导出约束。</summary>
    /// <param name="slice">在使用时转换 step/start/stop 的切片。</param>
    /// <param name="value">bytes、bytearray、连续 B/c memoryview 或精确 list/tuple/range。</param>
    public void SetSlice(PySlice slice, object? value)
    {
        try { SetSliceCore(slice, value); }
        catch (PyBaseException error)
        {
            // 共享 byte 转换、view 读取和导出守卫可能新造错误；只补缺失的现场。
            // PrepareRaise 不覆盖已有用户帧、身份或非空 context，也不替换异常类型。
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    private void SetSliceCore(PySlice slice, object? value)
    {
        var indices = UnpackAssignmentSlice(slice);
        if (value is PyByteArray array && !ReferenceEquals(this, array))
        {
            // 不同 bytearray 在 CP 中不经构造转换；只运行一次下标协议。
            // 全部用户代码已结束，源存储不与目标共享，不需要中间副本。
            WriteAssignmentSlice(indices, CollectionsMarshal.AsSpan(array.Bytes));
            return;
        }

        // CP 先完成首轮下标，再把其它源转换为 bytearray，最后重新调用切片入口。
        // bytes 已不可变，可复用原数组；self/view 必须在第二轮回调前复制。
        byte[] replacement = AssignmentBytes(value);
        indices = UnpackAssignmentSlice(slice);
        WriteAssignmentSlice(indices, replacement);
    }

    private byte[] AssignmentBytes(object? value)
    {
        switch (value)
        {
            case PyBytes bytes: return bytes.Bytes;
            case PyByteArray array: return [.. array.Bytes];
            case PyMemoryView view: return view.CopyBuffer();
            case PyList or PyListInstance or PyTuple or PyRange:
                var result = new List<byte>();
                IPyIterator iterator = PyIter.Create(value);
                // 不提前复制 list 元素；当前元素 __index__ 对后续元素的修改仍可见。
                while (iterator.MoveNext())
                {
                    object? item = iterator.Current();
                    GuardAssignmentIndexDescriptor(item);
                    result.Add(PyEncoding.ToByte(item));
                }
                return result.ToArray();
            case string or PyStringInstance or bool or int or long or short or byte or BigInteger or double:
                throw SliceError(new PyTypeError("can assign only bytes, buffers, or iterables of ints in range(0, 256)"));
            default:
                // 不把合法的通用 iterable 静默当非法，也不复用缺少完整构造合同的 ToBytes。
                throw SliceError(new PyNotImplementedError("bytearray slice assignment supports exact bytes, bytearray, contiguous memoryview, list, tuple and range sources"));
        }
    }

    private static (long Start, long Stop, long Step) UnpackAssignmentSlice(PySlice slice)
    {
        long step = AssignmentIndex(slice.Step, 1);
        if (step == 0) throw SliceError(new PyValueError("slice step cannot be zero"));
        // CP 将最小负 ssize_t 步长夹为 -MAX，后续取负不会溢出。
        if (step == long.MinValue) step = -long.MaxValue;
        long start = AssignmentIndex(slice.Start, step < 0 ? long.MaxValue : 0);
        long stop = AssignmentIndex(slice.Stop, step < 0 ? long.MinValue : long.MaxValue);
        return (start, stop, step);
    }

    private static long AssignmentIndex(object? value, long fallback)
    {
        if (value is null or PyNone) return fallback;
        object? integer = value;
        if (!PyInt.IsInt(integer))
        {
            GuardAssignmentIndexDescriptor(value);
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw SliceError(new PyTypeError("slice indices must be integers or None or have an __index__ method"));
            if (integer is bool)
                throw SliceError(new PyNotImplementedError("bytearray slice: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer))
                throw SliceError(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        if (PyInt.TryI64(integer!, out long small)) return small;
        BigInteger wide = PyInt.ToBigInteger(integer!);
        return wide < long.MinValue ? long.MinValue : wide > long.MaxValue ? long.MaxValue : (long)wide;
    }

    private static void GuardAssignmentIndexDescriptor(object? value)
    {
        if (value is PyInstance instance && instance.Class.TryLookup("__index__", out _, out object? slot)
            && (slot is PyProperty || slot is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _)))
            throw SliceError(new PyNotImplementedError("bytearray slice index descriptors are not supported"));
    }

    private static int AssignmentBound(long value, int length, bool backwards)
    {
        if (value < 0) value += length;
        if (value < 0) return backwards ? -1 : 0;
        if (value >= length) return backwards ? length - 1 : length;
        return (int)value;
    }

    private void WriteAssignmentSlice((long Start, long Stop, long Step) raw, ReadOnlySpan<byte> replacement)
    {
        // 最后一轮三个回调全部结束才读取长度，不能使用第一个回调前的陈旧窗口。
        int previousCount = Count;
        int start = AssignmentBound(raw.Start, previousCount, raw.Step < 0);
        int stop = AssignmentBound(raw.Stop, previousCount, raw.Step < 0);
        int count = raw.Step > 0
            ? (stop > start ? (int)(1 + (stop - (long)start - 1) / raw.Step) : 0)
            : (start > stop ? (int)(1 + (start - (long)stop - 1) / -raw.Step) : 0);
        if (raw.Step == 1)
        {
            WriteContiguousSlice(start, count, replacement);
            return;
        }
        // 空 RHS 表示扩展删除；非空长度不符先报 ValueError，不先检查导出锁。
        if (replacement.Length == 0)
        {
            DeleteExtendedSlice(start, raw.Step, count);
            return;
        }
        if (replacement.Length != count)
            throw SliceError(new PyValueError($"attempt to assign bytes of size {replacement.Length} to extended slice of size {count}"));
        long position = start;
        for (int index = 0; index < count; index++)
        {
            Bytes[(int)position] = replacement[index];
            // 巨大步长只选一个元素时不再做无用加法，checked 构建也不应溢出。
            if (index + 1 < count) position += raw.Step;
        }
    }

    private void WriteContiguousSlice(int start, int count, ReadOnlySpan<byte> replacement)
    {
        int previousCount = Count;
        int delta = replacement.Length - count;
        EnsureCanResize(delta);
        long requestedCount = (long)previousCount + delta;
        if (requestedCount > Array.MaxLength)
            throw SliceError(new PyNotImplementedError("bytearray slice result exceeds the CLR buffer capacity"));
        int newCount = (int)requestedCount;
        // 任何扩容失败都发生在改变目标内容前；之后只有已核边界的无回调 span 操作。
        if (delta > 0)
        {
            Bytes.EnsureCapacity(newCount);
            CollectionsMarshal.SetCount(Bytes, newCount);
        }
        Span<byte> content = CollectionsMarshal.AsSpan(Bytes);
        content.Slice(start + count, previousCount - start - count)
            .CopyTo(content.Slice(start + replacement.Length));
        replacement.CopyTo(content.Slice(start, replacement.Length));
        if (delta < 0) CollectionsMarshal.SetCount(Bytes, newCount);
        RecordResize(previousCount);
    }

    private void DeleteExtendedSlice(int start, long step, int count)
    {
        if (count == 0) return;
        EnsureCanResize(-count);
        int previousCount = Count;
        long selected = step > 0 ? start : start + (count - 1L) * step;
        long stride = step > 0 ? step : -step;
        int write = (int)selected;
        int remaining = count;
        Span<byte> content = CollectionsMarshal.AsSpan(Bytes);
        for (int read = write; read < previousCount; read++)
        {
            if (remaining > 0 && read == selected)
            {
                remaining--;
                if (remaining != 0) selected += stride;
            }
            else content[write++] = content[read];
        }
        CollectionsMarshal.SetCount(Bytes, previousCount - count);
        RecordResize(previousCount);
    }

    private static T SliceError<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
