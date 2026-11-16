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
            strictPrefix = Path.Combine(strictWork, "prefix");
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
            #include <stdatomic.h>

            typedef struct {
                PyObject_HEAD
                PyObject *child;
                PyObject *callback_on_dealloc;
            } ProbeObj;

            static atomic_long g_dealloc_count = 0;
            static atomic_long g_dealloc_gil_valid = 0;

            static void probe_dealloc(PyObject *self) {
                ProbeObj *obj = (ProbeObj *)self;
                atomic_fetch_add(&g_dealloc_count, 1);
                if (PyGILState_Check() && PyThreadState_Get() != NULL) {
                    atomic_fetch_add(&g_dealloc_gil_valid, 1);
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

            static PyObject *create_probe_instance(PyObject *child, PyObject *cb) {
                if (!Py_IsInitialized()) return NULL;
                PyTypeObject *heap_type = (PyTypeObject *)PyType_FromSpec(&probe_spec);
                if (!heap_type) return NULL;
                ProbeObj *obj = (ProbeObj *)PyType_GenericAlloc(heap_type, 0);
                /* PyType_GenericAlloc 已增加实例对 heap_type 的强引用；释放局部引用保持类型平衡。 */
                Py_DECREF((PyObject *)heap_type);
                if (!obj) return NULL;
                obj->child = child ? Py_NewRef(child) : NULL;
                obj->callback_on_dealloc = cb ? Py_NewRef(cb) : NULL;
                return (PyObject *)obj;
            }

            __attribute__((visibility("default"))) void probe_reset(void) {
                atomic_store(&g_dealloc_count, 0);
                atomic_store(&g_dealloc_gil_valid, 0);
            }

            __attribute__((visibility("default"))) long probe_count(void) {
                return atomic_load(&g_dealloc_count);
            }

            __attribute__((visibility("default"))) long probe_gil_valid_count(void) {
                return atomic_load(&g_dealloc_gil_valid);
            }

            __attribute__((visibility("default"))) PyObject *probe_create(PyObject *child) {
                return create_probe_instance(child, NULL);
            }

            __attribute__((visibility("default"))) PyObject *probe_create_with_callback(PyObject *child, PyObject *cb) {
                return create_probe_instance(child, cb);
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
        Task.WaitAll(outTask, errTask);
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
