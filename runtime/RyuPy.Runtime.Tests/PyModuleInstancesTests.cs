// CLR机制门禁：独立状态、真实字典、捕获builtins及弱引用；不代替编译Python端到端验收。
using System;
using System.Runtime.CompilerServices;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyModuleInstancesTests
{
    private static string Register(PyModuleExecute execute, bool package = false)
    {
        string name = "instances_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, package ? name : "", "instance.py", 1,
            "/virtual/instance.py", package, execute); return name;
    }
    private static PyDict Globals(PyModule m) => Assert.IsType<PyDict>(m.GetAttr("__dict__"));
    [Fact]
    public void InstancesAndRepeatedExecutionUseOneDictionary()
    {
        string name = Register(s => PyModuleInstances.StoreName(s, "items", new PyList([])));
        var a = PyModuleInstances.Create(name); var b = PyModuleInstances.Create(name);
        Assert.Throws<PyAttributeError>(() => a.GetAttr("items"));
        PyModuleInstances.Execute(a); PyModuleInstances.Execute(b);
        Assert.NotSame(Globals(a), Globals(b)); Assert.NotSame(a.GetAttr("items"), b.GetAttr("items"));
        Globals(a).Set("extra", 7L); a.SetAttr("other", 8L); Assert.Equal(8L, Globals(a).Get("other"));
        var dict = Globals(a); PyModuleInstances.Execute(a);
        Assert.Same(dict, Globals(a)); Assert.Equal(7L, a.GetAttr("extra"));
        Assert.Throws<PyAttributeError>(() => b.GetAttr("extra"));
        Assert.Throws<PyAttributeError>(() => a.SetAttr("__dict__", new PyDict()));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(a, "__dict__"));
        dict.Set("__dict__", "ordinary key"); Assert.Same(dict, a.GetAttr("__dict__"));
        Assert.Equal("ordinary key", dict.Get("__dict__"));
        Assert.Same(a.GetAttr("__builtins__"), b.GetAttr("__builtins__"));
    }
    [Fact]
    public void FailurePreservesPartialWritesAndRetryCreatesFreshState()
    {
        PyModule? old = null; int attempts = 0; string name = "";
        name = Register(s => { attempts++; PyModuleInstances.StoreName(s, "attempt", (long)attempts);
            if (attempts == 1) { old = PyModules.Import(name); throw new PyValueError("fail"); } });
        Assert.Throws<PyValueError>(() => PyModules.Import(name)); var fresh = PyModules.Import(name);
        Assert.NotSame(old, fresh); Assert.Equal(1L, old!.GetAttr("attempt")); Assert.Equal(2L, fresh.GetAttr("attempt"));
        var direct = PyModuleInstances.Create(Register(s => { PyModuleInstances.StoreName(s, "partial", 3L); throw new PyValueError("direct"); }));
        Assert.Throws<PyValueError>(() => PyModuleInstances.Execute(direct)); Assert.Equal(3L, direct.GetAttr("partial"));
    }
    [Fact]
    public void GlobalsShadowBuiltinsAndMissingNamesFail()
    {
        object? result = null; string key = "len";
        var m = PyModuleInstances.Create(Register(s => result = PyModuleInstances.LoadName(s, key)));
        PyModuleInstances.Execute(m); Assert.Same(PyOps.BuiltinFunction("len"), result);
        m.SetAttr("len", PyNone.Instance); PyModuleInstances.Execute(m); Assert.Same(PyNone.Instance, result);
        PyObject.DelAttr(m, "len"); PyModuleInstances.Execute(m); Assert.Same(PyOps.BuiltinFunction("len"), result);
        key = "int"; PyModuleInstances.Execute(m); Assert.Same(PyOps.TypeObject("int"), result);
        key = "ValueError"; PyModuleInstances.Execute(m); Assert.Same(PyOps.TypeObject("ValueError"), result);
        key = "IOError"; PyModuleInstances.Execute(m); Assert.Same(PyOps.TypeObject("OSError"), result);
        key = "definitely_not_builtin"; Assert.Throws<PyNameError>(() => PyModuleInstances.Execute(m));
        Assert.Throws<PyAttributeError>(() => m.GetAttr(key)); Assert.Throws<PyKeyError>(() => Globals(m).Get(key));
    }
    [Fact]
    public void ReentrantExecutionCapturesBuiltinsPerFrame()
    {
        PyModule? m = null; int depth = 0; object? outer = null; object? inner = null;
        var original = new PyDict(); original.Set("answer", 1L); var replacement = new PyDict(); replacement.Set("answer", 2L);
        string name = Register(s => { if (depth++ == 0) { m!.SetAttr("__builtins__", replacement); PyModuleInstances.Execute(m);
            outer = PyModuleInstances.LoadName(s, "answer"); } else inner = PyModuleInstances.LoadName(s, "answer"); depth--; });
        m = PyModuleInstances.Create(name); m.SetAttr("__builtins__", original); PyModuleInstances.Execute(m);
        Assert.Equal(1L, outer); Assert.Equal(2L, inner); m.SetAttr("__builtins__", 42L);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Execute(m));
    }
    [Fact]
    public void MetadataAndSourceBelongToTheActualInstance()
    {
        object? source = null; string name = Register(s => source = PyModuleInstances.ExecutionSource(s), true);
        var a = PyModuleInstances.Create(name); var b = PyModuleInstances.Create(name);
        var spec = a.GetAttr("__spec__"); Assert.NotSame(spec, b.GetAttr("__spec__"));
        Assert.Same(a.GetAttr("__path__"), PyObject.GetAttr(spec, "submodule_search_locations"));
        Assert.Same(a.GetAttr("__loader__"), PyObject.GetAttr(spec, "loader"));
        a.SetAttr("__name__", "renamed"); a.SetAttr("__file__", "changed"); PyModuleInstances.Execute(a);
        Assert.Same(spec, a.GetAttr("__spec__")); Assert.Equal("changed", a.GetAttr("__file__"));
        long mark = PyTraceback.PushFrameWithOrigin("instance.py", "<module>", source);
        try { Assert.Equal("renamed", PyTraceback.GetCallerModule()); } finally { PyTraceback.PopTo(mark); }
    }
    [Fact]
    public void SharedDefaultBuiltinsIsTheActualFallbackStorage()
    {
        string key = "test_builtin_" + Guid.NewGuid().ToString("N"); object? result = null;
        string name = Register(s => result = PyModuleInstances.LoadName(s, key));
        var a = PyModuleInstances.Create(name); var b = PyModuleInstances.Create(name);
        var builtins = Assert.IsType<PyDict>(a.GetAttr("__builtins__"));
        try
        {
            builtins.Set(key, 91L); PyModuleInstances.Execute(b); Assert.Equal(91L, result);
            b.SetAttr("__builtins__", new PyDict());
            Assert.Throws<PyNameError>(() => PyModuleInstances.Execute(b));
        }
        finally { builtins.Delete(key); }
    }
    [Fact]
    public void RegistrationReplacementDoesNotRetargetExistingCode()
    {
        string name = Register(s => PyModuleInstances.StoreName(s, "version", 1L));
        var old = PyModuleInstances.Create(name);
        PyModules.RegisterInstanceSourceLocated(name, "", "new.py", 1, "/virtual/new.py", false,
            s => PyModuleInstances.StoreName(s, "version", 2L));
        var fresh = PyModuleInstances.Create(name); PyModuleInstances.Execute(old); PyModuleInstances.Execute(fresh);
        Assert.Equal(1L, old.GetAttr("version")); Assert.Equal(2L, fresh.GetAttr("version"));
        Assert.Equal("/virtual/instance.py", old.GetAttr("__file__"));
    }
    [Fact]
    public void LegacyInstancesAndInvalidContextsAreRejectedBeforeExecution()
    {
        string name = "legacy_instance_" + Guid.NewGuid().ToString("N");
        PyModules.Register(name, "", "", () => { }, _ => throw new PyAttributeError("missing"), PyModules.SetDynamic);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Create(name));
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Execute(PyModules.Import(name)));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(PyModules.Import(name), "x"));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.LoadName(new object(), "x"));
    }
    [Fact]
    public void CrossInstanceReentryKeepsExplicitState()
    {
        PyModule? a = null; PyModule? b = null; int phase = 0;
        string name = Register(s => {
            if (phase == 0) { phase = 1; PyModuleInstances.Execute(b!); }
            else if (phase == 1) { phase = 2; PyModuleInstances.Execute(a!); }
            PyModuleInstances.StoreName(s, "observed", PyModuleInstances.LoadName(s, "identity"));
        });
        a = PyModuleInstances.Create(name); b = PyModuleInstances.Create(name);
        a.SetAttr("identity", "a"); b.SetAttr("identity", "b"); PyModuleInstances.Execute(a);
        Assert.Equal("a", a.GetAttr("observed")); Assert.Equal("b", b.GetAttr("observed"));
    }
    [Fact]
    public void ReboundMetadataIsReadByReferenceAndNeverResetOnExecution()
    {
        object? observed = null;
        string name = Register(s => observed = PyModuleInstances.LoadName(s, "__spec__"));
        var module = PyModuleInstances.Create(name); var replacement = new PyList([]);
        module.SetAttr("__spec__", replacement); PyModuleInstances.Execute(module);
        Assert.Same(replacement, observed); Assert.Same(replacement, module.GetAttr("__spec__"));
        Globals(module).Delete("__spec__");
        Assert.Throws<PyAttributeError>(() => module.GetAttr("__spec__"));
        Assert.Throws<PyNameError>(() => PyModuleInstances.Execute(module));
    }
    [Fact]
    public void FailedImportDoesNotRemoveAnExplicitReplacementCacheEntry()
    {
        // 公开sys.modules仍只读；反射只用于验证宿主缓存替换时的失败清理身份门禁。
        var cache = (System.Collections.IDictionary)typeof(PyModules).GetField("Cache",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        var replacement = PyModuleInstances.Create(Register(_ => { })); string name = "";
        name = Register(_ => { cache[name] = replacement; throw new PyValueError("replace then fail"); });
        try
        {
            Assert.Throws<PyValueError>(() => PyModules.Import(name));
            Assert.Same(replacement, PyModules.Import(name));
        }
        finally { cache.Remove(name); }
    }
    [Fact]
    public void ExecutionRestoresMissingBuiltinsAsSharedObservableDictionary()
    {
        object? observed = null;
        string name = Register(s => observed = PyModuleInstances.LoadName(s, "__builtins__"));
        var module = PyModuleInstances.Create(name); var other = PyModuleInstances.Create(name);
        var globals = Globals(module); globals.Delete("__builtins__");
        Assert.False(globals.TryGet("__builtins__", out _));
        PyModuleInstances.Execute(module);
        Assert.Same(other.GetAttr("__builtins__"), globals.Get("__builtins__"));
        Assert.Same(globals.Get("__builtins__"), observed);
        module.SetAttr("__builtins__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Execute(module));
        Assert.Same(PyNone.Instance, globals.Get("__builtins__"));
    }
    [Fact]
    public void UnsupportedDefaultBuiltinDoesNotHideInjectedOrCustomBindings()
    {
        object? observed = null; string key = "complex";
        var module = PyModuleInstances.Create(Register(s => observed = PyModuleInstances.LoadName(s, key)));
        module.SetAttr("complex", 17L); PyModuleInstances.Execute(module); Assert.Equal(17L, observed);
        Globals(module).Delete("complex");
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Execute(module));
        module.SetAttr("__builtins__", new PyDict());
        Assert.Throws<PyNameError>(() => PyModuleInstances.Execute(module));
        key = "__doc__"; Globals(module).Delete(key);
        Assert.Throws<PyNameError>(() => PyModuleInstances.Execute(module));
        module.SetAttr("__builtins__", PyModuleInstances.Create(Register(_ => { })).GetAttr("__builtins__"));
        Assert.Throws<PyNameError>(() => PyModuleInstances.Execute(module));
    }
    [Fact]
    public void MissingAttributeMessagesUseCurrentExactStringName()
    {
        var module = PyModuleInstances.Create(Register(_ => { }));
        module.SetAttr("__name__", "renamed");
        Assert.Equal("module 'renamed' has no attribute 'missing'", Assert.Throws<PyAttributeError>(() => module.GetAttr("missing")).Message);
        Assert.Equal("module 'renamed' has no attribute 'missing'", Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(module, "missing")).Message);
        module.SetAttr("__name__", new PyList([]));
        Assert.Equal("module has no attribute 'missing'", Assert.Throws<PyAttributeError>(() => module.GetAttr("missing")).Message);
        Globals(module).Delete("__name__");
        Assert.Equal("module has no attribute 'missing'", Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(module, "missing")).Message);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateUncached(string name)
    { var m = PyModuleInstances.Create(name); Globals(m).Set("cycle", m); PyModuleInstances.Execute(m); return new WeakReference(m); }
    [Fact]
    public void RegistryDoesNotKeepInstancesAlive()
    {
        var weak = CreateUncached(Register(_ => { })); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Assert.False(weak.IsAlive);
    }
}
