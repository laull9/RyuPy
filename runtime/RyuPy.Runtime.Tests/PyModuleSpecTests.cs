// ModuleSpec 使用公开 import/属性/调用协议测试，先证明缺失，避免按实现私有布局断言。
using System;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleSpecTests
{
    private static object Type()
    {
        PyNativeModules.Register();
        return PyModules.ImportAttr("importlib.machinery", "ModuleSpec")!;
    }
    private static object New(object? name, object? loader, string[]? keys = null, object?[]? values = null) =>
        PyOps.CallValue(Type(), [name, loader], keys ?? [], values ?? [])!;
    [Fact]
    public void RealTypeReferencesAndMutableProperties()
    {
        var name = new PyList([]); var loader = new PyList([]); var origin = new PyList([]); var state = new PyDict();
        object spec = New(name, loader, ["origin", "loader_state", "is_package"], [origin, state, true]);
        Assert.Same(Type(), PyObject.TypeOf(spec));
        Assert.Same(name, PyObject.GetAttr(spec, "name")); Assert.Same(loader, PyObject.GetAttr(spec, "loader"));
        Assert.Same(origin, PyObject.GetAttr(spec, "origin")); Assert.Same(state, PyObject.GetAttr(spec, "loader_state"));
        Assert.Same(name, PyObject.GetAttr(spec, "parent"));
        Assert.IsType<PyList>(PyObject.GetAttr(spec, "submodule_search_locations"));
        PyObject.SetAttr(spec, "name", "pkg.child"); Assert.Equal("pkg.child", PyObject.GetAttr(spec, "parent"));
        PyObject.SetAttr(spec, "submodule_search_locations", PyNone.Instance); Assert.Equal("pkg", PyObject.GetAttr(spec, "parent"));
        Assert.Equal(false, PyObject.GetAttr(spec, "has_location"));
        PyObject.SetAttr(spec, "has_location", new PyList([1L])); Assert.Equal(true, PyObject.GetAttr(spec, "has_location"));
        PyObject.SetAttr(spec, "cached", state); Assert.Same(state, PyObject.GetAttr(spec, "cached"));
    }
    [Fact]
    public void UserTruthRunsAfterBindingAndFailedSetterDoesNotWrite()
    {
        int calls = 0; bool fail = false;
        var cls = new PyClass("Truth");
        cls.SetAttr("__bool__", PyNativeFunctions.Create("__bool__", ["self"], [null], 1,
            _ => { calls++; if (fail) throw new PyValueError("truth"); return true; }));
        object truth = PyOps.CallValue(cls, [], [], [])!;
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Type(), ["x", PyNone.Instance, 1L], ["is_package"], [truth]));
        Assert.Equal(0, calls);
        object spec = New("x", PyNone.Instance, ["is_package"], [truth]); Assert.Equal(1, calls);
        PyObject.SetAttr(spec, "has_location", truth); Assert.Equal(2, calls);
        fail = true;
        Assert.Throws<PyValueError>(() => PyObject.SetAttr(spec, "has_location", truth));
        Assert.Equal(3, calls); Assert.Equal(true, PyObject.GetAttr(spec, "has_location"));
    }
    [Fact]
    public void CachedNoneRecomputesAndBytecodePreservesOriginReference()
    {
        object spec = New("x", PyNone.Instance, ["origin"], ["/missing/no-suffix"]);
        PyObject.SetAttr(spec, "has_location", true);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(spec, "cached"));
        PyObject.SetAttr(spec, "origin", "/missing/next.py");
        Assert.Equal("/missing/__pycache__/next.cpython-312.pyc", PyObject.GetAttr(spec, "cached"));
        object origin = new string("/missing/ready.pyc".ToCharArray());
        PyObject.SetAttr(spec, "origin", origin); PyObject.SetAttr(spec, "cached", PyNone.Instance);
        Assert.Same(origin, PyObject.GetAttr(spec, "cached"));
        PyObject.SetAttr(spec, "cached", PyNone.Instance); PyObject.SetAttr(spec, "origin", new PyList([]));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(spec, "cached"));
    }
    [Fact]
    public void RepresentationEqualityAndHashFollowThePublicObjectProtocol()
    {
        object spec = New("p.m", PyNone.Instance, ["origin", "is_package"], ["source.py", true]);
        object other = New("p.m", PyNone.Instance, ["origin", "is_package", "loader_state"], ["source.py", true, new PyList([])]);
        Assert.Equal("ModuleSpec(name='p.m', loader=None, origin='source.py', submodule_search_locations=[])", PyRepr.Repr(spec));
        Assert.True(PyOps.Compare(PyOps.Eq, spec, other));
        PyObject.SetAttr(other, "has_location", true); Assert.False(PyOps.Compare(PyOps.Eq, spec, other));
        Assert.Throws<PyTypeError>(() => PyHash.Of(spec));
        PyObject.DelAttr(other, "name");
        Assert.Same(PyNotImplemented.Instance, PyOps.CallMethod(other, "__eq__", [spec], [], []));
        PyObject.DelAttr(other, "submodule_search_locations");
        Assert.Throws<PyAttributeError>(() => PyOps.CallMethod(other, "__eq__", [spec], [], []));
    }
    [Fact]
    public void ExactObjectLoaderUsesIdentityWithoutBeingClassifiedAsCustomEquality()
    {
        object marker = PyBaseObject.Create([], []), distinct = PyBaseObject.Create([], []);
        object noneSpec = New("x", PyNone.Instance), markerSpec = New("x", marker);
        Assert.False(PyOps.Compare(PyOps.Eq, noneSpec, markerSpec));
        Assert.False(PyOps.Compare(PyOps.Eq, markerSpec, noneSpec));
        Assert.True(PyOps.Compare(PyOps.Eq, markerSpec, New("x", marker)));
        Assert.False(PyOps.Compare(PyOps.Eq, markerSpec, New("x", distinct)));
        var custom = new PyClass("CustomEquality");
        int calls = 0;
        custom.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2,
            _ => { calls++; return true; }));
        object value = PyOps.CallValue(custom, [], [], [])!;
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, markerSpec, New("x", value)));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, New("x", value), markerSpec));
        Assert.Equal(0, calls);
    }
    [Fact]
    public void UnsupportedCustomEqualityIsRejectedOnlyWhenItsFieldIsConsumed()
    {
        int comparisons = 0;
        var cls = new PyClass("CustomEquality");
        cls.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2,
            _ => { comparisons++; return new PyList([]); }));
        object custom = PyOps.CallValue(cls, [], [], [])!;
        object left = New("left", custom), right = New("right", custom);
        Assert.False(PyOps.Compare(PyOps.Eq, left, right)); Assert.Equal(0, comparisons);
        PyObject.SetAttr(right, "name", "left");
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, left, right));
        Assert.Equal(0, comparisons);
    }
    [Theory]
    [InlineData("mod.py", "__pycache__/mod.cpython-312.pyc")]
    [InlineData("/mod.py", "__pycache__/mod.cpython-312.pyc")]
    [InlineData("//mod.py", "/__pycache__/mod.cpython-312.pyc")]
    [InlineData("/a///mod.py", "/a/__pycache__/mod.cpython-312.pyc")]
    [InlineData("/a/.py", "/a/__pycache__/py.cpython-312.pyc")]
    [InlineData("/a/a.b.py", "/a/__pycache__/a.b.cpython-312.pyc")]
    public void PosixCachePathUsesFixedCpythonSplitting(string origin, string expected)
    {
        if (OperatingSystem.IsWindows()) return;
        object spec = New("x", PyNone.Instance, ["origin"], [origin]);
        PyObject.SetAttr(spec, "has_location", true); Assert.Equal(expected, PyObject.GetAttr(spec, "cached"));
    }
    [Fact]
    public void BindingIsBeforeTruthAndCachedIsLazy()
    {
        object type = Type();
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, ["x", PyNone.Instance, "bad"], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, ["x", PyNone.Instance], ["name"], ["other"]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, [], [], []));
        object spec = New("pkg.mod", PyNone.Instance, ["origin"], ["/abs/pkg/mod.py"]);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(spec, "cached"));
        PyObject.SetAttr(spec, "has_location", true);
        Assert.Equal("/abs/pkg/__pycache__/mod.cpython-312.pyc", PyObject.GetAttr(spec, "cached"));
        PyObject.SetAttr(spec, "origin", "/other.py");
        Assert.Equal("/abs/pkg/__pycache__/mod.cpython-312.pyc", PyObject.GetAttr(spec, "cached"));
    }
}

