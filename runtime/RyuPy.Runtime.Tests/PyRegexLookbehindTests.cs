// 固定码点宽度后顾的公开运行时专项：覆盖双域、Unicode、窗口、外围捕获和可变输入；不修改消费者。
using System.Text;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexLookbehindTests
{
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static object Domain(string value, bool bytes) => bytes ? B(value) : value;
    private static object? Call(PyNativeObject owner, string name, params object?[] args) =>
        PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static string Span(object? match) => PyRepr.Repr(Call(Assert.IsType<PyRegexMatch>(match), "span"));
    private static string Text(object? value) => value is PyBytes bytes ? Encoding.Latin1.GetString(bytes.Bytes) : Assert.IsType<string>(value);
    private static List<PyRegexMatch> Drain(object? value)
    {
        var iterator = Assert.IsAssignableFrom<IPyIterator>(value); var result = new List<PyRegexMatch>();
        while (iterator.MoveNext()) { Assert.True(result.Count < 100); result.Add(Assert.IsType<PyRegexMatch>(iterator.Current())); }
        Assert.False(iterator.MoveNext()); return result;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedBodiesAndNegativeAssertionsUseTheExistingConsumers(bool bytes)
    {
        foreach (string body in new[] { "ab", "(?:a)b", "(?:ab|cd)", "[ac][bd]", "[a-z]{2}", @"\w\w", @"\x61\142", ".." })
        {
            var yes = PyRegex.Compile(Domain("(?<=" + body + ")x", bytes));
            var no = PyRegex.Compile(Domain("(?<!" + body + ")x", bytes));
            Assert.Equal("(2, 3)", Span(Call(yes, "search", Domain("abx", bytes))));
            Assert.IsType<PyNone>(Call(no, "search", Domain("abx", bytes)));
            Assert.Equal("(0, 1)", Span(Call(no, "search", Domain("x", bytes))));
            Assert.IsType<PyNone>(Call(yes, "search", Domain("x", bytes)));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WindowCanSeeBeforePosButNeverConsumesBeyondEndpos(bool bytes)
    {
        object source = Domain("abx", bytes); var pattern = PyRegex.Compile(Domain("(?<=ab)x", bytes));
        foreach (string method in new[] { "search", "match", "fullmatch" })
        {
            var match = Assert.IsType<PyRegexMatch>(Call(pattern, method, source, 2L, 3L));
            Assert.Equal("(2, 3)", Span(match)); Assert.Same(source, match.GetAttr("string"));
            Assert.Equal(2L, match.GetAttr("pos")); Assert.Equal(3L, match.GetAttr("endpos"));
            Assert.IsType<PyNone>(Call(pattern, method, source, 2L, 2L));
            Assert.IsType<PyNone>(Call(pattern, method, source, 3L, 2L));
        }
        var empty = PyRegex.Compile(Domain("(?<=ab)", bytes));
        Assert.Equal("(2, 2)", Span(Call(empty, "search", source, 2L, 2L)));
        Assert.IsType<PyNone>(Call(empty, "search", source, 1L, 1L));
        Assert.Empty(Drain(Call(empty, "finditer", source, 3L, 2L)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroWidthAndSamePositionNonemptyRetryKeepPythonOrder(bool bytes)
    {
        foreach (string body in new[] { "", "a{0}", "(?:ab){0}", "(?:|)", "(?:){3}", "(?:a{0})*", "(?:a{0})+", "(?:a{0}){2,4}", "(?:a|bb){0}" })
        {
            Assert.Equal("(0, 0)", Span(Call(PyRegex.Compile(Domain("(?<=" + body + ")", bytes)), "fullmatch", Domain("", bytes))));
            Assert.IsType<PyNone>(Call(PyRegex.Compile(Domain("(?<!" + body + ")", bytes)), "search", Domain("", bytes)));
        }
        var pattern = PyRegex.Compile(Domain("(?<=a)|b", bytes)); object subject = Domain("ab", bytes);
        Assert.Equal(new[] { "(1, 1)", "(1, 2)" }, Drain(Call(pattern, "finditer", subject)).Select(value => Span(value)));
        var values = Assert.IsType<PyList>(Call(pattern, "findall", subject));
        Assert.Equal(new[] { "", "b" }, values.Items.Select(Text));
        Assert.Equal("aXX", Text(Call(pattern, "sub", Domain("X", bytes), subject)));
        var subn = Assert.IsType<PyTuple>(Call(pattern, "subn", Domain("X", bytes), subject));
        Assert.Equal("aXX", Text(subn.Items[0])); Assert.Equal(2L, subn.Items[1]);
        Assert.Equal(new[] { "a", "", "" }, Assert.IsType<PyList>(Call(pattern, "split", subject)).Items.Select(Text));
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("😀", "😀")]
    [InlineData("[a😀]", "😀")]
    [InlineData("[^z]", "😀")]
    [InlineData("(?:a|😀)", "😀")]
    [InlineData(@"\w", "𐐀")]
    [InlineData(@"\d", "𝟘")]
    public void WidthIsPythonCodepoints(string body, string prefix)
    {
        string subject = prefix + "x";
        Assert.Equal("(1, 2)", Span(Call(PyRegex.Compile("(?<=" + body + ")x"), "search", subject)));
        Assert.IsType<PyNone>(Call(PyRegex.Compile("(?<!" + body + ")x"), "search", subject));
        Assert.Equal("(2, 3)", Span(Call(PyRegex.Compile("(?<=(?:" + body + "){2})x"), "search", prefix + prefix + "x")));
    }

    [Fact]
    public void IsolatedSurrogatesAreConstructedInsideTheTestWithoutAttributeSerialization()
    {
        // xUnit的特性参数序列化会替换孤立代理；在方法内构造并核验真实码元后复用全部匹配断言。
        foreach (char codeUnit in new[] { (char)0xd800, (char)0xdc00 })
        {
            string prefix = new string(codeUnit, 1);
            Assert.Equal(1, prefix.Length); Assert.Equal(codeUnit, prefix[0]);
            foreach (string body in new[] { ".", "[" + prefix + "]" })
                WidthIsPythonCodepoints(body, prefix);
        }
    }

    [Fact]
    public void GlobalFlagsUseExistingSetTranslationsAndVerboseTokenization()
    {
        Assert.Equal("(1, 2)", Span(Call(PyRegex.Compile("(?i)(?<=k)x"), "search", "Kx")));
        Assert.IsType<PyNone>(Call(PyRegex.Compile("(?ai)(?<=k)x"), "search", "Kx"));
        Assert.Equal("(1, 2)", Span(Call(PyRegex.Compile("(?s)(?<=.)x"), "search", "\nx")));
        Assert.IsType<PyNone>(Call(PyRegex.Compile("(?<=.)x"), "search", "\nx"));
        Assert.Equal("(2, 3)", Span(Call(PyRegex.Compile("(?x)(?<= a # ignored | + ?\n b )x"), "search", "abx")));
        Assert.Equal("(1, 2)", Span(Call(PyRegex.Compile(B("(?i)(?<=[a-z])x")), "search", B("Ax"))));
        Assert.IsType<PyNone>(Call(PyRegex.Compile(B(@"(?<=\w)x")), "search", B("éx")));
        Assert.Equal("(1, 2)", Span(Call(PyRegex.Compile(B(@"(?<=\W)x")), "search", B("\xffx"))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoHiddenCapturesChangeOuterGroupsNamesOrExpansion(bool bytes)
    {
        var pattern = PyRegex.Compile(Domain("(a)(?<=a)(?P<tail>b)(c)?", bytes));
        var match = Assert.IsType<PyRegexMatch>(Call(pattern, "fullmatch", Domain("ab", bytes)));
        Assert.Equal(3L, pattern.GetAttr("groups"));
        // groupindex的只读映射仍是既有未支持项；捕获编号通过group、span和groupdict验收。
        Assert.Equal("re.Pattern.groupindex read-only mapping is not supported",
            Assert.Throws<PyNotImplementedError>(() => pattern.GetAttr("groupindex")).PyMessage);
        Assert.Equal("a", Text(Call(match, "group", 1L))); Assert.Equal("b", Text(Call(match, "group", "tail")));
        var groups = Assert.IsType<PyTuple>(Call(match, "groups")); Assert.Equal(3, groups.Items.Length);
        Assert.IsType<PyNone>(groups.Items[2]); Assert.Equal("b", Text(Assert.IsType<PyDict>(Call(match, "groupdict")).Get("tail")));
        Assert.Equal("(0, 1)", PyRepr.Repr(Call(match, "span", 1L))); Assert.Equal("(1, 2)", PyRepr.Repr(Call(match, "span", 2L)));
        Assert.Equal("(-1, -1)", PyRepr.Repr(Call(match, "span", 3L)));
        Assert.Equal("b-a-", Text(Call(match, "expand", Domain(@"\g<tail>-\1-\3", bytes))));
        // 后顾之外的既有捕获、引用和条件不能被后顾内部资格限制误伤。
        Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile(Domain(@"(a)(?<=a)\1(?(1)b|c)", bytes)), "fullmatch", Domain("aab", bytes)));
    }

    [Fact]
    public void MutableSubjectsKeepIdentityLiveGroupReadsAndFutureLookbehindReads()
    {
        var data = new PyByteArray(Encoding.Latin1.GetBytes("abxb"));
        var pattern = PyRegex.Compile(B("(?<=a)(b)"));
        var iterator = Assert.IsAssignableFrom<IPyIterator>(Call(pattern, "finditer", data));
        Assert.True(iterator.MoveNext()); var first = Assert.IsType<PyRegexMatch>(iterator.Current());
        Assert.Same(data, first.GetAttr("string")); Assert.Equal("(1, 2)", Span(first));
        var snapshot = Assert.IsType<PyBytes>(Call(first, "group"));
        data.SetItem(1L, 99L); data.SetItem(2L, 97L);
        Assert.Equal("c", Text(Call(first, "group"))); Assert.Equal("b", Text(snapshot));
        Assert.True(iterator.MoveNext()); Assert.Equal("(3, 4)", Span(iterator.Current()));
        Assert.False(iterator.MoveNext()); data.Append(33L);
        Assert.Throws<PyTypeError>(() => PyRegex.Compile(new PyByteArray(Encoding.Latin1.GetBytes("(?<=a)b"))));
    }
}
