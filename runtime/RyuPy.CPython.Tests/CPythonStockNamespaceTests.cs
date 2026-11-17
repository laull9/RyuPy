// 原版解释器权威namespace测试；不安装strict内核，不使用容器镜像或永久托管根。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    [Fact]
    public void StockNamespace_UsesActualDictionaryAndPreservesExternalWritesDeletesAndAliases()
    {
        RequireHost();
        var host = Bootstrap.Host;
        PyModule helper = host.Import("bridge_fixture.live_module_dict");
        var module = Assert.IsType<PyModule>(SuperCall(helper.GetAttr("namespace_fixture")));
        IPyModuleNamespace storage = host.StockModuleNamespace(module);
        Assert.Same(storage, host.StockModuleNamespace(module));
        Assert.Same(storage.ViewObject, SuperCall(helper.GetAttr("raw_namespace"), module));
        Assert.Same(storage, host.StockModuleNamespace(storage.ViewObject));
        Assert.False(storage.TryGet("missing", out _));
        storage.Set("answer", PyNone.Instance);
        Assert.True(storage.TryGet("answer", out object? none));
        Assert.Same(PyNone.Instance, none);
        SuperCall(helper.GetAttr("external_set"), module, "answer", 41L);
        Assert.True(storage.TryGet("answer", out object? answer));
        Assert.Equal(41L, PyCast.ToInt64(answer));
        storage.Set("answer", 42L);
        Assert.Equal(42L, PyCast.ToInt64(SuperCall(helper.GetAttr("external_get"), module, "answer")));
        storage.Set("中文\0键", 99L);
        Assert.True(storage.TryGet("中文\0键", out object? unicode));
        Assert.Equal(99L, PyCast.ToInt64(unicode));
        Assert.Equal(99L, PyCast.ToInt64(SuperCall(helper.GetAttr("external_get"), module, "中文\0键")));
        Assert.True(storage.DeleteIfPresent("中文\0键"));
        var alias = new PyList([]);
        storage.Set("alias", alias);
        Assert.Same(alias, SuperCall(helper.GetAttr("external_get"), module, "alias"));
        Assert.True(storage.DeleteIfPresent("answer"));
        Assert.False(storage.DeleteIfPresent("answer"));
        storage.Set("answer", 43L);
        SuperCall(helper.GetAttr("external_delete"), module, "answer");
        Assert.False(storage.TryGet("answer", out _));
        storage.DeleteIfPresent("alias");
        host.ReleaseObject(storage.ViewObject);
        host.ReleaseObject(module);
        host.DrainReferences();
    }

    [Fact]
    public void StockNamespace_BoundRuntimeFunctionReadsTheSameDictionaryWithoutPublishingManagedOwner()
    {
        RequireHost();
        var host = Bootstrap.Host;
        PyModule helper = host.Import("bridge_fixture.live_module_dict");
        var foreign = Assert.IsType<PyModule>(SuperCall(helper.GetAttr("namespace_fixture")));
        var storage = host.StockModuleNamespace(foreign);
        storage.Set("__builtins__", PyModuleInstances.CreateCanonicalBuiltins());
        object? context = null;
        string name = "stock_namespace_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "stock_namespace.py", 1, "/virtual/stock_namespace.py", false, state => context = state);
        PyModule bound = PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), storage, foreign.ForeignObject!);
        PyModuleInstances.ExecuteBound(bound);
        var function = PyModuleInstances.CreateOwnedFunction(context!, "read", PySignature.Create("read", [], [], []),
            static (state, _, _, _, _) => PyModuleInstances.LoadName(state, "answer"), [], false, null);
        Assert.Same(storage.ViewObject, bound.GetAttr("__dict__"));
        Assert.Same(storage.ViewObject, PyObject.GetAttr(function, "__globals__"));
        SuperCall(helper.GetAttr("external_set"), foreign, "answer", 71L);
        Assert.Equal(71L, PyCast.ToInt64(SuperCall(function)));
        bound.SetAttr("answer", 72L);
        Assert.Equal(72L, PyCast.ToInt64(SuperCall(helper.GetAttr("external_get"), foreign, "answer")));
        storage.DeleteIfPresent("answer");
        Assert.Throws<PyNameError>(() => SuperCall(function));
        // 此门只验证不导出function的有向所有权；导出闭环由独立生命周期门验收。
        storage.DeleteIfPresent("__builtins__");
        host.ReleaseObject(storage.ViewObject);
        host.ReleaseObject(foreign);
        host.DrainReferences();
    }
}
