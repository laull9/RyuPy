// 解包私有归档：先校验路径、类型、计数及长度，再向独占私有目录写入普通文件。
using System.Text;

internal static class Payload
{
    private const ulong MaxTotal = 16UL * 1024 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    internal static string Extract(Stream compressed, string root, Func<bool> interrupted)
    {
        using var stream = new DecodedPayloadStream(compressed);
        using var reader = new BinaryReader(stream, Utf8, leaveOpen: true);
        Expect(reader, "RYUPYOF1"u8);
        string entry = ReadPath(reader);
        uint count = reader.ReadUInt32();
        if (count == 0 || count > 100_000) throw new InvalidDataException("载荷文件数越界");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directories = new List<(string, uint)>();
        ulong total = 0;
        bool found = false;
        byte[] buffer = new byte[128 * 1024];
        for (uint i = 0; i < count; i++)
        {
            if (interrupted()) throw new OperationCanceledException("单文件启动被中断");
            string name = ReadPath(reader);
            if (!seen.Add(name)) throw new InvalidDataException("载荷路径重复");
            byte kind = reader.ReadByte();
            uint mode = reader.ReadUInt32();
            ulong size = reader.ReadUInt64();
            if (kind > 1 || mode > 0x1ff || size > 4UL * 1024 * 1024 * 1024 || size > MaxTotal - total || (kind == 0 && size != 0))
                throw new InvalidDataException("载荷类型、权限或大小越界");
            total += size;
            string path = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            // 每层都在本轮目录中；归档自身无链接，冲突文件/目录交给独占创建拒绝。
            if (kind == 0)
            {
                Directory.CreateDirectory(path);
                directories.Add((path, mode));
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ulong remaining = size;
                while (remaining > 0)
                {
                    if (interrupted()) throw new OperationCanceledException("单文件启动被中断");
                    int n = stream.Read(buffer, 0, (int)Math.Min((ulong)buffer.Length, remaining));
                    if (n == 0) throw new EndOfStreamException("载荷文件被截断");
                    output.Write(buffer, 0, n);
                    remaining -= (uint)n;
                }
            }
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, (UnixFileMode)mode);
            if (name == entry)
            {
                if (!OperatingSystem.IsWindows() && (mode & 0x49) == 0) throw new InvalidDataException("载荷入口没有执行权限");
                found = true;
            }
        }
        Expect(reader, "ENDOF001"u8);
        if (stream.ReadByte() != -1 || !found) throw new InvalidDataException("载荷尾部或入口无效");
        // 目录最后赋权，允许保留只读目录，同时避免中途封住子文件写入路径。
        if (!OperatingSystem.IsWindows())
            foreach (var (path, mode) in directories.OrderByDescending(x => x.Item1.Length))
                File.SetUnixFileMode(path, (UnixFileMode)mode);
        return Path.Combine(root, entry.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void Expect(BinaryReader reader, ReadOnlySpan<byte> expected)
    {
        if (!reader.ReadBytes(expected.Length).AsSpan().SequenceEqual(expected))
            throw new InvalidDataException("载荷签名或结束标记损坏/截断");
    }
    private static string ReadPath(BinaryReader reader)
    {
        uint length = reader.ReadUInt32();
        if (length == 0 || length > 4096) throw new InvalidDataException("载荷路径长度越界");
        byte[] bytes = reader.ReadBytes((int)length);
        if (bytes.Length != length) throw new EndOfStreamException("载荷路径被截断");
        string name = Utf8.GetString(bytes);
        foreach (string part in name.Split('/'))
        {
            if (part.Length == 0 || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
                part.Any(c => char.IsControl(c) || "\\:*?\"<>|".Contains(c)))
                throw new InvalidDataException("载荷路径越界或含平台歧义字符");
            string stem = part.Split('.')[0].ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9'))
                throw new InvalidDataException("载荷路径含平台保留名");
        }
        return name;
    }
}
