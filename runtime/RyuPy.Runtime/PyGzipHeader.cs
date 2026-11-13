// gzip 文件流头部的确定性时间戳写入适配：保留 GZipStream 输出，只覆盖 RFC1952 的 MTIME 四字节。
using System;
using System.IO;

namespace RyuPy.Runtime;

internal sealed class PyGzipHeader(Stream destination, uint timestamp) : Stream
{
    private long written;
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        int headerCount = (int)Math.Min(Math.Max(0, 10 - written), buffer.Length);
        if (headerCount > 0)
        {
            Span<byte> header = stackalloc byte[10];
            buffer[..headerCount].CopyTo(header);
            for (int index = 0; index < headerCount; index++)
            {
                long position = written + index;
                if (position is >= 4 and < 8) header[index] = (byte)(timestamp >> ((int)(position - 4) * 8));
            }
            destination.Write(header[..headerCount]);
        }
        destination.Write(buffer[headerCount..]); written += buffer.Length;
    }
    public override void Flush() => destination.Flush();
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { if (written == 0) Write(PyCompression.EmptyGzip(timestamp)); }
            finally { destination.Dispose(); }
        }
        base.Dispose(disposing);
    }
    public override bool CanRead => false;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
