// async-for协议的宿主边界测试：槽存在检查无getter副作用，真实调用重绑定，错误身份与cause保留。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyAsyncIterationTests : IDisposable
{
    public PyAsyncIterationTests() => PyTraceback.Reset();
    public void Dispose() => PyTraceback.Reset();
    private static PyFunction Method(string name, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, ["self"], [null], 1, body);
    private static PyInstance Instance(PyClass cls) => (PyInstance)cls.Invoke([], [], [])!;
    private static PyClass Source() => (PyClass)PyOps.MakeClass("Source", [], null);

    [Fact]
    public void StartDoesNotBindNextDescriptorAndSpecialCallsBypassInstanceHooks()
    {
        int starts = 0, binds = 0;
        var cls = Source();
        cls.SetAttr("__aiter__", Method("__aiter__", args => { starts++; return args[0]; }));
        var next = PyNativeFunctions.Create("next", [], [], 0, _ => 17L);
        cls.SetAttr("__anext__", new PyProperty(Method("getter", _ => { binds++; return next; })));
        // HIR尚不开放自定义__getattribute__，受信宿主在这里直接保护绕过合同。
        cls.SetAttr("__getattribute__", Method("override", _ => throw new Exception("不可调用实例hook")));
        cls.SetAttr("__getattr__", Method("fallback", _ => throw new Exception("不可调用实例fallback")));
        var source = Instance(cls);
        PyObject.SetAttr(source, "__aiter__", PyNone.Instance);
        Assert.Same(source, PyOps.AsyncIterStart(source));
        Assert.Equal(1, starts); Assert.Equal(0, binds);
        Assert.Equal(17L, PyOps.AsyncIterNext(source));
        Assert.Equal(1, binds);
        cls.SetAttr("__anext__", Method("changed", _ => 29L));
        PyObject.SetAttr(source, "__anext__", PyNone.Instance);
        Assert.Equal(29L, PyOps.AsyncIterNext(source));
        Assert.Equal(1, starts); Assert.Equal(1, binds);
    }

    [Fact]
    public void CoroutineReturnedByAiterIsNotAwaited()
    {
        int calls = 0;
        var coroutine = new PyCoroutine(new PyGenerator(_ => { calls++; return null; },
            [0L, null, null, null], "unused.py", "unused"));
        var cls = Source();
        cls.SetAttr("__aiter__", Method("__aiter__", _ => coroutine));
        Assert.Equal("'async for' received an object from __aiter__ that does not implement __anext__: coroutine",
            Assert.Throws<PyTypeError>(() => PyOps.AsyncIterStart(Instance(cls))).Message);
        Assert.Equal(0, calls);
        coroutine.Close();
    }

    [Fact]
    public void DirectNextFailureKeepsIdentityAndAwaitAcquisitionWrapsCause()
    {
        PyTraceback.PushFrame("protocol.py", "consume");
        var original = new PyValueError("next");
        var cls = Source();
        cls.SetAttr("__aiter__", Method("__aiter__", args => args[0]));
        cls.SetAttr("__anext__", Method("__anext__", _ => throw original));
        Assert.Same(original, Assert.Throws<PyValueError>(() => PyOps.AsyncIterNext(Instance(cls))));
        var awaitable = Source();
        awaitable.SetAttr("__await__", Method("__await__", _ => throw original));
        var wrapped = Assert.Throws<PyTypeError>(() => PyOps.CoroutineNextAwaitStart(Instance(awaitable)));
        Assert.Equal("'async for' received an invalid object from __anext__: Source", wrapped.Message);
        Assert.Same(original, wrapped.Cause); Assert.Same(original, wrapped.Context);
        // 宿主getter没有Python激活；获取失败尚未传播进consume，cause不能伪造调用者帧。
        Assert.Same(PyNone.Instance, PyOps.TracebackOf(original));
    }

    [Fact]
    public void IteratorExecutionFailureIsOutsideAcquisitionWrapping()
    {
        var original = new PyStopAsyncIteration();
        var iterator = Source();
        iterator.SetAttr("__next__", Method("__next__", _ => throw original));
        var awaitable = Source();
        awaitable.SetAttr("__await__", Method("__await__", _ => Instance(iterator)));
        object delegation = PyOps.CoroutineNextAwaitStart(Instance(awaitable));
        Assert.Same(original, Assert.Throws<PyStopAsyncIteration>(() =>
            PyOps.GeneratorDelegateStep(new object?[] { 0L, null, null, null }, delegation)));
        var named = new PyValueError("same spelling");
        named.BindPythonClass((PyClass)PyOps.MakeClass("StopAsyncIteration", [], null));
        Assert.Equal(true, PyOps.AsyncIterIsStop(original));
        Assert.Equal(false, PyOps.AsyncIterIsStop(named));
    }

    [Fact]
    public void DescriptorBindingFailureIsSlotAttributeErrorAndNeverExhaustion()
    {
        PyTraceback.PushFrame("protocol.py", "consume");
        var original = new PyStopAsyncIteration();
        var cls = Source();
        cls.SetAttr("__aiter__", Method("__aiter__", args => args[0]));
        cls.SetAttr("__anext__", new PyProperty(Method("getter", _ => throw original)));
        var source = Instance(cls);
        Assert.Same(source, PyOps.AsyncIterStart(source));
        var error = Assert.Throws<PyAttributeError>(() => PyOps.AsyncIterNext(source));
        Assert.Equal("object Source does not have __anext__ method", error.Message);
        Assert.Null(error.Cause);
        Assert.Equal(false, PyOps.AsyncIterIsStop(error));
        cls.SetAttr("__await__", new PyProperty(Method("await-getter", _ => throw original)));
        var wrapped = Assert.Throws<PyTypeError>(() => PyOps.CoroutineNextAwaitStart(source));
        Assert.Equal("object Source does not have __await__ method", wrapped.Cause!.Message);
        Assert.IsType<PyAttributeError>(wrapped.Cause);
    }
}
