// 精确 dict update 的流式前缀、mapping 优先、内对完整消费及回调异常状态回归。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictUpdateProtocolTests
{
    private static PyFunction Fn(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static PyInstance User(params (string Name, Func<object?[], object?> Body)[] slots)
    {
        var cls = (PyClass)PyOps.MakeClass("UpdateUser", [], null);
        foreach (var (name, body) in slots) cls.SetAttr(name, Fn(name, body));
        PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }
    private static PyTuple Pair(object? key, object? value) => new([key, value]);
    private static object? Call(string route, PyDict target, object?[] args, string[]? names = null, object?[]? values = null) => route switch
    {
        "direct" => PyOps.CallMethod(target, "update", args, names?.Cast<object?>().ToArray() ?? [], values ?? []),
        "prepared" => PyOps.CallValue(PyOps.PrepareMethod(target, "update"), args, names?.Cast<object?>().ToArray() ?? [], values ?? []),
        "saved" => PyOps.CallValue(PyObject.GetAttr(target, "update"), args, names?.Cast<object?>().ToArray() ?? [], values ?? []),
        "invoke" => Assert.IsType<PyBuiltinMethod>(PyObject.GetAttr(target, "update")).Invoke(args, names ?? [], values ?? []),
        _ => PyPic.CallMethod(3995, target, "update", args, names?.Cast<object?>().ToArray() ?? [], values ?? []),
    };

    [Fact]
    public void AllEntrypointsBindZeroOrOneBeforeConsumptionAndApplyKeywordsLast()
    {
        foreach (string route in new[] { "direct", "prepared", "saved", "invoke", "pic" })
        {
            var target = new PyDict(); int consumed = 0;
            var source = User(("__iter__", _ => { consumed++; throw new PyRuntimeError("must not iterate"); }));
            var error = Assert.Throws<PyTypeError>(() => Call(route, target, [source, source], ["z"], [3L]));
            Assert.Equal("update expected at most 1 argument, got 2", error.PyMessage);
            Assert.Equal(0, consumed); Assert.Equal(0, target.Count);
            Assert.Same(PyNone.Instance, Call(route, target, []));
            Call(route, target, [], ["other"], [7L]);
            Call(route, target, [new PyList([Pair("a", 1L), Pair("a", 2L)])], ["a", "z"], [4L, 5L]);
            Assert.Equal(new object?[] { "other", "a", "z" }, target.Keys.ToArray());
            Assert.Equal(4L, target.Get("a"));
            Call(route, target, [target]); Assert.Equal(3, target.Count);
        }
    }

    [Fact]
    public void OuterHasNoHintAndCommitsEachPairBeforeNext()
    {
        var target = new PyDict(); int next = 0, hint = 0;
        var failure = new PyValueError("later");
        var source = User(("__iter__", a => a[0]),
            ("__length_hint__", _ => { hint++; return 10L; }),
            ("__next__", _ =>
            {
                next++;
                if (next == 1) return Pair("prefix", 1L);
                Assert.Equal(1L, target.Get("prefix")); throw failure;
            }));
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Call("direct", target, [source], ["z"], [2L])));
        Assert.Equal(2, next); Assert.Equal(0, hint); Assert.False(target.Contains("z"));
    }

    [Fact]
    public void InnerPairIsFullyConsumedWithSecondIterAndHintBeforeLengthFailure()
    {
        var log = new List<string>(); int index = 0;
        var inner = User(("__iter__", a => { log.Add("iter"); return a[0]; }),
            ("__length_hint__", _ => { log.Add("hint"); return 4L; }),
            ("__next__", _ => { log.Add("next"); if (index == 4) throw new PyStopIteration(); return (long)++index; }));
        var target = new PyDict();
        var error = Assert.Throws<PyValueError>(() => target.Update(new PyList([Pair("prefix", 0L), inner, Pair("later", 1L)])));
        Assert.Equal("dictionary update sequence element #1 has length 4; 2 is required", error.PyMessage);
        Assert.Equal(new[] { "iter", "iter", "hint", "next", "next", "next", "next", "next" }, log);
        Assert.Equal(1, target.Count); Assert.Equal(0L, target.Get("prefix"));
    }

    [Fact]
    public void MappingMaterializesKeysThenReadsAndCommitsWithoutCallingSourceIter()
    {
        var log = new List<string>(); int index = 0;
        var keys = User(("__iter__", a => { log.Add("iter"); return a[0]; }),
            ("__length_hint__", _ => { log.Add("hint"); return 2L; }),
            ("__next__", _ => { log.Add("next"); if (index == 2) throw new PyStopIteration(); return ++index == 1 ? "a" : "b"; }));
        var target = new PyDict(); var failure = new PyValueError("read b");
        var mapping = User(("keys", _ => { log.Add("keys"); return keys; }),
            ("__iter__", _ => throw new PyAssertionError("must not iterate mapping")),
            ("__getitem__", a =>
            {
                log.Add("get:" + a[1]);
                if (Equals(a[1], "b")) { Assert.Equal(1L, target.Get("a")); throw failure; }
                return 1L;
            }));
        Assert.Same(failure, Assert.Throws<PyValueError>(() => target.Update(mapping)));
        Assert.Equal(new[] { "keys", "iter", "iter", "hint", "next", "next", "next", "get:a", "get:b" }, log);
        Assert.Equal(1, target.Count);
    }

    [Fact]
    public void ExactListKeysRemainLiveAndSequenceFallbackIsSupported()
    {
        var keys = new PyList(["a", "old"]);
        var mapping = User(("keys", _ => keys), ("__getitem__", a =>
        {
            if (Equals(a[1], "a")) keys.SetItem(1L, "new");
            return a[1];
        }));
        var target = new PyDict(); target.Update(mapping);
        Assert.Equal(new object?[] { "a", "new" }, target.Keys.ToArray());
        var inner = User(("__getitem__", a => (long)a[1]! switch { 0 => "sequence", 1 => 9L, _ => throw new PyIndexError("end") }));
        var outer = User(("__getitem__", a => (long)a[1]! == 0 ? inner : throw new PyIndexError("end")));
        target.Update(outer); Assert.Equal(9L, target.Get("sequence"));
    }

    [Fact]
    public void InnerTypeErrorIsReplacedButRuntimeExceptionIdentityAndChainSurvive()
    {
        var original = new PyTypeError("inner");
        var bad = User(("__iter__", _ => throw original));
        var target = new PyDict();
        var replaced = Assert.Throws<PyTypeError>(() => target.Update(new PyList([Pair("prefix", 0L), bad])));
        Assert.NotSame(original, replaced);
        Assert.Equal("cannot convert dictionary update sequence element #1 to a sequence", replaced.PyMessage);
        Assert.Null(replaced.Context); Assert.Null(replaced.Cause);
        var ambient = new PyValueError("ambient"); var cause = new PyTypeError("cause");
        var failure = new PyRuntimeError("callback") { Context = ambient };
        failure.WithCause(cause);
        long frame = PyTraceback.PushFrame("dict_update.py", "caller");
        object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            var ambientReplacement = Assert.Throws<PyTypeError>(() => target.Update(new PyList([bad])));
            Assert.Same(ambient, ambientReplacement.Context); Assert.Null(ambientReplacement.Cause);
            Assert.False(ambientReplacement.SuppressContextFlag);
            long depth = PyTraceback.CaptureDepth();
            var source = User(("__iter__", _ =>
            {
                PyTraceback.PushFrame("dict_update.py", "callback"); throw failure;
            }));
            Assert.Same(failure, Assert.Throws<PyRuntimeError>(() => target.Update(source)));
            Assert.Same(cause, failure.Cause); Assert.Same(ambient, failure.Context); Assert.True(failure.SuppressContextFlag);
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.NotEmpty(failure.Frames);
        }
        finally { PyTraceback.PopTo(frame); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void CustomKeyBoundaryDoesNotRehashNewProtocolKeysOrRemoveRawLegacySupport()
    {
        int hashes = 0;
        var key = User(("__hash__", _ => { hashes++; return 17L; }));
        var raw = new PyDict(); raw.Set(key, 3L); hashes = 0;
        var copied = new PyDict(); copied.Update(raw); Assert.Equal(3L, copied.Get(key)); Assert.True(hashes > 0);
        hashes = 0;
        var target = new PyDict();
        Assert.Throws<PyNotImplementedError>(() => target.Update(new PyList([Pair("prefix", 0L), Pair(key, 3L)])));
        Assert.Equal(0, hashes); Assert.Equal(1, target.Count);
        Assert.Throws<PyNotImplementedError>(() => Call("direct", raw, [], ["x"], [1L])); Assert.Equal(0, hashes);
        var injected = User(("__iter__", a => a[0]), ("__next__", _ =>
        {
            target.Set(key, 2L); return Pair("next", 3L);
        }));
        Assert.Throws<PyNotImplementedError>(() => target.Update(injected)); Assert.False(target.Contains("next"));
    }

    [Fact]
    public void KeysGetterAttributeErrorFallsBackButKeysCallErrorDoesNot()
    {
        int index = 0;
        var source = User(("__iter__", a => a[0]), ("__next__", _ =>
        {
            if (index++ == 0) return Pair("fallback", 1L); throw new PyStopIteration();
        }));
        source.Class.SetAttr("keys", new PyProperty(Fn("getter", _ => throw new PyAttributeError("missing"))));
        var target = new PyDict(); target.Update(source); Assert.Equal(1L, target.Get("fallback"));
        var failure = new PyAttributeError("inside call");
        source.Class.SetAttr("keys", Fn("keys", _ => throw failure));
        Assert.Same(failure, Assert.Throws<PyAttributeError>(() => target.Update(source)));
        Assert.Equal(2, index);
        var other = new PyRuntimeError("getter");
        source.Class.SetAttr("keys", new PyProperty(Fn("getter", _ => throw other)));
        Assert.Same(other, Assert.Throws<PyRuntimeError>(() => target.Update(source)));
        source.Class.SetAttr("keys", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => target.Update(source)); Assert.Equal(2, index);
    }

    [Fact]
    public void HintTypeErrorIsIgnoredAndNextTypeErrorIsReplaced()
    {
        int calls = 0;
        var nextError = new PyTypeError("next");
        var source = User(("__iter__", a => a[0]),
            ("__length_hint__", _ => throw new PyTypeError("hint")),
            ("__next__", _ => { calls++; throw nextError; }));
        var target = new PyDict();
        var error = Assert.Throws<PyTypeError>(() => target.Update(new PyList([source])));
        Assert.Equal(1, calls); Assert.NotSame(nextError, error);
        Assert.Equal("cannot convert dictionary update sequence element #0 to a sequence", error.PyMessage);
        Assert.Null(error.Context);
    }

    [Fact]
    public void MappingReadsValueBeforeRejectingAnUnhashableKey()
    {
        var log = new List<object?>();
        var invalid = new PyList(); var failure = new PyRuntimeError("getitem first");
        var source = User(("keys", _ => new PyList(["prefix", invalid])),
            ("__getitem__", a =>
            {
                log.Add(a[1]);
                if (ReferenceEquals(a[1], invalid)) throw failure;
                return 7L;
            }));
        var target = new PyDict();
        Assert.Same(failure, Assert.Throws<PyRuntimeError>(() => target.Update(source)));
        Assert.Equal(2, log.Count); Assert.Equal("prefix", log[0]); Assert.Same(invalid, log[1]);
        Assert.Equal(1, target.Count); Assert.Equal(7L, target.Get("prefix"));
        source.Class.SetAttr("__getitem__", Fn("__getitem__", a => { log.Add(a[1]); return 8L; }));
        Assert.Throws<PyTypeError>(() => target.Update(source));
        Assert.Same(invalid, log[^1]); Assert.Equal(1, target.Count); Assert.Equal(8L, target.Get("prefix"));
    }

    [Fact]
    public void ExplicitUserIteratorRetainsMappingPriorityBeforePairs()
    {
        var log = new List<string>();
        var source = User(("__iter__", a => { log.Add("iter"); return a[0]; }),
            ("__next__", _ => throw new PyAssertionError("must not read pairs")),
            ("keys", _ => { log.Add("keys"); return new PyList(["adapter"]); }),
            ("__getitem__", a => { log.Add("get:" + a[1]); return 9L; }));
        var target = new PyDict(); target.Update(PyIter.Create(source));
        var copy = Assert.IsType<PyDict>(PyOps.CallBuiltin("dict", [PyIter.Create(source)], [], []));
        Assert.Equal(9L, target.Get("adapter")); Assert.Equal(9L, copy.Get("adapter"));
        Assert.Equal(new[] { "iter", "keys", "get:adapter", "iter", "keys", "get:adapter" }, log);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactOuterListReadsLiveLengthAfterInnerCallbacks(bool shrink)
    {
        var log = new List<string>(); var outer = new PyList();
        var inner = User(("__iter__", _ =>
        {
            log.Add("inner.iter");
            if (shrink) PyOps.CallMethod(outer, "clear", [], [], []);
            else outer.Append(Pair("tail", 3L));
            return PyIter.Create(Pair("middle", 2L));
        }));
        outer.Append(Pair("prefix", 1L)); outer.Append(inner); outer.Append(Pair("old", 4L));
        var target = new PyDict(); target.Update(outer);
        Assert.Equal(shrink ? new object?[] { "prefix", "middle" }
            : new object?[] { "prefix", "middle", "old", "tail" }, target.Keys.ToArray());
        Assert.Equal(2L, target.Get("middle")); Assert.Equal(new[] { "inner.iter" }, log);
    }

    [Fact]
    public void SealedGeneratorCannotAcquireKeysAndStillUsesPairs()
    {
        int calls = 0;
        var generator = new PyGenerator(raw =>
        {
            calls++; var slots = (object?[])raw;
            if ((long)slots[0]! == 0) { slots[0] = 1L; return Pair("a", 1L); }
            slots[0] = -1L; return null;
        }, [0L, null, null, null], "dict_update.py", "pairs");
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(generator, "keys", Fn("keys", _ => new PyList(["wrong"]))));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(generator, "keys")); Assert.Equal(0, calls);
        var target = new PyDict(); target.Update(generator);
        Assert.Equal(1L, target.Get("a")); Assert.Equal(2, calls);
    }

    [Fact]
    public void ConstructorUsesTheSameStreamingAndBindingBody()
    {
        int calls = 0;
        var source = User(("__iter__", a => a[0]), ("__next__", _ =>
        {
            calls++; if (calls == 1) return Pair("a", 1L); throw new PyStopIteration();
        }));
        var result = Assert.IsType<PyDict>(PyOps.CallBuiltin("dict", [source], ["a", "b"], [2L, 3L]));
        Assert.Equal(2, calls); Assert.Equal(2L, result.Get("a")); Assert.Equal(3L, result.Get("b"));
        Assert.Equal("dict expected at most 1 argument, got 2", Assert.Throws<PyTypeError>(() =>
            PyOps.CallBuiltin("dict", [source, source], [], [])).PyMessage);
        Assert.Equal(2, calls);
    }
}
