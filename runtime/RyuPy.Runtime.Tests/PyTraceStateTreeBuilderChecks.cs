// 六个TreeBuilder冷producer的实际IL连续性门；不实现通用owner/effect或全runtime数据流。
using System.Reflection;
using System.Reflection.Emit;
using C = RyuPy.Runtime.Tests.PyTraceStateV1ArtifactContract;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    private const string TreeBuilderProducerFailure = "TreeBuilder producer结构：";
    private static void CheckTreeBuilderProducers(TraceBody[] bodies, List<string> failures)
    {
        foreach (var (name, type) in C.TreeBuilderProducers)
        {
            string source = C.Method("PyXmlTreeBuilderErrors", name, type, false, "String");
            void Need(bool condition, string reason)
            {
                if (!condition) failures.Add(TreeBuilderProducerFailure + source + " | " + reason);
            }
            var matches = bodies.Where(b => b.Source == source).ToArray();
            Need(matches.Length == 1, "完整静态签名必须唯一存在");
            if (matches.Length != 1) continue;
            var body = matches[0]; var raw = body.Instructions; var il = raw.Where(i => i.Op != "nop").ToArray();
            Need(body.Owner == "PyXmlTreeBuilderErrors" && (body.Attributes & (int)MethodAttributes.Static) != 0, "真实静态producer身份");
            Need(body.Regions.Length == 0 && !raw.Any(i => i.Op == "switch"), "不得有EH或switch入口");
            string fresh = "PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0<" + type + ">";
            string constructor = C.Method(type, ".ctor", "Void", true, "String");
            Need(il.Count(i => i.Target?.StartsWith("PyTraceProduct::NativeFresh", StringComparison.Ordinal) == true) == 1, "Fresh必须恰一次");
            Need(il.Count(i => i.Op == "throw") == 2 && !il.Any(i => i.Op is "ret" or "rethrow"), "V1与legacy各自throw，不可返回或重抛");
            Need(il.Count(i => i.Op == "newobj" && i.Target == constructor) == 2, "两分支各自构造准确异常");
            Need(il.Count(i => i.Op == "call" && i.Target == C.Method("PyTraceProduct", "get_Enabled", "Boolean")) == 1, "仅一次显式V1/legacy选择");
            Need(il.Count(i => i.Op == "call" && i.Target == C.Method("PyTraceback", "Attach", "Void", false, "Object")) == 1, "legacy独立附帧");
            int at = Array.FindIndex(il, i => i.Op == "call" && i.Target == fresh);
            bool sequence = at > 0 && at + 1 < il.Length && il[at - 1].Op == "newobj"
                && il[at - 1].Target == constructor && il[at + 1].Op == "throw";
            Need(sequence, "必须对应newobj→NativeFresh→throw且只可夹nop");
            if (!sequence) continue;
            int start = il[at - 1].Offset, end = il[at + 1].Offset;
            var branches = Opcodes.Values.Where(op => op.OperandType is OperandType.ShortInlineBrTarget or OperandType.InlineBrTarget)
                .Select(op => op.Name!).ToHashSet(StringComparer.Ordinal);
            Need(!raw.Any(i => branches.Contains(i.Op) && i.Integer is long target && target > start && target <= end), "不得跳入new之后的Fresh/throw");
        }
    }
}
