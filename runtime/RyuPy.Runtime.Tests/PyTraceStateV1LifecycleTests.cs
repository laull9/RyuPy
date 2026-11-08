// 有界 V1 行为门：fixture 不自动 Enter，不增加生产计数/重置接口；手工输入不代替真实产品差分。
#if !TRACE_ADAPTER_TEST
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTraceStateV1LifecycleTests : IDisposable
{
    private readonly List<PyBaseException> errors = new();
    public PyTraceStateV1LifecycleTests()
    {
        // 必须先观察，不能用清理掩盖前一个执行域的泄漏。
        Assert.Null(PyTraceProduct.Active);
        PyTraceback.Reset(); PyErrors.LeaveHandler(null); PyAtexit.Clear();
        PySystem.Initialize("trace-v1-lifecycle.py", []);
        Assert.Null(PyTraceProduct.Active);
    }
    private PyTypeError Error(string text = "source")
    {
        var error = new PyTypeError(text); errors.Add(error); return error;
    }
    private long Push(string name, int line)
    {
        long depth = PyTraceback.PushFrame("trace-v1-lifecycle.py", name); PyTraceback.SetLine(line); return depth;
    }
    public void Dispose()
    {
        foreach (var error in errors)
        {
            if (error.TraceProductBinding is not { } binding || !ReferenceEquals(binding.Scope, PyTraceProduct.Active)) continue;
            if (binding.Owner.Snapshot.Park is { } park) PyTraceProduct.Abort(park);
            if (binding.Owner.Snapshot.PendingCount is not null) PyTraceProduct.Suppress(error);
        }
        PyTraceback.Reset(); PyErrors.LeaveHandler(null); PyAtexit.Clear();
        if (PyTraceProduct.Enabled) PyTraceProduct.Exit();
    }

    [Fact]
    public void DefaultRuntimeNeverBindsTraceWithoutEnter()
    {
        long module = Push("module", 1); var error = Error();
        Assert.Null(error.TraceProductBinding); Assert.Same(error, PyTraceProduct.NativeFresh(error));
        PyTraceback.Attach(error);
        Assert.Equal("module", Assert.Single(error.Frames).Function);
        Assert.Null(error.TraceProductBinding); Assert.Null(PyTraceProduct.Active);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Explicit(Error("no domain")));
        var local = Assert.Throws<PyUnboundLocalError>(() => PyErrors.Load(null, "missing"));
        var global = Assert.Throws<PyNameError>(() => PyOps.CheckGlobalBound(false, "__trace_v1_unbound_name__"));
        var io = Assert.Throws<PyOSError>(() => PyIO.Guard(() => throw new System.IO.IOException("default")));
        foreach (var e in new PyBaseException[] { local, global, io }) Assert.Null(e.TraceProductBinding);
        var frame = new object?[] { 0L, null, null, null };
        var generator = new PyGenerator(value =>
        {
            var state = (object?[])value;
            Assert.Null(PyTraceProduct.Active);
            if ((long)state[0]! == 0) { state[0] = 1L; return 7L; }
            state[0] = -1L; return 8L;
        }, frame, "trace-v1-lifecycle.py", "default-generator");
        Assert.Equal(7L, generator.Next(null, false));
        var stop = Assert.Throws<PyStopIteration>(() => generator.Next(null, false));
        Assert.Null(stop.TraceProductBinding);
        PyTraceback.PopTo(module);
        Assert.Equal(0, Builtins.FinishExit(0)); Assert.Null(PyTraceProduct.Active);
        Assert.All(errors, e => Assert.Null(e.TraceProductBinding));
    }

    [Fact]
    public void FramesAreDetachedReverseProjectionOfLatestOwnerHistory()
    {
        PyTraceProduct.Enter(1); Push("module", 1); long leaf = Push("leaf", 2);
        var error = Error(); Assert.Same(error, PyTraceProduct.Explicit(error));
        var owner = error.TraceProductBinding!.Owner;
        PyTraceProduct.Handle(error, leaf); var first = owner.Snapshot;
        var frames = error.Frames; Assert.Equal("leaf", Assert.Single(frames).Function);
        frames[0] = new("changed", "changed", 99);
        Assert.Same(first, owner.Snapshot); Assert.Equal("leaf", Assert.Single(error.Frames).Function);
        Assert.NotSame(error.Frames, error.Frames);
        PyTraceback.PopTo(leaf); PyTraceback.SetLine(3);
        PyTraceProduct.Explicit(error); PyTraceProduct.Terminal(error);
        Assert.Equal(new[] { "leaf", "module" }, error.Frames.Select(f => f.Function));
        Assert.Equal(new[] { 2, 3 }, error.Frames.Select(f => f.Line));
        Assert.Equal(new[] { "module", "leaf" }, owner.Snapshot.History.Select(f => f.Function));
        Assert.Equal(new[] { "leaf" }, first.History.Select(f => f.Function));
    }

    [Fact]
    public void FramesSetterRejectsActiveOrPreviouslyBoundHistory()
    {
        var legacy = Error("legacy"); var original = new[] { new PyStackFrame("legacy.py", "legacy", 9) };
        legacy.Frames = original; Assert.Same(original, legacy.Frames);
        PyTraceProduct.Enter(1); var unbound = Error("unbound");
        Assert.Throws<InvalidOperationException>(() => unbound.Frames = original);
        Assert.Null(unbound.TraceProductBinding); Assert.Empty(unbound.Frames);
        Push("module", 1); var bound = Error("bound"); PyTraceProduct.Explicit(bound); PyTraceProduct.Terminal(bound);
        var owner = bound.TraceProductBinding!.Owner; var before = owner.Snapshot;
        Assert.Throws<InvalidOperationException>(() => bound.Frames = original); Assert.Same(before, owner.Snapshot);
        Assert.Equal(0, Builtins.FinishExit(0)); Assert.Null(PyTraceProduct.Active);
        Assert.Throws<InvalidOperationException>(() => bound.Frames = original); Assert.Same(before, owner.Snapshot);
        Assert.Equal("module", Assert.Single(bound.Frames).Function);
        legacy.Frames = []; Assert.Empty(legacy.Frames);
    }

    [Fact]
    public void EnterIsOnlyDomainStartAndRejectsInvalidRoots()
    {
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(2)); Assert.Null(PyTraceProduct.Active);
        Push("not a root", 1);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(1)); Assert.Null(PyTraceProduct.Active);
        PyTraceback.Reset();
        PyModules.Import("sys").SetAttr("stdout", PyNone.Instance);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(1)); Assert.Null(PyTraceProduct.Active);
        PySystem.Initialize("trace-v1-lifecycle.py", []);
        var callback = PyNativeFunctions.Create("noop", [], [], 0, _ => PyNone.Instance);
        PyOps.CallValue(PyAtexit.CreateAttributes()["register"], [callback], [], []);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(1)); Assert.Null(PyTraceProduct.Active);
        PyAtexit.Clear();
        var unbound = Error(); Assert.Null(unbound.TraceProductBinding);
        PyTraceProduct.Enter(1); var scope = PyTraceProduct.Active;
        Assert.NotNull(scope); Assert.Null(unbound.TraceProductBinding);
        Assert.Equal(0L, scope.Flights); Assert.Equal(0L, scope.Parks); Assert.Empty(scope.Generators);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(1));
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(2)); Assert.Same(scope, PyTraceProduct.Active);
    }

    [Fact]
    public void ExitRejectsReachableUnfinishedStateAndFinishExitClearsScope()
    {
        PyTraceProduct.Enter(1); long module = Push("module", 1); var scope = PyTraceProduct.Active;
        var error = Error(); PyTraceProduct.Explicit(error); var owner = error.TraceProductBinding!.Owner;
        var before = owner.Snapshot;
        Assert.Throws<InvalidOperationException>(PyTraceProduct.Exit); Assert.Same(scope, PyTraceProduct.Active); Assert.Same(before, owner.Snapshot);
        PyTraceProduct.Terminal(error);
        var parked = Error("parked"); PyTraceProduct.Explicit(parked); var park = PyTraceProduct.Park(parked, module);
        before = parked.TraceProductBinding!.Owner.Snapshot;
        Assert.Throws<InvalidOperationException>(PyTraceProduct.Exit); Assert.Same(scope, PyTraceProduct.Active);
        Assert.Same(before, parked.TraceProductBinding.Owner.Snapshot); PyTraceProduct.Match(park);
        var frame = new object?[] { 0L, null, null, null };
        var generator = new PyGenerator(value =>
        {
            Assert.Single(scope!.Generators); Assert.Throws<InvalidOperationException>(PyTraceProduct.Exit);
            Assert.Same(scope, PyTraceProduct.Active); ((object?[])value)[0] = -1L; return PyNone.Instance;
        }, frame, "trace-v1-lifecycle.py", "exit-guard");
        Assert.False(generator.MoveNext()); Assert.Empty(scope!.Generators);
        Assert.Equal(0, Builtins.FinishExit(0)); Assert.Null(PyTraceProduct.Active); Assert.Equal(0L, PyTraceback.CaptureDepth());
        Assert.Null(PyErrors.Current); Assert.NotEmpty(error.Frames);
        PyTraceProduct.Enter(1); Assert.NotSame(scope, PyTraceProduct.Active);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Explicit(error));
        Push("second module", 2); var second = Error("second domain"); PyTraceProduct.Explicit(second);
        PyTraceProduct.Terminal(second); PyErrors.EnterHandler(second);
        Assert.Equal(1, Builtins.FinishExit(1)); Assert.Null(PyTraceProduct.Active);
        Assert.Equal(0L, PyTraceback.CaptureDepth()); Assert.Null(PyErrors.Current); Assert.NotEmpty(second.Frames);
    }

    [Fact]
    public void RealGeneratorRetryKeepsActivationAndNextResumeAllocatesNew()
    {
        PyTraceProduct.Enter(1); Push("module", 1);
        var activations = new List<long>(); int calls = 0;
        var generator = new PyGenerator(value =>
        {
            activations.Add(PyTraceback.ProductScene()[^1].Activation);
            Assert.Single(PyTraceProduct.Active!.Generators);
            if (calls++ == 0) return PyOps.GeneratorRetry();
            ((object?[])value)[0] = calls == 2 ? 1L : -1L; return 7L;
        }, [0L, null, null, null], "trace-v1-lifecycle.py", "retry");
        Assert.True(generator.MoveNext()); Assert.Empty(PyTraceProduct.Active!.Generators);
        Assert.False(generator.MoveNext()); Assert.Empty(PyTraceProduct.Active.Generators);
        Assert.Equal(3, activations.Count); Assert.Equal(activations[0], activations[1]); Assert.NotEqual(activations[1], activations[2]);
        var error = Error("escape");
        var failing = new PyGenerator(_ => throw (PyBaseException)PyTraceProduct.Explicit(error),
            [0L, null, null, null], "trace-v1-lifecycle.py", "failing");
        Assert.Same(error, Assert.Throws<PyTypeError>(() => failing.MoveNext()));
        Assert.Empty(PyTraceProduct.Active.Generators); Assert.Equal(1L, PyTraceback.CaptureDepth());
        PyTraceProduct.Terminal(error);
        Assert.Equal(new[] { "failing", "module" }, error.Frames.Select(f => f.Function));
    }
}
#endif
