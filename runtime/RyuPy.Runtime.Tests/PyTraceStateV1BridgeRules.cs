// 手写 V1 接线合同，按 source、receive、物理帧、具体 producer 分组。
// 共用只读 getter 的名字不等于整类授权，每个 Rule 仍列出自己的准确目标。
namespace RyuPy.Runtime.Tests;

internal sealed partial class PyTraceStateV1ArtifactContract
{
    private void ProductRules()
    {
        string active = P("get_Active", "TraceProductScope"), scope = P("get_Scope", "TraceProductScope");
        string enabled = P("get_Enabled", "Boolean"), setActive = P("set_Active", "Void", "TraceProductScope");
        string activeField = Field("PyTraceProduct", "<Active>k__BackingField", "TraceProductScope");
        string binding = Field("PyBaseException", "TraceProductBinding", "TraceProductBinding");
        string authority = Field("TraceProductScope", "Authority", "PyTraceContextAuthority");
        string owner = Getter("TraceProductBinding", "Owner", "PyTraceState");
        string boundScope = Getter("TraceProductBinding", "Scope", "TraceProductScope");
        string snapshot = Getter("PyTraceState", "Snapshot", "TraceSnapshot");
        string epoch = Getter("TraceSnapshot", "Epoch", "TraceEpoch");
        string request = Getter("TraceSnapshot", "Request", "Nullable<TraceRequest>");
        string apply = P("Apply", "Void", "TraceProductBinding", "TraceSnapshot", "TraceResult");
        string bind = P("Bind", "TraceProductBinding", "PyBaseException"), existing = P("Existing", "TraceProductBinding", "Object");
        string next = P("NextRequest", "TraceRequest", "PyTraceState");
        string scene = S("PyTraceback", "ProductScene", "TraceFrame[]");
        string boundary = S("PyTraceback", "ProductBoundary", "TraceBoundary", "TraceEpoch", "Int64");
        string receive = P("Receive", "Void", "Object", "Int64", "TraceReceiveKind");
        string generators = Field("TraceProductScope", "Generators", "Stack<Int64>");
        string flights = Field("TraceProductScope", "Flights", "Int64"), parks = Field("TraceProductScope", "Parks", "Int64");
        string newBinding = I("TraceProductBinding", ".ctor", "Void", "TraceProductScope", "PyTraceState");
        Add(activeField, "field", "TraceProductScope");
        Add("PyTraceProduct::Enabled" + Signature(8, 0, "Boolean"), "property", Signature(8, 0, "Boolean"));
        foreach (string name in new[] { "Active", "Scope" })
            Add("PyTraceProduct::" + name + Signature(8, 0, "TraceProductScope"), "property", Signature(8, 0, "TraceProductScope"));
        Rule(active, [], "ldsfld " + activeField);
        Rule(enabled, [], Call(active)); Rule(scope, [], Call(active));
        Rule(setActive, [], "stsfld " + activeField);
        Rule(P("Enter", "Void", "Int64"), [], Call(active), New(I("TraceProductScope", ".ctor", "Void")), Call(setActive));
        Rule(P("Exit", "Void"), ["TraceProductScope"], Call(scope), Read(flights), Read(parks), Read(generators), Call(setActive));

        // 只有两个源操作发布 binding；Bind 单独有 new owner 权限，构造异常没有。
        Rule(bind, ["TraceProductBinding"], Read(binding), Call(existing), Call(scope), Read(authority),
            New(I("PyTraceState", ".ctor", "Void", "PyTraceContextAuthority")), New(newBinding));
        Rule(existing, ["TraceProductBinding"], Read(binding), Virtual(boundScope), Call(scope));
        Rule(P("Explicit", "Object", "Object"), ["TraceProductBinding", "TraceSnapshot"],
            Call(bind), Virtual(owner), Virtual(snapshot), Call(next), Call(scene),
            Virtual(I("PyTraceState", "BeginExplicit", "TraceResult", "TraceRequest", "TraceFrame[]")), Call(apply), Write(binding));
        Rule(Fresh, ["TraceProductBinding", "TraceSnapshot"], Call(enabled), Read(binding), Call(bind), Virtual(owner),
            Virtual(snapshot), Call(next), Virtual(Getter("PyTraceState", "BirthGrant", "TraceBirthGrant")), Call(scene),
            Virtual(I("PyTraceState", "BeginNativeFresh", "TraceResult", "TraceRequest", "TraceBirthGrant", "TraceFrame[]")), Call(apply), Write(binding));
        Rule(next, ["Nullable<TraceRequest>", "TraceRequest"], Virtual(snapshot), Virtual(epoch), Virtual(request),
            Call(I("Nullable<TraceRequest>", "get_HasValue", "Boolean")), Call(I("Nullable<TraceRequest>", "GetValueOrDefault", "!0")),
            Call(Getter("TraceRequest", "Number", "Int64")), New(I("TraceRequest", ".ctor", "Void", "PyTraceState", "TraceEpoch", "Int64")));

        Rule(P("Park", "Object", "Object", "Int64"), ["TraceProductBinding", "TraceSnapshot", "TraceBoundary", "TraceResult", "TraceDispatchPark"],
            Call(existing), Virtual(owner), Virtual(snapshot), Virtual(epoch), Call(boundary),
            Virtual(I("PyTraceState", "DispatchPark", "TraceResult", "TraceBoundary", "TraceDispatchPark&")), Call(apply));
        string consume = P("Consume", "Void", "Object", "Boolean");
        foreach (string name in new[] { "Match", "Abort", "AllMiss" }) Rule(P(name, "Void", "Object"), [], Call(consume));
        Rule(consume, ["TraceDispatchPark", "PyTraceState", "TraceSnapshot", "TraceRequest"], "isinst TraceDispatchPark",
            Virtual(Getter("TraceDispatchPark", "Lineage", "TraceRequest")), Call(Getter("TraceRequest", "Epoch", "TraceEpoch")),
            Virtual(Getter("TraceEpoch", "Authority", "PyTraceContextAuthority")), Call(scope), Read(authority),
            Call(Getter("TraceRequest", "Owner", "PyTraceState")), Virtual(snapshot), New(newBinding),
            Virtual(I("PyTraceState", "ResumePark", "TraceResult", "TraceDispatchPark")),
            Virtual(I("PyTraceState", "ConsumePark", "TraceResult", "TraceDispatchPark")), Call(apply));

        foreach (string name in new[] { "Handle", "Forward" }) Rule(P(name, "Void", "Object", "Int64"), [], Call(receive));
        Rule(P("Terminal", "Void", "PyBaseException"), [], Call(enabled), Call(receive));
        Rule(P("Suppress", "Void", "System.Exception"), [], Call(enabled), Call(receive));
        Rule(receive, ["TraceProductBinding", "PyTraceState", "TraceSnapshot", "TraceRequest", "TraceBoundary",
            "Nullable<TraceRequest>", "TraceFrame", "TraceDispatchPark", "TraceReceipt", "Nullable<TraceBeginKind>", "TraceReceive", "TraceResult"],
            Call(existing), Virtual(owner), Virtual(snapshot), Virtual(request),
            Call(I("Nullable<TraceRequest>", "get_HasValue", "Boolean")), Call(I("Nullable<TraceRequest>", "GetValueOrDefault", "!0")),
            Virtual(Getter("TraceSnapshot", "PendingCount", "Nullable<Int32>")), Virtual(epoch), Call(boundary),
            Call(S("TraceBoundary", "Root", "TraceBoundary", "TraceEpoch")), Call(Getter("TraceBoundary", "Position", "Int32")),
            Virtual(Getter("TraceSnapshot", "Scene", "ReadOnlyCollection<TraceFrame>")),
            Virtual(Getter("ReadOnlyCollection<TraceFrame>", "Count", "Int32")), Virtual(I("ReadOnlyCollection<TraceFrame>", "get_Item", "!0", "Int32")),
            Call(Getter("TraceFrame", "Activation", "Int64")), Call(Getter("TraceBoundary", "Activation", "Int64")),
            Virtual(Getter("TraceSnapshot", "BeginKind", "Nullable<TraceBeginKind>")), Call(I("Nullable<TraceBeginKind>", "GetValueOrDefault", "!0")),
            Virtual(Getter("TraceSnapshot", "Continuation", "TraceDispatchPark")), Virtual(Getter("TraceDispatchPark", "Boundary", "TraceBoundary")),
            Call(S("TraceBoundary", "op_Equality", "Boolean", "TraceBoundary", "TraceBoundary")),
            Virtual(Getter("TraceDispatchPark", "Remaining", "Int32")), Virtual(Getter("TraceSnapshot", "Receipt", "TraceReceipt")),
            Virtual(Getter("TraceReceipt", "Event", "TraceReceive")), Call(Getter("TraceReceive", "Request", "TraceRequest")),
            Call(S("TraceRequest", "op_Equality", "Boolean", "TraceRequest", "TraceRequest")),
            Call(Getter("TraceReceive", "Boundary", "TraceBoundary")), Call(Getter("TraceReceive", "Kind", "TraceReceiveKind")),
            Call(Getter("TraceReceive", "Inclusion", "TraceInclusion")), Virtual(I("PyTraceState", "Receive", "TraceResult", "TraceReceive")),
            Call(Getter("TraceResult", "Change", "TraceChange")), Virtual(Getter("TraceSnapshot", "NextReceive", "Int64")),
            New(I("TraceReceive", ".ctor", "Void", "TraceRequest", "Int64", "TraceBoundary", "TraceInclusion", "TraceReceiveKind")), Call(apply));
        Rule(apply, ["TraceSnapshot"], Call(Getter("TraceResult", "Change", "TraceChange")), Call(Getter("TraceResult", "Reject", "TraceReject")),
            "call System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted[48;1;1](!!0)->Void<TraceReject>",
            Virtual(owner), Virtual(snapshot), Virtual(boundScope), Virtual(Getter("TraceSnapshot", "PendingCount", "Nullable<Int32>")),
            Virtual(Getter("TraceSnapshot", "Park", "TraceDispatchPark")), Read(flights), Write(flights), Read(parks), Write(parks));

        foreach (string name in new[] { "EnterGenerator", "LeaveGenerator" }) Rule(P(name, "Void", "Int64"), [], Call(scope), Read(generators));
        Rule(P("GeneratorDepth", "Int64"), [], Call(scope), Read(generators));
        Rule(P("GeneratorCatch", "Void", "Object"), [], Call(scope), Read(generators), Call(P("Forward", "Void", "Object", "Int64")));
        Rule(P("EscapeGenerator", "Void", "Object", "Int64"), [], Call(P("Forward", "Void", "Object", "Int64")));
    }

