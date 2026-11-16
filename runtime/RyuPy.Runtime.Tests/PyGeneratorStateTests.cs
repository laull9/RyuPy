// 新状态载体复用PyGenerator生命周期：返回值一次消费、send/throw、完成清引用与精确ABI。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

public class PyGeneratorStateTests
{
    private sealed class Payload { internal int Index; }

    [Fact]
    public void StateIsNativeAndCompletionClearsPayload()
    {
        object? observed = null;
        var payload = new Payload();
        var g = new PyGenerator(frame => {
            observed = frame;
            var data = (Payload)PyOps.GeneratorStateData(frame);
            Assert.Equal((long)data.Index, PyOps.GeneratorStateGetPC(frame));
            if (data.Index++ == 0) { PyOps.GeneratorStateSetPC(frame, 1); return "yield"; }
            PyOps.GeneratorStateSetPC(frame, -1); return "return";
        }, payload, 0, "state.py", "g", null);
        Assert.Equal("yield", g.Next(null, false));
        Assert.Equal("return", Assert.Throws<PyStopIteration>(() => g.Next(null, false)).Value);
        Assert.Null(PyOps.GeneratorStateData(observed!));
        Assert.Equal("default", g.Next("default", true));
    }

    [Fact]
    public void SendAndThrowUseTheSameInputBoundary()
    {
        int calls = 0;
        var g = new PyGenerator(frame => {
            calls++;
            if (PyOps.GeneratorStateGetPC(frame) == 0) { PyOps.GeneratorStateSetPC(frame, 1); return "start"; }
            return PyOps.GeneratorInput(frame);
        }, new Payload(), 0, "state.py", "g", null);
        Assert.Throws<PyTypeError>(() => g.Send(1L)); Assert.Equal(0, calls);
        Assert.Equal("start", g.Next(null, false));
        object token = new(); Assert.Same(token, g.Send(token));
        var error = new PyValueError("injected");
        Assert.Same(error, Assert.Throws<PyValueError>(() => g.Throw(error)));
        Assert.Equal("closed", g.Next("closed", true));
    }

    [Fact]
    public void InitialThrowAndCloseDoNotEnterTheBody()
    {
        int calls = 0;
        PyGenerator Create() => new(_ => { calls++; return null; }, new Payload(), 0, "s.py", "g", null);
        var g = Create(); g.Close(); Assert.False(g.MoveNext());
        var other = Create(); Assert.Throws<PyValueError>(() => other.Throw(new PyValueError("x")));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void StateCarrierIsPrivateAndCompilerMethodsHaveFixedSignatures()
    {
        var assembly = typeof(PyGenerator).Assembly;
        Assert.False(assembly.GetType("RyuPy.Runtime.PyGeneratorState")!.IsPublic);
        Assert.Equal(typeof(long), typeof(PyOps).GetMethod("GeneratorStateGetPC")!.ReturnType);
        Assert.Equal(typeof(void), typeof(PyOps).GetMethod("GeneratorStateSetPC")!.ReturnType);
        Assert.NotNull(typeof(PyGenerator).GetConstructor([typeof(PyResume), typeof(object), typeof(long), typeof(string), typeof(string), typeof(object)]));
    }
}
