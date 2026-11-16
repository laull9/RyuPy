// Slice 1 严格内核测试构建支撑：于仓库外临时目录构建 CP3.12.13 严格内核、
// libryupy_cpython_strict、compat libryupy_cpython 与基于每实例 heap type 的真实 tp_dealloc 探针库。
using System.Diagnostics;

namespace RyuPy.CPython.Tests;

public sealed class StrictKernelBuildFixture : IDisposable
{
    private readonly string _tempDir;
    public bool Available { get; }
    public string RepoRoot { get; }
    public string StrictHome { get; } = "";
    public string StrictLib { get; } = "";
    public string StrictShim { get; } = "";
    public string CompatHome { get; } = "";
    public string CompatLib { get; } = "";
    public string CompatShim { get; } = "";
    public string DeallocProbePath { get; } = "";
    public string RunnerDll { get; } = "";
    public string CheckPython { get; } = "";

    public StrictKernelBuildFixture()
    {
        RepoRoot = FindRepoRoot();
        _tempDir = Path.Combine(Path.GetTempPath(), "ryupy-slice1-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        try
        {
            string? compatHome = FindCompatPythonHome();
            if (compatHome is null) return;
            string? compatLib = FindPythonLibrary(compatHome, "3.12");
            if (compatLib is null) return;
            string checkPy = Path.Combine(compatHome, "bin", "python3.12");
            if (!File.Exists(checkPy)) checkPy = Path.Combine(compatHome, "bin", "python3");
            if (!File.Exists(checkPy)) return;

            CompatHome = compatHome;
            CompatLib = compatLib;
            CheckPython = checkPy;

            string strictPrefix;
            if (Environment.GetEnvironmentVariable("RYUPY_STRICT_KERNEL_PREFIX") is { Length: > 0 } existingPrefix
                && Directory.Exists(existingPrefix))
            {
                strictPrefix = existingPrefix;
            }
            else
            {
                string? tarball = FindKernelTarball();
                if (tarball is null) return;

                string strictWork = Path.Combine(_tempDir, "strict-kernel-build");
                Directory.CreateDirectory(strictWork);
                RunCommand(checkPy, [
                    Path.Combine(RepoRoot, "runtime", "kernel", "build_kernel.py"),
                    "--source-tarball", tarball,
                    "--workdir", strictWork,
                    "--keep",
                ], timeoutMs: 900_000);
                // --workdir 是安全临时根；构建器在其中创建唯一子目录，不能猜成根/prefix。
                string[] builds = Directory.GetDirectories(strictWork, "ryupy-kernel-312-*");
                if (builds.Length != 1)
                    throw new InvalidOperationException($"严格内核构建目录应唯一，实际 {builds.Length}");
                strictPrefix = Path.Combine(builds[0], "prefix");
            }

            string? strictLib = FindPythonLibrary(strictPrefix, "3.12");
            if (strictLib is null) throw new InvalidOperationException("严格内核缺少 libpython3.12");
            StrictHome = strictPrefix;
            StrictLib = strictLib;

            string strictShimDir = Path.Combine(_tempDir, "strict-shim");
            string strictBuildScript = Path.Combine(RepoRoot, "runtime", "RyuPy.CPython", "native", "build_strict.sh");
            RunCommand("sh", [strictBuildScript, StrictHome, strictShimDir, "3.12"], timeoutMs: 60_000);
            string shimExt = OperatingSystem.IsMacOS() ? "dylib" : "so";
            StrictShim = Path.Combine(strictShimDir, $"libryupy_cpython_strict.{shimExt}");

            string compatShimDir = Path.Combine(_tempDir, "compat-shim");
            string compatBuildScript = Path.Combine(RepoRoot, "runtime", "RyuPy.CPython", "native", "build.sh");
            RunCommand("sh", [compatBuildScript, CompatHome, compatShimDir, "3.12"], timeoutMs: 60_000);
            CompatShim = Path.Combine(compatShimDir, $"libryupy_cpython.{shimExt}");

            DeallocProbePath = BuildDeallocProbe(_tempDir, StrictHome, shimExt);

            string runnerProj = Path.Combine(RepoRoot, "runtime", "RyuPy.CPython.Tests", "Runner", "RyuPy.CPython.StrictRunner.csproj");
            string runnerOut = Path.Combine(_tempDir, "runner-bin");
            RunCommand("dotnet", ["build", runnerProj, "-c", "Debug", "-o", runnerOut, "-nologo", "-v", "q"], timeoutMs: 120_000);
            RunnerDll = Path.Combine(runnerOut, "RyuPy.CPython.StrictRunner.dll");
            Available = true;
        }
        catch
        {
            PreserveFailureLogs();
            Dispose();
            throw;
        }
        finally
        {
            // 若未进入 Available=true（例如缺少源码包提前 return），同样立即清理临时目录。
            if (!Available) Dispose();
        }
    }

    private void PreserveFailureLogs()
    {
        string? evidenceDir = Environment.GetEnvironmentVariable("RYUPY_SLICE1_FAILURE_LOG_DIR");
        if (string.IsNullOrWhiteSpace(evidenceDir)) return;
        string buildRoot = Path.Combine(_tempDir, "strict-kernel-build");
        if (!Directory.Exists(buildRoot)) return;
        try
        {
            Directory.CreateDirectory(evidenceDir);
            foreach (string build in Directory.GetDirectories(buildRoot, "ryupy-kernel-312-*"))
            {
                string logsSrc = Path.Combine(build, "logs");
                if (!Directory.Exists(logsSrc)) continue;
                foreach (string file in Directory.GetFiles(logsSrc))
                    File.Copy(file, Path.Combine(evidenceDir, Path.GetFileName(file)), overwrite: true);
            }
        }
        catch { }
    }

    public int RunSubprocess(string scenario, out string stdout, out string stderr)
    {
        if (!Available) throw new InvalidOperationException("严格内核构建环境不可用");
        string workDir = Path.Combine(_tempDir, "run-" + scenario + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(workDir);
        var env = new Dictionary<string, string>
        {
            ["RYUPY_TEST_WORKDIR"] = workDir,
            ["RYUPY_TEST_REPO_ROOT"] = RepoRoot,
            ["RYUPY_TEST_STRICT_HOME"] = StrictHome,
            ["RYUPY_TEST_STRICT_LIB"] = StrictLib,
            ["RYUPY_TEST_STRICT_SHIM"] = StrictShim,
            ["RYUPY_TEST_COMPAT_HOME"] = CompatHome,
            ["RYUPY_TEST_COMPAT_LIB"] = CompatLib,
            ["RYUPY_TEST_COMPAT_SHIM"] = CompatShim,
            ["RYUPY_TEST_DEALLOC_PROBE"] = DeallocProbePath,
            ["RYUPY_TEST_CHECK_PYTHON"] = CheckPython,
        };
        return ExecuteProcess("dotnet", [RunnerDll, scenario], env, timeoutMs: 60_000, out stdout, out stderr);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    private static string BuildDeallocProbe(string tempDir, string strictHome, string ext)
    {
        string cPath = Path.Combine(tempDir, "dealloc_probe.c");
        string outPath = Path.Combine(tempDir, $"libdealloc_probe.{ext}");
        // 采用无静态类型缓存的 PyType_FromSpec heap type：实例销毁后同步递减类型自身引用，
        // 避免跨 Py_FinalizeEx 顺序重建时残留旧 tp_dict 或静态状态污染。
        string src = """
            #define PY_SSIZE_T_CLEAN
            #include <Python.h>
            #include <ryupy_kernel.h>
            #include <stdint.h>
            #include <stdatomic.h>

            typedef struct {
                PyObject_HEAD
                PyObject *child;
                PyObject *callback_on_dealloc;
                int test_late_guard;
            } ProbeObj;

            static _Atomic int64_t g_dealloc_count = 0;
            static _Atomic int64_t g_dealloc_gil_valid = 0;
            static _Atomic int64_t g_cil_cleanup_count = 0;
            static _Atomic int64_t g_late_guard_rejected = 0;

            static int dummy_cil_exec(PyInterpreterState *interp, PyObject *m, PyObject *s, void *ud) {
                (void)interp; (void)m; (void)s; (void)ud;
                return 0;
            }

            static void dummy_cil_cleanup(void *ud) {
                (void)ud;
                atomic_fetch_add(&g_cil_cleanup_count, 1);
            }

            static void decoy_fini_fence(void *ud) {
                (void)ud;
                atomic_fetch_add(&g_cil_cleanup_count, 100);
            }

            static void probe_dealloc(PyObject *self) {
                ProbeObj *obj = (ProbeObj *)self;
                atomic_fetch_add(&g_dealloc_count, 1);
                if (PyGILState_Check() && PyThreadState_Get() != NULL) {
                    atomic_fetch_add(&g_dealloc_gil_valid, 1);
                }
                if (obj->test_late_guard) {
                    PyObject *late_mod = PyImport_ImportModule("_ryupy_late_zombie_probe");
                    if (late_mod == NULL) {
                        PyErr_Clear();
                        atomic_fetch_add(&g_late_guard_rejected, 1);
                    } else {
                        Py_DECREF(late_mod);
                    }
                    RpKernelImportEntry late_entry = {0};
                    late_entry.abi_version = RYUPY_IMPORT_ABI_VERSION;
                    late_entry.struct_size = (uint32_t)sizeof(RpKernelImportEntry);
                    late_entry.kind = RYUPY_IMPORT_KIND_CIL;
                    late_entry.name = "_ryupy_late_zombie_reg";
                    if (rp_kernel_import_register(PyInterpreterState_Get(), &late_entry) < 0) {
                        PyErr_Clear();
                        atomic_fetch_add(&g_late_guard_rejected, 1);
                    }
                }
                if (obj->callback_on_dealloc != NULL) {
                    PyObject *res = PyObject_CallNoArgs(obj->callback_on_dealloc);
                    if (res == NULL) PyErr_Clear();
                    else Py_DECREF(res);
                    Py_CLEAR(obj->callback_on_dealloc);
                }
                Py_CLEAR(obj->child);
                PyTypeObject *tp = Py_TYPE(self);
                tp->tp_free(self);
                Py_DECREF(tp);
            }

            static PyType_Slot probe_slots[] = {
                {Py_tp_dealloc, probe_dealloc},
                {0, NULL}
            };

            static PyType_Spec probe_spec = {
                .name = "dealloc_probe.ProbeObj",
                .basicsize = sizeof(ProbeObj),
                .itemsize = 0,
                .flags = Py_TPFLAGS_DEFAULT,
                .slots = probe_slots,
            };

            static PyObject *create_probe_instance(PyObject *child, PyObject *cb, int test_late_guard) {
                if (!Py_IsInitialized()) return NULL;
                PyTypeObject *heap_type = (PyTypeObject *)PyType_FromSpec(&probe_spec);
                if (!heap_type) return NULL;
                ProbeObj *obj = (ProbeObj *)PyType_GenericAlloc(heap_type, 0);
                /* PyType_GenericAlloc 已增加实例对 heap_type 的强引用；释放局部引用保持类型平衡。 */
                Py_DECREF((PyObject *)heap_type);
                if (!obj) return NULL;
                obj->child = child ? Py_NewRef(child) : NULL;
                obj->callback_on_dealloc = cb ? Py_NewRef(cb) : NULL;
                obj->test_late_guard = test_late_guard;
                return (PyObject *)obj;
            }

            __attribute__((visibility("default"))) void probe_reset(void) {
                atomic_store(&g_dealloc_count, 0);
                atomic_store(&g_dealloc_gil_valid, 0);
                atomic_store(&g_late_guard_rejected, 0);
            }

            __attribute__((visibility("default"))) int64_t probe_count(void) {
                return atomic_load(&g_dealloc_count);
            }

            __attribute__((visibility("default"))) int64_t probe_gil_valid_count(void) {
                return atomic_load(&g_dealloc_gil_valid);
            }

            __attribute__((visibility("default"))) int64_t probe_late_guard_count(void) {
                return atomic_load(&g_late_guard_rejected);
            }

            __attribute__((visibility("default"))) PyObject *probe_create(PyObject *child) {
                return create_probe_instance(child, NULL, 0);
            }

            __attribute__((visibility("default"))) PyObject *probe_create_late_guard(PyObject *child) {
                return create_probe_instance(child, NULL, 1);
            }

            __attribute__((visibility("default"))) PyObject *probe_create_with_callback(PyObject *child, PyObject *cb) {
                return create_probe_instance(child, cb, 0);
            }

            __attribute__((visibility("default"))) void probe_toggle_strict_reject_0_to_1(void) {
                rp_kernel_set_strict_reject(0);
                rp_kernel_set_strict_reject(1);
            }

            __attribute__((visibility("default"))) void probe_reset_eval_stats(void) {
                rp_kernel_reset_eval_stats();
            }

            __attribute__((visibility("default"))) int64_t probe_replace_cil_callbacks_twice(void) {
                atomic_store(&g_cil_cleanup_count, 0);
                RpKernelCilImportCallbacks cb1 = {0};
                cb1.abi_version = RYUPY_IMPORT_ABI_VERSION;
                cb1.struct_size = (uint32_t)sizeof(RpKernelCilImportCallbacks);
                cb1.exec_module = dummy_cil_exec;
                cb1.cleanup = dummy_cil_cleanup;
                if (rp_kernel_import_set_cil_callbacks(PyInterpreterState_Get(), &cb1) != 0) return -1;

                RpKernelCilImportCallbacks cb2 = {0};
                cb2.abi_version = RYUPY_IMPORT_ABI_VERSION;
                cb2.struct_size = (uint32_t)sizeof(RpKernelCilImportCallbacks);
                cb2.exec_module = dummy_cil_exec;
                cb2.cleanup = NULL;
                if (rp_kernel_import_set_cil_callbacks(PyInterpreterState_Get(), &cb2) != 0) return -2;
                return atomic_load(&g_cil_cleanup_count);
            }

            __attribute__((visibility("default"))) int64_t probe_verify_fini_fence_immutable(void) {
                PyInterpreterState *interp = PyInterpreterState_Get();
                if (rp_kernel_set_fini_fence(interp, NULL, NULL) == 0) return -1;
                if (!PyErr_ExceptionMatches(PyExc_ValueError)) { PyErr_Clear(); return -2; }
                PyErr_Clear();

                if (rp_kernel_set_fini_fence(interp, decoy_fini_fence, NULL) == 0) return -3;
                if (!PyErr_ExceptionMatches(PyExc_RuntimeError)) { PyErr_Clear(); return -4; }
                PyErr_Clear();
                return 1;
            }
            """;
        File.WriteAllText(cPath, src);
        var args = new List<string>();
        if (OperatingSystem.IsMacOS())
        {
            args.Add("-dynamiclib");
        }
        else
        {
            args.Add("-shared");
            args.Add("-fPIC");
        }
        args.Add("-O2");
        args.Add("-Wall");
        args.Add("-Wextra");
        args.Add("-Werror");
        args.Add("-I" + Path.Combine(strictHome, "include", "python3.12"));
        args.Add(cPath);
        if (OperatingSystem.IsMacOS())
        {
            args.Add("-L" + Path.Combine(strictHome, "lib"));
            args.Add("-lpython3.12");
        }
        else
        {
            args.Add(FindPythonLibrary(strictHome, "3.12")!);
        }
        args.Add("-o");
        args.Add(outPath);
        RunCommand("cc", args, timeoutMs: 30_000);
        return outPath;
    }

    private static int ExecuteProcess(
        string fileName,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string>? env,
        int timeoutMs,
        out string stdout,
        out string stderr)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in arguments) psi.ArgumentList.Add(arg);
        if (env is not null)
        {
            foreach ((string key, string value) in env) psi.Environment[key] = value;
        }
        using Process proc = Process.Start(psi)!;
        Task<string> outTask = proc.StandardOutput.ReadToEndAsync();
        Task<string> errTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            proc.WaitForExit(5_000);
            stdout = outTask.Wait(1_000) ? outTask.Result : "";
            stderr = errTask.Wait(1_000) ? errTask.Result : "";
            throw new TimeoutException($"子进程超时 ({timeoutMs} ms): {fileName} {string.Join(' ', arguments)}\nstdout:\n{stdout}\nstderr:\n{stderr}");
        }
        // 子进程退出后，其后代仍可能持有重定向管道；排空也必须有界，避免测试永久挂起。
        if (!Task.WaitAll([outTask, errTask], 5_000))
        {
            stdout = outTask.IsCompletedSuccessfully ? outTask.Result : "<stdout 管道未关闭>";
            stderr = errTask.IsCompletedSuccessfully ? errTask.Result : "<stderr 管道未关闭>";
            throw new TimeoutException($"子进程已退出但输出管道未在 5000 ms 内关闭: {fileName}\nstdout:\n{stdout}\nstderr:\n{stderr}");
        }
        stdout = outTask.Result;
        stderr = errTask.Result;
        return proc.ExitCode;
    }

