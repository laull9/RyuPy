// 二进制序列：bytes（不可变）、bytearray（可变）与编码辅助 PyEncoding。
// 元素按 Python 规则是 0–255 的 int；下标、切片、repr 与 CPython 一致。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>Python <c>bytes</c>：不可变字节序列。</summary>
public sealed class PyBytes
{
    /// <summary>字节内容。</summary>
    public byte[] Bytes { get; }

    /// <summary>用字节数组构造。</summary>
    /// <param name="bytes">字节。</param>
    public PyBytes(byte[] bytes) => Bytes = bytes;

    /// <summary>只读视图。</summary>
    public ReadOnlySpan<byte> Span => Bytes;

    /// <summary>长度。</summary>
    public int Count => Bytes.Length;

    /// <summary>按下标取字节（返回 <c>int</c>），支持负数。</summary>
    /// <param name="index">下标。</param>
    public object GetItem(long index)
    {
        long normalized = index < 0 ? index + Bytes.Length : index;
        if (normalized < 0 || normalized >= Bytes.Length)
        {
            throw new PyIndexError("index out of range");
        }
        return (long)Bytes[normalized];
    }

    /// <summary>取切片。</summary>
    /// <param name="slice">切片。</param>
    public PyBytes GetSlice(PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Count);
        var result = new byte[count];
        long index = start;
        for (long i = 0; i < count; i++, index += step)
        {
            result[i] = Bytes[index];
        }
        return new PyBytes(result);
    }

    /// <summary>按 UTF-8 解码（默认编码）。</summary>
    /// <param name="encoding">编码名。</param>
    public string Decode(string encoding) => PyEncoding.Decode(Bytes, encoding);
}

/// <summary>Python <c>bytearray</c>：可变字节序列。</summary>
public sealed partial class PyByteArray
{
    /// <summary>字节内容。</summary>
    public List<byte> Bytes { get; }

    private int _exports;
    private long _resizeEpoch;
    /// <summary>成功改变长度的结构版本，识别用户回调中resize后恢复原长度的情况。</summary>
    internal long ResizeEpoch => _resizeEpoch;
    /// <summary>仅在实际写入成功后记录；同长写、空操作和失败检查不改变结构版本。</summary>
    internal void RecordResize(int previousCount)
    {
        if (Bytes.Count != previousCount) _resizeEpoch = unchecked(_resizeEpoch + 1);
    }
    /// <summary>每个独立memoryview持一个租约；最终回收可能与执行线程并发。</summary>
    internal void AcquireExport() => Interlocked.Increment(ref _exports);
    /// <summary>只由view的幂等release释放租约，不调用用户代码。</summary>
    internal void ReleaseExport() => Interlocked.Decrement(ref _exports);
    /// <summary>完成参数转换之后检查实际长度变化；空操作不被活动导出阻止。</summary>
    internal void EnsureCanResize(int delta)
    {
        if (delta != 0 && Volatile.Read(ref _exports) != 0)
            throw new PyBufferError("Existing exports of data: object cannot be re-sized");
    }


    /// <summary>用字节序列构造。</summary>
    /// <param name="bytes">字节。</param>
    public PyByteArray(IEnumerable<byte> bytes) => Bytes = new List<byte>(bytes);

    /// <summary>长度。</summary>
    public int Count => Bytes.Count;

    /// <summary>按下标取字节（返回 <c>int</c>）。</summary>
    /// <param name="index">下标。</param>
    public object GetItem(long index)
    {
        long normalized = index < 0 ? index + Bytes.Count : index;
        if (normalized < 0 || normalized >= Bytes.Count)
        {
            throw new PyIndexError("bytearray index out of range");
        }
        return (long)Bytes[(int)normalized];
    }

    /// <summary>按下标写字节。</summary>
    /// <param name="index">下标。</param>
    /// <param name="value">0~255 的整数。</param>
    public void SetItem(long index, object? value)
    {
        long normalized = index < 0 ? index + Bytes.Count : index;
        if (normalized < 0 || normalized >= Bytes.Count)
        {
            throw new PyIndexError("bytearray index out of range");
        }
        Bytes[(int)normalized] = PyEncoding.ToByte(value);
    }

