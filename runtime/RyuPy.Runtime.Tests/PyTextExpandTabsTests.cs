// expandtabs 的公开入口、窄绑定、码点和无分配容量守卫；未用宿主测试冒充真实源码验收。
using System.Numerics;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextExpandTabsTests
{
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("expandtabs_test", body);
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("ExpandIndex"); cls.SetAttr("__index__", Fn(_ => body()));
        return new PyInstance(cls);
    }
    private static object? Route(string route, object text, object?[] args, string[]? names = null, object?[]? values = null)
    {
        names ??= []; values ??= [];
        if (route == "direct") return PyOps.CallMethod(text, "expandtabs", args, names, values);
        if (route == "root") return PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), "expandtabs"), [text, .. args], names, values);
        object? method = route == "saved" ? PyObject.GetAttr(text, "expandtabs") : PyOps.PrepareMethod(text, "expandtabs");
        return PyOps.CallValue(method, args, names, values);
    }
    private static string Expand(string text, object? size) => Assert.IsType<string>(Route("direct", text, [size]));

    [Fact]
    public void FourRoutesShareDefaultsKeywordsAndBindingPriority()
    {
        int calls = 0; var index = Index(() => { calls++; return 4L; });
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
        {
            Assert.Equal("a       b       ", Route(route, "a\tb\t", []));
            Assert.Equal("a   b   ", Route(route, "a\tb\t", [], ["tabsize"], [4L]));
            Assert.Equal("expandtabs() takes at most 1 argument (2 given)",
                Assert.Throws<PyTypeError>(() => Route(route, "", [index], ["tabsize"], [index])).PyMessage);
            Assert.Equal("expandtabs() takes at most 1 argument (3 given)",
                Assert.Throws<PyTypeError>(() => Route(route, "", [index, index], ["bad"], [index])).PyMessage);
            Assert.Equal("expandtabs() takes at most 1 keyword argument (2 given)",
                Assert.Throws<PyTypeError>(() => Route(route, "", [], ["tabsize", "bad"], [index, index])).PyMessage);
            Assert.Equal("'bad' is an invalid keyword argument for expandtabs()",
                Assert.Throws<PyTypeError>(() => Route(route, "", [], ["bad"], [index])).PyMessage);
        }
        Assert.Equal(0, calls);
        object? root = PyObject.GetAttr(PyType.Get("str"), "expandtabs");
        Assert.Equal("unbound method str.expandtabs() needs an argument",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], ["self"], ["x"])).PyMessage);
        Assert.Equal("descriptor 'expandtabs' for 'str' objects doesn't apply to a 'int' object",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [7L, index], ["bad"], [index])).PyMessage);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ConversionRunsOnceEvenWithoutTabsAndRejectsBothCIntOverflows()
    {
        int count = 0; var index = Index(() => { count++; return 4L; });
        Assert.Equal("", Expand("", index)); Assert.Equal("abc", Expand("abc", index));
        Assert.Equal("a   ", Expand("a\t", index)); Assert.Equal(3, count);
        foreach (object size in new object[] { int.MinValue, int.MaxValue, (long)int.MinValue, (long)int.MaxValue })
            Assert.Equal("x", Expand("x", size));
        foreach (object size in new object[] { (long)int.MinValue - 1, (long)int.MaxValue + 1, BigInteger.One << 100, -(BigInteger.One << 100) })
            foreach (string text in new[] { "", "x", "\t" })
                Assert.Equal("Python int too large to convert to C int",
                    Assert.Throws<PyOverflowError>(() => Expand(text, size)).PyMessage);
        Assert.Equal("'float' object cannot be interpreted as an integer", Assert.Throws<PyTypeError>(() => Expand("", 1.0)).PyMessage);
        Assert.Equal("__index__ returned non-int (type str)", Assert.Throws<PyTypeError>(() => Expand("", Index(() => "4"))).PyMessage);
    }

    [Fact]
    public void CodePointColumnsCopyPairsAndLoneSurrogatesAndOnlyCrLfReset()
    {
        Assert.Equal("😀   x", Expand("😀\tx", 4L));
        Assert.Equal("\ud800   \udc00   ", Expand("\ud800\t\udc00\t", 4L));
        foreach (char ordinary in new[] { '\v', '\f', '\u0085', '\u2028', '\u2029' })
            Assert.Equal("a" + ordinary + "  x", Expand("a" + ordinary + "\tx", 4L));
        foreach (string newline in new[] { "\r", "\n", "\r\n" })
            Assert.Equal("ab" + newline + "    x", Expand("ab" + newline + "\tx", 4L));
        Assert.Equal("ab", Expand("\ta\tb\t", 0L)); Assert.Equal("ab", Expand("\ta\tb\t", int.MinValue));
        Assert.Equal(" ", Expand("\t", 1L)); Assert.Equal("a b ", Expand("a\tb\t", true));
        Assert.Equal("ab", Expand("a\tb\t", false));
    }

    [Fact]
    public void CapacityChecksNeverAllocateTheirHypotheticalOutput()
    {
        const long limit = 0x3FFFFFDF;
        PyTextExpandTabs.CheckCapacity(limit - 1); PyTextExpandTabs.CheckCapacity(limit);
        Assert.Equal("str.expandtabs: result exceeds managed string capacity",
            Assert.Throws<PyNotImplementedError>(() => PyTextExpandTabs.CheckCapacity(limit + 1)).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => PyTextExpandTabs.Measure("\t", int.MaxValue));
        Assert.Equal(5, PyTextExpandTabs.Measure("😀\t", 4));
    }

    [Fact]
    public void SubtypeInheritanceSavedOverrideShadowAndSuperKeepTheirRoutes()
    {
        var cls = (PyClass)PyOps.MakeClass("ExpandText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object text = cls.Invoke(["a\t"], [], [])!;
        cls.SetAttr("__str__", Fn(_ => throw new PyValueError("must use raw text")));
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
            Assert.Equal("a   ", Assert.IsType<string>(Route(route, text, [4L])));
        object? saved = PyObject.GetAttr(text, "expandtabs");
        cls.SetAttr("expandtabs", Fn(_ => "override"));
        Assert.Equal("override", Route("direct", text, [4L]));
        object? savedOverride = PyObject.GetAttr(text, "expandtabs");
        PyObject.SetAttr(text, "expandtabs", Fn(_ => "shadow"));
        Assert.Equal("shadow", Route("direct", text, [4L]));
        Assert.Equal("override", PyOps.CallValue(savedOverride, [4L], [], []));
        Assert.Equal("a   ", PyOps.CallValue(saved, [4L], [], []));
        Assert.Equal("a   ", Route("root", text, [4L]));
        Assert.Equal("a   ", PyOps.CallValue(PyObject.GetAttr(new PySuper(cls, text), "expandtabs"), [4L], [], []));
    }

    [Fact]
    public void SharedPicArgumentArrayIsFrozenBeforeIndexReentry()
    {
        const int site = 7391; int calls = 0;
        var index = Index(() =>
        {
            calls++; Assert.Equal("inner   ", PyPic.CallMethod1(site, "inner\t", "expandtabs", 4L)); return 8L;
        });
        Assert.Equal("outer   ", PyPic.CallMethod1(site, "outer\t", "expandtabs", index)); Assert.Equal(1, calls);
    }
}
