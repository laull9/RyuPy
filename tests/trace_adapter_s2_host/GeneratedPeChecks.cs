// 只解码真实generated PE；不加载程序集。引用表与实际方法操作数分别落证，再执行精确合同。
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

internal static class GeneratedPeChecks
{
    internal sealed record AssemblyId(string Name, string Version, string Culture, string Key, int Flags);
    internal sealed record TypeId(AssemblyId Assembly, string Name);
    internal sealed record Target(TypeId Owner, string Name, string Signature, string? Instantiation = null);
    internal sealed record Instruction(int Offset, int Opcode, int? Token);
    internal sealed record Site(int MethodToken, string Method, int Offset, int Opcode, Target Target);
    internal sealed record MethodInfo(int Token, string Name, int Rva, int Bytes, int Instructions);
    internal sealed class Evidence
    {
        public string Path { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public List<MethodInfo> Methods { get; } = new();
        public List<Target> References { get; } = new();
        public List<Site> Sites { get; } = new();
        public List<string> IndirectSignatures { get; } = new();
        public string? DecodeError { get; set; }
    }

    internal static readonly AssemblyId Runtime = new("RyuPy.Runtime", "1.0.0.0", "", "", 0);
    internal static readonly Target Explicit = new(new(Runtime, "RyuPy.Runtime.PyTraceAdapter"), "PrepareExplicit", "00011C1C");
    internal static readonly Target Handle = new(new(Runtime, "RyuPy.Runtime.PyTraceAdapter"), "Handle", "0002011C0A");
    internal static readonly Target Locals = new(new(Runtime, "RyuPy.Runtime.PyOps"), "FunctionLocalsValidate", "0003011D1C1D1C1D1C");

    internal enum Verdict { Accepted, ForbiddenExplicit, MissingLocals, MissingExplicit, MissingHandle }

    // 必须先查禁项。完整合法native集合再夹带真实Explicit，不能被MissingLocals掩盖。
    internal static Verdict Validate(IReadOnlyList<Site> sites, bool native)
    {
        if (native && sites.Any(s => s.Target == Explicit)) return Verdict.ForbiddenExplicit;
        bool Called(Target target) => sites.Any(s => s.Opcode is 0x28 or 0x6f && s.Target == target);
        if (native && !Called(Locals)) return Verdict.MissingLocals;
        if (!native && !Called(Explicit)) return Verdict.MissingExplicit;
        return Called(Handle) ? Verdict.Accepted : Verdict.MissingHandle;
    }

    // ECMA-335完整已定义opcode宽度表；保留洞位非法，不把任意字节当call。
    private static int Width(int op)
    {
        if (op is >= 0x00 and <= 0x0d or >= 0x14 and <= 0x1e or 0x25 or 0x26 or 0x2a
            or >= 0x46 and <= 0x6e or 0x76 or 0x7a or >= 0x82 and <= 0x8b or 0x8e
            or >= 0x90 and <= 0xa2 or >= 0xb3 and <= 0xba or 0xc3
            or >= 0xd1 and <= 0xdc or 0xdf or 0xe0
            or >= 0xfe00 and <= 0xfe05 or 0xfe0f or 0xfe11 or 0xfe13 or 0xfe14
            or 0xfe17 or 0xfe18 or 0xfe1a or 0xfe1d or 0xfe1e) return 0;
        if (op is >= 0x0e and <= 0x13 or 0x1f or >= 0x2b and <= 0x37 or 0xde or 0xfe12 or 0xfe19) return 1;
        if (op is >= 0xfe09 and <= 0xfe0e) return 2;
        if (op is 0x20 or 0x22 or 0x27 or 0x28 or 0x29 or >= 0x38 and <= 0x44
            or >= 0x6f and <= 0x75 or 0x79 or >= 0x7b and <= 0x81 or 0x8c or 0x8d or 0x8f
            or >= 0xa3 and <= 0xa5 or 0xc2 or 0xc6 or 0xd0 or 0xdd
            or 0xfe06 or 0xfe07 or 0xfe15 or 0xfe16 or 0xfe1c) return 4;
        if (op is 0x21 or 0x23) return 8;
        throw new BadImageFormatException($"未知IL opcode 0x{op:X}");
    }

