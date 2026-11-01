using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyGeneratorDelegationTests
{
    private static object?[] Frame(object? sent = null, object? injected = null) => [0L, sent, injected, null];

    private static PyFunction Fn(string name, Func<object?[], object?> body, params string[] names) =>
        new(name, PySignature.Create(name, names, names.Select(_ => "pos").ToArray(),
            names.Select(_ => "0").ToArray()), (_, args, _, _) => body(args), []);

    private static PyInstance Iterator(Func<object?[], object?> next, Action<PyClass>? configure = null)
    {
        var cls = (PyClass)PyOps.MakeClass("Iterator", [], null);
        cls.SetAttr("__iter__", Fn("__iter__", args => args[0], "self"));
        cls.SetAttr("__next__", Fn("__next__", next, "self"));
        configure?.Invoke(cls);
        return (PyInstance)cls.Invoke([], [], [])!;
    }

    [Fact]
    public void OrdinaryIteratorYieldsValuesThenNoneResult()
    {
        object state = PyOps.GeneratorDelegateStart(new PyTuple([1L, PyNone.Instance]));
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Equal(1L, PyOps.GeneratorDelegateCurrent(state));
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Same(PyNone.Instance, PyOps.GeneratorDelegateCurrent(state));
        Assert.False(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Same(PyNone.Instance, PyOps.GeneratorDelegateResult(state));
        Assert.False(PyOps.GeneratorDelegateStep(Frame(), state));
    }

    [Fact]
    public void IterIsCalledOnceAndCustomNextCompletionValueIsPreserved()
    {
        int iterations = 0, calls = 0;
        object result = new PyTuple([7L, 8L]);
        var iterator = Iterator(_ => ++calls == 1 ? 1L : throw new PyStopIteration(result));
        iterator.Class.SetAttr("__iter__", Fn("__iter__", args => { iterations++; return args[0]; }, "self"));
        object state = PyOps.GeneratorDelegateStart(iterator);
        Assert.Equal(1, iterations);
        Assert.Equal(0, calls);
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.False(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Same(result, PyOps.GeneratorDelegateResult(state));
        Assert.Equal(1, iterations);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void GeneratorSendAndReturnAreForwardedWithoutLosingIdentity()
    {
        var child = new PyGenerator(raw =>
        {
            var slots = (object?[])raw;
            object? input = PyOps.GeneratorInput(slots);
            long pc = (long)slots[0]!;
            slots[0] = pc == 2 ? -1L : pc + 1;
            return pc == 0 ? "ready" : input;
        }, Frame(), "delegate.py", "child");
        object state = PyOps.GeneratorDelegateStart(child);
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Equal("ready", PyOps.GeneratorDelegateCurrent(state));
        object value = new PyList();
        var sent = Frame(value);
        Assert.True(PyOps.GeneratorDelegateStep(sent, state));
        Assert.Same(value, PyOps.GeneratorDelegateCurrent(state));
        Assert.Null(sent[1]);
        Assert.False(PyOps.GeneratorDelegateStep(Frame(value), state));
        Assert.Same(value, PyOps.GeneratorDelegateResult(state));
    }

    [Fact]
    public void CustomSendThrowAndCloseUseActualIteratorAttributes()
    {
        var error = new PyValueError("injected");
        int closes = 0;
        var iterator = Iterator(_ => "next", cls =>
        {
            cls.SetAttr("send", Fn("send", args => args[1], "self", "value"));
            cls.SetAttr("throw", Fn("throw", args =>
            {
                Assert.Same(error, args[1]);
                throw new PyStopIteration(99L);
            }, "self", "error"));
            cls.SetAttr("close", Fn("close", _ => { closes++; return null; }, "self"));
        });
        object state = PyOps.GeneratorDelegateStart(iterator);
        Assert.True(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.True(PyOps.GeneratorDelegateStep(Frame(42L), state));
        Assert.Equal(42L, PyOps.GeneratorDelegateCurrent(state));
        Assert.False(PyOps.GeneratorDelegateStep(Frame(injected: error), state));
        Assert.Equal(99L, PyOps.GeneratorDelegateResult(state));
        object closing = PyOps.GeneratorDelegateStart(iterator);
        PyOps.GeneratorDelegateStep(Frame(), closing);
        var exit = new PyGeneratorExit();
        Assert.Same(exit, Assert.Throws<PyGeneratorExit>(() =>
            PyOps.GeneratorDelegateStep(Frame(injected: exit), closing)));
        Assert.Equal(1, closes);
    }

    [Fact]
    public void MissingThrowRethrowsOriginalAndCloseFailureIsNotConsumed()
    {
        var iterator = Iterator(_ => 1L);
        object state = PyOps.GeneratorDelegateStart(iterator);
        PyOps.GeneratorDelegateStep(Frame(), state);
        var stop = new PyStopIteration(8L);
        Assert.Same(stop, Assert.Throws<PyStopIteration>(() =>
            PyOps.GeneratorDelegateStep(Frame(injected: stop), state)));
        Assert.Throws<PyAttributeError>(() => PyOps.GeneratorDelegateStep(Frame(1L), state));
        iterator.Class.SetAttr("close", Fn("close", _ => throw stop, "self"));
        Assert.Same(stop, Assert.Throws<PyStopIteration>(() =>
            PyOps.GeneratorDelegateStep(Frame(injected: new PyGeneratorExit()), state)));
    }

    [Fact]
    public void AttributeLookupErrorsPropagateAndNonCallableMethodsFailAtCall()
    {
        var iterator = Iterator(_ => 1L);
        object state = PyOps.GeneratorDelegateStart(iterator);
        PyOps.GeneratorDelegateStep(Frame(), state);
        iterator.Class.SetAttr("throw", new PyProperty(Fn("throw", _ =>
            throw new PyValueError("lookup failure"), "self")));
        var error = Assert.Throws<PyValueError>(() =>
            PyOps.GeneratorDelegateStep(Frame(injected: new PyTypeError("injected")), state));
        Assert.Equal("lookup failure", error.PyMessage);
        iterator.Class.SetAttr("send", 42L);
        Assert.Throws<PyTypeError>(() => PyOps.GeneratorDelegateStep(Frame(1L), state));
    }

    [Fact]
    public void CompiledStopIterationConstructorPreservesLazyObjectValue()
    {
        int stringCalls = 0;
        var cls = (PyClass)PyOps.MakeClass("Result", [], null);
        cls.SetAttr("__str__", Fn("__str__", _ => { stringCalls++; return "result"; }, "self"));
        object result = cls.Invoke([], [], [])!;
        var error = Assert.IsType<PyStopIteration>(PyOps.MakeException("StopIteration", result));
        Assert.Equal(0, stringCalls);
        Assert.Same(result, error.Value);
        Assert.Same(result, Assert.Single(error.Args));
        var iterator = Iterator(_ => throw error);
        object state = PyOps.GeneratorDelegateStart(iterator);
        Assert.False(PyOps.GeneratorDelegateStep(Frame(), state));
        Assert.Same(result, PyOps.GeneratorDelegateResult(state));
        Assert.Equal(0, stringCalls);
        Assert.Equal("result", PyRepr.Str(error));
        Assert.Equal(1, stringCalls);
        // 现有 MakeException 单参 ABI 把无参与显式 None 合并；暂不声称 args 完整兼容。
        foreach (object? none in new object?[] { null, PyNone.Instance })
        {
            var empty = Assert.IsType<PyStopIteration>(PyOps.MakeException("StopIteration", none));
            Assert.Null(empty.Value);
            Assert.Empty(empty.Args);
        }
    }

    [Fact]
    public void GeneratorExitFactoryAndDerivedConstructorKeepArguments()
    {
        var error = Assert.IsType<PyGeneratorExit>(PyExceptionTypes.Create("GeneratorExit", "message"));
        Assert.Equal("message", error.PyMessage);
        var derived = new DerivedExit();
        Assert.Equal("DerivedExit", derived.PyTypeName);
        Assert.Equal(7L, Assert.Single(derived.Args));
    }

    private sealed class DerivedExit() : PyGeneratorExit("DerivedExit", "seven", [7L]);
}
