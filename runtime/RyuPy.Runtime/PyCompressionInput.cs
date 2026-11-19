// 向 .NET 解码器逐字节供给输入，阻止预取吞掉容器尾部和下一成员。
// 解码器尚未结束而源已耗尽时主动抛 EOFError，消除 DeflateStream 对截断流的宽松 EOF。
using System;
using System.IO;

namespace RyuPy.Runtime;

internal sealed class PyCompressionInput(Stream source, bool leaveOpen) : Stream
{
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length == 0) return 0;
        int value = source.ReadByte();
        if (value < 0) throw new PyEOFError("Compressed file ended before the end-of-stream marker was reached");
        buffer[0] = (byte)value;
        return 1;
    }
    protected override void Dispose(bool disposing) { if (disposing && !leaveOpen) source.Dispose(); base.Dispose(disposing); }
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
