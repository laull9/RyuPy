// 编码、摘要与认证共用的严格 buffer 边界：只消费真实原始字节，绝不经迭代器隐式构造 bytes。
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

internal static class PyBinaryBuffer
{
    internal static ReadOnlySpan<byte> Read(object? value) => value switch
    {
        PyBytes bytes => bytes.Span,
        PyByteArray array => CollectionsMarshal.AsSpan(array.Bytes),
        PyMemoryView view => view.ReadSynchronousBuffer(),
        _ => throw new PyTypeError($"a bytes-like object is required, not '{PyRepr.TypeName(value)}'"),
    };

    // 借用现有连续 buffer 导出租约；跨参数回调时固定尺寸和 view 活性，之后再取 span。
    internal static PyRegexBufferLease Acquire(object? value)
    {
        if (value is PyMemoryView view) return view.AcquireRegexExport();
        int length = Read(value).Length;
        var lease = new PyRegexBufferLease(value!, null, 0, length);
        lease.Activate();
        return lease;
    }

    // Python 层 base64 会物化任意 view，却保留 bytes/bytearray；binascii 的 C buffer 入口另取租约。
    internal static object DecodeInput(object? value) => value is PyBytes or PyByteArray ? value : new PyBytes(Ascii(value));

    internal static byte[] Ascii(object? value)
    {
        if (value is not string text) return Read(value).ToArray();
        var bytes = new byte[text.Length];
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] > 127) throw new PyValueError("string argument should contain only ASCII characters");
            bytes[index] = (byte)text[index];
        }
        return bytes;
    }

    internal static ReadOnlySpan<byte> HashData(object? value)
    {
        if (value is string) throw new PyTypeError("Strings must be encoded before hashing");
        if (value is not PyBytes and not PyByteArray and not PyMemoryView)
            throw new PyTypeError("object supporting the buffer API required");
        return Read(value);
    }

    internal static BigInteger Index(object? value)
    {
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("object cannot be interpreted as an integer");
            if (!PyInt.IsInt(value)) throw new PyTypeError("__index__ returned non-int");
        }
        return PyInt.ToBigInteger(value!);
    }

}
