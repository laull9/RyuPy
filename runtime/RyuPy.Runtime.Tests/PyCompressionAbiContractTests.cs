// 压缩模块只公开三个真实异常及构造 ABI；内部压缩与文件帮助类不扩大 CLR 公共表面。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] CompressionPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyZlibError:1048833:RyuPy.Runtime.PyException:",
        "M:RyuPy.Runtime.PyZlibError:.ctor:6278:[32;0;1](String)->Void:",
        "T:RyuPy.Runtime.PyBadGzipFile:1048833:RyuPy.Runtime.PyOSError:",
        "M:RyuPy.Runtime.PyBadGzipFile:.ctor:6278:[32;0;1](String)->Void:",
        "T:RyuPy.Runtime.PyMemoryError:1048577:RyuPy.Runtime.PyException:",
        "M:RyuPy.Runtime.PyMemoryError:.ctor:6278:[32;0;1](String)->Void:",
        "M:RyuPy.Runtime.PyMemoryError:.ctor:6276:[32;0;3](String,String,Object[])->Void:"
    ];

    [Fact]
    public void CompressionPublishesOnlyExactExceptionAbi()
    {
        var actual = PublicAbi(typeof(RyuPy.Runtime.PyZlibError).Assembly.Location);
        Assert.Equal(CurrentPublicAbiCount, actual.Length);
        var additions = actual.Where(signature => new[] { "PyZlibError", "PyBadGzipFile", "PyMemoryError" }
            .Any(name => signature.Contains(":RyuPy.Runtime." + name + ":", StringComparison.Ordinal)));
        Assert.Equal(CompressionPublicAdditions().Order(StringComparer.Ordinal), additions.Order(StringComparer.Ordinal));
        Assert.DoesNotContain(actual, signature => signature.Contains(":RyuPy.Runtime.PyCompression:", StringComparison.Ordinal)
            || signature.Contains(":RyuPy.Runtime.PyGzipFile:", StringComparison.Ordinal));
        var before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        var complete = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions()).Concat(SourcePublicAdditions())
            .Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions()).Concat(XmlPublicAdditions())
            .Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions())
            .Concat(RuntimeDerivedPublicAdditions()).Concat(ResourceContextPublicAdditions())
            .Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions()).ToArray();
        Assert.True(PreservesBaselineAbi(before, complete));
        for (int index = complete.Length - 7; index < complete.Length; index++)
        {
            Assert.False(PreservesBaselineAbi(before, complete.Where((_, position) => position != index).ToArray()));
            var changed = (string[])complete.Clone(); changed[index] += "改签";
            Assert.False(PreservesBaselineAbi(before, changed));
            changed[index] = complete[^1];
            if (index != complete.Length - 1) Assert.False(PreservesBaselineAbi(before, changed));
        }
        Assert.False(PreservesBaselineAbi(before, complete.Append("M:未知压缩入口").ToArray()));
    }
}
