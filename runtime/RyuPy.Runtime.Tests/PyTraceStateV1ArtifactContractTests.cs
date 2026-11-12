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
        var metadata = PyTraceStateArtifactTests.MetadataPublicAdditions();
        Assert.Equal(24, metadata.Length); Assert.Equal(24, metadata.Distinct().Count());
        var builtin = PyTraceStateArtifactTests.BuiltinPublicAdditions();
        var deployment = PyTraceStateArtifactTests.DefaultSourcePublicAdditions();
        var factory = PyTraceStateArtifactTests.LexicalClassFactoryPublicAdditions();
        Assert.Equal(new[] { "M:RyuPy.Runtime.PyOps:LoadDefaultBuiltin:150:[0;0;1](String)->Object:" }, builtin);
        var old1699 = after.Concat(source).Concat(metadata).ToArray();
        Assert.Equal(1699, old1699.Length);
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, old1699.Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray()));
        var old1700 = old1699.Concat(builtin).ToArray();
        Assert.Equal(1700, old1700.Length);
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, old1700.Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray()));
        var old1706 = old1700.Concat(PyTraceStateArtifactTests.XmlPublicAdditions()).ToArray();
        Assert.Equal(1706, old1706.Length);
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, old1706.Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray()));
        Assert.Equal(4, deployment.Length); Assert.Equal(4, deployment.Distinct().Count());
        var combined = old1706.Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray();
        Assert.False(C.PreservesAbi(before, combined));
        Assert.True(PyTraceStateArtifactTests.PreservesBaselineAbi(before, combined));
        Assert.True(PyTraceStateArtifactTests.PreservesBaselineAbi(before.Reverse().ToArray(), combined.Reverse().ToArray()));
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, after.Concat(source).Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray()));
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, after.Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray()));
        Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, before.Concat(source).Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray()));
        // 历史代表项与每条 V1/来源/metadata/默认builtins 增量：缺失、同数量改签、替换成重复项均拒绝。
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
    public void CombinedAbiRejectsInvalidAllowListsAndMetadataFlags()
    {
        var before = Enumerable.Range(0, 1659).Select(i => "历史逐字段记录:" + i).ToArray();
        var v1 = C.PublicAdditions();
        var source = PyTraceStateArtifactTests.SourcePublicAdditions();
        var metadata = PyTraceStateArtifactTests.MetadataPublicAdditions();
        var builtin = PyTraceStateArtifactTests.BuiltinPublicAdditions();
        var xml = PyTraceStateArtifactTests.XmlPublicAdditions();
        var deployment = PyTraceStateArtifactTests.DefaultSourcePublicAdditions();
        var factory = PyTraceStateArtifactTests.LexicalClassFactoryPublicAdditions();
        var groups = new[] { before, v1, source, metadata, builtin, xml, deployment };
        // 各组自身重复及任意两组重叠，即使 after 完全照着错误允许表拼成也不能通过。
        for (int group = 0; group < groups.Length; group++)
        for (int donor = 0; donor < groups.Length; donor++)
        {
            var changed = groups.Select(x => (string[])x.Clone()).ToArray();
            if (group == donor && groups[group].Length == 1) continue;
            changed[group][0] = groups[donor][donor == group ? 1 : 0];
            var after = changed.SelectMany(x => x).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray();
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(
                changed[0], after, changed[1], changed[2], changed[3], changed[4], changed[5], changed[6]));
        }
        // 即使候选跟着错误新表一起改签/重复，也不得接受新的任意 API 或重载。
        foreach (var wrong in new[] { Array.Empty<string>(), builtin.Concat(builtin).ToArray(),
            new[] { builtin[0].Replace("(String)->Object", "(Object)->Object", StringComparison.Ordinal) },
            new[] { builtin[0].Replace("(String)->Object", "(String)->String", StringComparison.Ordinal) },
            new[] { builtin[0].Replace(":150:", ":134:", StringComparison.Ordinal) } })
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before,
                before.Concat(v1).Concat(source).Concat(metadata).Concat(wrong).Concat(xml).Concat(deployment).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray(), v1, source, metadata, wrong));
        // 候选跟着伪造冷允许表一起改签也必须拒绝，不仅靠少四条的数量差。
        foreach (var wrong in new[] { Array.Empty<string>(), deployment.Concat(deployment).ToArray(),
            deployment.Select((s, i) => i == 0 ? s.Replace("System.Type", "Object", StringComparison.Ordinal) : s).ToArray(),
            deployment.Select((s, i) => i == 1 ? s.Replace(":150:", ":134:", StringComparison.Ordinal) : s).ToArray(),
            deployment.Select((s, i) => i == 2 ? s.Replace("->Void", "->Object", StringComparison.Ordinal) : s).ToArray(),
            deployment.Select((s, i) => i == 3 ? "M:RyuPy.Runtime.PyModules:Unknown:150:[0;0;0]()->Void:" : s).ToArray() })
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before,
                before.Concat(v1).Concat(source).Concat(metadata).Concat(builtin).Concat(xml).Concat(wrong).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray(),
                v1, source, metadata, builtin, xml, wrong));
        var good = groups.SelectMany(x => x).Concat(factory).Concat(PyTraceStateArtifactTests.DirectGlobalsPublicAdditions()).Concat(PyTraceStateArtifactTests.OrdinaryMiPublicAdditions()).ToArray();
        foreach (var mutation in new[] {
            (":Type:54:", ":Type:22:"), // readonly 标志
            (":Configure:150:", ":Configure:134:"), // static 标志
            (":GetAttr:198:", ":GetAttr:134:"), // virtual 标志
            ("(System.Type,String)", "(Object,String)"),
            (":Load:134:[32;0;0]()->Object:", ":Load:134:[32;0;0]()->String:") })
        {
            var changed = good.Select(x => x.Replace(mutation.Item1, mutation.Item2, StringComparison.Ordinal)).ToArray();
            Assert.False(good.SequenceEqual(changed));
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, changed));
        }
        foreach (string unknown in new[] { "M:未知方法", "M:未知重载", "T:未知类型" })
        {
            var changed = (string[])good.Clone(); changed[^1] = unknown;
            Assert.False(PyTraceStateArtifactTests.PreservesBaselineAbi(before, changed));
        }
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
        string lookup = C.Method("PyOps", "LoadDefaultBuiltin", "Object", false, "String");
        foreach (string type in new[] { "PyNotImplementedError", "PyNameError" })
        {
            var producer = new C.Edge("PyOps", lookup, "IL", "call PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<" + type + ">");
            Assert.True(contract.Allows(producer));
            foreach (string wrong in new[] { C.Method("PyOps", "LoadDefaultBuiltinExtra", "Object", false, "String"),
                C.Method("PyOps", "LoadDefaultBuiltin", "String", false, "String"),
                C.Method("PyOps", "LoadDefaultBuiltin", "Object", false, "Object"),
                C.Method("PyOps", "LoadDefaultBuiltin", "Object", true, "String") })
                Assert.False(contract.Allows(producer with { Source = wrong }));
            foreach (string wrong in new[] { "PyTypeError", "PyBaseException" })
                Assert.False(contract.Allows(producer with { Target = producer.Target.Replace("<" + type + ">", "<" + wrong + ">", StringComparison.Ordinal) }));
            foreach (string op in new[] { "ldftn ", "callvirt " })
                Assert.False(contract.Allows(producer with { Target = producer.Target.Replace("call ", op, StringComparison.Ordinal) }));
        }
        AssertDefaultBuiltinProducerStructure();
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

    // 只测直线producer判定器，不把合成记录冒称候选Debug/Release的真实IL。
    private static void AssertDefaultBuiltinProducerStructure()
    {
        string source = PyTraceStateArtifactTests.DefaultBuiltinProducerSource;
        string Fresh(string type) => "PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<" + type + ">";
        (int Offset, string Op, string? Target, long? Integer)[] good =
        [
            (0, "newobj", C.Method("PyNotImplementedError", ".ctor", "Void", true, "String"), null),
            (5, "nop", null, null), (6, "call", Fresh("PyNotImplementedError"), null),
            (11, "nop", null, null), (12, "throw", null, null),
            (13, "newobj", C.Method("PyNameError", ".ctor", "Void", true, "String"), null),
            (18, "call", Fresh("PyNameError"), null), (23, "throw", null, null), (24, "ret", null, null)
        ];
        string[] Check((int Offset, string Op, string? Target, long? Integer)[] il, string[]? names = null,
            int attributes = 150, int regions = 0)
            => PyTraceStateArtifactTests.DefaultBuiltinProducerProbe(names ?? [source], attributes, il, regions);
        Assert.Empty(Check(good));
        Assert.Empty(Check(good.Where(i => i.Op != "nop").ToArray()));
        Assert.NotEmpty(Check(good, [])); Assert.NotEmpty(Check(good, [source, source]));
        Assert.NotEmpty(Check(good, [source.Replace("(String)", "(Object)", StringComparison.Ordinal)]));
        Assert.NotEmpty(Check(good, attributes: 134)); Assert.NotEmpty(Check(good, regions: 1));
        foreach (int index in new[] { 4, 7 })
        {
            var changed = good.ToArray(); changed[index] = (changed[index].Offset, "ret", changed[index].Target, changed[index].Integer);
            Assert.NotEmpty(Check(changed));
        }
        foreach (string op in new[] { "stloc.0", "stfld", "dup", "call", "ret", "br.s" })
        foreach (int index in new[] { 2, 4, 6, 7 })
            Assert.NotEmpty(Check(good.Take(index).Append((good[index].Offset, op, (string?)null, (long?)null)).Concat(good.Skip(index)).ToArray()));
        foreach (string op in new[] { "br", "br.s", "beq.s", "leave" })
        foreach (long target in new long[] { 5, 6, 11, 12, 18, 23 })
            Assert.NotEmpty(Check(good.Append((25, op, (string?)null, (long?)target)).ToArray()));
        // 跳到newobj本身仍会新建；仅constructor之后的入口被拒绝。
        foreach (long target in new long[] { 0, 13 })
            Assert.Empty(Check(good.Append((25, "br.s", (string?)null, (long?)target)).ToArray()));
        foreach (string op in new[] { "switch", "throw", "rethrow" })
            Assert.NotEmpty(Check(good.Append((25, op, (string?)null, (long?)null)).ToArray()));
        foreach (int index in new[] { 0, 2, 5, 6 })
        {
            Assert.NotEmpty(Check(good.Append(good[index]).ToArray()));
            Assert.NotEmpty(Check(good.Where((_, at) => at != index).ToArray()));
        }
        var wrongFresh = good.ToArray(); wrongFresh[2] = (wrongFresh[2].Offset, wrongFresh[2].Op, Fresh("PyTypeError"), wrongFresh[2].Integer);
        Assert.NotEmpty(Check(wrongFresh));
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
