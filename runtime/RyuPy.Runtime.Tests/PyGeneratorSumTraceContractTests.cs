// 批量sum仅精确迁移私有方法签名；实际PE变异必须仍能拒绝越权调用和生命周期错序。
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using C = RyuPy.Runtime.Tests.PyTraceStateV1ArtifactContract;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void GeneratorSumBridgeRejectsWrongSignatureAndExtraAuthority()
    {
        var contract = new C();
        string enabled = "call " + C.Method("PyTraceProduct", "get_Enabled", "Boolean");
        string batch = C.Method("PyGenerator", "TrySumBatch", "Boolean", true, "Int64", "PyGeneratorSumResult&");
        var edge = new C.Edge("PyGenerator", batch, "IL", enabled);
        Assert.True(contract.Allows(edge));
        Assert.False(contract.Allows(edge with { Source = C.Method("PyGenerator", "TrySumBatch", "Boolean", true, "Int64", "Object&") }));
        foreach (string target in new[] {
            C.Method("PyTraceProduct", "EnterGenerator", "Void", false, "Int64"),
            C.Method("PyTraceProduct", "LeaveGenerator", "Void", false, "Int64"),
            C.Method("PyTraceProduct", "Exit", "Void") })
            Assert.False(contract.Allows(edge with { Target = "call " + target }));
        Assert.False(contract.Allows(edge with { Target = enabled.Replace("call ", "ldftn ", StringComparison.Ordinal) }));
        string old = C.Method("PyGenerator", "MoveNextCore", "Boolean", true, "Object", "Object&", "PyBaseException");
        string current = C.Method("PyGenerator", "MoveNextCore", "Boolean", true, "Object", "Object&", "PyBaseException", "PyGeneratorSumState");
        Assert.False(contract.Allows(edge with { Source = old }));
        Assert.True(contract.Allows(edge with { Source = current }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualPeRejectsGeneratorSumAuthorityAndFinallyOrderChanges(bool wrongFinally)
    {
        string candidate = Candidate();
        AssertCandidatePe(candidate, wrongFinally ? "candidate-pe-before-generator-sum-order.json"
            : "candidate-pe-before-generator-sum-authority.json");
        byte[] bytes = File.ReadAllBytes(candidate);
        using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var names = new Signatures();
        string source = wrongFinally
            ? "RyuPy.Runtime.PyGenerator::MoveNextCore[32;0;4](Object,Object&,RyuPy.Runtime.PyBaseException,RyuPy.Runtime.PyGeneratorSumState)->Boolean"
            : "RyuPy.Runtime.PyGenerator::TrySumBatch[32;0;2](Int64,RyuPy.Runtime.PyGeneratorSumResult&)->Boolean";
        var handle = metadata.MethodDefinitions.Single(h => Resolve(metadata, h, names) == source);
        var method = metadata.GetMethodDefinition(handle);
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var instructions = DecodeTraceInstructions(body.GetILBytes()!, metadata, names);
        var section = pe.PEHeaders.SectionHeaders.Single(s => method.RelativeVirtualAddress >= s.VirtualAddress
            && method.RelativeVirtualAddress < s.VirtualAddress + Math.Max(s.VirtualSize, s.SizeOfRawData));
        int start = section.PointerToRawData + method.RelativeVirtualAddress - section.VirtualAddress;
        int header = (bytes[start] & 3) == 2 ? 1 : (BitConverter.ToUInt16(bytes, start) >> 12) * 4;
        void ReplaceCall(string original, string replacement)
        {
            var call = instructions.Single(i => i.Op == "call" && i.Target == original);
            var target = metadata.MethodDefinitions.Single(h => Resolve(metadata, h, names) == replacement);
            int operand = start + header + call.Offset + 1;
            Assert.Equal(0x28, bytes[operand - 1]);
            BitConverter.GetBytes(MetadataTokens.GetToken(target)).CopyTo(bytes, operand);
        }
        const string enter = "RyuPy.Runtime.PyTraceProduct::EnterGenerator[0;0;1](Int64)->Void";
        const string leave = "RyuPy.Runtime.PyTraceProduct::LeaveGenerator[0;0;1](Int64)->Void";
        if (wrongFinally)
        {
            // 两个目标均在原许可内且签名相同；只有真实IL/EH顺序检查能识别交换。
            ReplaceCall(enter, leave);
            ReplaceCall(leave, enter);
        }
        else ReplaceCall("RyuPy.Runtime.PyTraceProduct::get_Enabled[0;0;0]()->Boolean",
            "RyuPy.Runtime.PyTraceProduct::Exit[0;0;0]()->Void");
        string mutated = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(mutated, bytes);
            var violations = ScanV1(mutated).Violations;
            if (wrongFinally) Assert.Contains(violations, v => v.Contains("LeaveGenerator 必须在 finally", StringComparison.Ordinal));
            else Assert.Contains(violations, v => v.Contains("PyGenerator::TrySumBatch", StringComparison.Ordinal)
                && v.Contains("PyTraceProduct::Exit", StringComparison.Ordinal));
        }
        finally { File.Delete(mutated); }
    }
}
