// 真实 PE 字节变异证明门读取方法 operand，而非仅比较手写列表；变异程序集绝不加载或执行。
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void ActualPeRejectsReceiveInjectedIntoFramesProjection()
    {
        string candidate = Candidate();
        Assert.Empty(ScanV1(candidate).Violations);
        byte[] bytes = File.ReadAllBytes(candidate);
        using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEReader(stream); var metadata = pe.GetMetadataReader(); var names = new Signatures();
        var getterHandle = metadata.MethodDefinitions.Single(h => Resolve(metadata, h, names)
            == "RyuPy.Runtime.PyBaseException::get_Frames[32;0;0]()->RyuPy.Runtime.PyStackFrame[]");
        var receive = metadata.MethodDefinitions.Single(h => Resolve(metadata, h, names)
            == "RyuPy.Runtime.PyTraceState::Receive[32;0;1](RyuPy.Runtime.TraceReceive)->RyuPy.Runtime.TraceResult");
        var getter = metadata.GetMethodDefinition(getterHandle); var body = pe.GetMethodBody(getter.RelativeVirtualAddress);
        var instruction = DecodeTraceInstructions(body.GetILBytes()!, metadata, names).Single(i => i.Op == "callvirt"
            && i.Target == "RyuPy.Runtime.TraceSnapshot::get_History[32;0;0]()->System.Collections.ObjectModel.ReadOnlyCollection`1<RyuPy.Runtime.TraceFrame>");
        var section = pe.PEHeaders.SectionHeaders.Single(s => getter.RelativeVirtualAddress >= s.VirtualAddress
            && getter.RelativeVirtualAddress < s.VirtualAddress + Math.Max(s.VirtualSize, s.SizeOfRawData));
        int start = section.PointerToRawData + getter.RelativeVirtualAddress - section.VirtualAddress;
        int headerLength = (bytes[start] & 3) == 2 ? 1 : (BitConverter.ToUInt16(bytes, start) >> 12) * 4;
        int operand = start + headerLength + instruction.Offset + 1; // callvirt 固定单字节 opcode。
        Assert.Equal(0x6f, bytes[operand - 1]);
        BitConverter.GetBytes(MetadataTokens.GetToken(receive)).CopyTo(bytes, operand);
        string mutated = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(mutated, bytes);
            var audit = ScanV1(mutated);
            Assert.Contains(audit.Violations, value => value.Contains("PyBaseException::get_Frames", StringComparison.Ordinal)
                && value.Contains("PyTraceState::Receive", StringComparison.Ordinal));
        }
        finally { File.Delete(mutated); }
    }
    [Fact]
    public void ActualPeRejectsAllMissChangedToConsume()
    {
        string candidate = Candidate(); Assert.Empty(ScanV1(candidate).Violations);
        byte[] bytes = File.ReadAllBytes(candidate);
        using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEReader(stream); var metadata = pe.GetMetadataReader(); var names = new Signatures();
        var handle = metadata.MethodDefinitions.Single(h => Resolve(metadata, h, names)
            == "RyuPy.Runtime.PyTraceProduct::AllMiss[0;0;1](Object)->Void");
        var method = metadata.GetMethodDefinition(handle); var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = DecodeTraceInstructions(body.GetILBytes()!, metadata, names).Where(i => i.Op != "nop").ToArray();
        int call = Array.FindIndex(il, i => i.Op == "call"
            && i.Target == "RyuPy.Runtime.PyTraceProduct::Consume[0;0;2](Object,Boolean)->Void");
        Assert.True(call > 0); Assert.Equal("ldc.i4.1", il[call - 1].Op);
        var section = pe.PEHeaders.SectionHeaders.Single(s => method.RelativeVirtualAddress >= s.VirtualAddress
            && method.RelativeVirtualAddress < s.VirtualAddress + Math.Max(s.VirtualSize, s.SizeOfRawData));
        int start = section.PointerToRawData + method.RelativeVirtualAddress - section.VirtualAddress;
        int header = (bytes[start] & 3) == 2 ? 1 : (BitConverter.ToUInt16(bytes, start) >> 12) * 4;
        bytes[start + header + il[call - 1].Offset] = 0x16; // true → false，保持合法指令长度。
        string mutated = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(mutated, bytes);
            Assert.Contains(ScanV1(mutated).Violations, value => value.Contains("固定 kind/bool 参数改变", StringComparison.Ordinal)
                && value.Contains("PyTraceProduct::AllMiss", StringComparison.Ordinal));
        }
        finally { File.Delete(mutated); }
    }

}
