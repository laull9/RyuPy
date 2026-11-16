// 编码密码标准库只新增 binascii.Error 异常的三条公开 ABI；逐条许可并保护完整历史合同。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] BinaryCryptoPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyBinasciiError:1048577:RyuPy.Runtime.PyValueError:",
        "M:RyuPy.Runtime.PyBinasciiError:.ctor:6278:[32;0;1](String)->Void:",
        "M:RyuPy.Runtime.PyBinasciiError:.ctor:6276:[32;0;3](String,String,Object[])->Void:"
    ];

    [Fact]
    public void ProducedBinaryCryptoAssemblyHasExactExceptionSignatures()
    {
        var actual = PublicAbi(typeof(RyuPy.Runtime.PyBinasciiError).Assembly.Location);
        AssertPublishedAbiSignatures(actual);
        var exception = actual.Where(signature => signature.Contains(":RyuPy.Runtime.PyBinasciiError:", StringComparison.Ordinal));
        Assert.Equal(BinaryCryptoPublicAdditions().OrderBy(signature => signature, StringComparer.Ordinal), exception);
    }

    [Fact]
    public void BinaryCryptoExceptionRequiresExactEntriesAndPreservesHistory()
    {
        var before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        var old = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions()).Concat(SourcePublicAdditions())
            .Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions()).Concat(XmlPublicAdditions())
            .Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions())
            .Concat(RuntimeDerivedPublicAdditions()).Concat(ResourceContextPublicAdditions())
            .Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions()).ToArray();
        Assert.Equal(1744, old.Length);
        var additions = BinaryCryptoPublicAdditions();
        Assert.Equal(3, additions.Length);
        var complete = old.Concat(additions).ToArray();
        Assert.True(PreservesBaselineAbi(before, complete));
        Assert.False(PreservesBaselineAbi(before, old));
        for (int index = 0; index < complete.Length; index++)
        {
            Assert.False(PreservesBaselineAbi(before, complete.Where((_, position) => position != index).ToArray()));
            var changed = (string[])complete.Clone();
            changed[index] += "改签";
            Assert.False(PreservesBaselineAbi(before, changed));
            changed[index] = complete[(index + 1) % complete.Length];
            Assert.False(PreservesBaselineAbi(before, changed));
        }
        Assert.False(PreservesBaselineAbi(before, complete.Append("M:未知编码重载").ToArray()));
        Assert.False(PreservesBaselineAbi(before, complete.Append(additions[0]).ToArray()));
    }
}
