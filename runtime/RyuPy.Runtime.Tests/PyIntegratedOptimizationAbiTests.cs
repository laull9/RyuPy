// 组合生成器与records候选后仍精确保护ABI；不从候选差集自动放行增量。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] IntegratedOptimizationPublicAdditions() => GeneratorSumPublicAdditions()
        .Concat(RecordsGuardPublicAdditions()).Concat(RecordsProbePublicAdditions()).ToArray();

    internal static bool ExactIntegratedOptimizationIncrement(string[] before, string[] after)
    {
        string[] expected = before.Concat(IntegratedOptimizationPublicAdditions()).ToArray();
        return before.Length > 0 && before.Distinct(StringComparer.Ordinal).Count() == before.Length
            && expected.Distinct(StringComparer.Ordinal).Count() == expected.Length
            && after.Distinct(StringComparer.Ordinal).Count() == after.Length
            && expected.Order(StringComparer.Ordinal).SequenceEqual(after.Order(StringComparer.Ordinal));
    }

    internal static bool PreservesIntegratedOptimizationBaselineAbi(string[] before, string[] after)
    {
        string[] generator = GeneratorSumPublicAdditions();
        return after.Distinct(StringComparer.Ordinal).Count() == after.Length
            && generator.All(item => after.Contains(item, StringComparer.Ordinal))
            && PreservesRecordsGuardBaselineAbi(before, after.Except(generator, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void IntegratedIncrementRejectsMissingChangedDuplicateAndUnknownMembers()
    {
        string[] before = ["固定dev成员:一", "固定dev成员:二"];
        string[] after = before.Concat(IntegratedOptimizationPublicAdditions()).ToArray();
        Assert.Equal(11, IntegratedOptimizationPublicAdditions().Length);
        Assert.True(ExactIntegratedOptimizationIncrement(before, after));
        for (int index = 0; index < after.Length; index++)
        {
            Assert.False(ExactIntegratedOptimizationIncrement(before, after.Where((_, at) => at != index).ToArray()));
            string[] changed = (string[])after.Clone(); changed[index] += "改签";
            Assert.False(ExactIntegratedOptimizationIncrement(before, changed));
        }
        Assert.False(ExactIntegratedOptimizationIncrement(before, after.Append(after[0]).ToArray()));
        Assert.False(ExactIntegratedOptimizationIncrement(before, after.Append("未知成员").ToArray()));
        Assert.False(ExactIntegratedOptimizationIncrement([], IntegratedOptimizationPublicAdditions()));
    }

    [Fact]
    public void IntegratedHistoricalIncrementRejectsMissingChangedDuplicateAndUnknownMembers()
    {
        string[] before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        string[] after = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions())
            .Concat(SourcePublicAdditions()).Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions())
            .Concat(XmlPublicAdditions()).Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions())
            .Concat(ResourceContextPublicAdditions()).Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions())
            .Concat(BinaryCryptoPublicAdditions()).Concat(ProviderPublicAdditions()).Concat(RecordsDevPublicAdditions())
            .Concat(IntegratedOptimizationPublicAdditions()).ToArray();
        Assert.True(PreservesIntegratedOptimizationBaselineAbi(before, after));
        for (int index = 0; index < after.Length; index++)
        {
            Assert.False(PreservesIntegratedOptimizationBaselineAbi(before, after.Where((_, at) => at != index).ToArray()));
            string[] changed = (string[])after.Clone(); changed[index] += "改签";
            Assert.False(PreservesIntegratedOptimizationBaselineAbi(before, changed));
        }
        Assert.False(PreservesIntegratedOptimizationBaselineAbi(before, after.Append(after[0]).ToArray()));
        Assert.False(PreservesIntegratedOptimizationBaselineAbi(before, after.Append("未知成员").ToArray()));
    }

}
