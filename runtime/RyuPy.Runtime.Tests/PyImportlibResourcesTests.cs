// 文件包资源冷能力与公开Path协议；注册表/帧栈共享，全部串行，不把模拟定位当真实AOT执行。
using System;
using System.Collections.Generic;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyImportlibResourcesTests
{
    private static string Name() => "resource_test_" + Guid.NewGuid().ToString("N");
    private static PyPath Files(object anchor) => Assert.IsType<PyPath>(PyImportlibResources.Files([anchor], [], []));
    private static void Register(string name, string file, Action? init = null, Dictionary<string, object?>? attrs = null,
        bool package = true, long kind = 1)
    {
        var values = attrs ?? new();
        PyModules.RegisterResourceLocated(name, name, "__init__.py", kind, file, false, package,
            () => init?.Invoke(), key => values.TryGetValue(key, out object? value) ? value : throw new PyAttributeError(key),
            (_, key, value) => values[key] = value);
    }
    private static object? Call(object target, string method, params object?[] args) => PyOps.CallMethod(target, method, args, [], []);

    [Fact]
    public void NativeParentAndChildExposeTheSameRealFilesFunction()
    {
        PySystem.Initialize("resources-tests.py", []);
        PyNativeModules.Register();
        var child = PyModules.Import("importlib.resources"); var parent = PyModules.Import("importlib");
        Assert.Same(child, parent.GetAttr("resources"));
        Assert.Same(PyImportlibResources.CreateAttributes()["files"], child.GetAttr("files"));
        Assert.IsType<PyFunction>(parent.GetAttr("import_module"));
        Assert.Throws<PyNotImplementedError>(() => Files(PyModules.Import("sys")));
    }
    [Fact]
    public void StringAnchorImportsOnceAndObjectAnchorHasTheSameRoot()
    {
        string name = Name(), file = Path.GetFullPath("资源 & package/__init__.py"); int calls = 0;
        Register(name, file, () => calls++);
        Assert.Equal(Path.GetDirectoryName(file), Files(name).Value);
        var module = PyModules.Import(name);
        Assert.Equal(Files(name).Value, Files(module).Value); Assert.Equal(1, calls);
    }
    [Fact]
    public void InitializingModuleAlreadyHasItsResourceCapability()
    {
        string name = Name(), file = Path.GetFullPath("cycle/__init__.py"); string? observed = null;
        Register(name, file, () => observed = Files(PyModules.Import(name)).Value);
        var module = PyModules.Import(name);
        Assert.Equal(Path.GetDirectoryName(file), observed); Assert.Equal(observed, Files(module).Value);
    }
    [Fact]
    public void FailedAttemptKeepsItsOwnCapabilityAndRetryUsesNewRegistration()
    {
        string name = Name(), first = Path.GetFullPath("first/__init__.py"), second = Path.GetFullPath("second/__init__.py");
        PyModule? failed = null; var error = new PyValueError("init failed");
        Register(name, first, () => { failed = PyModules.Import(name); throw error; });
        Assert.Same(error, Assert.Throws<PyValueError>(() => Files(name)));
        Register(name, second);
        Assert.Equal(Path.GetDirectoryName(second), Files(name).Value);
        Assert.Equal(Path.GetDirectoryName(first), Files(failed!).Value);
    }
    [Fact]
    public void SuccessfulCachedObjectDoesNotSwitchRootAfterNameReregistration()
    {
        string name = Name(), first = Path.GetFullPath("cached/__init__.py");
        Register(name, first); var module = PyModules.Import(name);
        Register(name, Path.GetFullPath("replacement/__init__.py"));
        Assert.Same(module, PyModules.Import(name)); Assert.Equal(Path.GetDirectoryName(first), Files(module).Value);
    }
    [Fact]
    public void PublicFilePackageAndNameValuesDoNotChooseTheResourceRoot()
    {
        string name = Name(), file = Path.GetFullPath("trusted/__init__.py"); var attrs = new Dictionary<string, object?>();
        Register(name, file, attrs: attrs); var module = PyModules.Import(name);
        module.SetAttr("__file__", PyNone.Instance); module.SetAttr("__package__", new PyList([])); module.SetAttr("__name__", "other");
        Assert.Equal(Path.GetDirectoryName(file), Files(module).Value);
    }
    [Fact]
    public void ExposedSpecOrLoaderSlotsAreExplicitUnsupported()
    {
        foreach (string slot in new[] { "__spec__", "__loader__" })
        {
            string name = Name(); var attrs = new Dictionary<string, object?>();
            Register(name, Path.GetFullPath("metadata/__init__.py"), attrs: attrs); var module = PyModules.Import(name);
            Assert.IsType<PyPath>(Files(module)); attrs[slot] = PyNone.Instance;
            Assert.Throws<PyNotImplementedError>(() => Files(module));
            string dynamicName = Name(); Register(dynamicName, Path.GetFullPath("dynamic/__init__.py"));
            var dynamicModule = PyModules.Import(dynamicName);
            PyModules.SetDynamic(dynamicModule, slot, PyNone.Instance);
            Assert.Throws<PyNotImplementedError>(() => Files(dynamicModule));
        }
    }
    [Fact]
    public void MetadataGetterUnexpectedFailureIsNotSwallowed()
    {
        string name = Name(); var error = new PyLookupError("getter failed");
        PyModules.RegisterResourceLocated(name, name, "x", 1, Path.GetFullPath("getter/__init__.py"), false, true,
            () => { }, _ => throw error, (_, _, _) => { });
        Assert.Same(error, Assert.Throws<PyLookupError>(() => Files(name)));
    }
    [Fact]
    public void LegacyNonpackageAndOldLocatedHaveNoImplicitResourceCapability()
    {
        string legacy = Name(); Register(legacy, "ignored", kind: 0);
        Assert.Throws<PyNotImplementedError>(() => Files(legacy));
        string ordinary = Name(); Register(ordinary, Path.GetFullPath("ordinary.py"), package: false);
        Assert.Throws<PyNotImplementedError>(() => Files(ordinary));
        string old = Name(); PyModules.RegisterLocated(old, old, "x", 1, Path.GetFullPath("old/__init__.py"), false,
            () => { }, key => throw new PyAttributeError(key), (_, _, _) => { });
        Assert.Throws<PyNotImplementedError>(() => Files(old));
    }
    [Fact]
    public void VirtualAnchorDoesNotRequireTheInitFileToExist()
    {
        string name = Name(), file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "__init__.py");
        Assert.False(File.Exists(file)); Register(name, file);
        Assert.Equal(Path.GetDirectoryName(file), Files(name).Value);
    }
    [Fact]
    public void BinderSupportsAnchorKeywordAndKeepsLimitedDomainsExplicit()
    {
        string name = Name(); Register(name, Path.GetFullPath("binder/__init__.py"));
        Assert.IsType<PyPath>(PyImportlibResources.Files([], ["anchor"], [name]));
        Assert.Throws<PyTypeError>(() => PyImportlibResources.Files([name, name], [], []));
        Assert.Throws<PyTypeError>(() => PyImportlibResources.Files([name], ["anchor"], [name]));
        Assert.Throws<PyTypeError>(() => PyImportlibResources.Files([], ["anchor", "package"], [name, name]));
        Assert.Throws<PyTypeError>(() => PyImportlibResources.Files([], ["unknown"], [name]));
        var unknown = Assert.Throws<PyTypeError>(() => PyImportlibResources.Files([name, name, name], ["unknown"], [name]));
        Assert.Contains("unexpected keyword", unknown.Message);
        var duplicate = Assert.Throws<PyTypeError>(() => PyImportlibResources.Files([name, name, name], ["anchor"], [name]));
        Assert.Contains("multiple values", duplicate.Message);
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([], ["package"], [name]));
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([PyNone.Instance], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([42L], [], []));
        Assert.Throws<PyModuleNotFoundError>(() => Files(Name()));
    }
    [Fact]
    public void ExistingPathAndBinaryStreamReadSeekCloseAndMissingFileRemainReal()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ryupy_resources_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "data.bin"), [97, 98, 99]);
            string name = Name(); Register(name, Path.Combine(directory, "__init__.py"));
            var root = Files(name); var child = Assert.IsType<PyPath>(Call(root, "joinpath", "data.bin"));
            Assert.Equal("data.bin", PyObject.GetAttr(child, "name")); Assert.Equal(true, Call(child, "is_file"));
            object stream = Call(child, "open", "rb")!;
            try
            {
                Assert.Equal(new byte[] { 97 }, Assert.IsType<PyBytes>(Call(stream, "read", 1L)).Bytes);
                Assert.Equal(0L, Call(stream, "seek", 0L));
                Assert.Equal(new byte[] { 97, 98, 99 }, Assert.IsType<PyBytes>(Call(stream, "read")).Bytes);
            }
            finally { Call(stream, "close"); }
            Assert.Throws<PyValueError>(() => Call(stream, "read"));
            var missing = Call(root, "joinpath", "missing.bin")!;
            Assert.Throws<PyFileNotFoundError>(() => Call(missing, "open", "rb"));
        }
        finally { Directory.Delete(directory, true); }
    }
}
