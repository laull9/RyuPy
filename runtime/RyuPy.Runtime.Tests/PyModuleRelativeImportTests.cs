// 相对导入冷协议：CP oracle已锁定元数据优先级及fromlist两个阶段；未知发现单列NI。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyModuleRelativeImportTests
{
    private static string Root() => "relative_" + Guid.NewGuid().ToString("N");
    private static void Register(string name, bool package, PyModuleExecute body)
    {
        string parent = package ? name : name.Contains('.') ? name[..name.LastIndexOf('.')] : "";
        PyModules.RegisterInstanceSourceLocated(name, parent, "relative.py", 1,
            "/virtual/" + name.Replace('.', '/') + (package ? "/__init__.py" : ".py"), package, body);
    }
    private static (PyModule Module, object Context) Context(string package)
    {
        object? context = null;
        string name = package + ".subject";
        Register(name, false, state => context = state);
        var module = PyModuleInstances.Create(name);
        PyModuleInstances.Execute(module);
        return (module, context!);
    }
    [Fact]
    public void MetadataUsesTargetGlobalsAndPreservesErrorPhases()
    {
        string root = Root(), other = Root();
        Register(root, true, state => PyModuleInstances.StoreName(state, "value", 1L));
        Register(other, true, state => PyModuleInstances.StoreName(state, "value", 2L));
        var (module, context) = Context(root);
        Assert.Same(PyModules.Import(root), PyModuleInstances.RelativeImport(context, "", 1, ["value"]));
        object? originalSpec = module.GetAttr("__spec__");
        module.SetAttr("__package__", PyNone.Instance);
        Assert.Same(PyModules.Import(root), PyModuleInstances.RelativeImport(context, "", 1, ["value"]));
        var globals = Assert.IsType<PyDict>(module.GetAttr("__dict__")); globals.Delete("__package__");
        Assert.Same(PyModules.Import(root), PyModuleInstances.RelativeImport(context, "", 1, ["value"]));
        Assert.Same(originalSpec, module.GetAttr("__spec__"));
        module.SetAttr("__package__", other);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context, "", 1, ["value"]));
        module.SetAttr("__spec__", PyNone.Instance);
        var imported = PyModuleInstances.RelativeImport(context, "", 1, ["value"]);
        Assert.Equal(2L, PyModuleInstances.RelativeImportAttr(context, imported, "value"));
        module.SetAttr("__package__", 3L); module.SetAttr("__spec__", new object());
        Assert.Equal("package must be a string", Assert.Throws<PyTypeError>(() => PyModuleInstances.RelativeImport(context, "", 1, [])).Message);
        module.SetAttr("__package__", root);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context, "", 1, []));
        module.SetAttr("__spec__", PyNone.Instance); module.SetAttr("__package__", "");
        Assert.Throws<PyImportError>(() => PyModuleInstances.RelativeImport(context, "", 1, []));
        module.SetAttr("__package__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context, "", 1, []));
        module.SetAttr("__package__", root);
        Assert.Throws<PyImportError>(() => PyModuleInstances.RelativeImport(context, "", 2, []));
    }
    [Fact]
    public void FromlistCompletesBeforeAnyAttributeBinding()
    {
        string root = Root(); var events = new List<string>();
        Register(root, true, _ => { });
        Register(root + ".a", false, _ => events.Add("a"));
        Register(root + ".b", false, _ => { events.Add("b"); PyModules.Import(root).SetAttr("a", "changed"); });
        var (_, context) = Context(root);
        var imported = PyModuleInstances.RelativeImport(context, "", 1, ["a", "b"]);
        Assert.Equal(new[] { "a", "b" }, events);
        Assert.Equal("changed", PyModuleInstances.RelativeImportAttr(context, imported, "a"));
        Assert.Same(PyModules.Import(root + ".b"), PyModuleInstances.RelativeImportAttr(context, imported, "b"));
    }
    [Fact]
    public void FailedChildRetainsEarlierDependencyAndCanRetry()
    {
        string root = Root(); int first = 0, attempts = 0;
        Register(root, true, _ => { });
        Register(root + ".a", false, _ => first++);
        Register(root + ".bad", false, _ => { attempts++; if (attempts == 1) throw new PyRuntimeError("bad init"); });
        var (_, context) = Context(root);
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.RelativeImport(context, "", 1, ["a", "bad"]));
        Assert.Equal(1, first); Assert.Equal(1, attempts);
        _ = PyModuleInstances.RelativeImport(context, "", 1, ["a", "bad"]);
        Assert.Equal(1, first); Assert.Equal(2, attempts);
    }
    [Fact]
    public void NonPackageMissingExportDiffersFromUnknownPackageDiscovery()
    {
        string root = Root(); int first = 0;
        Register(root, true, _ => { });
        Register(root + ".plain", false, state => PyModuleInstances.StoreName(state, "value", 7L));
        Register(root + ".a", false, _ => first++);
        var (_, context) = Context(root);
        var plain = PyModuleInstances.RelativeImport(context, "plain", 1, ["value", "absent"]);
        Assert.Equal(7L, PyModuleInstances.RelativeImportAttr(context, plain, "value"));
        Assert.Throws<PyImportError>(() => PyModuleInstances.RelativeImportAttr(context, plain, "absent"));
        // 闭世界无负存在性证明：NI不是CP缺子模块ImportError，保留前项初始化。
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context, "", 1, ["a", "unknown"]));
        Assert.Equal(1, first);
        PyModules.Import(root).SetAttr("__path__", new PyList(["/other"]));
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context, "", 1, ["newchild"]));
    }
    [Fact]
    public void HookIsCheckedBeforeMetadataAndDependencies()
    {
        string root = Root(); int initialized = 0, called = 0;
        Register(root, true, _ => initialized++);
        var (module, _) = Context(root);
        var builtins = new PyDict();
        builtins.Set("__import__", PyNativeFunctions.CreateVariadic("hook", _ => { called++; return null; }));
        module.SetAttr("__builtins__", builtins); module.SetAttr("__package__", 3L);
        object? context = null;
        string capture = root + ".capture";
        Register(capture, false, state => context = state);
        var target = PyModuleInstances.Create(capture); target.SetAttr("__builtins__", builtins); target.SetAttr("__package__", 3L);
        PyModuleInstances.Execute(target);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context!, "", 1, []));
        Assert.Equal(0, initialized); Assert.Equal(0, called);
        builtins.Delete("__import__");
        Assert.Throws<PyImportError>(() => PyModuleInstances.RelativeImport(context!, "", 1, []));
    }
    [Fact]
    public void FromlistReadsCurrentNameAndPathAfterPreviousChild()
    {
        string root = Root(), other = Root();
        Register(root, true, _ => { }); Register(other, true, _ => { });
        Register(root + ".a", false, _ => PyModules.Import(root).SetAttr("__name__", other));
        Register(other + ".b", false, _ => { });
        var (_, context) = Context(root);
        var imported = PyModuleInstances.RelativeImport(context, "", 1, ["a", "b"]);
        Assert.Same(PyModules.Import(other + ".b"), PyModuleInstances.RelativeImportAttr(context, imported, "b"));
        string altered = Root(); int next = 0;
        Register(altered, true, _ => { });
        Register(altered + ".a", false, _ => PyModules.Import(altered).SetAttr("__path__", new PyList(["/changed"])));
        Register(altered + ".b", false, _ => next++);
        var (_, changedContext) = Context(altered);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(changedContext, "", 1, ["a", "b"]));
        Assert.Equal(0, next);
    }
    [Fact]
    public void ParentReentryUsesPartialCachedChildWithoutSecondExecution()
    {
        string root = Root(); int initialized = 0;
        Register(root, true, _ => PyModules.Import(root + ".child"));
        Register(root + ".child", false, state => {
            initialized++;
            var package = PyModuleInstances.RelativeImport(state, "", 1, ["child"]);
            var partial = PyModuleInstances.RelativeImportAttr(state, package, "child");
            Assert.Same(PyModules.Import(root + ".child"), partial);
            PyModuleInstances.StoreName(state, "value", 3L);
        });
        var child = PyModules.Import(root + ".child");
        Assert.Equal(1, initialized); Assert.Equal(3L, child.GetAttr("value"));
    }

    [Fact]
    public void CachedChildDoesNotConsumeModifiedPath()
    {
        string root = Root(), other = Root();
        Register(root, true, _ => { }); Register(root + ".child", false, _ => { });
        Register(other, true, _ => { }); Register(other + ".child", false, _ => { });
        var child = PyModules.Import(root + ".child");
        var package = PyModules.Import(root);
        Assert.IsType<PyDict>(package.GetAttr("__dict__")).Delete("child");
        package.SetAttr("__path__", new object());
        var (_, context) = Context(root);
        var imported = PyModuleInstances.RelativeImport(context, "", 1, ["child"]);
        Assert.Same(child, PyModuleInstances.RelativeImportAttr(context, imported, "child"));
        package.SetAttr("__name__", other);
        imported = PyModuleInstances.RelativeImport(context, "", 1, ["child"]);
        Assert.Same(PyModules.Import(other + ".child"), PyModuleInstances.RelativeImportAttr(context, imported, "child"));
    }
    [Fact]
    public void MatchingMissingNameDefersFailureUntilAttributePhase()
    {
        foreach (bool same in new[] { true, false })
        {
            string root = Root();
            Register(root, true, state => PyModuleInstances.StoreName(state, "value", 7L));
            var error = new PyModuleNotFoundError("failed");
            error.UserAttributes["name"] = same ? root + ".bad" : "external";
            Register(root + ".bad", false, _ => throw error);
            var (_, context) = Context(root);
            if (same)
            {
                var imported = PyModuleInstances.RelativeImport(context, "", 1, ["value", "bad"]);
                Assert.Equal(7L, PyModuleInstances.RelativeImportAttr(context, imported, "value"));
                Assert.Throws<PyImportError>(() => PyModuleInstances.RelativeImportAttr(context, imported, "bad"));
            }
            else Assert.Same(error, Assert.Throws<PyModuleNotFoundError>(() => PyModuleInstances.RelativeImport(context, "", 1, ["value", "bad"])));
        }
    }

    [Fact]
    public void SharedNameReadHasNoneDefaultAndRejectsDescriptorsWithoutCallbacks()
    {
        Assert.Same(PyNone.Instance, PyObject.GetAttr(new PyImportError("plain"), "name"));
        string root = Root(); int calls = 0;
        Register(root, true, _ => { });
        var missing = new PyModuleNotFoundError("original");
        var cls = new PyClass("Missing"); missing.BindPythonClass(cls);
        cls.SetAttr("name", 7L);
        Assert.Equal(7L, PyObject.GetAttr(missing, "name"));
        cls.SetAttr("name", root + ".bad");
        Assert.Equal(root + ".bad", PyObject.GetAttr(missing, "name"));
        missing.UserAttributes["name"] = "instance";
        Assert.Equal("instance", PyObject.GetAttr(missing, "name"));
        cls.SetAttr("name", new PyProperty(PyNativeFunctions.Create("name", ["self"], [null], 1,
            _ => { calls++; throw new PyAttributeError("callback"); })));
        Register(root + ".bad", false, _ => throw missing);
        var (_, context) = Context(root);
        var outer = new PyValueError("ambient"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var unsupported = Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.RelativeImport(context, "", 1, ["bad"]));
            Assert.Same(missing, unsupported.Context); Assert.Same(outer, PyErrors.Current);
            Assert.Equal(0, calls);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

}
