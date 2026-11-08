// 读取本次构建的真实 PE，不以源码字符串替代调用、初始化或旧公开 ABI 证据。
// 反射只用于测试中的 IL opcode 表；事务语义测试始终直接调用 internal 类型。
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class TraceBaselineFactAttribute : FactAttribute
{
    public TraceBaselineFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RYUPY_TRACE_BASELINE_DLL")))
            Skip = "旧 ABI 比对需要明确指定历史构建 DLL；专用 S1 CI 必须提供并禁止跳过。";
    }
}

public partial class PyTraceStateArtifactTests
{
    private static string Candidate()
    {
        string loaded = typeof(PyTraceState).Assembly.Location;
        string? selected = Environment.GetEnvironmentVariable("RYUPY_TRACE_CANDIDATE_DLL");
        if (selected is null) return loaded;
        Assert.True(File.Exists(selected), "专用 CI 指定的本次 runtime DLL 必须存在。");
        Assert.Equal(File.ReadAllBytes(loaded), File.ReadAllBytes(selected));
        return selected;
    }

    private static void Evidence(string file, object data)
    {
        string? directory = Environment.GetEnvironmentVariable("RYUPY_TRACE_EVIDENCE_DIR");
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, file), JsonSerializer.Serialize(data));
    }

    [Fact]
    public void ProducedAssemblyHasOnlyApprovedV1BridgesAndNoCoreInitializers()
    {
        string candidate = Candidate();
        var audit = ScanV1(candidate);
        // 先保存完整失败明细，不能让 Assert 丢掉本次实际引用证据。
        Evidence("candidate-pe.json", new { candidate,
            sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(candidate))),
            audit.CoreTypes, audit.Bodies, audit.Operands, audit.Edges, audit.Violations,
            scope = "V1成员级PE边界/初始化；非全runtime形式化、默认行为或产品JIT/AOT验收" });
        Assert.Empty(audit.Violations);
    }

    [TraceBaselineFact]
    public void ProducedAssemblyPreservesEveryBaselinePublicSignature()
    {
        string? baseline = Environment.GetEnvironmentVariable("RYUPY_TRACE_BASELINE_DLL");
        Assert.False(string.IsNullOrEmpty(baseline));
        Assert.True(File.Exists(baseline));
        string candidate = Candidate();
        var before = PublicAbi(baseline!);
        var after = PublicAbi(candidate);
        Assert.NotEmpty(before);
        var expected = before.Concat(PyTraceStateV1ArtifactContract.PublicAdditions()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Evidence("baseline-abi.json", new { baseline, candidate, signatures = before.Length,
            candidateSignatures = after.Length, expectedAdditions = PyTraceStateV1ArtifactContract.PublicAdditions(),
            removed = before.Except(after).ToArray(), added = after.Except(before).ToArray(),
            expected, actual = after,
            baselineSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(baseline!))),
            candidateSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(candidate))),
            scope = "旧1659逐字段保留，仅允许12条V1增量；不覆盖默认参数、常量、全部属性或完整程序集scope" });
        Assert.True(PyTraceStateV1ArtifactContract.PreservesAbi(before, after), "历史ABI或精确V1增量不符；见baseline-abi.json。");
    }

    private static string[] PublicAbi(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var provider = new Signatures();
        var result = new List<string>();
        bool PublicType(TypeDefinitionHandle handle)
        {
            var type = reader.GetTypeDefinition(handle);
            var visibility = type.Attributes & TypeAttributes.VisibilityMask;
            if (visibility == TypeAttributes.Public) return true;
            return (visibility == TypeAttributes.NestedPublic || visibility == TypeAttributes.NestedFamily
                || visibility == TypeAttributes.NestedFamORAssem) && PublicType(type.GetDeclaringType());
        }
        bool PublicMethod(MethodDefinitionHandle handle)
        {
            if (handle.IsNil) return false;
            var access = reader.GetMethodDefinition(handle).Attributes & MethodAttributes.MemberAccessMask;
            return access is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;
        }
        string Generics(GenericParameterHandleCollection items) => string.Join(";", items.Select(item =>
        {
            var p = reader.GetGenericParameter(item);
            return p.Index + ":" + (int)p.Attributes + ":" + string.Join(",", p.GetConstraints().Select(c =>
                Resolve(reader, reader.GetGenericParameterConstraint(c).Type, provider)).OrderBy(x => x));
        }));
        foreach (var handle in reader.TypeDefinitions)
        {
            if (!PublicType(handle)) continue;
            var type = reader.GetTypeDefinition(handle);
            string name = provider.GetTypeFromDefinition(reader, handle, 0);
            result.Add("T:" + name + ":" + (int)type.Attributes + ":" + Resolve(reader, type.BaseType, provider) + ":" + Generics(type.GetGenericParameters()));
            foreach (var item in type.GetInterfaceImplementations())
                result.Add("I:" + name + ":" + Resolve(reader, reader.GetInterfaceImplementation(item).Interface, provider));
            foreach (var item in type.GetMethods())
            {
                if (!PublicMethod(item)) continue;
                var m = reader.GetMethodDefinition(item);
                result.Add("M:" + name + ":" + reader.GetString(m.Name) + ":" + (int)m.Attributes + ":" +
                    Format(m.DecodeSignature(provider, null)) + ":" + Generics(m.GetGenericParameters()));
            }
            foreach (var item in type.GetFields())
            {
                var f = reader.GetFieldDefinition(item);
                var access = f.Attributes & FieldAttributes.FieldAccessMask;
                if (access is FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem)
                    result.Add("F:" + name + ":" + reader.GetString(f.Name) + ":" + (int)f.Attributes + ":" + f.DecodeSignature(provider, null));
            }
            foreach (var item in type.GetProperties())
            {
                var p = reader.GetPropertyDefinition(item); var a = p.GetAccessors();
                if (PublicMethod(a.Getter) || PublicMethod(a.Setter) || a.Others.Any(PublicMethod))
                    result.Add("P:" + name + ":" + reader.GetString(p.Name) + ":" + (int)p.Attributes + ":" + Format(p.DecodeSignature(provider, null)));
            }
            foreach (var item in type.GetEvents())
            {
                var e = reader.GetEventDefinition(item); var a = e.GetAccessors();
                if (PublicMethod(a.Adder) || PublicMethod(a.Remover) || PublicMethod(a.Raiser) || a.Others.Any(PublicMethod))
                    result.Add("E:" + name + ":" + reader.GetString(e.Name) + ":" + (int)e.Attributes + ":" + Resolve(reader, e.Type, provider));
            }
        }
        return result.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static string Format(MethodSignature<string> s) =>
        $"[{s.Header.RawValue};{s.GenericParameterCount};{s.RequiredParameterCount}]({string.Join(',', s.ParameterTypes)})->{s.ReturnType}";

    private static string Resolve(MetadataReader r, EntityHandle h, Signatures p)
    {
        if (h.IsNil) return "";
        switch (h.Kind)
        {
            case HandleKind.TypeDefinition: return p.GetTypeFromDefinition(r, (TypeDefinitionHandle)h, 0);
            case HandleKind.TypeReference: return p.GetTypeFromReference(r, (TypeReferenceHandle)h, 0);
            case HandleKind.TypeSpecification: return p.GetTypeFromSpecification(r, null, (TypeSpecificationHandle)h, 0);
            case HandleKind.MethodDefinition:
                var m = r.GetMethodDefinition((MethodDefinitionHandle)h);
                return Resolve(r, m.GetDeclaringType(), p) + "::" + r.GetString(m.Name) + Format(m.DecodeSignature(p, null));
            case HandleKind.FieldDefinition:
                var f = r.GetFieldDefinition((FieldDefinitionHandle)h);
                return Resolve(r, f.GetDeclaringType(), p) + "::" + r.GetString(f.Name) + ":" + f.DecodeSignature(p, null);
            case HandleKind.MemberReference:
                var member = r.GetMemberReference((MemberReferenceHandle)h);
                return Resolve(r, member.Parent, p) + "::" + r.GetString(member.Name) +
                    (member.GetKind() == MemberReferenceKind.Method ? Format(member.DecodeMethodSignature(p, null)) : ":" + member.DecodeFieldSignature(p, null));
            case HandleKind.MethodSpecification:
                var spec = r.GetMethodSpecification((MethodSpecificationHandle)h);
                return Resolve(r, spec.Method, p) + "<" + string.Join(",", spec.DecodeSignature(p, null)) + ">";
            case HandleKind.StandaloneSignature:
                var sig = r.GetStandaloneSignature((StandaloneSignatureHandle)h);
                return sig.GetKind() == StandaloneSignatureKind.LocalVariables
                    ? string.Join(",", sig.DecodeLocalSignature(p, null)) : Format(sig.DecodeMethodSignature(p, null));
            case HandleKind.ModuleReference: return r.GetString(r.GetModuleReference((ModuleReferenceHandle)h).Name);
            default: throw new InvalidDataException("未覆盖的元数据引用: " + h.Kind);
        }
    }

    // 测试端只建一次 opcode 查表；不是 runtime 静态初始化。
    private static readonly Dictionary<ushort, OpCode> Opcodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null)!).GroupBy(op => unchecked((ushort)op.Value))
        .ToDictionary(group => group.Key, group => group.First());

    private sealed class Signatures : ISignatureTypeProvider<string, object?>
    {
        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + shape.Rank + ";" +
            string.Join(",", shape.Sizes) + ";" + string.Join(",", shape.LowerBounds) + "]";
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetFunctionPointerType(MethodSignature<string> signature) => "fn:" + Format(signature);
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> arguments) => genericType + "<" + string.Join(",", arguments) + ">";
        public string GetGenericMethodParameter(object? context, int index) => "!!" + index;
        public string GetGenericTypeParameter(object? context, int index) => "!" + index;
        public string GetModifiedType(string modifier, string unmodifiedType, bool required) => unmodifiedType + (required ? "modreq" : "modopt") + modifier;
        public string GetPinnedType(string elementType) => elementType + " pinned";
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetPrimitiveType(PrimitiveTypeCode code) => code.ToString();
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var type = reader.GetTypeDefinition(handle); string name = reader.GetString(type.Name);
            return type.IsNested ? GetTypeFromDefinition(reader, type.GetDeclaringType(), rawTypeKind) + "+" + name
                : reader.GetString(type.Namespace) + "." + name;
        }
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var type = reader.GetTypeReference(handle); string name = reader.GetString(type.Name);
            return type.ResolutionScope.Kind == HandleKind.TypeReference
                ? GetTypeFromReference(reader, (TypeReferenceHandle)type.ResolutionScope, rawTypeKind) + "+" + name
                : reader.GetString(type.Namespace) + "." + name;
        }
        public string GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte rawTypeKind)
            => reader.GetTypeSpecification(handle).DecodeSignature(this, context);
    }
}
