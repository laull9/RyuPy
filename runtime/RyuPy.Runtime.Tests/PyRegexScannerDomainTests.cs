// Scanner双域与flags/Match合同：根执行flags不随phrase inline变化，文本跨度按码点。
using System;
using System.Collections.Generic;
using System.Text;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed partial class PyRegexScannerTests
{
    private static PyBytes Bytes(string source) => new(Encoding.Latin1.GetBytes(source));

    [Fact]
    public void MixedPhraseSourcesCanScanTextAndBytesWithOnePlan()
    {
        PyClass root = Root(Module()); var scanner = New(root, Lex(Pair("a", Echo()), Pair(Bytes("b"), Echo())));
        object? plan = PyObject.GetAttr(scanner, "scanner");
        Assert.Equal("(['a', 'b'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
        Assert.Equal("([b'a', b'b'], b'!')", PyRepr.Repr(Scan(scanner, Bytes("ab!"))));
        Assert.Same(plan, PyObject.GetAttr(scanner, "scanner"));
        var match = Assert.IsType<PyRegexMatch>(PyObject.GetAttr(scanner, "match"));
        Assert.IsType<PyBytes>(match.GetAttr("string")); Assert.Equal(0L, match.GetAttr("pos"));
        Assert.Equal(3L, match.GetAttr("endpos")); Assert.Equal("(1, 2)", PyRepr.Repr(Method(match, "span")));
    }

    [Fact]
    public void DefaultAsciiCategoriesAndRootUnicodeAreSeparate()
    {
        PyClass root = Root(Module());
        Assert.Equal("([], 'é_a')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"\w+", "word"))), "é_a")));
        Assert.Equal("(['word'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"\w+", "word")), 32L), "é_a")));
        Assert.Equal("([], 'é')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"(?u)\w+", "word"))), "é")));
        Assert.Equal("([], 'Kk')", PyRepr.Repr(Scan(New(root, Lex(Pair("k+", "K")), 2L), "Kk")));
        Assert.Equal("(['K'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair("k+", "K")), 34L), "Kk")));
        Assert.Equal("(['K'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair("k+", "K")), 2L), "Kk")));
        Assert.Equal("([], '²')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"\d", "digit"))), "²")));
        Assert.Equal("([], '\\x1c')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"\s", "space"))), "\x1c")));
        Assert.Equal("(['space'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"\s", "space")), 32L), "\x1c")));
    }

    [Fact]
    public void InlineFlagsAffectLexingButNotRootCaseDotOrMultiline()
    {
        PyClass root = Root(Module());
        Assert.Equal("([], 'Aa')", PyRepr.Repr(Scan(New(root, Lex(Pair("(?i)a", "A"))), "Aa")));
        Assert.Equal("(['A'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair("(?i)a", "A"))), "a")));
        Assert.Equal("(['AB'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair("(?x)a # comment\n b", "AB"))), "ab!")));
        Assert.Equal("(['space'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair(@"(?x)a\ b", "space"))), "a b!")));
        Assert.Equal("(['class'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair("(?x)[ ]", "class"))), " !")));
        Assert.Equal("(['A'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair("(?x) # lead\n (?i)a", "A"))), "a!")));
        Assert.Equal("([], 'A')", PyRepr.Repr(Scan(New(root, Lex(Pair("  (?i)a", "A")), 64L), "A")));
        Assert.Equal("([], '\\n')", PyRepr.Repr(Scan(New(root, Lex(Pair("(?s).", "dot"))), "\n")));
        Assert.Equal("(['dot'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair(".", "dot")), 16L), "\n")));
        Assert.Equal("(['a', '\\n'], 'a')", PyRepr.Repr(Scan(New(root,
            Lex(Pair("(?m)^a", Echo()), Pair("\\n", Echo()))), "a\na")));
    }

    [Fact]
    public void WideLiteralsDotAndNegatedSetsDoNotBecomeAsciiOnly()
    {
        PyClass root = Root(Module());
        var scanner = New(root, Lex(Pair("😀", Echo()), Pair("[^!]", Echo())));
        string input = "😀é!";
        Assert.Equal("(['😀', 'é'], '!')", PyRepr.Repr(Scan(scanner, input)));
        var match = Assert.IsType<PyRegexMatch>(PyObject.GetAttr(scanner, "match"));
        Assert.Same(input, match.GetAttr("string"));
        Assert.Equal("(1, 2)", PyRepr.Repr(Method(match, "span"))); Assert.Equal(3L, match.GetAttr("endpos"));
        Assert.Equal("(['😀'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair(".(?=!)", Echo()))), "😀!")));
        Assert.Equal("([], b'x')", PyRepr.Repr(Scan(New(root, Lex(Pair("😀", Echo()))), Bytes("x"))));
        Assert.Equal("([b'\\xe9'], b'!')", PyRepr.Repr(Scan(New(root, Lex(Pair("é", Echo())), 32L), Bytes("é!"))));
    }

    [Fact]
    public void MatchUsesRealPatternIdentityWrapperGroupsAndWholeBounds()
    {
        var module = Module(); PyClass root = Root(module); var saved = new List<PyRegexMatch>();
        var callback = Fn("save", ["scanner", "token"], args =>
        { saved.Add(Assert.IsType<PyRegexMatch>(PyObject.GetAttr(args[0], "match"))); return args[1]; });
        var scanner = New(root, Lex(Pair("a", callback), Pair("😀", callback)));
        object? plan = PyObject.GetAttr(scanner, "scanner"); string input = "a😀!";
        Assert.Equal("(['a', '😀'], '!')", PyRepr.Repr(Scan(scanner, input)));
        Assert.Same(module["Pattern"], PyObject.TypeOf(plan));
        Assert.True(Assert.IsType<PyNativeType>(module["Pattern"]).Accepts(plan));
        Assert.Same(module["Match"], PyObject.TypeOf(saved[0]));
        Assert.True(Assert.IsType<PyNativeType>(module["Match"]).Accepts(saved[0]));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(plan, "pattern"));
        Assert.Equal(0L, PyObject.GetAttr(plan, "flags")); Assert.Equal(2L, PyObject.GetAttr(plan, "groups"));
        Assert.All(saved, m =>
        { Assert.Same(plan, m.GetAttr("re")); Assert.Same(input, m.GetAttr("string")); Assert.Equal(0L, m.GetAttr("pos")); Assert.Equal(3L, m.GetAttr("endpos")); });
        Assert.Equal("('a', None)", PyRepr.Repr(Method(saved[0], "groups")));
        Assert.Equal("(None, '😀')", PyRepr.Repr(Method(saved[1], "groups")));
        Assert.Equal("('missing', '😀')", PyRepr.Repr(Method(saved[1], "groups", "missing")));
        Assert.Equal("{}", PyRepr.Repr(Method(saved[1], "groupdict")));
        Assert.Equal(1L, saved[0].GetAttr("lastindex")); Assert.Equal(2L, saved[1].GetAttr("lastindex"));
        Assert.Same(PyNone.Instance, saved[0].GetAttr("lastgroup"));
        Assert.Equal("((0, 1), (0, 1), (-1, -1))", PyRepr.Repr(saved[0].GetAttr("regs")));
        Assert.Equal("((1, 2), (-1, -1), (1, 2))", PyRepr.Repr(saved[1].GetAttr("regs")));
        Assert.Equal("<re.Match object; span=(1, 2), match='😀'>", PyRepr.Repr(saved[1]));
        Assert.Throws<PyIndexError>(() => Method(saved[1], "group", "ryu2"));
        Assert.Throws<PyIndexError>(() => Method(saved[1], "group", 3L));
        Assert.Equal("(-1, -1)", PyRepr.Repr(Method(saved[1], "span", 1L)));
        Assert.Equal("('😀', None, '😀')", PyRepr.Repr(Method(saved[1], "group", 0L, 1L, 2L)));
    }

    [Fact]
    public void EmptyLexiconFailsAtFinalCompileWithoutPublishingFields()
    {
        PyClass root = Root(Module()); var scanner = new PyInstance(root); PyList empty = Lex();
        var error = Assert.Throws<PyRuntimeError>(() => Call(PyObject.GetAttr(root, "__init__"), scanner, empty));
        Assert.Equal("invalid SRE code", error.PyMessage);
        Assert.Same(empty, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "scanner"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "match"));
        // 非空计划的无匹配路径仍须验证subject，不能因预期空结果绕过输入域。
        scanner = New(root, Lex(Pair("a", Echo())));
        Assert.Equal("([], '!')", PyRepr.Repr(Scan(scanner, "!")));
        Assert.Equal("([], b'!')", PyRepr.Repr(Scan(scanner, Bytes("!"))));
        Assert.Throws<PyTypeError>(() => Scan(scanner, 42L));
        Assert.Throws<PyNotImplementedError>(() => Scan(scanner, new PyByteArray([97])));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "match"));
    }

    [Fact]
    public void OrdinaryRegexMetadataAndGlobalInlineSemanticsStayUnchanged()
    {
        var ordinary = PyRegex.Compile("(?i)(a)");
        var match = Assert.IsType<PyRegexMatch>(Method(ordinary, "match", "A"));
        Assert.Equal("A", Method(match, "group", 1L)); Assert.Equal(34L, ordinary.GetAttr("flags"));
        Assert.Throws<PyAttributeError>(() => match.GetAttr("lastindex"));
        Assert.Throws<PyAttributeError>(() => match.GetAttr("regs"));
        Assert.Throws<PyNotImplementedError>(() => ordinary.GetAttr("groupindex"));
        Assert.Equal("A", Method(match, "expand", @"\1"));
        Assert.IsType<PyRegexMatch>(Method(PyRegex.Compile("(?<=a)b"), "search", "ab"));
        Assert.Equal("é", Method(Assert.IsType<PyRegexMatch>(Method(PyRegex.Compile(@"\w"), "match", "é")), "group"));
    }
}
