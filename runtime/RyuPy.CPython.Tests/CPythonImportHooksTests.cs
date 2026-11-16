// CPython importer 首次反向导入真实 Runtime 注册：规格纯查询、单次初始化、循环、重试与缓存权威。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    private sealed class ImportState(string name, bool package)
    {
        internal int Runs;
        internal Action? Body;
        internal readonly Dictionary<string, object?> Fields = new()
        {
            ["__name__"] = name, ["__package__"] = package ? name : name.Contains('.') ? name[..name.LastIndexOf('.')] : "",
            ["__spec__"] = PyNone.Instance, ["__loader__"] = PyNone.Instance,
        };
        internal void Register()
        {
            if (package) Fields["__path__"] = new PyList([]);
            PyModules.RegisterSourceLocated(name, package ? name : name.Contains('.') ? name[..name.LastIndexOf('.')] : "",
                name + ".py", 0, "", false, package,
                () => { Runs++; Fields["runs"] = (long)Runs; Body?.Invoke(); },
                key => Fields.TryGetValue(key, out object? value) ? value : throw new PyAttributeError(key),
                (_, key, value) => Fields[key] = value);
        }
    }

    [Fact]
    public void NativeImportFinder_SpecIsNativePureQueryAndEmptyPackageIsPackage()
    {
        RequireHost();
        PyModule helper = Bootstrap.Host.Import("bridge_fixture.native_import_contract");
        var state = new ImportState("c1_native_empty_package", true);
        state.Register();
        object? description = SuperCall(helper.GetAttr("describe"), "c1_native_empty_package");
        Assert.Equal(new[] { "ryupy", "c1_native_empty_package", "c1_native_empty_package", "True", "ryupy" }, ClassStrings(description));
        Assert.Equal(0, state.Runs);
        var loaded = Assert.IsType<PyModule>(SuperCall(helper.GetAttr("load"), "c1_native_empty_package"));
        Assert.Equal(1, state.Runs);
        Assert.Same(loaded, SuperCall(helper.GetAttr("load"), "c1_native_empty_package"));
    }

    [Fact]
    public void NativeImportLoader_ParentCycleAndRepeatedExecHaveSingleInitializer()
    {
        RequireHost();
        PyModule helper = Bootstrap.Host.Import("bridge_fixture.native_import_contract");
        var parent = new ImportState("c1_native_package", true);
        var child = new ImportState("c1_native_package.leaf", false);
        parent.Register(); child.Register();
        object? initializing = null;
        child.Body = () =>
        {
            Assert.Equal(1, parent.Runs);
            Assert.True(PyModules.TryCached("c1_native_package.leaf", out PyModule? current));
            initializing = current;
            Assert.Same(current, SuperCall(helper.GetAttr("load"), "c1_native_package.leaf"));
        };
        var loaded = Assert.IsType<PyModule>(SuperCall(helper.GetAttr("load"), "c1_native_package.leaf"));
        Assert.Same(initializing, loaded);
        Assert.Equal(1, child.Runs);
        Assert.Same(loaded, parent.Fields["leaf"]);
        SuperCall(helper.GetAttr("repeat_exec"), "c1_native_package.leaf", loaded);
        Assert.Equal(1, child.Runs);
        Assert.Same(loaded, SuperCall(helper.GetAttr("cache_overrides"), "c1_native_package.leaf"));
        Assert.Equal(1, child.Runs);
    }

    [Fact]
    public void NativeImportLoader_FailedInitializationCanRetryWithNewModule()
    {
        RequireHost();
        PyModule helper = Bootstrap.Host.Import("bridge_fixture.native_import_contract");
        var state = new ImportState("c1_native_retry", false);
        PyModule? failed = null;
        state.Body = () =>
        {
            if (state.Runs != 1) return;
            Assert.True(PyModules.TryCached("c1_native_retry", out failed));
            throw new PyRuntimeError("c1 native retry");
        };
        state.Register();
        Assert.Throws<PyRuntimeError>(() => SuperCall(helper.GetAttr("load"), "c1_native_retry"));
        Assert.False(PyModules.TryCached("c1_native_retry", out _));
        var loaded = Assert.IsType<PyModule>(SuperCall(helper.GetAttr("load"), "c1_native_retry"));
        Assert.Equal(2, state.Runs);
        Assert.NotSame(failed, loaded);
        Assert.Same(loaded, SuperCall(helper.GetAttr("load"), "c1_native_retry"));
    }
}
