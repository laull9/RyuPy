// 有限纯解码/validator自检：合成元数据及IL字节，不执行合成程序集，不计Python产品覆盖。
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using static GeneratedPeChecks;

internal static class GeneratedPeChecksTests
{
    private static void Require(bool condition, string label)
    { if (!condition) throw new InvalidOperationException("PE合成自检失败: " + label); }

    private static byte[] InstructionBytes(int opcode, int token)
    {
        var bytes = new List<byte>();
        if (opcode > 255) bytes.Add(0xfe);
        bytes.Add((byte)opcode);
        bytes.AddRange(BitConverter.GetBytes(token));
        return bytes.ToArray();
    }

    internal static void Run(string evidencePath)
    {
        var passed = new List<string>();
        string? failure = null;
        try
        {
            var builder = new MetadataBuilder();
            builder.AddModule(0, builder.GetOrAddString("synthetic-audit-only"), default, default, default);
            var assembly = builder.AddAssemblyReference(builder.GetOrAddString(Runtime.Name), new Version(Runtime.Version),
                default, default, (AssemblyFlags)0, default);
            var adapter = builder.AddTypeReference(assembly, builder.GetOrAddString("RyuPy.Runtime"), builder.GetOrAddString("PyTraceAdapter"));
            var ops = builder.AddTypeReference(assembly, builder.GetOrAddString("RyuPy.Runtime"), builder.GetOrAddString("PyOps"));
            MemberReferenceHandle Member(EntityHandle owner, Target target) => builder.AddMemberReference(owner,
                builder.GetOrAddString(target.Name), builder.GetOrAddBlob(Convert.FromHexString(target.Signature)));
            var explicitRef = Member(adapter, Explicit);
            var handleRef = Member(adapter, Handle);
            var localsRef = Member(ops, Locals);
            var genericRef = builder.AddMemberReference(adapter, builder.GetOrAddString("GenericOnlyForDecoder"),
                builder.GetOrAddBlob(new byte[] { 0x10, 1, 0, 1 })); // static generic<T> void()
            var spec = builder.AddMethodSpecification(genericRef, builder.GetOrAddBlob(new byte[] { 0x0a, 1, 0x1c }));
            var invalidSpec = builder.AddMethodSpecification(explicitRef, builder.GetOrAddBlob(new byte[] { 0x0a, 1, 0x1c }));
            var root = new BlobBuilder();
            new MetadataRootBuilder(builder).Serialize(root, 0, 0);
            using var provider = MetadataReaderProvider.FromMetadataImage(root.ToArray().ToImmutableArray());
            var metadata = provider.GetMetadataReader();
            List<Site> Sites(byte[] bytes) => Decode(bytes).Where(i => MethodOperand(i.Opcode))
                .Select(i => new Site(0x06000001, "synthetic", i.Offset, i.Opcode,
                    Resolve(metadata, MetadataTokens.EntityHandle(i.Token!.Value)))).ToList();
            var nativeBytes = InstructionBytes(0x28, MetadataTokens.GetToken(localsRef))
                .Concat(InstructionBytes(0x28, MetadataTokens.GetToken(handleRef))).Concat(new byte[] { 0x2a }).ToArray();
            var native = Sites(nativeBytes);
            Require(Resolve(metadata, explicitRef) == Explicit && Validate(native, true) == Verdict.Accepted,
                "unused Explicit MemberRef允许");
            passed.Add("unused-memberref-with-complete-native-calls");
            // 先确认control完整，再仅加禁项；不能靠缺locals使负例假阳性。
            foreach (int opcode in new[] { 0x28, 0x6f, 0xfe06, 0xfe07 })
            {
                var contaminated = native.Concat(Sites(InstructionBytes(opcode, MetadataTokens.GetToken(explicitRef)))).ToList();
                Require(Validate(contaminated, true) == Verdict.ForbiddenExplicit, "actual Explicit或函数指针必须精确拒绝");
            }
            passed.Add("actual-explicit-call-callvirt-ldftn-ldvirtftn-forbidden");
            var explicitSites = Sites(InstructionBytes(0x28, MetadataTokens.GetToken(explicitRef)))
                .Concat(Sites(InstructionBytes(0x28, MetadataTokens.GetToken(handleRef)))).ToList();
            Require(Validate(explicitSites, false) == Verdict.Accepted, "旧explicit正例同步实际调用");
            passed.Add("explicit-positive-real-call-validator");
            foreach (var wrong in new[] {
                Locals with { Owner = Locals.Owner with { Name = "Other.PyOps" } },
                Locals with { Owner = Locals.Owner with { Assembly = Runtime with { Name = "Other.Runtime" } } },
                Locals with { Owner = Locals.Owner with { Assembly = Runtime with { Version = "2.0.0.0" } } },
                Locals with { Signature = "000001" },
                Locals with { Instantiation = "0A011C" },
            })
            {
                var forged = new[] { native[0] with { Target = wrong }, native[1] };
                Require(Validate(forged, true) == Verdict.MissingLocals, "同名不同owner/assembly/signature不得冒充");
            }
            passed.Add("exact-owner-assembly-version-signature-instantiation");
            var pointersOnly = native.Select(s => s with { Opcode = 0xfe06 }).ToArray();
            Require(Validate(pointersOnly, true) == Verdict.MissingLocals, "函数指针存在不能替代必需实际call");
            passed.Add("required-source-is-call-not-pointer");
            foreach (int opcode in new[] { 0x28, 0xfe06, 0xfe07 })
            {
                var generic = Sites(InstructionBytes(opcode, MetadataTokens.GetToken(spec))).Single();
                Require(generic.Target.Name == "GenericOnlyForDecoder" && generic.Target.Owner == Explicit.Owner
                    && generic.Target.Signature == "10010001" && generic.Target.Instantiation == "0A011C", "MethodSpec精确解包");
            }
            bool rejectedSpec = false;
            try { Resolve(metadata, invalidSpec); }
            catch (BadImageFormatException) { rejectedSpec = true; }
            Require(rejectedSpec, "non-generic Explicit不能伪装MethodSpec");
            passed.Add("methodspec-call-and-function-pointer-unwrapping");
            // 每类宽度包括二字节/变量/浮点/branch/switch，常数里的0x28不是call。
            byte[] widths = { 0x21, 0x28, 0, 0, 0, 0, 0, 0, 0, 0x22, 0, 0, 0, 0,
                0xfe, 0x09, 1, 0, 0xfe, 0x12, 1, 0x2b, 0, 0x38, 0, 0, 0, 0,
                0x45, 1, 0, 0, 0, 0, 0, 0, 0, 0xb3, 0x2a };
            var decoded = Decode(widths);
            Require(decoded.Count == 9 && decoded.All(i => !MethodOperand(i.Opcode)), "各种operand宽度精确");
            passed.Add("all-operand-width-classes-and-embedded-call-byte");
            foreach (byte[] bad in new byte[][] {
                new byte[] { 0x24 }, new byte[] { 0xfe }, new byte[] { 0xfe, 0x08 },
                new byte[] { 0x28, 1, 2 }, new byte[] { 0xfe, 0x09, 1 },
                new byte[] { 0x45, 1, 0, 0, 0 }, new byte[] { 0x45, 255, 255, 255, 255 },
            })
            {
                bool rejected = false;
                try { Decode(bad); }
                catch (BadImageFormatException) { rejected = true; }
                Require(rejected, "unknown/truncated必须拒绝");
            }
            passed.Add("unknown-and-truncated-opcodes-operands-switch-rejected");
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            File.WriteAllText(evidencePath, JsonSerializer.Serialize(new {
                Scope = "synthetic decoder/validator only; no product execution", Passed = passed, Failure = failure,
            }));
        }
    }
}
