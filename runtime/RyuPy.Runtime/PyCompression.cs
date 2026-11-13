// zlib/gzip 的原生入口、整数参数与结果上限；压缩算法全部交给 .NET 的成熟流。
// CRC32 独立实现以供后续 binascii 合并复用，当前不引用其它分支的未存在类型。
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>一次性压缩 API 与共享容器边界。</summary>
internal static class PyCompression
{
    /// <summary>每次解压或每个 gzip 文件最多产生 64 MiB；超限抛 MemoryError。</summary>
    public const int OutputLimit = 64 * 1024 * 1024;
    private static readonly uint[] CrcTable = CreateCrcTable();

    /// <summary>只登记本轮真正支持的 zlib API。</summary>
    public static Dictionary<string, object?> ZlibAttributes() => new()
    {
        ["compress"] = PyNativeFunctions.Create("compress", ["data", "level", "wbits"],
            [null, -1L, 15L], 1, args => Compress(Bytes(args[0]), Level(args[1]), Window(args[2], true)),
            ["posonly", "pos", "pos"]),
        ["decompress"] = PyNativeFunctions.Create("decompress", ["data", "wbits", "bufsize"],
            [null, 15L, 16384L], 1, Decompress, ["posonly", "pos", "pos"]),
        ["adler32"] = PyNativeFunctions.Create("adler32", ["data", "value"], [null, 1L], 1,
            args => (long)Adler32(Bytes(args[0]), Seed(args[1])), ["posonly", "posonly"]),
        ["crc32"] = PyNativeFunctions.Create("crc32", ["data", "value"], [null, 0L], 1,
            args => (long)Crc32(Bytes(args[0]), Seed(args[1])), ["posonly", "posonly"]),
        ["error"] = PyType.Get("zlib.error"),
        ["MAX_WBITS"] = 15L,
        ["DEFLATED"] = 8L,
    };

    /// <summary>gzip 的一次性与路径文件 API；不伪造 GzipFile 构造器。</summary>
    public static Dictionary<string, object?> GzipAttributes() => new()
    {
        ["compress"] = PyNativeFunctions.Create("compress", ["data", "compresslevel", "mtime"],
            [null, 9L, null], 1, args => GzipCompress(Bytes(args[0]), Level(args[1]), Timestamp(args[2])),
            ["pos", "pos", "kwonly"]),
        ["decompress"] = PyNativeFunctions.Create("decompress", ["data"], [null], 1,
            args => new PyBytes(ReadAll(new PyGzipDecoder(new MemoryStream(Bytes(args[0]), false))))),
        ["open"] = PyNativeFunctions.Create("open", ["filename", "mode", "compresslevel", "encoding", "errors", "newline"],
            [null, "rb", 9L, null, null, null], 1, PyGzipFile.Open),
        ["BadGzipFile"] = PyType.Get("BadGzipFile"),
    };

    // 连续 buffer 复用当前运行时协议；不通过可迭代整数伪造 bytes-like 对象。
    internal static byte[] Bytes(object? value) => value is IPyBufferView view ? view.CopyBuffer() : PyIO.Bytes(value);

    internal static int Level(object? value)
    {
        long level = PyIO.Index(value);
        if (level < -1 || level > 9)
            throw new PyZlibError("Bad compression level");
        return (int)level;
    }

    private static int Window(object? value, bool compress)
    {
        long window = PyIO.Index(value);
        if (window is 15 or -15 or 31 || !compress && window is 0 or 47) return (int)window;
        if (window is >= 8 and <= 14 or >= -14 and <= -8 or >= 24 and <= 30
            || !compress && window is >= 40 and <= 46)
            throw new PyNotImplementedError("only 32 KiB compression windows are supported (wbits=15, -15, 31, 0, 47)");
        throw new PyZlibError("Invalid initialization option");
    }

    private static uint Timestamp(object? value)
    {
        if (PyIO.IsNone(value)) return checked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        // CPython 接受有限浮点时间并向零取整；不能吞掉显式时间戳。
        double? floating = value as double?;
        if (floating.HasValue)
        {
            if (!double.IsFinite(floating.Value) || floating.Value < 0 || floating.Value >= 4294967296d)
                throw new PyOverflowError("mtime must fit an unsigned 32-bit timestamp");
            return (uint)floating.Value;
        }
        long stamp = PyIO.Index(value);
        if (stamp < 0 || stamp > uint.MaxValue) throw new PyOverflowError("mtime must fit an unsigned 32-bit timestamp");
        return (uint)stamp;
    }

