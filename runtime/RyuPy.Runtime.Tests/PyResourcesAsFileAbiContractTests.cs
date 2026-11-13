// as_file复用生成器上下文仅新增两个公开属性写删覆盖；独立手写签名并保护旧1719条完整合同。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] ResourceContextPublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyGeneratorContext:DelAttr:198:[32;0;1](String)->Void:",
        "M:RyuPy.Runtime.PyGeneratorContext:SetAttr:198:[32;0;2](String,Object)->Void:"
    ];

    [Fact]
    public void ProducedResourceContextAssemblyHasOnlyExactPublicOverrides()
    {
        var actual = PublicAbi(typeof(RyuPy.Runtime.PyOps).Assembly.Location);
        Assert.Equal(1721, actual.Length);
        Assert.Equal(1721, actual.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ResourceContextPublicAdditions(), actual.Where(x =>
            x.StartsWith("M:RyuPy.Runtime.PyGeneratorContext:DelAttr:", StringComparison.Ordinal)
            || x.StartsWith("M:RyuPy.Runtime.PyGeneratorContext:SetAttr:", StringComparison.Ordinal)).ToArray());
        Assert.DoesNotContain(actual, x => x.StartsWith("M:RyuPy.Runtime.PyImportlibResources:AsFile:", StringComparison.Ordinal));
    }

    [Fact]
    public void NewOverridesCannotReplaceAnyHistoricalRecordOrAuthorizeUnknownApi()
    {
        var before = Enumerable.Range(0, 1659).Select(i => "历史签名:" + i).ToArray();
        var old = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions()).Concat(SourcePublicAdditions())
            .Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions()).Concat(XmlPublicAdditions())
            .Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions()).ToArray();
        Assert.Equal(1719, old.Length);
        var additions = ResourceContextPublicAdditions();
        var complete = old.Concat(additions).ToArray();
        Assert.True(PreservesBaselineAbi(before, complete));
        for (int i = 0; i < complete.Length; i++)
        {
            Assert.False(PreservesBaselineAbi(before, complete.Where((_, index) => i != index).ToArray()));
            var changed = (string[])complete.Clone(); changed[i] += "改签";
            Assert.False(PreservesBaselineAbi(before, changed));
        }
        Assert.False(PreservesBaselineAbi(before, complete.Append(additions[0]).ToArray()));
        Assert.False(PreservesBaselineAbi(before, old.Concat(additions.Select(x => x.Replace(":198:", ":134:", StringComparison.Ordinal))).ToArray()));
        Assert.False(PreservesBaselineAbi(before, old.Concat(additions.Select(x => x.Replace(":SetAttr:", ":未知方法:", StringComparison.Ordinal))).ToArray()));
    }
}
