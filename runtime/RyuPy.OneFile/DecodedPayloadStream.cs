// BrotliStream 对部分截断输入会报告 EOF；这里要求解码器明确返回 Done，并拒绝压缩尾随数据。
using System.Buffers;
using System.IO.Compression;

internal sealed class DecodedPayloadStream(Stream input) : Stream
{
    private BrotliDecoder decoder = new();
    private readonly byte[] buffer = new byte[128 * 1024];
    private int offset;
    private int available;
    private bool done;
    public override int Read(byte[] destination, int start, int count) => Read(destination.AsSpan(start, count));
    public override int Read(Span<byte> destination)
    {
        if (destination.IsEmpty || done) return 0;
        while (true)
        {
            var status = decoder.Decompress(buffer.AsSpan(offset, available), destination, out int consumed, out int written);
            offset += consumed;
            available -= consumed;
            if (status == OperationStatus.InvalidData) throw new InvalidDataException("Brotli 载荷损坏");
            if (status == OperationStatus.Done)
            {
                done = true;
                if (available != 0 || input.ReadByte() != -1) throw new InvalidDataException("Brotli 载荷含尾随数据");
            }
            if (written != 0 || done) return written;
            if (status == OperationStatus.DestinationTooSmall) throw new InvalidDataException("Brotli 解码器未取得进展");
            // NeedMoreData 可留下尚未消耗的压缩头；移动后继续补读，保持内存有界。
            buffer.AsSpan(offset, available).CopyTo(buffer);
            offset = 0;
            int read = input.Read(buffer, available, buffer.Length - available);
            if (read == 0) throw new EndOfStreamException("Brotli 载荷被截断");
            available += read;
        }
    }
    protected override void Dispose(bool disposing) { decoder.Dispose(); base.Dispose(disposing); }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
