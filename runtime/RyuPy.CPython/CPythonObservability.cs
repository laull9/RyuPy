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
        nint keys = 0, tuple = 0;
        try
        {
            keys = Check(Native.rp_op(19, paths)); tuple = Check(Native.rp_op(26, keys));
            long count = Native.rp_length(tuple); ThrowIfError();
            for (long i = 0; i < count; i++)
            {
                nint key = Check(Native.rp_op(18, tuple, n: i));
                try { loadedModulePaths[ReadText(key)] = ReadTextOwned(Native.rp_op(6, paths, key)); }
                finally { Native.rp_decref(key); }
            }
        }
        finally { Native.rp_decref(tuple); Native.rp_decref(keys); Native.rp_decref(paths); }
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
        WriteStockProgramReport(writer);
        WriteExecutionAudit(writer);
        WriteInspectReport(writer);
        writer.WriteStartObject("module_paths");
        foreach ((string name, string filename) in loadedModulePaths) writer.WriteString(name, filename);
        writer.WriteEndObject(); writer.WriteEndObject();
    }
    // 关闭后的观测只读纯C计数及托管ledger；没有PythonCall、GIL或CP对象解引用。
    // 只为编译入口权限输出；closed阶段只读冻结托管快照，不重新进入Python。
    private void WriteStockProgramReport(Utf8JsonWriter writer)
    {
        if (!stockProgramAuthorized && !stockProgramExecuting) return;
        writer.WriteStartObject("stock_compiled_program");
        writer.WriteBoolean("permission", stockProgramAuthorized);
        writer.WriteBoolean("terminal", stockProgramResult is not null);
        writer.WriteNumber("entry_peak_records", stockProgramEntryRegistry?.PeakCount ?? 0);
        writer.WriteNumber("entry_final_records", stockProgramEntryRegistry?.Count ?? 0);
        writer.WriteNumber("identity_cache_entries", objects.Count);
        if (stockProgramResult is { } result)
        {
            writer.WriteNumber("finalize_result", result.FinalizeResult);
            writer.WriteNumber("native_live", result.NativeLive);
            writer.WriteNumber("destroyed", result.Destroyed);
            writer.WriteNumber("native_calls", result.NativeCalls);
            writer.WriteNumber("core_roots", result.CoreRoots);
            writer.WriteNumber("owned_h", result.OwnedHandles);
            writer.WriteNumber("ledger", result.Ledger);
            writer.WriteNumber("queue", result.Pending);
            writer.WriteNumber("late_releases", result.Late);
            writer.WriteNumber("post_finalize_decrefs", result.PostFinalizeDecrefs);
        }
        writer.WriteEndObject();
    }
}
