// 产品适配的有限事务反例。手工帧只核原子性/同对象事件边界，不作为真实driver验收替代品。
#if !TRACE_ADAPTER_TEST
using System;
using System.Collections.Generic;
using System.Linq;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTraceProductTests : IDisposable
{
    private readonly List<PyBaseException> errors = new();
    public PyTraceProductTests()
    {
        PyTraceback.Reset();
        PyErrors.LeaveHandler(null);
        PyAtexit.Clear();
        PySystem.Initialize("trace-product-tests.py", []);
        PyTraceProduct.Enter(1);
    }
    public void Dispose()
    {
        // 失败断言也不能把全局执行域遗留给其它测试；只终结本测试已登记的真实owner。
        foreach (var error in errors)
        {
            if (error.TraceProductBinding is not { } binding) continue;
            if (binding.Owner.Snapshot.Park is { } token) PyTraceProduct.Abort(token);
            if (binding.Owner.Snapshot.PendingCount is not null) PyTraceProduct.Suppress(error);
        }
        PyTraceback.Reset();
        PyErrors.LeaveHandler(null);
        if (PyTraceProduct.Enabled) PyTraceProduct.Exit();
        PyAtexit.Clear();
    }
    private PyTypeError Source(string message = "same")
    {
        var error = new PyTypeError(message);
        errors.Add(error);
        PyTraceProduct.Explicit(error);
        return error;
    }

    [Fact]
    public void FirstEmptyForwardGetsReceiptButExactReplayDoesNotAddVersion()
    {
        long depth = PyTraceback.PushFrame("trace-product-tests.py", "module");
        var error = Source();
        var owner = error.TraceProductBinding!.Owner;
        PyTraceProduct.Forward(error, depth);
        Assert.NotNull(owner.Snapshot.Receipt);
        Assert.Empty(owner.Snapshot.History);
        var received = owner.Snapshot;
        PyTraceProduct.Forward(error, depth);
        Assert.Same(received, owner.Snapshot);
        long foreign = PyTraceback.PushFrame("trace-product-tests.py", "not-in-source");
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Forward(error, foreign));
        Assert.Same(received, owner.Snapshot);
        PyTraceback.PopTo(foreign);
        PyTraceProduct.Handle(error, depth);
        Assert.Equal(new[] { "module" }, owner.Snapshot.History.Select(f => f.Function));
    }

    [Fact]
    public void FailedNewSourceDoesNotPublishBindingOrCountFlight()
    {
        PyTraceback.PushFrame("trace-product-tests.py", "bad-scene");
        PyTraceback.SetLine(-1);
        var error = new PyTypeError("candidate");
        errors.Add(error);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Explicit(error));
        Assert.Null(error.TraceProductBinding);
        Assert.Empty(error.Frames);
        Assert.Equal(0, PyTraceProduct.Active!.Flights);
        PyTraceback.SetLine(3);
        Assert.Same(error, PyTraceProduct.Explicit(error));
        Assert.NotNull(error.TraceProductBinding);
        PyTraceProduct.Terminal(error);
    }

    [Fact]
    public void ParkAbortConsumesOnlyOldContinuationOfSameObject()
    {
        PyTraceback.PushFrame("trace-product-tests.py", "module");
        long generator = PyTraceback.PushFrame("trace-product-tests.py", "generator");
        var error = Source();
        var token = PyTraceProduct.Park(error, generator);
        PyTraceback.PushFrame("trace-product-tests.py", "getter");
        PyTraceProduct.Explicit(error);
        var active = error.TraceProductBinding!.Owner.Snapshot;
        PyTraceProduct.Abort(token);
        var after = error.TraceProductBinding.Owner.Snapshot;
        Assert.Equal(active.Request, after.Request);
        Assert.Equal(active.PendingCount, after.PendingCount);
        Assert.Same(active.History, after.History);
        Assert.Null(after.Park);
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Abort(token));
        Assert.Same(after, error.TraceProductBinding.Owner.Snapshot);
        PyTraceProduct.Terminal(error);
        Assert.Equal(new[] { "module", "generator", "getter", "generator" },
            error.TraceProductBinding.Owner.Snapshot.History.Select(f => f.Function));
    }

    [Fact]
    public void ParkAllMissResumesOuterSegmentWithoutRestoringOldHistoryOrReceipt()
    {
        PyTraceback.PushFrame("trace-product-tests.py", "module");
        long generator = PyTraceback.PushFrame("trace-product-tests.py", "generator");
        var error = Source();
        var token = PyTraceProduct.Park(error, generator);
        long getter = PyTraceback.PushFrame("trace-product-tests.py", "getter");
        PyTraceProduct.Explicit(error);
        PyTraceProduct.Handle(error, getter);
        var handled = error.TraceProductBinding!.Owner.Snapshot;
        PyTraceback.PopTo(getter);
        PyTraceProduct.AllMiss(token);
        var resumed = error.TraceProductBinding.Owner.Snapshot;
        Assert.Same(handled.History, resumed.History);
        Assert.Null(resumed.Receipt);
        Assert.Equal(handled.Request!.Value.Number + 1, resumed.Request!.Value.Number);
        PyTraceProduct.Forward(error, generator);
        Assert.Same(resumed, error.TraceProductBinding.Owner.Snapshot);
        PyTraceProduct.Terminal(error);
        Assert.Equal(new[] { "module", "getter", "generator" },
            error.TraceProductBinding.Owner.Snapshot.History.Select(f => f.Function));
    }

    [Fact]
    public void PhysicalResetKeepsAuthorityForFreshEmptyStackExitProducer()
    {
        PyTraceback.PushFrame("trace-product-tests.py", "module");
        var error = Source();
        PyTraceProduct.Terminal(error);
        var scope = PyTraceProduct.Active;
        var epoch = error.TraceProductBinding!.Owner.Snapshot.Epoch;
        PyTraceback.Reset();
        var next = PyTraceProduct.NativeFresh(new PyOSError("flush"));
        errors.Add(next);
        Assert.Same(scope, PyTraceProduct.Active);
        Assert.Same(epoch, next.TraceProductBinding!.Owner.Snapshot.Epoch);
        Assert.Empty(next.TraceProductBinding.Owner.Snapshot.Scene);
        PyTraceProduct.Suppress(next);
        Assert.Equal(0, scope!.Flights);
    }

    [Fact]
    public void WrongAbiAndLegacyWriterCannotChangeAnActiveDomain()
    {
        var scope = PyTraceProduct.Active;
        Assert.Throws<InvalidOperationException>(() => PyTraceProduct.Enter(2));
        Assert.Same(scope, PyTraceProduct.Active);
        Assert.Throws<InvalidOperationException>(() => PyTraceback.Attach(new PyTypeError("legacy")));
    }
}
#endif