    internal static bool MethodOperand(int op) => op is 0x27 or 0x28 or 0x6f or 0x73 or 0xfe06 or 0xfe07;

    internal static List<Instruction> Decode(byte[] il)
    {
        var result = new List<Instruction>();
        int position = 0;
        while (position < il.Length)
        {
            int start = position, op = il[position++];
            if (op == 0xfe)
            {
                if (position == il.Length) throw new BadImageFormatException("截断双字节opcode");
                op = 0xfe00 | il[position++];
            }
            int length;
            if (op == 0x45)
            {
                if (il.Length - position < 4) throw new BadImageFormatException("截断switch计数");
                uint count = BinaryPrimitives.ReadUInt32LittleEndian(il.AsSpan(position, 4));
                if (count > (uint)(il.Length - position - 4) / 4) throw new BadImageFormatException("截断switch目标");
                length = 4 + checked((int)count * 4);
            }
            else length = Width(op);
            if (length > il.Length - position) throw new BadImageFormatException("截断IL操作数");
            int? token = MethodOperand(op) || op is 0x29 or 0xd0
                ? BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(position, 4)) : null;
            result.Add(new(start, op, token));
            position += length;
        }
        return result;
    }

    private static AssemblyId Assembly(MetadataReader md, AssemblyReferenceHandle handle)
    {
        var a = md.GetAssemblyReference(handle);
        return new(md.GetString(a.Name), a.Version.ToString(), md.GetString(a.Culture),
            Convert.ToHexString(md.GetBlobBytes(a.PublicKeyOrToken)), (int)a.Flags);
    }

    private static AssemblyId LocalAssembly(MetadataReader md)
    {
        var a = md.GetAssemblyDefinition();
        return new(md.GetString(a.Name), a.Version.ToString(), md.GetString(a.Culture),
            Convert.ToHexString(md.GetBlobBytes(a.PublicKey)), (int)a.Flags);
    }

