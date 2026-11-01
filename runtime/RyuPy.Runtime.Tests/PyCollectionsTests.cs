using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>collections 存储、别名、错误路径和迭代失效的运行时回归。</summary>
public class PyCollectionsTests
{
    private static object New(string name, params object?[] args) => PyOps.CallValue(PyCollections.CreateAttributes()[name], args, [], [])!;
    private static object? Call(object obj, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(obj, name), args, [], []);
    private static object?[] Items(object obj) => PyOps.ToSequence(obj);

    [Fact]
    public void DequeBoundedRingKeepsEndsAndRejectsMutationDuringIteration()
    {
        object deque = New("deque", new PyTuple([1L, 2L]), 2L);
        Call(deque, "append", 3L);
        Assert.Equal(new object?[] { 2L, 3L }, Items(deque));
        Call(deque, "appendleft", 4L);
        Assert.Equal(new object?[] { 4L, 2L }, Items(deque));
        IPyIterator iterator = PyIter.Create(deque);
        Call(deque, "rotate", 0L);
        Assert.Throws<PyRuntimeError>(() => iterator.MoveNext());
        Call(deque, "extend", deque);
        Assert.Equal(new object?[] { 4L, 2L }, Items(deque));
        Assert.Throws<PyTypeError>(() => Call(deque, "pop", 1L));
        Assert.Throws<PyValueError>(() => New("deque", new PyTuple([]), -1L));
    }

    [Fact]
    public void DequeZeroBoundStillConsumesInputAndClearsStoredReferences()
    {
        int advances = 0;
        var source = new CountingIterator(() => advances++);
        object deque = New("deque", source, 0L);
        Assert.Empty(Items(deque));
        Assert.Equal(4, advances);
        Assert.Throws<PyIndexError>(() => Call(deque, "popleft"));
        object value = New("deque");
        Call(value, "append", value);
        Assert.Contains("[...]", PyRepr.Repr(value));
        Call(value, "clear");
        Assert.Equal("deque([])", PyRepr.Repr(value));
    }

    [Fact]
    public void DefaultFactoryOnlyRunsForMissingSubscriptAndFailureDoesNotInsert()
    {
        int calls = 0;
        var factory = PyNativeFunctions.Create("factory", [], [], 0, _ => { calls++; return new PyList([]); });
        object value = New("defaultdict", factory);
        Assert.Same(PyNone.Instance, Call(value, "get", "absent"));
        Assert.False((bool)Call(value, "__contains__", "absent")!);
        Assert.Equal(0, calls);
        object? first = PyOps.GetItem(value, "x");
        Assert.Same(first, PyOps.GetItem(value, "x"));
        Assert.Equal(1, calls);
        PyObject.SetAttr(value, "default_factory", 3L);
        Assert.Throws<PyTypeError>(() => PyOps.GetItem(value, "bad"));
        Assert.False((bool)Call(value, "__contains__", "bad")!);
        Assert.IsType<PyDict>(PyOps.CallBuiltin("dict", [value], [], []));
        Assert.Throws<PyTypeError>(() => New("defaultdict", 3L));
    }

    [Fact]
    public void CounterUpdateSubtractZeroAndStableTiesRemainDistinctFromDictUpdate()
    {
        object value = New("Counter", "abaca");
        Assert.Equal(0L, PyOps.GetItem(value, "missing"));
        Assert.False((bool)Call(value, "__contains__", "missing")!);
        Call(value, "update", "bc");
        var subtract = new PyDict(); subtract.Set("a", 4L);
        Call(value, "subtract", subtract);
        Assert.Equal(-1L, PyOps.GetItem(value, "a"));
        Assert.Equal(3L, Call(value, "total"));
        Assert.Equal(new object?[] { "b", "b", "c", "c" }, Items(Call(value, "elements")!));
        Assert.Equal("Counter({'b': 2, 'c': 2, 'a': -1})", PyRepr.Repr(value));
        Assert.Empty(Items(Call(value, "most_common", 0L)!));
        var other = New("Counter", "bbcc");
        PyOps.SetItem(other, "a", -1L); PyOps.SetItem(other, "zero", 0L);
        Assert.True(PyHash.Equal(value, other));
    }

    [Fact]
    public void ExportsAreCachedAndNativeDictAncestryIsExplicit()
    {
        Assert.Same(PyCollections.CreateAttributes()["deque"], PyCollections.CreateAttributes()["deque"]);
        object dictionary = New("defaultdict");
        Assert.True((bool)PyOps.CallBuiltin("isinstance", [dictionary, PyType.Get("dict")], [], [])!);
        Assert.True((bool)PyOps.CallBuiltin("issubclass", [PyCollections.CreateAttributes()["Counter"], PyType.Get("dict")], [], [])!);
    }

    private sealed class CountingIterator(Action advance) : IPyIterator
    {
        private int _position;
        public bool MoveNext() { advance(); return _position++ < 3; }
        public object Current() => (long)_position;
    }
}
