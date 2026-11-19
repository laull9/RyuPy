// 无来源的新注册只提供前言初值，不扩文件资源/finder能力；真实槽、循环与重试保持同一合同。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyLocationlessModuleMetadataTests
{
    private static readonly string[] Fields = ["__spec__", "__loader__", "__cached__", "__path__"];
    private static string Unique() => "no_location_" + Guid.NewGuid().ToString("N");
    private static void Register(string name, bool package, Dictionary<string, object?> slots, Action? body = null)
    {
        PyModules.RegisterSourceLocated(name, package ? name : "", "diagnostic.py", 0, "ignored", true, package,
            () => {
                foreach (string key in Fields) slots[key] = PyModules.ModuleMetadata(name, key);
                body?.Invoke();
            }, key => slots.TryGetValue(key, out var value) && value is not PyUnbound
                ? value : throw new PyAttributeError(key), (_, key, value) => slots[key] = value);
    }
    private static object? Find(string name) => PyOps.CallValue(
        PyImportlibUtil.CreateAttributes()["find_spec"], [name], [], []);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialValuesAndSlotMutationDoNotInventSource(bool package)
    {
        string name = Unique(); var slots = new Dictionary<string, object?>();
        Register(name, package, slots);
        Assert.Throws<PyNotImplementedError>(() => Find(name));
        PyModule module = PyModules.Import(name);
        foreach (string key in Fields[..3]) Assert.Same(PyNone.Instance, module.GetAttr(key));
        if (package) Assert.Equal(0, Assert.IsType<PyList>(module.GetAttr("__path__")).Count);
        else Assert.Throws<PyAttributeError>(() => module.GetAttr("__path__"));
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([module], [], []));
        Assert.Throws<PyValueError>(() => Find(name));
        Assert.Throws<PyNotImplementedError>(() => Find(name + ".missing"));
        foreach (string key in Fields)
        {
            var marker = new PyList([]);
            module.SetAttr(key, marker); Assert.Same(marker, slots[key]);
            slots[key] = PyNone.Instance; Assert.Same(PyNone.Instance, module.GetAttr(key));
            slots[key] = PyUnbound.Instance;
            Assert.Throws<PyAttributeError>(() => module.GetAttr(key));
        }
    }

    [Fact]
    public void CircularPackageRetryCreatesFreshPathAndKeepsSuccessfulDependency()
    {
        string name = Unique(), child = name + ".child", dependency = Unique();
        var slots = new Dictionary<string, object?>();
        var childSlots = new Dictionary<string, object?>();
        PyModule? first = null; object? firstPath = null; PyModule? dep = null; int attempts = 0;
        Register(dependency, false, new());
        Register(child, false, childSlots);
        Register(name, true, slots, () => {
            PyModule current = PyModules.Import(name);
            Assert.Same(slots["__path__"], current.GetAttr("__path__"));
            dep ??= PyModules.Import(dependency);
            Assert.Same(dep, PyModules.Import(dependency));
            if (++attempts == 1)
            {
                first = current; firstPath = slots["__path__"];
                ((PyList)firstPath!).Append("not-a-source");
                throw new PyValueError("retry");
            }
        });
        Assert.Throws<PyValueError>(() => PyModules.Import(name));
        Assert.Throws<PyNotImplementedError>(() => PyModules.ModuleMetadata(name, "__path__"));
        PyModule second = PyModules.Import(name);
        Assert.NotSame(first, second); Assert.NotSame(firstPath, second.GetAttr("__path__"));
        Assert.Equal(0, Assert.IsType<PyList>(second.GetAttr("__path__")).Count);
        Assert.Throws<PyNotImplementedError>(() => Find(child));
        PyModule sub = PyModules.Import(child);
        Assert.Same(sub, second.GetAttr("child")); Assert.Equal(2, attempts);
    }

    [Fact]
    public void RealSourceLocationKeepsSpecsAndResourceCapability()
    {
        string name = Unique();
        string file = "/virtual/pkg/__init__.py";
        PyModules.RegisterSourceLocated(name, name, "__init__.py", 1, file, false, true,
            () => { }, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        object spec = module.GetAttr("__spec__")!;
        Assert.True((bool)PyObject.GetAttr(spec, "has_location")!);
        Assert.Same(module.GetAttr("__loader__"), PyObject.GetAttr(spec, "loader"));
        Assert.Same(module.GetAttr("__cached__"), PyObject.GetAttr(spec, "cached"));
        Assert.Same(module.GetAttr("__path__"), PyObject.GetAttr(spec, "submodule_search_locations"));
        Assert.IsType<PyPath>(PyImportlibResources.Files([module], [], []));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManualSourceRegistrationWithoutSlotsKeepsInitialFallback(bool package)
    {
        string name = Unique();
        PyModules.RegisterSourceLocated(name, "", "diagnostic.py", 0, "ignored", false, package,
            () => { }, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        Assert.Same(PyNone.Instance, module.GetAttr("__spec__"));
        if (package) Assert.Equal(0, Assert.IsType<PyList>(module.GetAttr("__path__")).Count);
        else Assert.Throws<PyAttributeError>(() => module.GetAttr("__path__"));
        module.SetAttr("__loader__", 23L); Assert.Equal(23L, module.GetAttr("__loader__"));
        Assert.Throws<PyNotImplementedError>(() => Find(name + ".child"));
    }

    [Fact]
    public void OldKindZeroAndUnregisteredNamesDoNotGainMetadata()
    {
        string name = Unique();
        PyModules.RegisterResourceLocated(name, name, "__init__.py", 0, "", true, true,
            () => { }, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__spec__"));
        Assert.Throws<PyNotImplementedError>(() => PyModules.ModuleMetadata(name, "__spec__"));
        Assert.Throws<PyNotImplementedError>(() => PyModules.ModuleMetadata(Unique(), "__spec__"));
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([module], [], []));
    }
}
