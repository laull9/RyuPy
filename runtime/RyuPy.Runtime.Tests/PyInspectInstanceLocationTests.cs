// Instance反射回调的provider bundle来源合同：沿用严格路径解析，不接受旧诊断字符串或逃逸路径。
using System;
using System.IO;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyInspectInstanceLocationTests
{
    [Fact]
    public void FrozenBundleLocationRegistersRealInstanceAndKeepsItsGlobals()
    {
        string name = "inspect_instance_" + Guid.NewGuid().ToString("N");
        string relative = "python/ryupy_compiled/" + name + ".py";
        PyModules.RegisterInstanceSourceLocated(name, "", "diagnostic.py", 3, relative, false, _ => { });
        var module = PyModuleInstances.Create(name);
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "python", "ryupy_compiled", name + ".py"), module.GetAttr("__file__"));
        Assert.Same(module.GetAttr("__dict__"), module.GetAttr("__dict__"));
    }

    [Theory]
    [InlineData("python/ryupy_compiled/../escape.py")]
    [InlineData("python/other/escape.py")]
    [InlineData("/absolute/source.py")]
    public void FrozenBundleRejectsUntrustedMapping(string location) =>
        Assert.Throws<PyRuntimeError>(() => PyModules.RegisterInstanceSourceLocated("inspect_bad", "", "d.py", 3,
            location, false, _ => { }));

    [Fact]
    public void OrdinaryLegacyLocationStillCannotRegisterAnInstance() =>
        Assert.Throws<PyNotImplementedError>(() => PyModules.RegisterInstanceSourceLocated("inspect_legacy", "", "d.py", 0,
            "python/ryupy_compiled/unit.py", false, _ => { }));
}
