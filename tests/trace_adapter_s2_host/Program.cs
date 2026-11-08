// 直接静态调用真实生成模块；仅观察实际H与context，不解释Python、不从旧Frames删帧。
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using RyuPy.Runtime;

internal static class Program
{
    private static void Require(bool condition, string why)
    {
        if (!condition) throw new InvalidOperationException(why);
    }

    private static JsonObject Observe(PyBaseException error)
    {
        var binding = error.TraceAdapterBinding ?? throw new InvalidOperationException("缺少真实source binding");
        var state = binding.Owner.Snapshot;
        Require(state.PendingCount is null, "最终Handle没有关闭flight");
        var frames = new JsonArray();
        foreach (var frame in state.History)
            frames.Add(new JsonObject { ["function"] = frame.Function, ["line"] = frame.Line });
        return new JsonObject {
            ["type"] = error.PyTypeName, ["message"] = error.PyMessage, ["frames"] = frames,
            ["context"] = error.Context is null ? null : Observe(error.Context),
        };
    }

    private static void InspectPe(string path, bool probe, bool runtime)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        bool adapter = runtime
            ? metadata.TypeDefinitions.Any(h => metadata.GetString(metadata.GetTypeDefinition(h).Name) == "PyTraceAdapter")
            : metadata.TypeReferences.Any(h => metadata.GetString(metadata.GetTypeReference(h).Name) == "PyTraceAdapter");
        Require(adapter == probe, "实际PE的probe标志/引用与隔离合同不符: " + path);
        if (probe && !runtime)
        {
            var members = metadata.MemberReferences.Select(h => metadata.GetString(metadata.GetMemberReference(h).Name)).ToArray();
            Require(members.Contains("PrepareExplicit") && members.Contains("Handle"), "生成PE没有真实source/receive调用");
        }
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("应该拒绝的适配调用成功了");
    }

    // 定向边界反例不冒充真实Python纵切；这里才允许手工建立单个帧。
    private static void Boundaries()
    {
        var external = new PyValueError("external");
        Require(external.TraceAdapterBinding is null, "构造不应读取scope");
        Reject(() => PyTraceAdapter.PrepareExplicit(external));
        Reject(() => PyTraceAdapter.Handle(external, 1));
        Require(external.TraceAdapterBinding is null, "scope外失败发布了binding");
        TraceSnapshot completed;
        using (PyTraceAdapter.EnterScope())
        {
            Reject(() => PyTraceAdapter.PrepareExplicit(external));
            Require(external.TraceAdapterBinding is null, "首次失败不应发布binding");
            var depth = PyTraceback.PushFrame("host-boundary", "boundary");
            Reject(() => PyTraceAdapter.Handle(external, depth));
            Require(external.TraceAdapterBinding is null, "catch代劳绑定");
            var legacy = new PyValueError("legacy");
            PyErrors.PrepareRaise(legacy);
            var oldFrames = legacy.Frames;
            Reject(() => PyTraceAdapter.PrepareExplicit(legacy));
            Require(legacy.TraceAdapterBinding is null && ReferenceEquals(oldFrames, legacy.Frames), "旧历史被导入或改写");
            PyTraceAdapter.PrepareExplicit(external);
            Require(!external.TraceAdapterBinding!.Owner.Snapshot.GrantValid, "Explicit必须撤销birth");
            var unfinished = external.TraceAdapterBinding.Owner.Snapshot;
            Reject(() => PyTraceAdapter.PrepareExplicit(external));
            Require(ReferenceEquals(unfinished, external.TraceAdapterBinding.Owner.Snapshot), "重复未终结Source改变快照");
            PyTraceAdapter.Handle(external, depth);
            completed = external.TraceAdapterBinding.Owner.Snapshot;
            PyTraceback.PopTo(depth);
        }
        using (PyTraceAdapter.EnterScope())
        {
            Reject(() => PyTraceAdapter.PrepareExplicit(external));
            Reject(() => PyTraceAdapter.Handle(external, 1));
            Require(ReferenceEquals(completed, external.TraceAdapterBinding!.Owner.Snapshot), "跨scope改变历史");
        }
        var unfinishedScope = PyTraceAdapter.EnterScope();
        PyTraceback.PushFrame("host-boundary", "unfinished");
        PyTraceAdapter.PrepareExplicit(new PyValueError("unfinished"));
        Reject(unfinishedScope.Dispose);
        Require(PyTraceAdapter.Active is null, "失败Close必须释放本次ambient scope");
    }

    private static int Main(string[] args)
    {
        // argv: expected、evidence、probe runtime、generated、default runtime、default generated。
        Require(args.Length == 6, "需要六个冻结产物参数");
        InspectPe(args[2], true, true);
        InspectPe(args[3], true, false);
        InspectPe(args[4], false, true);
        InspectPe(args[5], false, false);
        Require(File.ReadAllBytes(typeof(PyBaseException).Assembly.Location).SequenceEqual(File.ReadAllBytes(args[2])), "实际加载runtime不是本轮probe字节");
        Require(File.ReadAllBytes(typeof(RyuPy.Generated.Module).Assembly.Location).SequenceEqual(File.ReadAllBytes(args[3])), "实际加载模块不是本轮生成字节");
        Directory.CreateDirectory(args[1]);
        JsonObject actual;
        using (var scope = (TraceAdapterScope)PyTraceAdapter.EnterScope())
        {
            int code = RyuPy.Generated.Module.Main([]);
            Require(code == 0, "真实生成模块执行失败");
            Require(PyTraceback.CaptureDepth() == 0, "正常返回未恢复真实帧栈");
            var exported = PyModules.Import("__main__").GetAttr("result");
            Require(ReferenceEquals(exported, scope.LastHandled), "host捕获不是同源程序导出的真实result");
            actual = Observe(scope.LastHandled ?? throw new InvalidOperationException("没有真实Python接收"));
            Require(scope.Events.Count > 0, "没有真实事务事件");
            foreach (var item in scope.Events)
            {
                if (item.Kind == "Explicit")
                    Require(item.After.BeginKind == TraceBeginKind.Explicit && !item.After.GrantValid, "Source身份错误");
                else
                {
                    Require(item.Before.Request == item.After.Request, "接收错误地重新Begin");
                    Require(item.Before.NextReceive + 1 == item.After.NextReceive, "接收R不连续");
                    Require(item.LiveDepth >= item.After.Receipt!.Event.Boundary.Position + 1, "Receive晚于PopTo");
                }
            }
            if (Path.GetFileName(args[0]).StartsWith("explicit_inner", StringComparison.Ordinal))
            {
                Require(scope.Events.Count(e => e.Kind == "Explicit") == 2, "replacement需要两个独立源");
                Require(scope.Events.Count(e => e.Kind == "Forward") == 1, "replacement必须经过一次真实cleanup Forward");
                Require(scope.Events.Count(e => e.Kind == "Handle") == 2, "original和replacement各一次Handle");
                Require(scope.LastHandled!.Context is not null, "replacement丢失context身份");
            }
            if (Path.GetFileName(args[0]).StartsWith("recursive", StringComparison.Ordinal))
            {
                var frames = scope.LastHandled!.TraceAdapterBinding!.Owner.Snapshot.History;
                Require(frames.Select(f => f.Activation).Distinct().Count() == frames.Count, "递归activation被合并");
            }
            var events = scope.Events.Select(e => new {
                e.Kind, Type = e.Error.PyTypeName, e.LiveDepth,
                Q = e.After.Request?.Number, R = e.After.Receipt?.Event.Number,
                Scene = e.After.Scene.Select(f => new { f.Activation, f.Function, f.Line }),
                History = e.After.History.Select(f => new { f.Activation, f.Function, f.Line }),
            });
            File.WriteAllText(Path.Combine(args[1], "events.json"), JsonSerializer.Serialize(events));
        }
        File.WriteAllText(Path.Combine(args[1], "actual.json"), actual.ToJsonString());
        Require(JsonNode.DeepEquals(actual, JsonNode.Parse(File.ReadAllText(args[0]))), "实际H/context与冻结CP不符");
        Boundaries();
        var identity = args.Skip(2).Select(path => new { Path = path, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
        File.WriteAllText(Path.Combine(args[1], "identity.json"), JsonSerializer.Serialize(identity));
        Console.WriteLine(actual.ToJsonString());
        return 0;
    }
}
