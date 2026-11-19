// 批量sum编译ABI精确增量：不修改既有历史/provider时代判定器，不开放任意成员或签名。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] GeneratorSumPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyResumeSumCompiled:257:System.MulticastDelegate:",
        "M:RyuPy.Runtime.PyResumeSumCompiled:.ctor:6278:[32;0;2](Object,IntPtr)->Void:",
        "M:RyuPy.Runtime.PyResumeSumCompiled:Invoke:454:[32;0;2](Object,Object)->Object:",
        "M:RyuPy.Runtime.PyResumeSumCompiled:BeginInvoke:454:[32;0;4](Object,Object,System.AsyncCallback,Object)->System.IAsyncResult:",
        "M:RyuPy.Runtime.PyResumeSumCompiled:EndInvoke:454:[32;0;1](System.IAsyncResult)->Object:",
        "M:RyuPy.Runtime.PyGenerator:.ctor:6278:[32;0;7](RyuPy.Runtime.PyResume,Object,Int64,String,String,Object,RyuPy.Runtime.PyResumeSumCompiled)->Void:",
        "M:RyuPy.Runtime.PyOps:GeneratorSumTryConsume:150:[0;0;2](Object,Object)->Boolean:",
        "M:RyuPy.Runtime.PyOps:GeneratorSumRangeGuard:150:[0;0;1](Object)->Boolean:"
    ];

    internal static bool PreservesGeneratorSumBaselineAbi(string[] before, string[] after)
    {
        string[] additions = GeneratorSumPublicAdditions();
        if (after.Length != after.Distinct(StringComparer.Ordinal).Count()
            || additions.Any(item => !after.Contains(item, StringComparer.Ordinal))) return false;
        return PreservesProviderBaselineAbi(before, after.Except(additions, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void GeneratorSumCompiledAbiMatchesExactDeclaredIncrement()
    {
        string[] actual = PublicAbi(typeof(RyuPy.Runtime.PyResumeSumCompiled).Assembly.Location);
        AssertPublishedAbiSignatures(actual);
        bool Ours(string item) => item.Contains("PyResumeSumCompiled", StringComparison.Ordinal)
            || item.Contains(":GeneratorSumTryConsume:", StringComparison.Ordinal)
            || item.Contains(":GeneratorSumRangeGuard:", StringComparison.Ordinal);
        Assert.Equal(GeneratorSumPublicAdditions().Order(StringComparer.Ordinal),
            actual.Where(Ours).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GeneratorSumIncrementRejectsMissingChangedDuplicateAndUnknownEntries()
    {
        string[] before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        string[] old = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions())
            .Concat(SourcePublicAdditions()).Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions())
            .Concat(XmlPublicAdditions()).Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions())
            .Concat(ResourceContextPublicAdditions()).Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions())
            .Concat(BinaryCryptoPublicAdditions()).Concat(ProviderPublicAdditions()).ToArray();
        string[] complete = old.Concat(GeneratorSumPublicAdditions()).ToArray();
        Assert.True(PreservesGeneratorSumBaselineAbi(before, complete));
        Assert.False(PreservesGeneratorSumBaselineAbi(before, old));
        for (int index = 0; index < complete.Length; index++)
        {
            Assert.False(PreservesGeneratorSumBaselineAbi(before, complete.Where((_, at) => at != index).ToArray()));
            string[] changed = (string[])complete.Clone(); changed[index] += "改签";
            Assert.False(PreservesGeneratorSumBaselineAbi(before, changed));
        }
        Assert.False(PreservesGeneratorSumBaselineAbi(before, complete.Append(complete[^1]).ToArray()));
        Assert.False(PreservesGeneratorSumBaselineAbi(before, complete.Append("M:未知批量入口").ToArray()));
    }
}
