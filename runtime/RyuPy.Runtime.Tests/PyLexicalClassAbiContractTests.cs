// 独立固定六项许可的合成反例；真实PE仍由原S1门逐字段读取并验当前总数，不以本类替代。
using System;
using System.Linq;
using Xunit;
using A = RyuPy.Runtime.Tests.PyTraceStateArtifactTests;

namespace RyuPy.Runtime.Tests;

public class PyLexicalClassAbiContractTests
{
    private static string[] Baseline() => Enumerable.Range(0, 1659).Select(i => "历史签名:" + i).ToArray();
    private static string[][] Groups(string[] before) =>
    [
        before, PyTraceStateV1ArtifactContract.PublicAdditions(), A.SourcePublicAdditions(),
        A.MetadataPublicAdditions(), A.BuiltinPublicAdditions(), A.XmlPublicAdditions(), A.DefaultSourcePublicAdditions()
    ];
    private static string[] After(string[] before, string[] factory) => Groups(before).SelectMany(x => x).Concat(factory).Concat(A.DirectGlobalsPublicAdditions()).Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).Concat(A.CompressionPublicAdditions()).ToArray();
    private static bool WithPermission(string[] before, string[] factory)
    {
        var groups = Groups(before);
        return A.PreservesBaselineAbi(before, After(before, factory),
            groups[1], groups[2], groups[3], groups[4], groups[5], groups[6], factory);
    }

    [Fact]
    public void FixedSixRequireExact1716AndPreserveHistorical1659()
    {
        var before = Baseline(); var factory = A.LexicalClassFactoryPublicAdditions();
        Assert.Equal(6, factory.Length); Assert.Equal(6, factory.Distinct(StringComparer.Ordinal).Count());
        Assert.Single(factory.Where(x => x.StartsWith("T:", StringComparison.Ordinal)));
        Assert.Equal(5, factory.Count(x => x.StartsWith("M:", StringComparison.Ordinal)));
        var old = After(before, []); Assert.Equal(A.CurrentPublicAbiCount - 6, old.Length);
        var after = After(before, factory); Assert.Equal(A.CurrentPublicAbiCount, after.Length);
        Assert.True(A.PreservesBaselineAbi(before, after));
        Assert.True(A.PreservesBaselineAbi(before.Reverse().ToArray(), after.Reverse().ToArray()));
        Assert.False(A.PreservesBaselineAbi(before, old));
        Assert.False(A.PreservesBaselineAbi(before, After(before, factory.Skip(1).ToArray())));
        Assert.False(A.PreservesBaselineAbi(before.Skip(1).ToArray(), after));
    }

    [Fact]
    public void EveryFactoryEntryRejectsDeletionChangedSignatureDuplicateAndUnknownApi()
    {
        var before = Baseline(); var factory = A.LexicalClassFactoryPublicAdditions();
        for (int index = 0; index < factory.Length; index++)
        {
            Assert.False(A.PreservesBaselineAbi(before, After(before, factory.Where((_, i) => i != index).ToArray())));
            var changed = (string[])factory.Clone(); changed[index] += "改签";
            Assert.False(A.PreservesBaselineAbi(before, After(before, changed)));
            changed[index] = factory[(index + 1) % factory.Length];
            Assert.False(A.PreservesBaselineAbi(before, After(before, changed)));
        }
        foreach (string unknown in new[] { "M:未知方法", "M:未知重载", "T:未知类型" })
        {
            Assert.False(A.PreservesBaselineAbi(before, After(before, factory).Append(unknown).ToArray()));
            var changed = (string[])factory.Clone(); changed[0] = unknown;
            Assert.False(WithPermission(before, changed));
        }
    }

    [Fact]
    public void MatchingCandidateCannotAuthorizeWrongFlagsSignaturesOrCrossGroupDuplicates()
    {
        var before = Baseline(); var factory = A.LexicalClassFactoryPublicAdditions();
        foreach (var mutation in new (int Index, string Old, string New)[] {
            (0, "1048961", "1048960"), (0, "1048961", "1048833"), // visibility/abstract
            (0, "1048961", "1048705"), (0, "1048961", "385"), // sealed/beforefieldinit
            (0, "System.Object", "System.Exception"),
            (1, ":150:", ":134:"), (1, "[0;0;4]", "[32;0;4]"), (1, "[0;0;4]", "[0;0;3]"),
            (1, "(String,String", "(Object,String"), (1, "Object[]", "Object"),
            (2, "->Boolean", "->Object"), (3, "->Object", "->String"),
            (4, "->Void", "->Object"), (5, ":Finish:", ":Unknown:") })
        {
            var changed = (string[])factory.Clone();
            changed[mutation.Index] = changed[mutation.Index].Replace(mutation.Old, mutation.New, StringComparison.Ordinal);
            Assert.False(factory.SequenceEqual(changed));
            Assert.False(A.PreservesBaselineAbi(before, After(before, changed)));
            Assert.False(WithPermission(before, changed));
        }
        Assert.False(WithPermission(before, []));
        Assert.False(WithPermission(before, factory.Concat(factory).ToArray()));
        foreach (var group in Groups(before).Append(factory))
        {
            var changed = (string[])factory.Clone(); changed[0] = group[^1];
            Assert.False(WithPermission(before, changed));
        }
    }

    [Fact]
    public void OriginalGroupsCannotBeReplacedByNewFactoryRecords()
    {
        var before = Baseline(); var factory = A.LexicalClassFactoryPublicAdditions();
        var groups = Groups(before);
        for (int index = 0; index < groups.Length; index++)
        {
            var changed = groups.Select(x => (string[])x.Clone()).ToArray();
            changed[index][0] = factory[0];
            var after = changed.SelectMany(x => x).Concat(factory).Concat(A.DirectGlobalsPublicAdditions()).Concat(A.OrdinaryMiPublicAdditions()).Concat(A.RuntimeDerivedPublicAdditions()).Concat(A.ResourceContextPublicAdditions()).Concat(A.NativeAbiPublicAdditions()).Concat(A.CompressionPublicAdditions()).ToArray();
            Assert.False(A.PreservesBaselineAbi(changed[0], after,
                changed[1], changed[2], changed[3], changed[4], changed[5], changed[6], factory));
        }
    }
}
