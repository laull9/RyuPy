// 原生traceback的真实catch边界、激活身份、可写尾链和正常帧路径零分配保护。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyNativeTracebackTests : IDisposable
{
    private readonly object? previous = PyErrors.EnterHandler(null);
    public PyNativeTracebackTests() => PyTraceback.Reset();
    public void Dispose() { PyTraceback.Reset(); PyErrors.LeaveHandler(previous); }

    private static PyNativeTraceback Trace(PyBaseException error) =>
        Assert.IsType<PyNativeTraceback>(PyObject.GetAttr(error, "__traceback__"));
    private static object Frame(PyNativeTraceback traceback) => PyObject.GetAttr(traceback, "tb_frame")!;
    private static object Code(object frame) => PyObject.GetAttr(frame, "f_code")!;
    private static PyFunction Origin(string name = "source")
    {
        var signature = PySignature.Create(name, [], [], []);
        var source = PyTraceback.CreateCodeSource("trace", "trace.py", _ => PyNone.Instance);
        PyOps.FunctionDefinitionWithSource(signature, name, 1, source);
        return new(name, signature, (_, _, _, _) => PyNone.Instance, []);
    }

    [Fact]
    public void RealReceiverExcludesUnwoundDriversAndKeepsHeadIdentity()
    {
        PyTraceback.PushFrame("trace.py", "driver");
        long receiver = PyTraceback.PushFrameWithOrigin("trace.py", "operation", Origin("operation"));
        PyTraceback.SetLine(10);
        PyTraceback.PushFrameWithOrigin("trace.py", "failure", Origin("failure"));
        PyTraceback.SetLine(20);
        var error = new PyValueError("failure");
        PyErrors.PrepareExplicitRaise(error);
        PyTraceback.ReceiveException(error, receiver);
        var head = Trace(error);
        Assert.Same(head, PyOps.TracebackOf(error));
        Assert.Equal("operation", PyObject.GetAttr(Code(Frame(head)), "co_name"));
        var tail = Assert.IsType<PyNativeTraceback>(PyObject.GetAttr(head, "tb_next"));
        Assert.Equal("failure", PyObject.GetAttr(Code(Frame(tail)), "co_name"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(tail, "tb_next"));
        Assert.Equal(10L, PyObject.GetAttr(head, "tb_lineno"));
        Assert.Equal(20L, PyObject.GetAttr(tail, "tb_lineno"));
    }

    [Fact]
    public void SameDepthLaterCallGetsNewFrameButSameCompiledCode()
    {
        var origin = Origin();
        long firstDepth = PyTraceback.PushFrameWithOrigin("trace.py", "source", origin);
        PyTraceback.SetLine(12);
        var firstError = new PyValueError("one");
        PyErrors.PrepareExplicitRaise(firstError);
        object first = Frame(Trace(firstError));
        PyTraceback.PopTo(firstDepth);
        PyTraceback.PushFrameWithOrigin("trace.py", "source", origin);
        PyTraceback.SetLine(14);
        var secondError = new PyValueError("two");
        PyErrors.PrepareExplicitRaise(secondError);
        object second = Frame(Trace(secondError));
        Assert.NotSame(first, second);
        Assert.Same(Code(first), Code(second));
        Assert.Equal(12L, PyObject.GetAttr(first, "f_lineno"));
        Assert.Equal(14L, PyObject.GetAttr(second, "f_lineno"));
    }

    [Fact]
    public void GeneratorResumePreservesFrameIdentityAndFrozenNodeLines()
    {
        PyNativeTraceback? before = null, after = null;
        var generator = new PyGenerator(raw =>
        {
            var slots = (object?[])raw;
            var error = new PyValueError("observed");
            PyTraceback.SetLine((long)slots[0]! == 0 ? 20 : 30);
            PyErrors.PrepareExplicitRaise(error);
            if ((long)slots[0]! == 0) { before = Trace(error); slots[0] = 1L; return "waiting"; }
            after = Trace(error); slots[0] = -1L; return PyNone.Instance;
        }, [0L, null, null, null], "trace.py", "generator");
        Assert.Equal("waiting", generator.Send(PyNone.Instance));
        Assert.Throws<PyStopIteration>(() => generator.Send(PyNone.Instance));
        Assert.Same(Frame(before!), Frame(after!));
        Assert.Equal(20L, PyObject.GetAttr(before!, "tb_lineno"));
        Assert.Equal(30L, PyObject.GetAttr(after!, "tb_lineno"));
        Assert.Equal(30L, PyObject.GetAttr(Frame(before!), "f_lineno"));
    }

    [Fact]
    public void TailAssignmentRejectsCyclesAndUnsupportedBytecodeIsExplicit()
    {
        PyTraceback.PushFrameWithOrigin("trace.py", "source", Origin());
        PyTraceback.SetLine(10);
        var error = new PyValueError("failure");
        PyErrors.PrepareExplicitRaise(error);
        var trace = Trace(error);
        Assert.Equal("traceback loop detected", Assert.Throws<PyValueError>(() =>
            PyObject.SetAttr(trace, "tb_next", trace)).Message);
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(trace, "tb_next", 1L));
        PyObject.SetAttr(trace, "tb_next", PyNone.Instance);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(trace, "tb_next"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(trace, "tb_lasti"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(Code(Frame(trace)), "co_code"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(Frame(trace), "f_locals"));
    }

    [Fact]
    public void HostSharedSignatureDoesNotForgeCompiledCodeIdentity()
    {
        var signature = PySignature.Create("host", [], [], []);
        var first = new PyFunction("first", signature, (_, _, _, _) => 1L, []);
        var second = new PyFunction("second", signature, (_, _, _, _) => 2L, []);
        foreach (var function in new[] { first, second })
        {
            long depth = PyTraceback.PushFrameWithOrigin("host.cs", function.Name, function);
            PyTraceback.SetLine(10);
            var error = new PyValueError("host");
            PyErrors.PrepareExplicitRaise(error);
            Assert.Throws<PyNotImplementedError>(() => Code(Frame(Trace(error))));
            PyTraceback.PopTo(depth);
        }
    }

    [Fact]
    public void TracebackDataReadCannotBeShadowedAndNativeHeadWritesAreExplicitlyRejected()
    {
        var error = new PyValueError("subclass");
        error.BindPythonClass((PyClass)PyOps.MakeClass("Derived", [], null));
        PyTraceback.PushFrameWithOrigin("trace.py", "source", Origin());
        PyTraceback.SetLine(11);
        PyErrors.PrepareExplicitRaise(error);
        var head = Trace(error);
        error.UserAttributes["__traceback__"] = "shadow";
        Assert.Same(head, Trace(error));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(error, "__traceback__", PyNone.Instance));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(error, "__traceback__"));
        Assert.Same(head, Trace(error));
        error.PythonClass!.SetAttr("__traceback__", "class-shadow");
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(error, "__traceback__"));
    }

    [Fact]
    public void NormalPushLinePopRemainsAllocationFreeAfterTracebackObservation()
    {
        PyTraceback.PushFrame("trace.py", "warm");
        PyTraceback.SetLine(1);
        PyErrors.PrepareExplicitRaise(new PyValueError("warm"));
        PyTraceback.Reset();
        for (int i = 0; i < 100; i++)
        { long depth = PyTraceback.PushFrame("trace.py", "ordinary"); PyTraceback.SetLine(2); PyTraceback.PopTo(depth); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
        { long depth = PyTraceback.PushFrame("trace.py", "ordinary"); PyTraceback.SetLine(2); PyTraceback.PopTo(depth); }
        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void UnobservedShallowCatchHasCompactAllocationCost()
    {
        const int count = 1000;
        var ordinary = Enumerable.Range(0, count).Select(_ => new PyValueError("ordinary")).ToArray();
        var native = Enumerable.Range(0, count).Select(_ => new PyValueError("native")).ToArray();
        long receiver = PyTraceback.PushFrame("cold.py", "receiver");
        // 两条路径都真实throw/catch、附legacy Frames；只比较新增传播元数据的分配。
        // 先预热同样入口，避免把一次性表初始化计入每异常合同。
        CatchCost([new PyValueError("warm")], receiver, false);
        CatchCost([new PyValueError("warm")], receiver, true);
        long baseline = CatchCost(ordinary, receiver, false);
        long actual = CatchCost(native, receiver, true);
        Assert.InRange(actual - baseline, 1L, count * 224L);
        var trace = Trace(native[^1]);
        Assert.Equal(11L, PyObject.GetAttr(trace, "tb_lineno"));
        var tail = Assert.IsType<PyNativeTraceback>(PyObject.GetAttr(trace, "tb_next"));
        Assert.Equal(22L, PyObject.GetAttr(tail, "tb_lineno"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(tail, "tb_next"));
    }

    private static long CatchCost(PyValueError[] errors, long receiver, bool native)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        foreach (var error in errors)
        {
            PyTraceback.SetLine(11);
            long depth = PyTraceback.PushFrame("cold.py", "failure");
            PyTraceback.SetLine(22);
            try
            {
                throw (PyBaseException)(native ? PyErrors.PrepareExplicitRaise(error) : PyErrors.PrepareRaise(error));
            }
            catch (PyBaseException caught)
            {
                if (native) PyTraceback.ReceiveException(caught, receiver);
            }
            finally { PyTraceback.PopTo(depth); }
        }
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void CodeViewCacheHitsAcrossActivationsAllocateNoLookupClosures()
    {
        var origin = Origin();
        var frames = new object[100];
        var codes = new object[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            long depth = PyTraceback.PushFrameWithOrigin("trace.py", "source", origin);
            var error = new PyValueError("view");
            PyErrors.PrepareExplicitRaise(error);
            frames[i] = Frame(Trace(error));
            PyTraceback.PopTo(depth);
        }
        object first = Code(frames[0]);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < frames.Length; i++) codes[i] = Code(frames[i]);
        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        foreach (object code in codes) Assert.Same(first, code);
        Assert.NotSame(frames[0], frames[1]);
    }

    [Fact]
    public void TruncatedTailStaysTruncatedAcrossExplicitAndBareReraise()
    {
        long receiver = PyTraceback.PushFrame("trace.py", "receiver");
        long middle = PyTraceback.PushFrame("trace.py", "middle");
        long failure = PyTraceback.PushFrame("trace.py", "failure");
        var error = new PyValueError("failure");
        PyErrors.PrepareExplicitRaise(error);
        PyTraceback.ReceiveException(error, middle);
        var original = Trace(error);
        PyObject.SetAttr(original, "tb_next", PyNone.Instance);
        PyTraceback.PopTo(failure);
        PyErrors.PrepareExplicitRaise(error);
        var explicitHead = Trace(error);
        Assert.NotSame(original, explicitHead);
        Assert.Same(original, PyObject.GetAttr(explicitHead, "tb_next"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(original, "tb_next"));
        object? token = PyErrors.EnterHandler(error);
        try { Assert.Same(error, PyErrors.Reraise()); }
        finally { PyErrors.LeaveHandler(token); }
        PyTraceback.PopTo(middle);
        PyTraceback.ReceiveException(error, receiver);
        Assert.Same(explicitHead, PyObject.GetAttr(Trace(error), "tb_next"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(original, "tb_next"));
    }

    [Fact]
    public void DeepReceiversDeduplicateIndependentlyOfWritableTail()
    {
        for (int i = 0; i < 6; i++) PyTraceback.PushFrame("deep.py", "layer" + i);
        var error = new PyValueError("deep");
        PyErrors.PrepareExplicitRaise(error);
        PyTraceback.ReceiveException(error, 4);
        var inside = Trace(error);
        PyObject.SetAttr(inside, "tb_next", PyNone.Instance);
        // 中间catch再次接收时，已接收的三个内层激活都不能从去重历史中消失。
        PyTraceback.ReceiveException(error, 2);
        var head = Trace(error);
        var next = Assert.IsType<PyNativeTraceback>(PyObject.GetAttr(head, "tb_next"));
        Assert.Same(inside, PyObject.GetAttr(next, "tb_next"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(inside, "tb_next"));
    }

    [Fact]
    public void ColdTablesGrowWithRealStackAndPoppedSlotsGetNewActivation()
    {
        PyTraceback.PushFrame("grow.py", "warm");
        PyErrors.PrepareExplicitRaise(new PyValueError("warm"));
        PyTraceback.Reset();
        for (int i = 0; i < 260; i++) PyTraceback.PushFrame("grow.py", "layer");
        var error = new PyValueError("grow");
        PyErrors.PrepareExplicitRaise(error);
        PyTraceback.ReceiveException(error, 1);
        var frames = new HashSet<object>(ReferenceEqualityComparer.Instance);
        object? node = Trace(error);
        while (node is PyNativeTraceback trace)
        {
            Assert.True(frames.Add(Frame(trace)));
            node = PyObject.GetAttr(trace, "tb_next");
        }
        Assert.Equal(260, frames.Count);
        PyTraceback.Reset();
        PyTraceback.PushFrame("grow.py", "layer");
        var later = new PyValueError("later");
        PyErrors.PrepareExplicitRaise(later);
        Assert.DoesNotContain(Frame(Trace(later)), frames);
    }
}
