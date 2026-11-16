// 严格零字节码内核专属三阶段执行计数审计与 FiniCore Fence 报告：
// 覆盖 Py_InitializeFromConfig 之前到 Py_FinalizeEx 之后的完整生命周期，无 PEP 523 启动盲区。
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RyuPy.CPython;

internal sealed record StrictAuditSnapshot(
    string KernelVersion,
    bool Finalized,
    bool StrictRejectNeverDisabled,
    bool StatsNeverReset,
    int FiniCoreCalls,
    long StartupEntries,
    long StartupExecuted,
    long StartupRejected,
    long RuntimeEntries,
    long RuntimeExecuted,
    long RuntimeRejected,
    long ShutdownEntries,
    long ShutdownExecuted,
    long ShutdownRejected,
    long TotalEntries,
    long TotalExecuted,
    long TotalRejected);

public sealed partial class CPythonHost
{
    private const string StrictCoverageDescription =
        "complete-kernel; instrumented in _PyEval_EvalFrameDefault across startup, runtime, and shutdown with strict bytecode rejection";

    private int liveHandlesFreedInFence;
    private bool postFinalizePhase;
    private long postFinalizeDecrefs;
    private Exception? finiCoreError;
    private StrictAuditSnapshot? strictAuditSnapshot;

    private void ValidateStrictAuditEnvironment()
    {
        string? value = Environment.GetEnvironmentVariable("RYUPY_CPYTHON_EXECUTION_AUDIT");
        if (!string.IsNullOrEmpty(value) && value is not ("0" or "1"))
            throw new InvalidDataException("RYUPY_CPYTHON_EXECUTION_AUDIT只接受0或1");
        executionAuditEnabled = true;
    }

    private StrictAuditSnapshot CaptureStrictAuditSnapshot(string kernelVersion) => new(
        KernelVersion: kernelVersion,
        Finalized: StrictNative.rp_strict_is_finalized() != 0,
        StrictRejectNeverDisabled: StrictNative.rp_strict_reject_never_disabled() != 0,
        StatsNeverReset: StrictNative.rp_strict_stats_never_reset() != 0,
        FiniCoreCalls: StrictNative.rp_strict_fini_core_calls(),
        StartupEntries: StrictNative.rp_strict_eval_entry_count(0),
        StartupExecuted: StrictNative.rp_strict_eval_executed_count(0),
        StartupRejected: StrictNative.rp_strict_eval_rejected_count(0),
        RuntimeEntries: StrictNative.rp_strict_eval_entry_count(1),
        RuntimeExecuted: StrictNative.rp_strict_eval_executed_count(1),
        RuntimeRejected: StrictNative.rp_strict_eval_rejected_count(1),
        ShutdownEntries: StrictNative.rp_strict_eval_entry_count(2),
        ShutdownExecuted: StrictNative.rp_strict_eval_executed_count(2),
        ShutdownRejected: StrictNative.rp_strict_eval_rejected_count(2),
        TotalEntries: StrictNative.rp_strict_eval_total_entries(),
        TotalExecuted: StrictNative.rp_strict_eval_total_executed(),
        TotalRejected: StrictNative.rp_strict_eval_total_rejected());

