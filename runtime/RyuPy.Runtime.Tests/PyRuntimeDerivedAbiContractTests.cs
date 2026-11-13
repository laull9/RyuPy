// 动态单基仅新增一个固定公开冷ABI；历史1718条与所有组的精确签名仍不可替换。
using A = RyuPy.Runtime.Tests.PyTraceStateArtifactTests;

namespace RyuPy.Runtime.Tests;

public class PyRuntimeDerivedAbiContractTests
{
    private const string Signature = "M:RyuPy.Runtime.PyOps:PrepareRuntimeDerivedClass:150:[0;0;2](String,Object[])->Object:";
    private static string[][] Groups() =>
    [
        Enumerable.Range(0, 1659).Select(i => "历史签名:" + i).ToArray(),
        PyTraceStateV1ArtifactContract.PublicAdditions(), A.SourcePublicAdditions(),
        A.MetadataPublicAdditions(), A.BuiltinPublicAdditions(), A.XmlPublicAdditions(),
        A.DefaultSourcePublicAdditions(), A.LexicalClassFactoryPublicAdditions(),
        A.DirectGlobalsPublicAdditions(), A.OrdinaryMiPublicAdditions()
    ];
    private static bool Check(string[][] g, string[] additions) => A.PreservesBaselineAbi(g[0],
        g.SelectMany(x => x).Concat(additions).Concat(A.ResourceContextPublicAdditions()).ToArray(), g[1], g[2], g[3], g[4], g[5],
        g[6], g[7], g[8], g[9], additions);

    [Fact]
    public void OneAdditionPreservesEveryPrior1718Record()
    {
        var groups = Groups(); var old = groups.SelectMany(x => x).ToArray();
        Assert.Equal(1718, old.Length);
        Assert.Equal(new[] { Signature }, A.RuntimeDerivedPublicAdditions());
        Assert.True(Check(groups, [Signature]));
        Assert.False(A.PreservesBaselineAbi(groups[0], old));
        for (int index = 0; index < old.Length; index++)
        {
            Assert.False(A.PreservesBaselineAbi(groups[0], old.Where((_, i) => i != index).Append(Signature).Concat(A.ResourceContextPublicAdditions()).ToArray()));
            var changed = (string[])old.Clone(); changed[index] += "改签";
            Assert.False(A.PreservesBaselineAbi(groups[0], changed.Append(Signature).Concat(A.ResourceContextPublicAdditions()).ToArray()));
        }
    }

    [Fact]
    public void CandidateCannotAuthorizeMutatedMissingDuplicateOrUnknownApi()
    {
        var groups = Groups();
        foreach (var replacement in new[] {
            Signature.Replace(":150:", ":134:", StringComparison.Ordinal),
            Signature.Replace("[0;0;2]", "[32;0;2]", StringComparison.Ordinal),
            Signature.Replace("[0;0;2]", "[0;0;1]", StringComparison.Ordinal),
            Signature.Replace("(String,Object[])", "(Object,Object[])", StringComparison.Ordinal),
            Signature.Replace("Object[]", "Object", StringComparison.Ordinal),
            Signature.Replace("->Object", "->String", StringComparison.Ordinal),
            Signature.Replace(":PrepareRuntimeDerivedClass:", ":Unknown:", StringComparison.Ordinal) })
            Assert.False(Check(groups, [replacement]));
        Assert.False(Check(groups, []));
        Assert.False(Check(groups, [Signature, Signature]));
        Assert.False(Check(groups, [Signature, "M:未知方法"]));
        foreach (var group in groups) Assert.False(Check(groups, [group[0]]));
    }
}

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void ProducedRuntimeDerivedAssemblyHasExactSingleAdditionAndTotal()
    {
        var actual = PublicAbi(typeof(RyuPy.Runtime.PyOps).Assembly.Location);
        Assert.Equal(1722, actual.Length);
        Assert.Equal(1722, actual.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(RuntimeDerivedPublicAdditions(), actual.Where(x => x.Contains(":PrepareRuntimeDerivedClass:", StringComparison.Ordinal)).ToArray());
    }
}
