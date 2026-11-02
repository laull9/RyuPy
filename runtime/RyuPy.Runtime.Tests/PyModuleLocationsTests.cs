// 冷位置解析与声明的 __file__ 全局槽：不读资源、不触导入、不用可变属性重新定位。
using System;
using System.Collections.Generic;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleLocationsTests
{
    private static string Name() => "location_test_" + Guid.NewGuid().ToString("N");

    [Fact]
    public void PureResolverKeepsSourceIdentityAndUsesExecutableNotCurrentDirectory()
    {
        string source = Path.GetFullPath("中文 & fixture/module.py");
        Assert.Same(source, PyModuleLocations.ResolveForPlatform(1, source, true, null));
        Assert.Equal("module.py", PyModuleLocations.ResolveForPlatform(0, "module.py", true, null));
        string executable = Path.GetFullPath("bundle/program");
        string relative = "_ryupy_resources/v1/modules/pkg/module.py";
        string expected = Path.Combine(Path.GetDirectoryName(executable)!, "_ryupy_resources", "v1", "modules", "pkg", "module.py");
        Assert.Equal(expected, PyModuleLocations.ResolveForPlatform(2, relative, false, executable));
        Assert.Throws<PyNotImplementedError>(() => PyModuleLocations.ResolveForPlatform(2, relative, true, executable));
        Assert.Throws<PyNotImplementedError>(() => PyModuleLocations.ResolveForPlatform(2, relative, false, null));
        Assert.Throws<PyRuntimeError>(() => PyModuleLocations.ResolveForPlatform(1, "relative.py", true, null));
    }

    [Theory]
    [InlineData("/absolute")]
    [InlineData("_ryupy_resources/v1/../secret")]
    [InlineData("_ryupy_resources/v1/a//b")]
    [InlineData("_ryupy_resources/v1/C:x")]
    [InlineData("_ryupy_resources/v1/a\\b")]
    [InlineData("_ryupy_resources/v1/a. ")]
    [InlineData("_ryupy_resources/v1/中文.py")]
    [InlineData("_ryupy_resources/v1/CON.py")]
    [InlineData("_ryupy_resources/v1/nul")]
    [InlineData("_ryupy_resources/v1/COM1.txt")]
    [InlineData("_ryupy_resources/v1/LPT9")]
    [InlineData("_ryupy_resources/v1/CONIN$")]
    [InlineData("_ryupy_resources/v1/CONOUT$")]
    [InlineData("_ryupy_resources/v1/CLOCK$")]
    public void ExecutableRelativeLocationRejectsUnsafeComponents(string value)
    {
        Assert.Throws<PyRuntimeError>(() => PyModuleLocations.ResolveForPlatform(2, value, false, Path.GetFullPath("app")));
    }

    [Fact]
    public void InitialValueIsSharedButLiveGlobalRebindingPreservesArbitraryObjectIdentity()
    {
        string name = Name(), diagnostic = name + ".py", source = Path.GetFullPath(diagnostic);
        object? slot = null; bool assigned = false; PyModule? during = null;
        PyModules.RegisterLocated(name, "", diagnostic, 1, source, true,
            () =>
            {
                during = PyModules.Import(name);
                Assert.Throws<PyAttributeError>(() => during.GetAttr("__file__"));
                slot = PyModules.ModuleFile(name); assigned = true;
                Assert.Same(during.File, slot);
            },
            key => key == "__file__" && assigned ? slot : throw new PyAttributeError("unbound"),
            (module, key, value) => { if (key == "__file__") slot = value; else PyModules.SetDynamic(module, key, value); });
        var imported = PyModules.Import(name);
        Assert.Same(imported, during); Assert.Same(source, imported.GetAttr("__file__"));
        Assert.Equal(name, PyModules.ModuleForFile(diagnostic));
        Assert.Null(PyModules.ModuleForFile(source));
        slot = "inside"; Assert.Equal("inside", imported.GetAttr("__file__"));
        var marker = new PyList([]); imported.SetAttr("__file__", marker);
        Assert.Same(marker, slot); Assert.Same(marker, imported.GetAttr("__file__"));
        imported.SetAttr("__file__", PyNone.Instance); Assert.Same(PyNone.Instance, slot);
        Assert.Same(PyNone.Instance, imported.GetAttr("__file__"));
        Assert.Same(source, PyModules.ModuleFile(name));
        PyModules.SetDynamic(imported, "__file__", marker);
        Assert.Same(marker, imported.GetAttr("__file__"));
        Assert.Same(PyNone.Instance, slot);
    }

    [Fact]
    public void MissingCacheAndGetterErrorsAreNotHiddenByImportOrInitialFileFallback()
    {
        string name = Name(); int entered = 0;
        var error = new PyValueError("getter failure");
        PyModules.RegisterLocated(name, "", "display.py", 0, "display.py", true,
            () => entered++, _ => throw error, (_, _, _) => { });
        Assert.Throws<PyRuntimeError>(() => PyModules.ModuleFile(name)); Assert.Equal(0, entered);
        var module = PyModules.Import(name); Assert.Equal(1, entered);
        Assert.Same(error, Assert.Throws<PyValueError>(() => module.GetAttr("__file__")));
        Assert.Equal("display.py", PyModules.ModuleFile(name));
        string legacy = Name();
        PyModules.Register(legacy, "", "legacy.py", () => { }, _ => throw error, (_, _, _) => { });
        Assert.Equal("legacy.py", PyModules.Import(legacy).GetAttr("__file__"));
    }
}
