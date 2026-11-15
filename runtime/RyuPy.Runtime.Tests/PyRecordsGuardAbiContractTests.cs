// records守卫ABI精确增量：不修改既有历史/provider时代判定器，不开放任意成员或签名。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] RecordsGuardPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyRecordsIncrement:1048961:System.Object:",
        "M:RyuPy.Runtime.PyRecordsIncrement:TryMaterialized:150:[0;0;2](Object,Object)->Boolean:"
    ];

    // 固定81758bb dev已存在的九条；必须由独立dev DLL门验证，不从候选差集自动放行。
    internal static string[] RecordsDevPublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyGenerator:.ctor:6278:[32;0;6](RyuPy.Runtime.PyResume,Object,Int64,String,String,Object)->Void:",
        "M:RyuPy.Runtime.PyOps:GeneratorStateData:150:[0;0;1](Object)->Object:",
        "M:RyuPy.Runtime.PyOps:GeneratorStateGetPC:150:[0;0;1](Object)->Int64:",
        "M:RyuPy.Runtime.PyOps:GeneratorStateSetPC:150:[0;0;2](Object,Int64)->Void:",
        "M:RyuPy.Runtime.PyOps:GuardedListSetFalseLoop:150:[0;0;4](Object,Object,Object,Object)->Object:",
        "M:RyuPy.Runtime.PyOps:EnumerateCurrentIndex:150:[0;0;1](Object)->Object:",
        "M:RyuPy.Runtime.PyOps:EnumerateCurrentValue:150:[0;0;1](Object)->Object:",
        "M:RyuPy.Runtime.PyOps:IsEnumerateIterator:150:[0;0;1](Object)->Boolean:",
        "M:RyuPy.Runtime.PyOps:MakeComprehensionList:150:[0;0;1](Object)->Object:"
    ];

    // d9044214 records探针引入，绝不是dev既有项；实现的泛型参数虽internal，接口行仍可见。
    internal static string[] RecordsProbePublicAdditions() =>
    ["I:RyuPy.Runtime.PyKeyComparer:System.Collections.Generic.IAlternateEqualityComparer`2<RyuPy.Runtime.PyDictStringPair,Object>"];

    internal static bool PreservesRecordsDevBaselineAbi(string[] before, string[] after)
    {
        string[] additions = RecordsDevPublicAdditions();
        if (after.Length != after.Distinct(StringComparer.Ordinal).Count()
            || additions.Any(item => !after.Contains(item, StringComparer.Ordinal))) return false;
        return PreservesProviderBaselineAbi(before, after.Except(additions, StringComparer.Ordinal).ToArray());
    }

    [TraceBaselineFact]
    public void PinnedRecordsDevPublicAbiProvesNinePreexistingAdditions()
    {
        string baseline = Environment.GetEnvironmentVariable("RYUPY_TRACE_BASELINE_DLL")!;
        string dev = Environment.GetEnvironmentVariable("RYUPY_RECORDS_DEV_BASELINE_DLL")!;
        Assert.True(File.Exists(baseline)); Assert.True(File.Exists(dev));
        string[] before = PublicAbi(baseline); string[] after = PublicAbi(dev);
        Evidence("records-dev-abi.json", new { baseline, dev, baselineSignatures = before.Length,
            devSignatures = after.Length, devAdditions = RecordsDevPublicAdditions(), actual = after,
            baselineBytes = new FileInfo(baseline).Length, devBytes = new FileInfo(dev).Length,
            removed = before.Except(after).ToArray(), probePresent = after.Contains(RecordsProbePublicAdditions()[0]) });
        Assert.False(after.Contains(RecordsProbePublicAdditions()[0]));
        Assert.DoesNotContain(after, x => x.Contains("PyRecordsIncrement", StringComparison.Ordinal));
        Assert.True(PreservesRecordsDevBaselineAbi(before, after));
    }

    internal static bool PreservesRecordsGuardBaselineAbi(string[] before, string[] after)
    {
        string[] additions = RecordsGuardPublicAdditions().Concat(RecordsProbePublicAdditions()).ToArray();
        if (after.Length != after.Distinct(StringComparer.Ordinal).Count()
            || additions.Any(item => !after.Contains(item, StringComparer.Ordinal))) return false;
        return PreservesRecordsDevBaselineAbi(before, after.Except(additions, StringComparer.Ordinal).ToArray());
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
        Assert.True(PreservesProviderBaselineAbi(before, old));
        string[] dev = old.Concat(RecordsDevPublicAdditions()).ToArray();
        Assert.True(PreservesRecordsDevBaselineAbi(before, dev));
        Assert.False(PreservesProviderBaselineAbi(before, dev));
        for (int index = 0; index < dev.Length; index++)
        {
            Assert.False(PreservesRecordsDevBaselineAbi(before, dev.Where((_, at) => at != index).ToArray()));
            string[] changedDev = (string[])dev.Clone(); changedDev[index] += "改签";
            Assert.False(PreservesRecordsDevBaselineAbi(before, changedDev));
        }
        Assert.False(PreservesRecordsDevBaselineAbi(before, dev.Append(dev[^1]).ToArray()));
        Assert.False(PreservesRecordsDevBaselineAbi(before, dev.Concat(RecordsProbePublicAdditions()).ToArray()));
        string[] complete = dev.Concat(RecordsProbePublicAdditions()).Concat(RecordsGuardPublicAdditions()).ToArray();
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
