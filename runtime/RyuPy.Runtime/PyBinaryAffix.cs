using System;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>
/// bytes/bytearray的startswith/endswith：位置边界先转换、tuple候选按序短路、按原始字节匹配。
/// 不解码文本；仅memoryview前缀经现有buffer接口复制一次，其余内建buffer不复制。
/// </summary>
public static class PyBinaryAffix
{
    /// <summary>接受prefix以及可选start/end，共1至3个位置参数；关键字由调用分派统一拒绝。</summary>
    public static bool Match(object receiver, string method, object?[] args)
    {
        if (args.Length is < 1 or > 3) throw new PyTypeError($"{method}() takes from 1 to 3 arguments ({args.Length} given)");
        int length = Length(receiver);
        long epoch = receiver is PyByteArray mutable ? mutable.ResizeEpoch : 0;
        // 固定CPython在边界协议调用前捕获长度，但同长用户写入在比较时可见。
        long start = args.Length > 1 ? Index(args[1], 0) : 0;
        long end = args.Length > 2 ? Index(args[2], long.MaxValue) : long.MaxValue;
        if (receiver is PyByteArray array && array.ResizeEpoch != epoch)
            throw new PyNotImplementedError("resizing bytearray during startswith/endswith index conversion is not supported");
        start = start < 0 ? Math.Max(0, start + length) : start;
        end = end < 0 ? Math.Max(0, end + length) : Math.Min(length, end);
        if (args[0] is PyTuple tuple)
        {
            foreach (object? candidate in tuple.Items)
                if (Matches(receiver, candidate, start, end, method == "endswith", tupleItem: true, method)) return true;
            return false;
        }
        return Matches(receiver, args[0], start, end, method == "endswith", tupleItem: false, method);
    }

    private static int Length(object value) => value switch
    {
        PyBytes bytes => bytes.Count,
        PyByteArray bytes => bytes.Count,
        _ => throw new PyTypeError("binary affix receiver must be bytes or bytearray"),
    };

    private static long Index(object? value, long fallback)
    {
        if (value is null or PyNone) return fallback;
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("slice indices must be integers or None or have an __index__ method");
            if (value is bool) throw new PyNotImplementedError("bool returned by __index__ in binary affix bounds is not supported");
            if (!PyInt.IsInt(value)) throw new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(value)})");
        }
        if (PyInt.TryI64(value!, out long small)) return small;
        var integer = PyInt.ToBigInteger(value!);
        return integer < long.MinValue ? long.MinValue : integer > long.MaxValue ? long.MaxValue : (long)integer;
    }

    private static bool Matches(object receiver, object? candidate, long start, long end, bool suffix, bool tupleItem, string method)
    {
        // 验证buffer必须早于空范围判断；tuple尚未访问的候选不能提前验证。
        ReadOnlySpan<byte> prefix = candidate switch
        {
            PyBytes bytes => bytes.Span,
            PyByteArray bytes => CollectionsMarshal.AsSpan(bytes.Bytes),
            PyMemoryView view => view.CopyBuffer(),
            _ => throw new PyTypeError(tupleItem
                ? $"a bytes-like object is required, not '{PyRepr.TypeName(candidate)}'"
                : $"{method} first arg must be bytes or a tuple of bytes, not {PyRepr.TypeName(candidate)}"),
        };
        if (start > end || prefix.Length > end - start) return false;
        int offset = (int)(suffix ? end - prefix.Length : start);
        ReadOnlySpan<byte> source = receiver is PyBytes immutable
            ? immutable.Span : CollectionsMarshal.AsSpan(((PyByteArray)receiver).Bytes);
        return source.Slice(offset, prefix.Length).SequenceEqual(prefix);
    }
}
