// V1 的手写语义边界。记录的是用途明确的成员身份，不是候选 PE 的输出快照。
// 原 Core 内部仍由事务测试负责；域外及桥接间的未知成员、签名和操作一律拒绝。
using System.Reflection;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime.Tests;

internal sealed partial class PyTraceStateV1ArtifactContract
{
    internal static readonly HashSet<string> Core = new(StringComparer.Ordinal)
    {
        "PyTraceState", "PyTraceContextAuthority", "TraceEpoch", "TraceRotatePermit", "TraceReject",
        "TraceChange", "TraceResult", "TraceBeginKind", "TraceReceiveKind", "TraceInclusion",
        "TraceFrame", "TraceRequest", "TraceBoundary", "TraceReceive", "TraceReceipt",
        "TraceSnapshot", "TraceBirthGrant"
    };
    internal static readonly HashSet<string> Carriers = new(StringComparer.Ordinal)
        { "TraceProductBinding", "TraceProductScope", "TraceDispatchPark" };
    internal static readonly HashSet<string> Domain = new(Core.Concat(Carriers).Append("PyTraceProduct"), StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> rules = new(StringComparer.Ordinal);
    internal readonly record struct Edge(string Owner, string Source, string Kind, string Target);
    internal readonly record struct Storage(string Name, string Type, int Attributes);

    internal static string Short(string text) => text.Replace("RyuPy.Runtime.", "", StringComparison.Ordinal)
        .Replace("System.Collections.ObjectModel.ReadOnlyCollection`1", "ReadOnlyCollection", StringComparison.Ordinal)
        .Replace("System.Collections.Generic.EqualityComparer`1", "EqualityComparer", StringComparison.Ordinal)
        .Replace("System.Collections.Generic.Stack`1", "Stack", StringComparison.Ordinal)
        .Replace("System.Nullable`1", "Nullable", StringComparison.Ordinal);
    internal static bool MentionsDomain(string text) => Regex.Matches(Short(text), @"[A-Za-z_][A-Za-z0-9_.+`]*")
        .Any(match => Domain.Contains(match.Value));
    internal static string Signature(int header, int generics, string result, params string[] args)
        => $"[{header};{generics};{args.Length}]({string.Join(',', args)})->{result}";
    internal static string Method(string owner, string name, string result, bool instance = false, params string[] args)
        => owner + "::" + name + Signature(instance ? 32 : 0, 0, result, args);
    private static string P(string name, string result, params string[] args) => Method("PyTraceProduct", name, result, false, args);
    private static string I(string owner, string name, string result, params string[] args) => Method(owner, name, result, true, args);
    private static string S(string owner, string name, string result, params string[] args) => Method(owner, name, result, false, args);
    private static string Getter(string owner, string name, string result) => I(owner, "get_" + name, result);
    private static string Field(string owner, string name, string type) => owner + "::" + name + ":" + type;
    private static string Call(string target) => "call " + target;
    private static string Virtual(string target) => "callvirt " + target;
    private static string New(string target) => "newobj " + target;
    private static string Read(string target) => "ldfld " + target;
    private static string Write(string target) => "stfld " + target;
    private const string Fresh = "PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0";
    private static string FreshCall(string type) => Call(Fresh + "<" + type + ">");

    internal PyTraceStateV1ArtifactContract()
    {
        ProductRules(); ProjectionAndProducerRules();
        RecordRules("TraceProductBinding", [("Scope", "TraceProductScope", false), ("Owner", "PyTraceState", false)]);
        RecordRules("TraceDispatchPark", [("Lineage", "TraceRequest", true), ("Boundary", "TraceBoundary", true),
            ("Scene", "ReadOnlyCollection<TraceFrame>", false), ("Remaining", "Int32", true)]);
        Add("TraceProductScope", "field", "Authority", "PyTraceContextAuthority");
        Rule(I("TraceProductScope", ".ctor", "Void"), [],
            New(I("PyTraceContextAuthority", ".ctor", "Void")), Write(Field("TraceProductScope", "Authority", "PyTraceContextAuthority")),
            Write(Field("TraceProductScope", "Generators", "Stack<Int64>")));
    }

    private void Add(string source, string kind, params string[] targets)
    {
        string key = source + "|" + kind;
        if (!rules.TryGetValue(key, out var values)) rules.Add(key, values = new(StringComparer.Ordinal));
        values.UnionWith(targets);
    }
    private void Add(string owner, string kind, string name, string type)
        => Add(Field(owner, name, type), kind, type);
    private void Rule(string source, string[] locals, params string[] operations)
    {
        Add(source, "method", source[source.IndexOf('[')..]);
        Add(source, "local", locals);
        Add(source, "IL", operations);
    }
    internal bool KnowsMember(string source, string kind) => rules.ContainsKey(Short(source) + "|" + kind);
    internal bool Allows(Edge edge)
    {
        string owner = Short(edge.Owner), source = Short(edge.Source), target = Short(edge.Target);
        if (Core.Contains(owner)) return true;
        return rules.TryGetValue(source + "|" + edge.Kind, out var targets) && targets.Contains(target);
    }

    // 只对实际 Generators 字段的用途开放 Count/Peek/Push/Pop，未知容器方法仍拒绝。
    internal static bool AllowsGeneratorStackUse(string source, string opcode, string target)
    {
        if (opcode != "callvirt") return false;
        source = Short(source); target = Short(target);
        string count = Getter("Stack<Int64>", "Count", "Int32"), peek = I("Stack<Int64>", "Peek", "!0");
        if (source == P("Exit", "Void")) return target == count;
        if (source == P("GeneratorDepth", "Int64") || source == P("GeneratorCatch", "Void", "Object")) return target == peek;
        if (source == P("EnterGenerator", "Void", "Int64")) return target == I("Stack<Int64>", "Push", "Void", "!0");
        if (source == P("LeaveGenerator", "Void", "Int64")) return target == count || target == I("Stack<Int64>", "Pop", "!0");
        return false;
    }

    // 固定字段集合同时阻止新的 History 缓存、额外消费位和另一份 owner 权威。
    internal static Storage[] CarrierFields(string owner) => owner switch
    {
        "TraceProductBinding" => [new("<Scope>k__BackingField", "TraceProductScope", 0x21), new("<Owner>k__BackingField", "PyTraceState", 0x21)],
        "TraceProductScope" => [new("Authority", "PyTraceContextAuthority", 0x23), new("Generators", "Stack<Int64>", 0x23),
            new("Activation", "Int64", 3), new("Flights", "Int64", 3), new("Parks", "Int64", 3)],
        "TraceDispatchPark" => [new("<Lineage>k__BackingField", "TraceRequest", 0x21), new("<Boundary>k__BackingField", "TraceBoundary", 0x21),
            new("<Scene>k__BackingField", "ReadOnlyCollection<TraceFrame>", 0x21), new("<Remaining>k__BackingField", "Int32", 0x21)],
        _ => throw new InvalidDataException("未知 trace 承载体：" + owner)
    };

    // 仅这两个具体 sealed record 的机械成员模板，不授予新 record、字段或任意合成方法权限。
    private void RecordRules(string owner, (string Name, string Type, bool Value)[] fields)
    {
        Add(owner, "interface", "System.IEquatable`1<" + owner + ">");
        Add(owner + "::EqualityContract" + Signature(40, 0, "System.Type"), "property", Signature(40, 0, "System.Type"));
        var reads = fields.Select(f => Read(Field(owner, "<" + f.Name + ">k__BackingField", f.Type))).ToArray();
        var writes = fields.Select(f => Write(Field(owner, "<" + f.Name + ">k__BackingField", f.Type))).ToArray();
        string copy = I(owner, ".ctor", "Void", owner), equals = I(owner, "Equals", "Boolean", owner);
        string equality = Getter(owner, "EqualityContract", "System.Type");
        Rule(I(owner, ".ctor", "Void", fields.Select(f => f.Type).ToArray()), [], writes);
        Rule(copy, [], reads.Concat(writes).ToArray());
        Rule(I(owner, "<Clone>$", owner), [], New(copy));
        Rule(equality, [], "ldtoken " + owner);
        Rule(I(owner, "Equals", "Boolean", "Object"), [], "isinst " + owner, Call(equals));
        Rule(S(owner, "op_Equality", "Boolean", owner, owner), [], Virtual(equals));
        Rule(S(owner, "op_Inequality", "Boolean", owner, owner), [], Call(S(owner, "op_Equality", "Boolean", owner, owner)));
        Rule(I(owner, "ToString", "String"), [], Call(I(owner, "PrintMembers", "Boolean", "System.Text.StringBuilder")));
        var equalityOps = new List<string> { Call(equality), Virtual(equality) };
        var hashOps = new List<string> { Call(equality) };
        var printOps = new List<string>(); var deconstructOps = new List<string>();
        foreach (var (name, type, value) in fields)
        {
            string field = Field(owner, "<" + name + ">k__BackingField", type), get = Getter(owner, name, type);
            Add(field, "field", type);
            Add(owner + "::" + name + Signature(40, 0, type), "property", Signature(40, 0, type));
            Rule(get, [], Read(field));
            Rule(I(owner, "set_" + name, "VoidmodreqSystem.Runtime.CompilerServices.IsExternalInit", type), [], Write(field));
            string comparer = "EqualityComparer<" + type + ">";
            string getComparer = Call(S(comparer, "get_Default", "EqualityComparer<!0>"));
            equalityOps.AddRange([Read(field), getComparer, Virtual(I(comparer, "Equals", "Boolean", "!0", "!0"))]);
            hashOps.AddRange([Read(field), getComparer, Virtual(I(comparer, "GetHashCode", "Int32", "!0"))]);
            printOps.Add(Call(get)); deconstructOps.Add(Call(get));
            if (value) { printOps.Add("constrained. " + type); deconstructOps.Add("stobj " + type); }
        }
        Rule(equals, [], equalityOps.ToArray());
        Rule(I(owner, "GetHashCode", "Int32"), [], hashOps.ToArray());
        Rule(I(owner, "PrintMembers", "Boolean", "System.Text.StringBuilder"), fields.Where(f => f.Value).Select(f => f.Type).ToArray(), printOps.ToArray());
        Rule(I(owner, "Deconstruct", "Void", fields.Select(f => f.Type + "&").ToArray()), [], deconstructOps.ToArray());
    }

    // 与原 PublicAbi 相同字段口径；无未批准的新增/删除/改签，不将集合包含冒作兼容证明。
    internal static string[] PublicAdditions()
    {
        const string owner = "RyuPy.Runtime.PyTraceProduct";
        var result = new List<string> { "T:" + owner + ":1048961:System.Object:" };
        void Add(string name, string returns, params string[] args)
            => result.Add("M:" + owner + ":" + name + ":150:" + Signature(0, 0, returns, args) + ":");
        Add("Enter", "Void", "Int64"); Add("Exit", "Void"); Add("Explicit", "Object", "Object");
        Add("Park", "Object", "Object", "Int64"); Add("Handle", "Void", "Object", "Int64");
        Add("Forward", "Void", "Object", "Int64"); Add("Abort", "Void", "Object");
        Add("Match", "Void", "Object"); Add("AllMiss", "Void", "Object");
        Add("GeneratorDepth", "Int64"); Add("GeneratorCatch", "Void", "Object");
        return result.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }
    internal static bool PreservesAbi(string[] before, string[] after)
        => before.Length == 1659 && after.Length == 1671
            && before.Concat(PublicAdditions()).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(after.OrderBy(x => x, StringComparer.Ordinal));
}
