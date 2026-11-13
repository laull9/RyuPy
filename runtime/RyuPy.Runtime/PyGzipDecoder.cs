// gzip 容器状态机：逐成员验证头部、CRC32 与 ISIZE，支持零填充，拒绝损坏尾随数据。
// DEFLATE 数据只交给 DeflateStream；不复制或重写任何压缩算法。
using System;
using System.IO;
using System.IO.Compression;

namespace RyuPy.Runtime;

internal sealed class PyGzipDecoder(Stream source, bool firstOnly = false) : Stream
{
    private DeflateStream? inflater;
    private uint checksum, memberSize;
    private uint headerChecksum;
    private bool readingHeader;
    private long total;
    private bool finished, hasMember;

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length == 0 || finished) return 0;
        while (true)
        {
            if (inflater is null && !StartMember()) return 0;
            int count;
            try { count = inflater!.Read(buffer[..Math.Min(buffer.Length, 8192)]); }
            catch (InvalidDataException error) { throw new PyZlibError("Error decompressing data: " + error.Message); }
            if (count != 0)
            {
                if (total + count > PyCompression.OutputLimit)
                    throw new PyMemoryError("decompressed data exceeds 64 MiB limit");
                total += count;
                checksum = PyCompression.Crc32(buffer[..count], checksum);
                memberSize = unchecked(memberSize + (uint)count);
                return count;
            }
            inflater!.Dispose(); inflater = null;
            uint expectedCrc = ReadUInt32(), expectedSize = ReadUInt32();
            if (checksum != expectedCrc) throw new PyBadGzipFile("CRC check failed");
            if (memberSize != expectedSize) throw new PyBadGzipFile("Incorrect length of data produced");
            if (firstOnly) { finished = true; return 0; }
        }
    }

    private bool StartMember()
    {
        int first = source.ReadByte();
        // 零填充只允许在完整成员之后，输入起始的零仍是无效 magic。
        while (hasMember && first == 0) first = source.ReadByte();
        if (first < 0)
        {
            if (firstOnly && !hasMember) throw Truncated();
            finished = true; return false;
        }
        int second = source.ReadByte();
        if (first != 31 || second != 139) throw new PyBadGzipFile("Not a gzipped file");
        headerChecksum = PyCompression.Crc32([31, 139]); readingHeader = true;
        int method = Required(), flags = Required();
        if (method != 8) throw new PyBadGzipFile("Unknown compression method");
        if (firstOnly && (flags & 224) != 0) throw new PyZlibError("unknown gzip header flags");
        for (int index = 0; index < 6; index++) Required();
        if ((flags & 4) != 0)
        {
            int length = Required() | Required() << 8;
            for (int index = 0; index < length; index++) Required();
        }
        if ((flags & 8) != 0) ReadTerminated();
        if ((flags & 16) != 0) ReadTerminated();
        // gzip 模块跳过 FHCRC，zlib 的 gzip 模式严格核对头校验与保留位。
        readingHeader = false;
        if ((flags & 2) != 0)
        {
            int expectedHeader = Required() | Required() << 8;
            if (firstOnly && expectedHeader != (headerChecksum & 65535))
                throw new PyZlibError("header crc mismatch");
        }
        checksum = 0; memberSize = 0; hasMember = true;
        inflater = new DeflateStream(new PyCompressionInput(source, true), CompressionMode.Decompress);
        return true;
    }

    private void ReadTerminated()
    {
        for (int count = 0; count < 1024 * 1024; count++) if (Required() == 0) return;
        throw new PyMemoryError("gzip header exceeds 1 MiB limit");
    }

    private uint ReadUInt32()
    {
        uint result = 0;
        for (int index = 0; index < 4; index++) result |= (uint)Required() << (index * 8);
        return result;
    }
    private int Required()
    {
        int value = source.ReadByte();
        if (value < 0) throw Truncated();
        if (readingHeader) headerChecksum = PyCompression.Crc32([(byte)value], headerChecksum);
        return value;
    }
    private static PyEOFError Truncated() => new("Compressed file ended before the end-of-stream marker was reached");
    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { inflater?.Dispose(); } finally { source.Dispose(); finished = true; } }
        base.Dispose(disposing);
    }
    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
