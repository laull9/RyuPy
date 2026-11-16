// str前后缀的公开入口专项：窗口、tuple、整数协议与错误现场；不以宿主测试替代CP/JIT应用。
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextAffixTests
{
    private static PyFunction Method(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("affix_test", body);
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("AffixIndex");
        cls.SetAttr("__index__", Method(_ => body()));
        return new PyInstance(cls);
    }
    private static PyClass TextClass()
    {
        var cls = (PyClass)PyOps.MakeClass("AffixText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static object? Call(object text, string name, params object?[] args) => PyOps.CallMethod(text, name, args, [], []);
    private static object? Route(string route, object text, string name, object?[] args, string[]? names = null)
    {
        string[] keys = names ?? [];
        object?[] values = Array.ConvertAll(keys, _ => (object?)1L);
        if (route == "direct") return PyOps.CallMethod(text, name, args, keys, values);
        if (route == "root") return PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), name), [text, .. args], keys, values);
        object? target = route == "saved" ? PyObject.GetAttr(text, name) : PyOps.PrepareMethod(text, name);
        return PyOps.CallValue(target, args, keys, values);
    }

    [Fact]
    public void DirectSavedPreparedAndRootUseRawSubtypeText()
    {
        PyClass cls = TextClass(); int conversions = 0;
        cls.SetAttr("__str__", Method(_ => { conversions++; return "wrong"; }));
        object text = cls.Invoke(["[alpha]"], [], [])!, part = cls.Invoke(["alpha"], [], [])!;
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
            foreach (string method in new[] { "startswith", "endswith" })
            {
                Assert.Equal(true, Route(route, text, method, [part, 1L, 6L]));
                Assert.Equal(true, Route(route, "[alpha]", method, [part, 1L, 6L]));
                Assert.Equal(false, Route(route, text, method, [part, 2L, 6L]));
            }
        Assert.Equal(0, conversions);
    }

    [Fact]
    public void EmptyCandidatesKeepWideNegativeAndReversedWindowsDistinct()
    {
        BigInteger huge = BigInteger.One << 100;
        foreach (string method in new[] { "startswith", "endswith" })
        {
            Assert.Equal(true, Call("abc", method, "", 3L));
            Assert.Equal(false, Call("abc", method, "", 4L));
            Assert.Equal(false, Call("abc", method, "", 2L, 1L));
            Assert.Equal(true, Call("abc", method, "", 0L, -huge));
            Assert.Equal(false, Call("abc", method, "", huge));
            Assert.Equal(false, Call("abc", method, "", long.MaxValue));
            Assert.Equal(true, Call("abc", method, "abc", -huge, huge));
            Assert.Equal(true, Call("abc", method, "abc", long.MinValue, long.MaxValue));
            Assert.Equal(true, Call("abc", method, "abc", PyNone.Instance, PyNone.Instance));
            Assert.Equal(true, Call("abc", method, "a", false, true));
            Assert.Equal(true, Call("", method, "", 0L));
            Assert.Equal(false, Call("", method, "", 1L));
        }
        Assert.Equal(true, Call("abc", "startswith", "b", true));
    }

    [Fact]
    public void SignedWindowsUseCodePointsAndKeepLoneSurrogates()
    {
        string text = "a😀中\uD800x\uDC00z";
        foreach (string method in new[] { "startswith", "endswith" })
        {
            Assert.Equal(true, Call(text, method, "😀中", 1L, 3L));
            Assert.Equal(true, Call(text, method, "😀中", -6L, -4L));
            Assert.Equal(true, Call(text, method, "\uD800", 3L, 4L));
            Assert.Equal(true, Call(text, method, "\uDC00", -2L, -1L));
            Assert.Equal(true, Call(text, method, "z", -1L));
            Assert.Equal(true, Call(text, method, "", 7L));
            Assert.Equal(false, Call(text, method, "", 8L));
        }
    }

    [Fact]
    public void FullAndBoundedMatchesNeverAcceptHalfANormalScalar()
    {
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
            foreach (string method in new[] { "startswith", "endswith" })
                foreach (string half in new[] { "\uD83D", "\uDE00" })
                {
                    Assert.Equal(false, Route(route, "😀", method, [half]));
                    Assert.Equal(false, Route(route, "x😀y", method, [half, 1L, 2L]));
                    Assert.Equal(true, Route(route, "😀", method, [new PyTuple([half, "😀"])]));
                }
    }

    [Fact]
    public void TupleChecksAreLazyButBothBoundsPrecedeCandidateValidation()
    {
        var events = new List<string>();
        PyInstance start = Index(() => { events.Add("start"); return 0L; });
        PyInstance end = Index(() => { events.Add("end"); return 3L; });
        Assert.Equal(true, Call("abc", "startswith", new PyTuple(["a", 42L]), start, end));
        Assert.Equal(new[] { "start", "end" }, events); events.Clear();
        Assert.Equal(false, Call("abc", "endswith", new PyTuple([]), start, end));
        Assert.Equal(new[] { "start", "end" }, events); events.Clear();
        var error = Assert.Throws<PyTypeError>(() => Call("abc", "startswith", new PyTuple(["x", 42L]), start, end));
        Assert.Equal("tuple for startswith must only contain str, not int", error.PyMessage);
        Assert.Equal(new[] { "start", "end" }, events);
        Assert.Throws<PyTypeError>(() => Call("abc", "endswith", new PyTuple([42L, "c"]), 9L, -9L));
        Assert.Throws<PyTypeError>(() => Call("abc", "startswith", new PyList(["a"])));
    }

    [Fact]
    public void ExistingNamedTupleIsARealCandidateTuple()
    {
        object? factory = PyCollections.CreateAttributes()["namedtuple"];
        var cls = Assert.IsType<PyClass>(PyOps.CallValue(factory, ["Affixes", "first second"], ["module"], ["tests.affix"]));
        object tuple = cls.Invoke(["a", 42L], [], [])!;
        Assert.Equal(true, Call("abc", "startswith", tuple));
        Assert.Throws<PyTypeError>(() => Call("abc", "endswith", tuple));
    }

    [Fact]
    public void IndexUsesTypeSlotsOncePerBoundWithoutIntFallback()
    {
        int count = 0;
        PyInstance shared = Index(() => { count++; return 1L; });
        Assert.Equal(true, Call("abc", "startswith", "", shared, shared));
        Assert.Equal(2, count);
        PyObject.SetAttr(shared, "__index__", Method(_ => throw new PyValueError("instance slot must be ignored")));
        Assert.Equal(true, Call("abc", "startswith", "b", shared));
        var cls = new PyClass("IntOnly");
        cls.SetAttr("__int__", Method(_ => throw new PyValueError("int must not run")));
        var error = Assert.Throws<PyTypeError>(() => Call("abc", "startswith", "a", new PyInstance(cls)));
        Assert.Equal("slice indices must be integers or None or have an __index__ method", error.PyMessage);
        Assert.Throws<PyTypeError>(() => Call("abc", "startswith", "a", Index(() => shared)));
    }

    [Fact]
    public void StartCanChangeTheEndObjectsTypeSlotBeforeItIsLookedUp()
    {
        var events = new List<string>();
        PyInstance end = Index(() => throw new PyValueError("stale end slot"));
        PyInstance start = Index(() =>
        {
            events.Add("start");
            end.Class.SetAttr("__index__", Method(_ => { events.Add("new-end"); return 3L; }));
            return 0L;
        });
        Assert.Equal(true, Call("abc", "startswith", "a", start, end));
        Assert.Equal(new[] { "start", "new-end" }, events);
    }

    [Fact]
    public void SameSiteReentryCannotReplaceTheOriginalPrefixOrEnd()
    {
        const int site = 3861;
        var events = new List<string>();
        PyInstance start = Index(() =>
        {
            events.Add("outer-start");
            Assert.Equal(false, PyPic.CallMethod3(site, "abc", "startswith", "x", 0L, 1L));
            return 0L;
        });
        PyInstance end = Index(() => { events.Add("outer-end"); return 3L; });
        Assert.Equal(true, PyPic.CallMethod3(site, "abc", "startswith", "a", start, end));
        Assert.Equal(new[] { "outer-start", "outer-end" }, events);
    }

    [Fact]
    public void BindingErrorsKeepKnownRouteSpellingAndNeverRunIndexes()
    {
        int calls = 0; PyInstance index = Index(() => { calls++; return 0L; });
        foreach (string route in new[] { "direct", "saved", "root" })
        {
            string label = route == "saved" ? "" : "str.";
            var keyword = Assert.Throws<PyTypeError>(() => Route(route, "abc", "startswith", [], ["start"]));
            Assert.Equal(label + "startswith() takes no keyword arguments", keyword.PyMessage);
            keyword = Assert.Throws<PyTypeError>(() => Route(route, "abc", "endswith", ["c", index, index, 9L], ["extra"]));
            Assert.Equal(label + "endswith() takes no keyword arguments", keyword.PyMessage);
            Assert.Equal("startswith() takes at least 1 argument (0 given)",
                Assert.Throws<PyTypeError>(() => Route(route, "abc", "startswith", [])).PyMessage);
            Assert.Equal("endswith() takes at most 3 arguments (4 given)",
                Assert.Throws<PyTypeError>(() => Route(route, "abc", "endswith", ["c", index, index, 9L])).PyMessage);
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    public void RootReceiverErrorsPrecedeKeywordsAndBounds()
    {
        object? root = PyObject.GetAttr(PyType.Get("str"), "startswith");
        Assert.Equal("unbound method str.startswith() needs an argument",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], ["start"], [1L])).PyMessage);
        Assert.Equal("descriptor 'startswith' for 'str' objects doesn't apply to a 'int' object",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [7L, "a"], ["start"], [1L])).PyMessage);
    }

    [Fact]
    public void OwnedFailuresKeepCallerContextAndUserFailuresKeepOriginalObjects()
    {
        long depth = PyTraceback.PushFrame("affix_test.py", "caller");
        PyTraceback.SetLine(7);
        var user = new PyValueError("index body");
        var cause = new PyKeyError("cause"); user.WithCause(cause); PyErrors.PrepareRaise(user);
        var frames = user.Frames;
        Assert.Null(user.Context);
        var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(41);
            PyBaseException[] owned =
            [
                Assert.Throws<PyTypeError>(() => Call("abc", "startswith")),
                Assert.Throws<PyTypeError>(() => Call("abc", "startswith", 7L)),
                Assert.Throws<PyTypeError>(() => Call("abc", "startswith", "a", 1.5)),
                Assert.Throws<PyTypeError>(() => Call("abc", "startswith", "a", Index(() => "bad"))),
            ];
            foreach (PyBaseException error in owned)
            {
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("affix_test.py", "caller", 41), error.Frames[0]);
            }
            int endCalls = 0;
            Assert.Same(user, Assert.Throws<PyValueError>(() => Call("abc", "startswith", 7L,
                Index(() => throw user), Index(() => { endCalls++; return 3L; }))));
            Assert.Equal(0, endCalls); Assert.Same(frames, user.Frames); Assert.Null(user.Context);
            Assert.Same(user, Assert.Throws<PyValueError>(() => Call("abc", "endswith", "c", 0L, Index(() => throw user))));
            Assert.Same(frames, user.Frames); Assert.Null(user.Context);
            Assert.Same(cause, user.Cause); Assert.True(user.SuppressContextFlag);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void ProtocolBoolWarningDomainRemainsAnExplicitBoundary()
    {
        int endCalls = 0;
        Assert.Throws<PyNotImplementedError>(() => Call("abc", "startswith", "b",
            Index(() => true), Index(() => { endCalls++; return 3L; })));
        Assert.Equal(0, endCalls);
        Assert.Equal(true, Call("abc", "startswith", "b", true));
    }

    [Fact]
    public void BinaryAffixKeepsItsExistingAcceptedDomain()
    {
        var bytes = new PyBytes([97, 98, 99]);
        var part = new PyBytes([98]);
        Assert.Equal(true, Call(bytes, "startswith", part, 1L, 2L));
        Assert.Equal(true, Call(bytes, "endswith", new PyTuple([part, 42L]), 1L, 2L));
        Assert.Throws<PyTypeError>(() => Call(bytes, "startswith", "b", 1L, 2L));
    }
}
