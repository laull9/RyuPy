// 真实实例状态冷合同；不代替Python编译/JIT/Native端到端验证。
using System;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyDirectGlobalsTests
{
    private static (PyModule Module, object Context) Create()
    {
        object? context = null;
        string name = "direct_globals_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "globals.py", 1,
            "/virtual/globals.py", false, state => context = state);
        var module = PyModuleInstances.Create(name);
        PyModuleInstances.Execute(module);
        return (module, context!);
    }
    [Fact]
    public void CanonicalCallReturnsOneRealMutableDictionary()
    {
        var (module, context) = Create();
        var dict = Assert.IsType<PyDict>(module.GetAttr("__dict__"));
        object? canonical = PyModuleInstances.LoadName(context, "globals");
        Assert.IsType<PyFunction>(canonical);
        Assert.Same(dict, PyModuleInstances.CallDirectGlobals(context, canonical));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(canonical, [], [], []));
        dict.Set("value", 7L);
        Assert.Equal(7L, PyModuleInstances.LoadName(context, "value"));
        dict.DeleteIfPresent("value");
        Assert.Throws<PyNameError>(() => PyModuleInstances.LoadName(context, "value"));
        Assert.NotSame(dict, Create().Module.GetAttr("__dict__"));
        Assert.Throws<PyTypeError>(() => PyModuleInstances.CallDirectGlobals(context, 42L));
    }
    [Fact]
    public void DefaultDeletionIsNameErrorAndStableIdentitySurvivesRebinding()
    {
        var (module, context) = Create();
        var builtins = Assert.IsType<PyDict>(module.GetAttr("__builtins__"));
        object? canonical = builtins.Get("globals");
        try
        {
            builtins.DeleteIfPresent("globals");
            Assert.Throws<PyNameError>(() => PyModuleInstances.LoadName(context, "globals"));
            builtins.Set("globals", 42L);
            Assert.Equal(42L, PyModuleInstances.LoadName(context, "globals"));
            Assert.Same(module.GetAttr("__dict__"), PyModuleInstances.CallDirectGlobals(context, canonical));
        }
        finally { builtins.Set("globals", canonical); }
    }
}
