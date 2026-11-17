// 应用入口的显式组合与冻结配置读取；相对路径只以分发目录为根，不搜索系统Python。
using System.Runtime.InteropServices;
using System.Text.Json;
using RyuPy.Runtime;

namespace RyuPy.CPython;

/// <summary>编译入口使用的启动/关闭ABI。</summary>
public static class Bootstrap
{
    private static CPythonHost? host;
    /// <summary>当前宿主，供显式生命周期验收与应用关闭作用域使用。</summary>
    public static CPythonHost Host => host ?? throw new InvalidOperationException("CPython宿主尚未启动");
    /// <summary>使用进程命令行初始化。</summary>
    public static void Start(string configPath) => Start(configPath, Environment.GetCommandLineArgs());
    /// <summary>加载固定声明，使用同一应用参数启动一次。</summary>
    public static void Start(string configPath, string[] args)
    {
        if (host is not null) throw new InvalidOperationException("CPython宿主已启动");
        // Runtime已经初始化argv，直接取同一来源，保留入口名、空参数与换行参数。
        if (PyModules.TryCached("sys", out PyModule? system) && system.GetAttr("argv") is PyList currentArgs)
            args = currentArgs.Items.Select(value => (string)value!).ToArray();
        host = CPythonHost.Start(configPath, args);
        PyProvider.Install(host);
    }
    /// <summary>仅显式编译资格调用：安装既有宿主后启用 stock 薄函数能力。</summary>
    public static void StartStockThin(string configPath, string[] args)
    {
        Start(configPath, args);
        try { Host.EnableStockThinFunctions(); }
        catch (Exception startup)
        {
            try { Stop(); }
            catch (Exception cleanup) { throw new AggregateException("stock 薄函数启动和回滚均失败", startup, cleanup); }
            throw;
        }
    }
    /// <summary>授权并执行封闭stock编译入口；finally交还入口内部临时H。</summary>
    public static void RunStockThinMain() => Host.RunStockProgramMain();

    /// <summary>一次性编译程序终态；外部owned拒绝时保留宿主供释放重试。</summary>
    public static int FinishStockThin(int code)
    {
        // 启动回滚或入口前初始化失败仍走原退出合同，不能凭finish调用伪造程序权限。
        if (host is not { HasStockProgramPermission: true } current) return Finish(code);
        try
        {
            int result = current.FinishStockProgram(() => Builtins.FinishExit(code));
            Builtins.Flush(); return result;
        }
        catch (Exception error)
        { Console.Error.WriteLine("CPython关闭失败：" + error.Message); return code == 0 ? 1 : code; }
        finally
        {
            if (current.IsClosed) { PyProvider.Remove(current); host = null; }
        }
    }

    /// <summary>统一退出路径；标准流/运行时退出失败也会执行解释器关闭。</summary>
    public static int Finish(int code)
    {
        int result = code;
        try { result = Builtins.FinishExit(code); }
        catch (Exception error) { Console.Error.WriteLine("RyuPy退出失败：" + error.Message); if (result == 0) result = 1; }
        finally
        {
            try { Stop(); Builtins.Flush(); }
            catch (Exception error) { Console.Error.WriteLine("CPython关闭失败：" + error.Message); if (result == 0) result = 1; }
        }
        return result;
    }
    /// <summary>在初始化线程关闭，CPython退出钩子仅由Finalize执行。</summary>
    public static void Stop()
    {
        if (host is not { } current) return;
        try { current.Dispose(); }
        finally
        {
            // 拒绝跨线程/租约内关闭时仍保留活宿主；Finalize完成后的报告失败也撤销provider。
            if (current.IsClosed) { PyProvider.Remove(current); host = null; }
        }
    }
}

