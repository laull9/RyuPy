// Slice 1 独立子进程测试执行器：用于验证进程级原生绑定（ProcessNativeBinding）、
// 严格零字节码生命周期、FiniCore 真实 tp_dealloc 释放与三阶段审计闭环。
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using RyuPy.CPython;
using RyuPy.Runtime;

internal static partial class StrictRunnerProgram
{
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("用法: RyuPy.CPython.StrictRunner <scenario>");
            return 2;
        }
        try
        {
            return args[0] switch
            {
                "strict_math_atexit_audit" => RunStrictMathAtexitAudit(),
                "strict_fini_core_real_dealloc" => RunStrictFiniCoreRealDealloc(),
                "strict_sequential_rebuild_same_triple" => RunStrictSequentialRebuild(),
                "neg_compat_then_strict_rejected" => RunNegCompatThenStrict(),
                "neg_strict_then_compat_rejected" => RunNegStrictThenCompat(),
                "neg_strict_path_change_after_closed_rejected" => RunNegStrictPathChangeAfterClosed(),
                "neg_fake_kernel_triggers_terminal_failed" => RunNegFakeKernelTriggersFailed(),
                "neg_fence_blocks_new_leases_allows_op0" => RunNegFenceBlocksNewLeasesAllowsOp0(),
                "neg_strict_toggle_0_to_1_breaks_audit" => RunNegStrictToggle0To1BreaksAudit(),
                "neg_runtime_reset_eval_stats_breaks_audit" => RunNegRuntimeResetEvalStatsBreaksAudit(),
                "strict_replace_cil_callbacks_keeps_host_active" => RunStrictReplaceCilCallbacksKeepsHostActive(),
                "neg_compat_rebuild_rejected" => RunNegCompatRebuildRejected(),
                _ => throw new ArgumentException($"未知测试场景: {args[0]}")
            };
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.ToString());
            return 1;
        }
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"缺少环境变量 {name}");

    private static string WriteConfig(
        string dir,
        string name,
        string kernelMode,
        string home,
        string library,
        string shim,
        string[] searchPaths)
    {
        string path = Path.Combine(dir, name);
        var config = new
        {
            schema = 1,
            python_version = "3.12.13",
            development = true,
            kernel_mode = kernelMode,
            home,
            library,
            shim,
            search_paths = searchPaths,
            modules = new[] { "math", "atexit" },
            container_transfer = "proxy",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(config));
        return path;
    }

    private static PyFunction MakeNoArgCallable(string name, Func<object?> body)
    {
        var sig = PySignature.Create(name, [], [], []);
        return new PyFunction(name, sig, (_, _, _, _) => body(), [], usesSuper: false);
    }

    private static object? CallAttr(PyModule module, string name, params object?[] args) =>
        PyOps.CallValue(module.GetAttr(name), args, [], []);

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("断言失败: " + message);
    }

    private static (int ExitCode, string Output) InvokeCheckAudit(string reportPath)
    {
        string repoRoot = RequireEnv("RYUPY_TEST_REPO_ROOT");
        string py = RequireEnv("RYUPY_TEST_CHECK_PYTHON");
        string script = Path.Combine(repoRoot, "tests", "cil_execution", "check_audit.py");
        var psi = new ProcessStartInfo(py)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-B");
        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add(reportPath);
        using Process proc = Process.Start(psi)!;
        Task<string> outTask = proc.StandardOutput.ReadToEndAsync();
        Task<string> errTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(30_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("check_audit.py 超时");
        }
        Task.WaitAll(outTask, errTask);
        return (proc.ExitCode, $"{outTask.Result} {errTask.Result}");
    }

    private static void RunCheckAudit(string reportPath)
    {
        (int exitCode, string output) = InvokeCheckAudit(reportPath);
        AssertTrue(exitCode == 0, $"check_audit.py 失败 (exit={exitCode}): {output}");
    }

    private static int RunStrictMathAtexitAudit()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string reportPath = Path.Combine(workDir, "strict_math_atexit_report.json");
        Environment.SetEnvironmentVariable("RYUPY_CPYTHON_REPORT", reportPath);

        string cfg = WriteConfig(workDir, "strict.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        AssertTrue(ProcessNativeBinding.Current.State == BindingState.Unbound, "初始状态必须为 Unbound");

        Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
        AssertTrue(ProcessNativeBinding.Current.State == BindingState.Active, "启动后状态必须为 Active");
        AssertTrue(ProcessNativeBinding.Current.Profile == BindingProfile.StrictKernel, "Profile 必须为 StrictKernel");

        PyModule mathMod = PyModules.Import("math");
        double sqrtVal = PyCast.ToFloat64(CallAttr(mathMod, "sqrt", 9.0));
        AssertTrue(Math.Abs(sqrtVal - 3.0) < 1e-12, $"math.sqrt(9.0) 应为 3.0，实际 {sqrtVal}");

        bool atexitRan = false;
        double cosVal = double.NaN;
        PyModule atexitMod = PyModules.Import("atexit");
        PyFunction cb = MakeNoArgCallable("strict_atexit_cb", () =>
        {
            atexitRan = true;
            cosVal = PyCast.ToFloat64(CallAttr(mathMod, "cos", 0.0));
            return PyNone.Instance;
        });
        CallAttr(atexitMod, "register", cb);

        CPythonHost hostRef = Bootstrap.Host;
        Bootstrap.Stop();

        AssertTrue(atexitRan, "atexit CIL 回调未在关闭阶段执行");
        AssertTrue(Math.Abs(cosVal - 1.0) < 1e-12, $"atexit 内 math.cos(0.0) 应为 1.0，实际 {cosVal}");
        AssertTrue(ProcessNativeBinding.Current.State == BindingState.Closed, "关闭后状态必须为 Closed");
        HostStatistics stats = hostRef.Statistics;
        AssertTrue(stats.State == "closed", $"期望 state=closed，实际 {stats.State}");
        AssertTrue(stats.Handles == 0 && stats.ManagedRoots == 0 && stats.PendingReleases == 0 && stats.LateReleases == 0,
            $"关闭后存在泄漏或迟到释放: {stats}");

        RunCheckAudit(reportPath);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));
        JsonElement audit = doc.RootElement.GetProperty("execution_audit");
        AssertTrue(audit.GetProperty("strict_kernel").GetBoolean(), "audit.strict_kernel 必须为 true");
        JsonElement fence = doc.RootElement.GetProperty("fini_core_fence");
        AssertTrue(fence.GetProperty("executed").GetBoolean(), "fini_core_fence.executed 必须为 true");
        AssertTrue(fence.GetProperty("post_finalize_decrefs").GetInt64() == 0, "post_finalize_decrefs 必须为 0");
        return 0;
    }

    private static int RunStrictFiniCoreRealDealloc()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string probePath = RequireEnv("RYUPY_TEST_DEALLOC_PROBE");
        string reportPath = Path.Combine(workDir, "strict_dealloc_report.json");
        Environment.SetEnvironmentVariable("RYUPY_CPYTHON_REPORT", reportPath);

        string cfg = WriteConfig(workDir, "strict_dealloc.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
        ProbeNative.Initialize(probePath);
        ProbeNative.probe_reset();

        // 构造三类释放时机均需通过 FiniCore 真实释放的探针对象：
        // 1) heldProxy: 一直保留至关闭；
        // 2) atexitDisposed: 在 atexit（state=4）期间显式 Dispose()；
        // 3) CreateUnreachableProbe(): 在 atexit（state=4）触发 GC + WaitForPendingFinalizers() 由后台 Finalizer 释放。
        CPythonHost hostRef = Bootstrap.Host;
        PyFunction managedChild = MakeNoArgCallable("held_managed_child", () => PyNone.Instance);
        object? CreateProbe()
        {
            return hostRef.PythonCall(() =>
            {
                nint childPtr = hostRef.ToPython(managedChild);
                nint probePtr;
                try { probePtr = ProbeNative.probe_create(childPtr); }
                finally { hostRef.DecrefDirect(childPtr); }
                return hostRef.FromOwned(probePtr);
            });
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        void SpawnUnreachableProbe()
        {
            object? temp = CreateProbe();
            AssertTrue(temp is PythonObject, "unreachable probe 应包装为 PythonObject");
        }

        object? heldProxy = hostRef.PythonCall(() =>
        {
            nint childPtr = hostRef.ToPython(managedChild);
            nint probePtr;
            try { probePtr = ProbeNative.probe_create_late_guard(childPtr); }
            finally { hostRef.DecrefDirect(childPtr); }
            return hostRef.FromOwned(probePtr);
        });
        var atexitDisposed = AssertIs<PythonObject>(CreateProbe(), "atexitDisposed 应为 PythonObject");
        SpawnUnreachableProbe();
        GC.KeepAlive(heldProxy);
        GC.KeepAlive(atexitDisposed);

        long countDuringAtexitBefore = -1;
        long countDuringAtexitAfterDisposeAndGc = -1;
        PyModule atexitMod = PyModules.Import("atexit");
        CallAttr(atexitMod, "register", MakeNoArgCallable("check_alive_in_atexit", () =>
        {
            countDuringAtexitBefore = ProbeNative.probe_count();
            atexitDisposed.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            countDuringAtexitAfterDisposeAndGc = ProbeNative.probe_count();
            return PyNone.Instance;
        }));

        Bootstrap.Stop();
        GC.KeepAlive(heldProxy);

        AssertTrue(countDuringAtexitBefore == 0 && countDuringAtexitAfterDisposeAndGc == 0,
            $"atexit（state=4）期间仅入队，不应提前触发或遗漏 tp_dealloc: before={countDuringAtexitBefore}, after={countDuringAtexitAfterDisposeAndGc}");
        long afterCount = ProbeNative.probe_count();
        long afterGilValid = ProbeNative.probe_gil_valid_count();
        long lateGuardRejected = ProbeNative.probe_late_guard_count();
        AssertTrue(afterCount == 3, $"FiniCore 必须真实 Py_DECREF 全部 3 个句柄（含 state=4 Dispose 与 Finalizer），实际={afterCount}");
        AssertTrue(afterGilValid == 3, $"全部 3 次 tp_dealloc 均须在有效 GIL/tstate 栅栏内运行，实际={afterGilValid}");
        AssertTrue(lateGuardRejected == 2, $"FiniCore 析构重入 PyImport_ImportModule 与 rp_kernel_import_register 均必须被墓碑与 finalizing 守卫拦截（期望=2，实际={lateGuardRejected}）");

        HostStatistics stats = hostRef.Statistics;
        AssertTrue(stats.Handles == 0 && stats.ManagedRoots == 0 && stats.PendingReleases == 0 && stats.LateReleases == 0,
            $"关闭后存在泄漏或迟到释放: {stats}");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));
        JsonElement fence = doc.RootElement.GetProperty("fini_core_fence");
        AssertTrue(fence.GetProperty("live_handles_at_shutdown").GetInt32() >= 1, "live_handles_at_shutdown 至少为 1");
        AssertTrue(fence.GetProperty("live_handles_freed_in_fence").GetInt32() >= 3, "live_handles_freed_in_fence 至少应包含全部 3 个句柄");
        AssertTrue(fence.GetProperty("post_finalize_decrefs").GetInt64() == 0, "post_finalize_decrefs 必须为 0");
        RunCheckAudit(reportPath);
        return 0;
    }

    private static T AssertIs<T>(object? value, string message)
    {
        if (value is T typed) return typed;
        throw new InvalidOperationException("断言失败: " + message);
    }

    private static int RunStrictSequentialRebuild()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string cfg = WriteConfig(workDir, "strict_rebuild.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);

        for (int round = 1; round <= 2; round++)
        {
            string reportPath = Path.Combine(workDir, $"strict_rebuild_{round}.json");
            Environment.SetEnvironmentVariable("RYUPY_CPYTHON_REPORT", reportPath);
            Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
            AssertTrue(ProcessNativeBinding.Current.State == BindingState.Active, $"第 {round} 轮应为 Active");
            PyModule mathMod = PyModules.Import("math");
            double v = PyCast.ToFloat64(CallAttr(mathMod, "sqrt", round == 1 ? 16.0 : 25.0));
            AssertTrue(Math.Abs(v - (round == 1 ? 4.0 : 5.0)) < 1e-12, $"第 {round} 轮 sqrt 错误: {v}");
            bool cbRan = false;
            PyModule atexitMod = PyModules.Import("atexit");
            CallAttr(atexitMod, "register", MakeNoArgCallable($"cb_{round}", () => { cbRan = true; return PyNone.Instance; }));
            Bootstrap.Stop();
            AssertTrue(cbRan, $"第 {round} 轮 atexit 未执行");
            AssertTrue(ProcessNativeBinding.Current.State == BindingState.Closed, $"第 {round} 轮关闭后应为 Closed");
            // 首次冷解析 pure-C 诊断入口，验证 Closed 状态仍允许 ProcessNativeBinding 解析已绑定进程库。
            int phaseAfterClose = round == 1
                ? StrictNative.rp_strict_get_phase()
                : StrictNative.rp_strict_get_strict_reject();
            AssertTrue(phaseAfterClose >= 0, $"Closed 后冷 P/Invoke 查询失败: round={round}");
            RunCheckAudit(reportPath);
        }
        return 0;
    }

    private static int RunNegCompatThenStrict()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string compatHome = RequireEnv("RYUPY_TEST_COMPAT_HOME");
        string compatLib = RequireEnv("RYUPY_TEST_COMPAT_LIB");
        string compatShim = RequireEnv("RYUPY_TEST_COMPAT_SHIM");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");

        string compatCfg = WriteConfig(workDir, "compat.json", "compat", compatHome, compatLib, compatShim, [Path.Combine(compatHome, "lib", "python3.12")]);
        string strictCfg = WriteConfig(workDir, "strict.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);

        Bootstrap.Start(compatCfg, ["ryupy-runner"]);
        Bootstrap.Stop();
        AssertTrue(ProcessNativeBinding.Current.State == BindingState.Closed && ProcessNativeBinding.Current.Profile == BindingProfile.Compat, "应已绑定 Compat");

        bool threw = false;
        try { Bootstrap.StartStrictUnsealed(strictCfg, ["ryupy-runner"]); }
        catch (InvalidOperationException) { threw = true; }
        AssertTrue(threw, "同进程从 Compat 切换 StrictKernel 必须抛出 InvalidOperationException");
        return 0;
    }

    private static int RunNegStrictThenCompat()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string compatHome = RequireEnv("RYUPY_TEST_COMPAT_HOME");
        string compatLib = RequireEnv("RYUPY_TEST_COMPAT_LIB");
        string compatShim = RequireEnv("RYUPY_TEST_COMPAT_SHIM");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");

        string compatCfg = WriteConfig(workDir, "compat.json", "compat", compatHome, compatLib, compatShim, [Path.Combine(compatHome, "lib", "python3.12")]);
        string strictCfg = WriteConfig(workDir, "strict.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);

        Bootstrap.StartStrictUnsealed(strictCfg, ["ryupy-runner"]);
        Bootstrap.Stop();

        bool threw = false;
        try { Bootstrap.Start(compatCfg, ["ryupy-runner"]); }
        catch (InvalidOperationException) { threw = true; }
        AssertTrue(threw, "同进程从 StrictKernel 切换 Compat 必须抛出 InvalidOperationException");
        return 0;
    }

    private static int RunNegStrictPathChangeAfterClosed()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string shimCopy = Path.Combine(workDir, "libryupy_cpython_strict_copy.dylib");
        File.Copy(strictShim, shimCopy, overwrite: true);

        string cfg1 = WriteConfig(workDir, "strict1.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        string cfg2 = WriteConfig(workDir, "strict2.json", "strict", strictHome, strictLib, shimCopy, [Path.Combine(strictHome, "lib", "python3.12")]);

        Bootstrap.StartStrictUnsealed(cfg1, ["ryupy-runner"]);
        Bootstrap.Stop();

        bool threw = false;
        try { Bootstrap.StartStrictUnsealed(cfg2, ["ryupy-runner"]); }
        catch (InvalidOperationException) { threw = true; }
        AssertTrue(threw, "Closed 后更换 ShimPath 重建必须被拒绝");
        return 0;
    }

    private static int RunNegFakeKernelTriggersFailed()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string compatHome = RequireEnv("RYUPY_TEST_COMPAT_HOME");
        string compatLib = RequireEnv("RYUPY_TEST_COMPAT_LIB");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");

        // 指向普通 CPython 3.12.13（缺少 rp_kernel_* 严格导出）试图启动 strict 模式。
        string fakeCfg = WriteConfig(workDir, "fake_strict.json", "strict", compatHome, compatLib, strictShim, [Path.Combine(compatHome, "lib", "python3.12")]);
        string validCfg = WriteConfig(workDir, "valid_strict.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);

        bool firstFailed = false;
        try { Bootstrap.StartStrictUnsealed(fakeCfg, ["ryupy-runner"]); }
        catch (InvalidOperationException) { firstFailed = true; }
        AssertTrue(firstFailed, "缺失 rp_kernel_* 的假内核必须拒绝启动");
        AssertTrue(ProcessNativeBinding.Current.State == BindingState.Failed,
            $"失败后 ProcessNativeBinding 必须进入不可逆 Failed 终态，实际为 {ProcessNativeBinding.Current.State}");

        bool secondBlocked = false;
        try { Bootstrap.StartStrictUnsealed(validCfg, ["ryupy-runner"]); }
        catch (InvalidOperationException) { secondBlocked = true; }
        AssertTrue(secondBlocked, "进入 Failed 终态后，即使换回合法配置同进程也必须永久拒绝");
        return 0;
    }

    private static int RunNegFenceBlocksNewLeasesAllowsOp0()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string probePath = RequireEnv("RYUPY_TEST_DEALLOC_PROBE");

        string cfg = WriteConfig(workDir, "strict_fence_reentry.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
        ProbeNative.Initialize(probePath);

        // 构造由 C 探针对象持有的两个托管对象：
        // 1) 作为子对象挂接、测试 tp_dealloc -> Managed op=0 正常清除 Roots；
        // 2) 在 FiniCore 期间尝试从 C tp_dealloc 再入调用 op=1（非 release 协议），验证栅栏拦截且不污染 Roots/handles。
        CPythonHost hostRef = Bootstrap.Host;
        bool illegalCallRan = false;
        PyFunction legalManagedObj = MakeNoArgCallable("legal_op0", () => PyNone.Instance);
        PyFunction illegalCallback = MakeNoArgCallable("illegal_op1_in_fence", () => { illegalCallRan = true; return PyNone.Instance; });
        object? held = hostRef.PythonCall(() =>
        {
            nint c1 = hostRef.ToPython(legalManagedObj);
            nint c2 = hostRef.ToPython(illegalCallback);
            try { return hostRef.FromOwned(ProbeNative.probe_create_with_callback(c1, c2)); }
            finally { hostRef.DecrefDirect(c1); hostRef.DecrefDirect(c2); }
        });
        GC.KeepAlive(held);

        Bootstrap.Stop();
        GC.KeepAlive(held);
        AssertTrue(!illegalCallRan, "FiniCore 释放栅栏内禁止再入 op!=0 用户/托管执行调用与新租约");
        AssertTrue(hostRef.Statistics.Handles == 0 && hostRef.Statistics.ManagedRoots == 0,
            $"允许 op=0 后 Handles/ManagedRoots 必须均归零: {hostRef.Statistics}");
        return 0;
    }

    private static int RunNegStrictToggle0To1BreaksAudit()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string probePath = RequireEnv("RYUPY_TEST_DEALLOC_PROBE");
        string reportPath = Path.Combine(workDir, "neg_toggle_report.json");
        Environment.SetEnvironmentVariable("RYUPY_CPYTHON_REPORT", reportPath);

        string cfg = WriteConfig(workDir, "strict_toggle.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
        ProbeNative.Initialize(probePath);
        ProbeNative.probe_toggle_strict_reject_0_to_1();
        Bootstrap.Stop();

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));
        JsonElement audit = doc.RootElement.GetProperty("execution_audit");
        AssertTrue(!audit.GetProperty("strict_reject_never_disabled").GetBoolean(), "0->1 切换后 strict_reject_never_disabled 必须为 false");
        AssertTrue(!audit.GetProperty("complete").GetBoolean(), "0->1 切换后 complete 必须为 false");
        AssertTrue(audit.GetProperty("coverage_failures").GetInt64() >= 1, "0->1 切换后 coverage_failures 必须 >= 1");
        (int exitCode, _) = InvokeCheckAudit(reportPath);
        AssertTrue(exitCode != 0, "check_audit.py 必须拒绝运行期间 0->1 关闭严格拒绝的报告");
        return 0;
    }

    private static int RunNegRuntimeResetEvalStatsBreaksAudit()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string probePath = RequireEnv("RYUPY_TEST_DEALLOC_PROBE");
        string reportPath = Path.Combine(workDir, "neg_reset_report.json");
        Environment.SetEnvironmentVariable("RYUPY_CPYTHON_REPORT", reportPath);

        string cfg = WriteConfig(workDir, "strict_reset.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
        ProbeNative.Initialize(probePath);
        ProbeNative.probe_reset_eval_stats();
        Bootstrap.Stop();

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));
        JsonElement audit = doc.RootElement.GetProperty("execution_audit");
        AssertTrue(!audit.GetProperty("stats_never_reset").GetBoolean(), "运行期 reset 后 stats_never_reset 必须为 false");
        AssertTrue(!audit.GetProperty("complete").GetBoolean(), "运行期 reset 后 complete 必须为 false");
        AssertTrue(audit.GetProperty("coverage_failures").GetInt64() >= 1, "运行期 reset 后 coverage_failures 必须 >= 1");
        (int exitCode, _) = InvokeCheckAudit(reportPath);
        AssertTrue(exitCode != 0, "check_audit.py 必须拒绝运行期重置求值计数的报告");
        return 0;
    }

    private static int RunStrictReplaceCilCallbacksKeepsHostActive()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string strictHome = RequireEnv("RYUPY_TEST_STRICT_HOME");
        string strictLib = RequireEnv("RYUPY_TEST_STRICT_LIB");
        string strictShim = RequireEnv("RYUPY_TEST_STRICT_SHIM");
        string probePath = RequireEnv("RYUPY_TEST_DEALLOC_PROBE");
        string reportPath = Path.Combine(workDir, "strict_replace_cb_report.json");
        Environment.SetEnvironmentVariable("RYUPY_CPYTHON_REPORT", reportPath);

        string cfg = WriteConfig(workDir, "strict_replace.json", "strict", strictHome, strictLib, strictShim, [Path.Combine(strictHome, "lib", "python3.12")]);
        Bootstrap.StartStrictUnsealed(cfg, ["ryupy-strict-runner"]);
        ProbeNative.Initialize(probePath);

        long cilCleanupCount = Bootstrap.Host.PythonCall(() => ProbeNative.probe_replace_cil_callbacks_twice());
        AssertTrue(cilCleanupCount == 1, $"两次合法替换 CIL callbacks 后应仅触发 CIL 自身 old_cleanup=1 次，实际={cilCleanupCount}");
        long fenceImmutable = Bootstrap.Host.PythonCall(() => ProbeNative.probe_verify_fini_fence_immutable());
        AssertTrue(fenceImmutable == 1, $"已安装的 FiniCore Fence 必须拒绝 NULL 清空与异构替换，实际={fenceImmutable}");
        AssertTrue(Bootstrap.Host.Statistics.State == "running",
            $"替换 CIL callbacks 绝不能提前触发 FiniCore Fence 或退化宿主状态，实际={Bootstrap.Host.Statistics.State}");

        PyModule mathMod = PyModules.Import("math");
        double v = PyCast.ToFloat64(CallAttr(mathMod, "sqrt", 9.0));
        AssertTrue(Math.Abs(v - 3.0) < 1e-12, $"替换 CIL callbacks 后 math.sqrt(9) 应仍为 3.0，实际={v}");

        Bootstrap.Stop();
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));
        JsonElement fence = doc.RootElement.GetProperty("fini_core_fence");
        AssertTrue(fence.GetProperty("executed").GetBoolean(), "真实关机必须执行 FiniCore Fence");
        AssertTrue(fence.GetProperty("calls").GetInt32() == 1, $"FiniCore Fence 须精确触发 1 次，实际={fence.GetProperty("calls").GetInt32()}");
        RunCheckAudit(reportPath);
        return 0;
    }

    private static int RunNegCompatRebuildRejected()
    {
        string workDir = RequireEnv("RYUPY_TEST_WORKDIR");
        string compatHome = RequireEnv("RYUPY_TEST_COMPAT_HOME");
        string compatLib = RequireEnv("RYUPY_TEST_COMPAT_LIB");
        string compatShim = RequireEnv("RYUPY_TEST_COMPAT_SHIM");
        string cfg = Path.Combine(workDir, "compat_inspect.json");
        File.WriteAllText(cfg, JsonSerializer.Serialize(new
        {
            schema = 1,
            python_version = "3.12.13",
            development = true,
            kernel_mode = "compat",
            native_inspect_profile = true,
            home = compatHome,
            library = compatLib,
            shim = compatShim,
            search_paths = new[]
            {
                Path.Combine(compatHome, "lib", "python3.12"),
                Path.Combine(compatHome, "lib", "python3.12", "lib-dynload"),
            },
            modules = new[] { "collections" },
            container_transfer = "proxy",
        }));

        Bootstrap.Start(cfg, ["ryupy-runner"]);
        AssertTrue(Bootstrap.Host.Statistics.State == "running", "Compat 应为 running");
        Bootstrap.Stop();
        AssertTrue(ProcessNativeBinding.Current.State == BindingState.Closed, "Compat 关闭后应为 Closed");

        bool threw = false;
        try { Bootstrap.Start(cfg, ["ryupy-runner"]); }
        catch (InvalidOperationException) { threw = true; }
        AssertTrue(threw, "compat 通道在 Closed 后顺序重建必须明确拒绝，避免 managed_type 泄漏");
        return 0;
    }
}
