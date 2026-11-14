// find_spec 先以公开模块、调用和属性入口锁定查询副作用；不访问实现私有注册表。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyImportlibUtilTests
{
    private static object Function()
    {
        PyNativeModules.Register();
        return PyModules.ImportAttr("importlib.util", "find_spec")!;
    }

    private static object? Find(object? name, object? package = null) =>
        PyOps.CallValue(Function(), [name, package ?? PyNone.Instance], [], []);

    private static string Unique() => "find_spec_" + Guid.NewGuid().ToString("N");

    private static Dictionary<string, object?> Register(string name, bool package, Action? initialize = null)
    {
        var fields = new Dictionary<string, object?>();
        string file = "/virtual/" + name.Replace('.', '/') + (package ? "/__init__.py" : ".py");
        string parent = package ? name : name.Contains('.') ? name[..name.LastIndexOf('.')] : "";
        PyModules.RegisterSourceLocated(name, parent, file, 1, file, true, package,
            () =>
            {
                foreach (string attribute in new[] { "__spec__", "__loader__", "__cached__", "__path__" })
                    fields[attribute] = PyModules.ModuleMetadata(name, attribute);
                initialize?.Invoke();
            }, key => fields.TryGetValue(key, out object? value) && value is not PyUnbound
                ? value : throw new PyAttributeError($"module '{name}' has no attribute '{key}'"),
            (_, key, value) => fields[key] = value);
        return fields;
    }

    [Fact]
    public void StableFunctionIdentityAndContainerAliasUseTheSameSignature()
    {
        object function = Function();
        Assert.Same(function, Function());
        Assert.Same(function, PyModules.Import("importlib.util").GetAttr("find_spec"));
        string name = Unique(); Register(name, false);
        var aliases = new PyList([function]);
        object? spec = PyOps.CallValue(PyOps.GetItem(aliases, 0L), [], ["name"], [name]);
        Assert.Equal(name, PyObject.GetAttr(spec, "name"));
        Assert.Same(PyImportlibUtil.CreateAttributes()["module_from_spec"],
            PyModules.Import("importlib.util").GetAttr("module_from_spec"));
    }

    [Fact]
    public void FindingTopLevelDoesNotRunOrCacheTargetAndReturnsFreshEquivalentSpecs()
    {
        string name = Unique(); int executions = 0; Register(name, false, () => executions++);
        object first = Find(name)!; object second = Find(name)!;
        Assert.Equal(0, executions); Assert.NotSame(first, second);
        Assert.True(PyOps.Compare(PyOps.Eq, first, second));
        Assert.NotSame(PyObject.GetAttr(first, "loader"), PyObject.GetAttr(second, "loader"));
        Assert.Equal(name, PyObject.GetAttr(first, "name"));
        Assert.Equal("/virtual/" + name + ".py", PyObject.GetAttr(first, "origin"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(first, "submodule_search_locations"));
        PyModule imported = PyModules.Import(name);
        Assert.Equal(1, executions); Assert.NotSame(first, imported.GetAttr("__spec__"));
        Assert.Same(imported.GetAttr("__spec__"), Find(name));
    }

    [Fact]
    public void ParentRunsBeforeLookupButTargetDoesNotRunOrBindToParent()
    {
        string parent = Unique(), child = parent + ".child";
        var trace = new List<string>(); Register(parent, true, () => trace.Add("parent"));
        Register(child, false, () => trace.Add("child"));
        object spec = Find(child)!;
        Assert.Equal(new[] { "parent" }, trace);
        Assert.Equal(parent, PyObject.GetAttr(spec, "parent"));
        Assert.Throws<PyAttributeError>(() => PyModules.Import(parent).GetAttr("child"));
        PyModules.Import(child); Assert.Equal(new[] { "parent", "child" }, trace);
    }

    [Fact]
    public void PackageDiscoveryAllocatesIndependentListsWithoutRunningThePackage()
    {
        string name = Unique(); int executions = 0; Register(name, true, () => executions++);
        object first = Find(name)!, second = Find(name)!;
        var path = Assert.IsType<PyList>(PyObject.GetAttr(first, "submodule_search_locations"));
        Assert.NotSame(path, PyObject.GetAttr(second, "submodule_search_locations"));
        Assert.Equal("/virtual/" + name, path.GetItem(0L));
        Assert.Equal(name, PyObject.GetAttr(first, "parent")); Assert.Equal(0, executions);
        path.Append("changed"); Assert.False(PyOps.Compare(PyOps.Eq, first, second));
    }

    [Fact]
    public void ParentImportingTargetStillReturnsFreshFinderSpec()
    {
        string parent = Unique(), child = parent + ".child";
        object marker = new PyList([]); int targetRuns = 0;
        Register(parent, true, () => PyModules.Import(child).SetAttr("__spec__", marker));
        Register(child, false, () => targetRuns++);
        object result = Find(child)!;
        Assert.Equal(1, targetRuns); Assert.NotSame(marker, result);
        Assert.Equal(child, PyObject.GetAttr(result, "name")); Assert.Same(marker, Find(child));
    }

    [Fact]
    public void CachedSpecReturnsCurrentArbitraryValueAndRelativeErrorsKeepOriginalName()
    {
        string parent = Unique(), child = parent + ".child";
        Register(parent, true); var fields = Register(child, false);
        PyModule module = PyModules.Import(child); object marker = new PyDict();
        module.SetAttr("__spec__", marker); Assert.Same(marker, Find(".child", parent));
        module.SetAttr("__spec__", PyNone.Instance);
        var none = Assert.Throws<PyValueError>(() => Find(".child", parent));
        Assert.Equal(".child.__spec__ is None", none.Message); Assert.False(none.SuppressContextFlag);
        fields.Remove("__spec__");
        var missing = Assert.Throws<PyValueError>(() => Find(".child", parent));
        Assert.Equal(".child.__spec__ is not set", missing.Message);
        Assert.True(missing.SuppressContextFlag); Assert.Null(missing.Cause);
        Assert.IsType<PyAttributeError>(missing.Context);
    }

    [Fact]
    public void CachedGetterErrorsOtherThanAttributeErrorKeepTheirIdentity()
    {
        string name = Unique(); var error = new PyRuntimeError("spec getter");
        PyModules.RegisterSourceLocated(name, "", "m.py", 1, "/virtual/m.py", true, false,
            () => { }, key => key == "__spec__" ? throw error : throw new PyAttributeError(key), PyModules.SetDynamic);
        PyModules.Import(name);
        Assert.Same(error, Assert.Throws<PyRuntimeError>(() => Find(name)));
    }

    [Fact]
    public void RelativeNamesResolveAndAbsoluteNamesIgnorePackageObject()
    {
        string parent = Unique(), child = parent + ".child";
        Register(parent, true); Register(child, false);
        Assert.Equal(child, PyObject.GetAttr(Find(".child", parent), "name"));
        Assert.Equal(parent, PyObject.GetAttr(Find(".", parent), "name"));
        Assert.Equal(child, PyObject.GetAttr(Find("..child", parent + ".inner"), "name"));
        Assert.Equal(child, PyObject.GetAttr(Find(child, new PyList([])), "name"));
        Assert.Equal("no package specified for '.child' (required for relative module names)",
            Assert.Throws<PyImportError>(() => Find(".child")).Message);
        Assert.Equal("attempted relative import beyond top-level package",
            Assert.Throws<PyImportError>(() => Find("..child", parent)).Message);
    }

    [Fact]
    public void NonPackageParentHasExactNameAndAttributeErrorCause()
    {
        string parent = Unique(), child = parent + ".child"; Register(parent, false);
        var error = Assert.Throws<PyModuleNotFoundError>(() => Find(child));
        Assert.Equal($"__path__ attribute not found on '{parent}' while trying to find '{child}'", error.Message);
        Assert.Equal(child, PyObject.GetAttr(error, "name"));
        Assert.IsType<PyAttributeError>(error.Cause); Assert.Same(error.Cause, error.Context);
        Assert.True(error.SuppressContextFlag);
    }

    [Fact]
    public void UnknownCompiledDomainAndLegacyMetadataFailExplicitlyWithoutExecutingTarget()
    {
        Assert.Throws<PyNotImplementedError>(() => Find(Unique()));
        Assert.Throws<PyNotImplementedError>(() => Find(Unique() + ".child"));
        string name = Unique(); int calls = 0;
        PyModules.Register(name, "", "m.py", () => calls++, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        Assert.Throws<PyNotImplementedError>(() => Find(name)); Assert.Equal(0, calls);
        string native = Unique(); PyModules.RegisterNative(native, new Dictionary<string, object?>());
        Assert.Throws<PyNotImplementedError>(() => Find(native));
        string kindZero = Unique();
        PyModules.RegisterSourceLocated(kindZero, "", "m.py", 0, "m.py", false, false,
            () => calls++, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        Assert.Throws<PyNotImplementedError>(() => Find(kindZero)); Assert.Equal(0, calls);
    }

    [Fact]
    public void ParentPathMutationIsRejectedOnlyWhenLookupIsNeeded()
    {
        string parent = Unique(), child = parent + ".child";
        Register(parent, true); Register(child, false);
        PyModule package = PyModules.Import(parent); object? initial = package.GetAttr("__path__");
        package.SetAttr("__path__", new PyList(["/replacement"]));
        Assert.Throws<PyNotImplementedError>(() => Find(child));
        package.SetAttr("__path__", initial); PyModule module = PyModules.Import(child);
        package.SetAttr("__path__", new PyList([]));
        Assert.Same(module.GetAttr("__spec__"), Find(child));
    }

    [Fact]
    public void ParentExceptionPropagatesAndAllowsARealRetry()
    {
        string parent = Unique(), child = parent + ".child"; int attempts = 0;
        var error = new PyValueError("parent failed");
        Register(parent, true, () => { if (++attempts == 1) throw error; }); Register(child, false);
        Assert.Same(error, Assert.Throws<PyValueError>(() => Find(child)));
        Assert.Equal(child, PyObject.GetAttr(Find(child), "name")); Assert.Equal(2, attempts);
    }

    [Fact]
    public void ChangedAncestorPathStopsUncachedIntermediateParentBeforeItsBody()
    {
        string root = Unique(), middle = root + ".middle", child = middle + ".child";
        int middleRuns = 0, targetRuns = 0;
        Register(root, true); Register(middle, true, () => middleRuns++);
        Register(child, false, () => targetRuns++);
        PyModules.Import(root).SetAttr("__path__", new PyList([]));
        Assert.Throws<PyNotImplementedError>(() => Find(child));
        Assert.Equal(0, middleRuns); Assert.Equal(0, targetRuns);
    }

    [Fact]
    public void CachedIntermediateParentDoesNotRecheckItsAncestorPath()
    {
        string root = Unique(), middle = root + ".middle", child = middle + ".child";
        int middleRuns = 0, targetRuns = 0;
        Register(root, true); Register(middle, true, () => middleRuns++);
        Register(child, false, () => targetRuns++);
        PyModules.Import(middle);
        PyModules.Import(root).SetAttr("__path__", new PyList([]));
        Assert.Equal(child, PyObject.GetAttr(Find(child), "name"));
        Assert.Equal(1, middleRuns); Assert.Equal(0, targetRuns);
    }

    [Fact]
    public void NativeParentWithoutSourceMetadataIsNotMisreportedAsANonPackage()
    {
        string parent = Unique(), child = parent + ".child"; int targetRuns = 0;
        PyModules.RegisterNative(parent, new Dictionary<string, object?>());
        Register(child, false, () => targetRuns++);
        Assert.Throws<PyNotImplementedError>(() => Find(child));
        Assert.Equal(0, targetRuns);
    }

    [Fact]
    public void NonPackageAncestorStopsIntermediateBodyAtTheOrdinaryImportPhase()
    {
        string root = Unique(), middle = root + ".middle", child = middle + ".child";
        int middleRuns = 0;
        Register(root, false); Register(middle, true, () => middleRuns++); Register(child, false);
        var error = Assert.Throws<PyModuleNotFoundError>(() => Find(child));
        Assert.Equal($"No module named '{middle}'; '{root}' is not a package", error.Message);
        Assert.Equal(middle, PyObject.GetAttr(error, "name"));
        Assert.Null(error.Cause); Assert.True(error.SuppressContextFlag);
        Assert.IsType<PyAttributeError>(error.Context); Assert.Equal(0, middleRuns);
    }


    [Fact]
    public void ConvertedAttributeErrorsPreserveTheOuterExceptionChain()
    {
        string cachedName = Unique(), parent = Unique();
        var fields = Register(cachedName, false); PyModules.Import(cachedName); fields.Remove("__spec__");
        Register(parent, false);
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var missingSpec = Assert.Throws<PyValueError>(() => Find(cachedName));
            var specAttribute = Assert.IsType<PyAttributeError>(missingSpec.Context);
            Assert.Same(outer, specAttribute.Context); Assert.Null(missingSpec.Cause);
            Assert.True(missingSpec.SuppressContextFlag);
            var missingPath = Assert.Throws<PyModuleNotFoundError>(() => Find(parent + ".child"));
            var pathAttribute = Assert.IsType<PyAttributeError>(missingPath.Cause);
            Assert.Same(pathAttribute, missingPath.Context); Assert.Same(outer, pathAttribute.Context);
            Assert.True(missingPath.SuppressContextFlag); Assert.Same(outer, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

}
