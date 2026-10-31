using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 基准统计出口：进程退出时把分配量、GC、JIT 与执行耗时写成一行 JSON。
/// </summary>
/// <remarks>
/// # 用途
///
/// <c>make bench</c>（<c>tests/run_benchmarks.py</c>）只能在进程外量墙钟时间，无法区分
/// 「.NET 宿主启动 + 程序集加载」「JIT 编译」与「Python 代码真正执行」。路线图要求性能数据
/// 记录分配量与内存，这里在进程内补上这几项：
///
/// <list type="bullet">
/// <item><c>main_ms</c>：从 <see cref="PyModules.RunMain"/> 开始到进程退出的耗时（含用户代码的 JIT）；</item>
/// <item><c>alloc_bytes</c>：整个进程累计分配的托管字节数（精确值）；</item>
/// <item><c>gc0</c> / <c>gc1</c> / <c>gc2</c>：各代回收次数，<c>gc_pause_ms</c>：累计暂停；</item>
/// <item><c>jit_ms</c> / <c>jit_methods</c>：JIT 编译总耗时与方法数（冷启动成本）。</item>
/// </list>
///
/// # 开关与成本
///
/// 只有设置环境变量 <c>RYUPY_STATS=&lt;输出文件&gt;</c> 时才注册退出回调；未设置时仅多一次
/// 环境变量读取，不影响热路径。统计写入失败不会改变程序退出码或输出——基准数据只是旁路信息。
/// </remarks>
public static class PyStats
{
    private const string EnvironmentKey = "RYUPY_STATS";

    /// <summary>输出路径；为 <c>null</c> 表示未开启。</summary>
    private static readonly string? OutputPath = InstallFromEnvironment();

    /// <summary>入口模块开始执行的时刻（<see cref="Stopwatch"/> 计数）。</summary>
    private static long _mainStart;

    /// <summary>读取环境变量并注册退出回调，返回输出路径。</summary>
    private static string? InstallFromEnvironment()
    {
        string? path = Environment.GetEnvironmentVariable(EnvironmentKey);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Write(path);
        return path;
    }

    /// <summary>记录入口模块开始执行；由 <see cref="PyModules.RunMain"/> 调用。</summary>
    public static void MarkMainStart()
    {
        if (OutputPath is not null)
        {
            _mainStart = Stopwatch.GetTimestamp();
        }
    }

    /// <summary>采集并写出统计；任何 IO 错误都吞掉，不影响被测程序。</summary>
    private static void Write(string path)
    {
        double mainMs = _mainStart == 0
            ? 0
            : Stopwatch.GetElapsedTime(_mainStart).TotalMilliseconds;
        var json = new StringBuilder();
        json.Append('{');
        Field(json, "main_ms", mainMs);
        Field(json, "alloc_bytes", GC.GetTotalAllocatedBytes(precise: true));
        Field(json, "gc0", GC.CollectionCount(0));
        Field(json, "gc1", GC.CollectionCount(1));
        Field(json, "gc2", GC.CollectionCount(2));
        Field(json, "gc_pause_ms", GC.GetTotalPauseDuration().TotalMilliseconds);
        Field(json, "jit_ms", JitInfo.GetCompilationTime().TotalMilliseconds);
        Field(json, "jit_methods", JitInfo.GetCompiledMethodCount());
        Field(json, "heap_bytes", GC.GetTotalMemory(forceFullCollection: false), last: true);
        json.Append('}');
        try
        {
            File.WriteAllText(path, json.ToString());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Field(StringBuilder json, string name, double value, bool last = false)
    {
        json.Append('"').Append(name).Append("\":");
        json.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        if (!last)
        {
            json.Append(',');
        }
    }
}
