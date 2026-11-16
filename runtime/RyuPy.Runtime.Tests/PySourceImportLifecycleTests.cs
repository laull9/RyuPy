// 路径冷守卫不得改变缓存/循环/失败清理，实际父对象来源不随同名登记替换而漂移。
using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;
using Fixture = RyuPy.Runtime.Tests.SourceImportFixture;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PySourceImportLifecycleTests
{
    [Fact]
    public void InitializingAndParentImportedTargetsBypassChangedPathsAndLaterHooks()
    {
        string root = Fixture.Unique(), child = root + ".child"; int calls = 0; PyModule? partial = null;
        Fixture? source = null;
        source = new Fixture(root, true, () => PyModules.Import(child));
        _ = new Fixture(child, false, () => {
            calls++; partial = PyModules.Import(child);
            source!.Fields["__path__"] = new PyList([]);
            Assert.Same(partial, PyModules.Import(child));
        });
        PyModule imported = PyModules.Import(child);
        Assert.Same(partial, imported); Assert.Equal(1, calls);
        Assert.Equal(new[] { "__path__" }, source.Reads);
        using var hooks = new Fixture.FinderConfiguration("meta_path");
        Assert.Same(imported, PyModules.Import(child)); Assert.Equal(new[] { "__path__" }, source.Reads);
    }

    [Fact]
    public void FailedChildRetainsDependencyAndRereadsPathBeforeRetry()
    {
        string root = Fixture.Unique(), child = root + ".child", dependency = Fixture.Unique();
        var source = new Fixture(root, true); _ = new Fixture(dependency, false);
        var failure = new PyValueError("first attempt"); PyModule? saved = null; int attempts = 0;
        _ = new Fixture(child, false, () => {
            PyModule current = PyModules.Import(dependency);
            if (saved is null) saved = current; else Assert.Same(saved, current);
            if (++attempts == 1) throw failure;
        });
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyModules.Import(child)));
        Assert.False(Fixture.Cached(child)); Assert.True(Fixture.Cached(dependency));
        object? original = source.Fields["__path__"];
        source.Fields["__path__"] = new PyTuple([]);
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(child)); Assert.Equal(1, attempts);
        source.Fields["__path__"] = original; PyModule imported = PyModules.Import(child);
        Assert.Equal(2, attempts); Assert.Same(imported, PyModules.Import(root).GetAttr("child"));
        Assert.Same(saved, PyModules.Import(dependency));
    }

    [Fact]
    public void ParentFailurePrecedesNewPathAndFinderChecks()
    {
        string root = Fixture.Unique(), child = root + ".child"; var failure = new PyValueError("parent");
        Fixture.FinderConfiguration? hooks = null; Fixture? source = null; int calls = 0;
        source = new Fixture(root, true, () => {
            source!.Fields["__path__"] = new PyList([]);
            hooks = new Fixture.FinderConfiguration("meta_path"); throw failure;
        });
        _ = new Fixture(child, false, () => calls++);
        try
        {
            Assert.Same(failure, Assert.Throws<PyValueError>(() => PyModules.Import(child)));
            Assert.False(Fixture.Cached(root)); Assert.False(Fixture.Cached(child));
            Assert.Empty(source.Reads); Assert.Equal(0, calls);
        }
        finally { hooks?.Dispose(); }
    }

    [Fact]
    public void CachedParentUsesItsOwnLocationAfterNameReregistration()
    {
        string root = Fixture.Unique(), child = root + ".child", other = root + ".other";
        var original = new Fixture(root, true); PyModule parent = PyModules.Import(root);
        string replacementDirectory = Path.GetFullPath("replacement-import-root");
        _ = new Fixture(root, true, file: Path.Combine(replacementDirectory, "__init__.py"));
        _ = new Fixture(child, false);
        PyModule imported = PyModules.Import(child);
        Assert.Same(parent, PyModules.Import(root)); Assert.Same(imported, parent.GetAttr("child"));
        _ = new Fixture(other, false, file: Path.Combine(replacementDirectory, "other.py"));
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(other)); Assert.False(Fixture.Cached(other));
        Assert.Same(original.Fields["__path__"], parent.GetAttr("__path__"));
    }

    [Fact]
    public void ParentInitializationSelectsTheLatestChildRegistration()
    {
        string root = Fixture.Unique(), child = root + ".child"; int stale = 0, latest = 0;
        _ = new Fixture(child, false, () => stale++, Path.GetFullPath("stale/child.py"));
        _ = new Fixture(root, true, () => { _ = new Fixture(child, false, () => latest++); });
        PyModules.Import(child); Assert.Equal(0, stale); Assert.Equal(1, latest);
    }

    [Fact]
    public void InstanceParentAndChildUseTheSameActualDictionaryPath()
    {
        string root = Fixture.Unique(), child = root + ".child"; int calls = 0;
        PyModules.RegisterInstanceSourceLocated(root, root, "__init__.py", 1,
            Fixture.Location(root, true), true, _ => { });
        PyModules.RegisterInstanceSourceLocated(child, root, "child.py", 1,
            Fixture.Location(child, false), false, _ => calls++);
        PyModule parent = PyModules.Import(root); var globals = Assert.IsType<PyDict>(parent.GetAttr("__dict__"));
        object? original = globals.Get("__path__"); globals.Set("__path__", new PyList([]));
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(child)); Assert.Equal(0, calls);
        globals.Set("__path__", original); PyModules.Import(child); Assert.Equal(1, calls);
    }

    [Fact]
    public void KindZeroAndOldResourceParentsKeepTheirClosedWorldImports()
    {
        string root = Fixture.Unique(), child = root + ".child"; int calls = 0;
        var source = new Fixture(root, true, kind: 0); _ = new Fixture(child, false, () => calls++);
        PyModules.Import(child); Assert.Equal(1, calls); Assert.Empty(source.Reads);
        string legacy = Fixture.Unique(), legacyChild = legacy + ".child";
        PyModules.RegisterResourceLocated(legacy, legacy, "__init__.py", 1, Fixture.Location(legacy, true), true, true,
            () => { }, _ => throw new PyRuntimeError("old parent must not gain path reads"), PyModules.SetDynamic);
        _ = new Fixture(legacyChild, false); PyModules.Import(legacyChild);
    }

    [Fact]
    public void LegacyAndLocationlessChildrenDoNotAddSourceParentChecks()
    {
        string root = Fixture.Unique(), legacy = root + ".legacy", locationless = root + ".locationless";
        var source = new Fixture(root, true); PyModules.Import(root);
        source.Getter = _ => throw new PyRuntimeError("old domain must not gain path reads");
        PyModules.Register(legacy, root, "legacy.py", () => { }, _ => null, PyModules.SetDynamic);
        _ = new Fixture(locationless, false, kind: 0);
        PyModules.Import(legacy); PyModules.Import(locationless); Assert.Empty(source.Reads);
    }

    [Fact]
    public void UnknownNamesKeepTheOldClosedWorldBranchWithoutNewGetters()
    {
        string root = Fixture.Unique(); var source = new Fixture(root, true); PyModules.Import(root);
        source.Getter = _ => throw new PyRuntimeError("unregistered name must not read path");
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(root + ".unknown")); Assert.Empty(source.Reads);
    }
}