[Collection("生成器帧栈")]
public class PyRegisteredModuleSpecTests
{
    [Fact]
    public void RegisteredSourcePackageHasOneSpecBeforeBodyAndRetryCreatesAnother()
    {
        string name = "spec_test_" + Guid.NewGuid().ToString("N");
        string file = "/virtual/" + name + "/__init__.py";
        var attrs = new System.Collections.Generic.Dictionary<string, object?>();
        PyModule? first = null; object? firstSpec = null; int attempts = 0;
        PyModules.RegisterSourceLocated(name, name, "__init__.py", 1, file, false, true,
            () => {
                var module = PyModules.Import(name);
                object spec = module.GetAttr("__spec__")!;
                Assert.Same(module.GetAttr("__loader__"), PyObject.GetAttr(spec, "loader"));
                Assert.Same(module.GetAttr("__path__"), PyObject.GetAttr(spec, "submodule_search_locations"));
                Assert.Equal(file, PyObject.GetAttr(spec, "origin"));
                Assert.Equal(name, PyObject.GetAttr(spec, "parent"));
                if (++attempts == 1) { first = module; firstSpec = spec; throw new PyValueError("first"); }
            }, key => attrs.TryGetValue(key, out object? value) ? value : throw new PyAttributeError(key),
            (_, key, value) => attrs[key] = value);
        Assert.Throws<PyValueError>(() => PyModules.Import(name));
        PyModule second = PyModules.Import(name);
        Assert.NotSame(first, second); Assert.NotSame(firstSpec, second.GetAttr("__spec__"));
        Assert.Same(second, PyModules.Import(name)); Assert.Equal(2, attempts);
        Assert.Equal("/virtual/" + name + "/__pycache__/__init__.cpython-312.pyc", second.GetAttr("__cached__"));
        second.SetAttr("__spec__", PyNone.Instance); Assert.Same(PyNone.Instance, second.GetAttr("__spec__"));
    }
    [Fact]
    public void GeneratedMetadataSlotsAreTheSingleReadableAndWritableState()
    {
        string name = "spec_slots_" + Guid.NewGuid().ToString("N");
        var slots = new System.Collections.Generic.Dictionary<string, object?>();
        PyModules.RegisterSourceLocated(name, "", "unit.py", 1, "/virtual/unit.py", true, false,
            () => {
                foreach (string key in new[] { "__spec__", "__loader__", "__cached__", "__path__" })
                    slots[key] = PyModules.ModuleMetadata(name, key);
                slots["__name__"] = name; slots["__package__"] = ""; slots["__file__"] = PyModules.ModuleFile(name);
            }, key => slots.TryGetValue(key, out object? value) && value is not PyUnbound ? value : throw new PyAttributeError(key),
            (_, key, value) => slots[key] = value);
        PyModule module = PyModules.Import(name);
        Assert.Same(slots["__spec__"], module.GetAttr("__spec__"));
        Assert.Same(module.GetAttr("__file__"), PyObject.GetAttr(slots["__spec__"], "origin"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__path__"));
        var marker = new PyList([]);
        foreach (string key in new[] { "__spec__", "__loader__", "__cached__", "__path__", "__name__", "__package__" })
        {
            module.SetAttr(key, marker); Assert.Same(marker, slots[key]);
            slots[key] = PyNone.Instance; Assert.Same(PyNone.Instance, module.GetAttr(key));
        }
    }
    [Fact]
    public void ResourceSpecMutationIsRejectedAndLoaderDoesNotClaimExecution()
    {
        string name = "spec_resource_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterSourceLocated(name, name, "__init__.py", 1, "/virtual/__init__.py", false, true,
            () => { }, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        object spec = module.GetAttr("__spec__")!; object loader = module.GetAttr("__loader__")!;
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallMethod(loader, "exec_module", [module], [], []));
        Assert.Same(PyNone.Instance, PyOps.CallMethod(loader, "create_module", [spec], [], []));
        Assert.IsType<PyPath>(PyImportlibResources.Files([module], [], []));
        PyObject.SetAttr(spec, "origin", "/replacement.py");
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.Files([module], [], []));
    }

}