internal sealed record HostConfiguration(string Home, string Library, string Shim, string[] SearchPaths, string[] Modules, bool SynchronizeContainers, string[] NativeSearchPaths, string ExpectedVersion, string? EntryScript = null, bool NativeInspectProfile = false)
{
    internal static HostConfiguration Read(string filename)
    {
        filename = Path.GetFullPath(filename, AppContext.BaseDirectory);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(filename));
        JsonElement root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1)
            throw new InvalidDataException("CPython配置要求schema=1");
        string expectedVersion = root.GetProperty("python_version").GetString()!;
        string[] versionParts = (expectedVersion ?? "").Split('.');
        if (versionParts.Length != 3 || versionParts.Any(p => p.Length == 0 || p.Any(c => c < '0' || c > '9'))
            || !Version.TryParse(expectedVersion, out Version? parsedVersion)
            || parsedVersion.Major != 3 || parsedVersion.Minor < 9 || parsedVersion.Minor > 15)
            throw new InvalidDataException("CPython配置要求正式版3.9–3.15，固定声明版本为3.12.13，实际为：" + expectedVersion);
        string directory = Path.GetDirectoryName(filename)!;
        bool development = root.TryGetProperty("development", out JsonElement developer) && developer.GetBoolean();
        string ResolveValue(string value)
        {
            string resolved = Path.GetFullPath(value, directory);
            if (!development)
            {
                StringComparison cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (Path.IsPathRooted(value) || (!resolved.StartsWith(directory + Path.DirectorySeparatorChar, cmp) && !resolved.Equals(directory, cmp)))
                    throw new InvalidDataException("CPython发行路径必须位于配置目录：" + value);
                // 逐段解析符号链接，避免相对路径经过链接逃离发行闭包。
                string current = directory;
                foreach (string component in Path.GetRelativePath(directory, resolved).Split(Path.DirectorySeparatorChar))
                {
                    current = Path.Combine(current, component);
                    FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
                    if (info.LinkTarget is not null) current = info.ResolveLinkTarget(true)!.FullName;
                    if (!current.StartsWith(directory + Path.DirectorySeparatorChar, cmp) && !current.Equals(directory, cmp))
                        throw new InvalidDataException("CPython发行链接越过配置目录：" + value);
                }
            }
            return resolved;
        }
        string Resolve(string key) => ResolveValue(root.GetProperty(key).GetString()!);
        string[] paths = root.GetProperty("search_paths").EnumerateArray().Select(p => ResolveValue(p.GetString()!)).ToArray();
        if (paths.Length == 0 || paths.Any(p => p.Contains('\n'))) throw new InvalidDataException("CPython搜索路径为空或包含换行");
        string mode = root.TryGetProperty("container_transfer", out JsonElement transfer) ? transfer.GetString()! : "proxy";
        if (mode is not ("proxy" or "synchronized")) throw new InvalidDataException("container_transfer必须为proxy或synchronized");
        string? entryScript = null;
        if (root.TryGetProperty("entry_script", out JsonElement entryElement) && entryElement.ValueKind != JsonValueKind.Null)
        {
            if (entryElement.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("entry_script必须为字符串");
            string rawEntry = entryElement.GetString()!;
            if (string.IsNullOrEmpty(rawEntry))
                throw new InvalidDataException("entry_script不能为空字符串");
            entryScript = ResolveValue(rawEntry);
            if (!File.Exists(entryScript))
                throw new FileNotFoundException("CPython委派入口脚本缺失：" + entryScript, entryScript);
        }
        return new(Resolve("home"), Resolve("library"), Resolve("shim"), paths,
            root.GetProperty("modules").EnumerateArray().Select(p => p.GetString()!).ToArray(),
            mode == "synchronized",
            root.TryGetProperty("native_search_paths", out JsonElement nativePaths)
                ? nativePaths.EnumerateArray().Select(p => ResolveValue(p.GetString()!)).ToArray() : [],
            expectedVersion,
            entryScript,
            root.TryGetProperty("native_inspect_profile", out JsonElement inspectProfile) && inspectProfile.GetBoolean());
    }
}