    private static void RunCommand(string fileName, IReadOnlyList<string> arguments, int timeoutMs)
    {
        int code = ExecuteProcess(fileName, arguments, null, timeoutMs, out string stdout, out string stderr);
        if (code != 0)
            throw new InvalidOperationException($"命令失败 ({fileName}): exit={code}\n{stdout}\n{stderr}");
    }

    private static string FindRepoRoot()
    {
        if (Environment.GetEnvironmentVariable("RYUPY_TEST_REPO_ROOT") is { Length: > 0 } explicitRoot)
        {
            string full = Path.GetFullPath(explicitRoot);
            if (!File.Exists(Path.Combine(full, "Makefile")) || !Directory.Exists(Path.Combine(full, "runtime")))
                throw new InvalidOperationException($"RYUPY_TEST_REPO_ROOT 无效（缺少 Makefile 或 runtime）：{full}");
            return full;
        }
        string? current = AppContext.BaseDirectory;
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current, "Makefile")) && Directory.Exists(Path.Combine(current, "runtime")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }
        throw new InvalidOperationException("无法定位 RyuPy 仓库根目录");
    }

    private static string? FindCompatPythonHome()
    {
        if (Environment.GetEnvironmentVariable("RYUPY_CPYTHON_TEST_HOME") is { Length: > 0 } selected) return selected;
        string homeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "uv", "python");
        if (Directory.Exists(homeDir))
        {
            foreach (string dir in Directory.GetDirectories(homeDir, "cpython-3.12.13-*"))
                if (Directory.Exists(Path.Combine(dir, "lib", "python3.12"))) return dir;
        }
        return null;
    }

    private static string? FindKernelTarball()
    {
        if (Environment.GetEnvironmentVariable("RYUPY_STRICT_SOURCE_TARBALL") is { Length: > 0 } explicitTar && File.Exists(explicitTar))
            return explicitTar;
        foreach (string root in EnumerateTempRoots())
        {
            foreach (string dir in SafeDirectories(root, "ryupy-kernel-312-*"))
            {
                string candidate = Path.Combine(dir, "Python-3.12.13.tar.xz");
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static IEnumerable<string> EnumerateTempRoots()
    {
        yield return Path.GetTempPath();
        if (Directory.Exists("/tmp")) yield return "/tmp";
        if (OperatingSystem.IsMacOS() && Directory.Exists("/private/var/folders"))
        {
            foreach (string bucket in SafeDirectories("/private/var/folders", "*"))
                foreach (string user in SafeDirectories(bucket, "*"))
                {
                    string t = Path.Combine(user, "T");
                    if (Directory.Exists(t)) yield return t;
                }
        }
    }

    private static string[] SafeDirectories(string path, string pattern)
    {
        try { return Directory.Exists(path) ? Directory.GetDirectories(path, pattern) : []; }
        catch { return []; }
    }

    private static string? FindPythonLibrary(string pyHome, string series)
    {
        string[] candidates =
        [
            Path.Combine(pyHome, "lib", $"libpython{series}.dylib"),
            Path.Combine(pyHome, "lib", $"libpython{series}.so.1.0"),
            Path.Combine(pyHome, "lib", $"libpython{series}.so"),
        ];
        return candidates.FirstOrDefault(File.Exists);
    }
}
