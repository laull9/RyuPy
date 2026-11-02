// 显式固定 C ABI 库的冷登记与绝对路径加载；不在登记时打开库，不允许默认名称搜索回退。
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>编译器生成入口专用；每程序集仅一个显式 native 库，JIT 与 AOT 均登记。</summary>
public static class PyNativeBindingLibrary
{
    private static readonly object Gate = new();
    private static readonly ConditionalWeakTable<Assembly, Library> Libraries = new();

    /// <summary>在用户代码前安装 resolver；完全相同的本方登记幂等，冲突不覆盖。</summary>
    public static void Register(Assembly owner, string logicalName, string relativeFile)
    {
        // 只接 CLR 实际程序集，拒用户 Assembly 包装器、动态与可卸载宿主。
        if (owner is null || owner.GetType() != typeof(PyNativeBindingLibrary).Assembly.GetType()
            || owner.IsDynamic || owner.IsCollectible)
            throw new PyRuntimeError("native binding requires a non-collectible runtime assembly");
        ValidateName(logicalName);
        string path = ResolveForBaseDirectory(AppContext.BaseDirectory, relativeFile);
        lock (Gate)
        {
            if (Libraries.TryGetValue(owner, out Library? prior))
            {
                if (prior.Name != logicalName || prior.Path != path)
                    throw new PyRuntimeError("conflicting native binding registration");
                return;
            }
            var library = new Library(owner, logicalName, path);
            // 不读取 DynamicCodeSupported；普通 JIT 也能通过配置关闭它。
            // 此调用仅安装回调。AOT DirectPInvoke 静态入口不执行回调，也不需要共享文件。
            NativeLibrary.SetDllImportResolver(owner, library.Resolve);
            Libraries.Add(owner, library);
        }
    }

    /// <summary>纯路径合同：应用根必须绝对，部署名仅允许受限 ASCII 相对路径。</summary>
    public static string ResolveForBaseDirectory(string root, string relativeFile)
    {
        if (string.IsNullOrEmpty(root) || !System.IO.Path.IsPathFullyQualified(root))
            throw new PyRuntimeError("native binding application base is not absolute");
        if (relativeFile is null || relativeFile.Length > 1024
            || !relativeFile.StartsWith("_internal/native/v1/", StringComparison.Ordinal)
            || relativeFile.Contains('\\') || relativeFile.Contains(':'))
            throw new PyRuntimeError("invalid native binding relative path");
        foreach (string part in relativeFile.Split('/'))
        {
            if (part.Length == 0 || part.Length > 255 || part is "." or ".."
                || part.EndsWith('.') || part.EndsWith(' '))
                throw new PyRuntimeError("invalid native binding path component");
            string stem = part.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" or "CLOCK$"
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal)
                    || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '1' && stem[3] <= '9'))
                throw new PyRuntimeError("reserved native binding path component");
            foreach (char c in part)
                if (c > 127 || char.IsControl(c) || "<>\"|?*".Contains(c))
                    throw new PyRuntimeError("invalid native binding path character");
        }
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(root,
            relativeFile.Replace('/', System.IO.Path.DirectorySeparatorChar)));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 128)
            throw new PyRuntimeError("invalid native binding library name");
        foreach (char c in name)
            if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_'))
                throw new PyRuntimeError("invalid native binding library name");
    }

    private sealed class Library(Assembly owner, string name, string path)
    {
        internal readonly string Name = name;
        internal readonly string Path = path;
        private readonly object _gate = new();
        private IntPtr _handle;

        internal IntPtr Resolve(string requested, Assembly actualOwner, DllImportSearchPath? ignored)
        {
            if (!ReferenceEquals(owner, actualOwner) || !string.Equals(requested, Name, StringComparison.Ordinal))
                throw new DllNotFoundException("undeclared native binding library: " + requested);
            lock (_gate)
            {
                if (_handle != IntPtr.Zero) return _handle;
                // 只在首次实际调用检查可见 reparse/symlink；不宣称抵御并发文件系统替换。
                try
                {
                    for (string? current = Path; current is not null; current = System.IO.Path.GetDirectoryName(current))
                        if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                            throw new DllNotFoundException("native binding path traverses a link");
                }
                catch (IOException error)
                {
                    throw new DllNotFoundException("native binding path is unavailable", error);
                }
                catch (UnauthorizedAccessException error)
                {
                    throw new DllNotFoundException("native binding path is inaccessible", error);
                }
                // 绝对路径且失败抛出：绝不返回零句柄触发 cwd/PATH 探测。
                // 成功句柄进程期保留，不在 Python GC/模块删除时 Free 已缓存函数地址。
                IntPtr loaded = NativeLibrary.Load(Path);
                if (loaded == IntPtr.Zero) throw new DllNotFoundException("native binding library returned a null handle");
                _handle = loaded;
                return loaded;
            }
        }
    }
}
