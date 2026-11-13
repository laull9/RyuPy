// 使用公开模块接口；每例唯一名字，不覆盖真实标准库、不依赖Registry私有布局。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyDeclaredBindingModulesTests
{
    private static string Name() => "cabi_module_" + Guid.NewGuid().ToString("N");
    private static object? Missing(string key) => throw new PyAttributeError($"no attribute '{key}'");

    [Fact]
    public void AvailabilityIsReadOnlyAndRegistrationRechecksBeforeAnyWrite()
    {
        string name = Name();
        PyModules.EnsureDeclaredBindingAvailable(name);
        PyModules.EnsureDeclaredBindingAvailable(name);
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(name));
        var marker = new PyList([]);
        PyModules.RegisterNative(name, new Dictionary<string, object?> { ["marker"] = marker });
        int initialized = 0;
        Assert.Throws<PyImportError>(() => PyModules.RegisterDeclaredBinding(name,
            () => initialized++, Missing, PyModules.SetDynamic));
        Assert.Equal(0, initialized);
        PyModule original = PyModules.Import(name);
        Assert.Same(marker, original.GetAttr("marker"));
        Assert.Throws<PyImportError>(() => PyModules.EnsureDeclaredBindingAvailable(name));
        Assert.Throws<PyImportError>(() => PyModules.RegisterDeclaredBinding(name,
            () => initialized++, Missing, PyModules.SetDynamic));
        Assert.Same(original, PyModules.Import(name));
        Assert.Same(marker, original.GetAttr("marker"));
        Assert.Equal(0, initialized);
    }

    [Fact]
    public void NativeDescendantConflictsButAnUnrelatedPrefixDoesNot()
    {
        string parent = Name(), child = parent + ".child";
        var marker = new PyList([]);
        PyModules.RegisterNative(child, new Dictionary<string, object?> { ["marker"] = marker });
        Assert.Throws<PyImportError>(() => PyModules.EnsureDeclaredBindingAvailable(parent));
        Assert.Throws<PyImportError>(() => PyModules.RegisterDeclaredBinding(parent, () => { }, Missing, PyModules.SetDynamic));
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(parent));
        PyModules.RegisterNative(parent, new Dictionary<string, object?>());
        Assert.Same(marker, PyModules.Import(child).GetAttr("marker"));
        string available = Name();
        PyModules.RegisterNative(available + "_suffix", new Dictionary<string, object?>());
        PyModules.EnsureDeclaredBindingAvailable(available);
        PyModules.RegisterDeclaredBinding(available, () => { }, Missing, PyModules.SetDynamic);
        Assert.Equal(available, PyModules.Import(available).GetAttr("__name__"));
    }

    [Fact]
    public void GeneratedModuleHasRealIdentityAndMissingFileUntilExplicitAssignment()
    {
        string name = Name(); object? slot = null; int initialized = 0;
        PyModules.RegisterDeclaredBinding(name, () =>
        {
            var ready = PyNativeFunctions.Create("add", ["left", "right"], [null, null], 2,
                args => (long)args[0]! + (long)args[1]!, ["posonly", "posonly"]);
            slot = ready; initialized++;
        }, key => key == "add" ? slot : Missing(key),
            (module, key, value) => { if (key == "add") slot = value; else PyModules.SetDynamic(module, key, value); });
        PyModule module = PyModules.Import(name);
        Assert.Same(module, PyModules.Import(name)); Assert.Equal(1, initialized);
        Assert.Equal(name, module.GetAttr("__name__")); Assert.Equal("", module.GetAttr("__package__"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__file__"));
        object? original = module.GetAttr("add"); Assert.IsType<PyFunction>(original);
        Assert.Same(original, PyModules.Import(name).GetAttr("add"));
        var marker = new PyList([]);
        module.SetAttr("__file__", marker); Assert.Same(marker, module.GetAttr("__file__"));
        module.SetAttr("__file__", PyNone.Instance); Assert.Same(PyNone.Instance, module.GetAttr("__file__"));
        module.SetAttr("add", marker); Assert.Same(marker, module.GetAttr("add"));
        Assert.IsType<PyFunction>(original); // 已保存对象不被随后公开槽替换。
        string legacy = Name();
        PyModules.RegisterNative(legacy, new Dictionary<string, object?>());
        Assert.Equal("", PyModules.Import(legacy).GetAttr("__file__")); // 旧native语义不变。
    }

    [Fact]
    public void FailedInitializerDoesNotPublishSlotOrRetainCachedModule()
    {
        string name = Name(); var old = new PyList([]); object? slot = old; int attempts = 0;
        PyModules.RegisterDeclaredBinding(name, () =>
        {
            attempts++;
            if (attempts == 1) throw new PyValueError("construction failed before commit");
            object ready = PyNativeFunctions.Create("ready", [], [], 0, _ => 1L);
            slot = ready;
        }, key => key == "add" ? slot : Missing(key), PyModules.SetDynamic);
        Assert.Throws<PyValueError>(() => PyModules.Import(name));
        Assert.Same(old, slot);
        PyModule retried = PyModules.Import(name);
        Assert.Equal(2, attempts); Assert.IsType<PyFunction>(retried.GetAttr("add"));
        Assert.Same(retried, PyModules.Import(name)); Assert.Equal(2, attempts);
    }

    [Fact]
    public void SyntheticMetadataDefaultsOnlyMissingSpecAndLoaderAndPreservesActualAssignments()
    {
        string name = Name();
        PyModules.RegisterDeclaredBinding(name, () => { }, Missing, PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        Assert.Same(PyNone.Instance, module.GetAttr("__spec__"));
        Assert.Same(PyNone.Instance, module.GetAttr("__loader__"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__file__"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("unrelated"));
        object spec = new PyDict(), loader = new PyList([]);
        module.SetAttr("__spec__", spec); module.SetAttr("__loader__", loader);
        Assert.Same(spec, PyModules.Import(name).GetAttr("__spec__"));
        Assert.Same(loader, PyModules.Import(name).GetAttr("__loader__"));
        string explicitName = Name();
        PyModules.RegisterDeclaredBinding(explicitName, () => { }, key => key switch
        { "__spec__" => spec, "__loader__" => loader, _ => Missing(key) }, PyModules.SetDynamic);
        Assert.Same(spec, PyModules.Import(explicitName).GetAttr("__spec__"));
        Assert.Same(loader, PyModules.Import(explicitName).GetAttr("__loader__"));
    }

    [Fact]
    public void RegistrationConflictCapturesActualCallerAndKeepsOuterContext()
    {
        string name = Name(); PyModules.RegisterNative(name, new Dictionary<string, object?>());
        var outer = new PyValueError("outer"); object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("binding_main.py", "<module>");
        try
        {
            PyTraceback.SetLine(3);
            var error = Assert.Throws<PyImportError>(() => PyModules.EnsureDeclaredBindingAvailable(name));
            Assert.Same(outer, error.Context);
            Assert.Equal(new PyStackFrame("binding_main.py", "<module>", 3), error.Frames[0]);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }
}
