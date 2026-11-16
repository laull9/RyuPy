// NativeAOT 单文件入口：独占安全解包、继承工作目录和三流、逐项转发参数并清理。
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

internal static class Program
{
    private static int Main(string[] args)
    {
        string? root = null;
        Process? child = null;
        int interrupted = 0;
        var signals = new List<IDisposable>();
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            Interlocked.Exchange(ref interrupted, 2);
            if (OperatingSystem.IsWindows()) Stop(child);
        };
        try
        {
            Console.CancelKeyPress += cancel;
            if (!OperatingSystem.IsWindows())
            {
                foreach (var (signal, number) in new[] { (PosixSignal.SIGINT, 2), (PosixSignal.SIGTERM, 15), (PosixSignal.SIGHUP, 1) })
                    signals.Add(PosixSignalRegistration.Create(signal, context =>
                    {
                        context.Cancel = true;
                        Interlocked.Exchange(ref interrupted, number);
                        var current = child;
                        if (current != null && !current.HasExited) Kill(current.Id, number);
                    }));
            }
            root = PrivateDirectory();
            using Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("RyuPy.OneFile.Payload")
                ?? throw new InvalidDataException("缺少内嵌 onefile 载荷");
            string executable = Payload.Extract(payload, root, () => Volatile.Read(ref interrupted) != 0);
            if (Volatile.Read(ref interrupted) != 0) return 128 + interrupted;
            var start = new ProcessStartInfo(executable) { UseShellExecute = false };
            foreach (string arg in args) start.ArgumentList.Add(arg);
            child = Process.Start(start) ?? throw new IOException("无法启动内层原生程序");
            // 覆盖启动与登记 child 之间到来的信号，避免遗留后台进程。
            int pending = Volatile.Read(ref interrupted);
            if (pending != 0 && !child.HasExited)
            {
                if (OperatingSystem.IsWindows()) Stop(child); else Kill(child.Id, pending);
            }
            child.WaitForExit();
            return child.ExitCode;
        }
        catch (OperationCanceledException) { return 128 + Volatile.Read(ref interrupted); }
        catch (Exception error)
        {
            Console.Error.WriteLine($"RyuPy onefile 启动失败：{error.Message}");
            return 125;
        }
        finally
        {
            Stop(child);
            child?.Dispose();
            foreach (var signal in signals) signal.Dispose();
            Console.CancelKeyPress -= cancel;
            if (root != null)
            {
                try { Remove(root); }
                catch (Exception error) { Console.Error.WriteLine($"RyuPy onefile 清理失败 {root}：{error.Message}"); }
            }
        }
    }

    private static string PrivateDirectory()
    {
        if (!OperatingSystem.IsWindows()) return Directory.CreateTempSubdirectory("ryupy-onefile-").FullName;
        // 带受保护 ACL 原子创建；不继承可能过宽的公共临时目录权限。
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        using var identity = WindowsIdentity.GetCurrent();
        security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        string path = Path.Combine(Path.GetTempPath(), "ryupy-onefile-" + Guid.NewGuid().ToString("N"));
        new DirectoryInfo(path).Create(security);
        return path;
    }

    private static void Stop(Process? process)
    {
        try { if (process != null && !process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(); } }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception error)
        { Console.Error.WriteLine($"RyuPy onefile 终止子进程失败：{error.Message}"); }
    }
    private static void Remove(string root)
    {
        // 子程序可生成只读文件；只处理本轮目录，递归删除从不跟随链接/reparse。
        foreach (string path in Directory.EnumerateFileSystemEntries(root))
        {
            var attributes = File.GetAttributes(path);
            bool directory = (attributes & FileAttributes.Directory) != 0;
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            { if (directory) Directory.Delete(path); else File.Delete(path); continue; }
            if (directory)
            {
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                Remove(path);
            }
            else { File.SetAttributes(path, FileAttributes.Normal); File.Delete(path); }
        }
        Directory.Delete(root);
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int Kill(int pid, int signal);
}
