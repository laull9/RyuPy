// F2专项：bytearray模板短租约/快照、准备顺序、独立buffer escape和原exact控制；view模板仍明确NI。
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexBufferTemplateTests
{
    private static readonly Type BytesType = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBytes")!;
    private static readonly Type TemplateType = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBytesTemplate")!;
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyByteArray A(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyRegexPattern P(string value) => PyRegex.Compile(B(value));
    private static PyMemoryView View(object source) => (PyMemoryView)PyMemoryView.Create([source], [], []);
    private static object? Call(PyNativeObject owner, string name, params object?[] args) => PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static object? Module(string name, params object?[] args) => PyOps.CallValue(PyRegex.CreateAttributes()[name], args, [], []);
    private static string Text(object? value) => Encoding.Latin1.GetString(Assert.IsType<PyBytes>(value).Bytes);
    private static int Exports(object owner) => (int)owner.GetType().GetField(
        owner is PyMemoryView ? "_regexExports" : "_exports", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static object? Invoke(Type type, string method, object? owner, params object?[] args)
    {
        try { return type.GetMethod(method, BindingFlags.NonPublic | (owner is null ? BindingFlags.Static : BindingFlags.Instance))!.Invoke(owner, args); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static PyInstance Index(Func<object?> action)
    {
        var cls = new PyClass("TemplateCount");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("__index__", _ => action()));
        return new PyInstance(cls);
    }
    private static void Replace(PyByteArray data, string value)
    { data.DelItem(new PySlice(PyNone.Instance, PyNone.Instance, 1L)); data.Extend(B(value)); }
    private static PyRegexMatch Match(string expression, object source) => Assert.IsType<PyRegexMatch>(Call(P(expression), "search", source));
    private static (object Source, PyByteArray Owner, PyMemoryView? View) Carrier(string kind, string content)
    {
        var data = A(kind == "slice" ? "!" + content + "?" : content);
        if (kind == "array") return (data, data, null);
        var view = View(kind.StartsWith("readonly", StringComparison.Ordinal) ? B(content) : data);
        if (kind is "c" or "readonly_c" or "slice")
        {
            var parent = view;
            view = kind == "slice" ? (PyMemoryView)parent.GetItem(new PySlice(1L, (long)data.Count - 1, 1L))
                : (PyMemoryView)Call(parent, "cast", "c")!;
            parent.Release();
        }
        return (view, data, view);
    }

    [Fact]
    public void BytearrayLiteralUsesPublicModulePatternAndSavedMethods()
    {
        var replacement = A("X"); var pattern = P("a"); object? saved = pattern.GetAttr("subn");
        var data = A("aa"); var view = View(data);
        try
        {
            foreach (object source in new object[] { B("aa"), data, view })
            {
                Assert.Equal("XX", Text(Call(pattern, "sub", replacement, source)));
                Assert.Equal("(b'XX', 2)", PyRepr.Repr(PyOps.CallValue(saved, [replacement, source], [], [])));
                Assert.Equal("Xa", Text(Module("sub", B("a"), replacement, source, 1L)));
            }
        }
        finally { view.Release(); }
        replacement.Append(33L); data.Append(33L);
    }

    [Fact]
    public void BytearrayTemplatesReuseNamedNumericMissingAndEscapedParsing()
    {
        var expression = P("(?P<x>a)(b)?"); var template = A(@"<\g<x>|\2|\g<0>>");
        Assert.Equal("<a||a> <a|b|ab>", Text(Call(expression, "sub", template, A("a ab"))));
        Assert.Equal("<a|b|ab>", Text(Call(Match("(?P<x>a)(b)?", B("ab")), "expand", template)));
        Assert.Equal(new byte[] { 0, 255, 10, 92 }, Assert.IsType<PyBytes>(
            Call(P("a"), "sub", A(@"\000\377\n\\"), B("a"))).Bytes);
        Assert.Equal("$1", Text(Call(P("a"), "sub", A("$1"), B("a")))); template.Append(33L);
    }

    [Fact]
    public void CountConversionMutatesOrResizesReplacementBeforePreparation()
    {
        var template = A(@"\1"); var events = new List<string>();
        var count = Index(() => { events.Add("count"); Assert.Equal(0, Exports(template)); template.SetItem(1L, 50L); return 1L; });
        Assert.Equal("b", Text(Module("sub", B("(a)(b)"), template, B("ab"), count)));
        Assert.Equal(new[] { "count" }, events);
        var literal = A("X");
        Assert.Equal("YZ", Text(Call(P("a"), "sub", literal, B("a"),
            Index(() => { Replace(literal, "YZ"); return 1L; }))));
        literal.Append(33L); template.Append(33L);
    }

    [Fact]
    public void InvalidBytearrayTemplatePrecedesSubjectAndKeepsSnapshotMetadata()
    {
        foreach (object source in new object[] { B("a"), B("z"), 3L })
        foreach (long count in new[] { 0L, -1L })
        {
            var template = A(@"\9");
            var error = Assert.Throws<PyRegexError>(() => Call(P("(a)"), "sub", template, source, count));
            Assert.Equal("invalid group reference 9 at position 1", error.PyMessage);
            Assert.Equal("invalid group reference 9", error.MessageText); Assert.Equal(1L, error.Position);
            Assert.Equal(1L, error.LineNumber); Assert.Equal(2L, error.ColumnNumber);
            Assert.NotSame(template, error.Pattern); Assert.Equal(@"\9", Text(error.Pattern));
            Assert.Equal(0, Exports(template)); Replace(template, "XX"); Assert.Equal(@"\9", Text(error.Pattern));
        }
    }

    [Fact]
    public void PreparedTemplateIsFixedButEachNewCallReadsChangedContent()
    {
        var pattern = P("(a)(b)"); var template = A(@"<\1>");
        object prepared = Invoke(BytesType, "PrepareArrayTemplate", null, pattern, template)!;
        Assert.Equal(0, Exports(template));
        var snapshot = Assert.IsType<PyBytes>(TemplateType.GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(prepared));
        Assert.NotSame(template, snapshot); Assert.Equal(@"<\1>", Text(snapshot));
        Replace(template, @"<\2>"); var match = Match("(a)(b)", B("ab"));
        Assert.Equal("<a>", Text(Invoke(TemplateType, "Expand", prepared, match)));
        Assert.Equal("<b>", Text(Call(pattern, "sub", template, B("ab"))));
        Replace(template, "XYZ!"); Assert.Equal("XYZ!", Text(Call(pattern, "sub", template, B("ab"))));
        Replace(template, @"<\1>"); Assert.Equal("<a>", Text(Call(pattern, "sub", template, B("ab"))));
    }

    [Fact]
    public void LiteralPreparationRetainsOriginalObjectWithoutALongLease()
    {
        var literal = A("X"); var pattern = P("a");
        Assert.Null(Invoke(BytesType, "PrepareArrayTemplate", null, pattern, literal)); Assert.Equal(0, Exports(literal));
        Replace(literal, "YZ");
        object? output = Invoke(typeof(PyRegexPattern), "SubstituteMutableBytesPrepared", pattern,
            literal, A("aa"), 0L, false, false, null, null);
        Assert.Equal("YZYZ", Text(output)); Assert.Equal(0, Exports(literal)); literal.Append(33L);
    }

    [Fact]
    public void BytearrayExpandUsesLiveCapturesAndReleasesTemplateBeforeGroupRead()
    {
        var data = A("ab"); var source = View(data); var match = Match("(a)(b)?", source); var template = A(@"<\1>");
        data.SetItem(0L, 88L); Assert.Equal("<X>", Text(Call(match, "expand", template)));
        Assert.Equal(0, Exports(template)); source.Release();
        Assert.Equal("literal", Text(Call(match, "expand", A("literal"))));
        Assert.Equal("expected string or bytes-like object, got 'memoryview'",
            Assert.Throws<PyTypeError>(() => Call(match, "expand", template)).PyMessage);
        Assert.Equal(0, Exports(template)); template.Append(33L); data.Append(33L);
    }

    [Fact]
    public void ReplacementCanShareSubjectExporterWithoutKeepingPreparationExports()
    {
        foreach (bool useView in new[] { false, true })
        {
            var data = A("aa"); PyMemoryView? view = useView ? View(data) : null; object subject = (object?)view ?? data;
            Assert.Equal("aaaa", Text(Call(P("a"), "sub", data, subject)));
            Assert.Equal(useView ? 1 : 0, Exports(data)); if (view is not null) Assert.Equal(0, Exports(view));
            view?.Release(); data.Append(33L);
        }
    }

    [Fact]
    public void EscapeAllBytesMatchesFrozenReferenceAcrossEveryExistingCarrier()
    {
        byte[] input = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        byte[] expected = Convert.FromHexString(AllBytesEscapedHex);
        foreach (string kind in new[] { "array", "B", "c", "slice", "readonly_B", "readonly_c" })
        {
            var carrier = Carrier(kind, Encoding.Latin1.GetString(input));
            try
            {
                object? result = Module("escape", carrier.Source);
                Assert.Equal(expected, Assert.IsType<PyBytes>(result).Bytes); Assert.NotSame(carrier.Source, result);
                if (carrier.View is not null) Assert.Equal(0, Exports(carrier.View));
            }
            finally { carrier.View?.Release(); }
            carrier.Owner.Append(33L);
        }
    }

    [Fact]
    public void EscapeReadsCurrentContentAndDoesNotRetainSourceOrExports()
    {
        var data = A("abc"); var first = Assert.IsType<PyBytes>(Module("escape", data)); data.SetItem(0L, 46L);
        Assert.Equal("abc", Text(first)); Assert.Equal(@"\.bc", Text(Module("escape", data)));
        Assert.Equal(0, Exports(data)); data.Append(35L);
        var view = View(data); Assert.Equal(@"\.bc\#", Text(Module("escape", view)));
        Assert.Equal(0, Exports(view)); view.Release(); data.Append(33L);
    }

    [Fact]
    public void EmptyEscapeStillChecksLivenessAndDoesNotClaimGlobalIdentity()
    {
        foreach (string kind in new[] { "array", "B", "c", "slice", "readonly_B", "readonly_c" })
        {
            var carrier = Carrier(kind, ""); object? output = Module("escape", carrier.Source);
            Assert.Empty(Assert.IsType<PyBytes>(output).Bytes); Assert.NotSame(carrier.Source, output);
            carrier.View?.Release(); carrier.Owner.Append(33L);
        }
        var dead = View(B("")); dead.Release();
        Assert.Equal("decoding to str: need a bytes-like object, memoryview found",
            Assert.Throws<PyTypeError>(() => Module("escape", dead)).PyMessage);
    }

    [Fact]
    public void ReleasedEscapeOwnErrorPreservesAmbientContextAndCallerFrame()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        var view = View(B("a")); view.Release();
        try
        {
            PyTraceback.PushFrame("buffer_escape.py", "caller"); PyTraceback.SetLine(31);
            var error = Assert.Throws<PyTypeError>(() => Module("escape", view));
            Assert.Same(ambient, error.Context); Assert.Same(ambient, PyErrors.Current);
            Assert.Equal(new PyStackFrame("buffer_escape.py", "caller", 31), Assert.Single(error.Frames));
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void ViewTemplateInputsStayExplicitlyUnsupportedAndExactPathsRemain()
    {
        var pattern = P("(a)"); var match = Match("(a)", B("a"));
        foreach (object owner in new object[] { A("X"), B("X"), A(@"\1"), B(@"\1") })
        {
            var view = View(owner);
            Assert.Throws<PyNotImplementedError>(() => Call(pattern, "sub", view, B("a")));
            Assert.Throws<PyNotImplementedError>(() => Call(match, "expand", view)); view.Release();
        }
        var exact = B("only"); Assert.Same(exact, Call(pattern, "sub", exact, B("a")));
        Assert.Equal("<a>", Text(Call(pattern, "sub", B(@"<\1>"), B("a"))));
        Assert.Equal(@"a\.b", Text(Module("escape", B("a.b")))); Assert.Equal(@"a\.b", Module("escape", "a.b"));
        Assert.Equal("sequence item 1: expected str instance, bytes found",
            Assert.Throws<PyTypeError>(() => Call(match, "expand", @"\1")).PyMessage);
    }

    [Fact]
    public void CountExceptionWinsTemplateParsingAndPreservesOriginalIdentity()
    {
        var template = A(@"\9"); var marker = new PyRuntimeError("count marker");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Call(P("a"), "sub", template, 3L, Index(() => throw marker))));
        Assert.Equal(0, Exports(template)); template.Append(33L);
    }

    // 原67行补参考X.escape_array.256的完整输出，不用被测算法生成golden。
    private const string AllBytesEscapedHex =
        "0001020304050607085c095c0a5c0b5c0c5c0d0e0f101112131415161718191a1b1c1d1e1f5c2021225c235c24255c26275c" +
        "285c295c2a5c2b2c5c2d5c2e2f303132333435363738393a3b3c3d3e5c3f404142434445464748494a4b4c4d4e4f50515253" +
        "5455565758595a5c5b5c5c5c5d5c5e5f606162636465666768696a6b6c6d6e6f707172737475767778797a5c7b5c7c5c7d5c" +
        "7e7f808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9fa0a1a2a3a4a5a6a7a8a9aaabacadaeaf" +
        "b0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecfd0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1" +
        "e2e3e4e5e6e7e8e9eaebecedeeeff0f1f2f3f4f5f6f7f8f9fafbfcfdfeff";
}
