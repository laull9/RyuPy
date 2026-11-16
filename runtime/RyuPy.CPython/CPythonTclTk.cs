// 随包 Tcl/Tk 资源目录定位与进程环境隔离；启动前记录原环境，并在初始化失败或关闭时恢复。
namespace RyuPy.CPython;

/// <summary>管理嵌入式宿主生命周期内的 `TCL_LIBRARY` / `TK_LIBRARY` 隔离与恢复。</summary>
internal sealed class TclTkEnvironment
{
    private readonly string? previousTcl;
    private readonly string? previousTk;
    private bool restored;

    private TclTkEnvironment(string? previousTcl, string? previousTk)
    {
        this.previousTcl = previousTcl;
        this.previousTk = previousTk;
    }

    /// <summary>
    /// 若分发 `home` 内存在 `init.tcl` / `tk.tcl`，将 `TCL_LIBRARY` / `TK_LIBRARY` 绑定到随包目录；
    /// 否则清除外部环境变量，防止宿主机残留或错版本 Tcl/Tk 污染隔离解释器。
    /// </summary>
    internal static TclTkEnvironment Configure(string home)
    {
        string? previousTcl = Environment.GetEnvironmentVariable("TCL_LIBRARY");
        string? previousTk = Environment.GetEnvironmentVariable("TK_LIBRARY");
        var guard = new TclTkEnvironment(previousTcl, previousTk);
        try
        {
            string? tclLibrary = FindBundledDirectory(home, "tcl", "init.tcl");
            string? tkLibrary = FindBundledDirectory(home, "tk", "tk.tcl");
            SetProcessVariable("TCL_LIBRARY", tclLibrary);
            SetProcessVariable("TK_LIBRARY", tkLibrary);
            return guard;
        }
        catch
        {
            guard.TryRestore();
            throw;
        }
    }

    /// <summary>尽力恢复宿主启动前的原始 `TCL_LIBRARY` / `TK_LIBRARY`，不掩盖外层主异常。</summary>
    internal void TryRestore()
    {
        if (restored) return;
        restored = true;
        try { SetProcessVariable("TCL_LIBRARY", previousTcl); } catch { }
        try { SetProcessVariable("TK_LIBRARY", previousTk); } catch { }
    }

    private static string? FindBundledDirectory(string home, string prefix, string markerFile)
    {
        if (string.IsNullOrEmpty(home) || !Directory.Exists(home)) return null;
        string resolvedHome = Path.GetFullPath(home);
        StringComparison cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (string sub in new[] { "lib", "tcl", "Lib" })
        {
            string baseDir = Path.Combine(resolvedHome, sub);
            if (!Directory.Exists(baseDir) || !IsWithinRoot(baseDir, resolvedHome, cmp, isDirectory: true)) continue;
            string[] candidates = Directory.GetDirectories(baseDir);
            Array.Sort(candidates, StringComparer.Ordinal);
            Array.Reverse(candidates);
            foreach (string candidate in candidates)
            {
                string name = Path.GetFileName(candidate);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsWithinRoot(candidate, resolvedHome, cmp, isDirectory: true)) continue;
                string marker = Path.Combine(candidate, markerFile);
                if (File.Exists(marker) && IsWithinRoot(marker, resolvedHome, cmp, isDirectory: false))
                    return candidate;
            }
        }
        return null;
    }

    private static bool IsWithinRoot(string path, string root, StringComparison cmp, bool isDirectory)
    {
        string full = Path.GetFullPath(path);
        FileSystemInfo info = isDirectory ? new DirectoryInfo(full) : new FileInfo(full);
        if (info.LinkTarget is not null)
        {
            if (info.ResolveLinkTarget(returnFinalTarget: true) is not { } target) return false;
            full = target.FullName;
        }
        return full.StartsWith(root + Path.DirectorySeparatorChar, cmp) || full.Equals(root, cmp);
    }

    private static void SetProcessVariable(string name, string? value)
    {
        Environment.SetEnvironmentVariable(name, value);
        int status;
        try
        {
            status = Native.rp_set_env(name, value);
        }
        catch (EntryPointNotFoundException error)
        {
            throw new InvalidOperationException(
                $"CPython shim 缺少 rp_set_env 导出，无法隔离 {name}；请使用同步构建的新版 shim",
                error);
        }
        if (status != 0)
            throw new InvalidOperationException($"无法设置或清除 {name}（目标={value ?? "<unset>"}，错误码={status}）");
    }
}
