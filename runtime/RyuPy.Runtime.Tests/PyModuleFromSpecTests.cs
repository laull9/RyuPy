// 公开创建/执行的CLR机制回归；实际Python正文、owner与生成CIL仍由完整多文件用例验收。
using System;
using System.Runtime.CompilerServices;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleFromSpecTests
{
    private static object Function => PyImportlibUtil.CreateAttributes()["module_from_spec"]!;
    private static string Register(PyModuleExecute body, bool package = false, string? name = null)
    {
        name ??= "public_instance_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, package ? name : "", "subject.py", 1,
            "/virtual/subject.py", package, body);
        return name;
    }
    private static object Spec(string name) => PyOps.CallValue(PyImportlibUtil.CreateAttributes()["find_spec"], [name], [], [])!;
    private static PyModule Create(object spec) => Assert.IsType<PyModule>(PyOps.CallValue(Function, [spec], [], []));
    private static PyDict Globals(PyModule m) => Assert.IsType<PyDict>(m.GetAttr("__dict__"));
    private static object Loader(object spec) => PyObject.GetAttr(spec, "loader")!;
    private static object? Exec(object loader, PyModule target) => PyOps.CallMethod(loader, "exec_module", [target], [], []);
    private static object EmptySpec(string name)
    {
        var type = PyModuleSpec.CreateAttributes()["ModuleSpec"];
        return PyOps.CallValue(type, [name, PyNone.Instance], [], [])!;
    }

    [Fact]
    public void CreationUsesOriginalMetadataAndOneEmptyUncachedDictionary()
    {
        int calls = 0; string name = Register(_ => calls++, true); object spec = Spec(name);
        var a = Create(spec); var b = Create(spec);
        Assert.Equal(0, calls); Assert.NotSame(Globals(a), Globals(b));
        Assert.False(Globals(a).TryGet("__builtins__", out _));
        Assert.Same(spec, a.GetAttr("__spec__")); Assert.Same(Loader(spec), a.GetAttr("__loader__"));
        Assert.Same(PyObject.GetAttr(spec, "submodule_search_locations"), a.GetAttr("__path__"));
        Assert.NotSame(spec, Spec(name));
        foreach (string field in new[] { "__name__", "__package__", "__spec__", "__loader__", "__file__", "__cached__", "__path__" })
        {
            a.SetAttr(field, 17L); Assert.Equal(17L, a.GetAttr(field)); Assert.Equal(17L, Globals(a).Get(field));
            PyObject.DelAttr(a, field); Assert.Throws<PyAttributeError>(() => a.GetAttr(field));
        }
        Assert.Throws<PyAttributeError>(() => a.SetAttr("__dict__", new PyDict()));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(a, "__dict__"));
    }

    [Fact]
    public void LoaderCodeWinsOverTargetCodeAndSurvivesRegistrationReplacement()
    {
        string name = Register(s => PyModuleInstances.StoreName(s, "result", "A"));
        object spec = Spec(name), loader = Loader(spec);
        Register(s => PyModuleInstances.StoreName(s, "result", "B"), name: name);
        var target = PyModuleInstances.Create(name); Exec(loader, target);
        Assert.Equal("A", target.GetAttr("result"));
        PyModuleInstances.Execute(target); Assert.Equal("B", target.GetAttr("result"));
        object emptySpec = EmptySpec(name); var empty = Create(emptySpec);
        Exec(loader, empty); Assert.Equal("A", empty.GetAttr("result"));
        Assert.Same(emptySpec, empty.GetAttr("__spec__")); Assert.Same(PyNone.Instance, empty.GetAttr("__loader__"));
    }

    [Fact]
    public void ReexecutionAndFailurePreserveDictionaryCacheAndOriginalException()
    {
        int runs = 0; var failure = new PyValueError("original");
        string name = Register(s => { PyModuleInstances.StoreName(s, "runs", (long)++runs); if (runs == 2) throw failure; });
        var cached = PyModules.Import(name); var clone = Create(cached.GetAttr("__spec__")!);
        var dict = Globals(clone); object loader = clone.GetAttr("__loader__")!;
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Exec(loader, clone)));
        Assert.Equal(2L, clone.GetAttr("runs")); Assert.Same(cached, PyModules.Import(name));
        clone.SetAttr("extra", "kept"); Assert.Same(PyNone.Instance, Exec(loader, clone));
        Assert.Same(dict, Globals(clone)); Assert.Equal(3L, clone.GetAttr("runs")); Assert.Equal("kept", clone.GetAttr("extra"));
        Assert.Equal(1L, cached.GetAttr("runs"));
    }

    [Fact]
    public void NameErrorsPrecedeBuiltinsAndNoneNameAdoptsLoaderName()
    {
        string name = Register(s => PyModuleInstances.StoreName(s, "ran", true)); object spec = Spec(name), loader = Loader(spec);
        var target = Create(spec); target.SetAttr("__name__", "wrong"); target.SetAttr("__builtins__", 42L);
        var error = Assert.Throws<PyImportError>(() => Exec(loader, target));
        Assert.Equal($"loader for {name} cannot handle wrong", error.Message); Assert.Equal("wrong", error.UserAttributes["name"]);
        PyObject.DelAttr(target, "__name__"); Assert.Throws<PyAttributeError>(() => Exec(loader, target));
        target.SetAttr("__name__", PyNone.Instance); Assert.Throws<PyNotImplementedError>(() => Exec(loader, target));
        PyObject.DelAttr(target, "__builtins__"); Exec(loader, target);
        Assert.Equal(true, target.GetAttr("ran")); Assert.Same(PyNone.Instance, target.GetAttr("__name__"));
    }

    [Fact]
    public void BindingUsesStableFunctionAndCorrectKeywordPriority()
    {
        Assert.Same(Function, Function); var spec = EmptySpec("blank");
        Assert.IsType<PyModule>(PyOps.CallValue(Function, [], ["spec"], [spec]));
        Assert.Equal("module_from_spec() missing 1 required positional argument: 'spec'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(Function, [], [], [])).Message);
        Assert.Equal("module_from_spec() got multiple values for argument 'spec'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(Function, [spec], ["spec"], [spec])).Message);
        Assert.Equal("module_from_spec() got an unexpected keyword argument 'other'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(Function, [spec, spec], ["other"], [spec])).Message);
    }

    [Fact]
    public void NoLocationDoesNotBorrowCompiledOriginAndSpecNameIsAuthoritative()
    {
        object spec = Spec(Register(_ => { })); PyObject.SetAttr(spec, "has_location", false);
        PyObject.SetAttr(spec, "name", "renamed"); var target = Create(spec);
        Assert.Equal("renamed", target.GetAttr("__name__")); Assert.Throws<PyAttributeError>(() => target.GetAttr("__file__"));
        Assert.Throws<PyAttributeError>(() => target.GetAttr("__cached__"));
        Assert.Throws<PyImportError>(() => Exec(Loader(spec), target));
    }

    [Fact]
    public void SourceMutationAndUnknownCallbacksFailWithoutInvokingThem()
    {
        object spec = Spec(Register(_ => { })), loader = Loader(spec); var target = Create(spec);
        PyObject.SetAttr(loader, "path", "/different.py"); Assert.Throws<PyNotImplementedError>(() => Exec(loader, target));
        Assert.False(Globals(target).TryGet("__builtins__", out _));
        PyObject.SetAttr(loader, "path", "/virtual/subject.py");
        int calls = 0; var callback = PyNativeFunctions.Create("custom", ["spec"], [null], 1, _ => { calls++; return PyNone.Instance; });
        PyObject.SetAttr(loader, "create_module", callback); Assert.Throws<PyNotImplementedError>(() => Create(spec));
        Assert.Equal(0, calls); PyObject.DelAttr(loader, "create_module");
        var cls = Assert.IsType<PyClass>(PyObject.TypeOf(spec)); object original = PyObject.GetAttr(cls, "parent")!;
        try { cls.SetAttr("parent", new PyProperty(callback, null)); Assert.Throws<PyNotImplementedError>(() => Create(spec)); Assert.Equal(0, calls); }
        finally { cls.SetAttr("parent", original); }
        Assert.IsType<PyModule>(Create(spec));
    }

    [Fact]
    public void SavedBoundMethodDoesNotRebindAfterClassMethodReplacement()
    {
        object spec = Spec(Register(s => PyModuleInstances.StoreName(s, "ran", true))), loader = Loader(spec);
        var target = Create(spec); object bound = PyObject.GetAttr(loader, "exec_module")!;
        var cls = Assert.IsType<PyClass>(PyObject.TypeOf(loader)); object original = PyObject.GetAttr(cls, "exec_module")!;
        try
        {
            cls.SetAttr("exec_module", PyNativeFunctions.Create("replacement", ["self", "module"], [null, null], 2, _ => 9L));
            Assert.Equal(9L, Exec(loader, target)); Assert.Throws<PyNotImplementedError>(() => Create(spec));
            PyOps.CallValue(bound, [target], [], []); Assert.Equal(true, target.GetAttr("ran"));
        }
        finally { cls.SetAttr("exec_module", original); }
    }

    [Fact]
    public void StaticSourcesRemainDiscoverableButCannotExecuteOrSupplyCopiedGlobals()
    {
        string name = "static_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterSourceLocated(name, "", "static.py", 1, "/virtual/static.py", false, false,
            () => { }, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        object spec = Spec(name); Assert.Throws<PyNotImplementedError>(() => Create(spec));
        var empty = Create(EmptySpec(name)); Assert.Throws<PyNotImplementedError>(() => Exec(Loader(spec), empty));
        Assert.False(Globals(empty).TryGet("__builtins__", out _));
        string instanceName = Register(_ => throw new Exception("must not execute")); object instanceLoader = Loader(Spec(instanceName));
        var staticTarget = PyModules.Import(name);
        // 静态目标错误名称仍先报告ImportError，不能用复制字典伪装实例执行。
        Assert.Throws<PyImportError>(() => Exec(instanceLoader, staticTarget));
    }

    [Fact]
    public void NamespaceAndCustomSpecsRemainExplicitlyPending()
    {
        object spec = EmptySpec("namespace"); PyObject.SetAttr(spec, "submodule_search_locations", new PyList([]));
        Assert.Throws<PyNotImplementedError>(() => Create(spec));
        var custom = new PyClass("CustomSpec"); int calls = 0;
        custom.SetAttr("loader", new PyProperty(PyNativeFunctions.Create("get", ["self"], [null], 1, _ => { calls++; return PyNone.Instance; }), null));
        Assert.Throws<PyNotImplementedError>(() => Create(new PyInstance(custom))); Assert.Equal(0, calls);
    }

    [Fact]
    public void KnownInvalidNamesAndMissingNameCarryExactExceptionMetadata()
    {
        object spec = Spec(Register(_ => { })); PyObject.SetAttr(spec, "name", 12L);
        Assert.Equal("module() argument 'name' must be str, not int", Assert.Throws<PyTypeError>(() => Create(spec)).Message);
        PyObject.SetAttr(spec, "name", PyNone.Instance);
        Assert.Equal("module() argument 'name' must be str, not NoneType", Assert.Throws<PyTypeError>(() => Create(spec)).Message);
        PyObject.SetAttr(spec, "name", "x"); var target = Create(spec); PyObject.DelAttr(target, "__name__");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var missing = Assert.Throws<PyAttributeError>(() => Exec(Loader(spec), target));
            Assert.Equal("__name__", missing.UserAttributes["name"]); Assert.Same(target, missing.UserAttributes["obj"]);
            Assert.Same(outer, missing.Context); Assert.Null(missing.Cause); Assert.False(missing.SuppressContextFlag);
            target.SetAttr("__name__", "wrong");
            var wrong = Assert.Throws<PyImportError>(() => Exec(Loader(spec), target));
            Assert.Same(outer, wrong.Context); Assert.Null(wrong.Cause); Assert.False(wrong.SuppressContextFlag);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void LoaderExecutionCapturesTheTargetsExistingBuiltinsByReference()
    {
        PyModule? target = null; object? actual = null; object? source = null;
        var builtins = new PyDict(); builtins.Set("answer", 1L);
        string name = Register(state =>
        {
            target!.SetAttr("__builtins__", new PyDict()); builtins.Set("answer", 2L);
            actual = PyModuleInstances.LoadName(state, "answer");
            source = PyModuleInstances.ExecutionSource(state);
        });
        object spec = Spec(name); target = Create(EmptySpec(name)); target.SetAttr("__builtins__", builtins);
        Exec(Loader(spec), target); Assert.Equal(2L, actual);
        target.SetAttr("__name__", "actual_target");
        long mark = PyTraceback.PushFrameWithOrigin("loader-code.py", "<module>", source);
        try { Assert.Equal("actual_target", PyTraceback.GetCallerModule()); }
        finally { PyTraceback.PopTo(mark); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Module, WeakReference Loader) MakeWeakClone(string name)
    {
        object spec = Spec(name); var module = Create(spec); module.SetAttr("cycle", module);
        return (new WeakReference(module), new WeakReference(Loader(spec)));
    }

    [Fact]
    public void WeakCapabilitiesDoNotKeepUncachedModulesOrLoadersAlive()
    {
        var weak = MakeWeakClone(Register(_ => { }));
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(weak.Module.IsAlive); Assert.False(weak.Loader.IsAlive);
    }

    [Fact]
    public void MatchingStaticTargetIsRejectedBeforeAnyBodyOrBuiltinsWrite()
    {
        string name = "static_target_" + Guid.NewGuid().ToString("N"); var fields = new PyDict();
        PyModules.RegisterSourceLocated(name, "", "static.py", 1, "/virtual/static.py", false, false,
            () => { }, key => fields.TryGet(key, out var value) ? value : throw new PyAttributeError(key),
            (_, key, value) => fields.Set(key, value));
        var target = PyModules.Import(name); int calls = 0;
        Register(_ => calls++, name: name);
        // 缓存仍指原静态对象；新的宿主实例具有新登记代码，其loader执行目标不能复制静态槽。
        object loader = PyModuleInstances.Create(name).GetAttr("__loader__")!;
        Assert.Throws<PyNotImplementedError>(() => Exec(loader, target));
        Assert.Equal(0, calls); Assert.False(fields.TryGet("__builtins__", out _)); Assert.Same(target, PyModules.Import(name));
    }

}
