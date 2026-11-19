// 复用 S1 的真实 PE 解码器，补齐成员上下文和少数 V1 特定结构检查；不加载目标程序集。
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using C = RyuPy.Runtime.Tests.PyTraceStateV1ArtifactContract;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    private sealed record TraceInstruction(int Offset, string Op, string? Target, long? Integer);
    private sealed record TraceBody(string Owner, string Source, int Attributes,
        TraceInstruction[] Instructions, ExceptionRegion[] Regions);
    private sealed record TraceAudit(string[] CoreTypes, int Bodies, int Operands, C.Edge[] Edges, string[] Violations);

    private static TraceAudit ScanV1(string path)
    {
        using var stream = File.OpenRead(path); using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader(); var names = new Signatures(); var contract = new C();
        var found = new HashSet<string>(StringComparer.Ordinal); var carriers = new HashSet<string>(StringComparer.Ordinal);
        var violations = new List<string>(); var edges = new List<C.Edge>(); var bodies = new List<TraceBody>(); int operands = 0;
        void Check(string owner, string source, string kind, string target)
        {
            if (!C.MentionsDomain(target)) return;
            var edge = new C.Edge(owner, source, kind, target); edges.Add(edge);
            if (!contract.Allows(edge)) violations.Add(source + " | " + kind + " → " + target);
        }
        // 本 runtime 的 Core/桥接必须解析到自己的 TypeDef，不能靠同名外部 TypeRef 冒充。
        foreach (var handle in metadata.TypeReferences)
        {
            string name = names.GetTypeFromReference(metadata, handle, 0);
            if (C.Domain.Contains(C.Short(name))) violations.Add("trace 类型不能是外部 TypeRef：" + name);
        }
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            string owner = names.GetTypeFromDefinition(metadata, handle, 0), shortOwner = C.Short(owner);
            bool core = C.Core.Contains(shortOwner), carrier = C.Carriers.Contains(shortOwner);
            bool bridge = carrier || shortOwner == "PyTraceProduct";
            if (core) found.Add(shortOwner);
            if (carrier) carriers.Add(shortOwner);
            if ((core || carrier) && (type.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NotPublic)
                violations.Add(owner + " 必须 internal。");
            if (carrier)
            {
                var fields = type.GetFields().Select(h => { var f = metadata.GetFieldDefinition(h);
                    return new C.Storage(metadata.GetString(f.Name), C.Short(f.DecodeSignature(names, null)), (int)f.Attributes); });
                if (!fields.OrderBy(f => f.Name, StringComparer.Ordinal).SequenceEqual(C.CarrierFields(shortOwner).OrderBy(f => f.Name, StringComparer.Ordinal)))
                    violations.Add(owner + " 的固定字段形状改变。");
                if ((type.Attributes & TypeAttributes.Sealed) == 0) violations.Add(owner + " 必须 sealed。");
            }
            Check(owner, owner, "base", Resolve(metadata, type.BaseType, names));
            foreach (var h in type.GetInterfaceImplementations())
                Check(owner, owner, "interface", Resolve(metadata, metadata.GetInterfaceImplementation(h).Interface, names));
            Attributes(owner, owner, type.GetCustomAttributes());
            Constraints(owner, owner, type.GetGenericParameters());
            foreach (var h in type.GetFields())
            {
                var field = metadata.GetFieldDefinition(h);
                string source = Resolve(metadata, h, names);
                if (shortOwner == "PyTraceProduct" && (!contract.KnowsMember(source, "field") || (int)field.Attributes != 0x11))
                    violations.Add(source + " 不是获准的私有静态 Active 字段。");
                Check(owner, source, "field", field.DecodeSignature(names, null));
            }
            foreach (var h in type.GetProperties())
            {
                var property = metadata.GetPropertyDefinition(h); string sig = Format(property.DecodeSignature(names, null));
                string source = owner + "::" + metadata.GetString(property.Name) + sig;
                if (bridge && !contract.KnowsMember(source, "property")) violations.Add("未批准的桥接属性：" + source);
                Check(owner, source, "property", sig);
            }
            foreach (var h in type.GetEvents())
            {
                var item = metadata.GetEventDefinition(h);
                if (bridge) violations.Add("桥接不允许事件：" + owner);
                Check(owner, owner + "::" + metadata.GetString(item.Name), "event", Resolve(metadata, item.Type, names));
            }
            foreach (var h in type.GetMethods())
            {
                var method = metadata.GetMethodDefinition(h); string source = Resolve(metadata, h, names);
                string methodName = metadata.GetString(method.Name);
                if (bridge && !contract.KnowsMember(source, "method")) violations.Add("未批准的桥接成员：" + source);
                if (shortOwner == "PyTraceProduct" && methodName == "NativeFresh")
                {
                    var parameters = method.GetGenericParameters().Select(p => metadata.GetGenericParameter(p)).ToArray();
                    if (parameters.Length != 1 || parameters[0].Index != 0 || parameters[0].Attributes != GenericParameterAttributes.None
                        || !parameters[0].GetConstraints().Select(c => Resolve(metadata, metadata.GetGenericParameterConstraint(c).Type, names))
                            .SequenceEqual(new[] { "RyuPy.Runtime.PyBaseException" }))
                        violations.Add("NativeFresh 的唯一 PyBaseException 泛型约束改变。");
                }
                if ((C.Domain.Contains(shortOwner) || shortOwner == ".<Module>") && methodName == ".cctor")
                    violations.Add(source + " 不允许静态初始化。");
                Check(owner, source, "method", Format(method.DecodeSignature(names, null)));
                Attributes(owner, source, method.GetCustomAttributes());
                Constraints(owner, source, method.GetGenericParameters());
                if (method.RelativeVirtualAddress == 0) continue;
                var body = pe.GetMethodBody(method.RelativeVirtualAddress);
                if (!body.LocalSignature.IsNil)
                    foreach (string local in metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(names, null))
                        Check(owner, source, "local", local);
                foreach (var region in body.ExceptionRegions)
                    if (region.Kind == ExceptionRegionKind.Catch) Check(owner, source, "catch", Resolve(metadata, region.CatchType, names));
                var il = DecodeTraceInstructions(body.GetILBytes()!, metadata, names);
                foreach (var instruction in il)
                    if (instruction.Target is { } target) { operands++; Check(owner, source, "IL", instruction.Op + " " + target); }
                bodies.Add(new(owner, source, (int)method.Attributes, il, body.ExceptionRegions.ToArray()));
            }
        }
        if (!C.Core.SetEquals(found)) violations.Add("原 17 Core 集合缺失。");
        if (!C.Carriers.SetEquals(carriers)) violations.Add("V1 三承载体集合缺失。");
        CheckV1InstructionBoundaries(bodies, violations);
        return new(found.OrderBy(x => x, StringComparer.Ordinal).ToArray(), bodies.Count, operands, edges.ToArray(), violations.ToArray());

        void Attributes(string owner, string source, CustomAttributeHandleCollection handles)
        {
            foreach (var h in handles)
            {
                string target = Resolve(metadata, metadata.GetCustomAttribute(h).Constructor, names);
                Check(owner, source, "attribute", target);
                if (target.Contains("System.Runtime.CompilerServices.ModuleInitializerAttribute::", StringComparison.Ordinal))
                    violations.Add(source + " 不允许 ModuleInitializer。");
            }
        }
        void Constraints(string owner, string source, GenericParameterHandleCollection handles)
        {
            foreach (var h in handles)
                foreach (var c in metadata.GetGenericParameter(h).GetConstraints())
                    Check(owner, source, "constraint", Resolve(metadata, metadata.GetGenericParameterConstraint(c).Type, names));
        }
    }

    private static TraceInstruction[] DecodeTraceInstructions(byte[] il, MetadataReader metadata, Signatures names)
    {
        var result = new List<TraceInstruction>(); int i = 0;
        while (i < il.Length)
        {
            int start = i; ushort code = il[i++];
            if (code == 0xfe) { if (i == il.Length) throw new InvalidDataException("截断 IL opcode。"); code = (ushort)(0xfe00 | il[i++]); }
            if (!Opcodes.TryGetValue(code, out var opcode)) throw new InvalidDataException("未知 IL opcode。");
            int size; string? target = null; long? integer = null;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch:
                    if (i + 4 > il.Length) throw new InvalidDataException("截断 switch。");
                    int count = BitConverter.ToInt32(il, i);
                    if (count < 0) throw new InvalidDataException("无效 switch 分支数。");
                    size = checked(4 + 4 * count); break;
                case OperandType.InlineField: case OperandType.InlineMethod: case OperandType.InlineSig:
                case OperandType.InlineTok: case OperandType.InlineType:
                case OperandType.InlineBrTarget: case OperandType.InlineI: case OperandType.ShortInlineR:
                case OperandType.InlineString: size = 4; break;
                default: throw new InvalidDataException("未知 IL operand。");
            }
            if (i + size > il.Length) throw new InvalidDataException("IL operand 超出方法体。");
            if (opcode.OperandType is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig
                or OperandType.InlineTok or OperandType.InlineType)
                target = Resolve(metadata, MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i)), names);
            if (opcode.OperandType == OperandType.InlineI8) integer = BitConverter.ToInt64(il, i);
            if (opcode.OperandType == OperandType.InlineI) integer = BitConverter.ToInt32(il, i);
            if (opcode.OperandType == OperandType.ShortInlineI) integer = (sbyte)il[i];
            if (opcode.OperandType == OperandType.ShortInlineBrTarget) integer = i + size + (sbyte)il[i];
            if (opcode.OperandType == OperandType.InlineBrTarget) integer = i + size + BitConverter.ToInt32(il, i);
            result.Add(new(start, opcode.Name!, target, integer)); i += size;
        }
        return result.ToArray();
    }
}
