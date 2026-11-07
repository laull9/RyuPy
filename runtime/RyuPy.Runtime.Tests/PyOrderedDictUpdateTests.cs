// OD 更新专属行为：外层逐项提交、inner N+1与mapping交错消费；callback异常不换写。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyOrderedDictUpdateTests
{
    private static object New() => PyOps.CallValue(PyCollections.CreateAttributes()["OrderedDict"], [], [], [])!;
    private static object? Update(object target, object? source, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyObject.GetAttr(target, "update"), [source], names ?? [], values ?? []);
    private static PyTuple Pair(object? key, object? value) => new([key, value]);
    private static PyInstance User(params (string Name, Func<object?[], object?> Body)[] methods)
    {
        var cls = (PyClass)PyOps.MakeClass("OrderedSource", [], null);
        foreach (var (name, body) in methods) cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, body));
        PyOps.FinalizeOrdinaryClass(cls); return Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }
    [Fact]
    public void MappingNextGetCommitAreInterleavedWithoutHints()
    {
        object target = New(); int index = 0; var log = new List<string>();
        object keys = User(("__iter__", a => { log.Add("iter"); return a[0]; }),
            ("__length_hint__", _ => throw new PyAssertionError("no hint")), ("__next__", _ =>
            {
                log.Add("next"); if (index++ == 0) return "a";
                Assert.Equal(1L, PyOps.GetItem(target, "a")); if (index == 2) return "b"; throw new PyStopIteration();
            }));
        object mapping = User(("keys", _ => { log.Add("keys"); return keys; }),
            ("__getitem__", a => { log.Add("get:" + a[1]); return 1L; }),
            ("__iter__", _ => throw new PyAssertionError("no source iter")));
        Update(target, mapping, ["z"], [9L]);
        Assert.Equal(new[] { "keys", "iter", "next", "get:a", "next", "get:b", "next" }, log);
        Assert.Equal(9L, PyOps.GetItem(target, "z"));
    }
    [Fact]
    public void InnerPairReadsOnlyThirdAndPreservesPrefix()
    {
        object target = New(); int next = 0;
        object pair = User(("__iter__", a => a[0]), ("__length_hint__", _ => throw new PyAssertionError("no hint")),
            ("__next__", _ => ++next));
        var error = Assert.Throws<PyValueError>(() => Update(target, new PyList([Pair("prefix", 1L), pair, Pair("late", 2L)]), ["kw"], [3L]));
        Assert.Equal("too many values to unpack (expected 2)", error.PyMessage); Assert.Equal(3, next);
        Assert.Equal(1L, PyOps.Len(target)); Assert.Equal(1L, PyOps.GetItem(target, "prefix"));
    }
    [Fact]
    public void ShortPairAndUserTypeErrorAreNotDictErrors()
    {
        object target = New();
        Assert.Equal("need more than 0 values to unpack", Assert.Throws<PyValueError>(() => Update(target, new PyList([new PyTuple([])]))).PyMessage);
        Assert.Equal("need more than 1 value to unpack", Assert.Throws<PyValueError>(() => Update(target, new PyList([new PyTuple(["a"])]))).PyMessage);
        var failure = new PyTypeError("inner failure"); object pair = User(("__iter__", _ => throw failure));
        Assert.Same(failure, Assert.Throws<PyTypeError>(() => Update(target, new PyList([Pair("first", 1L), pair]))));
        Assert.Equal(1L, PyOps.GetItem(target, "first"));
    }
    [Fact]
    public void CallbacksMayReenterTargetAndKeysListIsLive()
    {
        object target = New(); var keys = new PyList(["a", "b"]);
        object mapping = User(("keys", _ => keys), ("__getitem__", a =>
        {
            if (Equals(a[1], "a")) { keys.Append("c"); PyOps.SetItem(target, "callback", 8L); }
            return a[1];
        }));
        Update(target, mapping); Assert.Equal(4L, PyOps.Len(target)); Assert.Equal("c", PyOps.GetItem(target, "c"));
        Update(target, target); Assert.Equal(4L, PyOps.Len(target));
    }
    [Fact]
    public void KeysCallAttributeErrorDoesNotFallbackAndFailureIdentitySurvives()
    {
        object target = New(); var failure = new PyAttributeError("keys call");
        object mapping = User(("keys", _ => throw failure), ("__iter__", _ => throw new PyAssertionError("no fallback")));
        Assert.Same(failure, Assert.Throws<PyAttributeError>(() => Update(target, mapping)));
        int next = 0; var runtime = new PyRuntimeError("source failed"); var cause = new PyTypeError("cause"); runtime.WithCause(cause);
        object source = User(("__iter__", a => a[0]), ("__next__", _ => ++next == 1 ? Pair("ok", 1L) : throw runtime));
        Assert.Same(runtime, Assert.Throws<PyRuntimeError>(() => Update(target, source, ["kw"], [2L])));
        Assert.Same(cause, runtime.Cause); Assert.Equal(1L, PyOps.Len(target));
    }
}
