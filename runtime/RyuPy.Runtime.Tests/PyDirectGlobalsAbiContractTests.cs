// 独立唯一冷helper许可；不扩S1具名集合，不从候选差分产生允许表。
using System;
using System.Linq;
using Xunit;
using A = RyuPy.Runtime.Tests.PyTraceStateArtifactTests;
namespace RyuPy.Runtime.Tests;

public class PyDirectGlobalsAbiContractTests
{
    private const string Signature = "M:RyuPy.Runtime.PyModuleInstances:CallDirectGlobals:150:[0;0;2](Object,Object)->Object:";
    private static string[] Before() => Enumerable.Range(0, 1659).Select(i => "历史签名:" + i).ToArray();
    private static string[][] Groups(string[] before) =>
    [
        before, PyTraceStateV1ArtifactContract.PublicAdditions(), A.SourcePublicAdditions(),
        A.MetadataPublicAdditions(), A.BuiltinPublicAdditions(), A.XmlPublicAdditions(),
        A.DefaultSourcePublicAdditions(), A.LexicalClassFactoryPublicAdditions()
    ];
    private static bool Check(string[][] groups, string[] additions)
        => A.PreservesBaselineAbi(groups[0], groups.SelectMany(x => x).Concat(additions).Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).ToArray(),
            groups[1], groups[2], groups[3], groups[4], groups[5], groups[6], groups[7], additions);

    [Fact]
    public void ExactColdAdditionPreservesAll1716PriorRecords()
    {
        var groups = Groups(Before());
        var old = groups.SelectMany(x => x).ToArray();
        Assert.Equal(1716, old.Length);
        Assert.Equal(new[] { Signature }, A.DirectGlobalsPublicAdditions());
        Assert.True(Check(groups, [Signature]));
        Assert.True(A.PreservesBaselineAbi(groups[0], old.Append(Signature).Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).Reverse().ToArray()));
        Assert.False(A.PreservesBaselineAbi(groups[0], old.Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).ToArray()));
        for (int index = 0; index < old.Length; index++)
        {
            Assert.False(A.PreservesBaselineAbi(groups[0], old.Where((_, i) => i != index).Append(Signature).Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).ToArray()));
            var changed = (string[])old.Clone(); changed[index] += "改签";
            Assert.False(A.PreservesBaselineAbi(groups[0], changed.Append(Signature).Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).ToArray()));
        }
    }

    [Fact]
    public void ColdAdditionRejectsMissingChangedFlagsSignaturesAndOverloads()
    {
        var groups = Groups(Before());
        foreach (var replacement in new[] {
            Signature.Replace(":150:", ":134:", StringComparison.Ordinal),
            Signature.Replace("[0;0;2]", "[32;0;2]", StringComparison.Ordinal),
            Signature.Replace("(Object,Object)", "(Object,String)", StringComparison.Ordinal),
            Signature.Replace("->Object", "->String", StringComparison.Ordinal),
            Signature.Replace(":CallDirectGlobals:", ":Other:", StringComparison.Ordinal) })
        {
            Assert.NotEqual(Signature, replacement);
            Assert.False(Check(groups, [replacement]));
        }
        Assert.False(Check(groups, []));
        Assert.False(Check(groups, [Signature, Signature]));
        Assert.False(Check(groups, [Signature, Signature.Replace("(Object,Object)", "(Object)", StringComparison.Ordinal)]));
    }

    [Fact]
    public void CandidateMatchingForgedPermissionCannotCrossGroupDuplicate()
    {
        var groups = Groups(Before());
        foreach (var group in groups) Assert.False(Check(groups, [group[0]]));
        for (int index = 0; index < groups.Length; index++)
        {
            var changed = groups.Select(x => (string[])x.Clone()).ToArray();
            changed[index][0] = Signature;
            Assert.False(Check(changed, [Signature]));
        }
    }
}
