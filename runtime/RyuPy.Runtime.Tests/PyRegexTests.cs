using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>固定Python差分对应的正则风险回归，不以.NET原有行为作为期望。</summary>
public class PyRegexTests
{
    private static object? Call(PyNativeObject receiver, string method, params object?[] args) =>
        PyOps.CallValue(receiver.GetAttr(method), args, [], []);

    [Fact]
    public void UnicodeClassesAndScalarSpansFollowPython()
    {
        Assert.Equal("['a', '²猫𝟙']", PyRepr.Repr(Call(PyRegex.Compile(@"\w+"), "findall", "a\u0301\u203f²猫𝟙")));
        Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile(@"\s"), "fullmatch", "\x1c"));
        var match = Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile("猫"), "search", "😀猫"));
        Assert.Equal("(1, 2)", PyRepr.Repr(Call(match, "span")));
        Assert.Equal("['😀', '猫']", PyRepr.Repr(Call(PyRegex.Compile("."), "findall", "😀猫")));
        Assert.Equal("['😀😀']", PyRepr.Repr(Call(PyRegex.Compile(".+"), "findall", "😀😀")));
        Assert.Equal("['x']", PyRepr.Repr(Call(PyRegex.Compile("[^😀]"), "findall", "😀x")));
    }

    [Fact]
    public void IgnoreCaseUsesPythonUnicodeEquivalenceNotHostCulture()
    {
        foreach (string value in new[] { "İ", "ı", "ſ", "K" })
            Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile("[a-z]", 2L), "fullmatch", value));
        Assert.Equal("['Cat']", PyRepr.Repr(Call(PyRegex.Compile("[a-z]+", 258L), "findall", "ſCat")));
        Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile("i", 2L), "fullmatch", "ı"));
    }

    [Fact]
    public void GroupNumbersStayInSourceOrderAndReplacementIsPythonSyntax()
    {
        var pattern = PyRegex.Compile(@"(?P<first>a)(b)(?P<last>c)");
        var match = Assert.IsType<PyRegexMatch>(Call(pattern, "match", "abc"));
        Assert.Equal("('a', 'b', 'c')", PyRepr.Repr(Call(match, "group", 1L, 2L, 3L)));
        Assert.Equal("{'first': 'a', 'last': 'c'}", PyRepr.Repr(Call(match, "groupdict")));
        Assert.Equal("c-a-b", Call(match, "expand", @"\3-\g<first>-\2"));
        Assert.Equal("b-a b-a", Call(PyRegex.Compile(@"(?P<first>a)(b)"), "sub", @"\2-\g<first>", "ab ab"));
        Assert.Equal("$1", Call(PyRegex.Compile("a"), "sub", "$1", "a"));
        Assert.Throws<PyRegexError>(() => Call(pattern, "sub", @"\9", "no match"));
    }

    [Theory]
    [InlineData(".*?", "['', 'a', '', 'a', '']", "-----")]
    [InlineData("|a", "['', 'a', '', 'a', '']", "-----")]
    [InlineData("a*", "['aa', '']", "--")]
    [InlineData("(?=a)", "['', '']", "-a-a")]
    public void EmptyMatchRetriesNonemptyAtSamePosition(string expression, string all, string substituted)
    {
        var pattern = PyRegex.Compile(expression);
        Assert.Equal(all, PyRepr.Repr(Call(pattern, "findall", "aa")));
        Assert.Equal(substituted, Call(pattern, "sub", "-", "aa"));
    }

    [Fact]
    public void AnchorsAndFullmatchRespectEndPosition()
    {
        Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile("a$"), "search", "a\n"));
        Assert.IsType<PyNone>(Call(PyRegex.Compile("a$"), "fullmatch", "a\n"));
        Assert.Equal("['a', 'a']", PyRepr.Repr(Call(PyRegex.Compile("^a", 8L), "findall", "a\na")));
        var match = Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile("a+"), "fullmatch", "zaab", 1L, 3L));
        Assert.Equal("(1, 3)", PyRepr.Repr(Call(match, "span")));
        Assert.Equal("['', 'a']", PyRepr.Repr(Call(PyRegex.Compile("(a)?b"), "findall", "b ab")));
    }

    [Fact]
    public void InvalidPatternsAndUnsupportedConstructsDoNotLeakDotNet()
    {
        Assert.Equal(0L, Assert.Throws<PyRegexError>(() => PyRegex.Compile("[")).Position);
        Assert.Equal(0L, Assert.Throws<PyRegexError>(() => PyRegex.Compile("[abc")).Position);
        Assert.Equal(1L, Assert.Throws<PyRegexError>(() => PyRegex.Compile("a(bc")).Position);
        Assert.Throws<PyRegexError>(() => PyRegex.Compile(@"\p{L}"));
        Assert.Throws<PyRegexError>(() => PyRegex.Compile("(?<name>a)"));
        Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile("(?<=a)b"));
        Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile("a++"));
        Assert.Throws<PyValueError>(() => PyRegex.Compile("a", 4L));
        Assert.Throws<PyValueError>(() => PyRegex.Compile("a", 288L));
        Assert.Same(PyRegex.Compile("cache"), PyRegex.Compile("cache"));
    }
    [Fact]
    public void ErrorConstructionRetainsOptionalMetadataAndRealExceptionType()
    {
        var plain = PyRegex.ConstructError(["bad"], [], []);
        Assert.Equal("bad", plain.MessageText);
        Assert.IsType<PyNone>(plain.Pattern);
        Assert.IsType<PyNone>(plain.Position);
        Assert.Equal("('bad',)", PyRepr.Repr(new PyTuple(plain.Args)));
        var located = PyRegex.ConstructError([], ["msg", "pattern", "pos"], ["bad", "a\nb", 2L]);
        Assert.Equal(2L, located.LineNumber);
        Assert.Equal(1L, located.ColumnNumber);
        Assert.Equal("bad at position 2 (line 2, column 1)", located.PyMessage);
        Assert.Throws<PyTypeError>(() => PyRegex.ConstructError([], [], []));
        Assert.Throws<PyTypeError>(() => PyRegex.ConstructError(["bad"], ["msg"], ["twice"]));
    }

    [Fact]
    public void ReplacementMustBeValidEvenWhenNoMatchExists()
    {
        var pattern = PyRegex.Compile("x");
        Assert.Throws<PyTypeError>(() => Call(pattern, "sub", 1L, "no"));
        Assert.Throws<PyRegexError>(() => Call(pattern, "sub", @"\9", "no"));
        Assert.Throws<PyIndexError>(() => Call(pattern, "sub", @"\g<absent>", "no"));
    }

    [Fact]
    public void NoneReplacementCountsMatchesAndPreservesCallbackOrder()
    {
        var seen = new List<object?>();
        var callback = PyNativeFunctions.Create("erase", ["match"], [null], 1, args =>
        {
            seen.Add(Call((PyRegexMatch)args[0]!, "span"));
            return PyNone.Instance;
        });
        Assert.Equal("('X', 2)", PyRepr.Repr(Call(PyRegex.Compile("a"), "subn", callback, "aXa")));
        Assert.Equal("[(0, 1), (2, 3)]", PyRepr.Repr(new PyList(seen)));
    }

    [Fact]
    public void NestedPatternLimitRaisesPythonErrorBeforeHostStackExhaustion()
    {
        Assert.Throws<PyRecursionError>(() => PyRegex.Compile(new string('(', 257) + "a" + new string(')', 257)));
        Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(new string('a', 16385)));
    }

    [Fact]
    public void CountLimitDoesNotScanPotentiallyExpensiveTail()
    {
        string tail = new string('a', 80) + "!";
        Assert.Equal("-" + tail, Call(PyRegex.Compile("x|(a+)+$"), "sub", "-", "x" + tail, 1L));
        Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile("(?P<x>a)").GetAttr("groupindex"));
    }

}
