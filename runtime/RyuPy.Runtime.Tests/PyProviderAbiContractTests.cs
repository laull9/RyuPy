// CPython provider中性边界的手写精确ABI；外部执行域扩展不依赖公开成员总数魔数。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    internal static string[] ProviderPublicAdditions() =>
    [
        "T:RyuPy.Runtime.IPyForeignObject:1048737::",
        "I:RyuPy.Runtime.IPyForeignObject:RyuPy.Runtime.IPyNativeCallable",
        "M:RyuPy.Runtime.IPyForeignObject:get_ForeignType:3526:[32;0;0]()->Object:",
        "P:RyuPy.Runtime.IPyForeignObject:ForeignType:0:[40;0;0]()->Object",
        "M:RyuPy.Runtime.IPyForeignObject:ReadScalar:1478:[32;0;1](String)->Object:",
        "M:RyuPy.Runtime.IPyForeignObject:Item:1478:[32;0;1](Object)->Object:",
        "M:RyuPy.Runtime.IPyForeignObject:WriteItem:1478:[32;0;3](Object,Object,Boolean)->Void:",
        "M:RyuPy.Runtime.IPyForeignObject:Iterate:1478:[32;0;0]()->RyuPy.Runtime.IPyIterator:",
        "M:RyuPy.Runtime.IPyForeignObject:Length:1478:[32;0;0]()->Int64:",
        "M:RyuPy.Runtime.IPyForeignObject:Truth:1478:[32;0;0]()->Boolean:",
        "M:RyuPy.Runtime.IPyForeignObject:Hash:1478:[32;0;0]()->Int64:",
        "M:RyuPy.Runtime.IPyForeignObject:Binary:1478:[32;0;3](Int32,Object,Boolean)->Object:",
        "M:RyuPy.Runtime.IPyForeignObject:Compare:1478:[32;0;3](Int32,Object,Boolean)->Boolean:",
        "M:RyuPy.Runtime.IPyForeignObject:RichCompare:1478:[32;0;3](Int32,Object,Boolean)->Object:",
        "M:RyuPy.Runtime.IPyForeignObject:Unary:1478:[32;0;1](Int32)->Object:",
        "M:RyuPy.Runtime.IPyForeignObject:IsInstanceOf:1478:[32;0;1](Object)->Boolean:",
        "M:RyuPy.Runtime.IPyForeignObject:Accepts:1478:[32;0;1](Object)->Boolean:",
        "M:RyuPy.Runtime.IPyForeignObject:get_IsExceptionClass:3526:[32;0;0]()->Boolean:",
        "P:RyuPy.Runtime.IPyForeignObject:IsExceptionClass:0:[40;0;0]()->Boolean",
        "M:RyuPy.Runtime.IPyForeignObject:MatchesException:1478:[32;0;1](RyuPy.Runtime.PyBaseException)->Boolean:",
        "M:RyuPy.Runtime.IPyForeignObject:TracebackFrames:1478:[32;0;0]()->RyuPy.Runtime.PyStackFrame[]:",
        "M:RyuPy.Runtime.IPyForeignObject:SynchronizeException:1478:[32;0;2](RyuPy.Runtime.PyBaseException,Boolean)->Void:",
        "T:RyuPy.Runtime.IPyModuleProvider:1048737::",
        "M:RyuPy.Runtime.IPyModuleProvider:Owns:1478:[32;0;1](String)->Boolean:",
        "M:RyuPy.Runtime.IPyModuleProvider:SystemAttribute:1478:[32;0;1](String)->Object:",
        "M:RyuPy.Runtime.IPyModuleProvider:Import:1478:[32;0;1](String)->RyuPy.Runtime.PyModule:",
        "M:RyuPy.Runtime.IPyModuleProvider:Registered:1478:[32;0;1](String)->Void:",
        "M:RyuPy.Runtime.IPyModuleProvider:Initializing:1478:[32;0;1](RyuPy.Runtime.PyModule)->Void:",
        "M:RyuPy.Runtime.IPyModuleProvider:Failed:1478:[32;0;1](String)->Void:",
        "T:RyuPy.Runtime.PyProvider:1048961:System.Object:",
        "M:RyuPy.Runtime.PyProvider:get_Current:2198:[0;0;0]()->RyuPy.Runtime.IPyModuleProvider:",
        "P:RyuPy.Runtime.PyProvider:Current:0:[8;0;0]()->RyuPy.Runtime.IPyModuleProvider",
        "M:RyuPy.Runtime.PyProvider:Install:150:[0;0;1](RyuPy.Runtime.IPyModuleProvider)->Void:",
        "M:RyuPy.Runtime.PyProvider:Remove:150:[0;0;1](RyuPy.Runtime.IPyModuleProvider)->Void:",
        "M:RyuPy.Runtime.PyProvider:Module:150:[0;0;4](String,RyuPy.Runtime.PyModuleGet,RyuPy.Runtime.PyModuleSet,RyuPy.Runtime.IPyForeignObject)->RyuPy.Runtime.PyModule:",
        "M:RyuPy.Runtime.PyErrors:FromClr:150:[0;0;1](System.Exception)->RyuPy.Runtime.PyBaseException:",
        "M:RyuPy.Runtime.PyErrors:ExchangeHandledException:150:[0;0;1](RyuPy.Runtime.PyBaseException)->Object:",
        "M:RyuPy.Runtime.PyErrors:ReceiveForeign:150:[0;0;1](RyuPy.Runtime.PyBaseException)->RyuPy.Runtime.PyBaseException:",
        "M:RyuPy.Runtime.PyOps:CompareValue:150:[0;0;3](Int32,Object,Object)->Object:",
        "M:RyuPy.Runtime.PyBaseException:BindForeignException:134:[32;0;3](RyuPy.Runtime.IPyForeignObject,Object[],String)->Void:",
        "M:RyuPy.Runtime.PyBaseException:BindForeignChain:134:[32;0;3](RyuPy.Runtime.PyBaseException,RyuPy.Runtime.PyBaseException,Boolean)->Void:",
        "M:RyuPy.Runtime.PyBaseException:get_ForeignObject:2182:[32;0;0]()->RyuPy.Runtime.IPyForeignObject:",
        "M:RyuPy.Runtime.PyBaseException:set_ForeignObject:2182:[32;0;1](RyuPy.Runtime.IPyForeignObject)->Void:",
        "P:RyuPy.Runtime.PyBaseException:ForeignObject:0:[40;0;0]()->RyuPy.Runtime.IPyForeignObject",
        "M:RyuPy.Runtime.PyBaseException:get_ForeignDisplayName:2182:[32;0;0]()->String:",
        "P:RyuPy.Runtime.PyBaseException:ForeignDisplayName:0:[40;0;0]()->String",
        "M:RyuPy.Runtime.PyModule:get_ForeignObject:2182:[32;0;0]()->RyuPy.Runtime.IPyForeignObject:",
        "P:RyuPy.Runtime.PyModule:ForeignObject:0:[40;0;0]()->RyuPy.Runtime.IPyForeignObject",
        "M:RyuPy.Runtime.PyModules:TryCached:150:[0;0;2](String,RyuPy.Runtime.PyModule&)->Boolean:",
        "M:RyuPy.Runtime.PyClass:FreezeExportedException:134:[32;0;0]()->Void:"
    ];

    [Fact]
    public void ProviderNeutralProtocolsHaveExactPublishedSignatures()
    {
        string[] actual = PublicAbi(typeof(RyuPy.Runtime.PyProvider).Assembly.Location);
        string[] required = ProviderPublicAdditions();
        foreach (string signature in required) Assert.Contains(signature, actual);
        bool Protocol(string signature) => new[] { "IPyForeignObject", "IPyModuleProvider", "PyProvider" }
            .Any(name => signature.StartsWith("T:RyuPy.Runtime." + name + ":", StringComparison.Ordinal)
                || signature.Contains(":RyuPy.Runtime." + name + ":", StringComparison.Ordinal));
        Assert.Equal(required.Where(Protocol).Order(StringComparer.Ordinal), actual.Where(Protocol).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CurrentProviderBaselineRejectsEveryMissingChangedDuplicateAndUnknownEntry()
    {
        string[] before = Enumerable.Range(0, 1659).Select(index => "历史签名:" + index).ToArray();
        string[] historical = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions()).Concat(SourcePublicAdditions())
            .Concat(MetadataPublicAdditions()).Concat(BuiltinPublicAdditions()).Concat(XmlPublicAdditions())
            .Concat(DefaultSourcePublicAdditions()).Concat(LexicalClassFactoryPublicAdditions())
            .Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions())
            .Concat(RuntimeDerivedPublicAdditions()).Concat(ResourceContextPublicAdditions())
            .Concat(NativeAbiPublicAdditions()).Concat(CompressionPublicAdditions()).Concat(BinaryCryptoPublicAdditions()).ToArray();
        string[] complete = historical.Concat(ProviderPublicAdditions()).ToArray();
        Assert.True(PreservesProviderBaselineAbi(before, complete));
        Assert.False(PreservesProviderBaselineAbi(before, complete.Skip(1).ToArray()));
        for (int index = historical.Length; index < complete.Length; index++)
        {
            Assert.False(PreservesProviderBaselineAbi(before, complete.Where((_, position) => position != index).ToArray()));
            string[] changed = (string[])complete.Clone(); changed[index] += "改签";
            Assert.False(PreservesProviderBaselineAbi(before, changed));
            changed[index] = complete[index - 1]; Assert.False(PreservesProviderBaselineAbi(before, changed));
        }
        Assert.False(PreservesProviderBaselineAbi(before, complete.Append("M:未知provider入口").ToArray()));
        Assert.False(PreservesProviderBaselineAbi(before, complete.Append(complete[^1]).ToArray()));
    }

    [Fact]
    public void ProviderTraceBridgesAllowOnlyTheirThreeExactEdges()
    {
        var contract = new PyTraceStateV1ArtifactContract();
        string enabled = "call " + PyTraceStateV1ArtifactContract.Method("PyTraceProduct", "get_Enabled", "Boolean");
        string fresh = "call PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<PyBaseException>";
        string fromClr = PyTraceStateV1ArtifactContract.Method("PyErrors", "FromClr", "PyBaseException", false, "System.Exception");
        string receive = PyTraceStateV1ArtifactContract.Method("PyErrors", "ReceiveForeign", "PyBaseException", false, "PyBaseException");
        foreach (var (source, target) in new[] { (fromClr, enabled), (fromClr, fresh), (receive, enabled) })
        {
            var edge = new PyTraceStateV1ArtifactContract.Edge("PyErrors", source, "IL", target);
            Assert.True(contract.Allows(edge));
            Assert.False(contract.Allows(edge with { Source = source.Replace("PyErrors::", "PyErrors::Other", StringComparison.Ordinal) }));
            Assert.False(contract.Allows(edge with { Source = source.Replace("PyBaseException", "Object", StringComparison.Ordinal) }));
            Assert.False(contract.Allows(edge with { Target = target.Replace("call ", "ldftn ", StringComparison.Ordinal) }));
            Assert.False(contract.Allows(edge with { Target = "call " + PyTraceStateV1ArtifactContract.Method("PyTraceProduct", "Terminal", "Void", false, "PyBaseException") }));
        }
        Assert.False(contract.Allows(new("PyErrors", receive, "IL", fresh)));
        string old = PyTraceStateV1ArtifactContract.Method("PyTraceback", "ReportClr", "Void", false, "System.Exception");
        Assert.False(contract.Allows(new("PyTraceback", old, "IL", fresh)));
    }

    [Fact]
    public void ProviderHandledScopeRestoresTheCompleteNestedContextToken()
    {
        var outer = new RyuPy.Runtime.PyValueError("outer");
        var inner = new RyuPy.Runtime.PyTypeError("inner");
        object? original = RyuPy.Runtime.PyErrors.EnterHandler(outer);
        try
        {
            object? outerToken = RyuPy.Runtime.PyErrors.EnterHandler(inner);
            object? nestedToken = RyuPy.Runtime.PyErrors.ExchangeHandledException(null);
            Assert.Null(RyuPy.Runtime.PyErrors.Current);
            RyuPy.Runtime.PyErrors.LeaveHandler(nestedToken); Assert.Same(inner, RyuPy.Runtime.PyErrors.Current);
            RyuPy.Runtime.PyErrors.LeaveHandler(outerToken); Assert.Same(outer, RyuPy.Runtime.PyErrors.Current);
        }
        finally { RyuPy.Runtime.PyErrors.LeaveHandler(original); }
    }
}
