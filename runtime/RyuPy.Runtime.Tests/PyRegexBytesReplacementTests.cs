// bytes正则B内部专项：保模板域、展开/最终join两层错误、用户callback顺序和64位冷index。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexBytesReplacementTests
{
    private static object Invoke(Type type, string name, object? owner, params object?[] args)
    {
        try { return type.GetMethod(name, BindingFlags.NonPublic | (owner is null ? BindingFlags.Static : BindingFlags.Instance))!.Invoke(owner, args)!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyRegexPattern P(string value) => (PyRegexPattern)Invoke(typeof(PyRegexPattern), "CreateBytes", null, B(value), 0);
    private static object Apply(PyRegexPattern pattern, object? subject, object? start = null, object? end = null) =>
        Invoke(typeof(PyRegexPattern), "ApplyBytesInput", pattern, "search", subject, start ?? 0L, end ?? long.MaxValue);
    private static object Sub(PyRegexPattern pattern, object? replacement, object? subject, object? count = null, bool tuple = false) =>
        Invoke(typeof(PyRegexPattern), "SubstituteBytesInput", pattern, replacement, subject, count ?? 0L, tuple);
    private static object Expand(PyRegexMatch match, object? template) => Invoke(typeof(PyRegexMatch), "ExpandBytes", match, template);
    private static PyBytes Escape(object? source) => (PyBytes)Invoke(typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBytes")!, "Escape", null, source);
    private static PyFunction Callback(Func<PyRegexMatch, object?> callback) => PyNativeFunctions.CreateVariadic("replacement", a => callback((PyRegexMatch)a[0]!));
    private static object? Group(PyRegexMatch match) => PyOps.CallValue(match.GetAttr("group"), [], [], []);
    private static string Span(PyRegexMatch match) => PyRepr.Repr(PyOps.CallValue(match.GetAttr("span"), [], [], []));
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("Index"); cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ => body()));
        return new PyInstance(cls);
    }
    private static string BytesText(object? value) => Encoding.Latin1.GetString(Assert.IsType<PyBytes>(value).Bytes);

    [Fact]
    public void MixedStrTemplatesDistinguishExpansionFromTheFinalBytesJoin()
    {
        var pattern = P("(a)"); PyBytes source = B("a"); PyBytes miss = B("x");
        foreach (string template in new[] { @"\1", "", "X" }) Assert.Same(miss, Sub(pattern, template, miss));
        Assert.Equal("sequence item 1: expected str instance, bytes found",
            Assert.Throws<PyTypeError>(() => Sub(pattern, @"\1", source)).PyMessage);
        foreach (string template in new[] { "", "X" })
            Assert.Equal("sequence item 0: expected a bytes-like object, str found", Assert.Throws<PyTypeError>(() => Sub(pattern, template, source)).PyMessage);
        Assert.Equal("a", BytesText(Sub(pattern, B(@"\1"), source)));
        Assert.Equal("", BytesText(Sub(pattern, B(""), source)));
        Assert.Equal("X", BytesText(Sub(pattern, B("X"), source)));
    }

    [Fact]
    public void ExpandPreservesTheTemplateDomainAndMissingGroupsAreOmitted()
    {
        PyRegexMatch present = Assert.IsType<PyRegexMatch>(Apply(P("(a)"), B("a")));
        PyRegexMatch missing = Assert.IsType<PyRegexMatch>(Apply(P("(a)?b"), B("b")));
        Assert.Equal("", Expand(present, "")); Assert.Equal("X", Expand(present, "X"));
        Assert.Equal("sequence item 1: expected str instance, bytes found", Assert.Throws<PyTypeError>(() => Expand(present, @"\1")).PyMessage);
        Assert.Equal("", Expand(missing, @"\1")); Assert.Equal("", BytesText(Expand(missing, B(@"\1"))));
        Assert.Equal("a", BytesText(Expand(present, B(@"\1"))));
        Assert.Equal("sequence item 0: expected a bytes-like object, str found",
            Assert.Throws<PyTypeError>(() => Sub(P("(a)?b"), @"\1", B("b"))).PyMessage);
    }

    [Fact]
    public void NamedNumericAndEscapedTemplatesUsePythonRatherThanDollarSyntax()
    {
        var pattern = P("(?P<first>a)(b)"); PyBytes subject = B("ab ab");
        Assert.Equal("b-a-ab b-a-ab", BytesText(Sub(pattern, B(@"\2-\g<first>-\g<0>"), subject)));
        Assert.Equal("b-a", BytesText(Expand(Assert.IsType<PyRegexMatch>(Apply(pattern, subject)), B(@"\2-\g<first>"))));
        Assert.Equal("$1 $1", BytesText(Sub(pattern, B("$1"), subject)));
        Assert.Equal(new byte[] { 0, 255, 10, 92 }, Assert.IsType<PyBytes>(Sub(P("a"), B(@"\000\377\n\\"), B("a"))).Bytes);
        Assert.Equal(@"\&", BytesText(Sub(P("a"), B(@"\&"), B("a"))));
    }

    [Fact]
    public void InvalidTemplatesAreParsedBeforeSubjectEvenWithNoMatchesOrNegativeCount()
    {
        var pattern = P("(a)"); PyBytes template = B(@"\9");
        foreach (object subject in new object[] { B("x"), 3L })
        foreach (long count in new[] { 0L, -1L })
        {
            var error = Assert.Throws<PyRegexError>(() => Sub(pattern, template, subject, count));
            Assert.Same(template, error.Pattern); Assert.Equal(1L, error.Position);
            Assert.Equal("invalid group reference 9 at position 1", error.PyMessage);
        }
        Assert.Equal("unknown group name 'absent'", Assert.Throws<PyIndexError>(() => Sub(pattern, B(@"\g<absent>"), B("x"))).PyMessage);
        foreach (string value in new[] { @"\u0061", @"\q" })
        {
            PyBytes source = B(value); var error = Assert.Throws<PyRegexError>(() => Sub(pattern, source, B("x")));
            Assert.Same(source, error.Pattern); Assert.Equal(0L, error.Position);
        }
        Assert.Equal("decoding to str: need a bytes-like object, int found", Assert.Throws<PyTypeError>(() => Sub(pattern, 3L, B("x"))).PyMessage);
    }

    [Fact]
    public void CallbackWrongTypesAreCheckedOnlyAfterAllRequiredCallbacks()
    {
        var pattern = P("a"); PyBytes subject = B("aa");
        foreach (object result in new object[] { "X", "", 3L })
        {
            var seen = new List<string>();
            var callback = Callback(match => { seen.Add(Span(match)); Assert.Same(subject, match.GetAttr("string")); return result; });
            var error = Assert.Throws<PyTypeError>(() => Sub(pattern, callback, subject));
            Assert.Equal<string>(["(0, 1)", "(1, 2)"], seen);
            Assert.Equal($"sequence item 0: expected a bytes-like object, {PyRepr.TypeName(result)} found", error.PyMessage);
            seen.Clear(); PyBytes miss = B("zz"); Assert.Same(miss, Sub(pattern, callback, miss)); Assert.Empty(seen);
        }
        var preceded = Assert.Throws<PyTypeError>(() => Sub(pattern, Callback(_ => "bad"), B("za")));
        Assert.Equal("sequence item 1: expected a bytes-like object, str found", preceded.PyMessage);
    }

    [Fact]
    public void LaterCallbackErrorWinsOverAnEarlierInvalidResultAndKeepsIdentity()
    {
        var marker = new PyRuntimeError("later-callback-marker"); var seen = new List<string>();
        var callback = Callback(match =>
        {
            seen.Add(Span(match)); if (seen.Count == 2) throw marker; return "invalid earlier";
        });
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Sub(P("a"), callback, B("aa"))));
        Assert.Equal<string>(["(0, 1)", "(1, 2)"], seen);
    }

    [Fact]
    public void CountsNoneAndSinglePieceIdentityMatchTheRealBytesOutput()
    {
        var pattern = P("a"); PyBytes subject = B("aa"); var seen = new List<string>();
        var callback = Callback(match => { seen.Add(Span(match)); return PyNone.Instance; });
        Assert.Equal("(b'', 2)", PyRepr.Repr(Sub(pattern, callback, subject, tuple: true)));
        seen.Clear(); Assert.Equal("(b'a', 1)", PyRepr.Repr(Sub(pattern, callback, subject, 1L, true))); Assert.Single(seen);
        seen.Clear(); Assert.Same(subject, Sub(pattern, callback, subject, -1L)); Assert.Empty(seen);
        Assert.Equal("XX", BytesText(Sub(pattern, B("X"), subject, 1L << 40)));
        PyBytes whole = B("whole"); Assert.Same(whole, Sub(P(".+"), Callback(match => Group(match)), whole));
        PyBytes replacement = B("only"); Assert.Same(replacement, Sub(P(".+"), replacement, whole));
        Assert.Same(whole, Sub(P("Z"), B("X"), whole));
    }

    [Fact]
    public void MutableCallbackResultsReadFinalContentsAndRetainErrorPriority()
    {
        var source = new PyByteArray([65]); int calls = 0;
        var callback = Callback(_ => { calls++; source.Bytes[0]++; return source; });
        Assert.Equal("CC", BytesText(Sub(P("a"), callback, B("aa")))); Assert.Equal(2, calls);
        calls = 0;
        var firstInvalid = Callback(_ => ++calls == 1 ? 3L : source);
        Assert.Throws<PyTypeError>(() => Sub(P("a"), firstInvalid, B("aa"))); Assert.Equal(2, calls);
        calls = 0; var marker = new PyRuntimeError("later");
        var laterError = Callback(_ => { if (++calls == 2) throw marker; return source; });
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Sub(P("a"), laterError, B("aa")))); Assert.Equal(2, calls);
    }

    [Fact]
    public void DirectMutableSubjectsAndBytearrayTemplatesKeepIndexOrder()
    {
        var mutable = new PyByteArray([97]); var pattern = P("a"); var events = new List<string>();
        Assert.Equal("a", BytesText(Sub(pattern, mutable, B("a"), Index(() => { events.Add("count"); return 1L; }))));
        Assert.Equal<string>(["count"], events); events.Clear();
        Assert.IsType<PyRegexMatch>(Apply(pattern, mutable,
            Index(() => { events.Add("pos"); return 0L; }), Index(() => { events.Add("end"); return 1L; })));
        Assert.Equal<string>(["pos", "end"], events);
        Assert.Equal("X", BytesText(Sub(pattern, B("X"), mutable)));
        Assert.Equal("a", BytesText(Escape(mutable)));
        Assert.Equal("a", BytesText(Expand(Assert.IsType<PyRegexMatch>(Apply(pattern, B("a"))), mutable)));
    }

    [Fact]
    public void MutableCountConversionPrecedesLeaseAndFreezesTheResultingLength()
    {
        var pattern = P("a"); var data = new PyByteArray([97]); int counts = 0, callbacks = 0;
        var count = Index(() => { counts++; data.Append(97L); return 0L; });
        var replacement = Callback(match =>
        { callbacks++; Assert.Same(data, match.GetAttr("string")); Assert.Equal(2L, match.GetAttr("endpos")); return B("X"); });
        Assert.Equal("(b'XX', 2)", PyRepr.Repr(Sub(pattern, replacement, data, count, true)));
        Assert.Equal(1, counts); Assert.Equal(2, callbacks); data.Append(33L);
        var releasedData = new PyByteArray([97]); var view = (PyMemoryView)PyMemoryView.Create([releasedData], [], []);
        Assert.Equal("expected string or bytes-like object, got 'memoryview'",
            Assert.Throws<PyTypeError>(() => Sub(pattern, B("X"), view, Index(() => { view.Release(); return -1L; }))).PyMessage);
        releasedData.Append(33L);
    }

    [Fact]
    public void MutablePublicPreparationPreservesCountThenTemplateThenSubjectOrder()
    {
        var pattern = P("a"); var data = new PyByteArray([97]); var view = (PyMemoryView)PyMemoryView.Create([data], [], []);
        var marker = new PyRuntimeError("count before template"); PyBytes bad = B(@"\9");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Sub(pattern, bad, view, Index(() => throw marker))));
        // 模板报错前未取得本次export，之后可以直接release/resize。
        var error = Assert.Throws<PyRegexError>(() => Sub(pattern, bad, view, -1L));
        Assert.Same(bad, error.Pattern); Assert.Equal(1L, error.Position); view.Release(); data.Append(33L);
        Assert.Throws<PyRegexError>(() => Sub(pattern, bad, view, -1L));
        Assert.Equal("expected string or bytes-like object, got 'memoryview'",
            Assert.Throws<PyTypeError>(() => Sub(pattern, B("X"), view, -1L)).PyMessage);
        int conversions = 0;
        Assert.Equal("cannot process flags argument with a compiled pattern", Assert.Throws<PyValueError>(() =>
            PyOps.CallValue(PyRegex.CreateAttributes()["sub"], [pattern, B("X"), data,
                Index(() => { conversions++; return 0L; }), 2L], [], [])).PyMessage);
        Assert.Equal(0, conversions);
    }

    [Fact]
    public void WideIndexIsCheckedBeforeClampingAndUsesSsizeRatherThanCInt()
    {
        var pattern = P("a"); PyBytes source = B("a");
        Assert.IsType<PyNone>(Apply(pattern, source, 1L << 40));
        Assert.IsType<PyRegexMatch>(Apply(pattern, source, 0L, 1L << 40));
        Assert.IsType<PyRegexMatch>(Apply(pattern, source, new BigInteger(-1L << 40), BigInteger.One));
        foreach (BigInteger value in new[] { BigInteger.One << 100, -(BigInteger.One << 100) })
        {
            Assert.Equal("Python int too large to convert to C ssize_t", Assert.Throws<PyOverflowError>(() => Apply(pattern, source, value)).PyMessage);
            Assert.Equal("Python int too large to convert to C ssize_t", Assert.Throws<PyOverflowError>(() => Sub(pattern, B("X"), source, value)).PyMessage);
        }
    }

    [Fact]
    public void IndexOrderAndUserFailuresPrecedeTemplateAndSubjectConsumption()
    {
        var pattern = P("a"); var events = new List<string>();
        Assert.Throws<PyTypeError>(() => Apply(pattern, 3L, Index(() => { events.Add("pos"); return 0L; }), Index(() => { events.Add("end"); return 2L; })));
        Assert.Equal<string>(["pos", "end"], events); events.Clear();
        Assert.Throws<PyRegexError>(() => Sub(pattern, B(@"\9"), 3L, Index(() => { events.Add("count"); return 1L; })));
        Assert.Equal<string>(["count"], events); events.Clear();
        var marker = new PyRuntimeError("index-marker");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Apply(pattern, 3L, Index(() => { events.Add("bad-pos"); throw marker; }),
            Index(() => { events.Add("end"); return 2L; }))));
        Assert.Equal<string>(["bad-pos"], events);
        Assert.Equal("__index__ returned non-int (type str)", Assert.Throws<PyTypeError>(() => Sub(pattern, B(@"\9"), 3L, Index(() => "bad"))).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Apply(pattern, B("a"), Index(() => true)));
    }

    [Fact]
    public void IndexIgnoresInstanceAttributesAndGuardsOnlyTheConsumedTypeSlot()
    {
        var plain = new PyInstance(new PyClass("Plain")); int calls = 0;
        PyObject.SetAttr(plain, "__index__", PyNativeFunctions.CreateVariadic("index", _ => { calls++; return 0L; }));
        Assert.Throws<PyTypeError>(() => Apply(P("a"), B("a"), plain)); Assert.Equal(0, calls);
        var descriptorClass = new PyClass("Descriptor");
        descriptorClass.SetAttr("__get__", PyNativeFunctions.CreateVariadic("get", _ => { calls++; return 0L; }));
        descriptorClass.SetAttr("__call__", PyNativeFunctions.CreateVariadic("call", _ => { calls++; return 0L; }));
        var indexClass = new PyClass("SpecialIndex"); indexClass.SetAttr("__index__", new PyInstance(descriptorClass));
        Assert.Throws<PyNotImplementedError>(() => Apply(P("a"), B("a"), new PyInstance(indexClass))); Assert.Equal(0, calls);
    }

    [Fact]
    public void EscapePreservesAllBytesAndProducesARealLiteralPattern()
    {
        var source = new PyBytes(Enumerable.Range(0, 256).Select(value => (byte)value).ToArray()); PyBytes escaped = Escape(source);
        var pattern = (PyRegexPattern)Invoke(typeof(PyRegexPattern), "CreateBytes", null, escaped, 0);
        PyRegexMatch match = Assert.IsType<PyRegexMatch>(Apply(pattern, source)); Assert.Same(source, Group(match));
        Assert.Equal(new byte[] { 0, 92, 35, 92, 32, 92, 91, 255 }, Escape(new PyBytes([0, 35, 32, 91, 255])).Bytes);
    }

    [Fact]
    public void LocalErrorsCaptureAmbientAndCallerWithoutRewrappingUserExceptions()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("bytes_regex_b.py", "caller"); PyTraceback.SetLine(31);
            PyBaseException[] errors = [Assert.Throws<PyRegexError>(() => Sub(P("a"), B(@"\9"), 3L)),
                Assert.Throws<PyTypeError>(() => Sub(P("a"), Callback(_ => "bad"), B("a"))),
                Assert.Throws<PyOverflowError>(() => Apply(P("a"), B("a"), BigInteger.One << 100))];
            foreach (PyBaseException error in errors)
            {
                Assert.Same(ambient, error.Context); Assert.Equal("caller", Assert.Single(error.Frames).Function);
                Assert.Equal(31, error.Frames[0].Line);
            }
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void ExistingTextTemplateAndCallbackPositivePathsRemainUnchanged()
    {
        var pattern = PyRegex.Compile("(?P<x>a)");
        Assert.Equal("<a> <a>", PyOps.CallValue(pattern.GetAttr("sub"), [@"<\g<x>>", "a a"], [], []));
        Assert.Equal("(b'X', 1)", PyRepr.Repr(Sub(P("a"), B("X"), B("a"), tuple: true)));
        Assert.Equal(B("a").Bytes, Assert.IsType<PyBytes>(PyRegex.Compile(B("a")).GetAttr("pattern")).Bytes);
    }

    [Fact]
    public void ActualClrCapacityGuardDoesNotRequireAllocatingALargeObject()
    {
        Type helper = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBytes")!;
        Invoke(helper, "CheckCapacity", null, (long)Array.MaxLength);
        Assert.Throws<PyNotImplementedError>(() => Invoke(helper, "CheckCapacity", null, (long)Array.MaxLength + 1));
    }
}
