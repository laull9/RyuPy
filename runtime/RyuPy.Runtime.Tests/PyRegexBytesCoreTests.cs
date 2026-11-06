// bytes正则阶段A内部专项：锁真实字节域、原对象、ASCII语义和byte偏移；未代替公开API源码验收。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexBytesCoreTests
{
    private static object Invoke(string name, object? owner, params object?[] args)
    {
        try
        {
            return typeof(PyRegexPattern).GetMethod(name, BindingFlags.NonPublic |
                (owner is null ? BindingFlags.Static : BindingFlags.Instance))!.Invoke(owner, args)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw;
        }
    }
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyRegexPattern Compile(PyBytes source, int flags = 0) => (PyRegexPattern)Invoke("CreateBytes", null, source, flags);
    private static PyRegexPattern Compile(string source, int flags = 0) => Compile(B(source), flags);
    private static object Apply(PyRegexPattern pattern, string method, PyBytes subject, long start = 0, long end = long.MaxValue) =>
        Invoke("ApplyBytes", pattern, method, subject, start, end);
    private static object? Call(PyRegexMatch match, string name, params object?[] args) => PyOps.CallValue(match.GetAttr(name), args, [], []);
    private static PyRegexMatch Match(PyRegexPattern pattern, PyBytes subject, string method = "search", long start = 0, long end = long.MaxValue) =>
        Assert.IsType<PyRegexMatch>(Apply(pattern, method, subject, start, end));
    private static byte[] Flat(object value) => Assert.IsType<PyList>(value).Items
        .Select(item => Assert.Single(Assert.IsType<PyBytes>(item).Bytes)).ToArray();
    private static List<PyRegexMatch> Drain(object value)
    {
        var iterator = Assert.IsAssignableFrom<IPyIterator>(value); var result = new List<PyRegexMatch>();
        while (iterator.MoveNext())
        {
            Assert.True(result.Count < 300); result.Add(Assert.IsType<PyRegexMatch>(iterator.Current()));
        }
        Assert.False(iterator.MoveNext()); return result;
    }

    [Fact]
    public void PublishedPatternKeepsOriginalBytesAndExposesCompleteConsumers()
    {
        PyBytes source = B("(?P<body>.+)"); var pattern = Compile(source, 16);
        Assert.Same(source, pattern.GetAttr("pattern")); Assert.Equal(16L, pattern.GetAttr("flags"));
        Assert.Equal(1L, pattern.GetAttr("groups"));
        Assert.Equal("re.compile(b'(?P<body>.+)', re.DOTALL)", pattern.PythonRepr());
        Assert.Equal("re.compile(b'a', re.IGNORECASE|re.MULTILINE)", Compile("a", 10).PythonRepr());
        Assert.Equal("re.compile(b'a')", Compile("a").PythonRepr());
        Assert.True(Assert.IsType<PyNativeType>(PyRegex.CreateAttributes()["Pattern"]).Accepts(pattern));
        Assert.Same(source, PyRegex.Compile(source).GetAttr("pattern"));
        Assert.IsType<PyRegexMatch>(PyOps.CallValue(pattern.GetAttr("search"), [B("a")], [], []));
        Assert.Equal(B("a").Bytes, Assert.IsType<PyBytes>(Call(Match(pattern, B("a")), "expand", B(@"\1"))).Bytes);
    }

    [Fact]
    public void AllByteCategoriesUseAsciiWithoutInventingTheAsciiFlag()
    {
        var alphabet = new PyBytes(Enumerable.Range(0, 256).Select(value => (byte)value).ToArray());
        foreach (char category in "wd s".Replace(" ", ""))
        foreach (int flags in new[] { 0, 256, 2 })
        {
            bool Included(byte value) => category switch
            {
                'w' => value is >= 65 and <= 90 or >= 97 and <= 122 or >= 48 and <= 57 or 95,
                'd' => value is >= 48 and <= 57,
                _ => value is >= 9 and <= 13 or 32,
            };
            var positive = Compile("\\" + category, flags);
            var negative = Compile("\\" + char.ToUpperInvariant(category), flags);
            Assert.Equal((long)flags, positive.GetAttr("flags"));
            Assert.Equal(alphabet.Bytes.Where(Included), Flat(Apply(positive, "findall", alphabet)));
            Assert.Equal(alphabet.Bytes.Where(value => !Included(value)), Flat(Apply(negative, "findall", alphabet)));
        }
    }

    [Fact]
    public void IgnoreCaseExpandsAsciiRangesButNeverFoldsHighBytes()
    {
        PyBytes subject = B("AZaz\xc0\xe0\xff");
        Assert.Equal(B("AZaz").Bytes, Flat(Apply(Compile("[a-z]", 2), "findall", subject)));
        Assert.Equal(B("\xc0\xe0\xff").Bytes, Flat(Apply(Compile("[^a-z]", 2), "findall", subject)));
        Assert.Equal(B("\xc0").Bytes, Flat(Apply(Compile("\xc0", 2), "findall", subject)));
        Assert.Equal(B("\xe0").Bytes, Flat(Apply(Compile("\xe0", 2), "findall", subject)));
        Assert.Equal(B("A[\\]^_`z").Bytes, Flat(Apply(Compile("[A-z]", 2), "findall", B("A[\\]^_`z\xff"))));
    }

    [Fact]
    public void BinarySubjectsAndWholeGroupsPreserveOriginalIdentity()
    {
        var subject = new PyBytes([0, 65, 128, 255]); var pattern = Compile("(?P<body>.+)", 16);
        PyRegexMatch match = Match(pattern, subject, "fullmatch");
        Assert.Same(pattern, match.GetAttr("re")); Assert.Same(subject, match.GetAttr("string"));
        Assert.Same(subject, Call(match, "group")); Assert.Same(subject, Call(match, "group", 1L));
        Assert.Same(subject, Assert.Single(Assert.IsType<PyTuple>(Call(match, "groups")).Items));
        Assert.Same(subject, Assert.IsType<PyDict>(Call(match, "groupdict")).Get("body"));
        Assert.Equal("(0, 4)", PyRepr.Repr(Call(match, "span"))); Assert.Equal(0L, match.GetAttr("pos"));
        Assert.Equal(4L, match.GetAttr("endpos"));
        Assert.Equal("<re.Match object; span=(0, 4), match=b'\\x00A\\x80\\xff'>", match.PythonRepr());
        Assert.True(Assert.IsType<PyNativeType>(PyRegex.CreateAttributes()["Match"]).Accepts(match));
        Assert.Same(subject, Assert.Single(Assert.IsType<PyList>(Apply(pattern, "findall", subject)).Items));
        Assert.Equal(subject.Bytes, Flat(Apply(Compile(".", 16), "findall", subject)));
    }

    [Fact]
    public void MissingGroupsDefaultIdentityAndByteSpansRemainDistinct()
    {
        PyBytes subject = B("zbz"); PyRegexMatch match = Match(Compile("(?P<x>a)?(b)"), subject);
        Assert.Equal("(b'b', None, b'b')", PyRepr.Repr(Call(match, "group", 0L, 1L, 2L)));
        Assert.Equal("(None, b'b')", PyRepr.Repr(Call(match, "groups")));
        Assert.Equal("{'x': None}", PyRepr.Repr(Call(match, "groupdict")));
        object missing = new PyList();
        Assert.Same(missing, Assert.IsType<PyTuple>(Call(match, "groups", missing)).Items[0]);
        Assert.Same(missing, Assert.IsType<PyDict>(Call(match, "groupdict", missing)).Get("x"));
        Assert.Equal("(1, 2)", PyRepr.Repr(Call(match, "span")));
        Assert.Equal("(-1, -1)", PyRepr.Repr(Call(match, "span", 1L)));
        Assert.Equal("no such group", Assert.Throws<PyIndexError>(() => Call(match, "group", 3L)).PyMessage);
        Assert.Equal("no such group", Assert.Throws<PyIndexError>(() => Call(match, "group", "absent")).PyMessage);
        PyBytes part = Assert.IsType<PyBytes>(Call(match, "group")); Assert.NotSame(subject, part);
    }

    [Fact]
    public void FindallReturnsBytesOrTuplesAndUsesEmptyBytesForMissingGroups()
    {
        PyBytes subject = B("b ab");
        Assert.Equal("[b'b', b'ab']", PyRepr.Repr(Apply(Compile("a?b"), "findall", subject)));
        Assert.Equal("[b'', b'a']", PyRepr.Repr(Apply(Compile("(a)?b"), "findall", subject)));
        Assert.Equal("[(b'', b'b'), (b'a', b'b')]", PyRepr.Repr(Apply(Compile("(a)?(b)"), "findall", subject)));
        Assert.IsType<PyNone>(Apply(Compile("Z"), "search", subject));
        Assert.IsType<PyNone>(Apply(Compile("b"), "match", B("ab")));
    }

    [Fact]
    public void BoundsAndAnchorsUseByteIndicesWithoutUtf16Mapping()
    {
        var subject = new PyBytes([0, 65, 128, 255]); var pattern = Compile(".", 16);
        foreach (string method in new[] { "match", "search", "fullmatch" })
        {
            PyRegexMatch match = Match(pattern, subject, method, 2, 3);
            Assert.Equal(new byte[] { 128 }, Assert.IsType<PyBytes>(Call(match, "group")).Bytes);
            Assert.Equal("(2, 3)", PyRepr.Repr(Call(match, "span")));
            Assert.Equal(2L, match.GetAttr("pos")); Assert.Equal(3L, match.GetAttr("endpos"));
        }
        Assert.IsType<PyNone>(Apply(Compile("^A"), "search", subject, 1, 2));
        Assert.Equal("(1, 2)", PyRepr.Repr(Call(Match(Compile("A$"), subject, "search", 1, 2), "span")));
        Assert.Empty(Drain(Apply(Compile("a*"), "finditer", B("aa"), 2, 1)));
        var huge = Drain(Apply(Compile("a*"), "finditer", B("aa"), 1L << 40, 1L << 40));
        Assert.Equal("(2, 2)", PyRepr.Repr(Call(Assert.Single(huge), "span")));
        Assert.Equal(2, Drain(Apply(Compile("a*"), "finditer", B("aa"), -2, 1)).Count);
    }

    [Fact]
    public void EmptyMatchRetryAndBoundaryProgressUseTheExistingPythonRules()
    {
        foreach (string expression in new[] { ".*?", "|a" })
        {
            List<PyRegexMatch> values = Drain(Apply(Compile(expression), "finditer", B("aa")));
            Assert.Equal<string>(["(0, 0)", "(0, 1)", "(1, 1)", "(1, 2)", "(2, 2)"],
                values.Select(value => PyRepr.Repr(Call(value, "span"))));
        }
        Assert.Equal(2, Drain(Apply(Compile("a*"), "finditer", B("aa"))).Count);
        PyBytes subject = new([255, 97, 95, 128]);
        Assert.Equal<string>(["(1, 1)", "(3, 3)"], Drain(Apply(Compile(@"\b"), "finditer", subject))
            .Select(value => PyRepr.Repr(Call(value, "span"))));
        Assert.Equal<string>(["(0, 0)", "(2, 2)", "(4, 4)"], Drain(Apply(Compile(@"\B"), "finditer", subject))
            .Select(value => PyRepr.Repr(Call(value, "span"))));
        Assert.Empty(Drain(Apply(Compile(@"\B"), "finditer", B(""))));
    }

    [Fact]
    public void BytesFlagsArePubliclyExactAndInvalidFlagsKeepTheirOwnErrors()
    {
        foreach (int flag in new[] { 0, 2, 256, 88 }) Assert.Equal((long)flag, Compile("a", flag).GetAttr("flags"));
        Assert.Equal(2L, Compile("(?i)a").GetAttr("flags")); Assert.Equal(256L, Compile("(?a)a").GetAttr("flags"));
        foreach (int flag in new[] { 32, 288 })
            Assert.Equal("cannot use UNICODE flag with a bytes pattern", Assert.Throws<PyValueError>(() => Compile("a", flag)).PyMessage);
        Assert.Equal("ASCII and LOCALE flags are incompatible", Assert.Throws<PyValueError>(() => Compile("a", 260)).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Compile("a", 4));
        // 此交叉先后是固定CP源码推断，非新增参考进程的结果。
        Assert.IsType<PyRegexError>(Assert.Throws<PyRegexError>(() => Compile("[", 4)));
        var inline = Assert.Throws<PyRegexError>(() => Compile("(?u)a"));
        Assert.Equal("bad inline flags: cannot use 'u' flag with a bytes pattern at position 3", inline.PyMessage);
        Assert.Equal(3L, inline.Position);
    }

    [Fact]
    public void InvalidBytesEscapesAndNamesPreserveOriginalErrorMetadata()
    {
        foreach (string expression in new[] { @"\u0061", @"\U00000061", @"\N{LATIN SMALL LETTER A}",
            @"[\u0061]", @"[\U00000061]", @"[\N{LATIN SMALL LETTER A}]", @"\q", @"\xG0" })
        {
            PyBytes source = B(expression); var error = Assert.Throws<PyRegexError>(() => Compile(source));
            Assert.Same(source, error.Pattern); Assert.Equal(expression[0] == '[' ? 1L : 0L, error.Position);
            Assert.Equal(1L, error.LineNumber); Assert.Equal(expression[0] == '[' ? 2L : 1L, error.ColumnNumber);
        }
        var named = Assert.Throws<PyRegexError>(() => Compile("(?P<\xe9>a)"));
        Assert.Equal("bad character in group name '\\xe9' at position 4", named.PyMessage);
        PyBytes multiline = B("\xff\n["); var located = Assert.Throws<PyRegexError>(() => Compile(multiline));
        Assert.Same(multiline, located.Pattern); Assert.Equal(2L, located.Position);
        Assert.Equal(2L, located.LineNumber); Assert.Equal(1L, located.ColumnNumber);
    }

    [Fact]
    public void ExistingSyntaxLimitsRemainExplicitAndTextMatchingIsUnchanged()
    {
        Assert.Equal(B("aa").Bytes, Assert.IsType<PyBytes>(Call(Match(Compile(@"(a)\1"), B("aa")), "group")).Bytes);
        foreach (string expression in new[] { @"(a)\1", "(?i:a)", "(?<=a)b", "a++" })
            Assert.Throws<PyNotImplementedError>(() => Compile(expression, expression == @"(a)\1" ? 2 : 0));
        var text = PyRegex.Compile("[a-z]+", 2L);
        Assert.Equal(34L, text.GetAttr("flags"));
        Assert.Equal("['aİıſK']", PyRepr.Repr(PyOps.CallValue(text.GetAttr("findall"), ["aİıſK"], [], [])));
        var scalar = Assert.IsType<PyRegexMatch>(PyOps.CallValue(PyRegex.Compile("猫").GetAttr("search"), ["😀猫"], [], []));
        Assert.Equal("(1, 2)", PyRepr.Repr(Call(scalar, "span"))); Assert.Equal("猫", Call(scalar, "group"));
    }

    [Fact]
    public void InternalOwnErrorsKeepTheCallerFrameAndAmbientContext()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("bytes_regex_probe.py", "caller"); PyTraceback.SetLine(23);
            var error = Assert.Throws<PyRegexError>(() => Compile(@"\u0061"));
            Assert.Same(ambient, error.Context); Assert.Equal("caller", Assert.Single(error.Frames).Function);
            Assert.Equal(23, error.Frames[0].Line); Assert.Same(ambient, PyErrors.Current);
            Assert.Equal(1L, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }
}
