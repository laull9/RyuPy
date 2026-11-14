// 构建期流式压缩私有归档；压缩器不进入交付程序。
using System.IO;
using System.IO.Compression;
using var input = File.OpenRead("payload.raw");
using var output = File.Create("payload.br");
using var compressor = new BrotliStream(output, CompressionLevel.Optimal);
input.CopyTo(compressor);
