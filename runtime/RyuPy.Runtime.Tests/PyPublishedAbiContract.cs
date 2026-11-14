// 产品ABI专项保护稳定的手写签名集合；无关模块增加公开合同不会令旧模块计数测试误报。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    private static void AssertPublishedAbiSignatures(string[] actual)
    {
        // 同名重复记录会使许可与签名查询产生歧义，始终拒绝；各专项继续验证自身精确全集。
        Assert.Equal(actual.Length, actual.Distinct(StringComparer.Ordinal).Count());
        string[] required = PyTraceStateV1ArtifactContract.PublicAdditions()
            .Concat(SourcePublicAdditions()).Concat(MetadataPublicAdditions())
            .Concat(BuiltinPublicAdditions()).Concat(XmlPublicAdditions())
            .Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions())
            .Concat(RuntimeDerivedPublicAdditions()).Concat(ResourceContextPublicAdditions())
            .Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions())
            .Concat(BinaryCryptoPublicAdditions()).Concat(ProviderPublicAdditions()).ToArray();
        Assert.Equal(required.Length, required.Distinct(StringComparer.Ordinal).Count());
        foreach (string signature in required) Assert.Contains(signature, actual);
    }
}
