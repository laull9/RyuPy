// 五搜索公开调用合同；精确预期、真实PIC重入和原容器保护，不代替产品CP/JIT/AOT验收。
using System.Numerics;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextSearchTests
{
    private static readonly string[] Methods = ["count", "find", "rfind", "index", "rindex"];
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("search_test", body);
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("SearchIndex"); cls.SetAttr("__index__", Fn(_ => body())); return new PyInstance(cls);
    }
    private static object? Call(object text, string name, params object?[] args) => PyOps.CallMethod(text, name, args, [], []);
    private static object? Route(string route, object text, string name, object?[] args, string[]? names = null)
    {
        string[] keys = names ?? []; object?[] values = Array.ConvertAll(keys, _ => (object?)1L);
        if (route == "direct") return PyOps.CallMethod(text, name, args, keys, values);
        if (route == "root") return PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), name), [text, .. args], keys, values);
        object? target = route == "saved" ? PyObject.GetAttr(text, name) : PyOps.PrepareMethod(text, name);
        return PyOps.CallValue(target, args, keys, values);
    }
    private static void Window(string text, object?[] args, params long?[] expected)
    {
        Assert.Equal(Methods.Length, expected.Length);
        for (int i = 0; i < Methods.Length; i++)
            if (expected[i] is long value) Assert.Equal(value, Call(text, Methods[i], args));
            else Assert.Equal("substring not found", Assert.Throws<PyValueError>(() => Call(text, Methods[i], args)).PyMessage);
    }

    [Fact]
    public void CodePointWindowsKeepEmptyPastTailReversedNegativeAndWideDistinct()
    {
        BigInteger huge = BigInteger.One << 100;
        Window("a😀a😀z", ["😀", 1L, 4L], 2, 1, 3, 1, 3);
        Window("a😀a😀z", ["a", -3L, -1L], 1, 2, 2, 2, 2);
        Window("aaa", ["aa"], 1, 0, 1, 0, 1);
        Window("abc", ["", 3L], 1, 3, 3, 3, 3);
        Window("abc", ["", 4L], 0, -1, -1, null, null);
        Window("abc", ["", 2L, 1L], 0, -1, -1, null, null);
        Window("abc", ["", -huge, huge], 4, 0, 3, 0, 3);
        Window("abc", ["a", huge], 0, -1, -1, null, null);
        Window("abc", ["a", long.MinValue, long.MaxValue], 1, 0, 0, 0, 0);
        Window("abc", ["", 0L, -huge], 1, 0, 0, 0, 0);
        Window("abc", ["b", true, 2L], 1, 1, 1, 1, 1);
        Window("", ["", PyNone.Instance, PyNone.Instance], 1, 0, 0, 0, 0);
        Window("", ["x"], 0, -1, -1, null, null);
    }

    [Fact]
    public void BothMatchEndsRejectHalfPairsButKeepLoneSurrogatesAndLaterCandidates()
    {
        foreach (string half in new[] { "\uD83D", "\uDE00" })
        {
            Window("😀", [half], 0, -1, -1, null, null);
            Window("x😀y", [half, 1L, 2L], 0, -1, -1, null, null);
        }
        Window("\uD800x\uDC00", ["\uD800"], 1, 0, 0, 0, 0);
        Window("\uD800x\uDC00", ["\uDC00"], 1, 2, 2, 2, 2);
        Window("😀\uD83D", ["\uD83D"], 1, 1, 1, 1, 1);
        Window("\uDE00😀", ["\uDE00"], 1, 0, 0, 0, 0);
        // 长needle也不能以半pair为端点，拒绝后保留前后的完整候选。
        Window("😀x\uD83Dx", ["\uD83Dx"], 1, 2, 2, 2, 2);
        Window("x\uDE00x😀", ["x\uDE00"], 1, 0, 0, 0, 0);
    }

    [Fact]
    public void ExactAndInheritedRoutesUseRawTextAndRootDoesNotSwallowOverrides()
    {
        var cls = (PyClass)PyOps.MakeClass("SearchText", [PyType.Get("str")], null); PyOps.FinalizeOrdinaryClass(cls);
        cls.SetAttr("__str__", Fn(_ => throw new PyValueError("unexpected str")));
        // 旧槽拒绝必须发生在写入/版本变化之前；不绕过守卫构造伪支持类。
        long version = cls.Version; int attributes = cls.Attributes.Count;
        Assert.Equal("str subclass slot __len__ is not supported",
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr("__len__", Fn(_ => throw new PyValueError("unexpected len")))).PyMessage);
        Assert.Equal(version, cls.Version); Assert.Equal(attributes, cls.Attributes.Count);
        Assert.False(cls.Attributes.ContainsKey("__len__"));
        var invalid = (PyClass)PyOps.MakeClass("SearchInvalidText", [PyType.Get("str")], null);
        invalid.SetAttr("__len__", Fn(_ => 1L)); long invalidVersion = invalid.Version;
        Assert.Equal("str subclass slot __len__ is not supported",
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(invalid)).PyMessage);
        Assert.Equal(invalidVersion, invalid.Version);
        Assert.Throws<PyNotImplementedError>(() => invalid.Invoke(["a"], [], []));
        object text = cls.Invoke(["a😀a"], [], [])!, needle = cls.Invoke(["a"], [], [])!;
        long[] expected = [2, 0, 2, 0, 2];
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
            for (int i = 0; i < Methods.Length; i++)
            {
                Assert.Equal(expected[i], Route(route, text, Methods[i], [needle, null, null]));
                Assert.Equal(expected[i], Route(route, "a😀a", Methods[i], [needle]));
            }
        cls.SetAttr("count", Fn(_ => 91L));
        Assert.Equal(91L, Call(text, "count", "a", 0L, 3L));
        Assert.Equal(2L, Route("root", text, "count", ["a", 0L, 3L]));
        PyObject.SetAttr(text, "count", Fn(_ => 92L));
        Assert.Equal(92L, Call(text, "count", "a"));
        Assert.Equal(2L, Route("root", text, "count", ["a"]));
    }

    [Fact]
    public void AllBindingRoutesRejectKeywordsBeforeArityWithoutIndexCallbacks()
    {
        int calls = 0; var index = Index(() => { calls++; return 0L; });
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
            foreach (string method in Methods)
            {
                Assert.Equal($"{method}() takes no keyword arguments",
                    Assert.Throws<PyTypeError>(() => Route(route, "abc", method, ["a", index, 3L, 4L], ["sub"])).PyMessage);
                Assert.Equal($"{method}() takes at least 1 argument (0 given)",
                    Assert.Throws<PyTypeError>(() => Route(route, "abc", method, [])).PyMessage);
                Assert.Equal($"{method}() takes at most 3 arguments (4 given)",
                    Assert.Throws<PyTypeError>(() => Route(route, "abc", method, ["a", index, 3L, 4L])).PyMessage);
                Assert.Equal($"descriptor '{method}' for 'str' objects doesn't apply to a 'int' object",
                    Assert.Throws<PyTypeError>(() => Route("root", 42L, method, ["a"])).PyMessage);
            }
        Assert.Equal(0, calls);
    }

    [Fact]
    public void RealSameSiteThreeArgumentReentryFreezesNeedleAndEndButNotEndSlot()
    {
        long[] expected = [2, 0, 4, 0, 4];
        for (int i = 0; i < Methods.Length; i++)
        {
            int site = 3870 + i; string method = Methods[i]; var events = new List<string>();
            var end = Index(() => throw new PyValueError("stale end"));
            var start = Index(() =>
            {
                events.Add("start"); Assert.Equal(1L, PyPic.CallMethod3(site, "zz", method, "z", 1L, 2L));
                end.Class.SetAttr("__index__", Fn(_ => { events.Add("new-end"); return 5L; }));
                return 0L;
            });
            Assert.Equal(expected[i], PyPic.CallMethod3(site, "a😀bca", method, "a", start, end));
            Assert.Equal(new[] { "start", "new-end" }, events);
        }
    }

    [Fact]
    public void BoundsPrecedeNeedleValidationAndUserExceptionIdentitySurvives()
    {
        foreach (string method in Methods)
        {
            var events = new List<string>();
            var start = Index(() => { events.Add("start"); return 0L; });
            var end = Index(() => { events.Add("end"); return 3L; });
            Assert.Equal("must be str, not int", Assert.Throws<PyTypeError>(() => Call("abc", method, 42L, start, end)).PyMessage);
            Assert.Equal(new[] { "start", "end" }, events); events.Clear();
            var marker = new PyValueError("marker"); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
            var bad = Index(() => { events.Add("bad"); throw marker; });
            Assert.Same(marker, Assert.Throws<PyValueError>(() => Call("abc", method, 42L, start, bad)));
            Assert.Equal(new[] { "start", "bad" }, events); events.Clear();
            Assert.Same(marker, Assert.Throws<PyValueError>(() => Call("abc", method, "a", bad, end)));
            Assert.Equal(new[] { "bad" }, events); Assert.Same(frames, marker.Frames);
        }
    }

    [Fact]
    public void ListAndTupleCountAndIndexKeepTheirOldArity()
    {
        foreach (object collection in new object[] { new PyList([1L, 2L, 1L]), new PyTuple([1L, 2L, 1L]) })
        {
            Assert.Equal(2L, Call(collection, "count", 1L)); Assert.Equal(1L, Call(collection, "index", 2L));
            foreach (string name in new[] { "count", "index" })
                Assert.Equal($"{name}() expected 1 to 1 arguments, got 3",
                    Assert.Throws<PyTypeError>(() => Route("prepared", collection, name, [1L, 0L, 3L])).PyMessage);
        }
    }
}
