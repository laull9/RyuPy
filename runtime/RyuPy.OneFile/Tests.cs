// 源码负例与行为测试：归档当次内存生成，外部临时目录退出即清理。
using System.IO.Compression;
using System.Text;

internal static class Tests
{
    private static int Main()
    {
        int count = 0;
        void Check(byte[] raw, bool valid)
        {
            string root = Directory.CreateTempSubdirectory("ryupy-onefile-parser-test-").FullName;
            try
            {
                using var compressed = new MemoryStream();
                using (var br = new BrotliStream(compressed, CompressionMode.Compress, true)) br.Write(raw);
                compressed.Position = 0;
                try
                {
                    string entry = Payload.Extract(compressed, root, () => false);
                    if (!valid) throw new Exception("负例被错误接受");
                    if (File.ReadAllText(entry) != "hello") throw new Exception("载荷内容错误");
                }
                catch (Exception error) when (!valid && error is InvalidDataException or EndOfStreamException or IOException) { }
                count++;
            }
            finally { Directory.Delete(root, true); }
        }
        Check(Archive("程序 空格", 5), true);
        foreach (string path in new[] { "../escape", "/escape", "a\\b", "a/../b", "a//b", "NUL.txt", "C:escape", "a.", "a\0b" })
            Check(Archive(path, 5), false);
        Check(Archive("main", ulong.MaxValue), false);
        Check(Archive("main", 5, 100_001), false);
        Check(Archive("main", 5, duplicate: true), false);
        Check(Archive("main", 5, kind: 2), false);
        byte[] good = Archive("main", 5);
        // 每个截断点都必须拒绝，包含文件数据和结束标记的每个字节。
        for (int n = 0; n < good.Length; n++) Check(good[..n], false);
        good[^1] ^= 1;
        Check(good, false);
        // 压缩帧每个截断点和尾随数据都必须被拒绝。
        using (var packed = new MemoryStream())
        {
            using (var compressor = new BrotliStream(packed, CompressionMode.Compress, true)) compressor.Write(Archive("main", 5));
            byte[] full = packed.ToArray();
            for (int cut = 0; cut < full.Length; cut++)
            {
                using var source = new MemoryStream(full[..cut]);
                using var decoded = new DecodedPayloadStream(source);
                try { decoded.CopyTo(Stream.Null); throw new Exception("截断压缩帧被接受"); }
                catch (Exception error) when (error is InvalidDataException or EndOfStreamException) { count++; }
            }
            using var trailing = new MemoryStream(full.Concat(new byte[] { 0 }).ToArray());
            using var trailingDecoder = new DecodedPayloadStream(trailing);
            try { trailingDecoder.CopyTo(Stream.Null); throw new Exception("压缩尾随数据被接受"); }
            catch (InvalidDataException) { count++; }
        }
        Console.WriteLine($"onefile 解析器：{count} 项通过");
        return 0;
    }
    private static byte[] Archive(string name, ulong size, uint count = 1, bool duplicate = false, byte kind = 1)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes, Encoding.UTF8, true);
        void Text(string value) { byte[] data = Encoding.UTF8.GetBytes(value); writer.Write((uint)data.Length); writer.Write(data); }
        writer.Write("RYUPYOF1"u8);
        Text(name);
        writer.Write(duplicate ? 2u : count);
        for (int i = 0; i < (duplicate ? 2 : 1); i++)
        {
            Text(name); writer.Write(kind); writer.Write(0x1c0u); writer.Write(size); writer.Write("hello"u8);
        }
        writer.Write("ENDOF001"u8);
        return bytes.ToArray();
    }
}
