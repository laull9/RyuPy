// 每个新producer读取实际已构建PE的匹配new/Fresh/throw，再独立变异原字节；绝不加载变异程序集。
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using C = RyuPy.Runtime.Tests.PyTraceStateV1ArtifactContract;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void ActualPeTreeBuilderProducersRejectMissingWrongReusedEscapedAndJumpedFresh()
    {
        string candidate = Candidate(); AssertCandidatePe(candidate, "candidate-pe-before-treebuilder-mutation.json");
        byte[] original = File.ReadAllBytes(candidate);
        using var stream = new MemoryStream(original, writable: false);
        using var pe = new PEReader(stream); var metadata = pe.GetMetadataReader(); var names = new Signatures();
        var evidence = new List<object>();
        foreach (var (name, type) in C.TreeBuilderProducers)
        {
            string source = C.Method("PyXmlTreeBuilderErrors", name, type, false, "String");
            var handle = metadata.MethodDefinitions.Single(h => C.Short(Resolve(metadata, h, names)) == source);
            var method = metadata.GetMethodDefinition(handle); var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var raw = DecodeTraceInstructions(body.GetILBytes()!, metadata, names);
            var il = raw.Where(i => i.Op != "nop").ToArray();
            int at = Array.FindIndex(il, i => C.Short(i.Target ?? "") == "PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<" + type + ">");
            Assert.True(at > 0 && at + 1 < il.Length); Assert.Equal("newobj", il[at - 1].Op); Assert.Equal("throw", il[at + 1].Op);
            var section = pe.PEHeaders.SectionHeaders.Single(s => method.RelativeVirtualAddress >= s.VirtualAddress
                && method.RelativeVirtualAddress < s.VirtualAddress + Math.Max(s.VirtualSize, s.SizeOfRawData));
            int start = section.PointerToRawData + method.RelativeVirtualAddress - section.VirtualAddress;
            int header = (original[start] & 3) == 2 ? 1 : (BitConverter.ToUInt16(original, start) >> 12) * 4;
            int code = start + header, fresh = code + il[at].Offset, create = code + il[at - 1].Offset;
            // 不依赖MemberRef枚举：替代token取自另一个真实producer已解码的实际Fresh调用。
            string donorName = name == "Type" ? "Name" : "Type";
            var donor = metadata.GetMethodDefinition(metadata.MethodDefinitions.Single(h =>
                C.Short(Resolve(metadata, h, names)).StartsWith("PyXmlTreeBuilderErrors::" + donorName + "[", StringComparison.Ordinal)));
            var donorBody = pe.GetMethodBody(donor.RelativeVirtualAddress); byte[] donorIl = donorBody.GetILBytes()!;
            int donorOffset = DecodeTraceInstructions(donorIl, metadata, names).Single(i =>
                i.Target?.Contains("::NativeFresh", StringComparison.Ordinal) == true).Offset;
            int wrongToken = BitConverter.ToInt32(donorIl, donorOffset + 1);
            foreach (string mutation in new[] { "missing-fresh", "wrong-type", "reuse", "store-escape", "return", "jump-after-new" })
            {
                byte[] changed = (byte[])original.Clone();
                switch (mutation)
                {
                    case "missing-fresh": Array.Clear(changed, fresh, 5); break;
                    case "wrong-type": BitConverter.GetBytes(wrongToken).CopyTo(changed, fresh + 1); break;
                    case "reuse": Array.Clear(changed, create, 5); changed[create] = 0x25; break; // dup替代new
                    case "store-escape": Array.Clear(changed, fresh, 5); changed[fresh] = 0x0a; break; // stloc.0替代Fresh
                    case "return": changed[code + il[at + 1].Offset] = 0x2a; break;
                    default:
                        var jump = raw.First(i => i.Offset < il[at - 1].Offset && i.Integer is not null
                            && Opcodes.Values.Any(op => op.Name == i.Op && op.OperandType is OperandType.ShortInlineBrTarget or OperandType.InlineBrTarget));
                        var opcode = Opcodes.Values.Single(op => op.Name == jump.Op);
                        int size = opcode.OperandType == OperandType.ShortInlineBrTarget ? 1 : 4;
                        int delta = il[at].Offset - (jump.Offset + opcode.Size + size);
                        if (size == 1) { Assert.InRange(delta, sbyte.MinValue, sbyte.MaxValue); changed[code + jump.Offset + opcode.Size] = unchecked((byte)(sbyte)delta); }
                        else BitConverter.GetBytes(delta).CopyTo(changed, code + jump.Offset + opcode.Size);
                        break;
                }
                string path = Path.GetTempFileName();
                try
                {
                    File.WriteAllBytes(path, changed); var audit = ScanV1(path);
                    Assert.Contains(audit.Violations, value => value.StartsWith(TreeBuilderProducerFailure + source, StringComparison.Ordinal));
                    evidence.Add(new { source, mutation, instructions = raw, audit.Violations });
                }
                finally { File.Delete(path); }
            }
        }
        Evidence("treebuilder-producer-mutations.json", evidence);
    }

    [Fact]
    public void TreeBuilderAbiIncrementRejectsMissingDuplicateChangedAndInventedPermissions()
    {
        var before = Enumerable.Range(0, 1659).Select(i => "旧签名:" + i).ToArray();
        var v1 = C.PublicAdditions(); var source = SourcePublicAdditions(); var metadata = MetadataPublicAdditions(); var builtin = BuiltinPublicAdditions();
        var old = before.Concat(v1).Concat(source).Concat(metadata).Concat(builtin).ToArray();
        var xml = XmlPublicAdditions(); Assert.Equal(1700, old.Length); Assert.Equal(6, xml.Length);
        var deployment = DefaultSourcePublicAdditions();
        var factory = LexicalClassFactoryPublicAdditions();
        var full = old.Concat(xml).Concat(deployment).Concat(factory).Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions()).ToArray(); Assert.True(PreservesBaselineAbi(before, full));
        for (int at = 0; at < xml.Length; at++)
        {
            foreach (string[] wrong in new[] { xml.Where((_, i) => i != at).ToArray(),
                xml.Select((s, i) => i == at ? s + "改签" : s).ToArray(),
                xml.Select((s, i) => i == at ? xml[(i + 1) % xml.Length] : s).ToArray() })
            {
                Assert.False(PreservesBaselineAbi(before, old.Concat(wrong).Concat(deployment).Concat(factory).Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions()).ToArray()));
                Assert.False(PreservesBaselineAbi(before, old.Concat(wrong).Concat(deployment).Concat(factory).Concat(DirectGlobalsPublicAdditions()).Concat(OrdinaryMiPublicAdditions()).Concat(RuntimeDerivedPublicAdditions()).ToArray(), v1, source, metadata, builtin, wrong));
            }
        }
        foreach (var (name, type) in C.TreeBuilderProducers)
        {
            var contract = new C();
            var edge = new C.Edge("PyXmlTreeBuilderErrors", C.Method("PyXmlTreeBuilderErrors", name, type, false, "String"), "IL",
                "call PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<" + type + ">");
            Assert.True(contract.Allows(edge));
            Assert.False(contract.Allows(edge with { Source = edge.Source.Replace("(String)", "(Object)", StringComparison.Ordinal) }));
            Assert.False(contract.Allows(edge with { Source = edge.Source.Replace("Errors::", "Errors::Other", StringComparison.Ordinal) }));
            Assert.False(contract.Allows(edge with { Target = edge.Target.Replace("call ", "ldftn ", StringComparison.Ordinal) }));
        }
    }
}
