// 原生协程的协议合同：惰性、生命周期、await 槽及委托终值的身份。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyCoroutineTests
{
    private static object?[] Frame(object? sent = null) => [0L, sent, null, null];
    private static PyCoroutine Coroutine(PyResume body) =>
        new(new PyGenerator(body, Frame(), "async.py", "operation"));

    [Fact]
    public void CreationIsLazyAndFirstInvalidSendDoesNotConsumeState()
    {
        int calls = 0;
        var coroutine = Coroutine(raw =>
        {
            calls++;
            ((object?[])raw)[0] = -1L;
            return 42L;
        });
        Assert.Equal(0, calls);
        Assert.Throws<PyTypeError>(() => coroutine.Send(1L));
        Assert.Equal(0, calls);
        Assert.Equal(42L, Assert.Throws<PyStopIteration>(() => coroutine.Send(PyNone.Instance)).Value);
        Assert.Equal(1, calls);
        Assert.Equal("cannot reuse already awaited coroutine",
            Assert.Throws<PyRuntimeError>(() => coroutine.Send(PyNone.Instance)).Message);
    }

    [Fact]
    public void AwaitDelegationPreservesSuspendInputAndCompletionIdentity()
    {
        var expected = new PyList();
        var coroutine = Coroutine(raw =>
        {
            var slots = (object?[])raw;
            if ((long)slots[0]! == 0) { slots[0] = 1L; return "waiting"; }
            object? input = PyOps.GeneratorInput(slots);
            slots[0] = -1L;
            return input;
        });
        object state = PyOps.CoroutineAwaitStart(coroutine);
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Equal("waiting", PyOps.GeneratorDelegateCurrent(state));
        Assert.False(PyOps.GeneratorDelegateStep(Frame(expected), state));
        Assert.Same(expected, PyOps.GeneratorDelegateResult(state));
        object reused = PyOps.CoroutineAwaitStart(coroutine);
        Assert.Throws<PyRuntimeError>(() => PyOps.GeneratorDelegateStep(Frame(), reused));
    }

    [Fact]
    public void ConcurrentAwaitIsRejectedAndCompletionReleasesOwnership()
    {
        var coroutine = Coroutine(raw =>
        {
            var frame = (object?[])raw;
            frame[0] = (long)frame[0]! == 0 ? 1L : -1L;
            return PyNone.Instance;
        });
        object first = PyOps.CoroutineAwaitStart(coroutine);
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), first));
        Assert.Equal("coroutine is being awaited already",
            Assert.Throws<PyRuntimeError>(() => PyOps.CoroutineAwaitStart(coroutine)).Message);
        Assert.False(PyOps.GeneratorDelegateStep(Frame(), first));
        Assert.NotNull(PyOps.CoroutineAwaitStart(coroutine));
    }

    [Fact]
    public void CoroutineIsAwaitableButCannotBeOrdinarilyIterated()
    {
        var coroutine = Coroutine(_ => throw new Exception("不得执行"));
        Assert.Throws<PyTypeError>(() => PyIter.Create(coroutine));
        Assert.NotNull(PyObject.GetAttr(coroutine, "__await__"));
        coroutine.Close();
        Assert.Throws<PyRuntimeError>(() => coroutine.Send(PyNone.Instance));
    }

    [Fact]
    public void CloseBeforeStartDoesNotExecuteBodyAndReentrantResumeFails()
    {
        var unused = Coroutine(_ => throw new Exception("未启动协程不能执行正文"));
        Assert.Same(PyNone.Instance, unused.Close());
        PyCoroutine? coroutine = null;
        coroutine = Coroutine(raw =>
        {
            Assert.Equal("coroutine already executing",
                Assert.Throws<PyValueError>(() => coroutine!.Send(PyNone.Instance)).Message);
            ((object?[])raw)[0] = -1L;
            return PyNone.Instance;
        });
        Assert.Throws<PyStopIteration>(() => coroutine.Send(PyNone.Instance));
    }

    [Fact]
    public void ClosingCoroutineKeepsReentrancyGuardActive()
    {
        PyCoroutine? coroutine = null;
        coroutine = Coroutine(raw =>
        {
            var frame = (object?[])raw;
            if ((long)frame[0]! == 0) { frame[0] = 1L; return "waiting"; }
            try { return PyOps.GeneratorInput(frame); }
            catch (PyGeneratorExit)
            {
                Assert.Equal("coroutine already executing",
                    Assert.Throws<PyValueError>(() => coroutine!.Send(PyNone.Instance)).Message);
                throw;
            }
        });
        Assert.Equal("waiting", coroutine.Send(PyNone.Instance));
        Assert.Same(PyNone.Instance, coroutine.Close());
    }

    [Fact]
    public void PropagatedExceptionKeepsIdentityAndClosesCoroutine()
    {
        var expected = new PyValueError("failure");
        var coroutine = Coroutine(_ => throw expected);
        Assert.Same(expected, Assert.Throws<PyValueError>(() => coroutine.Send(PyNone.Instance)));
        Assert.Throws<PyRuntimeError>(() => coroutine.Send(PyNone.Instance));
    }

    [Fact]
    public void OwnLeakedStopIterationUsesCoroutinePep479Name()
    {
        var original = new PyStopIteration(12L);
        var coroutine = Coroutine(_ => throw original);
        var error = Assert.Throws<PyRuntimeError>(() => coroutine.Send(PyNone.Instance));
        Assert.Equal("coroutine raised StopIteration", error.Message);
        Assert.Same(original, error.Cause);
    }

    [Fact]
    public void ChildGeneratorPep479FailureKeepsItsOriginalNameAndIdentity()
    {
        var original = new PyRuntimeError("generator raised StopIteration");
        var coroutine = Coroutine(_ => throw original);
        Assert.Same(original, Assert.Throws<PyRuntimeError>(() => coroutine.Send(PyNone.Instance)));
    }

    [Fact]
    public void AwaitUsesTypeSlotAndNeverCallsReturnedIteratorsIterAgain()
    {
        int calls = 0;
        var iteratorClass = (PyClass)PyOps.MakeClass("Iterator", [], null);
        iteratorClass.SetAttr("__iter__", Function("__iter__", _ => throw new Exception("不能重取iter")));
        iteratorClass.SetAttr("__next__", Function("__next__", _ => throw new PyStopIteration(17L)));
        var iterator = (PyInstance)iteratorClass.Invoke([], [], [])!;
        var awaitableClass = (PyClass)PyOps.MakeClass("Awaitable", [], null);
        awaitableClass.SetAttr("__await__", Function("__await__", _ => { calls++; return iterator; }));
        var awaitable = (PyInstance)awaitableClass.Invoke([], [], [])!;
        PyObject.SetAttr(awaitable, "__await__", PyNone.Instance);
        object state = PyOps.CoroutineAwaitStart(awaitable);
        Assert.Equal(1, calls);
        Assert.False(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Equal(17L, PyOps.GeneratorDelegateResult(state));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void AwaitRejectsOrdinaryIterableAndInvalidAwaitResult()
    {
        Assert.Throws<PyTypeError>(() => PyOps.CoroutineAwaitStart(new PyList()));
        var cls = (PyClass)PyOps.MakeClass("InvalidAwait", [], null);
        cls.SetAttr("__await__", Function("__await__", _ => new PyList()));
        Assert.Equal("__await__() returned non-iterator of type 'list'",
            Assert.Throws<PyTypeError>(() => PyOps.CoroutineAwaitStart(cls.Invoke([], [], []))).Message);
    }

    private static PyFunction Function(string name, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, ["self"], [null], 1, body);
}
