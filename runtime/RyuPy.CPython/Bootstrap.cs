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
    /// <summary>统一退出路径；标准流/运行时退出失败也会执行解释器关闭。</summary>
    public static int Finish(int code)
    {
        int result = code;
        try { result = Builtins.FinishExit(code); }
        catch (Exception error) { Console.Error.WriteLine("RyuPy退出失败：" + error.Message); if (result == 0) result = 1; }
        finally
        {
            try { Stop(); }
            catch (Exception error) { Console.Error.WriteLine("CPython关闭失败：" + error.Message); if (result == 0) result = 1; }
        }
        return result;
    }
    /// <summary>在初始化线程关闭，CPython退出钩子仅由Finalize执行。</summary>
    public static void Stop()
    {
        if (host is not { } current) return;
        current.Dispose(); PyProvider.Remove(current); host = null;
    }
}

internal sealed record HostConfiguration(string Home, string Library, string Shim, string[] SearchPaths, string[] Modules, bool SynchronizeContainers)
{
    internal static HostConfiguration Read(string filename)
    {
        filename = Path.GetFullPath(filename, AppContext.BaseDirectory);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(filename));
        JsonElement root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("python_version").GetString() != "3.12.13")
            throw new InvalidDataException("CPython配置要求schema=1与python_version=3.12.13");
        string directory = Path.GetDirectoryName(filename)!;
        bool development = root.TryGetProperty("development", out JsonElement developer) && developer.GetBoolean();
        string ResolveValue(string value)
        {
            string resolved = Path.GetFullPath(value, directory);
            if (!development)
            {
                if (Path.IsPathRooted(value) || !resolved.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidDataException("CPython发行路径必须位于配置目录：" + value);
                // 逐段解析符号链接，避免相对路径经过链接逃离发行闭包。
                string current = directory;
                foreach (string component in Path.GetRelativePath(directory, resolved).Split(Path.DirectorySeparatorChar))
                {
                    current = Path.Combine(current, component);
                    FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
                    if (info.LinkTarget is not null) current = info.ResolveLinkTarget(true)!.FullName;
                    if (!current.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                        throw new InvalidDataException("CPython发行链接越过配置目录：" + value);
                }
            }
            return resolved;
        }
        string Resolve(string key) => ResolveValue(root.GetProperty(key).GetString()!);
        string[] paths = root.GetProperty("search_paths").EnumerateArray().Select(p => ResolveValue(p.GetString()!)).ToArray();
        if (paths.Length == 0 || paths.Any(p => p.Contains('\n'))) throw new InvalidDataException("CPython搜索路径为空或包含换行");
        return new(Resolve("home"), Resolve("library"), Resolve("shim"), paths,
            root.GetProperty("modules").EnumerateArray().Select(p => p.GetString()!).ToArray(),
            root.TryGetProperty("container_transfer", out JsonElement transfer) && transfer.GetString() == "synchronized");
    }
}
