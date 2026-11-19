// 压缩 API 独立验收：真实 .NET 互操作、完整截断矩阵、错误身份、文件关闭和输出上限。
using System.IO.Compression;
using System.Text;

namespace RyuPy.Runtime.Tests;

public class PyCompressionTests
{
    private static object? Call(bool gzip, string name, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue((gzip ? PyCompression.GzipAttributes() : PyCompression.ZlibAttributes())[name], args, names ?? [], values ?? []);
    private static byte[] Bytes(object? value) => Assert.IsType<PyBytes>(value).Bytes;
    private static byte[] Pack(byte[] bytes, int window = 15, int level = 6) =>
        Bytes(Call(false, "compress", [new PyBytes(bytes), (long)level, (long)window]));
    private static byte[] Unpack(byte[] bytes, int window = 15) =>
        Bytes(Call(false, "decompress", [new PyBytes(bytes), (long)window]));
    private static byte[] Gzip(byte[] bytes, object? stamp = null) =>
        Bytes(Call(true, "compress", [new PyBytes(bytes)], ["mtime"], [stamp ?? 0L]));
    private static byte[] Gunzip(byte[] bytes) => Bytes(Call(true, "decompress", [new PyBytes(bytes)]));
    private static object? Method(PyIOStream file, string name, params object?[] args) =>
        PyOps.CallValue(file.GetAttr(name), args, [], []);

    [Theory]
    [InlineData(15)]
    [InlineData(-15)]
    [InlineData(31)]
    public void LevelsInteroperateAndEveryTruncatedPrefixFails(int window)
    {
        byte[] original = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("猫\0hello🎉\n", 30)));
        foreach (int level in new[] { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })
        {
            Assert.Empty(Unpack(Pack([], window, level), window));
            byte[] packed = Pack(original, window, level);
            Assert.Equal(original, Unpack(packed, window));
            using var source = new MemoryStream(packed);
            using Stream decoder = window == 15 ? new ZLibStream(source, CompressionMode.Decompress)
                : window == -15 ? new DeflateStream(source, CompressionMode.Decompress)
                : new GZipStream(source, CompressionMode.Decompress);
            using var output = new MemoryStream(); decoder.CopyTo(output);
            Assert.Equal(original, output.ToArray());
            for (int end = 0; end < packed.Length; end++)
                Assert.Throws<PyZlibError>(() => Unpack(packed[..end], window));
            Assert.Equal(original, Unpack([.. packed, 42, 17], window));
        }
    }

    [Fact]
    public void GzipMembersFootersPaddingAndOptionalHeadersAreChecked()
    {
        byte[] packed = Gzip([1, 2, 3]);
        Assert.Empty(Gunzip([]));
        Assert.Empty(Gunzip(Gzip([])));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Gunzip([.. packed, 0, 0, .. Gzip([4]), 0]));
        Assert.Equal(new byte[] { 1, 2, 3 }, Unpack([.. packed, .. Gzip([4])], 31));
        for (int end = 1; end < packed.Length; end++) Assert.ThrowsAny<PyBaseException>(() => Gunzip(packed[..end]));
        Assert.Throws<PyBadGzipFile>(() => Gunzip([.. packed, 1]));
        Assert.Throws<PyBadGzipFile>(() => Gunzip([.. packed, 0, 1]));
        Assert.Throws<PyEOFError>(() => Gunzip([.. packed, 31, 139]));
        byte[] crc = (byte[])packed.Clone(); crc[^8] ^= 1;
        byte[] length = (byte[])packed.Clone(); length[^4] ^= 1;
        Assert.Throws<PyBadGzipFile>(() => Gunzip(crc));
        Assert.Throws<PyBadGzipFile>(() => Gunzip(length));
        byte[] optional = [.. packed[..10], 3, 0, 11, 12, 13, 110, 0, 99, 0, 0, 0, .. packed[10..]];
        optional[3] = 4 | 8 | 16 | 2;
        Assert.Equal(new byte[] { 1, 2, 3 }, Gunzip(optional));
        Assert.Throws<PyZlibError>(() => Unpack(optional, 31));
        uint headerCrc = PyCompression.Crc32(optional.AsSpan(0, 19));
        optional[19] = (byte)headerCrc; optional[20] = (byte)(headerCrc >> 8);
        Assert.Equal(new byte[] { 1, 2, 3 }, Unpack(optional, 31));
        for (int end = 10; end < 22; end++) Assert.Throws<PyEOFError>(() => Gunzip(optional[..end]));
        byte[] invalidBody = (byte[])packed.Clone(); invalidBody[10] = 7;
        Assert.Throws<PyZlibError>(() => Gunzip(invalidBody));
    }

    [Fact]
    public void TimestampAndChecksumsHaveObservableContracts()
    {
        foreach (long stamp in new[] { 0L, 1L, 1234567890L, 4294967295L })
        {
            byte[] packed = Gzip([10], stamp);
            Assert.Equal((uint)stamp, BitConverter.ToUInt32(packed, 4));
        }
        Assert.Equal(12U, BitConverter.ToUInt32(Gzip([], 12.9), 4));
        Assert.Throws<PyOverflowError>(() => Gzip([], -1L));
        Assert.Throws<PyOverflowError>(() => Gzip([], 4294967296L));
        Assert.Throws<PyOverflowError>(() => Gzip([], double.NaN));
        byte[] data = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0xcbf43926U, PyCompression.Crc32(data));
        Assert.Equal(0x091e01deU, PyCompression.Adler32(data));
        Assert.Equal(917518U, PyCompression.Adler32([], uint.MaxValue));
        Assert.Equal(14U, PyCompression.Adler32([], 65535));
        Assert.Equal(PyCompression.Crc32(data), PyCompression.Crc32(data.AsSpan(4), PyCompression.Crc32(data.AsSpan(0, 4))));
        Assert.Equal(PyCompression.Adler32(data), PyCompression.Adler32(data.AsSpan(4), PyCompression.Adler32(data.AsSpan(0, 4))));
        Assert.Equal(new byte[] { 1 }, Unpack(Pack([1]), 0));
        Assert.Equal(new byte[] { 1 }, Unpack(Gzip([1]), 47));
        Assert.Equal(new byte[] { 1 }, Unpack(Pack([1]), 47));
    }

    [Fact]
    public void UnsupportedOptionsAndWrongTypesFailBeforeOpeningAFile()
    {
        foreach (long window in new[] { 8L, 9L, 14L, -9L, 25L, 30L })
            Assert.Throws<PyNotImplementedError>(() => Call(false, "compress", [new PyBytes([])], ["wbits"], [window]));
        Assert.Throws<PyTypeError>(() => Call(false, "compress", ["text"]));
        Assert.Throws<PyTypeError>(() => Call(false, "compress", [new PyBytes([])], ["unknown"], [1L]));
        Assert.Throws<PyTypeError>(() => Call(false, "compress", [], ["data"], [new PyBytes([])]));
        Assert.Throws<PyTypeError>(() => Call(false, "crc32", [new PyBytes([])], ["value"], [1L]));
        Assert.Throws<PyValueError>(() => Call(false, "decompress", [new PyBytes([]), 15L, 0L]));
        Assert.Throws<PyZlibError>(() => Pack([], level: 10));
        Assert.Throws<PyZlibError>(() => Unpack([], 99));
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gz");
        Assert.Throws<PyValueError>(() => Call(true, "open", [path, "wb"], ["encoding"], ["utf-8"]));
        Assert.Throws<PyNotImplementedError>(() => Call(true, "open", [path, "wt"], ["errors"], ["ignore"]));
        Assert.False(File.Exists(path));
        File.WriteAllText(path, "parent is a file");
        try { Assert.Throws<PyNotADirectoryError>(() => Call(true, "open", [Path.Combine(path, "child.gz"), "wb"])); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FileStreamsAppendTextFlushCloseAndReleaseHandles()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "sample.gz");
        try
        {
            var writer = Assert.IsType<PyGzipFile>(Call(true, "open", [path, "wb"]));
            Assert.Equal(2L, Method(writer, "write", new PyBytes([1, 2])));
            Method(writer, "flush"); Method(writer, "close"); Method(writer, "close");
            Assert.True(writer.Closed);
            Assert.Throws<PyValueError>(() => Method(writer, "read"));
            Assert.InRange(BitConverter.ToUInt32(File.ReadAllBytes(path), 4),
                (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 10, (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var append = Assert.IsType<PyGzipFile>(Call(true, "open", [path, "ab"]));
            Method(append, "write", new PyBytes([3])); append.Close();
            Assert.Equal(new byte[] { 1, 2, 3 }, Gunzip(File.ReadAllBytes(path)));
            var reader = Assert.IsType<PyGzipFile>(Call(true, "open", [path]));
            Assert.Empty(Bytes(Method(reader, "read", 0L)));
            Assert.Equal(new byte[] { 1 }, Bytes(Method(reader, "read", 1L)));
            Assert.Equal(1L, Method(reader, "tell"));
            Assert.Equal(false, Method(reader, "seekable"));
            Assert.Throws<PyUnsupportedOperation>(() => Method(reader, "seek", 0L));
            Method(reader, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance);
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            var text = Assert.IsType<PyGzipFile>(Call(true, "open", [path, "wt"]));
            Assert.Equal(7L, Method(text, "write", "猫\r\n🎉\rl\n")); text.Close();
            var decoded = Assert.IsType<PyGzipFile>(Call(true, "open", [path, "rt"]));
            Assert.Equal("猫\n", Method(decoded, "readline"));
            Assert.Equal("🎉", Method(decoded, "read", 1L));
            Assert.Equal("\nl\n", Method(decoded, "read")); decoded.Close();
            var empty = Assert.IsType<PyGzipFile>(Call(true, "open", [path, "wb"])); empty.Close();
            Assert.True(File.ReadAllBytes(path).Length >= 20);
            Assert.Empty(Gunzip(File.ReadAllBytes(path)));
            Assert.Throws<PyFileExistsError>(() => Call(true, "open", [path, "xb"]));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ExpansionBombAndAccumulatedMembersHaveAFixedLimit()
    {
        byte[] zeros = new byte[1024 * 1024];
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true))
            for (int count = 0; count <= 64; count++) gzip.Write(zeros);
        Assert.Throws<PyMemoryError>(() => Gunzip(output.ToArray()));
        byte[] member = Gzip(zeros);
        using var members = new MemoryStream();
        for (int count = 0; count <= 64; count++) members.Write(member);
        Assert.Throws<PyMemoryError>(() => Gunzip(members.ToArray()));
    }
}
