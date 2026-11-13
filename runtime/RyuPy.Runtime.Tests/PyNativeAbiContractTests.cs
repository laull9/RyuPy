// ADR0173 的十五条公开桥接合同：手写精确元数据，并验证缺项、改签、重复和未知项均被拒绝。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal const int CurrentPublicAbiCount = 1744;

    internal static string[] NativeAbiPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyNativeCall:1048833:System.Object:",
        "I:RyuPy.Runtime.PyNativeCall:System.IDisposable",
        "M:RyuPy.Runtime.PyNativeCall:Prepare:150:[0;0;4](Object[],String,String,String)->RyuPy.Runtime.PyNativeCall:",
        "M:RyuPy.Runtime.PyNativeCall:Signed:134:[32;0;1](Int32)->Int64:",
        "M:RyuPy.Runtime.PyNativeCall:Unsigned:134:[32;0;1](Int32)->UInt64:",
        "M:RyuPy.Runtime.PyNativeCall:Floating:134:[32;0;1](Int32)->Double:",
        "M:RyuPy.Runtime.PyNativeCall:Boolean:134:[32;0;1](Int32)->Byte:",
        "M:RyuPy.Runtime.PyNativeCall:Pointer:134:[32;0;1](Int32)->IntPtr:",
        "M:RyuPy.Runtime.PyNativeCall:Length:134:[32;0;1](Int32)->UIntPtr:",
        "M:RyuPy.Runtime.PyNativeCall:Dispose:486:[32;0;0]()->Void:",
        "M:RyuPy.Runtime.PyNativeBindings:CreateAbi:150:[0;0;3](String,String,System.Func`2<Object[],Object>)->RyuPy.Runtime.PyFunction:",
        "M:RyuPy.Runtime.PyNativeBindings:BoxSigned:150:[0;0;1](Int64)->Object:",
        "M:RyuPy.Runtime.PyNativeBindings:BoxUnsigned:150:[0;0;1](UInt64)->Object:",
        "M:RyuPy.Runtime.PyNativeBindings:BoxFloating:150:[0;0;1](Double)->Object:",
        "M:RyuPy.Runtime.PyNativeBindings:BoxBoolean:150:[0;0;1](Byte)->Object:"
    ];

    [Fact]
    public void ProducedNativeAbiBridgeHasExactPublicSignatures()
    {
        var actual = PublicAbi(typeof(RyuPy.Runtime.PyNativeCall).Assembly.Location);
        Assert.Equal(CurrentPublicAbiCount, actual.Length);
        Assert.Equal(CurrentPublicAbiCount, actual.Distinct(StringComparer.Ordinal).Count());
        var bridge = actual.Where(signature => signature.Contains(":RyuPy.Runtime.PyNativeCall:", StringComparison.Ordinal)
            || signature.StartsWith("M:RyuPy.Runtime.PyNativeBindings:CreateAbi:", StringComparison.Ordinal)
            || signature.StartsWith("M:RyuPy.Runtime.PyNativeBindings:Box", StringComparison.Ordinal)).ToArray();
        Assert.Equal(NativeAbiPublicAdditions().OrderBy(signature => signature, StringComparer.Ordinal), bridge);
    }

    [Fact]
    public void NativeAbiBridgeRequiresEveryApprovedEntryAndPreservesHistory()
    {
        var before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        var old = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions()).Concat(SourcePublicAdditions())
            .Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions()).Concat(XmlPublicAdditions())
            .Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions())
            .Concat(RuntimeDerivedPublicAdditions()).Concat(ResourceContextPublicAdditions()).ToArray();
        Assert.Equal(1722, old.Length);
        var additions = NativeAbiPublicAdditions();
        Assert.Equal(15, additions.Length);
        var complete = old.Concat(additions).Concat(CompressionPublicAdditions()).ToArray();
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
        Assert.False(PreservesBaselineAbi(before, complete.Append("M:未知桥接重载").ToArray()));
        Assert.False(PreservesBaselineAbi(before, complete.Append(additions[0]).ToArray()));
    }
}
