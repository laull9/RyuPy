// 模块位置只在注册时解析一次；诊断文件名、公开可写 __file__ 槽与不可变初始位置分别保存。
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>编译器显式位置模式的冷路径，不执行导入、不搜索资源或回退到源码目录。</summary>
public static class PyModuleLocations
{
    /// <summary>0=旧诊断位置，1=源码绝对位置，2=真实 NativeAOT 可执行文件相对位置。</summary>
    public static string Resolve(long kind, string value) =>
        ResolveForPlatform(kind, value, RuntimeFeature.IsDynamicCodeSupported, Environment.ProcessPath);

    /// <summary>纯平台分支供跨平台专测；生产 Resolve 使用真实宿主状态，不接受 Python 层覆盖。</summary>
    public static string ResolveForPlatform(long kind, string value, bool dynamicCodeSupported, string? processPath)
    {
        if (kind == 0) return value;
        if (kind == 1)
        {
            if (!Path.IsPathFullyQualified(value)) throw new PyRuntimeError("source module location is not absolute");
            return value;
        }
        if (kind != 2) throw new PyRuntimeError("unknown compiled module location kind");
        // 普通 dotnet/JIT 进程不能拿 dotnet 宿主目录冒充 NativeAOT 应用目录。
        if (dynamicCodeSupported) throw new PyNotImplementedError("executable-relative module locations require NativeAOT");
        if (value.Length > 1024 || !value.StartsWith("_internal/resources/v1/", StringComparison.Ordinal)
            || value.Contains('\\') || value.Contains(':'))
            throw new PyRuntimeError("invalid executable-relative module location");
        foreach (string part in value.Split('/'))
        {
            if (part.Length == 0 || part.Length > 255 || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' '))
                throw new PyRuntimeError("invalid module location component");
            string stem = part.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" or "CLOCK$"
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal)
                    || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '1' && stem[3] <= '9'))
                throw new PyRuntimeError("reserved module location component");
            foreach (char c in part)
                if (c > 127 || char.IsControl(c) || "<>\"|?*".Contains(c))
                    throw new PyRuntimeError("invalid module location character");
        }
        if (processPath is null || !Path.IsPathFullyQualified(processPath))
            throw new PyNotImplementedError("actual NativeAOT executable path is unavailable");
        string directory = Path.GetDirectoryName(processPath)
            ?? throw new PyRuntimeError("NativeAOT executable has no directory");
        return Path.GetFullPath(Path.Combine(directory, value.Replace('/', Path.DirectorySeparatorChar)));
    }
}

public static partial class PyModules
{
    /// <summary>登记具有显式位置及真实 __file__ 全局槽的编译模块；诊断来源仍单独保留。</summary>
    public static void RegisterLocated(string name, string package, string diagnosticFile, long locationKind,
        string locationValue, bool fileBackedByGlobal, PyModuleInit init, PyModuleGet get, PyModuleSet set)
    {
        EnsureRegistrationAvoidsCacheAliases(name);
        string resolved = PyModuleLocations.Resolve(locationKind, locationKind == 0 ? diagnosticFile : locationValue);
        Registry[name] = new Registration(package, diagnosticFile, init, get, set, resolved, fileBackedByGlobal);
        PyProvider.Current?.Registered(name);
    }

    /// <summary>模块入口专用：返回已缓存对象的初始位置，不读取可变属性，也不触发导入。</summary>
    public static string ModuleFile(string name) => Cache.TryGetValue(name, out PyModule? module)
        ? module.File : throw new PyRuntimeError("module location requested before module initialization");
}
