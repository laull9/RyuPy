// 唯一固定冷签名增量；保留旧1717逐项，不能由候选差分授权未知API。
using A = RyuPy.Runtime.Tests.PyTraceStateArtifactTests;

namespace RyuPy.Runtime.Tests;

public class PyOrdinaryMiAbiContractTests
{
    private const string Signature = "M:RyuPy.Runtime.PyOps:PrepareOrdinaryMiClass:150:[0;0;2](String,Object[])->Object:";
    private static string[][] Groups() =>
    [
        Enumerable.Range(0, 1659).Select(i => "历史签名:" + i).ToArray(),
        PyTraceStateV1ArtifactContract.PublicAdditions(), A.SourcePublicAdditions(),
        A.MetadataPublicAdditions(), A.BuiltinPublicAdditions(), A.XmlPublicAdditions(),
        A.DefaultSourcePublicAdditions(), A.LexicalClassFactoryPublicAdditions(), A.DirectGlobalsPublicAdditions()
    ];
    private static bool Check(string[][] g, string[] mi) => A.PreservesBaselineAbi(g[0],
        g.SelectMany(x => x).Concat(mi).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).Concat(A.CompressionPublicAdditions()).ToArray(), g[1], g[2], g[3], g[4], g[5], g[6], g[7], g[8], mi);

    [Fact]
    public void OneColdAdditionPreservesEveryPrior1717Record()
    {
        var groups = Groups(); var old = groups.SelectMany(x => x).ToArray();
        Assert.Equal(1717, old.Length);
        Assert.Equal(new[] { Signature }, A.OrdinaryMiPublicAdditions());
        Assert.True(Check(groups, [Signature]));
        Assert.False(A.PreservesBaselineAbi(groups[0], old));
        for (int index = 0; index < old.Length; index++)
        {
            Assert.False(A.PreservesBaselineAbi(groups[0], old.Where((_, i) => i != index).Append(Signature).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).Concat(A.CompressionPublicAdditions()).ToArray()));
            var changed = (string[])old.Clone(); changed[index] += "改签";
            Assert.False(A.PreservesBaselineAbi(groups[0], changed.Append(Signature).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).Concat(A.CompressionPublicAdditions()).ToArray()));
        }
    }

    [Fact]
    public void CandidateCannotAuthorizeWrongColdSignatureOrDuplicate()
    {
        var groups = Groups();
        foreach (var replacement in new[] {
            Signature.Replace(":150:", ":134:", StringComparison.Ordinal),
            Signature.Replace("[0;0;2]", "[32;0;2]", StringComparison.Ordinal),
            Signature.Replace("(String,Object[])", "(Object,Object[])", StringComparison.Ordinal),
            Signature.Replace("Object[]", "Object", StringComparison.Ordinal),
            Signature.Replace("->Object", "->String", StringComparison.Ordinal),
            Signature.Replace(":PrepareOrdinaryMiClass:", ":Unknown:", StringComparison.Ordinal) })
            Assert.False(Check(groups, [replacement]));
        Assert.False(Check(groups, []));
        Assert.False(Check(groups, [Signature, Signature]));
        foreach (var group in groups) Assert.False(Check(groups, [group[0]]));
    }
}

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void ProducedMiAssemblyHasExactColdSignatureAndTotal()
    {
        // 实际本次Runtime PE计数/冷签名；历史DLL逐项比对仍由带baseline的S1门负责。
        var actual = PublicAbi(typeof(RyuPy.Runtime.PyOps).Assembly.Location);
        Assert.Equal(CurrentPublicAbiCount, actual.Length);
        Assert.Equal(CurrentPublicAbiCount, actual.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(OrdinaryMiPublicAdditions(), actual.Where(x => x.Contains(":PrepareOrdinaryMiClass:", StringComparison.Ordinal)).ToArray());
    }
}
