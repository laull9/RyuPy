// zip_longest 阶段 A 内部专项：构造只取 iter、逐槽 next、共享输入、终止状态及冷协议边界。
// 保留阶段 A 内部入口细测；阶段 B 另锁公开类型/导出，不把本文件当源码验收。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyZipLongestCoreTests
{
    private static IPyIterator Construct(object?[] args, string[]? names = null, object?[]? values = null)
    {
        Type type = typeof(PyToolIterator).Assembly.GetType("RyuPy.Runtime.PyZipLongest")!;
        try
        {
            return (IPyIterator)type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [args, names ?? [], values ?? []])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw;
        }
    }
    private static IPyIterator New(params object?[] args) => Construct(args);
    private static PyTuple Row(IPyIterator iterator)
    {
        Assert.True(iterator.MoveNext()); return Assert.IsType<PyTuple>(iterator.Current());
    }
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);

    [Fact]
    public void ConstructorChecksKeywordsBeforeTakingEveryIteratorFromLeftToRight()
    {
        var log = new List<string>(); var a = new User(log, "A", [1L]); var b = new User(log, "B", [2L]);
        IPyIterator iterator = New(a.Value, b.Value);
        Assert.Equal<string>(["iter:A", "iter:B"], log); Assert.Equal(0, a.Position); Assert.Equal(0, b.Position);
        Assert.Equal<object?>([1L, 2L], Row(iterator).Items);
        log.Clear();
        foreach (string[] names in new[] { new[] { "bad" }, new[] { "strict" }, new[] { "fillvalue", "bad" } })
        {
            object?[] values = names.Length == 1 ? [true] : [PyNone.Instance, true];
            Assert.Equal("zip_longest() got an unexpected keyword argument",
                Assert.Throws<PyTypeError>(() => Construct([a.Value], names, values)).PyMessage);
        }
        Assert.Empty(log);
        var original = new PyRuntimeError("iter marker");
        b.Value.Class.SetAttr("__iter__", Method("iter", _ => { log.Add("iter:bad"); throw original; }));
        var c = new User(log, "C", [3L]);
        Assert.Same(original, Assert.Throws<PyRuntimeError>(() => New(a.Value, b.Value, c.Value)));
        Assert.Equal<string>(["iter:A", "iter:bad"], log); Assert.Equal(0, c.Position);
        // 阶段 A 的未注册断言在 B 明确迁为真实导出/实例身份，保留其余内部覆盖。
        PyNativeType type = Assert.IsType<PyNativeType>(PyItertools.CreateAttributes()["zip_longest"]);
        Assert.Same(type, PyObject.TypeOf(iterator)); Assert.True(type.Accepts(iterator));
    }

    [Fact]
    public void EmptyAndUnequalInputsRetireSlotsAndNeverPublishAnAllFillTail()
    {
        Assert.False(New().MoveNext()); Assert.False(New(new PyTuple([]), new PyTuple([])).MoveNext());
        var log = new List<string>(); var a = new User(log, "A", [1L, 2L]); var b = new User(log, "B", [10L]);
        var c = new User(log, "C", [100L, 200L, 300L]); IPyIterator iterator = New(a.Value, b.Value, c.Value);
        PyTuple first = Row(iterator); PyTuple second = Row(iterator); PyTuple third = Row(iterator);
        Assert.Equal<object?>([1L, 10L, 100L], first.Items);
        Assert.Equal<object?>([2L, PyNone.Instance, 200L], second.Items);
        Assert.Equal<object?>([PyNone.Instance, PyNone.Instance, 300L], third.Items);
        Assert.NotSame(first, second); Assert.NotSame(first.Items, second.Items); Assert.NotSame(second, third);
        Assert.False(iterator.MoveNext()); int count = log.Count; Assert.False(iterator.MoveNext()); Assert.Equal(count, log.Count);
        Assert.Equal<string>(["iter:A", "iter:B", "iter:C", "next:A:0", "next:B:0", "next:C:0",
            "next:A:1", "next:B:1", "next:C:1", "next:A:2", "next:C:2", "next:C:3"], log);
    }

    [Fact]
    public void FillIsOneOriginalReferenceAndItsProtocolsAreNeverInvoked()
    {
        var fill = new PyList(); IPyIterator iterator = Construct([new PyTuple([1L, 2L]), new PyTuple([]), new PyTuple([])], ["fillvalue"], [fill]);
        PyTuple first = Row(iterator); fill.Append("changed"); PyTuple second = Row(iterator);
        Assert.Same(fill, first.Items[1]); Assert.Same(fill, first.Items[2]); Assert.Same(fill, second.Items[1]); Assert.Same(fill, second.Items[2]);
        var cls = new PyClass("Fill");
        foreach (string name in new[] { "__bool__", "__str__", "__iter__", "__len__" })
            cls.SetAttr(name, Method(name, _ => throw new InvalidOperationException("fill protocol invoked")));
        object original = new PyInstance(cls);
        Assert.Same(original, Row(Construct([new PyTuple([]), new PyTuple([1L])], ["fillvalue"], [original])).Items[0]);
    }

    [Fact]
    public void SharedIteratorFreezesEachValueImmediatelyAndRetirementIsPerSlot()
    {
        var raw = new Counting(3); IPyIterator iterator = Construct([raw, raw], ["fillvalue"], ["F"]);
        Assert.Equal<object?>([1L, 2L], Row(iterator).Items); Assert.Equal<object?>([3L, "F"], Row(iterator).Items); Assert.False(iterator.MoveNext());
        var log = new List<string>(); var user = new User(log, "revive", [new PyStopIteration("pause"), 9L, new PyStopIteration(), 77L]);
        iterator = Construct([user.Value, user.Value], ["fillvalue"], ["F"]);
        Assert.Equal<object?>(["F", 9L], Row(iterator).Items); Assert.False(iterator.MoveNext());
        Assert.Equal(77L, PyOps.CallBuiltin("next", [user.Value], [], []));
        Assert.Equal<string>(["iter:revive", "iter:revive", "next:revive:0", "next:revive:1", "next:revive:2", "next:revive:3"], log);
    }

    [Fact]
    public void AlreadyFinishedHiddenAdapterDoesNotReplaceTheRealUserIterator()
    {
        var log = new List<string>(); var user = new User(log, "revive", [new PyStopIteration(), 9L, new PyStopIteration()]);
        IPyIterator adapter = PyIter.Create(user.Value); Assert.False(adapter.MoveNext());
        IPyIterator iterator = New(adapter);
        Assert.Equal<object?>([9L], Row(iterator).Items); Assert.False(iterator.MoveNext());
        Assert.Equal<string>(["iter:revive", "next:revive:0", "iter:revive", "next:revive:1", "next:revive:2"], log);
    }

    [Fact]
    public void AnyNonStopFailureClosesTheZipAfterTheAlreadyConsumedPrefix()
    {
        for (int failed = 0; failed < 3; failed++)
        {
            var log = new List<string>(); var marker = new PyRuntimeError("marker" + failed);
            var users = new[] { new User(log, "A", [1L, 2L]), new User(log, "B", [10L, 20L]), new User(log, "C", [100L, 200L]) };
            users[failed].Steps[0] = marker; IPyIterator iterator = New(users[0].Value, users[1].Value, users[2].Value);
            Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => iterator.MoveNext()));
            for (int index = 0; index < 3; index++) Assert.Equal(index <= failed ? 1 : 0, users[index].Position);
            int calls = log.Count; Assert.False(iterator.MoveNext()); Assert.False(iterator.MoveNext()); Assert.Equal(calls, log.Count);
            Assert.NotNull(PyOps.CallBuiltin("next", [users[failed].Value], [], []));
        }
        var log2 = new List<string>(); var indexError = new PyIndexError("ordinary next error");
        IPyIterator bad = New(new User(log2, "index", [indexError]).Value);
        Assert.Same(indexError, Assert.Throws<PyIndexError>(() => bad.MoveNext())); Assert.False(bad.MoveNext());
    }

    [Fact]
    public void SequenceFallbackUsesIntegerIndicesAndDoesNotAskForLength()
    {
        foreach (PyBaseException end in new PyBaseException[] { new PyIndexError("end"), new PyStopIteration("end"), new StopChild() })
        {
            var indices = new List<long>(); var cls = new PyClass("Sequence");
            cls.SetAttr("__getitem__", Method("getitem", args =>
            {
                long index = Assert.IsType<long>(args[1]); indices.Add(index); if (index == 2) throw end; return index + 10;
            }));
            cls.SetAttr("__len__", Method("len", _ => throw new InvalidOperationException("length queried")));
            cls.SetAttr("__length_hint__", Method("hint", _ => throw new InvalidOperationException("hint queried")));
            IPyIterator iterator = Construct([new PyInstance(cls), new PyTuple([1L, 2L, 3L])], ["fillvalue"], ["F"]);
            Assert.Empty(indices); Assert.Equal<object?>([10L, 1L], Row(iterator).Items); Assert.Equal<object?>([11L, 2L], Row(iterator).Items);
            Assert.Equal<object?>(["F", 3L], Row(iterator).Items); Assert.False(iterator.MoveNext()); Assert.Equal<long>([0, 1, 2], indices);
            cls.SetAttr("__iter__", PyNone.Instance);
            Assert.Equal("'Sequence' object is not iterable", Assert.Throws<PyTypeError>(() => New(new PyInstance(cls))).PyMessage);
            Assert.Equal<long>([0, 1, 2], indices);
        }
    }

    [Fact]
    public void IterLookupFailureFallsBackButIterBodyErrorsAndBoundNoneDoNot()
    {
        var log = new List<string>(); var cls = new PyClass("Fallback"); var marker = new PyRuntimeError("getter marker");
        cls.SetAttr("__iter__", new PyProperty(Method("iter_getter", _ => { log.Add("getter"); throw marker; })));
        cls.SetAttr("__getitem__", Method("getitem", args => { log.Add("item:" + args[1]); if ((long)args[1]! == 1) throw new PyIndexError("end"); return 5L; }));
        IPyIterator iterator = New(new PyInstance(cls)); Assert.Equal<string>(["getter"], log);
        Assert.Equal<object?>([5L], Row(iterator).Items); Assert.False(iterator.MoveNext()); Assert.Equal<string>(["getter", "item:0", "item:1"], log);
        log.Clear(); cls.SetAttr("__iter__", Method("iter_body", _ => { log.Add("body"); throw marker; }));
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => New(new PyInstance(cls)))); Assert.Equal<string>(["body"], log);
        log.Clear(); cls.SetAttr("__iter__", new PyProperty(Method("iter_getter", _ => { log.Add("none"); return PyNone.Instance; })));
        Assert.Equal("'Fallback' object is not iterable", Assert.Throws<PyTypeError>(() => New(new PyInstance(cls))).PyMessage); Assert.Equal<string>(["none"], log);
    }

    [Fact]
    public void GetitemBindingIsOnlyDoneAtConstructionAfterAnIterLookupFailure()
    {
        int getters = 0; var cls = new PyClass("SequenceProperty");
        cls.SetAttr("__getitem__", new PyProperty(Method("getter", _ =>
        {
            getters++; return Method("item", args => { if ((long)args[0]! != 0) throw new PyIndexError("end"); return 7L; });
        })));
        IPyIterator ordinary = New(new PyInstance(cls)); Assert.Equal(0, getters); Assert.Equal<object?>([7L], Row(ordinary).Items); Assert.Equal(1, getters);
        cls.SetAttr("__iter__", new PyProperty(Method("bad_getter", _ => throw new PyValueError("lookup"))));
        IPyIterator fallback = New(new PyInstance(cls)); Assert.Equal(2, getters); Assert.Equal<object?>([7L], Row(fallback).Items); Assert.Equal(3, getters);
        cls.SetAttr("__getitem__", new PyProperty(Method("bad_item_getter", _ => throw new PyRuntimeError("item lookup"))));
        Assert.Equal("'SequenceProperty' object is not iterable", Assert.Throws<PyTypeError>(() => New(new PyInstance(cls))).PyMessage);
    }

    [Fact]
    public void PropertyIterAndNextBindNormallyWhileInstanceMagicShadowingIsIgnored()
    {
        int iteratorGetters = 0, nextGetters = 0, index = 0; var cls = new PyClass("PropertyIterator"); var value = new PyInstance(cls);
        cls.SetAttr("__iter__", new PyProperty(Method("get_iter", _ => { iteratorGetters++; return Method("iter", _ => value); })));
        cls.SetAttr("__next__", new PyProperty(Method("get_next", _ =>
        {
            nextGetters++; return Method("next", _ => index++ == 0 ? 3L : throw new StopChild());
        })));
        // 普通实例值不参与特殊槽查找。
        var ordinary = new User(new List<string>(), "ordinary", [4L]);
        PyObject.SetAttr(ordinary.Value, "__iter__", Method("bad", _ => throw new InvalidOperationException("instance iter")));
        PyObject.SetAttr(ordinary.Value, "__next__", Method("bad", _ => throw new InvalidOperationException("instance next")));
        IPyIterator iterator = New(value, ordinary.Value); Assert.Equal(1, iteratorGetters); Assert.Equal(0, nextGetters);
        Assert.Equal<object?>([3L, 4L], Row(iterator).Items); Assert.False(iterator.MoveNext()); Assert.Equal(2, nextGetters);
        var invalid = new PyClass("Invalid"); invalid.SetAttr("__iter__", Method("iter", _ => 1L));
        Assert.Equal("iter() returned non-iterator of type 'int'", Assert.Throws<PyTypeError>(() => New(new PyInstance(invalid))).PyMessage);
    }

    [Fact]
    public void OneLevelReentryKeepsIndependentOuterAndInnerResultArrays()
    {
        IPyIterator? iterator = null; PyTuple? nested = null; int position = 0;
        var cls = new PyClass("Reentrant"); var source = new PyInstance(cls);
        cls.SetAttr("__iter__", Method("iter", _ => source));
        cls.SetAttr("__next__", Method("next", _ =>
        {
            int step = ++position;
            if (step == 1) { nested = Row(iterator!); return "outer"; }
            if (step == 2) return "inner"; if (step == 3) return "after"; throw new PyStopIteration();
        }));
        iterator = New(source, new PyTuple([10L, 20L, 30L]));
        PyTuple outer = Row(iterator); Assert.Equal<object?>(["inner", 10L], nested!.Items); Assert.Equal<object?>(["outer", 20L], outer.Items);
        Assert.NotSame(nested, outer); Assert.NotSame(nested.Items, outer.Items);
        Assert.Equal<object?>(["after", 30L], Row(iterator).Items); Assert.False(iterator.MoveNext());
        Assert.Equal<object?>(["inner", 10L], nested.Items); Assert.Equal<object?>(["outer", 20L], outer.Items);
    }

    [Fact]
    public void ListMutationIsVisibleWhileActiveButCannotReviveARetiredSlot()
    {
        var left = new PyList([1L]); var right = new PyList([10L, 20L, 30L]);
        IPyIterator iterator = Construct([left, right], ["fillvalue"], ["F"]);
        Assert.Equal<object?>([1L, 10L], Row(iterator).Items); left.Append(2L);
        Assert.Equal<object?>([2L, 20L], Row(iterator).Items); Assert.Equal<object?>(["F", 30L], Row(iterator).Items);
        left.Append(3L); Assert.False(iterator.MoveNext()); Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void SwallowedStopAndLookupErrorsRestoreFramesAndAmbientBeforeFurtherCallbacks()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("zip_probe.py", "caller"); PyTraceback.SetLine(19);
            var stop = new User(new List<string>(), "stop", []); var stopMarker = new PyStopIteration("discarded");
            stop.Value.Class.SetAttr("__next__", Method("next", _ =>
            {
                PyTraceback.PushFrame("zip_probe.py", "stopping"); throw stopMarker;
            }));
            var marker = new PyRuntimeError("failure"); var failing = new User(new List<string>(), "failing", []);
            failing.Value.Class.SetAttr("__next__", Method("next", _ =>
            {
                Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
                PyTraceback.PushFrame("zip_probe.py", "failing"); throw marker;
            }));
            IPyIterator iterator = New(stop.Value, failing.Value);
            Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => iterator.MoveNext())); Assert.Same(ambient, marker.Context);
            Assert.Equal("failing", marker.Frames[0].Function); Assert.Equal(2, marker.Frames.Length);
            Assert.Same(ambient, stopMarker.Context); Assert.Equal("stopping", stopMarker.Frames[0].Function); Assert.Equal(2, stopMarker.Frames.Length);
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth()); Assert.False(iterator.MoveNext());
            var cls = new PyClass("LookupFallback"); var lookupMarker = new PyValueError("lookup");
            cls.SetAttr("__iter__", new PyProperty(Method("get_iter", _ =>
            {
                PyTraceback.PushFrame("zip_probe.py", "lookup"); PyErrors.PrepareRaise(lookupMarker);
                PyErrors.EnterHandler(lookupMarker); throw lookupMarker;
            })));
            cls.SetAttr("__getitem__", new PyProperty(Method("get_item", _ =>
            {
                Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
                return Method("item", _ => throw new PyIndexError("end"));
            })));
            Assert.False(New(new PyInstance(cls)).MoveNext()); Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
            Assert.Same(ambient, lookupMarker.Context); Assert.Equal("lookup", lookupMarker.Frames[0].Function); Assert.Equal(2, lookupMarker.Frames.Length);
            // __next__ 的 property getter 自身抛 Stop 同样只退休该槽，并恢复后续输入的 ambient。
            var propertyMarker = new StopChild();
            stop.Value.Class.SetAttr("__next__", new PyProperty(Method("get_next", _ =>
            {
                PyTraceback.PushFrame("zip_probe.py", "next_getter"); throw propertyMarker;
            })));
            IPyIterator propertyStop = New(stop.Value, new PyTuple([1L]));
            Assert.Equal<object?>([PyNone.Instance, 1L], Row(propertyStop).Items);
            Assert.False(propertyStop.MoveNext()); Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
            Assert.Same(ambient, propertyMarker.Context); Assert.Equal("next_getter", propertyMarker.Frames[0].Function); Assert.Equal(2, propertyMarker.Frames.Length);
            foreach (PyBaseException end in new PyBaseException[] { new PyIndexError("end"), new PyStopIteration("end") })
            {
                var sequence = new PyClass("SequenceEnd"); sequence.SetAttr("__getitem__", Method("getitem", _ =>
                {
                    PyTraceback.PushFrame("zip_probe.py", "indexing"); throw end;
                }));
                Assert.False(New(new PyInstance(sequence)).MoveNext()); Assert.Same(ambient, end.Context);
                Assert.Equal("indexing", end.Frames[0].Function); Assert.Equal(2, end.Frames.Length);
                Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
            }
            // zip 退休 map 槽，但 map 自身仍可继续；原 callback Stop 必须在被吞前保现场。
            var mapStop = new PyStopIteration("map pause"); int mapCalls = 0;
            PyFunction function = Method("map_callback", args =>
            {
                if (++mapCalls == 1) { PyTraceback.PushFrame("zip_probe.py", "map_callback"); throw mapStop; }
                return args[0];
            });
            PyMap map = Assert.IsType<PyMap>(PyMap.Create([function, new PyTuple([7L, 8L])], [], []));
            IPyIterator mapped = New(map, new PyTuple([10L, 20L]));
            Assert.Equal<object?>([PyNone.Instance, 10L], Row(mapped).Items);
            Assert.Same(ambient, mapStop.Context); Assert.Equal("map_callback", mapStop.Frames[0].Function); Assert.Equal(2, mapStop.Frames.Length);
            Assert.Equal<object?>([PyNone.Instance, 20L], Row(mapped).Items); Assert.False(mapped.MoveNext()); Assert.Equal(1, mapCalls);
            Assert.Equal(8L, map.Next()); Assert.Equal(2, mapCalls);
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void UnsupportedDescriptorIsNotSwallowedAsAnIterLookupFallback()
    {
        int calls = 0; var descriptorClass = new PyClass("Descriptor");
        descriptorClass.SetAttr("__get__", Method("get", _ => { calls++; throw new PyRuntimeError("getter"); }));
        var cls = new PyClass("Source"); cls.SetAttr("__iter__", new PyInstance(descriptorClass));
        cls.SetAttr("__getitem__", Method("item", _ => { calls++; return 1L; }));
        Assert.Throws<PyNotImplementedError>(() => New(new PyInstance(cls))); Assert.Equal(0, calls);
        cls.DeleteAttr("__iter__"); cls.SetAttr("__getitem__", new PyInstance(descriptorClass));
        IPyIterator iterator = New(new PyInstance(cls)); Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => iterator.MoveNext()); Assert.False(iterator.MoveNext()); Assert.Equal(0, calls);
    }

    private sealed class User
    {
        internal readonly PyInstance Value;
        internal readonly object?[] Steps;
        internal int Position;
        internal User(List<string> log, string name, object?[] steps)
        {
            Steps = steps; Value = new PyInstance(new PyClass(name));
            Value.Class.SetAttr("__iter__", Method("iter", _ => { log.Add("iter:" + name); return Value; }));
            Value.Class.SetAttr("__next__", Method("next", _ =>
            {
                log.Add("next:" + name + ":" + Position);
                if (Position >= Steps.Length) throw new PyStopIteration();
                object? value = Steps[Position++]; if (value is PyBaseException error) throw error; return value;
            }));
        }
    }

    private sealed class Counting(int count) : IPyIterator
    {
        private int index;
        public bool MoveNext() => ++index <= count;
        public object Current() => (long)index;
    }

    private sealed class StopChild : PyStopIteration { }
}
