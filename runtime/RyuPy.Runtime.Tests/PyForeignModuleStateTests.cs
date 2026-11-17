// 冷态模块行为门：模拟独立外来字典的外部写入，验证真实别名、旧函数捕获及一次性执行事务。
// FakeNamespace不用PyDict镜像；Runtime只能通过中性协议读写，外部可直接改变权威存储。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyForeignModuleStateTests
{
    private static object Code(PyModuleExecute execute)
    {
        string name = "foreign_state_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "foreign.py", 1, "/virtual/foreign.py", false, execute);
        return PyModuleInstances.CaptureRegisteredCode(name);
    }
    private static PyFunction Function(object context, string name = "read") => PyModuleInstances.CreateOwnedFunction(
        PyModuleInstances.CaptureDefinitionContext(context), name, PySignature.Create(name, [], [], []),
        (state, _, _, _, _) => PyModuleInstances.LoadName(state, "answer"), [], false, null);
    private static object? Call(PyFunction function) => PyOps.CallValue(function, [], [], []);
    private static FakeNamespace Builtins(long answer)
    {
        var values = new FakeNamespace();
        foreach (var (key, value) in PyModuleInstances.CreateCanonicalBuiltins().Entries()) values.External[(string)key!] = value;
        values.External["answer"] = answer;
        return values;
    }

    [Fact]
    public void NamespaceViewAndExternalWritesAreTheActualGlobalEnvironment()
    {
        object? context = null;
        var globals = new FakeNamespace(); var builtins = Builtins(7);
        globals.External["__name__"] = "original"; globals.External["__builtins__"] = builtins;
        var module = PyModuleInstances.BindForeign(Code(s => { context = s; PyModuleInstances.StoreName(s, "published", 3L); }), globals, new ForeignModule());
        Assert.False(globals.External.ContainsKey("published"));
        PyModuleInstances.ExecuteBound(module);
        Assert.Equal(3L, globals.External["published"]);
        Assert.Same(globals, module.GetAttr("__dict__"));
        globals.External["answer"] = 11L;
        Assert.Equal(11L, PyModuleInstances.LoadName(context!, "answer"));
        PyModuleInstances.DeleteName(context!, "answer");
        Assert.False(globals.External.ContainsKey("answer"));
        Assert.Equal(7L, PyModuleInstances.LoadName(context!, "answer"));
        module.SetAttr("write", 17L); Assert.Equal(17L, globals.External["write"]);
        PyObject.DelAttr(module, "write"); Assert.False(globals.External.ContainsKey("write"));
        globals.External["__name__"] = "renamed";
        Assert.Equal("module 'renamed' has no attribute 'missing'", Assert.Throws<PyAttributeError>(() => module.GetAttr("missing")).Message);
        var function = Function(context!);
        Assert.Same(globals, PyObject.GetAttr(function, "__globals__"));
        Assert.Same(builtins, PyObject.GetAttr(function, "__builtins__"));
        Assert.Same(globals, PyModuleInstances.CallDirectGlobals(context!, builtins.External["globals"]));
    }

    [Fact]
    public void ExistingFunctionsCaptureBuiltinsWhileNewDefinitionsReadCurrentKey()
    {
        object? context = null; var globals = new FakeNamespace(); var first = Builtins(1); var second = Builtins(2);
        globals.External["__builtins__"] = first;
        var module = PyModuleInstances.BindForeign(Code(s => context = s), globals, new ForeignModule());
        PyModuleInstances.ExecuteBound(module);
        var old = Function(context!);
        globals.External["__builtins__"] = second;
        var fresh = Function(context!);
        Assert.Equal(1L, Call(old)); Assert.Equal(2L, Call(fresh));
        first.External["answer"] = 9L;
        Assert.Equal(9L, Call(old)); Assert.Equal(2L, Call(fresh));
        globals.External.Remove("__builtins__");
        Assert.Same(first, PyObject.GetAttr(Function(context!), "__builtins__"));
        Assert.False(globals.External.ContainsKey("__builtins__"));
    }

    [Fact]
    public void ForeignBuiltinsGuardsReadInPlaceReplacementBeforeAnyHookRuns()
    {
        object? context = null; var globals = new FakeNamespace(); var builtins = Builtins(1);
        globals.External["__builtins__"] = builtins;
        PyModuleInstances.ExecuteBound(PyModuleInstances.BindForeign(Code(s => context = s), globals, new ForeignModule()));
        PyModuleInstances.ImportGuard(context!); PyModuleInstances.ClassGuard(context!);
        bool called = false;
        var custom = PyNativeFunctions.CreateVariadic("hook", _ => { called = true; return PyNone.Instance; });
        builtins.External["__import__"] = custom;
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ImportGuard(context!));
        builtins.External["__build_class__"] = custom;
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ClassGuard(context!));
        Assert.False(called);
        builtins.External.Remove("__import__");
        Assert.Throws<PyImportError>(() => PyModuleInstances.ImportGuard(context!));
        builtins.External.Remove("__build_class__");
        Assert.Throws<PyNameError>(() => PyModuleInstances.ClassGuard(context!));
    }

    [Fact]
    public void FailedBindingPreservesOldFunctionAndNewBindingHasFreshIdentity()
    {
        int attempts = 0; PyFunction? old = null;
        object code = Code(s => { attempts++; PyModuleInstances.StoreName(s, "partial", (long)attempts); old ??= Function(s); throw new PyValueError("body"); });
        var first = new FakeNamespace(); first.External["__builtins__"] = Builtins(1);
        var foreign = new ForeignModule(); var module = PyModuleInstances.BindForeign(code, first, foreign);
        object token = PyModuleInstances.CaptureBinding(module);
        Assert.Throws<PyValueError>(() => PyModuleInstances.ExecuteBound(module));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.ExecuteBound(module));
        Assert.Equal(1, attempts); Assert.Equal(1L, first.External["partial"]);
        Assert.Equal(1L, Call(old!));
        Assert.Same(module, PyModuleInstances.RestoreForeign(token, foreign));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.RestoreForeign(token, new ForeignModule()));
        var second = new FakeNamespace(); second.External["__builtins__"] = Builtins(2);
        var fresh = PyModuleInstances.BindForeign(code, second, new ForeignModule());
        Assert.NotSame(module, fresh); Assert.NotSame(token, PyModuleInstances.CaptureBinding(fresh));
        Assert.Throws<PyValueError>(() => PyModuleInstances.ExecuteBound(fresh));
        Assert.Equal(2L, second.External["partial"]); Assert.Equal(1L, Call(old!));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.BindForeign(new object(), second, new ForeignModule()));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.RestoreForeign(new object(), foreign));
    }

    [Fact]
    public void BoundExecutionIsOnceAndFrozenCodeDoesNotFollowReregistration()
    {
        int runs = 0; PyModule? module = null;
        object code = Code(_ => { runs++; Assert.Equal(1, module!.State); PyModuleInstances.ExecuteBound(module);
            Assert.Equal(1, module.State); });
        var globals = new FakeNamespace(); globals.External["__builtins__"] = Builtins(1);
        module = PyModuleInstances.BindForeign(code, globals, new ForeignModule());
        PyModuleInstances.ExecuteBound(module); PyModuleInstances.ExecuteBound(module);
        Assert.Equal(1, runs);
        var unbound = PyProvider.Module("unbound", _ => throw new PyAttributeError("missing"), (_, _, _) => { });
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.CaptureBinding(unbound));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.ExecuteBound(unbound));
    }

    [Fact]
    public void CapturedCodeSurvivesSameNameRegistrationReplacement()
    {
        string name = "frozen_code_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "old.py", 1, "/virtual/old.py", false,
            context => PyModuleInstances.StoreName(context, "body", "old"));
        object old = PyModuleInstances.CaptureRegisteredCode(name);
        PyModules.RegisterInstanceSourceLocated(name, "", "new.py", 1, "/virtual/new.py", false,
            context => PyModuleInstances.StoreName(context, "body", "new"));
        var first = new FakeNamespace(); first.External["__builtins__"] = Builtins(1);
        var second = new FakeNamespace(); second.External["__builtins__"] = Builtins(2);
        var foreign = new ForeignModule();
        var module = PyModuleInstances.BindForeign(old, first, foreign);
        Assert.Same(module, PyModuleInstances.BindForeign(old, first, foreign));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.BindForeign(old, second, foreign));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), first, foreign));
        PyModuleInstances.ExecuteBound(module);
        PyModuleInstances.ExecuteBound(PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), second, new ForeignModule()));
        Assert.Equal("old", first.External["body"]); Assert.Equal("new", second.External["body"]);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference Peer, WeakReference State, PyFunction Function, ForeignModule Foreign) ReleasePeer()
    {
        var globals = new FakeNamespace(); globals.External["__builtins__"] = Builtins(6);
        var foreign = new ForeignModule();
        var module = PyModuleInstances.BindForeign(Code(static state => PyModuleInstances.StoreName(state, "reader", Function(state))), globals, foreign);
        PyModuleInstances.ExecuteBound(module);
        return (new WeakReference(module), new WeakReference(PyModuleInstances.CaptureBinding(module)),
            (PyFunction)module.GetAttr("reader")!, foreign);
    }

    [Fact]
    public void OldFunctionRestoresCollectedPeerThroughTheOriginalStateToken()
    {
        var old = ReleasePeer();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(old.Peer.IsAlive); Assert.True(old.State.IsAlive);
        var peer = PyModuleInstances.RestoreForeign(old.State.Target!, old.Foreign);
        Assert.Same(old.Function, peer.GetAttr("reader")); Assert.Equal(6L, Call(old.Function));
        Assert.Same(PyObject.GetAttr(old.Function, "__globals__"), peer.GetAttr("__dict__"));
        Assert.Same(old.State.Target, PyModuleInstances.CaptureBinding(peer));
        PyModuleInstances.ExecuteBound(peer); Assert.Same(old.Function, peer.GetAttr("reader"));
    }

    [Fact]
    public void TracebackAndCallerSourceReadTheSameLiveForeignView()
    {
        object? context = null;
        var globals = new FakeNamespace(); globals.External["__name__"] = "before"; globals.External["__builtins__"] = Builtins(1);
        var module = PyModuleInstances.BindForeign(Code(s => context = s), globals, new ForeignModule());
        PyModuleInstances.ExecuteBound(module);
        object codeSource = PyTraceback.CreateCodeSource("frozen_name", "/virtual/foreign.py", null);
        object origin = PyTraceback.CreateModuleExecutionSourceWithCode(codeSource, context);
        long depth = PyTraceback.PushFrameWithOrigin("/virtual/foreign.py", "<module>", origin);
        try
        {
            var selected = PyTraceback.CallerSource(1);
            Assert.Same(globals, selected.Environment);
            globals.External["__name__"] = "after";
            Assert.True(selected.TryReadName(out var current)); Assert.Equal("after", current);
            Assert.Equal("after", PyTraceback.GetCallerModule());
            globals.External.Remove("__name__"); Assert.False(selected.TryReadName(out _));
            Assert.Same(PyNone.Instance, PyTraceback.GetCallerModule());
        }
        finally { PyTraceback.PopTo(depth); }
    }

    internal sealed class FakeNamespace : IPyModuleNamespace
    {
        internal readonly Dictionary<string, object?> External = new(StringComparer.Ordinal);
        public object ViewObject => this;
        public bool TryGet(string name, out object? value) => External.TryGetValue(name, out value);
        public void Set(string name, object? value) => External[name] = value;
        public bool DeleteIfPresent(string name) => External.Remove(name);
    }
    internal class ForeignModule : IPyForeignObject
    {
        public virtual object ForeignType => PyType.Get("module");
        public object? GetAttr(string name) => throw new PyAttributeError(name);
        public void SetAttr(string name, object? value) => throw new NotSupportedException();
        public object? Invoke(object?[] args, string[] names, object?[] values) => throw new NotSupportedException();
        public object ReadScalar(string kind) => throw new NotSupportedException();
        public object? Item(object? key) => throw new NotSupportedException();
        public void WriteItem(object? key, object? value, bool deleting) => throw new NotSupportedException();
        public IPyIterator Iterate() => throw new NotSupportedException();
        public long Length() => throw new NotSupportedException();
        public bool Truth() => true;
        public long Hash() => throw new NotSupportedException();
        public object Binary(int operation, object? other, bool reverse) => throw new NotSupportedException();
        public bool Compare(int operation, object? other, bool reverse) => throw new NotSupportedException();
        public object RichCompare(int operation, object? other, bool reverse) => throw new NotSupportedException();
        public object Unary(int operation) => throw new NotSupportedException();
        public bool IsInstanceOf(object type) => false;
        public bool Accepts(object? value) => false;
        public bool IsExceptionClass => false;
        public bool MatchesException(PyBaseException error) => false;
        public PyStackFrame[] TracebackFrames() => [];
        public void SynchronizeException(PyBaseException error, bool write) => throw new NotSupportedException();
    }
}
