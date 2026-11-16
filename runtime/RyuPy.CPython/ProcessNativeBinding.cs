// 进程级原生绑定状态机（Unbound -> Reserved -> Active -> Closed | Failed）：
// 在任何 dlopen / NativeLibrary.Load 前锁定唯一 (Profile, LibraryPath, ShimPath) 三元组，
// 单次注册 NativeLibrary.SetDllImportResolver 并保留不可逆 Failed 终态语义。
using System.Reflection;
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

/// <summary>进程内 CPython 原生绑定状态。</summary>
public enum BindingState
{
    Unbound,
    Reserved,
    Active,
    Closed,
    Failed,
}

/// <summary>进程内已锁定的 CPython 执行通道配置。</summary>
public enum BindingProfile
{
    Compat,
    StrictKernel,
}

/// <summary>进程原生绑定只读快照，供诊断与测试校验。</summary>
public sealed record ProcessBindingSnapshot(
    BindingState State,
    BindingProfile? Profile,
    string LibraryPath,
    string ShimPath,
    string? FailureReason);

/// <summary>管理单 OS 进程唯一的 libpython 与 shim 原生绑定及单次 DllImportResolver 注册。</summary>
public static class ProcessNativeBinding
{
    private static readonly object Gate = new();
    private static BindingState _state = BindingState.Unbound;
    private static BindingProfile? _profile;
    private static string _libraryPath = "";
    private static string _shimPath = "";
    private static string? _failureReason;
    private static nint _pythonHandle;
    private static nint _shimHandle;
    private static bool _resolverRegistered;

    /// <summary>当前进程原生绑定状态快照。</summary>
    public static ProcessBindingSnapshot Current
    {
        get { lock (Gate) return new(_state, _profile, _libraryPath, _shimPath, _failureReason); }
    }

    internal static (nint PythonHandle, nint ShimHandle) AcquireReservation(
        BindingProfile profile,
        string libraryPath,
        string shimPath)
    {
        string canonicalLib = Path.GetFullPath(libraryPath);
        string canonicalShim = Path.GetFullPath(shimPath);
        StringComparison cmp = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        lock (Gate)
        {
            if (_state == BindingState.Failed)
            {
                throw new InvalidOperationException(
                    $"进程原生绑定已处于不可恢复 Failed 终态，必须启动新 OS 进程：{_failureReason}");
            }
            if (_state is BindingState.Reserved or BindingState.Active)
            {
                throw new InvalidOperationException(
                    $"进程原生绑定当前处于 {_state}（profile={_profile}），禁止同进程并发或重入初始化");
            }
            if (_state == BindingState.Closed)
            {
                if (_profile != profile
                    || !string.Equals(_libraryPath, canonicalLib, cmp)
                    || !string.Equals(_shimPath, canonicalShim, cmp))
                {
                    throw new InvalidOperationException(
                        $"同进程禁止切换原生绑定三元组：已绑定 ({_profile}, {_libraryPath}, {_shimPath})，请求 ({profile}, {canonicalLib}, {canonicalShim})");
                }
            }
            else
            {
                _profile = profile;
                _libraryPath = canonicalLib;
                _shimPath = canonicalShim;
            }

            _state = BindingState.Reserved;
            return (_pythonHandle, _shimHandle);
        }
    }

    internal static void SetReservedHandles(nint pythonHandle, nint shimHandle)
    {
        lock (Gate)
        {
            if (_state != BindingState.Reserved)
                throw new InvalidOperationException($"仅 Reserved 状态允许提交句柄，当前 {_state}");
            _pythonHandle = pythonHandle;
            _shimHandle = shimHandle;
        }
    }

    internal static void EnsureResolverRegistered()
    {
        lock (Gate)
        {
            if (_resolverRegistered) return;
            _resolverRegistered = true;
            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, ResolveLibrary);
        }
    }

    internal static void CommitActive()
    {
        lock (Gate)
        {
            if (_state != BindingState.Reserved)
                throw new InvalidOperationException($"仅 Reserved 状态允许迁移到 Active，当前 {_state}");
            _state = BindingState.Active;
        }
    }

    internal static void CommitClosed()
    {
        lock (Gate)
        {
            if (_state == BindingState.Active) _state = BindingState.Closed;
        }
    }

    internal static void MarkFailed(string reason)
    {
        lock (Gate)
        {
            if (_state == BindingState.Failed) return;
            _state = BindingState.Failed;
            _failureReason = reason;
        }
    }

    private static nint ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        _ = assembly;
        _ = searchPath;
        lock (Gate)
        {
            // 允许 Closed 态继续解析同进程已驻留库句柄，确保收尾观测与纯 C 诊断入口即使首次冷解析也不会失配；
            // 实际执行边界由 CPythonHost 租约与状态机门控。
            if (_state is not (BindingState.Reserved or BindingState.Active or BindingState.Closed) || _shimHandle == 0)
                return 0;

            return _profile switch
            {
                BindingProfile.Compat when libraryName == Native.Library => _shimHandle,
                BindingProfile.StrictKernel when libraryName == StrictNative.Library || libraryName == Native.Library => _shimHandle,
                _ => 0,
            };
        }
    }
}