    // 独立手写六个cold producer；不按类名通配授权，也不扩大S1产品owner准入。
    internal static readonly (string Name, string Type)[] TreeBuilderProducers =
    [
        ("Type", "PyTypeError"), ("Unsupported", "PyNotImplementedError"),
        ("Index", "PyIndexError"), ("Parse", "PyXmlParseError"),
        ("Attribute", "PyAttributeError"), ("Name", "PyNameError")
    ];

    private void ProjectionAndProducerRules()
    {
        string enabled = Call(P("get_Enabled", "Boolean")), suppress = Call(P("Suppress", "Void", "System.Exception"));
        string binding = Field("PyBaseException", "TraceProductBinding", "TraceProductBinding");
        Add(binding, "field", "TraceProductBinding");
        Rule(I("PyBaseException", "get_Frames", "PyStackFrame[]"), ["TraceProductBinding", "ReadOnlyCollection<TraceFrame>", "TraceFrame"],
            Read(binding), Virtual(Getter("TraceProductBinding", "Owner", "PyTraceState")), Virtual(Getter("PyTraceState", "Snapshot", "TraceSnapshot")),
            Virtual(Getter("TraceSnapshot", "History", "ReadOnlyCollection<TraceFrame>")),
            Virtual(Getter("ReadOnlyCollection<TraceFrame>", "Count", "Int32")), Virtual(I("ReadOnlyCollection<TraceFrame>", "get_Item", "!0", "Int32")),
            Call(Getter("TraceFrame", "File", "String")), Call(Getter("TraceFrame", "Function", "String")), Call(Getter("TraceFrame", "Line", "Int32")));
        Rule(I("PyBaseException", "set_Frames", "Void", "PyStackFrame[]"), [], Read(binding), enabled);
        string activation = Field("TraceProductScope", "Activation", "Int64");
        Rule(S("PyTraceback", "ProductPush", "Void", "Int32"), ["TraceProductScope"],
            Call(P("get_Active", "TraceProductScope")), Read(activation), Write(activation));
        Rule(S("PyTraceback", "ProductScene", "TraceFrame[]"), ["TraceFrame[]"], enabled, "newarr TraceFrame",
            New(I("TraceFrame", ".ctor", "Void", "Int64", "String", "String", "Int32")), "stelem TraceFrame");
        Rule(S("PyTraceback", "ProductBoundary", "TraceBoundary", "TraceEpoch", "Int64"), ["TraceBoundary"],
            New(I("TraceBoundary", ".ctor", "Void", "TraceEpoch", "Int32", "Int64")));
        Rule(S("PyTraceback", "Attach", "Void", "Object"), [], enabled);
        Rule(S("PyTraceback", "CheckDepth", "Void"), [], FreshCall("PyRecursionError"));
        // 仅迁移实际 formatter 的完整签名，保留原 Suppress 单目标，不授权整类。
        Rule(S("PyTraceback", "AppendSingle", "Void", "System.Text.StringBuilder", "PyBaseException",
            "Nullable<Int64>", "System.Collections.Generic.List`1<String>"), [], suppress);
        // 模块观察入口只能读取启用状态并拒绝 V1，不允许接收/终结或抑制传播。
        Rule(S("PyTracebackModule", "Parts", "String[]", "Object", "Object"), [], enabled);
        Rule(S("PyTraceback", "Report", "Void", "PyBaseException"), [], Call(P("Terminal", "Void", "PyBaseException")), suppress);
        // CLR映射由终端报告与provider回调共享；权限随完整签名迁移，不保留旧ReportClr许可。
        Rule(S("PyErrors", "FromClr", "PyBaseException", "System.Exception"), [], enabled, FreshCall("PyBaseException"));
        // 外部异常接收仅检查V1并明确拒绝，不能修改V1传播状态。
        Rule(S("PyErrors", "ReceiveForeign", "PyBaseException", "PyBaseException"), [], enabled);
        Rule(S("PyErrors", "Load", "Object", "Object", "String"), [], FreshCall("PyUnboundLocalError"));
        Rule(S("PyOps", "ThrowUnboundGlobal", "Void", "String"), [], FreshCall("PyNotImplementedError"), FreshCall("PyNameError"));
        Rule(S("PyOps", "LoadDefaultBuiltin", "Object", "String"), [], FreshCall("PyNotImplementedError"), FreshCall("PyNameError"));
        foreach (var (name, type) in TreeBuilderProducers)
            Rule(S("PyXmlTreeBuilderErrors", name, type, "String"), [], enabled, FreshCall(type));
        Rule(S("PyIO", "Guard", "Object", "System.Func`1<Object>"), [], enabled, FreshCall("PyBaseException"));
        Rule(I("PyGenerator", "Next", "Object", "Object", "Boolean"), [], FreshCall("PyStopIteration"));
        Rule(I("PyGenerator", "MoveNextCore", "Boolean", "Object", "Object&", "PyBaseException"), [], enabled,
            Call(P("EnterGenerator", "Void", "Int64")), Call(P("EscapeGenerator", "Void", "Object", "Int64")), Call(P("LeaveGenerator", "Void", "Int64")));
        Rule(S("PySystem", "FlushOutputs", "Void"), [], enabled, suppress);
        Rule(S("Builtins", "Flush", "Void"), [], enabled);
        Rule(S("Builtins", "FlushForExit", "Int32", "Int32"), [], enabled, FreshCall("PyBaseException"), Call(P("Terminal", "Void", "PyBaseException")));
        Rule(S("Builtins", "FinishExit", "Int32", "Int32"), [], enabled, Call(P("Exit", "Void")));
    }
}
