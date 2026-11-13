// 按显式环境变量输出临时观测记录；不生成产品源码快照，不计算普通文件摘要。
using System.Text.Json;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private readonly Dictionary<string, string> loadedModulePaths = new(StringComparer.Ordinal);
    private int liveHandlesAtShutdown;
    private void CaptureModulePaths()
    {
        nint paths = Check(Native.rp_loaded_modules());
        try
        {
            (string[] names, object?[] values) = ReadKeywords(paths);
            for (int i = 0; i < names.Length; i++)
                loadedModulePaths[names[i]] = values[i] is string text ? text : PyCast.ToStr(values[i]);
        }
        finally { Native.rp_decref(paths); }
    }
    private void WriteReport()
    {
        string? path = Environment.GetEnvironmentVariable("RYUPY_CPYTHON_REPORT");
        if (string.IsNullOrEmpty(path)) return;
        HostStatistics stats = Statistics;
        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteString("state", stats.State);
        writer.WriteNumber("bridge_calls", stats.BridgeCalls);
        writer.WriteNumber("active_leases", stats.ActiveLeases);
        writer.WriteNumber("handles", stats.Handles);
        writer.WriteNumber("managed_roots", stats.ManagedRoots);
        writer.WriteNumber("pending_releases", stats.PendingReleases);
        writer.WriteNumber("late_releases", stats.LateReleases);
        writer.WriteNumber("live_handles_at_shutdown", liveHandlesAtShutdown);
        writer.WriteString("container_transfer", synchronizeContainers ? "synchronized" : "proxy");
        writer.WriteString("synchronization_failure", synchronizationFailure);
        writer.WriteString("python_library", pythonPath);
        writer.WriteString("shim_library", shimPath);
        writer.WriteStartObject("module_paths");
        foreach ((string name, string filename) in loadedModulePaths) writer.WriteString(name, filename);
        writer.WriteEndObject(); writer.WriteEndObject();
    }
}
