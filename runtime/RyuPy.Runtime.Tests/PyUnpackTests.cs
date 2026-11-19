// 非星号解包的有限消费、冻结与局部异常恢复；不依赖 SDK 子进程或第三方项目。
using System;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyUnpackTests
{
    private static PyTuple Unpack(object? source, long count) =>
        Assert.IsType<PyTuple>(PyOps.UnpackSequence(source, count));
    private static PyFunction Fn(string name, PyCall body) => new(
        name, PySignature.Create(name, ["self"], ["pos"], ["0"]), body, []);
    private static PyInstance User(Func<object?> next, Action? iter = null)
    {
        var cls = (PyClass)PyOps.MakeClass("UnpackCursor", [], null);
        cls.SetAttr("__iter__", Fn("__iter__", (_, args, _, _) => { iter?.Invoke(); return args[0]; }));
        cls.SetAttr("__next__", Fn("__next__", (_, _, _, _) => next()));
        foreach (string name in new[] { "__len__", "__length_hint__", "close" })
            cls.SetAttr(name, Fn(name, (_, _, _, _) => throw new PyAssertionError("unexpected protocol")));
        return (PyInstance)cls.Invoke([], [], [])!;
    }
    private static PyInstance Indexed(Func<long, object?> get)
    {
        var cls = (PyClass)PyOps.MakeClass("UnpackSequence", [], null);
        cls.SetAttr("__getitem__", new PyFunction("__getitem__",
            PySignature.Create("__getitem__", ["self", "index"], ["pos", "pos"], ["0", "0"]),
            (_, args, _, _) => get((long)args[1]!), []));
        cls.SetAttr("__len__", Fn("__len__", (_, _, _, _) => throw new PyAssertionError("unexpected len")));
        return (PyInstance)cls.Invoke([], [], [])!;
    }
    private sealed class Cursor(params object?[] values) : IPyIterator
    {
        public int Calls { get; private set; }
        private object? _current;
        public bool MoveNext()
        {
            int index = Calls++;
            if (index >= values.Length) return false;
            if (values[index] is PyBaseException error) throw error;
            _current = values[index]; return true;
        }
        public object? Current() => _current;
    }

    [Fact]
    public void ExactTupleValidatesAndPreservesIdentity()
    {
        var tuple = new PyTuple([1L, PyNone.Instance]);
        Assert.Same(tuple, Unpack(tuple, 2));
        Assert.Contains("expected 3, got 2", Assert.Throws<PyValueError>(() => Unpack(tuple, 3)).PyMessage);
        Assert.Contains("too many", Assert.Throws<PyValueError>(() => Unpack(tuple, 1)).PyMessage);
        Assert.Throws<PyValueError>(() => Unpack(tuple, -1));
        Assert.Throws<PyValueError>(() => Unpack(tuple, long.MaxValue));
    }

    [Fact]
    public void ExactListFreezesWithoutMaterializingNumericBacking()
    {
        var list = Assert.IsType<PyList>(PyNumericListOps.Create());
        PyNumericListOps.AppendF64(list, 1234.5); PyNumericListOps.AppendF64(list, double.NaN);
        PyTuple frozen = Unpack(list, 2);
        Assert.Null(typeof(PyList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list));
        Assert.Same(list.GetItem(0), frozen.Items[0]);
        Assert.Same(list.GetItem(1), frozen.Items[1]);
        list.SetItem(0, 9L);
        Assert.Equal(1234.5, frozen.Items[0]);
    }

    [Fact]
    public void IteratorAcquiredOnceAndReadsNPlusOne()
    {
        int acquired = 0, calls = 0;
        var user = User(() => ++calls <= 2 ? (object?)PyNone.Instance : throw new PyStopIteration(), () => acquired++);
        Assert.Equal(new object?[] { PyNone.Instance, PyNone.Instance }, Unpack(user, 2).Items);
        Assert.Equal(1, acquired); Assert.Equal(3, calls);
        var empty = new Cursor(); Assert.Empty(Unpack(empty, 0).Items); Assert.Equal(1, empty.Calls);
        var indexes = new System.Collections.Generic.List<long>();
        var indexed = Indexed(index => { indexes.Add(index); return index < 2 ? index : throw new PyIndexError("end"); });
        Assert.Equal(new object?[] { 0L, 1L }, Unpack(indexed, 2).Items);
        Assert.Equal(new long[] { 0, 1, 2 }, indexes);
    }

    [Fact]
    public void ShortIteratorReportsActualCount()
    {
        foreach (var source in new[] { new Cursor(), new Cursor(1L) })
        {
            var error = Assert.Throws<PyValueError>(() => Unpack(source, 2));
            Assert.Equal($"not enough values to unpack (expected 2, got {source.Calls - 1})", error.PyMessage);
        }
        foreach (PyBaseException end in new PyBaseException[] { new PyIndexError("end"), new PyStopIteration("end") })
        {
            var sequence = Indexed(index => index < 2 ? index : throw end);
            Assert.Equal(2, Unpack(sequence, 2).Length);
            Assert.Equal("not enough values to unpack (expected 3, got 2)",
                Assert.Throws<PyValueError>(() => Unpack(sequence, 3)).PyMessage);
        }
    }

    [Fact]
    public void ExtraValueStopsImmediatelyAndPreservesRemainder()
    {
        var source = new Cursor(1L, 2L, 3L, 4L);
        Assert.Equal("too many values to unpack (expected 2)", Assert.Throws<PyValueError>(() => Unpack(source, 2)).PyMessage);
        Assert.Equal(3, source.Calls);
        Assert.True(source.MoveNext()); Assert.Equal(4L, source.Current());
    }

    [Fact]
    public void ProbeErrorPreservesExceptionIdentityAndFrames()
    {
        long depth = PyTraceback.PushFrame("unpack.py", "caller");
        try
        {
            var error = new PyRuntimeError("probe");
            PyErrors.PrepareRaise(error);
            var frames = error.Frames;
            var source = new Cursor(1L, 2L, error, 4L);
            Assert.Same(error, Assert.Throws<PyRuntimeError>(() => Unpack(source, 2)));
            Assert.Same(frames, error.Frames);
            Assert.True(source.MoveNext()); Assert.Equal(4L, source.Current());
            foreach (PyBaseException failure in new PyBaseException[] { new PyKeyError("key"), new PyValueError("value") })
                Assert.Same(failure, Assert.ThrowsAny<PyBaseException>(() => Unpack(Indexed(_ => throw failure), 1)));
            var nextIndexError = new PyIndexError("next is not sequence fallback");
            Assert.Same(nextIndexError, Assert.Throws<PyIndexError>(() => Unpack(User(() => throw nextIndexError), 1)));
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void IteratorConstructionFailureIsNotExhaustion()
    {
        foreach (PyBaseException error in new PyBaseException[] { new PyStopIteration("iter"), new PyTypeError("user iter") })
        {
            var user = User(() => 1L, () => throw error);
            Assert.Same(error, Assert.ThrowsAny<PyBaseException>(() => Unpack(user, 2)));
        }
        Assert.Equal("'int' object is not iterable", Assert.Throws<PyTypeError>(() => Unpack(1L, 2)).PyMessage);
        int getCalls = 0;
        var disabled = Indexed(_ => { getCalls++; return 1L; });
        disabled.Class.SetAttr("__iter__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => Unpack(disabled, 1));
        var failed = new PyTypeError("iter must win");
        disabled.Class.SetAttr("__iter__", Fn("__iter__", (_, _, _, _) => throw failed));
        Assert.Same(failed, Assert.Throws<PyTypeError>(() => Unpack(disabled, 1)));
        Assert.Equal(0, getCalls);
    }

    [Fact]
    public void InvalidIteratorResultDoesNotReiterate()
    {
        int calls = 0;
        var cls = (PyClass)PyOps.MakeClass("BadUnpack", [], null);
        cls.SetAttr("__iter__", Fn("__iter__", (_, _, _, _) => { calls++; return new PyList([1L, 2L]); }));
        var source = cls.Invoke([], [], []);
        Assert.Equal("iter() returned non-iterator of type 'list'", Assert.Throws<PyTypeError>(() => Unpack(source, 2)).PyMessage);
        Assert.Equal(1, calls);
        var indexed = Indexed(_ => throw new PyAssertionError("fallback must not run"));
        indexed.Class.SetAttr("__iter__", Fn("__iter__", (_, _, _, _) => new PyList([1L])));
        Assert.Throws<PyTypeError>(() => Unpack(indexed, 1));
    }

    [Fact]
    public void UserIteratorCanResumeAfterConsumedStop()
    {
        int calls = 0;
        var user = User(() => ++calls == 2 ? throw new PyStopIteration("pause") : (long)calls);
        // 直接传已包装游标，确认 helper 没有永久设置其 finished 标志。
        IPyIterator iterator = PyIter.Create(user);
        Assert.Throws<PyValueError>(() => Unpack(iterator, 2));
        Assert.True(iterator.MoveNext()); Assert.Equal(3L, iterator.Current());
    }

    [Fact]
    public void MapCallbackStopEndsThisUnpackWithoutClosingMap()
    {
        object function = PyNativeFunctions.Create("f", ["x"], [null], 1,
            args => (long)args[0]! == 2 ? throw new PyStopIteration("pause") : args[0]);
        var map = (PyMap)PyMap.Create([function, new PyTuple([1L, 2L, 3L])], [], []);
        Assert.Throws<PyValueError>(() => Unpack(map, 2));
        Assert.Equal(3L, map.Next());
    }

    [Fact]
    public void SwallowedStopRestoresDepthAndAmbientContext()
    {
        long frame = PyTraceback.PushFrame("unpack.py", "caller");
        var ambient = new PyKeyError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            long depth = PyTraceback.CaptureDepth();
            for (int repetition = 0; repetition < 2; repetition++)
            {
                object callback = PyNativeFunctions.Create("stop", ["x"], [null], 1, _ =>
                {
                    PyTraceback.PushFrame("unpack.py", "swallowed_callback");
                    PyErrors.EnterHandler(new PyValueError("inner"));
                    throw new PyStopIteration();
                });
                var map = (PyMap)PyMap.Create([callback, new PyTuple([1L])], [], []);
                Assert.Empty(Unpack(map, 0).Items);
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
                var shortMap = (PyMap)PyMap.Create([callback, new PyTuple([1L])], [], []);
                var error = Assert.Throws<PyValueError>(() => Unpack(shortMap, 1));
                Assert.Same(ambient, error.Context); Assert.Same(ambient, PyErrors.Reraise());
                Assert.DoesNotContain(error.Frames, f => f.Function == "swallowed_callback");
                Assert.Equal(depth, PyTraceback.CaptureDepth());
                var indexed = Indexed(_ =>
                {
                    PyTraceback.PushFrame("unpack.py", "getitem_end");
                    PyErrors.EnterHandler(new PyValueError("inner index"));
                    throw new PyIndexError("end");
                });
                Assert.Empty(Unpack(indexed, 0).Items);
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
                var shortIndex = Assert.Throws<PyValueError>(() => Unpack(indexed, 1));
                Assert.Same(ambient, shortIndex.Context);
                Assert.DoesNotContain(shortIndex.Frames, f => f.Function == "getitem_end");
                Assert.Equal(depth, PyTraceback.CaptureDepth());
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }

    [Fact]
    public void PropagatedCallbackFailureRestoresCallerButRetainsOriginalTraceback()
    {
        long frame = PyTraceback.PushFrame("unpack.py", "caller");
        var ambient = new PyKeyError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            long depth = PyTraceback.CaptureDepth();
            foreach (bool duringAcquisition in new[] { false, true })
            {
                var error = new PyRuntimeError("callback"); var inner = new PyValueError("inner");
                object? Fail()
                {
                    PyTraceback.PushFrame("unpack.py", "failing_callback");
                    PyErrors.EnterHandler(inner); throw error;
                }
                var source = duringAcquisition ? User(() => 1L, () => { Fail(); }) : User(Fail);
                Assert.Same(error, Assert.Throws<PyRuntimeError>(() => Unpack(source, 1)));
                Assert.Same(inner, error.Context);
                Assert.Contains(error.Frames, f => f.Function == "failing_callback");
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }
}
