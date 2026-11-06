using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>代理局部迭代八组合同：公开身份、早停、反向活性和被吞终止的异常现场。</summary>
[Collection("生成器帧栈")]
public class PyMappingProxyIterationTests
{
    private static PyMappingProxy New(object mapping) => (PyMappingProxy)PyMappingProxy.Create([mapping], [], []);
    private static object? Method(object value, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);
    private static PyFunction Slot(string name, string[] parameters, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, new object?[parameters.Length], parameters.Length, body);
    private static PyClass Mapping(string name = "IterMapping")
    {
        var cls = new PyClass(name);
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ => throw new PyIndexError("end")));
        return cls;
    }
    private static PyInstance Iterator(Func<object?[], object?> next, Action? iter = null)
    {
        var cls = new PyClass("RawIterator");
        cls.SetAttr("__iter__", Slot("__iter__", ["self"], args => { iter?.Invoke(); return args[0]; }));
        cls.SetAttr("__next__", Slot("__next__", ["self"], next));
        return new PyInstance(cls);
    }

    [Fact]
    public void PublicIterationKeepsRawIdentityWithoutValidationCallbacks()
    {
        int iterCalls = 0, nextCalls = 0;
        PyInstance raw = Iterator(_ => { nextCalls++; throw new PyStopIteration(); }, () => iterCalls++);
        var cls = Mapping(); cls.SetAttr("__iter__", Slot("__iter__", ["self"], _ => raw));
        PyMappingProxy proxy = New(new PyInstance(cls));
        Assert.Same(raw, Method(proxy, "__iter__"));
        Assert.Same(raw, PyOps.CallBuiltin("iter", [proxy], [], []));
        Assert.Equal(0, iterCalls); Assert.Equal(0, nextCalls);
        raw.Class.SetAttr("__next__", PyNone.Instance);
        Assert.Same(raw, Method(proxy, "__iter__"));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("next", [raw], [], []));
    }

    [Fact]
    public void InternalAndMaterializedIterationNeverIteratesRawResultAgain()
    {
        int outerCalls = 0, rawCalls = 0, next = 0;
        PyInstance raw = Iterator(_ => next++ == 0 ? 7L : throw new PyStopIteration(), () => rawCalls++);
        var cls = Mapping(); cls.SetAttr("__iter__", Slot("__iter__", ["self"], _ => { outerCalls++; return raw; }));
        PyMappingProxy proxy = New(new PyInstance(cls));
        Assert.Equal(new object?[] { 7L }, PyOps.ToSequence(proxy));
        Assert.Equal(1, outerCalls); Assert.Equal(0, rawCalls);
        next = 0; IPyIterator internalIterator = PyIter.Create(proxy);
        Assert.True(internalIterator.MoveNext()); Assert.Equal(7L, internalIterator.Current());
        Assert.False(internalIterator.MoveNext()); Assert.Equal(2, outerCalls); Assert.Equal(0, rawCalls);
        next = 0;
        var list = Assert.IsType<PyList>(PyOps.CallBuiltin("list", [proxy], [], []));
        Assert.Equal(1, list.Count); Assert.Equal(3, outerCalls); Assert.Equal(0, rawCalls);
    }

    [Fact]
    public void IndexedFallbackIsLazyFiniteAndContainsStopsAtFirstMatch()
    {
        var indices = new List<long>(); var cls = Mapping("IndexedMapping");
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], args =>
        {
            long index = (long)args[1]!; indices.Add(index);
            return index < 3 ? index + 10 : throw new PyIndexError("end");
        }));
        cls.SetAttr("__len__", Slot("__len__", ["self"], _ => throw new PyValueError("must not ask length")));
        PyMappingProxy proxy = New(new PyInstance(cls));
        IPyIterator iterator = Assert.IsAssignableFrom<IPyIterator>(Method(proxy, "__iter__"));
        Assert.Empty(indices);
        for (int index = 0; index < 3; index++) Assert.True(iterator.MoveNext());
        Assert.False(iterator.MoveNext()); Assert.False(iterator.MoveNext());
        Assert.Equal(new long[] { 0, 1, 2, 3 }, indices);
        indices.Clear(); Assert.True(PyOps.Contains(proxy, 10L)); Assert.Equal(new long[] { 0 }, indices);
        indices.Clear(); Assert.False(PyOps.Contains(proxy, 99L)); Assert.Equal(new long[] { 0, 1, 2, 3 }, indices);
    }

    [Fact]
    public void IterSlotsIgnoreInstanceFieldsAndRejectInvalidResults()
    {
        var cls = Mapping(); cls.SetAttr("__iter__", PyNone.Instance);
        PyMappingProxy proxy = New(new PyInstance(cls));
        Assert.Throws<PyTypeError>(() => Method(proxy, "__iter__"));
        cls.SetAttr("__iter__", Slot("__iter__", ["self"], _ => new PyList()));
        Assert.Throws<PyTypeError>(() => Method(proxy, "__iter__"));
        cls.SetAttr("__iter__", Slot("__iter__", ["self"], _ => PyNone.Instance));
        Assert.Throws<PyTypeError>(() => Method(proxy, "__iter__"));
        var indexed = new PyInstance(Mapping("InstanceFakeIter"));
        PyObject.SetAttr(indexed, "__iter__", Slot("fake", [], _ => throw new PyValueError("instance fake")));
        Assert.False(Assert.IsAssignableFrom<IPyIterator>(Method(New(indexed), "__iter__")).MoveNext());
        var noneItem = Mapping("NoneItem"); noneItem.SetAttr("__getitem__", PyNone.Instance);
        IPyIterator cursor = Assert.IsAssignableFrom<IPyIterator>(Method(New(new PyInstance(noneItem)), "__iter__"));
        Assert.Throws<PyTypeError>(() => cursor.MoveNext());
        foreach (PyBaseException original in new PyBaseException[] { new PyStopIteration("acquire"), new PyValueError("acquire") })
        {
            var active = new PyRuntimeError("raw context"); object? previous = PyErrors.EnterHandler(active);
            long depth = PyTraceback.PushFrame("proxy-iteration.py", "raw caller");
            try
            {
                cls.SetAttr("__iter__", Slot("__iter__", ["self"], _ =>
                { PyTraceback.PushFrame("raw-callback.py", "iter"); throw original; }));
                Assert.Same(original, Assert.ThrowsAny<PyBaseException>(() => Method(proxy, "__iter__")));
                Assert.Contains(original.Frames, frame => frame.File == "raw-callback.py");
                Assert.Same(active, original.Context);
                var after = new PyValueError("after raw"); PyErrors.PrepareRaise(after);
                Assert.DoesNotContain(after.Frames, frame => frame.File == "raw-callback.py");
            }
            finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        }
    }

    [Fact]
    public void ContainsUsesOwnSlotAndIdentityBeforeEquality()
    {
        PyMappingProxy text = New("abc");
        Assert.True(PyOps.Contains(text, "bc")); Assert.False(PyOps.Contains(text, "z"));
        Assert.Equal("'in <string>' requires string as left operand, not int",
            Assert.Throws<PyTypeError>(() => PyOps.Contains(text, 1L)).Message);
        int calls = 0; var cls = Mapping();
        cls.SetAttr("__contains__", Slot("__contains__", ["self", "needle"], _ => { calls++; return new PyList([1L]); }));
        PyMappingProxy proxy = New(new PyInstance(cls));
        Assert.True(PyOps.Contains(proxy, "x")); Assert.Equal(1, calls);
        cls.SetAttr("__contains__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => PyOps.Contains(proxy, "x"));
        object nan = double.NaN; var indexed = Mapping("NaNMapping");
        indexed.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], args =>
            (long)args[1]! == 0 ? nan : throw new PyValueError("must stop at identical object")));
        Assert.True(PyOps.Contains(New(new PyInstance(indexed)), nan));
    }

    [Fact]
    public void DictReverseUsesLiveIndependentCursorsAndStableExhaustion()
    {
        var dictionary = new PyDict(); dictionary.Set("a", 1L); dictionary.Set("gone", 0L);
        dictionary.Set("b", 2L); dictionary.Delete("gone"); PyMappingProxy proxy = New(dictionary);
        IPyIterator first = Assert.IsAssignableFrom<IPyIterator>(Method(proxy, "__reversed__"));
        IPyIterator second = Assert.IsAssignableFrom<IPyIterator>(PyOps.CallBuiltin("reversed", [proxy], [], []));
        Assert.True(first.MoveNext()); Assert.Equal("b", first.Current());
        dictionary.Set("a", 9L); Assert.True(first.MoveNext()); Assert.Equal("a", first.Current());
        Assert.False(first.MoveNext());
        Assert.True(second.MoveNext()); Assert.Equal("b", second.Current());
        dictionary.Set("c", 3L);
        Assert.False(first.MoveNext()); Assert.Throws<PyRuntimeError>(() => second.MoveNext());
    }

    [Fact]
    public void ReversedDelegatesOrdinaryMethodAndKeepsArbitraryReturnIdentity()
    {
        var cls = Mapping(); var marker = new object(); var instance = new PyInstance(cls);
        cls.SetAttr("__reversed__", Slot("__reversed__", ["self"], _ => marker));
        PyMappingProxy proxy = New(instance);
        Assert.Same(marker, PyOps.CallBuiltin("reversed", [proxy], [], []));
        var replacement = new object();
        PyObject.SetAttr(instance, "__reversed__", Slot("replacement", [], _ => replacement));
        Assert.Same(replacement, PyOps.CallBuiltin("reversed", [proxy], [], []));
        Assert.Throws<PyAttributeError>(() => Method(New(new PyInstance(Mapping("NoReverse"))), "__reversed__"));
    }

    [Fact]
    public void SwallowedTerminationKeepsOriginalSceneAndOtherErrorsPropagate()
    {
        foreach (PyBaseException ending in new PyBaseException[] { new PyIndexError("end"), new PyStopIteration() })
        {
            var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
            long depth = PyTraceback.PushFrame("proxy-iteration.py", "caller");
            try
            {
                var cls = Mapping(); cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ =>
                {
                    PyTraceback.PushFrame("native-callback.py", "item");
                    throw ending; // 刻意不预 Prepare，由吞终止边界保存此同一对象的现场。
                }));
                IPyIterator cursor = Assert.IsAssignableFrom<IPyIterator>(Method(New(new PyInstance(cls)), "__iter__"));
                Assert.False(cursor.MoveNext()); Assert.Same(outer, ending.Context);
                Assert.Contains(ending.Frames, frame => frame.File == "native-callback.py");
                PyStackFrame[] frames = ending.Frames; Assert.False(cursor.MoveNext()); Assert.Same(frames, ending.Frames);
                Assert.Same(outer, PyErrors.Current);
                var subsequent = new PyValueError("after"); PyErrors.PrepareRaise(subsequent);
                Assert.DoesNotContain(subsequent.Frames, frame => frame.File == "native-callback.py");
                var failure = new PyValueError("not termination"); PyErrors.PrepareRaise(failure);
                PyStackFrame[] originalFrames = failure.Frames;
                cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ => throw failure));
                IPyIterator failing = Assert.IsAssignableFrom<IPyIterator>(Method(New(new PyInstance(cls)), "__iter__"));
                Assert.Same(failure, Assert.Throws<PyValueError>(() => failing.MoveNext()));
                Assert.Same(originalFrames, failure.Frames); Assert.Same(outer, failure.Context);
                var fresh = new PyValueError("fresh nontermination");
                cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ =>
                { PyTraceback.PushFrame("fresh-callback.py", "item"); throw fresh; }));
                IPyIterator freshCursor = Assert.IsAssignableFrom<IPyIterator>(Method(New(new PyInstance(cls)), "__iter__"));
                Assert.Same(fresh, Assert.Throws<PyValueError>(() => freshCursor.MoveNext()));
                Assert.Contains(fresh.Frames, frame => frame.File == "fresh-callback.py");
                Assert.Same(outer, fresh.Context);
            }
            finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        }
        var mapEnding = new PyStopIteration("map end"); var mapContext = new PyValueError("outer map");
        object? beforeMap = PyErrors.EnterHandler(mapContext);
        long mapDepth = PyTraceback.PushFrame("proxy-iteration.py", "map caller");
        try
        {
            object function = Slot("map_callback", ["value"], _ =>
            {
                PyTraceback.PushFrame("map-callback.py", "stop"); throw mapEnding;
            });
            object map = PyMap.Create([function, new PyTuple([1L])], [], []);
            var source = Mapping("MapResult"); source.SetAttr("__iter__", Slot("__iter__", ["self"], _ => map));
            Assert.Empty(PyOps.ToSequence(New(new PyInstance(source))));
            Assert.Same(mapContext, mapEnding.Context);
            Assert.Contains(mapEnding.Frames, frame => frame.File == "map-callback.py");
            var after = new PyValueError("after map"); PyErrors.PrepareRaise(after);
            Assert.DoesNotContain(after.Frames, frame => frame.File == "map-callback.py");
            Assert.Same(mapContext, PyErrors.Current);
        }
        finally { PyTraceback.PopTo(mapDepth); PyErrors.LeaveHandler(beforeMap); }
    }
}
