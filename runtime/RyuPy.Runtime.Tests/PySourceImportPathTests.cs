// CP3.12.13已冻结路径事实的runtime回归草稿；真实JIT/完整门未运行，不能据此宣称验收。
using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;
using Fixture = RyuPy.Runtime.Tests.SourceImportFixture;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PySourceImportPathTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyExactSequencesRejectFreshResourceChildWithoutPublishing(bool tuple)
    {
        string root = Fixture.Unique(), child = root + ".data"; int calls = 0;
        var source = new Fixture(root, true); _ = new Fixture(child, true, () => calls++);
        PyModule parent = PyModules.Import(root);
        parent.SetAttr("__path__", tuple ? new PyTuple([]) : new PyList([])); source.Reads.Clear();
        var error = Assert.Throws<PyModuleNotFoundError>(() => PyImportlibResources.Files([child], [], []));
        Assert.Equal($"No module named '{child}'", error.Message);
        Assert.Equal(child, PyObject.GetAttr(error, "name")); Assert.Null(error.Cause); Assert.Null(error.Context);
        Assert.False(error.SuppressContextFlag); Assert.Equal(new[] { "__path__" }, source.Reads);
        Assert.Equal(0, calls); Assert.False(Fixture.Cached(child));
        Assert.Throws<PyAttributeError>(() => parent.GetAttr("data"));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void EqualNewSequencesAndRepeatedDirectoriesAllowFreshChild(bool tuple, int count)
    {
        string root = Fixture.Unique(), child = root + ".child"; int calls = 0;
        var source = new Fixture(root, true); _ = new Fixture(child, false, () => calls++);
        PyModule parent = PyModules.Import(root); object?[] values = new object?[count];
        for (int i = 0; i < count; i++) values[i] = new string(source.Directory.ToCharArray());
        parent.SetAttr("__path__", tuple ? new PyTuple(values) : new PyList(values)); source.Reads.Clear();
        PyModule imported = PyModules.Import(child);
        Assert.Equal(1, calls); Assert.Same(imported, parent.GetAttr("child"));
        Assert.Equal(new[] { "__path__", "child" }, source.Reads);
    }

    [Fact]
    public void MissingPathPreservesOriginalAttributeErrorAndOuterHandler()
    {
        string root = Fixture.Unique(), child = root + ".child";
        var source = new Fixture(root, true); _ = new Fixture(child, false); PyModules.Import(root);
        var missing = new PyAttributeError("actual missing path"); source.Getter = _ => throw missing;
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            using var hooks = new Fixture.FinderConfiguration("meta_path");
            var error = Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(child));
            Assert.Equal($"No module named '{child}'; '{root}' is not a package", error.Message);
            Assert.Equal(child, PyObject.GetAttr(error, "name")); Assert.Same(missing, error.Context);
            Assert.Same(outer, missing.Context); Assert.Null(error.Cause); Assert.True(error.SuppressContextFlag);
            Assert.Equal(new[] { "__path__" }, source.Reads); Assert.False(Fixture.Cached(child));
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void RealPathGetterFailurePrecedesHooksAndKeepsIdentity()
    {
        string root = Fixture.Unique(), child = root + ".child";
        var source = new Fixture(root, true); _ = new Fixture(child, false); PyModules.Import(root);
        var failure = new PyRuntimeError("getter"); source.Getter = _ => throw failure;
        using var hooks = new Fixture.FinderConfiguration("meta_path");
        Assert.Same(failure, Assert.Throws<PyRuntimeError>(() => PyModules.Import(child)));
        Assert.Equal(new[] { "__path__" }, source.Reads); Assert.False(Fixture.Cached(child));
    }

    [Theory]
    [InlineData("meta_path")]
    [InlineData("path_hooks")]
    [InlineData("path_importer_cache")]
    public void FinderConfigurationPrecedesEmptyPathNegativeResult(string field)
    {
        string root = Fixture.Unique(), child = root + ".child";
        var source = new Fixture(root, true); _ = new Fixture(child, false); PyModules.Import(root);
        source.Fields["__path__"] = new PyList([]);
        using var hooks = new Fixture.FinderConfiguration(field);
        var error = Assert.Throws<PyNotImplementedError>(() => PyModules.Import(child));
        Assert.Contains("explicit sys." + field, error.Message); Assert.False(Fixture.Cached(child));
        Assert.Equal(new[] { "__path__" }, source.Reads);
    }

    private sealed class ProtocolPath : PyNativeObject
    {
        internal int Calls;
        public override string TypeName => "protocol-path";
        public override object? GetAttr(string name) { Calls++; throw new PyRuntimeError("must not call"); }
    }
    private sealed class DerivedTuple : PyTuple { internal DerivedTuple() : base([]) { } }

    [Fact]
    public void UnknownPathsRemainExplicitWithoutUserProtocolsOrFalseAbsence()
    {
        string root = Fixture.Unique(), child = root + ".child"; var source = new Fixture(root, true);
        _ = new Fixture(child, false); PyModules.Import(root); var custom = new ProtocolPath();
        object[] paths = [PyNone.Instance, custom, new DerivedTuple(), new PyList([custom]),
            new PyList([source.Directory, source.Directory + "-other"]),
            new PyTuple([source.Directory + Path.DirectorySeparatorChar + "."])];
        foreach (object path in paths)
        {
            source.Fields["__path__"] = path;
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(child));
            Assert.False(Fixture.Cached(child));
        }
        Assert.Equal(0, custom.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncoherentChildIsUnsupportedEvenWhenThePathIsEmpty(bool empty)
    {
        string root = Fixture.Unique(), child = root + ".child"; int calls = 0;
        var source = new Fixture(root, true); _ = new Fixture(child, false, () => calls++,
            Path.GetFullPath("another-root/child.py")); PyModules.Import(root);
        if (empty) source.Fields["__path__"] = new PyList([]);
        var error = Assert.Throws<PyNotImplementedError>(() => PyModules.Import(child));
        Assert.Contains("incoherent", error.Message); Assert.Equal(0, calls); Assert.False(Fixture.Cached(child));
    }

    [Fact]
    public void SourceNonPackageFirstReadsPathBeforeClassifyingItsSource()
    {
        string root = Fixture.Unique(), child = root + ".child"; var source = new Fixture(root, false);
        _ = new Fixture(child, false);
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(child));
        Assert.Equal(new[] { "__path__" }, source.Reads);
        source.Fields["__path__"] = new PyList([]);
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(child));
    }

    [Fact]
    public void PublicMetadataChangesDoNotChooseOrRejectTheSource()
    {
        string root = Fixture.Unique(), child = root + ".child"; var source = new Fixture(root, true);
        _ = new Fixture(child, false); PyModule parent = PyModules.Import(root);
        source.Fields["__spec__"] = PyNone.Instance; source.Fields["__loader__"] = new object();
        source.Fields["__name__"] = "renamed"; source.Fields["__file__"] = "elsewhere"; source.Reads.Clear();
        PyModule imported = PyModules.Import(child);
        Assert.Equal(new[] { "__path__" }, source.Reads); Assert.Same(imported, parent.GetAttr("child"));
    }
    [Fact]
    public void HostRegistrationCannotEscapeParentWithPathComponentsInChildName()
    {
        foreach (string component in new[] { Path.DirectorySeparatorChar + "escape", "nested" + Path.DirectorySeparatorChar + "escape" })
        {
            string root = Fixture.Unique(), child = root + "." + component; int calls = 0;
            var source = new Fixture(root, true);
            _ = new Fixture(child, false, () => calls++, file: Path.Combine(source.Directory, component + ".py"));
            var error = Assert.Throws<PyNotImplementedError>(() => PyModules.Import(child));
            Assert.Contains("nonordinary source child names", error.Message);
            Assert.Equal(0, calls); Assert.False(Fixture.Cached(child));
            Assert.Equal(new[] { "__path__" }, source.Reads);
        }
    }

}
