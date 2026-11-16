// C0 显式逐帧观测与关闭后的原生快照报告；固定 ABI，保留启动盲区以拒绝错误晋升。
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private bool executionAuditEnabled;

    // 观测模式由单独环境变量启用；不改变宿主声明与默认执行路径。
    private void ConfigureExecutionAudit()
    {
        string? value = Environment.GetEnvironmentVariable("RYUPY_CPYTHON_EXECUTION_AUDIT");
        if (string.IsNullOrEmpty(value) || value == "0") return;
        if (value != "1") throw new InvalidDataException("RYUPY_CPYTHON_EXECUTION_AUDIT只接受0或1");
        if (Native.rp_execution_audit_start() != 0)
            throw new InvalidOperationException(Marshal.PtrToStringUTF8(Native.rp_init_error()));
        executionAuditEnabled = true;
    }

    private void WriteExecutionAudit(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("execution_audit");
        writer.WriteBoolean("enabled", executionAuditEnabled);
        writer.WriteBoolean("complete", false);
        writer.WriteBoolean("startup_complete", false);
        writer.WriteString("coverage", executionAuditEnabled
            ? "partial-startup; interpreter-wide PEP523 frames after first native audit event with thread state; direct _PyEval_EvalFrameDefault and other interpreters require separate instrumentation"
            : "disabled");
        if (executionAuditEnabled)
        {
            writer.WriteString("python_version", "3.12.13");
            writer.WriteBoolean("hook_installed", Native.rp_execution_audit_status(0) != 0);
            writer.WriteBoolean("finalized", Native.rp_execution_audit_status(1) != 0);
            writer.WriteNumber("dropped_frames", Native.rp_execution_audit_status(2));
            writer.WriteNumber("coverage_failures", Native.rp_execution_audit_status(3));
            string[] phases = ["startup", "runtime", "shutdown"];
            for (int i = 0; i < phases.Length; i++)
                writer.WriteNumber(phases[i] + "_frames", Native.rp_execution_audit_count(i));
            writer.WriteStartArray("frames");
            for (int index = 0; Native.rp_execution_audit_row(index, out int phase, out nint file, out nint function, out long calls) != 0; index++)
            {
                writer.WriteStartObject();
                writer.WriteString("phase", phases[phase]);
                writer.WriteString("file", Marshal.PtrToStringUTF8(file));
                writer.WriteString("function", Marshal.PtrToStringUTF8(function));
                writer.WriteNumber("calls", calls);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}
