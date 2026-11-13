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
    private sealed record LoadedImage(string Role, string Path, long Bytes, string Sha256,
        string? CopiedPath, string? CopiedSha256);

    private static LoadedImage[] LoadedImages()
    {
        LoadedImage Image(string role, Assembly assembly)
        {
            string path = assembly.Location;
            byte[] bytes = File.ReadAllBytes(path);
            // 只有原 fullcheck 在 build 后保存这两个输入；独立 S1 不冒用 Debug 副本。
            string? fullcheck = Environment.GetEnvironmentVariable("FULL_CHECK_EVIDENCE");
            string? copied = fullcheck is null ? null
                : System.IO.Path.Combine(fullcheck, "managed-test-inputs", System.IO.Path.GetFileName(path));
            string? copiedSha256 = copied is null ? null
                : Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(copied)));
            return new(role, path, bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)), copied, copiedSha256);
        }
        return [Image("runtime", typeof(PyTraceState).Assembly), Image("tests", typeof(PyTraceStateArtifactTests).Assembly)];
    }

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
        AssertCandidatePe(Candidate(), "candidate-pe.json");
    }

    private static void AssertCandidatePe(string candidate, string evidenceFile)
    {
        // 与原测试同进程的实际已加载程序集；独立留证文件避免三个前置互相覆盖。
        var loadedBefore = LoadedImages();
        var audit = ScanV1(candidate);
        var loadedAfter = LoadedImages();
        string candidateSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(candidate)));
        // 先保存完整失败明细，不能让 Assert 丢掉本次实际引用证据。
        Evidence(evidenceFile, new { candidate,
            sha256 = candidateSha256,
            loadedBefore, loadedAfter, phase = "original-candidate", mutationStarted = false,
            audit.CoreTypes, audit.Bodies, audit.Operands, audit.Edges, audit.Violations,
            scope = "V1成员级PE边界/初始化；非全runtime形式化、默认行为或产品JIT/AOT验收" });
        Assert.Equal(loadedBefore, loadedAfter);
        Assert.Equal(loadedAfter[0].Sha256, candidateSha256);
        foreach (var image in loadedAfter)
            if (image.CopiedPath is not null) Assert.Equal(image.Sha256, image.CopiedSha256);
        // 谓词仍是完整 Violations 为空；不用集合格式化的省略号代替真实失败字符串。
        Assert.True(audit.Violations.Length == 0, "原始候选 PE 违规：\n" + string.Join("\n", audit.Violations));
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
        var v1Additions = PyTraceStateV1ArtifactContract.PublicAdditions();
        var sourceAdditions = SourcePublicAdditions();
        var metadataAdditions = MetadataPublicAdditions();
        var builtinAdditions = BuiltinPublicAdditions();
        var xmlAdditions = XmlPublicAdditions();
        var deploymentAdditions = DefaultSourcePublicAdditions();
        var factoryAdditions = LexicalClassFactoryPublicAdditions();
        var globalsAdditions = DirectGlobalsPublicAdditions();
        var miAdditions = OrdinaryMiPublicAdditions();
        var runtimeDerivedAdditions = RuntimeDerivedPublicAdditions();
        var resourceContextAdditions = ResourceContextPublicAdditions();
        var nativeAbiAdditions = NativeAbiPublicAdditions();
        var additions = v1Additions.Concat(sourceAdditions).Concat(metadataAdditions).Concat(builtinAdditions).Concat(xmlAdditions).Concat(deploymentAdditions).Concat(factoryAdditions).Concat(globalsAdditions).Concat(miAdditions).Concat(runtimeDerivedAdditions).Concat(resourceContextAdditions).Concat(nativeAbiAdditions).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var expected = before.Concat(additions).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Evidence("baseline-abi.json", new { baseline, candidate, signatures = before.Length,
            candidateSignatures = after.Length, expectedAdditions = additions, v1Additions, sourceAdditions, metadataAdditions, builtinAdditions, xmlAdditions, deploymentAdditions, factoryAdditions, globalsAdditions, miAdditions, runtimeDerivedAdditions, resourceContextAdditions, nativeAbiAdditions,
            removed = before.Except(after).ToArray(), added = after.Except(before).ToArray(),
            expected, actual = after,
            baselineSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(baseline!))),
            candidateSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(candidate))),
            scope = "旧1722逐字段保留；新增ADR0173声明式C ABI调用帧及返回物化入口；不覆盖默认参数、常量、全部属性或完整程序集scope" });
        Assert.True(PreservesBaselineAbi(before, after), "历史ABI、精确V1、冷来源、metadata或默认builtins、XML异常或默认来源/类工厂冷增量不符；见baseline-abi.json。");
    }

    // 全程序集门另列 ADR0145 冷 API；不改变 V1 专属十二条合同，也不接受其它来源 API/重载。
    internal static string[] SourcePublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyOps:FunctionDefinitionWithSource:150:[0;0;4](Object,String,Int64,Object)->Void:",
        "M:RyuPy.Runtime.PyTraceback:CreateLegacyCodeSourceV1:150:[0;0;3](String,String,RyuPy.Runtime.PyModuleGet)->Object:",
        "M:RyuPy.Runtime.PyTraceback:CreateCodeSource:150:[0;0;3](String,String,RyuPy.Runtime.PyModuleGet)->Object:",
        "M:RyuPy.Runtime.PyTraceback:CreateModuleExecutionSourceWithCode:150:[0;0;2](Object,Object)->Object:"
    ];

    // ADR0150 独立审准的冷部署/模块对象适配表；不从候选 PE 差集生成许可。
    internal static string[] MetadataPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyDistributionSnapshot:1048961:System.Object:",
        "M:RyuPy.Runtime.PyDistributionSnapshot:Configure:150:[0;0;2](System.Type,String)->Void:",
        "T:RyuPy.Runtime.PyEntryPoint:1048833:RyuPy.Runtime.PyNativeObject:",
        "F:RyuPy.Runtime.PyEntryPoint:Type:54:RyuPy.Runtime.PyNativeType",
        "M:RyuPy.Runtime.PyEntryPoint:GetAttr:198:[32;0;1](String)->Object:",
        "M:RyuPy.Runtime.PyEntryPoint:Load:134:[32;0;0]()->Object:",
        "M:RyuPy.Runtime.PyEntryPoint:get_Name:2182:[32;0;0]()->String:",
        "M:RyuPy.Runtime.PyEntryPoint:get_Value:2182:[32;0;0]()->String:",
        "M:RyuPy.Runtime.PyEntryPoint:get_Group:2182:[32;0;0]()->String:",
        "M:RyuPy.Runtime.PyEntryPoint:get_TypeName:2246:[32;0;0]()->String:",
        "P:RyuPy.Runtime.PyEntryPoint:Name:0:[40;0;0]()->String",
        "P:RyuPy.Runtime.PyEntryPoint:Value:0:[40;0;0]()->String",
        "P:RyuPy.Runtime.PyEntryPoint:Group:0:[40;0;0]()->String",
        "P:RyuPy.Runtime.PyEntryPoint:TypeName:0:[40;0;0]()->String",
        "T:RyuPy.Runtime.PyEntryPoints:1048833:RyuPy.Runtime.PyNativeObject:",
        "F:RyuPy.Runtime.PyEntryPoints:Type:54:RyuPy.Runtime.PyNativeType",
        "M:RyuPy.Runtime.PyEntryPoints:GetAttr:198:[32;0;1](String)->Object:",
        "M:RyuPy.Runtime.PyEntryPoints:get_TypeName:2246:[32;0;0]()->String:",
        "P:RyuPy.Runtime.PyEntryPoints:TypeName:0:[40;0;0]()->String",
        "T:RyuPy.Runtime.PyImportlib:1048961:System.Object:",
        "M:RyuPy.Runtime.PyImportlib:CreateAttributes:150:[0;0;0]()->System.Collections.Generic.Dictionary`2<String,Object>:",
        "M:RyuPy.Runtime.PyImportlib:ImportModule:150:[0;0;2](Object,Object)->RyuPy.Runtime.PyModule:",
        "T:RyuPy.Runtime.PyImportlibMetadata:1048961:System.Object:",
        "M:RyuPy.Runtime.PyImportlibMetadata:Register:150:[0;0;0]()->Void:"
    ];

    // 本切只增加一个精确冷读取 ABI；原 1699 条由历史基线与原三表共同逐条保留。
    internal static string[] BuiltinPublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyOps:LoadDefaultBuiltin:150:[0;0;1](String)->Object:"
    ];

    // TreeBuilder本身及cold producer均internal；仅异常CLR父链和必要消息覆盖进入公开ABI。
    internal static string[] XmlPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PySyntaxError:1048577:RyuPy.Runtime.PyException:",
        "M:RyuPy.Runtime.PySyntaxError:.ctor:6278:[32;0;1](String)->Void:",
        "M:RyuPy.Runtime.PySyntaxError:get_PyMessage:2246:[32;0;0]()->String:",
        "P:RyuPy.Runtime.PySyntaxError:PyMessage:0:[40;0;0]()->String",
        "T:RyuPy.Runtime.PyXmlParseError:1048833:RyuPy.Runtime.PySyntaxError:",
        "M:RyuPy.Runtime.PyXmlParseError:.ctor:6278:[32;0;1](String)->Void:"
    ];

    // ADR0155只准这四条冷登记ABI；手写固定签名，不从候选PE差集推导许可。
    internal static string[] DefaultSourcePublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyDistributionSnapshot:ConfigureAutoUnavailable:150:[0;0;2](System.Type,String)->Void:",
        "M:RyuPy.Runtime.PyModules:ConfigureOptionalSources:150:[0;0;1](String)->Void:",
        "M:RyuPy.Runtime.PyModules:RegisterCandidateInstance:150:[0;0;7](String,String,String,Int64,String,Boolean,RyuPy.Runtime.PyModuleExecute)->Void:",
        "M:RyuPy.Runtime.PyModules:RegisterCandidateSource:150:[0;0;10](String,String,String,Int64,String,Boolean,Boolean,RyuPy.Runtime.PyModuleInit,RyuPy.Runtime.PyModuleGet,RyuPy.Runtime.PyModuleSet)->Void:"
    ];

    // ADR0156精确六条；公开static类型本身也计一条，不从候选PE差集授权。
    internal static string[] LexicalClassFactoryPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyLexicalClassFactory:1048961:System.Object:",
        "M:RyuPy.Runtime.PyLexicalClassFactory:Prepare:150:[0;0;4](String,String,Object,Object[])->Object:",
        "M:RyuPy.Runtime.PyLexicalClassFactory:NamespaceContains:150:[0;0;2](Object,String)->Boolean:",
        "M:RyuPy.Runtime.PyLexicalClassFactory:NamespaceGet:150:[0;0;2](Object,String)->Object:",
        "M:RyuPy.Runtime.PyLexicalClassFactory:NamespaceSet:150:[0;0;3](Object,String,Object)->Void:",
        "M:RyuPy.Runtime.PyLexicalClassFactory:Finish:150:[0;0;1](Object)->Object:"
    ];

    internal static string[] DirectGlobalsPublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyModuleInstances:CallDirectGlobals:150:[0;0;2](Object,Object)->Object:"
    ];

    internal static string[] OrdinaryMiPublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyOps:PrepareOrdinaryMiClass:150:[0;0;2](String,Object[])->Object:"
    ];

    internal static string[] RuntimeDerivedPublicAdditions() =>
    [
        "M:RyuPy.Runtime.PyOps:PrepareRuntimeDerivedClass:150:[0;0;2](String,Object[])->Object:"
    ];

    internal static bool PreservesBaselineAbi(string[] before, string[] after)
        => PreservesBaselineAbi(before, after, PyTraceStateV1ArtifactContract.PublicAdditions(),
            SourcePublicAdditions(), MetadataPublicAdditions(), BuiltinPublicAdditions(), XmlPublicAdditions());

    // 参数化仅供反例检验允许表自身；真实 PE 门始终使用逐条审准的固定合同表。
    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin)
        => PreservesBaselineAbi(before, after, v1, source, metadata, builtin, XmlPublicAdditions());

    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin, string[] xml)
        => PreservesBaselineAbi(before, after, v1, source, metadata, builtin, xml, DefaultSourcePublicAdditions());

    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin, string[] xml, string[] deployment)
        => PreservesBaselineAbi(before, after, v1, source, metadata, builtin, xml, deployment, LexicalClassFactoryPublicAdditions());

    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin, string[] xml, string[] deployment, string[] factory)
        => PreservesBaselineAbi(before, after, v1, source, metadata, builtin, xml, deployment, factory, DirectGlobalsPublicAdditions());

    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin, string[] xml, string[] deployment, string[] factory, string[] globals)
        => PreservesBaselineAbi(before, after, v1, source, metadata, builtin, xml, deployment, factory, globals, OrdinaryMiPublicAdditions());

    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin, string[] xml, string[] deployment, string[] factory, string[] globals, string[] mi)
        => PreservesBaselineAbi(before, after, v1, source, metadata, builtin, xml, deployment, factory, globals, mi, RuntimeDerivedPublicAdditions());

    internal static bool PreservesBaselineAbi(string[] before, string[] after,
        string[] v1, string[] source, string[] metadata, string[] builtin, string[] xml, string[] deployment, string[] factory, string[] globals, string[] mi, string[] runtimeDerived)
    {
        if (before.Length != 1659 || after.Length != CurrentPublicAbiCount
            || v1.Length != 12 || source.Length != 4 || metadata.Length != 24
            || !builtin.SequenceEqual(BuiltinPublicAdditions()) || !xml.SequenceEqual(XmlPublicAdditions())
            || !deployment.SequenceEqual(DefaultSourcePublicAdditions())
            || !factory.SequenceEqual(LexicalClassFactoryPublicAdditions())
            || !globals.SequenceEqual(DirectGlobalsPublicAdditions())
            || !mi.SequenceEqual(OrdinaryMiPublicAdditions())
            || !runtimeDerived.SequenceEqual(RuntimeDerivedPublicAdditions())) return false;
        var expected = before.Concat(v1).Concat(source).Concat(metadata).Concat(builtin).Concat(xml).Concat(deployment).Concat(factory).Concat(globals).Concat(mi).Concat(runtimeDerived).Concat(ResourceContextPublicAdditions()).Concat(NativeAbiPublicAdditions()).ToArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        // 不用去重后的集合判等：任一组内或组间重复直接拒绝。
        if (expected.Any(signature => !seen.Add(signature))) return false;
        return expected.OrderBy(x => x, StringComparer.Ordinal)
            .SequenceEqual(after.OrderBy(x => x, StringComparer.Ordinal));
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
