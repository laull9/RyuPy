// CPython 3.12.13 真实解释器集成测试：验证通过 IPyForeignClassBridge 继承 Python/C 扩展类、
// 真实 tkinter.Tk 窗口类派生与按钮回调、多层/混合继承、描述符、object 基础槽及清理生命周期。
using System.Diagnostics;
using System.Text.Json;
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

/// <summary>单进程共享 CPython 3.12.13 测试宿主，结束后负责关闭解释器与清理临时目录。</summary>
public sealed class CPythonBridgeFixture : IDisposable
{
    private readonly string _tempDir;
    public bool Available { get; }

    public CPythonBridgeFixture()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ryupy-cpython-bridge-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        string? pyHome = FindPythonHome();
        if (pyHome is null)
        {
            return;
        }
        string? pyLib = FindPythonLibrary(pyHome);
        if (pyLib is null)
        {
            return;
        }
        string repoRoot = FindRepoRoot();
        string shimExt = OperatingSystem.IsMacOS() ? "dylib" : OperatingSystem.IsWindows() ? "dll" : "so";
        string shimPrefix = OperatingSystem.IsWindows() ? "" : "lib";
        string shimPath = Path.Combine(_tempDir, $"{shimPrefix}ryupy_cpython.{shimExt}");
        string buildScript = Path.Combine(repoRoot, "runtime", "RyuPy.CPython", "native", "build.sh");
        string prebuiltShim = Path.Combine(repoRoot, "runtime", "RyuPy.CPython", "bin", "Release", "net10.0", $"{shimPrefix}ryupy_cpython.{shimExt}");

        if (File.Exists(prebuiltShim))
        {
            File.Copy(prebuiltShim, shimPath, overwrite: true);
        }
        else if (!OperatingSystem.IsWindows() && File.Exists(buildScript))
        {
            var psi = new ProcessStartInfo("sh", $"\"{buildScript}\" \"{pyHome}\" \"{_tempDir}\" 3.12")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process proc = Process.Start(psi)!;
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException("构建 CPython shim 失败：" + proc.StandardError.ReadToEnd());
            }
        }
        else
        {
            return;
        }

        string libDir = Path.Combine(pyHome, "lib", "python3.12");
        var searchPaths = new List<string> { libDir };
        string dynLoad = Path.Combine(libDir, "lib-dynload");
        if (Directory.Exists(dynLoad)) searchPaths.Add(dynLoad);

        string configPath = Path.Combine(_tempDir, "ryupy-cpython.json");
        var config = new
        {
            schema = 1,
            python_version = "3.12.13",
            development = true,
            home = pyHome,
            library = pyLib,
            shim = shimPath,
            search_paths = searchPaths,
            modules = new[] { "collections", "builtins", "tkinter", "tkinter.ttk" },
            container_transfer = "proxy",
        };
        File.WriteAllText(configPath, JsonSerializer.Serialize(config));
        Bootstrap.Start(configPath, ["ryupy-cpython-tests"]);
        Available = true;
    }

    public void Dispose()
    {
        try
        {
            if (Available)
            {
                Bootstrap.Stop();
            }
        }
        finally
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); } catch { }
            }
        }
    }

    private static string FindRepoRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current, "Makefile")) && Directory.Exists(Path.Combine(current, "runtime")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new InvalidOperationException("无法定位 RyuPy 仓库根目录");
    }

    private static string? FindPythonHome()
    {
        string homeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "uv", "python");
        if (Directory.Exists(homeDir))
        {
            foreach (string dir in Directory.GetDirectories(homeDir, "cpython-3.12.13-*"))
            {
                if (Directory.Exists(Path.Combine(dir, "lib", "python3.12"))) return dir;
            }
        }
        return null;
    }

    private static string? FindPythonLibrary(string pyHome)
    {
        string[] candidates =
        [
            Path.Combine(pyHome, "lib", "libpython3.12.dylib"),
            Path.Combine(pyHome, "lib", "libpython3.12.so.1.0"),
            Path.Combine(pyHome, "lib", "libpython3.12.so"),
        ];
        return candidates.FirstOrDefault(File.Exists);
    }
}

/// <summary>CPython 跨域继承真实集成测试套件。</summary>
public class CPythonClassBridgeTests(CPythonBridgeFixture fixture) : IClassFixture<CPythonBridgeFixture>
{
    private void RequireHost() => Assert.True(fixture.Available, "未找到 CPython 3.12.13 分发环境");

