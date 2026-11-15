// records守卫ABI精确增量：不修改既有历史/provider时代判定器，不开放任意成员或签名。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] RecordsGuardPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyRecordsIncrement:1048961:System.Object:",
        "M:RyuPy.Runtime.PyRecordsIncrement:TryMaterialized:150:[0;0;2](Object,Object)->Boolean:"
    ];

    internal static bool PreservesRecordsGuardBaselineAbi(string[] before, string[] after)
    {
        string[] additions = RecordsGuardPublicAdditions();
        if (after.Length != after.Distinct(StringComparer.Ordinal).Count()
            || additions.Any(item => !after.Contains(item, StringComparer.Ordinal))) return false;
        return PreservesProviderBaselineAbi(before, after.Except(additions, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void RecordsGuardCompiledAbiMatchesExactDeclaredIncrement()
    {
        string[] actual = PublicAbi(typeof(RyuPy.Runtime.PyRecordsIncrement).Assembly.Location);
        AssertPublishedAbiSignatures(actual);
        bool Ours(string item) => item.Contains("RyuPy.Runtime.PyRecordsIncrement", StringComparison.Ordinal);
        Assert.Equal(RecordsGuardPublicAdditions().Order(StringComparer.Ordinal),
            actual.Where(Ours).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RecordsGuardIncrementRejectsMissingChangedDuplicateAndUnknownEntries()
    {
        string[] before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        string[] old = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions())
            .Concat(SourcePublicAdditions()).Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions())
            .Concat(XmlPublicAdditions()).Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions())
            .Concat(ResourceContextPublicAdditions()).Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions())
            .Concat(BinaryCryptoPublicAdditions()).Concat(ProviderPublicAdditions()).ToArray();
        string[] complete = old.Concat(RecordsGuardPublicAdditions()).ToArray();
        Assert.True(PreservesRecordsGuardBaselineAbi(before, complete));
        Assert.False(PreservesRecordsGuardBaselineAbi(before, old));
        for (int index = 0; index < complete.Length; index++)
        {
            Assert.False(PreservesRecordsGuardBaselineAbi(before, complete.Where((_, at) => at != index).ToArray()));
            string[] changed = (string[])complete.Clone(); changed[index] += "改签";
            Assert.False(PreservesRecordsGuardBaselineAbi(before, changed));
        }
        Assert.False(PreservesRecordsGuardBaselineAbi(before, complete.Append(complete[^1]).ToArray()));
        Assert.False(PreservesRecordsGuardBaselineAbi(before, complete.Append("M:未知records入口").ToArray()));
    }
}