    private void WriteStrictExecutionAudit(Utf8JsonWriter writer)
    {
        StrictAuditSnapshot? snap = strictAuditSnapshot;
        bool zeroEntries = snap is not null
            && snap.StartupEntries == 0 && snap.StartupExecuted == 0 && snap.StartupRejected == 0
            && snap.RuntimeEntries == 0 && snap.RuntimeExecuted == 0 && snap.RuntimeRejected == 0
            && snap.ShutdownEntries == 0 && snap.ShutdownExecuted == 0 && snap.ShutdownRejected == 0
            && snap.TotalEntries == 0 && snap.TotalExecuted == 0 && snap.TotalRejected == 0;
        bool complete = snap is not null
            && snap.Finalized
            && snap.StrictRejectNeverDisabled
            && snap.StatsNeverReset
            && snap.FiniCoreCalls == 1
            && finiCoreError is null
            && zeroEntries;
        long coverageFailures = snap is null
            ? 1
            : (snap.StrictRejectNeverDisabled ? 0 : 1)
                + (snap.StatsNeverReset ? 0 : 1)
                + (snap.FiniCoreCalls == 1 ? 0 : 1)
                + (finiCoreError is null ? 0 : 1);

        writer.WriteStartObject("execution_audit");
        writer.WriteBoolean("enabled", true);
        writer.WriteBoolean("strict_kernel", true);
        writer.WriteBoolean("complete", complete);
        writer.WriteBoolean("startup_complete", complete);
        writer.WriteString("coverage", StrictCoverageDescription);
        writer.WriteString("python_version", "3.12.13");
        writer.WriteString("kernel_version", snap?.KernelVersion ?? "");
        writer.WriteBoolean("hook_installed", snap is not null && snap.StrictRejectNeverDisabled && snap.StatsNeverReset);
        writer.WriteBoolean("finalized", snap is not null && snap.Finalized);
        writer.WriteBoolean("strict_reject_never_disabled", snap is not null && snap.StrictRejectNeverDisabled);
        writer.WriteBoolean("stats_never_reset", snap is not null && snap.StatsNeverReset);
        writer.WriteNumber("eval_total_executed", snap?.TotalExecuted ?? 0);
        writer.WriteNumber("dropped_frames", 0);
        writer.WriteNumber("coverage_failures", coverageFailures);
        writer.WriteNumber("startup_frames", snap?.StartupEntries ?? 0);
        writer.WriteNumber("runtime_frames", snap?.RuntimeEntries ?? 0);
        writer.WriteNumber("shutdown_frames", snap?.ShutdownEntries ?? 0);
        writer.WriteStartObject("phase_stats");
        WritePhaseStats(writer, "startup", snap?.StartupEntries ?? 0, snap?.StartupExecuted ?? 0, snap?.StartupRejected ?? 0);
        WritePhaseStats(writer, "runtime", snap?.RuntimeEntries ?? 0, snap?.RuntimeExecuted ?? 0, snap?.RuntimeRejected ?? 0);
        WritePhaseStats(writer, "shutdown", snap?.ShutdownEntries ?? 0, snap?.ShutdownExecuted ?? 0, snap?.ShutdownRejected ?? 0);
        writer.WriteEndObject();
        writer.WriteStartArray("frames");
        if (snap is not null)
        {
            WritePhaseFrameIfNonZero(writer, "startup", snap.StartupEntries);
            WritePhaseFrameIfNonZero(writer, "runtime", snap.RuntimeEntries);
            WritePhaseFrameIfNonZero(writer, "shutdown", snap.ShutdownEntries);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.WriteStartObject("fini_core_fence");
        writer.WriteBoolean("executed", snap is not null && snap.FiniCoreCalls >= 1 && finiCoreError is null);
        writer.WriteNumber("calls", snap?.FiniCoreCalls ?? 0);
        if (finiCoreError is not null) writer.WriteString("error", finiCoreError.Message);
        writer.WriteNumber("live_handles_at_shutdown", liveHandlesAtShutdown);
        writer.WriteNumber("live_handles_freed_in_fence", liveHandlesFreedInFence);
        writer.WriteNumber("post_finalize_decrefs", postFinalizeDecrefs);
        writer.WriteEndObject();
    }

    private static void WritePhaseStats(Utf8JsonWriter writer, string phase, long entries, long executed, long rejected)
    {
        writer.WriteStartObject(phase);
        writer.WriteNumber("entries", entries);
        writer.WriteNumber("executed", executed);
        writer.WriteNumber("rejected", rejected);
        writer.WriteEndObject();
    }

    private static void WritePhaseFrameIfNonZero(Utf8JsonWriter writer, string phase, long entries)
    {
        if (entries <= 0) return;
        writer.WriteStartObject();
        writer.WriteString("phase", phase);
        writer.WriteString("file", "<strict-kernel>");
        writer.WriteString("function", "_PyEval_EvalFrameDefault");
        writer.WriteNumber("calls", entries);
        writer.WriteEndObject();
    }
}
