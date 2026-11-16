// 受限 sys.modules 新增别名：真实身份、未执行实例、拒绝原子性及旧查询快照的公开入口回归。
using System;
using System.Collections.Generic;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public partial class PyModuleCacheAliasTests
{
    private static string Unique() => "cache_alias_" + Guid.NewGuid().ToString("N");
    private static PyModuleView View() => new();
    private static object? Call(object target, string method, params object?[] args) =>
        PyOps.CallMethod(target, method, args, [], []);
    private static object? Find(string name) =>
        PyOps.CallValue(PyImportlibUtil.CreateAttributes()["find_spec"], [name], [], []);
    private static PyModule Create(object spec) => Assert.IsType<PyModule>(
        PyOps.CallValue(PyImportlibUtil.CreateAttributes()["module_from_spec"], [spec], [], []));
    private static PyModule Empty()
    {
        object spec = PyOps.CallValue(PyModuleSpec.CreateAttributes()["ModuleSpec"], [Unique(), PyNone.Instance], [], [])!;
        return Create(spec);
    }
    private static PyModule Native(Action? init = null, PyModuleGet? get = null)
    {
        string name = Unique();
        PyModules.Register(name, "", "", () => init?.Invoke(), get ?? (_ => throw new PyAttributeError("missing")), PyModules.SetDynamic);
        return PyModules.Import(name);
    }
    private static string Instance(PyModuleExecute body, bool package = false)
    {
        string name = Unique();
        PyModules.RegisterInstanceSourceLocated(name, package ? name : "", "source.py", 1,
            "/virtual/source.py", package, body);
        return name;
    }
    private static PyDict Globals(PyModule module) => Assert.IsType<PyDict>(module.GetAttr("__dict__"));
    private static string Add(PyModule module)
    {
        string alias = Unique(); PyOps.SetItem(View(), alias, module); return alias;
    }

    [Fact]
    public void NativeAliasSharesIdentityAndCurrentAttributesWithoutReinitialization()
    {
        int runs = 0; PyModule source = Native(() => runs++); var marker = new PyList([]);
        source.SetAttr("value", marker); string alias = Add(source);
        Assert.Same(source, PyModules.Import(alias)); Assert.Equal(1, runs);
        Assert.Same(source, PyOps.GetItem(View(), alias)); Assert.Equal(source.Name, source.GetAttr("__name__"));
        Assert.Same(marker, PyModules.ImportAttr(alias, "value"));
        source.SetAttr("value", PyNone.Instance); Assert.Same(PyNone.Instance, PyModules.ImportAttr(alias, "value"));
        Assert.Same(source, PyModules.Import(source.Name)); Assert.Equal(1, runs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceAndInstanceModulesKeepOriginalMetadata(bool instance)
    {
        int runs = 0; string name;
        if (instance) name = Instance(_ => runs++);
        else
        {
            name = Unique();
            PyModules.RegisterSourceLocated(name, "", "source.py", 1, "/virtual/source.py", false, false,
                () => runs++, _ => throw new PyAttributeError("missing"), PyModules.SetDynamic);
        }
        PyModule source = PyModules.Import(name); object? spec = source.GetAttr("__spec__"), loader = source.GetAttr("__loader__");
        string alias = Add(source);
        Assert.Same(source, PyModules.Import(alias)); Assert.Equal(1, runs);
        Assert.Same(spec, Find(alias)); Assert.Same(loader, source.GetAttr("__loader__"));
        Assert.Equal(name, PyObject.GetAttr(spec, "name")); Assert.Equal(name, source.GetAttr("__name__"));
    }

    [Fact]
    public void UnexecutedPublicCloneStaysEmptyUntilExplicitLoaderExecution()
    {
        int runs = 0; string name = Instance(s => PyModuleInstances.StoreName(s, "runs", (long)++runs));
        object spec = Find(name)!; PyModule clone = Create(spec); var globals = Globals(clone);
        string alias = Add(clone);
        Assert.Same(clone, PyModules.Import(alias)); Assert.Same(spec, Find(alias));
        Assert.Equal(0, runs); Assert.False(globals.TryGet("__builtins__", out _));
        Assert.False(globals.TryGet("runs", out _)); Assert.False(PyOps.Contains(View(), name));
        Call(clone.GetAttr("__loader__")!, "exec_module", clone);
        Assert.Equal(1, runs); Assert.Equal(1L, clone.GetAttr("runs"));
        Assert.Same(globals, Globals(PyModules.Import(alias))); Assert.Same(clone, PyModules.Import(alias));
        var normal = PyModules.Import(name); Assert.NotSame(normal, clone); Assert.Equal(2, runs);
        Assert.Equal(1L, clone.GetAttr("runs")); Assert.Equal(2L, normal.GetAttr("runs"));
    }

    [Fact]
    public void EmptyModuleAndExplicitExecutionFailureKeepOriginalDictionaryAndCache()
    {
        int runs = 0; var failure = new PyValueError("execution failed");
        string name = Instance(s => { PyModuleInstances.StoreName(s, "runs", (long)++runs); throw failure; });
        PyModule clone = Create(Find(name)!); object loader = clone.GetAttr("__loader__")!;
        string alias = Add(clone); var globals = Globals(clone);
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Call(loader, "exec_module", clone)));
        Assert.Same(clone, PyModules.Import(alias)); Assert.Same(globals, Globals(clone)); Assert.Equal(1L, clone.GetAttr("runs"));
        Assert.False(PyOps.Contains(View(), name));
        PyModule blank = Empty(); var blankGlobals = Globals(blank); string blankAlias = Add(blank);
        Assert.Same(blank, PyModules.Import(blankAlias)); Assert.False(blankGlobals.TryGet("__builtins__", out _));
    }

    [Fact]
    public void InitializingAndEscapedFailedModulesCannotBePublishedUnderAliases()
    {
        string name = Unique(), alias = Unique(), escapedAlias = Unique(); int runs = 0; PyModule? escaped = null;
        var failure = new PyValueError("init failure");
        PyModules.Register(name, "", "source.py", () =>
        {
            runs++; escaped = PyModules.Import(name);
            Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), alias, escaped));
            Assert.False(PyOps.Contains(View(), alias));
            if (runs == 1) throw failure;
        }, _ => throw new PyAttributeError("missing"), PyModules.SetDynamic);
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyModules.Import(name)));
        PyModule failed = escaped!;
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), escapedAlias, failed));
        Assert.False(PyOps.Contains(View(), name)); Assert.False(PyOps.Contains(View(), escapedAlias));
        var successful = PyModules.Import(name); Assert.NotSame(failed, successful); Assert.Equal(2, runs);
        PyOps.SetItem(View(), alias, successful); Assert.Same(successful, PyModules.Import(alias));
        PyModules.Register(escapedAlias, "", "", () => { }, _ => 1L, PyModules.SetDynamic);
        Assert.Equal(1L, PyModules.Import(escapedAlias).GetAttr("value"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData(".leading")]
    [InlineData("trailing.")]
    [InlineData("part.child")]
    public void UnsupportedStringKeysFailBeforeMutation(string key)
    {
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), key, Empty()));
        Assert.False(PyOps.Contains(View(), key));
    }

    [Fact]
    public void NonStringKeysAndStringSubclassDoNotRunProtocols()
    {
        int calls = 0; var cls = Assert.IsType<PyClass>(PyOps.MakeClass("AliasKey", [PyType.Get("str")], null));
        PyOps.FinalizeOrdinaryClass(cls);
        cls.SetAttr("__str__", PyNativeFunctions.CreateVariadic("__str__", _ => { calls++; return "wrong"; }));
        object subclass = cls.Invoke([Unique()], [], [])!;
        var protocols = new PyClass("AliasProtocolKey");
        foreach (string method in new[] { "__hash__", "__eq__", "__str__", "__repr__", "__getattr__" })
            protocols.SetAttr(method, PyNativeFunctions.CreateVariadic(method, _ => { calls++; return 1L; }));
        var protocolKey = new PyInstance(protocols);
        var probe = new CallbackProbe(() => calls++); var module = Empty();
        foreach (object? key in new object?[] { null, PyNone.Instance, 1L, true, new PyList([]), new PyDict(), subclass, protocolKey, probe })
            Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), key, module));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void NonModuleValuesFailWithoutInspectingObjectsOrReservingKeys()
    {
        int calls = 0; var probe = new CallbackProbe(() => calls++);
        foreach (object? value in new object?[] { null, PyNone.Instance, 42L, "module", new PyList([]), new PyDict(), probe })
        {
            string alias = Unique(); Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), alias, value));
            Assert.False(PyOps.Contains(View(), alias));
            var module = Empty(); PyOps.SetItem(View(), alias, module); Assert.Same(module, PyModules.Import(alias));
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ExistingRegistrationsCachesAndAliasesRejectEvenSameObjectWrites()
    {
        var source = Native(); string pending = Unique(); int runs = 0;
        PyModules.Register(pending, "", "", () => runs++, _ => 5L, PyModules.SetDynamic);
        foreach (string key in new[] { pending, source.Name, Add(source) })
        {
            Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), key, source));
            Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), key, Empty()));
        }
        Assert.Equal(0, runs); Assert.Same(source, PyModules.Import(source.Name));
        Assert.Equal(5L, PyModules.Import(pending).GetAttr("value")); Assert.Equal(1, runs);
    }

    [Fact]
    public void RegisteredPackagePrefixesAreReservedButTextPrefixesAreNot()
    {
        var source = Empty(); string prefix = Unique(); int runs = 0;
        PyModules.Register(prefix + ".child.deep", prefix + ".child", "", () => runs++, _ => 1L, PyModules.SetDynamic);
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), prefix, source));
        Assert.False(PyOps.Contains(View(), prefix)); Assert.Equal(0, runs);
        string independent = Unique(); PyModules.RegisterNative(independent + "_suffix", new Dictionary<string, object?>());
        PyOps.SetItem(View(), independent, source); Assert.Same(source, PyModules.Import(independent));
    }

    [Fact]
    public void NonIdentifierAndOrdinalDistinctKeysRemainValid()
    {
        var source = Empty(); string prefix = Unique();
        foreach (string key in new[] { prefix + "-词", prefix + " with space", prefix + "\0", prefix.ToUpperInvariant(), prefix })
        { PyOps.SetItem(View(), key, source); Assert.Same(source, PyModules.Import(key)); }
    }

    [Fact]
    public void DeletionRejectsPresentAbsentAndCallbackKeysWithoutChangingCache()
    {
        var source = Empty(); string alias = Add(source); int calls = 0;
        foreach (object? key in new object?[] { alias, Unique(), new CallbackProbe(() => calls++) })
            Assert.Throws<PyNotImplementedError>(() => PyOps.DelItem(View(), key));
        Assert.Same(source, PyModules.Import(alias)); Assert.Equal(0, calls);
    }

    [Fact]
    public void SavedSettersAndSeparateViewsShareCacheWhileReadListsStaySnapshots()
    {
        var first = View(); var second = View(); var source = Empty(); string alias = Unique();
        object setter = first.GetAttr("__setitem__")!;
        var oldKeys = Assert.IsType<PyList>(Call(first, "keys")); var oldValues = Assert.IsType<PyList>(Call(first, "values"));
        Assert.Same(PyNone.Instance, PyOps.CallValue(setter, [alias, source], [], []));
        Assert.Same(source, PyOps.GetItem(second, alias)); Assert.True(PyOps.Contains(second, alias));
        Assert.False(PyOps.Contains(oldKeys, alias)); Assert.False(PyOps.Contains(oldValues, source));
        Assert.Contains(alias, PyOps.ToSequence(Call(second, "keys")));
        var pairs = PyOps.ToSequence(Call(second, "items")).Cast<PyTuple>();
        Assert.Same(source, pairs.Single(p => (string)p.Items[0]! == alias).Items[1]);
        Assert.Same(source, Call(second, "get", alias)); Assert.Same(PyNone.Instance, Call(first, "get", Unique()));
        Assert.Equal("mappingproxy", first.TypeName);
        Assert.Throws<PyAttributeError>(() => first.GetAttr("update")); Assert.Throws<PyAttributeError>(() => first.GetAttr("clear"));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(setter, [alias, source], [], []));
    }

    [Fact]
    public void MultipleAliasesKeepOneTargetAndDoNotIntroduceAnEvictionLimit()
    {
        int runs = 0; var source = Native(() => runs++); var aliases = new List<string>();
        for (int index = 0; index < 64; index++) aliases.Add(Add(source));
        foreach (string alias in aliases) Assert.Same(source, PyModules.Import(alias));
        Assert.Equal(1, runs);
    }

    private sealed class CallbackProbe(Action callback) : PyNativeObject
    {
        public override string TypeName { get { callback(); return "module"; } }
        public override object? GetAttr(string name) { callback(); throw new PyAttributeError(name); }
        public override string PythonRepr() { callback(); return "probe"; }
        public override string PythonStr() { callback(); return "probe"; }
    }
}