    private static TypeId Owner(MetadataReader md, EntityHandle handle, int depth = 0)
    {
        if (depth > 64) throw new BadImageFormatException("类型owner递归过深");
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
                var reference = md.GetTypeReference((TypeReferenceHandle)handle);
                string name = md.GetString(reference.Name), ns = md.GetString(reference.Namespace);
                if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
                {
                    var enclosing = Owner(md, reference.ResolutionScope, depth + 1);
                    return enclosing with { Name = enclosing.Name + "+" + name };
                }
                var assembly = reference.ResolutionScope.Kind == HandleKind.AssemblyReference
                    ? Assembly(md, (AssemblyReferenceHandle)reference.ResolutionScope)
                    : reference.ResolutionScope.Kind == HandleKind.ModuleDefinition
                        ? LocalAssembly(md)
                        : throw new BadImageFormatException("TypeRef没有可解析的Assembly/Module定义scope");
                return new(assembly, string.IsNullOrEmpty(ns) ? name : ns + "." + name);
            case HandleKind.TypeDefinition:
                var definition = md.GetTypeDefinition((TypeDefinitionHandle)handle);
                var parent = definition.GetDeclaringType();
                string own = md.GetString(definition.Name), space = md.GetString(definition.Namespace);
                if (!parent.IsNil)
                {
                    var enclosing = Owner(md, parent, depth + 1);
                    return enclosing with { Name = enclosing.Name + "+" + own };
                }
                return new(LocalAssembly(md), string.IsNullOrEmpty(space) ? own : space + "." + own);
            case HandleKind.TypeSpecification:
                var specification = md.GetTypeSpecification((TypeSpecificationHandle)handle);
                var decoded = specification.DecodeSignature(new Types(), depth + 1);
                return decoded with { Name = decoded.Name + "|typespec:" + Convert.ToHexString(md.GetBlobBytes(specification.Signature)) };
            case HandleKind.MethodDefinition:
                return Owner(md, md.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType(), depth + 1);
            case HandleKind.ModuleReference:
                throw new BadImageFormatException("ModuleRef owner无法在单PE内证明程序集身份");
            default: throw new BadImageFormatException("无法解析方法owner: " + handle.Kind);
        }
    }

    internal static Target Resolve(MetadataReader md, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            var spec = md.GetMethodSpecification((MethodSpecificationHandle)handle);
            var target = Resolve(md, spec.Method);
            var args = spec.DecodeSignature(new Types(), 0);
            // 非generic方法不能伪装MethodSpec；保留实例化签名，不能混同nongeneric目标。
            var signature = Convert.FromHexString(target.Signature);
            var reader = md.GetBlobReader(spec.Method.Kind == HandleKind.MethodDefinition
                ? md.GetMethodDefinition((MethodDefinitionHandle)spec.Method).Signature
                : md.GetMemberReference((MemberReferenceHandle)spec.Method).Signature);
            if ((signature[0] & 0x10) == 0) throw new BadImageFormatException("MethodSpec目标不是generic方法");
            reader.ReadByte();
            if (reader.ReadCompressedInteger() != args.Length) throw new BadImageFormatException("MethodSpec泛型实参数不符");
            return target with { Instantiation = Convert.ToHexString(md.GetBlobBytes(spec.Signature)) };
        }
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var m = md.GetMethodDefinition((MethodDefinitionHandle)handle);
            _ = m.DecodeSignature(new Types(), 0);
            return new(Owner(md, m.GetDeclaringType()), md.GetString(m.Name), Convert.ToHexString(md.GetBlobBytes(m.Signature)));
        }
        if (handle.Kind == HandleKind.MemberReference)
        {
            var m = md.GetMemberReference((MemberReferenceHandle)handle);
            if (m.GetKind() != MemberReferenceKind.Method) throw new BadImageFormatException("方法指令指向字段");
            _ = m.DecodeMethodSignature(new Types(), 0);
            return new(Owner(md, m.Parent), md.GetString(m.Name), Convert.ToHexString(md.GetBlobBytes(m.Signature)));
        }
        throw new BadImageFormatException("方法指令令牌类型非法: " + handle.Kind);
    }

    internal static Evidence Scan(string path)
    {
        var evidence = new Evidence { Path = path };
        try
        {
            var bytes = File.ReadAllBytes(path);
            evidence.Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
            using var pe = new PEReader(new MemoryStream(bytes));
            var md = pe.GetMetadataReader();
            foreach (var h in md.MemberReferences)
                if (md.GetMemberReference(h).GetKind() == MemberReferenceKind.Method)
                    evidence.References.Add(Resolve(md, h));
            foreach (var h in md.MethodDefinitions)
            {
                var m = md.GetMethodDefinition(h);
                int token = MetadataTokens.GetToken(h);
                string name = md.GetString(m.Name);
                if (m.RelativeVirtualAddress == 0)
                {
                    evidence.Methods.Add(new(token, name, 0, 0, 0));
                    continue;
                }
                byte[] il = pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes()
                    ?? throw new BadImageFormatException("缺少IL字节");
                var decoded = Decode(il);
                evidence.Methods.Add(new(token, name, m.RelativeVirtualAddress, il.Length, decoded.Count));
                foreach (var instruction in decoded)
                {
                    if (MethodOperand(instruction.Opcode))
                        evidence.Sites.Add(new(token, name, instruction.Offset, instruction.Opcode,
                            Resolve(md, MetadataTokens.EntityHandle(instruction.Token!.Value))));
                    else if (instruction.Opcode == 0x29)
                    {
                        var signatureHandle = MetadataTokens.EntityHandle(instruction.Token!.Value);
                        if (signatureHandle.Kind != HandleKind.StandaloneSignature) throw new BadImageFormatException("calli签名令牌非法");
                        var signature = md.GetStandaloneSignature((StandaloneSignatureHandle)signatureHandle);
                        _ = signature.DecodeMethodSignature(new Types(), 0);
                        evidence.IndirectSignatures.Add(Convert.ToHexString(md.GetBlobBytes(signature.Signature)));
                    }
                    else if (instruction.Opcode == 0xd0)
                    {
                        var operand = MetadataTokens.EntityHandle(instruction.Token!.Value);
                        if (operand.Kind is HandleKind.MethodDefinition or HandleKind.MethodSpecification
                            || operand.Kind == HandleKind.MemberReference && md.GetMemberReference((MemberReferenceHandle)operand).GetKind() == MemberReferenceKind.Method)
                            // ldtoken仅留存引用事实，不算实际调用或函数指针。
                            evidence.References.Add(Resolve(md, operand));
                    }
                }
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { evidence.DecodeError = error.GetType().Name + ": " + error.Message; }
        return evidence;
    }

    internal static void Inspect(string path, string evidencePath, bool native)
    {
        var evidence = Scan(path);
        File.WriteAllText(evidencePath, JsonSerializer.Serialize(evidence));
        if (evidence.DecodeError is not null) throw new InvalidOperationException("generated PE解码失败: " + evidence.DecodeError);
        var verdict = Validate(evidence.Sites, native);
        File.WriteAllText(evidencePath + ".verdict.json", JsonSerializer.Serialize(new { Verdict = verdict.ToString() }));
        if (verdict != Verdict.Accepted) throw new InvalidOperationException("generated PE实际调用合同拒绝: " + verdict);
    }

    // 静态签名解码provider，不反射执行被测模块；generic owner保留完整类型实参。
    private sealed class Types : ISignatureTypeProvider<TypeId, int>
    {
        private static TypeId Shape(string name) => new(new("<signature>", "", "", "", 0), name);
        public TypeId GetArrayType(TypeId elementType, ArrayShape shape) => elementType with { Name = elementType.Name + "[rank=" + shape.Rank + "]" };
        public TypeId GetByReferenceType(TypeId elementType) => elementType with { Name = elementType.Name + "&" };
        public TypeId GetFunctionPointerType(MethodSignature<TypeId> signature) => Shape("fnptr:" + signature.Header.RawValue + ":" + signature.ReturnType + "(" + string.Join(",", signature.ParameterTypes) + ")");
        public TypeId GetGenericInstantiation(TypeId genericType, ImmutableArray<TypeId> typeArguments) => genericType with { Name = genericType.Name + "<" + string.Join(",", typeArguments) + ">" };
        public TypeId GetGenericMethodParameter(int context, int index) => Shape("!!" + index);
        public TypeId GetGenericTypeParameter(int context, int index) => Shape("!" + index);
        public TypeId GetModifiedType(TypeId modifier, TypeId unmodifiedType, bool isRequired) => unmodifiedType with { Name = unmodifiedType.Name + (isRequired ? " modreq(" : " modopt(") + modifier + ")" };
        public TypeId GetPinnedType(TypeId elementType) => elementType with { Name = elementType.Name + " pinned" };
        public TypeId GetPointerType(TypeId elementType) => elementType with { Name = elementType.Name + "*" };
        public TypeId GetPrimitiveType(PrimitiveTypeCode typeCode) => Shape(typeCode.ToString());
        public TypeId GetSZArrayType(TypeId elementType) => elementType with { Name = elementType.Name + "[]" };
        public TypeId GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => Owner(reader, handle);
        public TypeId GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => Owner(reader, handle);
        public TypeId GetTypeFromSpecification(MetadataReader reader, int context, TypeSpecificationHandle handle, byte rawTypeKind) => Owner(reader, handle, context);
    }
}
