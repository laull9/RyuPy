// 精确 list 必要基础协议：切片、index 缺项、extend 部分提交/提示、数值背板与永久耗尽。
// 独立硬编码断言对照已冻结 CP3.12.13 参考，不扩到 list 派生布局或完整任意描述符。
using System.Numerics;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyListPrimitivesTests
{
    private static PyFunction Fn(string name, PyCall body) => new(
        name, PySignature.Create(name, ["self"], ["pos"], ["0"]), body, []);
    private static PyFunction Nullary(Func<object?> body) => new(
        "body", PySignature.Create("body", [], [], []), (_, _, _, _) => body(), []);
    private static PyInstance User(Func<object?> next, Func<PyInstance, object?>? iter = null)
    {
        var cls = (PyClass)PyOps.MakeClass("Cursor", [], null);
        cls.SetAttr("__iter__", Fn("__iter__", (_, args, _, _) => iter?.Invoke((PyInstance)args[0]!) ?? args[0]));
        cls.SetAttr("__next__", Fn("__next__", (_, _, _, _) => next()));
        return (PyInstance)cls.Invoke([], [], [])!;
    }
    private static PyInstance Values(params object?[] values)
    {
        int index = 0;
        return User(() => index < values.Length ? values[index++] : throw new PyStopIteration());
    }
    private static void Apply(PyList list, object? source, bool inplace)
    {
        if (inplace) Assert.Same(list, PyOps.InPlaceBinary(PyOps.Add, list, source!));
        else Assert.Same(PyNone.Instance, PyOps.CallMethod(list, "extend", [source], [], []));
    }
    private static object? NumericStorage(PyList list, string field) =>
        typeof(PyList).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list);

    [Fact]
    public void EmptyForwardSlicesInsertOrDeleteNothing()
    {
        var list = new PyList([0L, 1L, 2L, 3L]);
        list.DelSlice(new PySlice(3L, 1L, 1L));
        Assert.Equal(new object?[] { 0L, 1L, 2L, 3L }, list.Items);
        list.SetSlice(new PySlice(3L, 1L, 1L), new PyTuple([8L, 9L]));
        Assert.Equal(new object?[] { 0L, 1L, 2L, 8L, 9L, 3L }, list.Items);
        list.SetSlice(new PySlice(99L, -99L, 1L), new PyTuple([10L]));
        Assert.Equal(10L, list.GetItem(-1));
        list.DelSlice(new PySlice(99L, -99L, 1L));
        Assert.Equal(7, list.Count);
    }

    [Fact]
    public void NegativeStepDeletionUsesDescendingOriginalIndices()
    {
        foreach (var item in new[]
        {
            (Slice: new PySlice(6L, 0L, -2L), Expected: new object?[] { 0L, 1L, 3L, 5L }),
            (Slice: new PySlice(null, null, -1L), Expected: Array.Empty<object?>()),
            (Slice: new PySlice(0L, 5L, -1L), Expected: new object?[] { 0L, 1L, 2L, 3L, 4L, 5L, 6L }),
            (Slice: new PySlice(0L, 7L, 2L), Expected: new object?[] { 1L, 3L, 5L }),
        })
        {
            var list = new PyList([0L, 1L, 2L, 3L, 4L, 5L, 6L]);
            list.DelSlice(item.Slice);
            Assert.Equal(item.Expected, list.Items);
        }
    }

    [Fact]
    public void MissingPublicIndexRaisesButInternalSearchKeepsSentinel()
    {
        var list = new PyList([1L, 2L]);
        Assert.Equal(-1, list.IndexOf("missing"));
        Assert.False(list.Contains("missing"));
        Assert.Equal("'missing' is not in list", Assert.Throws<PyValueError>(() => PyOps.CallMethod(list, "index", ["missing"], [], [])).PyMessage);
        Assert.Equal(1L, PyOps.CallMethod(list, "index", [2L], [], []));
        Assert.Equal("list.remove(x): x not in list", Assert.Throws<PyValueError>(() => list.Remove("missing")).PyMessage);
        object nan = double.NaN;
        list.Append(nan);
        Assert.Equal(2L, PyOps.CallMethod(list, "index", [nan], [], []));
    }

    [Fact]
    public void ExactSelfAndTupleSourcesKeepIdentityAndReturnContracts()
    {
        foreach (bool inplace in new[] { false, true })
        {
            object marker = new PyList();
            var list = new PyList([marker, 1L]);
            var backing = list.Items;
            Apply(list, list, inplace);
            Assert.Same(backing, list.Items);
            Assert.Equal(4, list.Count);
            Assert.Same(marker, list.GetItem(0)); Assert.Same(marker, list.GetItem(2));
            Apply(list, new PyTuple([marker]), inplace);
            Assert.Same(marker, list.GetItem(-1));
        }
    }

    [Fact]
    public void NumericSourcesTargetsAndSelfKeepBackingAndSharedBoxes()
    {
        var source = Assert.IsType<PyList>(PyNumericListOps.Create());
        PyNumericListOps.AppendF64(source, 1234.5); PyNumericListOps.AppendF64(source, double.NaN);
        var sourceBacking = NumericStorage(source, "_numeric");
        var target = Assert.IsType<PyList>(PyNumericListOps.Create());
        var targetBacking = NumericStorage(target, "_numeric");
        target.Extend(source);
        Assert.Same(sourceBacking, NumericStorage(source, "_numeric"));
        Assert.Same(targetBacking, NumericStorage(target, "_numeric"));
        Assert.Null(NumericStorage(source, "_items")); Assert.Null(NumericStorage(target, "_items"));
        Assert.Same(source.GetItem(0), target.GetItem(0)); Assert.Same(source.GetItem(1), target.GetItem(1));
        target.Extend(target);
        Assert.Equal(4, target.Count); Assert.Same(target.GetItem(0), target.GetItem(2));
        target.Extend(new PyTuple([source.GetItem(0)]));
        target.Extend(Values(source.GetItem(1)));
        Assert.Null(NumericStorage(target, "_items")); Assert.Same(targetBacking, NumericStorage(target, "_numeric"));
        Assert.Same(source.GetItem(0), target.GetItem(4)); Assert.Same(source.GetItem(1), target.GetItem(5));
    }

    [Fact]
    public void GeneralSourceUsesIterThenSourceLengthAndStreamsPrefix()
    {
        foreach (bool inplace in new[] { false, true })
        {
            var target = new PyList([10L]); var events = new List<string>(); int calls = 0;
            var failure = new PyValueError("next failed");
            var cursor = User(() =>
            {
                events.Add($"next:{target.Count}");
                return calls++ == 0 ? 20L : throw failure;
            }, _ => throw new PyAssertionError("cursor iter must not run"));
            cursor.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => throw new PyAssertionError("cursor hint")));
            var source = User(() => throw new PyAssertionError("source next"), _ => { events.Add("iter"); target.Append(11L); return cursor; });
            source.Class.SetAttr("__len__", Fn("len", (_, _, _, _) => { events.Add("len"); target.Append(12L); return 2L; }));
            source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => throw new PyAssertionError("len wins")));
            Assert.Same(failure, Assert.Throws<PyValueError>(() => Apply(target, source, inplace)));
            Assert.Equal(new object?[] { 10L, 11L, 12L, 20L }, target.Items);
            Assert.Equal(new[] { "iter", "len", "next:3", "next:4" }, events);
        }
    }

    [Fact]
    public void HintFailureKeepsCallbackMutationButConsumesNoNext()
    {
        foreach (bool inplace in new[] { false, true })
        foreach (string slot in new[] { "__len__", "__length_hint__" })
        {
            var target = new PyList([1L]); var failure = new PyValueError("hint");
            var source = User(() => throw new PyAssertionError("must not consume"));
            source.Class.SetAttr(slot, Fn(slot, (_, _, _, _) => { target.Clear(); target.Append(3L); throw failure; }));
            Assert.Same(failure, Assert.Throws<PyValueError>(() => Apply(target, source, inplace)));
            Assert.Equal(new object?[] { 3L }, target.Items);
        }
    }

    [Fact]
    public void LenTypeErrorFallsBackToHintWithFreshAmbientAndFrames()
    {
        long frame = PyTraceback.PushFrame("list.py", "caller");
        var ambient = new PyKeyError("outer"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            long depth = PyTraceback.CaptureDepth(); int calls = 0;
            var source = User(() =>
            {
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
                if (calls++ == 0) return 4L;
                PyTraceback.PushFrame("list.py", "stopped");
                PyErrors.EnterHandler(new PyValueError("inner stop"));
                throw new PyStopIteration();
            });
            source.Class.SetAttr("__len__", Fn("len", (_, _, _, _) =>
            {
                PyTraceback.PushFrame("list.py", "ignored_len");
                PyErrors.EnterHandler(new PyValueError("inner len")); throw new PyTypeError("ignored");
            }));
            source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) =>
            {
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
                PyTraceback.PushFrame("list.py", "ignored_hint");
                PyErrors.EnterHandler(new PyValueError("inner hint")); throw new PyTypeError("ignored");
            }));
            var target = new PyList(); target.Extend(source);
            Assert.Equal(new object?[] { 4L }, target.Items);
            Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
            var missing = Assert.Throws<PyValueError>(() => PyOps.CallMethod(target, "index", [99L], [], []));
            Assert.Same(ambient, missing.Context);
            Assert.DoesNotContain(missing.Frames, f => f.Function is "ignored_len" or "ignored_hint" or "stopped");
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }

    [Fact]
    public void CallbackFailurePreservesExceptionIdentityChainAndPreparedFrames()
    {
        var failure = new PyValueError("callback"); var cause = new PyKeyError("cause");
        var context = new PyRuntimeError("original context");
        PyErrors.WithCause(failure, cause); failure.Context = context;
        long saved = PyTraceback.PushFrame("origin.py", "prepared");
        PyTraceback.Attach(failure); PyTraceback.PopTo(saved);
        var frames = failure.Frames;
        long caller = PyTraceback.PushFrame("list.py", "caller");
        var ambient = new PyTypeError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            foreach (string stage in new[] { "iter", "len", "hint", "next" })
            {
                var source = User(() => stage == "next" ? throw failure : throw new PyStopIteration(), self => stage == "iter" ? throw failure : self);
                if (stage is "len" or "hint") source.Class.SetAttr(stage == "len" ? "__len__" : "__length_hint__", Fn(stage, (_, _, _, _) => throw failure));
                Assert.Same(failure, Assert.Throws<PyValueError>(() => new PyList().Extend(source)));
                Assert.Same(cause, failure.Cause); Assert.Same(context, failure.Context); Assert.Same(frames, failure.Frames);
                Assert.Same(ambient, PyErrors.Current);
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(caller); }
    }

    [Fact]
    public void HintPropertiesSeparateLookupCallAndResultTypeErrors()
    {
        foreach (string slot in new[] { "__len__", "__length_hint__" })
        foreach (string mode in new[] { "lookup", "call", "result", "valid" })
        {
            var source = Values(1L, 2L); var target = new PyList(); var error = new PyTypeError(mode);
            source.Class.SetAttr(slot, new PyProperty(Fn("getter", (_, _, _, _) =>
            {
                if (mode == "lookup") throw error;
                return Nullary(() => mode == "call" ? throw error : mode == "result" ? "bad" : 0L);
            })));
            if (slot == "__length_hint__" && mode is "lookup" or "result")
            {
                var result = Assert.Throws<PyTypeError>(() => target.Extend(source));
                if (mode == "lookup") Assert.Same(error, result);
                else Assert.Equal("__length_hint__ must be an integer, not str", result.PyMessage);
                Assert.Empty(target.Items);
            }
            else { target.Extend(source); Assert.Equal(new object?[] { 1L, 2L }, target.Items); }
        }
    }

    [Fact]
    public void HintChecksIntegerDomainAndDoesNotLimitActualElementCount()
    {
        foreach (object hint in new object[] { true, false, 0, 1L, new BigInteger(2), 4097L, PyNotImplemented.Instance })
        {
            var source = Values(1L, 2L); source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => hint));
            var target = new PyList(); target.Extend(source); Assert.Equal(2, target.Count);
        }
        foreach (string slot in new[] { "__len__", "__length_hint__" })
        foreach (int sign in new[] { -1, 1 })
        {
            var source = Values(1L); source.Class.SetAttr(slot, Fn("size", (_, _, _, _) => sign * (BigInteger.One << 100)));
            if (slot == "__len__" && sign < 0) Assert.Throws<PyValueError>(() => new PyList().Extend(source));
            else Assert.Equal(slot == "__len__" ? "cannot fit 'int' into an index-sized integer" : "Python int too large to convert to C ssize_t",
                Assert.Throws<PyOverflowError>(() => new PyList().Extend(source)).PyMessage);
        }
        foreach (string slot in new[] { "__len__", "__length_hint__" })
        {
            var source = Values(1L); source.Class.SetAttr(slot, Fn("size", (_, _, _, _) => -1L));
            Assert.Equal($"{slot}() should return >= 0", Assert.Throws<PyValueError>(() => new PyList().Extend(source)).PyMessage);
        }
        int index = 0; var many = User(() => index < 4100 ? (long)index++ : throw new PyStopIteration());
        many.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => 0L));
        var targetMany = new PyList(); targetMany.Extend(many);
        Assert.Equal(4100, targetMany.Count); Assert.Equal(4099L, targetMany.GetItem(-1));
    }

    [Fact]
    public void LenAllowsIndexButHintNeverCallsIt()
    {
        int indexes = 0;
        var cls = (PyClass)PyOps.MakeClass("Index", [], null);
        cls.SetAttr("__index__", Fn("index", (_, _, _, _) => { indexes++; return 0L; }));
        object value = cls.Invoke([], [], [])!;
        var source = Values(1L); source.Class.SetAttr("__len__", Fn("len", (_, _, _, _) => value));
        new PyList().Extend(source); Assert.Equal(1, indexes);
        var hinted = Values(1L); hinted.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => value));
        Assert.Equal("__length_hint__ must be an integer, not Index", Assert.Throws<PyTypeError>(() => new PyList().Extend(hinted)).PyMessage);
        Assert.Equal(1, indexes);
    }

    [Fact]
    public void ExplicitUserIteratorReiteratesAndCanResumeAfterTemporaryStop()
    {
        int calls = 0, iters = 0, hints = 0;
        var source = User(() => ++calls == 2 || calls > 3 ? throw new PyStopIteration() : (long)calls, self => { iters++; return self; });
        source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => { hints++; return 0L; }));
        var iterator = PyIter.Create(source); var target = new PyList();
        target.Extend(iterator); target.Extend(iterator);
        Assert.Equal(new object?[] { 1L, 3L }, target.Items);
        Assert.Equal(3, iters); Assert.Equal(2, hints); Assert.Equal(4, calls);
    }

    [Fact]
    public void GetItemFallbackUsesSourceHintAndDoesNotChangeGlobalIter()
    {
        var events = new List<string>(); var cls = (PyClass)PyOps.MakeClass("Sequence", [], null);
        cls.SetAttr("__getitem__", new PyFunction("get", PySignature.Create("get", ["self", "index"], ["pos", "pos"], ["0", "0"]),
            (_, args, _, _) => { long index = (long)args[1]!; events.Add($"get:{index}"); return index < 2 ? index : throw new PyIndexError("end"); }, []));
        cls.SetAttr("__len__", Fn("len", (_, _, _, _) => { events.Add("len"); throw new PyTypeError("fallback"); }));
        cls.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => { events.Add("hint"); return 0L; }));
        object source = cls.Invoke([], [], [])!;
        Assert.Throws<PyTypeError>(() => PyIter.Create(source));
        var target = new PyList(); target.Extend(source);
        Assert.Equal(new object?[] { 0L, 1L }, target.Items);
        Assert.Equal(new[] { "len", "hint", "get:0", "get:1", "get:2" }, events);
        cls.SetAttr("__iter__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => target.Extend(source));
        Assert.Equal(2, target.Count);
    }

    [Fact]
    public void IterationStaysLiveUntilFirstExhaustionThenNeverResumes()
    {
        var list = new PyList([0L, 1L]); var iterator = PyIter.Create(list);
        Assert.True(iterator.MoveNext()); Assert.Equal(0L, iterator.Current());
        list.Append(2L); list.DelItem(0L);
        Assert.True(iterator.MoveNext()); Assert.Equal(2L, iterator.Current());
        Assert.False(iterator.MoveNext()); list.Append(3L); Assert.False(iterator.MoveNext());
        var empty = new PyList(); var emptyIterator = PyIter.Create(empty);
        Assert.False(emptyIterator.MoveNext()); empty.Append(1L); Assert.False(emptyIterator.MoveNext());
        var shrunk = new PyList([0L, 1L]); var shrunkIterator = PyIter.Create(shrunk);
        Assert.True(shrunkIterator.MoveNext()); shrunk.Clear(); Assert.False(shrunkIterator.MoveNext());
        shrunk.Extend(new PyTuple([8L, 9L])); Assert.False(shrunkIterator.MoveNext());
    }

    [Fact]
    public void NumericIterationKeepsPriorMaterializationAndBoxIdentity()
    {
        var list = Assert.IsType<PyList>(PyNumericListOps.Create()); PyNumericListOps.AppendF64(list, 1234.5);
        var iterator = PyIter.Create(list);
        Assert.True(iterator.MoveNext()); Assert.Same(list.GetItem(0), iterator.Current());
        Assert.NotNull(NumericStorage(list, "_items")); Assert.Null(NumericStorage(list, "_numeric"));
        PyNumericListOps.AppendF64(list, 5678.5); Assert.True(iterator.MoveNext());
        Assert.Equal(5678.5, iterator.Current()); Assert.False(iterator.MoveNext());
        PyNumericListOps.AppendF64(list, 9012.5); Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void RangeHintDoesNotOverflowIntermediateArithmeticOrConsumeHugeSources()
    {
        var target = new PyList();
        target.Extend(new PyRange(-9223372036854775807L, -11, 2305843009213693951L));
        Assert.Equal(new object?[] { -9223372036854775807L, -6917529027641081856L, -4611686018427387905L, -2305843009213693954L }, target.Items);
        foreach (var source in new[] { new PyRange(0, -1, long.MinValue), new PyRange(0, -2, long.MinValue), new PyRange(0, 2, long.MaxValue) })
        {
            var one = new PyList(); one.Extend(source);
            Assert.Equal(new object?[] { 0L }, one.Items);
        }
        var before = new PyList(["before"]);
        Assert.Equal("Python int too large to convert to C ssize_t", Assert.Throws<PyOverflowError>(() => before.Extend(new PyRange(long.MinValue, long.MaxValue, 1))).PyMessage);
        Assert.Equal(new object?[] { "before" }, before.Items);
        before.Extend(new PyRange(4, 0, 1)); Assert.Equal(1, before.Count);
    }

    private sealed class NativeSource(Func<string, object?> get) : PyNativeObject
    {
        public override string TypeName => "native_source";
        public override object? GetAttr(string name) => get(name);
    }

    [Fact]
    public void NativeSourceUsesExplicitIterLengthHintAndPreservesMissingBoundary()
    {
        var events = new List<string>(); var cursor = Values(4L, 5L);
        var source = new NativeSource(name =>
        {
            events.Add($"lookup:{name}");
            return name switch
            {
                "__iter__" => Nullary(() => { events.Add("iter"); return cursor; }),
                "__len__" => Nullary(() => { events.Add("len"); throw new PyTypeError("fallback"); }),
                "__length_hint__" => Nullary(() => { events.Add("hint"); return 0L; }),
                _ => throw new PyAttributeError(name)
            };
        });
        var target = new PyList(); target.Extend(source);
        Assert.Equal(new object?[] { 4L, 5L }, target.Items);
        Assert.Equal(new[] { "lookup:__iter__", "iter", "lookup:__len__", "len", "lookup:__length_hint__", "hint" }, events);
        var failure = new PyTypeError("hint lookup");
        var invalid = new NativeSource(name => name == "__iter__" ? Nullary(() => Values())
            : name == "__length_hint__" ? throw failure : throw new PyAttributeError(name));
        Assert.Same(failure, Assert.Throws<PyTypeError>(() => target.Extend(invalid)));
    }

    [Fact]
    public void NativeStreamKeepsItsIterationAndClosedStateGuard()
    {
        var stream = Assert.IsAssignableFrom<PyIOStream>(PyIO.CreateStringIO(["first\nsecond\n"], [], []));
        var target = new PyList(); target.Extend(stream);
        Assert.Equal(new object?[] { "first\n", "second\n" }, target.Items);
        PyOps.CallValue(stream.GetAttr("close"), [], [], []);
        Assert.Equal("I/O operation on closed file.", Assert.Throws<PyValueError>(() => target.Extend(stream)).PyMessage);
        Assert.Equal(2, target.Count);
    }

}