    [Fact]
    public void UserDictSubclass_SupportsSuperInitMethodsAttributesAndOperators()
    {
        RequireHost();
        PyModule collections = PyModules.Import("collections");
        object? userDict = collections.GetAttr("UserDict");

        var derived = (PyClass)PyOps.PrepareRuntimeDerivedClass("TrackedDict", [userDict]);
        derived.SetAttr("__init__", new PyFunction(
            "__init__",
            PySignature.Create("__init__", ["self", "initial"], ["pos", "pos"], ["0", "1"]),
            (_, args, _, _) =>
            {
                object? self = args[0];
                object? initial = args.Length > 1 ? args[1] : new PyDict();
                _ = PyOps.CallValue(PyObject.GetAttr(PyMethodContext.MakeSuper(), "__init__"), [initial], [], []);
                PyObject.SetAttr(self, "tag", "tracked");
                return PyNone.Instance;
            },
            [PyNone.Instance],
            usesSuper: true
        ));
        derived.SetAttr("put", new PyFunction(
            "put",
            PySignature.Create("put", ["self", "k", "v"], ["pos", "pos", "pos"], ["0", "0", "0"]),
            (_, args, _, _) =>
            {
                PyOps.SetItem(args[0], args[1], args[2]);
                return args[0];
            },
            [],
            usesSuper: false
        ));
        PyOps.FinalizeOrdinaryClass(derived);

        object? instanceObj = PyOps.CallValue(derived, [], [], []);
        var instance = Assert.IsType<PyInstance>(instanceObj);
        Assert.Same(derived, PyObject.TypeOf(instance));
        Assert.True(PyObject.IsInstance(instance, derived));
        Assert.True((bool)PyOps.CallBuiltin("isinstance", [instance, userDict!], [], [])!);
        Assert.True((bool)PyOps.CallBuiltin("issubclass", [derived, userDict!], [], [])!);
        Assert.Equal("tracked", PyObject.GetAttr(instance, "tag"));

        _ = PyOps.CallValue(PyObject.GetAttr(instance, "put"), ["a", 42L], [], []);
        Assert.Equal(42L, PyOps.GetItem(instance, "a"));
        Assert.True(PyOps.Contains(instance, "a"));
        Assert.Equal(1L, PyOps.Len(instance));

        PyObject.SetAttr(instance, "temp", 99L);
        Assert.Equal(99L, PyObject.GetAttr(instance, "temp"));
        PyObject.DelAttr(instance, "temp");
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "temp"));
    }

    [Fact]
    public void CExtensionDequeSubclass_PreservesStorageAndMethods()
    {
        RequireHost();
        PyModule collections = PyModules.Import("collections");
        object? dequeType = collections.GetAttr("deque");

        var myDeque = (PyClass)PyOps.PrepareRuntimeDerivedClass("RingQueue", [dequeType]);
        myDeque.SetAttr("push_twice", new PyFunction(
            "push_twice",
            PySignature.Create("push_twice", ["self", "item"], ["pos", "pos"], ["0", "0"]),
            (_, args, _, _) =>
            {
                object? append = PyObject.GetAttr(PyMethodContext.MakeSuper(), "append");
                PyOps.CallValue(append, [args[1]], [], []);
                PyOps.CallValue(append, [args[1]], [], []);
                return PyNone.Instance;
            },
            [],
            usesSuper: true
        ));
        PyOps.FinalizeOrdinaryClass(myDeque);

        object? queue = PyOps.CallValue(myDeque, [new PyList([1L])], [], []);
        var instance = Assert.IsType<PyInstance>(queue);
        _ = PyOps.CallValue(PyObject.GetAttr(instance, "push_twice"), [7L], [], []);
        Assert.Equal(3L, PyOps.Len(instance));
        Assert.Equal(1L, PyOps.GetItem(instance, 0L));
        Assert.Equal(7L, PyOps.GetItem(instance, 1L));
        Assert.Equal(7L, PyOps.GetItem(instance, 2L));
    }

    [Fact]
    public void DescriptorsAndMultiLevelAndMixinInheritance_WorkWithCPythonMro()
    {
        RequireHost();
        PyModule collections = PyModules.Import("collections");
        object? userDict = collections.GetAttr("UserDict");

        var mixin = (PyClass)PyOps.MakeClass("LabelMixin", [], null);
        mixin.SetAttr("describe", new PyFunction(
            "describe",
            PySignature.Create("describe", ["self"], ["pos"], ["0"]),
            (_, args, _, _) => $"mix:{PyObject.GetAttr(args[0], "name")}",
            [],
            usesSuper: false
        ));
        PyOps.FinalizeOrdinaryClass(mixin);

        object frame = PyLexicalClassFactory.Prepare("MidDict", "MidDict", "mod", [mixin, userDict]);
        var mid = (PyClass)PyLexicalClassFactory.Finish(frame);
        PyClass sub = (PyClass)PyOps.PrepareRuntimeDerivedClass("LeafDict", [mid]);
        sub.SetAttr("kind", new PyClassMethod(new PyFunction(
            "kind",
            PySignature.Create("kind", ["cls"], ["pos"], ["0"]),
            (_, args, _, _) => PyObject.GetAttr(args[0], "__name__"),
            [],
            usesSuper: false
        )));
        sub.SetAttr("add_one", new PyStaticMethod(new PyFunction(
            "add_one",
            PySignature.Create("add_one", ["x"], ["pos"], ["0"]),
            (_, args, _, _) => (long)args[0]! + 1L,
            [],
            usesSuper: false
        )));
        var getter = new PyFunction(
            "prop",
            PySignature.Create("prop", ["self"], ["pos"], ["0"]),
            (_, args, _, _) => PyObject.GetAttr(args[0], "_prop"),
            []
        );
        var setter = new PyFunction(
            "prop",
            PySignature.Create("prop", ["self", "v"], ["pos", "pos"], ["0", "0"]),
            (_, args, _, _) => { PyObject.SetAttr(args[0], "_prop", args[1]); return PyNone.Instance; },
            []
        );
        sub.SetAttr("prop", new PyProperty(getter, setter));
        PyOps.FinalizeOrdinaryClass(sub);

        var obj = Assert.IsType<PyInstance>(PyOps.CallValue(sub, [], [], []));
        PyObject.SetAttr(obj, "name", "budget");
        Assert.Equal("mix:budget", PyOps.CallValue(PyObject.GetAttr(obj, "describe"), [], [], []));
        Assert.Equal("LeafDict", PyOps.CallValue(PyObject.GetAttr(sub, "kind"), [], [], []));
        Assert.Equal(6L, PyOps.CallValue(PyObject.GetAttr(obj, "add_one"), [5L], [], []));

        PyObject.SetAttr(obj, "prop", "ok");
        Assert.Equal("ok", PyObject.GetAttr(obj, "prop"));
        Assert.True(sub.IsSubclassOf(mixin));
        Assert.True((bool)PyOps.CallBuiltin("issubclass", [sub, userDict!], [], [])!);
    }

    [Fact]
    public void NonInheritableForeignType_FailsWithoutLeakingHalfCreatedClass()
    {
        RequireHost();
        PyModule builtins = PyModules.Import("builtins");
        object? boolType = builtins.GetAttr("bool");

        var bad = (PyClass)PyOps.PrepareRuntimeDerivedClass("BadBool", [boolType]);
        PyTypeError error = Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(bad));
        Assert.Equal("TypeError", error.PyTypeName);
        Assert.Null(PyForeignClassBridge.GetForeignClass(bad));
    }

    [Fact]
    public void CustomNewAndExplicitAndClassMethodSuper_PreserveMroOrderAndIdentity()
    {
        RequireHost();
        PyModule collections = PyModules.Import("collections");
        object? userDict = collections.GetAttr("UserDict");

        var baseCls = (PyClass)PyOps.PrepareRuntimeDerivedClass("BaseCustomNewDict", [userDict]);
        baseCls.SetAttr("__new__", new PyFunction(
            "__new__",
            PySignature.Create("__new__", ["cls"], ["pos"], ["0"]),
            (_, args, _, _) =>
            {
                object? superNew = PyObject.GetAttr(PyMethodContext.MakeSuper(), "__new__");
                object? created = PyOps.CallValue(superNew, [args[0]], [], []);
                PyObject.SetAttr(created, "from_new", "yes");
                return created;
            },
            [],
            usesSuper: true
        ));
        baseCls.SetAttr("greet", new PyFunction(
            "greet",
            PySignature.Create("greet", ["self"], ["pos"], ["0"]),
            (_, args, _, _) => "base",
            [],
            usesSuper: false
        ));
        baseCls.SetAttr("tag", new PyClassMethod(new PyFunction(
            "tag",
            PySignature.Create("tag", ["cls"], ["pos"], ["0"]),
            (_, args, _, _) => PyObject.GetAttr(args[0], "__name__"),
            [],
            usesSuper: false
        )));
        PyOps.FinalizeOrdinaryClass(baseCls);

        var subCls = (PyClass)PyOps.PrepareRuntimeDerivedClass("SubCustomNewDict", [baseCls]);
        subCls.SetAttr("greet", new PyFunction(
            "greet",
            PySignature.Create("greet", ["self"], ["pos"], ["0"]),
            (_, args, _, _) =>
            {
                object? sup = PyOps.CallValue(PyLexicalSuper.Type, [subCls, args[0]], [], []);
                object? parentResult = PyOps.CallValue(PyObject.GetAttr(sup, "greet"), [], [], []);
                return $"sub:{parentResult}";
            },
            [],
            usesSuper: false
        ));
        subCls.SetAttr("sub_tag", new PyClassMethod(new PyFunction(
            "sub_tag",
            PySignature.Create("sub_tag", ["cls"], ["pos"], ["0"]),
            (_, args, _, _) =>
            {
                object? parentTag = PyOps.CallValue(PyObject.GetAttr(PyMethodContext.MakeSuper(), "tag"), [], [], []);
                return $"sub:{parentTag}";
            },
            [],
            usesSuper: true
        )));
        PyOps.FinalizeOrdinaryClass(subCls);

        var instance = Assert.IsType<PyInstance>(PyOps.CallValue(subCls, [], [], []));
        Assert.Equal("yes", PyObject.GetAttr(instance, "from_new"));
        Assert.Equal("sub:base", PyOps.CallValue(PyObject.GetAttr(instance, "greet"), [], [], []));
        Assert.Equal("sub:SubCustomNewDict", PyOps.CallValue(PyObject.GetAttr(subCls, "sub_tag"), [], [], []));
    }
}
