// 星号解包的异常对象、ambient handler和帧恢复；不把协议阶段的Stop当作正常结束。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime.Tests;

public partial class PyUnpackExTests
{
    [Fact]
    public void SecondAcquisitionRejectsNextOnlyButCanUseItsGetItemFallback()
    {
        var nextOnly = Values(10L); PyObject.DelAttr(nextOnly.Class, "__iter__");
        var source = User(() => throw new PyAssertionError("source.next"), _ => nextOnly);
        Assert.Equal("'StarCursor' object is not iterable", Assert.Throws<PyTypeError>(() => Unpack(source)).PyMessage);
        var events = new List<string>();
        var indexed = Indexed(index => { events.Add($"get:{index}"); return index < 2 ? 70L + index : throw new PyStopIteration(); });
        indexed.Class.SetAttr("__next__", Fn("next", (_, _, _, _) => { events.Add("next"); return 10L; }));
        indexed.Class.SetAttr("__len__", Fn("len", (_, _, _, _) => { events.Add("len"); return 2L; }));
        var indexedSource = User(() => throw new PyAssertionError("source.next"), _ => indexed);
        AssertParts(Unpack(indexedSource), 10L, [70L], 71L);
        Assert.Equal(new[] { "next", "len", "get:0", "get:1", "get:2" }, events);
        // 显式None和非法iter返回都不能偷偷改走getitem。
        indexed.Class.SetAttr("__iter__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => Unpack(indexed));
        indexed.Class.SetAttr("__iter__", Fn("iter", (_, _, _, _) => new PyList([1L])));
        Assert.Equal("iter() returned non-iterator of type 'list'", Assert.Throws<PyTypeError>(() => Unpack(indexed)).PyMessage);
        Assert.Equal(5, events.Count);
        var notIterable = (PyClass)PyOps.MakeClass("NotIterable", [], null);
        Assert.Equal("cannot unpack non-iterable type object", Assert.Throws<PyTypeError>(() => Unpack(notIterable)).PyMessage);
    }

    [Fact]
    public void CallbackFailuresKeepIdentityCauseContextAndTracebackAtEveryStage()
    {
        long frame = PyTraceback.PushFrame("starred.py", "caller");
        var ambient = new PyKeyError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            long depth = PyTraceback.CaptureDepth();
            foreach (string stage in new[] { "first", "prefix", "second", "len", "hint", "tail" })
            foreach (bool stop in new[] { false, true })
            {
                // next的Stop是正常耗尽，另一个测试覆盖；其它四阶段必须传播。
                if (stop && (stage == "prefix" || stage == "tail")) continue;
                PyBaseException error = stop ? new PyStopIteration(stage) : new PyRuntimeError(stage);
                var cause = new PyValueError("cause"); var inner = new PyIndexError("inner");
                error.WithCause(cause);
                object? Fail()
                {
                    PyTraceback.PushFrame("starred.py", $"callback_{stage}");
                    PyErrors.EnterHandler(inner); throw error;
                }
                int iters = 0, calls = 0;
                var source = User(() => ++calls == 1 ? stage == "prefix" ? Fail() : 1L : stage == "tail" ? Fail() : throw new PyStopIteration(), self =>
                { iters++; return (stage == "first" && iters == 1) || (stage == "second" && iters == 2) ? Fail() : self; });
                if (stage == "len" || stage == "hint") source.Class.SetAttr(stage == "len" ? "__len__" : "__length_hint__", Fn(stage, (_, _, _, _) => Fail()));
                Assert.Same(error, Assert.ThrowsAny<PyBaseException>(() => Unpack(source)));
                Assert.Same(cause, error.Cause); Assert.Same(inner, error.Context);
                Assert.Contains(error.Frames, f => f.Function == $"callback_{stage}");
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
                Assert.Same(ambient, PyErrors.Reraise());
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }

    [Fact]
    public void SwallowedStopIndexAndTypeErrorRestoreCallerBeforeQuantityFailure()
    {
        long frame = PyTraceback.PushFrame("starred.py", "caller");
        var ambient = new PyKeyError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            long depth = PyTraceback.CaptureDepth();
            foreach (string stage in new[] { "prefix", "tail", "getitem", "len", "hint" })
            {
                object? Fail(PyBaseException error)
                {
                    PyTraceback.PushFrame("starred.py", "swallowed_callback");
                    PyErrors.EnterHandler(new PyRuntimeError("inner")); throw error;
                }
                int calls = 0;
                PyInstance source;
                if (stage == "getitem") source = Indexed(index => index == 0 ? 1L : Fail(new PyIndexError("end")));
                else
                {
                    source = User(() => ++calls == 1 && stage != "prefix" ? 1L : Fail(new PyStopIteration("end")));
                    if (stage == "len" || stage == "hint") source.Class.SetAttr(stage == "len" ? "__len__" : "__length_hint__",
                        Fn(stage, (_, _, _, _) => Fail(new PyTypeError("ignored"))));
                }
                var error = Assert.Throws<PyValueError>(() => Unpack(source));
                Assert.Equal($"not enough values to unpack (expected at least 2, got {(stage == "prefix" ? 0 : 1)})", error.PyMessage);
                Assert.Same(ambient, error.Context); Assert.Same(ambient, PyErrors.Current);
                Assert.Same(ambient, PyErrors.Reraise()); Assert.Equal(depth, PyTraceback.CaptureDepth());
                Assert.DoesNotContain(error.Frames, f => f.Function == "swallowed_callback");
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }

    [Fact]
    public void UserAndMapRemainResumableAfterTemporaryStop()
    {
        foreach (bool shortPrefix in new[] { false, true })
        {
            int calls = 0;
            var source = User(() => ++calls == 2 ? throw new PyStopIteration("pause") : (long)calls);
            IPyIterator wrapper = PyIter.Create(source);
            if (shortPrefix) Assert.Throws<PyValueError>(() => Unpack(wrapper, 2, 0));
            else { var result = Unpack(wrapper, 1, 0); Assert.Equal(1L, result.Items[0]); Assert.Empty(Assert.IsType<PyList>(result.Items[1]).Items); }
            Assert.True(wrapper.MoveNext()); Assert.Equal(3L, wrapper.Current());
        }
        object callback = PyNativeFunctions.Create("map_body", ["x"], [null], 1,
            args => (long)args[0]! == 2 ? throw new PyStopIteration("pause") : args[0]);
        var map = (PyMap)PyMap.Create([callback, new PyTuple([1L, 2L, 3L])], [], []);
        var mapped = Unpack(map, 1, 0);
        Assert.Equal(1L, mapped.Items[0]); Assert.Empty(Assert.IsType<PyList>(mapped.Items[1]).Items);
        Assert.Equal(3L, map.Next());
    }

    [Fact]
    public void NextIndexErrorPropagatesAndExistingPreparedFramesArePreserved()
    {
        long frame = PyTraceback.PushFrame("starred.py", "caller");
        try
        {
            var error = new PyIndexError("next is not getitem"); PyErrors.PrepareRaise(error);
            var frames = error.Frames; int calls = 0;
            var source = User(() => ++calls == 1 ? 1L : throw error);
            Assert.Same(error, Assert.Throws<PyIndexError>(() => Unpack(source)));
            Assert.Same(frames, error.Frames); Assert.Equal(2, calls);
            // 形参数量无效必须在读取用户协议前拒绝。
            var untouched = User(() => throw new PyAssertionError("next"), _ => throw new PyAssertionError("iter"));
            Assert.Throws<PyValueError>(() => Unpack(untouched, -1, 0));
            Assert.Throws<PyValueError>(() => Unpack(untouched, 0, -1));
            Assert.Throws<PyValueError>(() => Unpack(untouched, long.MaxValue, 1));
        }
        finally { PyTraceback.PopTo(frame); }
    }
}
