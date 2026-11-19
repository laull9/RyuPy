// 实例import只消费本次frame捕获的builtins；有限默认函数必须真实可调用，不能用哨兵冒充。
using System;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyModuleImportGuardTests
{
    private static PyModule Create(PyModuleExecute body)
    {
        string name = "guard_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "guard.py", 1, "/virtual/guard.py", false, body);
        return PyModuleInstances.Create(name);
    }
    [Fact]
    public void DefaultImportIsCallableAndReturnsTopModuleAfterLoadingChild()
    {
        var module = Create(_ => { }); var builtins = Assert.IsType<PyDict>(module.GetAttr("__builtins__"));
        object? importer = builtins.Get("__import__");
        string root = "builtin_import_" + Guid.NewGuid().ToString("N"); int count = 0;
        PyModules.Register(root, "", "", () => { }, _ => throw new PyAttributeError("missing"), PyModules.SetDynamic);
        PyModules.Register(root + ".child", root, "", () => count++, _ => throw new PyAttributeError("missing"), PyModules.SetDynamic);
        Assert.Same(PyModules.Import(root), PyOps.CallValue(importer, [root + ".child"], [], []));
        Assert.Equal(1, count);
        Assert.Same(PyModules.Import(root), PyOps.CallValue(importer,
            [root, 123L, new object(), new PyList([]), 0L], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(importer, [root, PyNone.Instance, PyNone.Instance, new PyTuple(["x"])], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(importer, [root, PyNone.Instance, PyNone.Instance, new PyTuple([]), 1L], [], []));
    }
    [Fact]
    public void MissingAndReplacedImportNeverExecuteHook()
    {
        var module = Create(PyModuleInstances.ImportGuard); var custom = new PyDict();
        module.SetAttr("__builtins__", custom);
        Assert.Equal("__import__ not found", Assert.Throws<PyImportError>(() => PyModuleInstances.Execute(module)).Message);
        bool called = false; custom.Set("__import__", PyNativeFunctions.CreateVariadic("hook", _ => { called = true; return null; }));
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Execute(module)); Assert.False(called);
    }
    [Fact]
    public void GuardUsesCapturedDictionaryDespiteGlobalsRebinding()
    {
        object? captured = null; var module = Create(context => captured = context);
        PyModuleInstances.Execute(module); module.SetAttr("__builtins__", new PyDict());
        PyModuleInstances.ImportGuard(captured!);
        PyModuleInstances.Execute(module);
        Assert.Throws<PyImportError>(() => PyModuleInstances.ImportGuard(captured!));
    }
    [Fact]
    public void InPlaceDefaultDeletionIsObservedDuringExecution()
    {
        var module = Create(context => {
            var builtins = Assert.IsType<PyDict>(PyModuleInstances.LoadName(context, "__builtins__"));
            object? saved = builtins.Get("__import__");
            try { builtins.Delete("__import__"); Assert.Throws<PyImportError>(() => PyModuleInstances.ImportGuard(context)); }
            finally { builtins.Set("__import__", saved); }
            PyModuleInstances.ImportGuard(context);
        });
        PyModuleInstances.Execute(module);
    }
}
