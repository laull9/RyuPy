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
}
