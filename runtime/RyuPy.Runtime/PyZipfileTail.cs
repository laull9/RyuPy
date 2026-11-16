// 对齐固定 CP3.12.13 的 EOCD/ZIP64 尾探测，不读取中央目录、不解压、不额外收紧普通EOCD。
using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

internal static class PyZipfileTail
{
    private const int EndSize = 22, Window = 65535 + EndSize;
    private static object? Call(object? stream, string name, params object?[] args) =>
        PyOps.CallMethod(stream, name, args, [], []);
    private static object Position(BigInteger value) => PyInt.Normalize(value);

    internal static bool Check(object? stream)
    {
        Call(stream, "seek", 0L, 2L);
        object? reported = Call(stream, "tell");
        if (!PyInt.IsInt(reported))
            throw Own(new PyNotImplementedError("custom non-integer tell arithmetic is not supported"));
        BigInteger size = PyInt.ToBigInteger(reported!);
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try { Call(stream, "seek", -(long)EndSize, 2L); }
        catch (PyOSError error) { PyZipfile.Restore(error, depth, context); return false; }
        object? data = Call(stream, "read", (long)EndSize);
        if (Length(data) == EndSize && IsBytes(data) && FastEnd(Bytes(data)))
            return Check64(stream, size - EndSize);

        BigInteger start = BigInteger.Max(size - Window, BigInteger.Zero);
        Call(stream, "seek", Position(start), 0L);
        data = Call(stream, "read", (long)Window);
        int index = LastEnd(data);
        if (index < 0) return false;
        if (Bytes(data).Length - index < EndSize) return false;
        // CP只切取声明的comment，不要求实际comment完整；本识别无需保留comment副本。
        return Check64(stream, start + index);
    }

    private static bool Check64(object? stream, BigInteger endOffset)
    {
        BigInteger offset = endOffset - 20;
        if (offset < 0) return true;
        Call(stream, "seek", Position(offset));
        object? data = Call(stream, "read", 20L);
        if (Length(data) != 20) throw new PyOSError("Unknown I/O error");
        uint disk, disks; BigInteger relative;
        {
            var locator = Bytes(data);
            if (U32(locator, 0) != 0x07064b50) return true;
            disk = U32(locator, 4); disks = U32(locator, 16); relative = U64(locator, 8);
        }
        if (disk != 0 || disks > 1) throw new PyBadZipFile("zipfiles that span multiple disks are not supported");
        offset -= 56;
        if (relative > offset) throw new PyBadZipFile("Corrupt zip64 end of central directory locator");
        // 上述标量已冻结；可变响应的span不能跨下一次用户回调。
        Call(stream, "seek", Position(relative));
        BigInteger extraSize = offset - relative;
        data = Call(stream, "read", 56L);
        if (Length(data) != 56) throw new PyOSError("Unknown I/O error");
        bool signature = Starts64(data);
        if (!signature && relative != offset)
        {
            Call(stream, "seek", Position(offset)); extraSize = 0;
            data = Call(stream, "read", 56L);
            if (Length(data) != 56) throw new PyOSError("Unknown I/O error");
            signature = Starts64(data);
        }
        if (!signature) throw new PyBadZipFile("Zip64 end of central directory record not found");
        var record = Bytes(data);
        BigInteger recordSize = U64(record, 4), directorySize = U64(record, 40), directoryOffset = U64(record, 48);
        if (directoryOffset + directorySize != relative || recordSize + 12 != 56 + extraSize)
            throw new PyBadZipFile("Corrupt zip64 end of central directory record");
        return true;
    }

    private static bool FastEnd(ReadOnlySpan<byte> data) => U32(data, 0) == 0x06054b50 && data[20] == 0 && data[21] == 0;
    private static bool Starts64(object? data)
    {
        if (data is string) throw Own(new PyTypeError("startswith first arg must be str or a tuple of str, not bytes"));
        return U32(Bytes(data), 0) == 0x06064b50;
    }
    private static bool IsBytes(object? data) => data is PyBytes or PyByteArray;
    private static int Length(object? data) => data switch
    {
        PyBytes bytes => bytes.Count,
        PyByteArray bytes => bytes.Count,
        string text => checked((int)PyText.Length(text)),
        null or PyNone => throw Own(new PyTypeError("object of type 'NoneType' has no len()")),
        _ => throw Own(new PyNotImplementedError("custom read result length protocol is not supported")),
    };
    private static ReadOnlySpan<byte> Bytes(object? data) => data switch
    {
        PyBytes bytes => bytes.Bytes,
        PyByteArray bytes => CollectionsMarshal.AsSpan(bytes.Bytes),
        string => throw Own(new PyTypeError("a bytes-like object is required, not 'str'")),
        _ => throw Own(new PyNotImplementedError("custom read result buffer protocol is not supported")),
    };
    private static int LastEnd(object? data)
    {
        if (data is null or PyNone) throw Own(new PyAttributeError("'NoneType' object has no attribute 'rfind'"));
        if (data is string) throw Own(new PyTypeError("must be str, not bytes"));
        var bytes = Bytes(data);
        for (int i = bytes.Length - 4; i >= 0; i--)
            if (U32(bytes, i) == 0x06054b50) return i;
        return -1;
    }
    // 仅本解析器新造且向外传播的冷错误准备报告帧；用户回调异常不包catch。
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    private static uint U32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4));
    private static ulong U64(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(offset, 8));
}