    /// <summary>取切片。</summary>
    /// <param name="slice">切片。</param>
    public PyByteArray GetSlice(PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Count);
        var result = new List<byte>((int)count);
        long index = start;
        for (long i = 0; i < count; i++, index += step)
        {
            result.Add(Bytes[(int)index]);
        }
        return new PyByteArray(result);
    }

    /// <summary><c>del ba[i]</c> / <c>del ba[i:j]</c>。</summary>
    /// <param name="index">下标或切片。</param>
    public void DelItem(object? index)
    {
        switch (index)
        {
            case PySlice slice:
                {
                    var (start, _, step, count) = slice.Indices(Count);
                    if (count == 0)
                    {
                        return;
                    }
                    int previousCount = Count;
                    EnsureCanResize(-(int)count);
                    if (step > 0)
                    {
                        // 从后往前删，前面的下标才不会因为移除而失效
                        long position = start + (count - 1) * step;
                        for (long i = 0; i < count; i++, position -= step)
                        {
                            Bytes.RemoveAt((int)position);
                        }
                    }
                    else
                    {
                        long position = start;
                        for (long i = 0; i < count; i++, position += step)
                        {
                            Bytes.RemoveAt((int)position);
                        }
                    }
                    RecordResize(previousCount);
                    return;
                }
            case long raw:
                {
                    long normalized = raw < 0 ? raw + Count : raw;
                    if (normalized < 0 || normalized >= Count)
                    {
                        throw new PyIndexError("bytearray index out of range");
                    }
                    EnsureCanResize(-1);
                    int previousCount = Count;
                    Bytes.RemoveAt((int)normalized);
                    RecordResize(previousCount);
                    return;
                }
            case int or bool or short or byte or System.Numerics.BigInteger:
                DelItem(ToIndex(index));
                return;
            default:
                throw new PyTypeError(
                    $"bytearray indices must be integers or slices, not {PyRepr.TypeName(index)}"
                );
        }
    }

    /// <summary><c>extend(x)</c>。</summary>
    /// <param name="value">字节序列。</param>
    public void Extend(object? value)
    {
        // 输入迭代/元素校验先完成，再检查导出锁；失败不部分写入或提前吞掉副作用。
        byte[] bytes = PyEncoding.ToBytes(value);
        EnsureCanResize(bytes.Length);
        int previousCount = Count;
        Bytes.AddRange(bytes);
        RecordResize(previousCount);
    }

    /// <summary><c>append(x)</c>。</summary>
    /// <param name="value">0~255 的整数。</param>
    public void Append(object? value)
    {
        byte item = PyEncoding.ToByte(value);
        EnsureCanResize(1);
        int previousCount = Count;
        Bytes.Add(item);
        RecordResize(previousCount);
    }

    /// <summary>复制成 <c>bytes</c>。</summary>
    public PyBytes ToBytes() => new([.. Bytes]);

    private static long ToIndex(object? key) => key switch
    {
        bool b => b ? 1 : 0,
        long l => l,
        int i => i,
        short s => s,
        byte b2 => b2,
        System.Numerics.BigInteger big when big >= long.MinValue && big <= long.MaxValue =>
            (long)big,
        System.Numerics.BigInteger => throw new PyIndexError(
            "cannot fit 'int' into an index-sized integer"
        ),
        _ => throw new PyTypeError(
            $"bytearray indices must be integers or slices, not {PyRepr.TypeName(key)}"
        ),
    };
}

/// <summary>编码与字节转换。</summary>
public static class PyEncoding
{
    /// <summary>按名字解码字节串（支持 utf-8 / ascii / latin-1）。</summary>
    /// <param name="bytes">字节。</param>
    /// <param name="encoding">编码名。</param>
    public static string Decode(ReadOnlySpan<byte> bytes, string encoding)
    {
        string normalized = encoding.ToLowerInvariant().Replace("-", "").Replace("_", "");
        try
        {
            return normalized switch
            {
                "utf8" or "u8" or "" => new UTF8Encoding(false, true).GetString(bytes),
                "ascii" or "usascii" => Encoding.ASCII.GetString(bytes),
                "latin1" or "iso88591" => Encoding.Latin1.GetString(bytes),
                _ => throw new PyLookupError($"unknown encoding: {encoding}"),
            };
        }
        catch (DecoderFallbackException)
        {
            throw new PyUnicodeDecodeError(
                $"'utf-8' codec can't decode bytes: invalid start byte"
            );
        }
    }

    /// <summary>按名字编码字符串。</summary>
    /// <param name="text">字符串。</param>
    /// <param name="encoding">编码名。</param>
    public static byte[] Encode(string text, string encoding)
    {
        string normalized = encoding.ToLowerInvariant().Replace("-", "").Replace("_", "");
        return normalized switch
        {
            "utf8" or "u8" or "" => new UTF8Encoding(false, true).GetBytes(text),
            "ascii" or "usascii" => Encoding.ASCII.GetBytes(text),
            "latin1" or "iso88591" => Encoding.Latin1.GetBytes(text),
            _ => throw new PyLookupError($"unknown encoding: {encoding}"),
        };
    }

    /// <summary>把任意序列转成字节数组。</summary>
    /// <param name="value">bytes / bytearray / 整数序列。</param>
    public static byte[] ToBytes(object? value) => value switch
    {
        PyBytes bytes => bytes.Bytes,
        PyByteArray array => [.. array.Bytes],
        IPyBufferView view => view.CopyBuffer(),
        string text => Encoding.Latin1.GetBytes(text),
        _ => CollectBytes(value),
    };

    private static byte[] CollectBytes(object? value)
    {
        var bytes = new List<byte>();
        IPyIterator iterator = PyIter.Create(value);
        // 每次next之后立即验证当前元素，非法元素后的用户迭代器副作用不能继续执行。
        while (iterator.MoveNext()) bytes.Add(ToByte(iterator.Current()));
        return bytes.ToArray();
    }

    /// <summary>把整数转成单字节，越界抛 <c>ValueError</c>。</summary>
    /// <param name="value">整数值。</param>
    public static byte ToByte(object? value)
    {
        if (!PyInt.IsInt(value))
        {
            string type = PyRepr.TypeName(value);
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError($"'{type}' object cannot be interpreted as an integer");
            if (value is bool)
                throw new PyNotImplementedError("bool returned by __index__ in byte conversion is not supported");
            if (!PyInt.IsInt(value))
                throw new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(value)})");
        }
        // 只检查转换结果的范围，不catch用户__index__抛出的OverflowError或其它异常。
        if (PyInt.TryI64(value!, out long number))
        {
            if (number >= 0 && number <= 255) return (byte)number;
        }
        else
        {
            var integer = PyInt.ToBigInteger(value!);
            if (integer >= 0 && integer <= 255) return (byte)integer;
        }
        throw new PyValueError("byte must be in range(0, 256)");
    }
}
