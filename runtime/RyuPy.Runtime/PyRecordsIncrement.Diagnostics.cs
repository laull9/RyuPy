// 专用构建的私有计数：单独诊断臂使用；普通/性能构建无字段、事件处理器或计数调用。
#if RECORDS_GUARD_DIAGNOSTICS
using System;
using System.Globalization;
using System.IO;
using System.Threading;
namespace RyuPy.Runtime;

public static partial class PyRecordsIncrement
{
    private static readonly bool DiagnosticsReady = RegisterDiagnostics();
    private static long _diagnosticFast, _diagnosticFallback;
    private static bool RegisterDiagnostics()
    {
        string? path = Environment.GetEnvironmentVariable("RYUPY_RECORDS_GUARD_TRACE");
        string? token = Environment.GetEnvironmentVariable("RYUPY_RECORDS_GUARD_TOKEN");
        if (string.IsNullOrEmpty(path) || token is null || token.Length != 32) return false;
        foreach (char value in token)
            if (value is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')) return false;
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            // CreateNew拒绝跨次复用；编译源码输出保持不变。诊断写失败不能伪装为有效证据。
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            writer.Write("{\"schema\":1,\"scope\":\"records-compiled-guard-diagnostic\",\"token\":\"" + token
                + "\",\"fast\":" + Interlocked.Read(ref _diagnosticFast).ToString(CultureInfo.InvariantCulture)
                + ",\"fallback\":" + Interlocked.Read(ref _diagnosticFallback).ToString(CultureInfo.InvariantCulture) + "}");
        };
        return true;
    }
    private static void Observe(bool fast)
    {
        if (!DiagnosticsReady) return;
        if (fast) Interlocked.Increment(ref _diagnosticFast);
        else Interlocked.Increment(ref _diagnosticFallback);
    }
}
#endif