    private static uint Seed(object? value)
    {
        object? integer = value;
        if (integer is not (bool or int or long or BigInteger))
        {
            if (!PyObject.TryMethod(integer, "__index__", [], out integer)) throw new PyTypeError("an integer is required");
        }
        BigInteger number = integer switch
        {
            bool boolean => boolean ? BigInteger.One : BigInteger.Zero,
            int small => small, long normal => normal, BigInteger big => big,
            _ => throw new PyTypeError("__index__ returned non-int"),
        };
        return (uint)(number & uint.MaxValue);
    }

    private static PyBytes Compress(byte[] data, int level, int window)
    {
        if (window == 31) return GzipCompress(data, level, 0);
        // .NET 对从未写入正文的流不生成产物；空块使用 RFC1951 的固定终止块。
        if (data.Length == 0) return new PyBytes(window == -15 ? [3, 0] : [120, 1, 3, 0, 0, 0, 0, 1]);
        using var output = new MemoryStream();
        using (Stream compressor = window == -15
            ? new DeflateStream(output, new ZLibCompressionOptions { CompressionLevel = level }, true)
            : new ZLibStream(output, new ZLibCompressionOptions { CompressionLevel = level }, true))
            compressor.Write(data);
        return new PyBytes(output.ToArray());
    }

    private static PyBytes GzipCompress(byte[] data, int level, uint stamp)
    {
        if (data.Length == 0) return new PyBytes(EmptyGzip(stamp));
        using var output = new MemoryStream();
        using (var compressor = new GZipStream(output, new ZLibCompressionOptions { CompressionLevel = level }, true))
        {
            compressor.Write(data);
        }
        byte[] result = output.ToArray();
        for (int index = 0; index < 4; index++) result[4 + index] = (byte)(stamp >> (index * 8));
        return new PyBytes(result);
    }

    internal static byte[] EmptyGzip(uint stamp) =>
        [31, 139, 8, 0, (byte)stamp, (byte)(stamp >> 8), (byte)(stamp >> 16), (byte)(stamp >> 24),
            0, 255, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    private static object Decompress(object?[] args)
    {
        byte[] data = Bytes(args[0]);
        int window = Window(args[1], false);
        if (PyIO.Index(args[2]) <= 0) throw new PyValueError("bufsize must be greater than zero");
        try
        {
            if (window == 31 || window == 47 && data.Length > 0 && data[0] == 31)
                return new PyBytes(ReadAll(new PyGzipDecoder(new MemoryStream(data, false), firstOnly: true)));
            using var input = new PyCompressionInput(new MemoryStream(data, false), false);
            using Stream decoder = window == -15 ? new DeflateStream(input, CompressionMode.Decompress)
                : new ZLibStream(input, CompressionMode.Decompress);
            return new PyBytes(ReadAll(decoder, false));
        }
        catch (Exception error) when (error is InvalidDataException or PyEOFError or PyBadGzipFile)
        { throw new PyZlibError("Error decompressing data: " + error.Message); }
    }

    internal static byte[] ReadAll(Stream decoder, bool dispose = true)
    {
        try
        {
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = decoder.Read(buffer)) != 0)
            {
                if (output.Length + count > OutputLimit) throw new PyMemoryError("decompressed data exceeds 64 MiB limit");
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
        finally { if (dispose) decoder.Dispose(); }
    }

    /// <summary>IEEE CRC32，支持传入上次公开结果继续计算。</summary>
    public static uint Crc32(ReadOnlySpan<byte> data, uint seed = 0)
    {
        uint crc = ~seed;
        foreach (byte value in data) crc = CrcTable[(crc ^ value) & 255] ^ (crc >> 8);
        return ~crc;
    }

    /// <summary>Adler32，初始值按 Python 的低 32 位解释。</summary>
    public static uint Adler32(ReadOnlySpan<byte> data, uint seed = 1)
    {
        uint lower = (seed & 65535) % 65521, upper = (seed >> 16) % 65521;
        foreach (byte value in data) { lower = (lower + value) % 65521; upper = (upper + lower) % 65521; }
        return (upper << 16) | lower;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            uint value = index;
            for (int bit = 0; bit < 8; bit++) value = (value >> 1) ^ ((value & 1) == 0 ? 0 : 0xedb88320);
            table[index] = value;
        }
        return table;
    }
}
