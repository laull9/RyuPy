// 只用显式Utf8JsonWriter基本值API；不依赖匿名DTO反射序列化，失败也先保留已发生事件。
using System.Text;
using System.Text.Json;
using RyuPy.Runtime;

internal static class ObservationWriter
{
    private static void Write(string path, Action<Utf8JsonWriter> body)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream);
        body(writer);
        writer.Flush();
    }

    private static void Number(Utf8JsonWriter writer, string key, long? value)
    {
        if (value is long number) writer.WriteNumber(key, number);
        else writer.WriteNull(key);
    }

    private static void Frames(Utf8JsonWriter writer, string key, IEnumerable<TraceFrame> frames, bool activation)
    {
        writer.WriteStartArray(key);
        foreach (var frame in frames)
        {
            writer.WriteStartObject();
            if (activation) writer.WriteNumber("Activation", frame.Activation);
            writer.WriteString(activation ? "Function" : "function", frame.Function);
            writer.WriteNumber(activation ? "Line" : "line", frame.Line);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void Error(Utf8JsonWriter writer, PyBaseException? error, HashSet<PyBaseException> seen)
    {
        if (error is null) { writer.WriteNullValue(); return; }
        // 此证据路径必须在身份断言之前可用；坏binding/循环context也留下可解析事实。
        writer.WriteStartObject();
        writer.WriteString("type", error.PyTypeName);
        writer.WriteString("message", error.PyMessage);
        if (!seen.Add(error)) writer.WriteBoolean("contextCycle", true);
        else
        {
            if (error.TraceAdapterBinding is { } binding) Frames(writer, "frames", binding.Owner.Snapshot.History, false);
            else writer.WriteNull("frames");
            writer.WritePropertyName("context");
            Error(writer, error.Context, seen);
        }
        writer.WriteEndObject();
    }

    private static void State(Utf8JsonWriter writer, TraceSnapshot state)
    {
        writer.WriteString("BeginKind", state.BeginKind?.ToString());
        writer.WriteBoolean("GrantUsed", state.GrantUsed);
        writer.WriteBoolean("GrantValid", state.GrantValid);
        Number(writer, "PendingCount", state.PendingCount);
        writer.WriteNumber("NextReceive", state.NextReceive);
        writer.WriteNumber("Version", state.Version);
        Number(writer, "Q", state.Request?.Number);
        Number(writer, "R", state.Receipt?.Event.Number);
        if (state.Receipt is { } receipt)
        {
            writer.WriteStartObject("Boundary");
            writer.WriteNumber("Position", receipt.Event.Boundary.Position);
            writer.WriteNumber("Activation", receipt.Event.Boundary.Activation);
            writer.WriteString("Inclusion", receipt.Event.Inclusion.ToString());
            writer.WriteString("Kind", receipt.Event.Kind.ToString());
            writer.WriteEndObject();
        }
        else writer.WriteNull("Boundary");
        Frames(writer, "Scene", state.Scene, true);
        Frames(writer, "History", state.History, true);
    }

    internal static void Capture(string directory, TraceAdapterScope scope)
    {
        Write(Path.Combine(directory, "events.json"), writer => {
            writer.WriteStartArray();
            foreach (var item in scope.Events)
            {
                writer.WriteStartObject();
                writer.WriteString("Kind", item.Kind);
                writer.WriteString("Type", item.Error.PyTypeName);
                writer.WriteNumber("LiveDepth", item.LiveDepth);
                Number(writer, "LegacyFrameCount", item.LegacyFrameCount);
                writer.WriteString("LegacyContextType", item.LegacyContext?.PyTypeName);
                State(writer, item.After);
                writer.WriteStartObject("Before");
                State(writer, item.Before);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        });
        Write(Path.Combine(directory, "actual.json"), writer =>
            Error(writer, scope.LastHandled, new HashSet<PyBaseException>(ReferenceEqualityComparer.Instance)));
    }

    internal static void Status(string directory, string stage, string? error, string? disposeError)
        => Write(Path.Combine(directory, "execution-status.json"), writer => {
            writer.WriteStartObject();
            writer.WriteString("Stage", stage);
            writer.WriteString("Error", error);
            writer.WriteString("DisposeError", disposeError);
            writer.WriteEndObject();
        });

    internal static void Boundaries(string directory, IEnumerable<string> passed)
        => Write(Path.Combine(directory, "boundaries.json"), writer => {
            writer.WriteStartObject();
            writer.WriteString("Scope", "supplemental manual boundaries; not Python execution");
            writer.WriteStartArray("Passed");
            foreach (var name in passed) writer.WriteStringValue(name);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

    internal static string ActualText(string directory)
        => File.ReadAllText(Path.Combine(directory, "actual.json"), Encoding.UTF8);
}
