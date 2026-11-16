// 严格零字节码内核启动与 FiniCore 释放栅栏实现：
// 1. 遵循 ProcessNativeBinding 预占 -> TryGetExport 仅检存在 -> 静态 P/Invoke 核验 -> Active/Failed 终态；
// 2. 启动设置 strict_reject=1, _install_importlib=0，跳过一切 compat 导入/审计源码；
// 3. 关闭时保留所有类/方法/根至 _PyAtExit_Call 完成，在 _PyImport_FiniCore 栅栏内真实 Py_DECREF 释放全部句柄。
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private static readonly string[] RequiredStrictKernelExports =
    [
        "Py_GetVersion",
        "rp_kernel_version",
        "rp_kernel_set_phase",
        "rp_kernel_get_phase",
        "rp_kernel_set_strict_reject",
        "rp_kernel_get_strict_reject",
        "rp_kernel_reset_eval_stats",
        "rp_kernel_eval_entry_count",
        "rp_kernel_eval_executed_count",
        "rp_kernel_eval_rejected_count",
        "rp_kernel_eval_total_entries",
        "rp_kernel_eval_total_rejected",
        "rp_kernel_import_register",
        "rp_kernel_import_set_cil_callbacks",
        "rp_kernel_import_freeze",
        "rp_kernel_import_is_frozen",
    ];

    private static readonly string[] RequiredStrictShimExports =
    [
        "rp_strict_build_version",
        "rp_strict_python_version",
        "rp_strict_kernel_version",
        "rp_strict_has_import_abi",
        "rp_strict_start",
        "rp_strict_stop",
    ];

    private readonly bool isStrictKernel;
    private string strictKernelVersion = "";

    internal static unsafe CPythonHost StartStrictUnsealed(string path, string[] argv)
    {
        HostConfiguration configuration = HostConfiguration.Read(path);
        if (configuration.KernelMode != "strict")
            throw new InvalidDataException($"StartStrictUnsealed 要求 kernel_mode=strict，实际为 {configuration.KernelMode}");

        (nint existingPython, nint existingShim) = ProcessNativeBinding.AcquireReservation(
            BindingProfile.StrictKernel,
            configuration.Library,
            configuration.Shim);

        var host = new CPythonHost(configuration, strictKernel: true);
        Instance = host;
        try
        {
            host.pythonPath = configuration.Library;
            host.shimPath = configuration.Shim;
            if (OperatingSystem.IsWindows()) ConfigureWindowsSearch(configuration);

            nint pyLib = existingPython != 0 ? existingPython : LoadPythonLibrary(configuration.Library);
            host.pythonLibrary = pyLib;
            foreach (string symbol in RequiredStrictKernelExports)
            {
                if (!NativeLibrary.TryGetExport(pyLib, symbol, out _))
                    throw new MissingMethodException($"目标 libpython 缺少严格内核导出符号：{symbol}");
            }

            nint shimLib = existingShim != 0
                ? existingShim
                : (OperatingSystem.IsWindows() ? WindowsLoad(configuration.Shim) : NativeLibrary.Load(configuration.Shim));
            host.shimLibrary = shimLib;
            foreach (string symbol in RequiredStrictShimExports)
            {
                if (!NativeLibrary.TryGetExport(shimLib, symbol, out _))
                    throw new MissingMethodException($"目标严格 shim 缺少必需导出符号：{symbol}");
            }

            ProcessNativeBinding.SetReservedHandles(pyLib, shimLib);
            ProcessNativeBinding.EnsureResolverRegistered();

            string shimBuildVersion = Marshal.PtrToStringUTF8(StrictNative.rp_strict_build_version()) ?? "";
            if (shimBuildVersion != configuration.ExpectedVersion)
                throw new InvalidDataException($"严格 shim 构建版本应为 {configuration.ExpectedVersion}，实际为 {shimBuildVersion}");
            string actualPythonVersion = Marshal.PtrToStringUTF8(StrictNative.rp_strict_python_version()) ?? "";
            if (!actualPythonVersion.StartsWith(configuration.ExpectedVersion + " ", StringComparison.Ordinal))
                throw new InvalidDataException($"严格内核 ABI 要求 {configuration.ExpectedVersion}，实际加载 {actualPythonVersion}");
            string kernelVersion = Marshal.PtrToStringUTF8(StrictNative.rp_strict_kernel_version()) ?? "";
            if (string.IsNullOrEmpty(kernelVersion))
                throw new InvalidDataException("严格内核返回空的 rp_kernel_version");
            if (StrictNative.rp_strict_has_import_abi() == 0)
                throw new InvalidDataException("严格内核 C4-1 导入 ABI 不可用");
            host.strictKernelVersion = kernelVersion;

            host.ValidateStrictAuditEnvironment();
            host.tclTkEnvironment = TclTkEnvironment.Configure(configuration.Home);
            nint[] paths = configuration.SearchPaths.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
            nint[] args = argv.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
            try
            {
                fixed (nint* pathPointer = paths, argPointer = args)
                    if (StrictNative.rp_strict_start(
                            configuration.Home,
                            pathPointer,
                            paths.Length,
                            argPointer,
                            args.Length,
                            &Dispatch,
                            &OnFiniCoreFence) != 0)
                    {
                        throw new InvalidOperationException(
                            "严格 CPython 内核初始化失败：" + Marshal.PtrToStringUTF8(StrictNative.rp_strict_init_error()));
                    }
            }
            finally
            {
                foreach (nint item in paths) Marshal.FreeCoTaskMem(item);
                foreach (nint item in args) Marshal.FreeCoTaskMem(item);
            }

            host.state = 1;
            host.InstallInspectBridge(configuration.NativeInspectProfile);
            if (StrictNative.rp_strict_set_phase(1) != 0)
                throw new InvalidOperationException("切换严格内核审计阶段到 RUNTIME 失败");
            ProcessNativeBinding.CommitActive();
            return host;
        }
        catch (Exception error)
        {
            if (host.state == 1)
            {
                try { host.Dispose(); } catch { }
            }
            host.tclTkEnvironment?.TryRestore();
            host.state = 3;
            Instance = null;
            ProcessNativeBinding.MarkFailed(error.Message);
            throw new InvalidOperationException(
                $"严格 CPython 启动失败 [stage=load/init config={path} library={configuration.Library} shim={configuration.Shim}]：{error.Message}",
                error);
        }
    }

    internal void DecrefDirect(nint pointer)
    {
        if (pointer == 0) return;
        if (postFinalizePhase)
        {
            postFinalizeDecrefs++;
            throw new InvalidOperationException("Py_FinalizeEx 返回后禁止再调用 rp_decref");
        }
        Native.rp_decref(pointer);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnFiniCoreFence()
    {
        CPythonHost? host = Instance;
        if (host is null) return;
        try
        {
            host.RunFiniCoreFence();
        }
        catch (Exception error)
        {
            // 绝不允许托管异常穿过 C 栈（_PyImport_FiniCore），记录失败以令 Dispose 与审计拒绝完成态。
            host.finiCoreError ??= error;
        }
    }

    private void RunFiniCoreFence()
    {
        lock (gate)
        {
            state = 5;
            liveHandlesAtShutdown = handles.Count;
        }

        // 在解释器/GIL 仍有效且 atexit 已结束的 FiniCore 栅栏内释放类桥接缓存与全部原生句柄，
        // 若 Py_DECREF 触发 Managed::tp_dealloc 则允许 op=0 重入清除 Roots/reverse，
        // 同时拒绝一切新租约与新托管引用注册。
        try
        {
            ReleaseClassBridgeResources();
        }
        catch (Exception error)
        {
            finiCoreError ??= error;
        }

        while (TryDequeueOrPopHandle(out nint pointer))
        {
            liveHandlesFreedInFence++;
            try
            {
                DecrefDirect(pointer);
            }
            catch (Exception error)
            {
                finiCoreError ??= error;
            }
        }
    }

    private bool TryDequeueOrPopHandle(out nint pointer)
    {
        if (releases.TryDequeue(out pointer)) return true;
        lock (gate)
        {
            if (releases.TryDequeue(out pointer)) return true;
            if (handles.Count == 0)
            {
                pointer = 0;
                return false;
            }
            using Dictionary<long, nint>.Enumerator enumerator = handles.GetEnumerator();
            enumerator.MoveNext();
            long token = enumerator.Current.Key;
            pointer = enumerator.Current.Value;
            handles.Remove(token);
            return true;
        }
    }

    private void DisposeStrictCore()
    {
        Exception? observationError = null;
        try
        {
            using Lease lease = Enter();
            if (StrictNative.rp_strict_set_phase(2) != 0)
                throw new InvalidOperationException("切换严格内核审计阶段到 SHUTDOWN 失败");
            try { CaptureModulePaths(); } catch (Exception error) { observationError = error; }
            // 严格保留类/方法缓存与活跃句柄直到 _PyAtExit_Call 完成，实际释放在 FiniCore 栅栏完成。
        }
        catch (Exception error) { observationError ??= error; }

        lock (gate) state = 4;
        int result;
        try { result = StrictNative.rp_strict_stop(); }
        finally
        {
            postFinalizePhase = true;
            RemoveInspectBridge();
            strictAuditSnapshot = CaptureStrictAuditSnapshot(strictKernelVersion);
            // Py_FinalizeEx 已经返回，此处绝不再调用任何 rp_decref，只清空托管账本。
            Roots.Clear();
            imported.Clear();
            objects.Clear();
            transfers.Clear();
            transferIndex.Clear();
            lock (gate)
            {
                handles.Clear();
                state = 3;
            }
            Instance = null;
            ProcessNativeBinding.CommitClosed();
            tclTkEnvironment?.TryRestore();
        }
        WriteReport();
        if (finiCoreError is not null) throw new InvalidOperationException("FiniCore释放栅栏执行失败", finiCoreError);
        if (strictAuditSnapshot is { FiniCoreCalls: < 1 }) throw new InvalidOperationException("strict内核关闭未触发FiniCore释放栅栏");
        if (observationError is not null) throw new InvalidOperationException("CPython关闭观测失败", observationError);
        if (result != 0) throw new InvalidOperationException("CPython关闭时标准流刷新失败");
    }
}
