// 手写合同的有限反例；合成记录仅验证判定器，真实 PE 读取另由 artifact 主门和字节变异测试证明。
using C = RyuPy.Runtime.Tests.PyTraceStateV1ArtifactContract;

namespace RyuPy.Runtime.Tests;

public sealed class PyTraceStateV1ArtifactContractTests
{
    [Fact]
    public void PublicIncrementContainsOnlyTheTwelveApprovedRecords()
    {
        var added = C.PublicAdditions();
        Assert.Equal(12, added.Length); Assert.Equal(12, added.Distinct().Count());
        Assert.Single(added.Where(x => x.StartsWith("T:", StringComparison.Ordinal)));
        Assert.Equal(new[] { "Abort", "AllMiss", "Enter", "Exit", "Explicit", "Forward", "GeneratorCatch", "GeneratorDepth", "Handle", "Match", "Park" },
            added.Where(x => x.StartsWith("M:", StringComparison.Ordinal)).Select(x => x.Split(':')[2]).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(added.Where(x => x.StartsWith("M:", StringComparison.Ordinal)), x => Assert.Contains(":150:[0;0;", x));
    }

    [Fact]
    public void AbiRejectsMissingChangedExtraAndDuplicateRecords()
    {
        var before = Enumerable.Range(0, 1659).Select(i => "历史逐字段记录:" + i).ToArray();
        var after = before.Concat(C.PublicAdditions()).ToArray();
        Assert.True(C.PreservesAbi(before, after));
        Assert.False(C.PreservesAbi(before, after.Skip(1).ToArray()));
        foreach (string change in new[] { "返回值改变", "参数改变", "可见性改变", "泛型约束改变" })
        {
            var changed = (string[])after.Clone(); changed[0] += change;
            Assert.False(C.PreservesAbi(before, changed));
        }
        var replacement = (string[])after.Clone(); replacement[^1] = "M:RyuPy.Runtime.PyTraceProduct:Experimental";
        Assert.False(C.PreservesAbi(before, replacement));
        Assert.False(C.PreservesAbi(before, after.Append(after[0]).ToArray()));
        replacement[^1] = after[0]; Assert.False(C.PreservesAbi(before, replacement));
        Assert.False(C.PreservesAbi(before, after.Append("M:RyuPy.Runtime.PyTraceProduct:Enter:错误重载").ToArray()));

        // 复用本反例组验证全程序集门；V1 仍独立拒绝来源增量，S1 的精确测试名单不变。
        var source = PyTraceStateArtifactTests.SourcePublicAdditions();
        Assert.Equal(4, source.Length); Assert.Equal(4, source.Distinct().Count());
        var combined = after.Concat(source).ToArray();
        Assert.False(C.PreservesAbi(before, combined));
        Assert.True(PyTraceStateArtifactTests.PreservesBaselineAbi(before, combined));
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, after));
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, before.Concat(source).ToArray()));
        // 历史代表项与每条 V1/来源增量：缺失、同数量改签、替换成重复项均拒绝。
        foreach (int index in new[] { 0 }.Concat(Enumerable.Range(before.Length, combined.Length - before.Length)))
        {
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before,
                combined.Where((_, at) => at != index).ToArray()));
            var changed = (string[])combined.Clone(); changed[index] += "改签";
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, changed));
            changed[index] = combined[(index + 1) % combined.Length];
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, changed));
        }
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, combined.Append(combined[0]).ToArray()));
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before,
            combined.Append("M:RyuPy.Runtime.PyTraceback:CreateCodeSource:错误重载").ToArray()));
    }

    [Fact]
    public void BridgeRulesRejectUnknownSourcesTargetsSignaturesAndOpcodes()
    {
        var contract = new C();
        string source = C.Method("PyBaseException", "get_Frames", "PyStackFrame[]", true);
        var good = new C.Edge("PyBaseException", source, "IL", "callvirt " + C.Method("PyTraceState", "get_Snapshot", "TraceSnapshot", true));
        Assert.True(contract.Allows(good));
        Assert.False(contract.Allows(good with { Source = C.Method("PyBaseException", ".ctor", "Void", true) }));
        Assert.False(contract.Allows(good with { Target = "callvirt " + C.Method("PyTraceState", "Receive", "TraceResult", true, "TraceReceive") }));
        Assert.False(contract.Allows(good with { Target = good.Target.Replace("callvirt ", "ldftn ", StringComparison.Ordinal) }));
        Assert.False(contract.Allows(good with { Target = good.Target.Replace("TraceSnapshot", "Object", StringComparison.Ordinal) }));
        Assert.False(contract.Allows(good with { Source = C.Method("PyTraceProduct", "Hidden", "Void") }));
        Assert.False(contract.Allows(new("PyIO", C.Method("PyIO", "Guard", "Object", false, "System.Func`1<Object>"), "IL",
            "call PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<PyTypeError>")));
        string depth = C.Method("PyTraceProduct", "GeneratorDepth", "Int64");
        Assert.True(C.AllowsGeneratorStackUse(depth, "callvirt", C.Method("Stack<Int64>", "Peek", "!0", true)));
        Assert.False(C.AllowsGeneratorStackUse(depth, "callvirt", C.Method("Stack<Int64>", "Clear", "Void", true)));
        Assert.False(C.AllowsGeneratorStackUse(depth, "ldftn", C.Method("Stack<Int64>", "Peek", "!0", true)));

        // 三个具体方法可将已有值物化为局部；这不授予其它方法、包装类型或 IL 操作权限。
        var materialized = new[]
        {
            new C.Edge("PyTraceback", C.Method("PyTraceback", "ProductBoundary", "TraceBoundary", false, "TraceEpoch", "Int64"), "local", "TraceBoundary"),
            new C.Edge("PyTraceProduct", C.Method("PyTraceProduct", "Park", "Object", false, "Object", "Int64"), "local", "TraceProductBinding"),
            new C.Edge("PyTraceProduct", C.Method("PyTraceProduct", "Bind", "TraceProductBinding", false, "PyBaseException"), "local", "TraceProductBinding")
        };
        foreach (var local in materialized)
        {
            Assert.True(contract.Allows(local));
            Assert.False(contract.Allows(local with { Source = C.Method(local.Owner, "Unlisted", "Void") }));
            Assert.False(contract.Allows(local with { Owner = "PyTraceProduct", Source = C.Method("PyTraceProduct", "AllMiss", "Void", false, "Object") }));
            Assert.False(contract.Allows(local with { Source = local.Source.Replace("[0;0;", "[32;0;", StringComparison.Ordinal) }));
            Assert.False(contract.Allows(local with { Target = "TraceBirthGrant" }));
            Assert.False(contract.Allows(local with { Target = local.Target + "[]" }));
            Assert.False(contract.Allows(local with { Target = local.Target + "&" }));
            Assert.False(contract.Allows(local with { Kind = "field" }));
            Assert.False(contract.Allows(local with { Kind = "catch" }));
            Assert.False(contract.Allows(local with { Kind = "IL", Target = "box " + local.Target }));
            Assert.False(contract.Allows(local with { Kind = "IL", Target = "ldftn " + C.Method("PyTraceState", "Receive", "TraceResult", true, "TraceReceive") }));
        }
    }

    [Fact]
    public void CarrierTemplatesRejectExtraFieldsAndUnlistedMechanicalMembers()
    {
        var contract = new C();
        Assert.Equal(new[] { "<Boundary>k__BackingField", "<Lineage>k__BackingField", "<Remaining>k__BackingField", "<Scene>k__BackingField" },
            C.CarrierFields("TraceDispatchPark").Select(f => f.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(C.CarrierFields("TraceDispatchPark"), f => Assert.Equal(0x21, f.Attributes));
        Assert.False(contract.KnowsMember("TraceDispatchPark::<History>k__BackingField:ReadOnlyCollection<TraceFrame>", "field"));
        Assert.True(contract.KnowsMember(C.Method("TraceProductBinding", "get_Owner", "PyTraceState", true), "method"));
        Assert.False(contract.KnowsMember(C.Method("TraceProductBinding", "RewriteOwner", "Void", true, "PyTraceState"), "method"));
        Assert.False(contract.KnowsMember(C.Method("TraceProductBinding", "Equals", "Boolean", true, "TraceSnapshot"), "method"));
    }

    [Fact]
    public void TraceTypeMatchingDoesNotTreatNamePrefixesAsIdentity()
    {
        Assert.True(C.MentionsDomain("System.Collections.ObjectModel.ReadOnlyCollection`1<RyuPy.Runtime.TraceFrame>"));
        Assert.True(C.MentionsDomain("RyuPy.Runtime.TraceRequest&"));
        Assert.False(C.MentionsDomain("RyuPy.Runtime.TraceFrameExtra"));
        Assert.False(C.MentionsDomain("Another.Assembly.TraceFrame"));
    }
}
