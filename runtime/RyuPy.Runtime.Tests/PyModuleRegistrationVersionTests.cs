// 旧十参资源入口不承诺metadata槽；冻结旧PE之外，再按公开C#接口锁定缺属性与副作用顺序。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleRegistrationVersionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyResourceEntryKeepsMissingMetadataAndResourceReads(bool globalFile)
    {
        string name = "legacy_spec_" + Guid.NewGuid().ToString("N"); var trace = new List<string>();
        PyModules.RegisterResourceLocated(name, name, "__init__.py", 1, "/virtual/legacy/__init__.py", globalFile, true,
            () => { }, key => { trace.Add(key); throw new PyAttributeError(key); }, PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__spec__"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__loader__"));
        trace.Clear();
        var path = Assert.IsType<PyPath>(PyImportlibResources.Files([module], [], []));
        Assert.Equal("/virtual/legacy", path.Value);
        Assert.Equal(new[] { "__spec__", "__loader__" }, trace);
        Assert.Throws<PyNotImplementedError>(() => PyModules.ModuleMetadata(name, "__spec__"));
    }

    [Theory]
    [InlineData("__spec__")]
    [InlineData("__loader__")]
    public void LegacyOverridesAreRejectedInOriginalGetterOrder(string overridden)
    {
        string name = "legacy_override_" + Guid.NewGuid().ToString("N"); var trace = new List<string>();
        PyModules.RegisterResourceLocated(name, name, "__init__.py", 1, "/virtual/legacy/__init__.py", true, true,
            () => { }, key => { trace.Add(key); return key == overridden ? PyNone.Instance : throw new PyAttributeError(key); },
            PyModules.SetDynamic);
        var module = PyModules.Import(name);
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([module], [], []));
        Assert.Equal(overridden == "__spec__" ? new[] { "__spec__" } : new[] { "__spec__", "__loader__" }, trace);
    }

    [Fact]
    public void LegacyGetterExceptionIsNotConvertedOrSwallowed()
    {
        string name = "legacy_error_" + Guid.NewGuid().ToString("N"); var trace = new List<string>();
        var error = new PyValueError("getter-error");
        PyModules.RegisterResourceLocated(name, name, "__init__.py", 1, "/virtual/legacy/__init__.py", true, true,
            () => { }, key => { trace.Add(key); throw error; }, PyModules.SetDynamic);
        Assert.Same(error, Assert.Throws<PyValueError>(() => PyImportlibResources.Files([PyModules.Import(name)], [], [])));
        Assert.Equal(new[] { "__spec__" }, trace);
    }
}
