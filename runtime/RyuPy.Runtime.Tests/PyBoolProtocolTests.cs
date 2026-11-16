// bool 的绑定与共享真值冷协议；不改普通 len()，不把警告长尾当支持，报告帧测试必须串行。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyBoolProtocolTests
{
    private static object? Bool(object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallBuiltin("bool", args, names ?? [], values ?? []);
    private static PyInstance Slot(string name, Func<object?> body)
    {
        var cls = new PyClass("TruthProtocol");
        cls.SetAttr(name, PyNativeFunctions.Create(name, ["self"], [null], 1, _ => body()));
        return new PyInstance(cls);
    }
    private static void Caller(Action<PyValueError> body)
    {
        long depth = PyTraceback.PushFrame("bool_test.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try { PyTraceback.SetLine(37); body(outer); }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
    private static void Prepared(PyBaseException error, PyBaseException outer)
    {
        Assert.Same(outer, error.Context);
        Assert.Equal(new PyStackFrame("bool_test.py", "caller", 37), error.Frames[0]);
    }

    [Fact]
    public void ZeroPrimitiveAndContainersKeepExistingTruthValues()
    {
        Assert.Equal(false, Bool([]));
        foreach (object value in new object[] { PyNone.Instance, false, 0L, 0.0, "", new PyList([]), new PyTuple([]), new PyDict() })
            Assert.Equal(false, Bool([value]));
        foreach (object value in new object[] { true, -1L, double.NaN, "x", new PyList([0L]), BigInteger.One << 100 })
            Assert.Equal(true, Bool([value]));
    }
    [Fact]
    public void BinderRejectsKeywordsBeforeArityAndNeverCallsProtocol()
    {
        int calls = 0; var value = Slot("__bool__", () => { calls++; return true; });
        Assert.Throws<PyTypeError>(() => Bool([value, value]));
        var error = Assert.Throws<PyTypeError>(() => Bool([value, value], ["x"], [value]));
        Assert.Contains("takes no keyword arguments", error.Message);
        Assert.Throws<PyTypeError>(() => Bool([], ["x"], [value])); Assert.Equal(0, calls);
    }
    [Fact]
    public void ExpandedArgumentsUseStrictBinderAndInvalidStarKeepsItsError()
    {
        Assert.Equal(false, Bool([PyOps.StarArg(new PyList([]))]));
        Assert.Equal(true, Bool([PyOps.StarArg(new PyList([1L]))]));
        Assert.Throws<PyTypeError>(() => Bool([PyOps.StarArg(new PyList([0L, 1L]))]));
        Assert.Throws<PyTypeError>(() => Bool([PyOps.StarArg(42L)]));
    }
    [Fact]
    public void BoolSlotWinsOnceAndInstanceSlotDoesNotOverrideIt()
    {
        int calls = 0, lengths = 0; var value = Slot("__bool__", () => { calls++; return false; });
        value.Class.SetAttr("__len__", PyNativeFunctions.Create("__len__", ["self"], [null], 1, _ => { lengths++; return 1L; }));
        PyObject.SetAttr(value, "__bool__", PyNativeFunctions.Create("ignored", [], [], 0, _ => true));
        Assert.Equal(false, Bool([value])); Assert.Equal(1, calls); Assert.Equal(0, lengths);
    }
    [Fact]
    public void BadBoolAndDisabledSlotNeverFallBackOrReprepareUserFailure()
    {
        foreach (object result in new object[] { 0L, PyNone.Instance, new PyList([]) })
            Assert.Throws<PyTypeError>(() => Bool([Slot("__bool__", () => result)]));
        int lengths = 0; var value = Slot("__len__", () => { lengths++; return 1L; });
        value.Class.SetAttr("__bool__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => Bool([value])); Assert.Equal(0, lengths);
        var error = new PyLookupError("user bool");
        Caller(_ => Assert.Same(error, Assert.Throws<PyLookupError>(() => Bool([Slot("__bool__", () => throw error)]))));
        Assert.Empty(error.Frames); Assert.Null(error.Context);
    }
    [Fact]
    public void DefaultAndDirectLengthBoolAreSupportedWithoutIndexCoercion()
    {
        Assert.Equal(true, Bool([new PyInstance(new PyClass("Plain"))]));
        Assert.Equal(false, Bool([Slot("__len__", () => false)]));
        Assert.Equal(true, Bool([Slot("__len__", () => true)]));
        Assert.Equal(false, Bool([Slot("__len__", () => 0L)]));
        Assert.Equal(true, Bool([Slot("__len__", () => 3L)]));
    }
    [Fact]
    public void NegativeLengthIsValueErrorEvenOutsideSigned64Range()
    {
        foreach (object result in new object[] { -1L, -(BigInteger.One << 100) })
            Assert.Throws<PyValueError>(() => Bool([Slot("__len__", () => result)]));
    }
    [Fact]
    public void PositiveLengthMustFitIndexSizedInteger()
    {
        Assert.Equal(true, Bool([Slot("__len__", () => (long)int.MaxValue + 1)]));
        Assert.Equal(true, Bool([Slot("__len__", () => long.MaxValue)]));
        Assert.Throws<PyOverflowError>(() => Bool([Slot("__len__", () => (BigInteger)long.MaxValue + 1)]));
    }
    [Fact]
    public void LengthIndexRunsOnceThenAppliesTheSameBounds()
    {
        int lengths = 0, indices = 0; object current = 0L;
        var index = Slot("__index__", () => { indices++; return current; });
        var value = Slot("__len__", () => { lengths++; return index; });
        Assert.Equal(false, Bool([value])); current = 3L; Assert.Equal(true, Bool([value]));
        current = -(BigInteger.One << 100); Assert.Throws<PyValueError>(() => Bool([value]));
        current = BigInteger.One << 100; Assert.Throws<PyOverflowError>(() => Bool([value]));
        Assert.Equal(4, lengths); Assert.Equal(4, indices);
    }
    [Fact]
    public void BadLengthResultsAndDeprecatedIndexBoolHaveExplicitBoundaries()
    {
        Assert.Throws<PyTypeError>(() => Bool([Slot("__len__", () => 1.0)]));
        Assert.Throws<PyTypeError>(() => Bool([Slot("__len__", () => Slot("__index__", () => 1.0))]));
        Assert.Throws<PyNotImplementedError>(() => Bool([Slot("__len__", () => Slot("__index__", () => true))]));
        var failure = new PyLookupError("index failed");
        Assert.Same(failure, Assert.Throws<PyLookupError>(() => Bool([Slot("__len__", () => Slot("__index__", () => throw failure))])));
    }
    [Fact]
    public void OwnBindingAndProtocolErrorsPrepareCallerButBodyKeepsExistingFrames()
    {
        Caller(outer =>
        {
            Prepared(Assert.Throws<PyTypeError>(() => Bool([1L, 2L])), outer);
            Prepared(Assert.Throws<PyTypeError>(() => Bool([Slot("__bool__", () => 1L)])), outer);
            Prepared(Assert.Throws<PyValueError>(() => Bool([Slot("__len__", () => -1L)])), outer);
            Prepared(Assert.Throws<PyOverflowError>(() => Bool([Slot("__len__", () => BigInteger.One << 100)])), outer);
        });
        var error = new PyValueError("prepared body"); Caller(_ => PyErrors.PrepareRaise(error));
        var frames = error.Frames; var context = error.Context;
        Assert.Same(error, Assert.Throws<PyValueError>(() => Bool([Slot("__len__", () => throw error)])));
        Assert.Same(frames, error.Frames); Assert.Same(context, error.Context);
    }
    [Fact]
    public void SharedTruthyAndConstructorHaveTheSameColdLengthSemantics()
    {
        var value = Slot("__len__", () => Slot("__index__", () => 2L));
        Assert.True(PyOps.Truthy(value)); Assert.Equal(true, Bool([value]));
        var bad = Slot("__len__", () => -1L);
        Assert.Throws<PyValueError>(() => PyOps.Truthy(bad));
        Assert.Throws<PyValueError>(() => Bool([bad]));
    }
}
