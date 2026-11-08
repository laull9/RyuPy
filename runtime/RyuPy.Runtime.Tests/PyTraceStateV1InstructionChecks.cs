// 少数必须有的写入与相对顺序：不以允许边集合冒充 activation、域生命周期或发布原子性。
// 这些检查只覆盖明确成员；真实行为仍交给 lifecycle/产品差分，不实现通用 IL 数据流框架。
using System.Reflection;
using System.Reflection.Metadata;
using C = RyuPy.Runtime.Tests.PyTraceStateV1ArtifactContract;

namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    private static void CheckV1InstructionBoundaries(List<TraceBody> raw, List<string> failures)
    {
        var bodies = raw.Select(b => b with { Owner = C.Short(b.Owner), Source = C.Short(b.Source),
            Instructions = b.Instructions.Select(i => i with { Target = i.Target is null ? null : C.Short(i.Target) }).ToArray() }).ToArray();
        string M(string owner, string name, string result, bool instance = false, params string[] args)
            => C.Method(owner, name, result, instance, args);
        string P(string name, string result, params string[] args) => M("PyTraceProduct", name, result, false, args);
        string enter = P("Enter", "Void", "Int64"), exit = P("Exit", "Void");
        string setActive = P("set_Active", "Void", "TraceProductScope");
        string explicitSource = P("Explicit", "Object", "Object"), fresh = "PyTraceProduct::NativeFresh[16;1;1](!!0)->!!0";
        string apply = P("Apply", "Void", "TraceProductBinding", "TraceSnapshot", "TraceResult");
        string bind = P("Bind", "TraceProductBinding", "PyBaseException");
        string productPush = M("PyTraceback", "ProductPush", "Void", false, "Int32");
        string pushCore = M("PyTraceback", "PushFrameCore", "Int64", false, "String", "String", "Object");
        string bindingField = "PyBaseException::TraceProductBinding:TraceProductBinding";
        string activeField = "PyTraceProduct::<Active>k__BackingField:TraceProductScope";
        string getter = M("PyBaseException", "get_Frames", "PyStackFrame[]", true);
        string setter = M("PyBaseException", "set_Frames", "Void", true, "PyStackFrame[]");
        string scopeCtor = M("TraceProductScope", ".ctor", "Void", true);

        void Need(bool condition, string explanation) { if (!condition) failures.Add(explanation); }
        TraceBody? Body(string source)
        {
            var matches = bodies.Where(b => b.Source == source).ToArray();
            Need(matches.Length == 1, "必要方法必须唯一存在：" + source);
            return matches.Length == 1 ? matches[0] : null;
        }
        // 拒绝写地址和委托等绕路；按完整目标身份匹配，不依赖当前 token。
        void OnlyUses(string target, string opcode, string[] sources, bool required = true)
        {
            var uses = bodies.SelectMany(b => b.Instructions.Where(i => i.Target == target).Select(i => (b.Source, i.Op))).ToArray();
            Need(uses.All(u => u.Op == opcode && sources.Contains(u.Source)), "未批准的操作/来源：" + target);
            if (required) foreach (string source in sources)
                Need(uses.Count(u => u.Source == source && u.Op == opcode) == 1, "必要边须恰一次：" + source + " → " + target);
        }
        void OnlyWrites(string target, string[] sources)
        {
            var writes = bodies.SelectMany(b => b.Instructions.Where(i => i.Target == target
                && i.Op is "stfld" or "stsfld" or "ldflda" or "ldsflda").Select(i => (b.Source, i.Op))).ToArray();
            Need(writes.All(w => w.Op is "stfld" or "stsfld" && sources.Contains(w.Source)), "未批准的状态写入：" + target);
            foreach (string source in sources) Need(writes.Count(w => w.Source == source) == 1, "必要状态写入须恰一次：" + source + " → " + target);
        }
        OnlyUses(enter, "call", [], false); // 只有生成产物的真实 Main 开域，runtime 不得暗开。
        OnlyUses(scopeCtor, "newobj", [enter]); OnlyUses(setActive, "call", [enter, exit]);
        OnlyWrites(activeField, [setActive]); OnlyWrites(bindingField, [explicitSource, fresh]);
        OnlyWrites("TraceProductScope::Activation:Int64", [productPush]);
        OnlyWrites("TraceProductScope::Flights:Int64", [apply]); OnlyWrites("TraceProductScope::Parks:Int64", [apply]);
        OnlyUses(productPush, "call", [pushCore]);
        OnlyUses(M("PyTraceState", ".ctor", "Void", true, "PyTraceContextAuthority"), "newobj", [bind]);
        OnlyUses(M("PyTraceContextAuthority", ".ctor", "Void", true), "newobj", [scopeCtor]);
        OnlyWrites("TraceProductScope::Authority:PyTraceContextAuthority", [scopeCtor]);
        OnlyWrites("TraceProductScope::Generators:Stack<Int64>", [scopeCtor]);
        OnlyUses(M("Stack<Int64>", "Push", "Void", true, "!0"), "callvirt", [P("EnterGenerator", "Void", "Int64")]);
        OnlyUses(M("Stack<Int64>", "Pop", "!0", true), "callvirt", [P("LeaveGenerator", "Void", "Int64")]);
        string generatorField = "TraceProductScope::Generators:Stack<Int64>";
        foreach (var body in bodies.Where(b => b.Instructions.Any(i => i.Op == "ldfld" && i.Target == generatorField)))
        {
            var il = body.Instructions.Where(i => i.Op != "nop").ToArray();
            foreach (var instruction in il.Where(i => i.Target?.StartsWith("Stack<Int64>::", StringComparison.Ordinal) == true))
                Need(C.AllowsGeneratorStackUse(body.Source, instruction.Op, instruction.Target!), "未批准的 Generators 容器操作：" + body.Source);
            for (int n = 0; n < il.Length; n++)
            {
                if (il[n].Op != "ldfld" || il[n].Target != generatorField) continue;
                int use = n + 1;
                if (body.Source == P("EnterGenerator", "Void", "Int64"))
                {
                    Need(use < il.Length && il[use].Op == "ldarg.0", "EnterGenerator 只能 Push 真实 depth 参数。"); use++;
                }
                Need(use < il.Length && il[use].Target is { } target && C.AllowsGeneratorStackUse(body.Source, il[use].Op, target),
                    "Generators 不得经局部/返回/helper 逃逸：" + body.Source);
            }
        }
        OnlyWrites("PyBaseException::_legacyFrames:PyStackFrame[]", [setter,
            M("PyBaseException", ".ctor", "Void", true, "String", "String", "Object[]", "PyBaseException")]);

        if (Body(setActive) is { } activeSetter)
            Need((activeSetter.Attributes & (int)MethodAttributes.MemberAccessMask) == (int)MethodAttributes.Private,
                "Active setter 必须 private。");
        foreach (string source in new[] { explicitSource, fresh })
            if (Body(source) is { } b)
            {
                int applied = Array.FindIndex(b.Instructions, i => i.Op == "call" && i.Target == apply);
                int published = Array.FindIndex(b.Instructions, i => i.Op == "stfld" && i.Target == bindingField);
                Need(applied >= 0 && published > applied, "binding 必须在成功 Apply 后发布：" + source);
            }
        if (Body(exit) is { } exiting)
        {
            var il = exiting.Instructions.Where(i => i.Op != "nop").ToArray();
            int index = Array.FindIndex(il, i => i.Target == setActive);
            Need(index > 0 && il[index - 1].Op == "ldnull", "Exit 只能把 Active 设为 null。");
        }
        if (Body(enter) is { } entering)
        {
            var il = entering.Instructions.Where(i => i.Op != "nop").ToArray();
            int index = Array.FindIndex(il, i => i.Target == setActive);
            Need(index > 0 && il[index - 1].Op == "newobj" && il[index - 1].Target == scopeCtor,
                "Enter 只发布自己新建的 scope。");
        }
        // getter 的全部字段/调用也受限；否则可经无 Core 名字的新 helper 或缓存偷偷创建第二权威。
        if (Body(getter) is { } projection)
        {
            var contract = new C();
            foreach (var i in projection.Instructions)
            {
                if (i.Op is "stfld" or "stsfld" or "ldflda" or "ldsflda") failures.Add("Frames getter 不得持久写入或取字段地址。");
                if (i.Op is "ldfld" or "ldsfld")
                    Need(i.Target == bindingField || i.Target == "PyBaseException::_legacyFrames:PyStackFrame[]", "Frames getter 读取了第二份状态。");
                if (i.Op is "call" or "callvirt" or "newobj" or "ldftn" or "ldvirtftn" or "calli")
                    Need(i.Op == "newobj" && i.Target == M("PyStackFrame", ".ctor", "Void", true, "String", "String", "Int32")
                        || contract.Allows(new(projection.Owner, getter, "IL", i.Op + " " + i.Target)), "Frames getter 调用了未批准操作。");
            }
        }
        // 同一目标不等于同一语义：固定接收 kind、root 深度及 Park consume 的 bool 都读真实 IL。
        static long? Constant(TraceInstruction instruction) => instruction.Op switch
        {
            "ldc.i4.m1" => -1, "ldc.i4.0" => 0, "ldc.i4.1" => 1, "ldc.i4.2" => 2,
            "ldc.i4.3" => 3, "ldc.i4.4" => 4, "ldc.i4.5" => 5, "ldc.i4.6" => 6,
            "ldc.i4.7" => 7, "ldc.i4.8" => 8, "ldc.i4" or "ldc.i4.s" or "ldc.i8" => instruction.Integer,
            _ => null
        };
        void FixedArgument(string source, string target, long expected, bool root = false)
        {
            if (Body(source) is not { } method) return;
            var il = method.Instructions.Where(i => i.Op != "nop").ToArray();
            var positions = il.Select((i, n) => (i, n)).Where(x => x.i.Op == "call" && x.i.Target == target).Select(x => x.n).ToArray();
            Need(positions.Length == 1, "固定语义桥必须唯一调用：" + source);
            if (positions.Length != 1) return;
            int index = positions[0];
            Need(index > 0 && Constant(il[index - 1]) == expected, "固定 kind/bool 参数改变：" + source);
            if (root) Need(index >= 2 && (il[index - 2].Op == "ldc.i8" && Constant(il[index - 2]) == 0
                || index >= 3 && il[index - 2].Op == "conv.i8" && Constant(il[index - 3]) == 0), "root 接收深度必须为 Int64 0：" + source);
        }
        string receiveTarget = P("Receive", "Void", "Object", "Int64", "TraceReceiveKind");
        FixedArgument(P("Handle", "Void", "Object", "Int64"), receiveTarget, (int)RyuPy.Runtime.TraceReceiveKind.Handle);
        FixedArgument(P("Forward", "Void", "Object", "Int64"), receiveTarget, (int)RyuPy.Runtime.TraceReceiveKind.Forward);
        FixedArgument(P("Terminal", "Void", "PyBaseException"), receiveTarget, (int)RyuPy.Runtime.TraceReceiveKind.Terminal, true);
        FixedArgument(P("Suppress", "Void", "System.Exception"), receiveTarget, (int)RyuPy.Runtime.TraceReceiveKind.Suppress, true);
        string consumeTarget = P("Consume", "Void", "Object", "Boolean");
        foreach (string name in new[] { "Match", "Abort", "AllMiss" })
            FixedArgument(P(name, "Void", "Object"), consumeTarget, name == "AllMiss" ? 1 : 0);
        string move = M("PyGenerator", "MoveNextCore", "Boolean", true, "Object", "Object&", "PyBaseException");
        if (Body(move) is { } generator)
        {
            var il = generator.Instructions;
            int enterIndex = Array.FindIndex(il, i => i.Target == P("EnterGenerator", "Void", "Int64"));
            int leaveIndex = Array.FindIndex(il, i => i.Target == P("LeaveGenerator", "Void", "Int64"));
            int escapeIndex = Array.FindIndex(il, i => i.Target == P("EscapeGenerator", "Void", "Object", "Int64"));
            int[] pushes = il.Select((i, n) => (i, n)).Where(x => x.i.Target == M("PyTraceback", "PushFrame", "Int64", false, "String", "String")
                || x.i.Target == M("PyTraceback", "PushFrameWithOrigin", "Int64", false, "String", "String", "Object")).Select(x => x.n).ToArray();
            Need(enterIndex >= 0 && pushes.Length == 2 && pushes.All(n => n < enterIndex), "真实 Push 必须先于 EnterGenerator。");
            if (enterIndex >= 0)
                Need(!il.Any(i => i.Integer is long target && (i.Op.StartsWith("br", StringComparison.Ordinal) || i.Op.StartsWith("b", StringComparison.Ordinal))
                    && target <= il[enterIndex].Offset && il[enterIndex].Offset <= i.Offset), "Retry 回边不得包含 EnterGenerator。");
            bool InFinally(int index) => index >= 0 && generator.Regions.Any(r => r.Kind == ExceptionRegionKind.Finally
                && il[index].Offset >= r.HandlerOffset && il[index].Offset < r.HandlerOffset + r.HandlerLength);
            Need(InFinally(leaveIndex), "LeaveGenerator 必须在 finally。");
            int finalPop = Array.FindIndex(il, Math.Max(0, leaveIndex), i => i.Target == M("PyTraceback", "PopTo", "Void", false, "Int64"));
            Need(escapeIndex >= 0 && finalPop > leaveIndex && InFinally(finalPop), "接收/Leave 必须先于最终 PopTo。");
        }
        if (Body(M("Builtins", "FinishExit", "Int32", false, "Int32")) is { } finish)
        {
            string[] order = [M("PyTraceback", "Reset", "Void"), M("PyErrors", "LeaveHandler", "Void", false, "Object"),
                M("PyAtexit", "RunExitFunctions", "Void"), M("Builtins", "FlushForExit", "Int32", false, "Int32"), exit];
            int previous = -1;
            foreach (string target in order)
            {
                int index = Array.FindIndex(finish.Instructions, i => i.Target == target);
                Need(index > previous, "FinishExit 必须在物理清理和刷新之后关闭 scope：" + target); previous = index;
            }
        }
    }
}
