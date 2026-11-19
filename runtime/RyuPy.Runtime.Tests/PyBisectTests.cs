// bisect 的真实算法、调用方向、key 时机及失败不插入；完整 Python 序列协议另由 e2e 差分。
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyBisectTests
{
    private static readonly Dictionary<string, object?> Attributes = PyBisect.CreateAttributes();
    private static object? Call(string name, params object?[] args) => PyOps.CallValue(Attributes[name], args, [], []);
    private static object? KeyCall(string name, object?[] args, object? key) =>
        PyOps.CallValue(Attributes[name], args, ["key"], [key]);

    [Fact]
    public void DuplicateBoundsAliasesAndSubranges()
    {
        var values = new PyList([1L, 2L, 2L, 4L]);
        Assert.Same(Attributes["bisect_right"], Attributes["bisect"]);
        Assert.Same(Attributes["insort_right"], Attributes["insort"]);
        Assert.Equal(1L, Call("bisect_left", values, 2L));
        Assert.Equal(3L, Call("bisect_right", values, 2L));
        Assert.Equal(2L, Call("bisect_left", values, 2L, 2L, 3L));
        Assert.Equal(2L, Call("bisect_right", values, 2L, 1L, 2L));
        Assert.Equal(1L, Call("bisect_left", values, 2L, 0L, -1L));
        Assert.Equal(0L, Call("bisect_right", values, 2L, 0L, -2L));
        Assert.Equal(9L, Call("bisect_left", values, 2L, 9L, 1L));
        Assert.Same(PyNone.Instance, Call("insort", values, 3L));
        Assert.Equal(new object?[] { 1L, 2L, 2L, 3L, 4L }, PyOps.ToSequence(values));
    }

    [Fact]
    public void KeyIsAppliedOnlyToSequenceForSearchAndOnceToInsertedValue()
    {
        var events = new List<object?>();
        var key = PyNativeFunctions.CreateUnary("key", value =>
        {
            events.Add(value);
            return PyOps.GetItem(value, 0L);
        });
        object first = new PyTuple([1L, "a"]), second = new PyTuple([2L, "old"]), third = new PyTuple([3L, "c"]);
        var values = new PyList([first, second, third]);
        Assert.Equal(1L, KeyCall("bisect_left", [values, 2L], key));
        Assert.Equal(new object?[] { second, first }, events);
        events.Clear();
        object added = new PyTuple([2L, "new"]);
        Assert.Same(PyNone.Instance, KeyCall("insort_right", [values, added], key));
        Assert.Equal(new object?[] { added, second, third }, events);
        Assert.Same(added, PyOps.GetItem(values, 2L));
    }

    [Fact]
    public void EmptyRangeAndArgumentErrorsRetainCallbackOrder()
    {
        var values = new PyList([1L]);
        int calls = 0;
        var key = PyNativeFunctions.CreateUnary("key", value => { calls++; return value; });
        Assert.Throws<PyValueError>(() => KeyCall("insort", [values, 2L, -1L], key));
        Assert.Equal(1, calls);
        Assert.Throws<PyTypeError>(() => KeyCall("insort", [values, 2L, 0.5], key));
        Assert.Equal(1, calls);
        Assert.Equal(0L, KeyCall("bisect", [values, 2L, 0L, 0L], 42L));
        Assert.Throws<PyTypeError>(() => KeyCall("insort", [values, 2L, 0L, 0L], 42L));
        Assert.Throws<PyIndexError>(() => Call("bisect_left", values, 2L, 0L, 10L));
        Assert.Throws<PyOverflowError>(() => Call("bisect", values, 2L, BigInteger.One << 100));
        Assert.Throws<PyTypeError>(() => Call("bisect", values, 2L, 0L, 1L, PyNone.Instance));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Attributes["bisect"], [values, 2L], ["missing"], [1L]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Attributes["bisect"], [values, 2L], ["x"], [1L]));
        Assert.Equal(new object?[] { 1L }, PyOps.ToSequence(values));
    }

    [Fact]
    public void KeyFailureDoesNotInsertAndNanUsesOnlyLessThan()
    {
        var values = new PyList([1L, 2L]);
        var key = PyNativeFunctions.CreateUnary("key", _ => throw new PyValueError("callback"));
        Assert.Throws<PyValueError>(() => KeyCall("insort_left", [values, 3L], key));
        Assert.Equal(new object?[] { 1L, 2L }, PyOps.ToSequence(values));
        Assert.Equal(0L, Call("bisect_left", values, double.NaN));
        Assert.Equal(2L, Call("bisect_right", values, double.NaN));
        Assert.Equal(1L, Call("bisect", new PyTuple([1L, 3L]), 2L));
    }
}
