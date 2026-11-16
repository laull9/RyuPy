// split直接专项：冻结CP向量、独立绑定顺序、一次index、整片身份与安全可控租约收尾。
using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexSplitTests
{
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyByteArray A(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyMemoryView View(object value) => (PyMemoryView)PyMemoryView.Create([value], [], []);
    private static object? Call(object? fn, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(fn, args, names ?? [], values ?? []);
    private static object? Module => PyRegex.CreateAttributes()["split"];
    private static object? Method(object pattern) => PyRegex.Compile(pattern).GetAttr("split");
    private static PyList Split(object pattern, object? source, object? limit = null) =>
        Assert.IsType<PyList>(Call(Method(pattern), [source, limit ?? 0L]));
    private static PyInstance Index(Func<object?> action)
    {
        var cls = new PyClass("SplitIndex");
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }
    private static PyList Controlled(PyRegexPattern pattern, object source, long limit, Func<Regex, string, int, Match> executor)
    {
        try
        {
            return (PyList)typeof(PyRegexPattern).GetMethod("SplitMutableBytes", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(pattern, [source, limit, executor])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }

    [Theory]
    [InlineData(",", "a,b,c", "['a', 'b', 'c']", "[b'a', b'b', b'c']")]
    [InlineData(",", ",a,", "['', 'a', '']", "[b'', b'a', b'']")]
    [InlineData("(,)(x)?", "a,b", "['a', ',', None, 'b']", "[b'a', b',', None, b'b']")]
    [InlineData("(,)|(:)", "a,b:c", "['a', ',', None, 'b', None, ':', 'c']", "[b'a', b',', None, b'b', None, b':', b'c']")]
    [InlineData("((,))", "a,b", "['a', ',', ',', 'b']", "[b'a', b',', b',', b'b']")]
    [InlineData("(a)+", "baac", "['b', 'a', 'c']", "[b'b', b'a', b'c']")]
    [InlineData("", "ab", "['', 'a', 'b', '']", "[b'', b'a', b'b', b'']")]
    [InlineData("", "", "['', '']", "[b'', b'']")]
    [InlineData("x", "abc", "['abc']", "[b'abc']")]
    [InlineData("x", "", "['']", "[b'']")]
    [InlineData("|a", "a", "['', '', '', '']", "[b'', b'', b'', b'']")]
    [InlineData(".*?", "ab", "['', '', '', '', '', '']", "[b'', b'', b'', b'', b'', b'']")]
    [InlineData("()", "ab", "['', '', 'a', '', 'b', '', '']", "[b'', b'', b'a', b'', b'b', b'', b'']")]
    [InlineData("(?=a)", "aba", "['', 'ab', 'a']", "[b'', b'ab', b'a']")]
    [InlineData("^|$", "ab", "['', 'ab', '']", "[b'', b'ab', b'']")]
    public void FrozenRawMatchVectorsApplyToBothDomainsAndEntries(string pattern, string input, string expected, string byteExpected)
    {
        foreach (bool bytes in new[] { false, true })
        {
            object p = bytes ? B(pattern) : pattern, source = bytes ? B(input) : input;
            // 两域期望均为冻结CP原流的显式常量，不从另一生产路径的结果生成。
            string wanted = bytes ? byteExpected : expected;
            Assert.Equal(expected, PyRepr.Repr(Split(pattern, input)));
            Assert.Equal(wanted, PyRepr.Repr(Call(Module, [p, source])));
            Assert.Equal(wanted, PyRepr.Repr(Call(Method(p), [source])));
        }
    }

    [Fact]
    public void ScalarAdvanceKeepsAstralPairsAndIsolatedSurrogates()
    {
        var result = Split("", "a😀b");
        Assert.Equal(new object?[] { "", "a", "😀", "b", "" }, result.Items);
        result = Split("", "\ud800x\udc00");
        Assert.Equal(new object?[] { "", "\ud800", "x", "\udc00", "" }, result.Items);
    }

    [Theory]
    [InlineData(0L, "['a', ',', 'b', ',', 'c']")]
    [InlineData(1L, "['a', ',', 'b,c']")]
    [InlineData(2L, "['a', ',', 'b', ',', 'c']")]
    [InlineData(-1L, "['a,b,c']")]
    [InlineData(1099511627776L, "['a', ',', 'b', ',', 'c']")]
    public void LimitCountsSeparatorsNotCapturesAndIsSsizeT(long limit, string expected)
    {
        Assert.Equal(expected, PyRepr.Repr(Split("(,)", "a,b,c", limit)));
        Assert.Equal(PyRepr.Repr(Split("(,)", "a,b,c", limit)),
            PyRepr.Repr(Call(Module, ["(,)", "a,b,c", limit])));
        Assert.Equal(limit == -1 ? 1 : limit == 1 ? 3 : 5, Split(B("(,)"), B("a,b,c"), limit).Items.Count);
    }

    [Fact]
    public void WholeExactSubjectsKeepIdentityButMutableResultsAreIndependent()
    {
        string text = new("not interned".ToCharArray()); var bytes = B(text);
        foreach (long limit in new[] { 0L, -1L })
        {
            Assert.Same(text, Assert.Single(Split("z", text, limit).Items));
            Assert.Same(bytes, Assert.Single(Split(B("z"), bytes, limit).Items));
            var array = A(text); var result = Assert.IsType<PyBytes>(Assert.Single(Split(B("z"), array, limit).Items));
            array.SetItem(0L, 88L); array.Append(33L);
            Assert.Equal(bytes.Bytes, result.Bytes);
        }
    }

    [Fact]
    public void IndexRunsOnceBeforeSubjectConsumptionAndKeepsOriginalFailure()
    {
        foreach (object pattern in new object[] { ",", B(",") })
        {
            int calls = 0; var marker = new PyLookupError("index marker");
            var error = Assert.Throws<PyLookupError>(() => Split(pattern, PyNone.Instance, Index(() => { calls++; throw marker; })));
            Assert.Same(marker, error); Assert.Equal(1, calls);
            Assert.Equal("expected string or bytes-like object, got 'NoneType'",
                Assert.Throws<PyTypeError>(() => Split(pattern, PyNone.Instance, -1L)).PyMessage);
            Assert.Equal("Python int too large to convert to C ssize_t",
                Assert.Throws<PyOverflowError>(() => Split(pattern, PyNone.Instance, BigInteger.One << 63)).PyMessage);
            Assert.Equal("Python int too large to convert to C ssize_t",
                Assert.Throws<PyOverflowError>(() => Split(pattern, PyNone.Instance, -(BigInteger.One << 63) - 1)).PyMessage);
        }
        var data = A("a,b"); int seen = 0;
        var result = Split(B(","), data, Index(() => { seen++; data.SetItem(1L, 58L); data.Append(33L); return 0L; }));
        Assert.Equal("[b'a:b!']", PyRepr.Repr(result)); Assert.Equal(1, seen); data.Append(33L);
        var view = View(data);
        Assert.Throws<PyTypeError>(() => Split(B(","), view, Index(() => { view.Release(); return 0L; })));
        data.Append(33L);
    }

    [Fact]
    public void MutableAndViewResultsCopyOnlyFinalPiecesAndReleaseExports()
    {
        var data = A("!a,b?"); var view = View(data);
        var slice = (PyMemoryView)view.GetItem(new PySlice(1L, 4L, 1L));
        var chars = (PyMemoryView)Call(view.GetAttr("cast"), ["c"])!;
        var readOnly = View(B("a,b"));
        foreach (object source in new object[] { data, view, slice, chars, readOnly })
        {
            var output = Split(B("(,)(x)?"), source);
            string before = PyRepr.Repr(output);
            data.SetItem(2L, 58L); Assert.Equal(before, PyRepr.Repr(output)); data.SetItem(2L, 44L);
            Assert.IsType<PyNone>(output.Items[2]);
        }
        slice.Release(); chars.Release(); view.Release(); readOnly.Release(); data.Append(33L);
    }

    [Fact]
    public void ControlledFailureReleasesBothLeaseLevelsAndPreservesException()
    {
        var data = A("a,a"); var view = View(data); var marker = new PyRuntimeError("safe backend fault");
        int calls = 0;
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Controlled(PyRegex.Compile(B("a")), view, 0L,
            (regex, text, position) =>
            {
                Assert.Throws<PyBufferError>(() => data.Append(33L));
                Assert.Equal("memoryview has 1 exported buffer", Assert.Throws<PyBufferError>(view.Release).PyMessage);
                if (++calls == 2) throw marker;
                return regex.Match(text, position);
            })));
        Assert.Equal(2, calls); view.Release(); data.Append(33L);
    }

    [Fact]
    public void LimitStopsImmediatelyAndRetryReusesOneInputEncoding()
    {
        var data = A("aa"); int calls = 0;
        var pattern = PyRegex.Compile(B("a"));
        Assert.Equal("[b'', b'a']", PyRepr.Repr(Controlled(pattern, data, 1L, (regex, text, position) =>
        { Assert.Equal(1, ++calls); return regex.Match(text, position); })));
        Controlled(pattern, data, -1L, (_, _, _) => throw new InvalidOperationException("must not scan"));
        string? input = null;
        Assert.Equal("[b'', b'', b'', b'', b'', b'']", PyRepr.Repr(Controlled(PyRegex.Compile(B(".*?")), data, 0L,
            (regex, text, position) => { input ??= text; Assert.Same(input, text); return regex.Match(text, position); })));
        data.Append(33L);
    }

    [Fact]
    public void ModuleBindingMatchesPythonFunctionErrorsBeforeCompile()
    {
        void Error(string expected, object?[] args, string[]? names = null, object?[]? values = null) =>
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Call(Module, args, names, values)).PyMessage);
        Error("split() missing 2 required positional arguments: 'pattern' and 'string'", []);
        Error("split() missing 1 required positional argument: 'string'", [","]);
        Error("split() takes from 2 to 4 positional arguments but 5 were given", [",", "a", 0L, 0L, 1L]);
        Error("split() got an unexpected keyword argument 'unknown'", ["[", "a", 0L, 0L, 1L], ["unknown"], [1L]);
        Error("split() got multiple values for argument 'maxsplit'", [",", "a", 0L], ["maxsplit"], [1L]);
        Assert.Equal("['a', 'b']", PyRepr.Repr(Call(Module, [], ["pattern", "string", "maxsplit", "flags"], [",", "a,b", 1L, 0L])));
    }

    [Fact]
    public void PatternBindingHasItsOwnCountMissingDuplicateAndUnknownPriority()
    {
        var fn = Method(",");
        void Error(string expected, object?[] args, string[]? names = null, object?[]? values = null) =>
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Call(fn, args, names, values)).PyMessage);
        Error("split() missing required argument 'string' (pos 1)", []);
        Error("split() missing required argument 'string' (pos 1)", [], ["unknown"], [1L]);
        Error("split() takes at most 2 arguments (3 given)", ["a", 0L], ["maxsplit"], [1L]);
        Error("argument for split() given by name ('string') and position (1)", ["a"], ["string"], ["b"]);
        Error("'flags' is an invalid keyword argument for split()", ["a"], ["flags"], [0L]);
        Assert.Equal("['a', 'b']", PyRepr.Repr(Call(fn, [], ["string", "maxsplit"], ["a,b", 1L])));
    }

    [Fact]
    public void CompileAndFlagsPrecedeIndexAndDomainsFollowIt()
    {
        int calls = 0; var index = Index(() => { calls++; return 0L; });
        Assert.Throws<PyRegexError>(() => Call(Module, ["[", PyNone.Instance, index]));
        Assert.Throws<PyValueError>(() => Call(Module, [PyRegex.Compile(","), PyNone.Instance, index, 2L]));
        Assert.Equal(0, calls);
        Assert.Equal("cannot use a string pattern on a bytes-like object", Assert.Throws<PyTypeError>(() => Split(",", B("a"), index)).PyMessage);
        Assert.Equal(1, calls);
        Assert.Equal("cannot use a bytes pattern on a string-like object", Assert.Throws<PyTypeError>(() => Split(B(","), "a", index)).PyMessage);
        Assert.Equal(2, calls);
    }
}
