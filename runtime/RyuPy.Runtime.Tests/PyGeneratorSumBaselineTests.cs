// 与冻结dev真实DLL逐项比较；不把较新dev当作1659条旧trace历史合同的替代品。
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
#if GENERATOR_ABI_BASELINE_TEST
    private const string GeneratorBaselineCommit = "81758bb477729aee4636f2ca6d6b0815b949849a";

#endif

    private static bool ExactGeneratorIncrement(string[] before, string[] after)
    {
        string[] additions = GeneratorSumPublicAdditions();
        string[] expected = before.Concat(additions).ToArray();
        return before.Length > 0 && before.Distinct(StringComparer.Ordinal).Count() == before.Length
            && expected.Distinct(StringComparer.Ordinal).Count() == expected.Length
            && after.Distinct(StringComparer.Ordinal).Count() == after.Length
            && expected.Order(StringComparer.Ordinal).SequenceEqual(after.Order(StringComparer.Ordinal));
    }

    // 仅专用构建包含真实历史门，默认测试不新增skip。
#if GENERATOR_ABI_BASELINE_TEST
    [Fact]
    public void ProducedGeneratorAssemblyPreservesFrozenDevAbiExactly()
    {
        string manifestPath = Environment.GetEnvironmentVariable("RYUPY_GENERATOR_BASELINE_MANIFEST")!;
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var record = manifest.RootElement;
        Assert.Equal(GeneratorBaselineCommit, record.GetProperty("commit").GetString());
        Assert.Equal("10.0.403", record.GetProperty("sdk").GetString());
        Assert.Equal("Release", record.GetProperty("configuration").GetString());
        string baseline = record.GetProperty("dll").GetString()!;
        string hash = record.GetProperty("sha256").GetString()!;
        Assert.True(Path.IsPathFullyQualified(baseline));
        Assert.Equal(64, hash.Length);
        byte[] beforeBytes = File.ReadAllBytes(baseline);
        Assert.Equal(record.GetProperty("bytes").GetInt64(), beforeBytes.LongLength);
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(beforeBytes)).ToLowerInvariant());
        string candidate = typeof(PyGenerator).Assembly.Location;
        Assert.NotEqual(Path.GetFullPath(baseline), Path.GetFullPath(candidate));
        byte[] candidateBytes = File.ReadAllBytes(candidate);
        string evidence = Environment.GetEnvironmentVariable("RYUPY_TRACE_EVIDENCE_DIR")
            ?? throw new InvalidOperationException("历史ABI门必须提供证据目录");
        Directory.CreateDirectory(evidence);
        const string archiveName = "generator-loaded-candidate.dll";
        // 归档实际加载的程序集，不猜测bin/Release另一份输出；新文件防止混入旧轮工件。
        using (var archive = new FileStream(Path.Combine(evidence, archiveName), FileMode.CreateNew, FileAccess.Write))
            archive.Write(candidateBytes);
        string[] before = PublicAbi(baseline), after = PublicAbi(candidate);
        // 防止读取过程中共享输出被另一构建覆盖；比较的必须是manifest所指同一DLL。
        Assert.Equal(beforeBytes, File.ReadAllBytes(baseline));
        Assert.Equal(candidateBytes, File.ReadAllBytes(candidate));
        Evidence("generator-frozen-dev-abi.json", new {
            commit = GeneratorBaselineCommit, sdk = "10.0.403", baseline, sha256 = hash,
            baselineManifest = Path.GetFullPath(manifestPath), baselineBytes = beforeBytes.LongLength,
            candidate, candidateArchive = archiveName, candidateBytes = candidateBytes.LongLength,
            candidateSha256 = Convert.ToHexString(SHA256.HashData(candidateBytes)).ToLowerInvariant(),
            oldSignatures = before.Length, candidateSignatures = after.Length,
            additions = IntegratedOptimizationPublicAdditions(),
            removed = before.Except(after).ToArray(), added = after.Except(before).ToArray(),
            expected = before.Concat(IntegratedOptimizationPublicAdditions()).Order(StringComparer.Ordinal).ToArray(), actual = after,
            scope = "冻结dev公开ABI字段逐项保留及精确11条增量；不替代更早1659历史基线测试"
        });
        Assert.True(ExactIntegratedOptimizationIncrement(before, after), "冻结dev ABI不是精确11条增量，见真实PE证据。");
    }

#endif

    [Fact]
    public void FrozenDevIncrementRejectsRemovalChangesDuplicateAndUnknownMembers()
    {
        string[] before = ["历史成员:一", "历史成员:二"];
        string[] after = before.Concat(GeneratorSumPublicAdditions()).ToArray();
        Assert.True(ExactGeneratorIncrement(before, after));
        for (int i = 0; i < after.Length; i++)
        {
            Assert.False(ExactGeneratorIncrement(before, after.Where((_, at) => at != i).ToArray()));
            string[] changed = (string[])after.Clone(); changed[i] += "改签";
            Assert.False(ExactGeneratorIncrement(before, changed));
        }
        Assert.False(ExactGeneratorIncrement(before, after.Append(after[0]).ToArray()));
        Assert.False(ExactGeneratorIncrement(before, after.Append("未知成员").ToArray()));
        Assert.False(ExactGeneratorIncrement([], GeneratorSumPublicAdditions()));
    }
}
