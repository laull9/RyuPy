// 单个有序候选注册域；唯一静态配置不reset，真实执行/缓存/副作用而非只测JSON形状。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyDefaultSourceCandidatesTests
{
    [Fact]
    public void DefaultSourceCandidateActivationPreservesCacheNativeAndFailurePhases()
    {
        PySystem.Initialize("default-candidates-tests.py", []);
        string prefix = "candidate_" + Guid.NewGuid().ToString("N");
        string fresh = prefix + "_fresh", cached = prefix + "_cached", dead = prefix + "_dead";
        string bad = prefix + "_bad", guarded = prefix + "_guarded", outside = prefix + "_outside";
        var dependencies = new Dictionary<string, string[]>();
        var locations = new Dictionary<string, object>();
        var paths = new Dictionary<string, string>();
        string alias = prefix + "_alias", finder = prefix + "_finder";
        int aliasParentRuns = 0, aliasChildRuns = 0, aliasCallbacks = 0;
        int finderParentRuns = 0, finderChildRuns = 0;
        string aliasOriginal = prefix + "_alias_original";
        var aliasMarker = new PyList([]);
        PyModules.RegisterNative(aliasOriginal, new Dictionary<string, object?> { ["marker"] = aliasMarker });
        PyModule aliasObject = PyModules.Import(aliasOriginal);
        PyOps.SetItem(new PyModuleView(), alias, aliasObject);
        var roots = new List<string>();
        var errors = new Dictionary<string, string>();
        string deferredNew = prefix + "_deferred_new", deferredCached = prefix + "_deferred_cached";
        string deferredInit = prefix + "_deferred_init", deferredProtected = prefix + "_deferred_protected";
        int rejectedChildRuns = 0;
        int parentRuns = 0, childRuns = 0, badParentRuns = 0;
        void Candidate(string name, bool package, Action<object> body, bool legacyProbe = false)
        {
            string path = Path.GetFullPath(Path.Combine("candidate-fixtures", name.Replace('.', Path.DirectorySeparatorChar))
                + (package ? Path.DirectorySeparatorChar + "__init__.py" : ".py"));
            string owner = package ? name : name[..name.LastIndexOf('.')];
            string diagnostic = name + ".py";
            locations.Add(name, new { package = owner, file = diagnostic, path, is_package = package });
            paths.Add(name, path);
            if (legacyProbe)
                PyModules.RegisterCandidateSource(name, owner, diagnostic, 1, path, true, package,
                    () => body(new object()), key => { aliasCallbacks++; throw new PyAttributeError(key); },
                    (_, _, _) => { aliasCallbacks++; });
            else PyModules.RegisterCandidateInstance(name, owner, diagnostic, 1, path, package, state => body(state));
        }
        void Family(string name, Action<object> parent, Action<object> child)
        {
            Candidate(name, true, parent); Candidate(name + ".child", false, child);
            dependencies[name] = []; dependencies[name + ".child"] = [name]; roots.Add(name + ".child");
        }
        Candidate(alias, true, _ => aliasParentRuns++, legacyProbe: true);
        Candidate(alias + ".child", false, _ => aliasChildRuns++, legacyProbe: true);
        dependencies[alias] = []; dependencies[alias + ".child"] = [alias]; roots.Add(alias + ".child");
        Family(finder, _ => finderParentRuns++, _ => finderChildRuns++);
        Family(fresh, _ => parentRuns++, state => { childRuns++; PyModuleInstances.StoreName(state, "value", 41L); });
        Family(cached, _ => throw new Exception("cached parent must not execute"), _ => { });
        Family(dead, _ => throw new Exception("dead family must not execute"), _ => { });
        Family(guarded, _ => throw new Exception("protected parent must not execute"), _ => { });
        Candidate(bad, true, _ => badParentRuns++);
        string badPath = Path.GetFullPath(Path.Combine("candidate-fixtures", bad, "child.py"));
        locations[bad + ".child"] = new { package = bad, file = "child.py", path = badPath, is_package = false };
        dependencies[bad] = []; dependencies[bad + ".child"] = [bad]; roots.Add(bad + ".child");
        errors[bad + ".child"] = "bad source loading is not implemented";
        foreach (string name in new[] { deferredNew, deferredCached, deferredInit, deferredProtected })
        {
            Candidate(name + ".child", false, _ => rejectedChildRuns++);
            string path = Path.GetFullPath(Path.Combine("candidate-fixtures", name, "__init__.py"));
            locations[name] = new { package = name, file = "__init__.py", path, is_package = true };
            errors[name] = "bad parent source loading is not implemented";
            dependencies[name] = []; dependencies[name + ".child"] = [name]; roots.Add(name + ".child");
        }
        foreach (string name in new[] { deferredNew, deferredCached })
            PyModules.RegisterNative(name, new Dictionary<string, object?> { ["marker"] = name });
        PyModule savedDeferred = PyModules.Import(deferredCached);
        PyModules.RegisterNativeLazy(deferredInit, "", "", () => {
            PyModule initializing = PyModules.Import(deferredInit);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(deferredInit + ".child"));
            Assert.Same(initializing, PyModules.Import(deferredInit));
            Assert.Null(PyModules.ModuleForFile(deferredInit + ".child.py"));
        }, name => name == "marker" ? deferredInit : throw new PyAttributeError(name), PyModules.SetDynamic);
        foreach (string name in new[] { fresh, cached, dead, guarded, outside })
            PyModules.RegisterNative(name, new Dictionary<string, object?> { ["marker"] = name });
        PyModule savedCached = PyModules.Import(cached);
        PyModule savedOutside = PyModules.Import(outside);
        string payload = JsonSerializer.Serialize(new { schema_version = 1, roots, dependencies, locations,
            deferred = errors,
            outside = new[] { outside, prefix + "_unknown.child" }, @protected = new[] { guarded, deferredProtected } });
        PyModules.ConfigureOptionalSources(payload);
        PyModules.ConfigureOptionalSources(payload);
        Assert.Throws<PyNotImplementedError>(() => PyModules.ConfigureOptionalSources("{}"));
        // 真正Import进入候选激活而非finder前置alias拒绝；整族不发布、不调用任何候选委托。
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(alias + ".child"));
        Assert.Null(PyModules.ModuleForFile(alias + ".py"));
        Assert.Null(PyModules.ModuleForFile(alias + ".child.py"));
        Assert.False(PyOps.Contains(new PyModuleView(), alias + ".child"));
        Assert.Same(aliasObject, PyOps.GetItem(new PyModuleView(), alias));
        Assert.Same(aliasMarker, aliasObject.GetAttr("marker"));
        Assert.Equal(0, aliasParentRuns); Assert.Equal(0, aliasChildRuns); Assert.Equal(0, aliasCallbacks);
        // 未缓存finder必须自己激活，执行父但不执行或缓存目标；origin来自同次注册路径。
        Assert.False(PyOps.Contains(new PyModuleView(), finder));
        Assert.False(PyOps.Contains(new PyModuleView(), finder + ".child"));
        object? finderSpec = PyOps.CallValue(PyImportlibUtil.CreateAttributes()["find_spec"], [finder + ".child"], [], []);
        Assert.Equal(1, finderParentRuns); Assert.Equal(0, finderChildRuns);
        Assert.False(PyOps.Contains(new PyModuleView(), finder + ".child"));
        Assert.Equal(paths[finder + ".child"], PyObject.GetAttr(finderSpec, "origin"));
        object? finderLoader = PyObject.GetAttr(finderSpec, "loader");
        Assert.NotNull(finderLoader);
        Assert.Equal("CompiledModuleLoader", PyRepr.TypeName(finderLoader));
        Assert.Equal(finder + ".child", PyObject.GetAttr(finderLoader, "name"));
        Assert.Equal(paths[finder + ".child"], PyObject.GetAttr(finderLoader, "path"));
        PyModule finderChild = PyModules.Import(finder + ".child");
        Assert.Equal(1, finderParentRuns); Assert.Equal(1, finderChildRuns);
        Assert.Same(finderChild, PyModules.Import(finder + ".child"));
        Assert.Equal(1, finderChildRuns);
        // 不可执行父同样参与全组验证：未缓存/已缓存/初始化/认证保护冲突均零发布。
        foreach (string name in new[] { deferredNew, deferredCached, deferredProtected })
        {
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(name + ".child"));
            Assert.Null(PyModules.ModuleForFile(name + ".child.py"));
        }
        Assert.Equal(deferredNew, PyModules.Import(deferredNew).GetAttr("marker"));
        Assert.Same(savedDeferred, PyModules.Import(deferredCached));
        Assert.Equal(deferredInit, PyModules.Import(deferredInit).GetAttr("marker"));
        Assert.Equal(0, rejectedChildRuns);
        // 普通native父有active实现：dead import/hint没有执行，不能仅因族成员而激活。
        PyModule deadNative = PyModules.Import(dead);
        Assert.Equal(dead, deadNative.GetAttr("marker"));
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(dead + ".child"));
        Assert.Same(deadNative, PyModules.Import(dead));
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(cached + ".child"));
        Assert.Same(savedCached, PyModules.Import(cached));
        // fresh目标真正cache miss：发布source父后才初始化，目标缓存短路不重放。
        PyModule child = PyModules.Import(fresh + ".child");
        Assert.Equal(41L, child.GetAttr("value")); Assert.Equal(1, parentRuns); Assert.Equal(1, childRuns);
        Assert.Same(child, PyModules.Import(fresh + ".child"));
        object? spec = PyOps.CallValue(PyImportlibUtil.CreateAttributes()["find_spec"], [fresh + ".child"], [], []);
        Assert.Same(child.GetAttr("__spec__"), spec);
        // 认证native冲突零发布；普通原native仍可导入。
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(guarded + ".child"));
        Assert.Equal(guarded, PyModules.Import(guarded).GetAttr("marker"));
        // 被拒子源码零正文，但真实父副作用保留；重试创建新异常，不回退native。
        var first = Assert.Throws<PyNotImplementedError>(() => PyModules.Import(bad + ".child"));
        var second = Assert.Throws<PyNotImplementedError>(() => PyModules.Import(bad + ".child"));
        Assert.NotSame(first, second); Assert.Equal(1, badParentRuns);
        PyModule badParent = PyModules.Import(bad);
        object? originalPath = badParent.GetAttr("__path__");
        badParent.SetAttr("__path__", new PyList([]));
        Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(bad + ".child"));
        badParent.SetAttr("__path__", originalPath);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyImportlibUtil.CreateAttributes()["find_spec"], [bad + ".child"], [], []));
        Assert.Same(savedOutside, PyModules.Import(outside));
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(prefix + "_unknown.child"));
    }
}
