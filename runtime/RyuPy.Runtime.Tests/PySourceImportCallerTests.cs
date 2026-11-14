// 单成员Legacy from-import：同名缺子须走第二阶段，正文发布属性与异常handler不能丢失。
using System;
using RyuPy.Runtime;
using Xunit;
using Fixture = RyuPy.Runtime.Tests.SourceImportFixture;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PySourceImportCallerTests
{
    private static PyModuleNotFoundError Missing(string child)
    {
        var error = new PyModuleNotFoundError("child body"); error.UserAttributes["name"] = child; return error;
    }
    private static void Diagnostic(PyImportError error, Fixture source)
    {
        Assert.Equal($"cannot import name 'child' from '{source.Name}' ({source.File})", error.Message);
        Assert.Equal(source.Name, PyObject.GetAttr(error, "name"));
        Assert.Equal(source.File, PyObject.GetAttr(error, "path"));
        Assert.Null(error.Cause); Assert.False(error.SuppressContextFlag);
        Assert.False(Fixture.Cached(source.Name + ".child")); Assert.True(Fixture.Cached(source.Name));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyPathBecomesSourceImportErrorWithoutRunningChild(bool tuple)
    {
        string root = Fixture.Unique(); int calls = 0; var source = new Fixture(root, true);
        _ = new Fixture(root + ".child", false, () => calls++); PyModules.Import(root);
        source.Fields["__path__"] = tuple ? new PyTuple([]) : new PyList([]); source.Reads.Clear();
        var error = Assert.Throws<PyImportError>(() => PyModules.ImportAttr(root, "child"));
        Diagnostic(error, source); Assert.Null(error.Context); Assert.Null(PyErrors.Current);
        Assert.Equal(0, calls); Assert.Equal(new[] { "child", "__path__", "child" }, source.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MatchingBodyFailureLeavesHandlerBeforeFinalError(bool outerHandler)
    {
        string root = Fixture.Unique(); var source = new Fixture(root, true); var missing = Missing(root + ".child");
        _ = new Fixture(root + ".child", false, () => throw missing);
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outerHandler ? outer : null);
        try
        {
            var error = Assert.Throws<PyImportError>(() => PyModules.ImportAttr(root, "child"));
            Diagnostic(error, source); Assert.NotSame(missing, error);
            Assert.Same(outerHandler ? outer : null, error.Context);
            Assert.Same(outerHandler ? outer : null, PyErrors.Current);
            Assert.Equal(new[] { "child", "__path__", "child" }, source.Reads);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Theory]
    [InlineData("different")]
    [InlineData("none")]
    [InlineData("absent")]
    public void UnrelatedOrNamelessBodyFailureKeepsOriginalIdentity(string mode)
    {
        string root = Fixture.Unique(); var source = new Fixture(root, true); var missing = Missing("elsewhere");
        if (mode == "none") missing.UserAttributes["name"] = PyNone.Instance;
        if (mode == "absent") missing.UserAttributes.Remove("name");
        _ = new Fixture(root + ".child", false, () => throw missing);
        Assert.Same(missing, Assert.Throws<PyModuleNotFoundError>(() => PyModules.ImportAttr(root, "child")));
        Assert.Null(PyErrors.Current); Assert.False(Fixture.Cached(root + ".child"));
        Assert.Equal(new[] { "child", "__path__" }, source.Reads);
    }

    [Fact]
    public void PublishedParentValueWinsAfterMatchingBodyFailure()
    {
        string root = Fixture.Unique(); var source = new Fixture(root, true); var missing = Missing(root + ".child");
        _ = new Fixture(root + ".child", false, () => {
            PyModules.Import(root).SetAttr("child", "published"); throw missing;
        });
        Assert.Equal("published", PyModules.ImportAttr(root, "child"));
        Assert.False(Fixture.Cached(root + ".child")); Assert.Null(PyErrors.Current);
        Assert.Equal(new[] { "child", "__path__", "child" }, source.Reads);
    }

    [Fact]
    public void SecondAttributeReadKeepsFailureAndOuterHandler()
    {
        string root = Fixture.Unique(); var source = new Fixture(root, true); var missing = Missing(root + ".child");
        _ = new Fixture(root + ".child", false, () => throw missing); PyModules.Import(root);
        var failure = new PyRuntimeError("second getter"); var outer = new PyValueError("outer"); int reads = 0;
        source.Getter = key => {
            if (key == "child" && ++reads == 2) { Assert.Same(outer, PyErrors.Current); throw failure; }
            return source.Read(key);
        };
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            Assert.Same(failure, Assert.Throws<PyRuntimeError>(() => PyModules.ImportAttr(root, "child")));
            Assert.Same(outer, PyErrors.Current); Assert.Equal(2, reads);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void SecondAttributeMissCanReturnTheExistingCacheWithoutRestartingImport()
    {
        string root = Fixture.Unique(), child = root + ".child"; var source = new Fixture(root, true);
        var missing = Missing(child); int reads = 0, calls = 0; PyModule? published = null;
        _ = new Fixture(child, false, () => { calls++; throw missing; }); PyModules.Import(root);
        source.Getter = key => {
            if (key == "child" && ++reads == 2)
            {
                _ = new Fixture(child, false, () => calls++); published = PyModules.Import(child);
                source.Fields.Remove("child"); throw new PyAttributeError("second missing");
            }
            return source.Read(key);
        };
        object? result = PyModules.ImportAttr(root, "child");
        Assert.Same(published, result); Assert.Same(published, PyModules.Import(child));
        Assert.Equal(2, calls); Assert.Equal(2, reads); Assert.Null(PyErrors.Current);
    }

    [Fact]
    public void NameDescriptorRemainsExplicitAndRestoresOuterHandler()
    {
        string root = Fixture.Unique(); _ = new Fixture(root, true); var missing = Missing(root + ".child"); int calls = 0;
        var cls = new PyClass("Missing"); missing.BindPythonClass(cls);
        cls.SetAttr("name", new PyProperty(PyNativeFunctions.Create("name", ["self"], [null], 1,
            _ => { calls++; throw new PyAttributeError("callback"); })));
        _ = new Fixture(root + ".child", false, () => throw missing);
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var error = Assert.Throws<PyNotImplementedError>(() => PyModules.ImportAttr(root, "child"));
            Assert.Same(missing, error.Context); Assert.Same(outer, PyErrors.Current); Assert.Equal(0, calls);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void KindZeroKeepsClosedWorldAndOldDiagnosticFormat()
    {
        string root = Fixture.Unique(); _ = new Fixture(root, true, kind: 0);
        _ = new Fixture(root + ".child", false, kind: 0);
        Assert.Same(PyModules.Import(root + ".child"), PyModules.ImportAttr(root, "child"));
        var error = Assert.Throws<PyImportError>(() => PyModules.ImportAttr(root, "absent"));
        Assert.Equal($"cannot import name 'absent' from '{root}'", error.Message);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(error, "name"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(error, "path"));
    }
}
