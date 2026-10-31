// 二进制序列：bytes（不可变）、bytearray（可变）与编码辅助 PyEncoding。
// 元素按 Python 规则是 0–255 的 int；下标、切片、repr 与 CPython 一致。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
public sealed class PyByteArray
{
    /// <summary>字节内容。</summary>
    public List<byte> Bytes { get; }

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
                    return;
                }
            case long raw:
                {
                    long normalized = raw < 0 ? raw + Count : raw;
                    if (normalized < 0 || normalized >= Count)
                    {
                        throw new PyIndexError("bytearray index out of range");
                    }
                    Bytes.RemoveAt((int)normalized);
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
    public void Extend(object? value) => Bytes.AddRange(PyEncoding.ToBytes(value));

    /// <summary><c>append(x)</c>。</summary>
    /// <param name="value">0~255 的整数。</param>
    public void Append(object? value) => Bytes.Add(PyEncoding.ToByte(value));

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
        string text => Encoding.Latin1.GetBytes(text),
        _ => Array.ConvertAll(PyOps.ToSequence(value), ToByte),
    };

    /// <summary>把整数转成单字节，越界抛 <c>ValueError</c>。</summary>
    /// <param name="value">整数值。</param>
    public static byte ToByte(object? value)
    {
        long number = value switch
        {
            bool b => b ? 1 : 0,
            long l => l,
            int i => i,
            _ => throw new PyTypeError(
                $"'{
                    PyRepr.TypeName(value)
                }' object cannot be interpreted as an integer"
            ),
        };
        if (number < 0 || number > 255)
        {
            throw new PyValueError("byte must be in range(0, 256)");
        }
        return (byte)number;
    }
}
