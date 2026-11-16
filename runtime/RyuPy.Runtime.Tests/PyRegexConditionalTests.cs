// 条件组经过真实PyRegex parser/公开消费者：固定CP捕获/错误，另锁Ryu明确拒绝与资源边界。
using System.Text;
using System.Text.Json.Nodes;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed partial class PyRegexConditionalTests
{
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static object? Call(PyNativeObject owner, string name, params object?[] args) =>
        PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static object Domain(string value, bool bytes) => bytes ? B(value) : value;
    private static JsonNode? JsonValueOf(object? value)
    {
        switch (value)
        {
            case null or PyNone: return null;
            case string text: return JsonValue.Create(text);
            case long number: return JsonValue.Create(number);
            case PyBytes bytes:
                return new JsonObject { ["bytes_hex"] = Convert.ToHexString(bytes.Bytes).ToLowerInvariant() };
            case PyTuple tuple:
                return new JsonArray(tuple.Items.Select(JsonValueOf).ToArray());
            case PyDict dictionary:
                var result = new JsonObject();
                foreach (var entry in dictionary.Entries()) result[Assert.IsType<string>(entry.Key)] = JsonValueOf(entry.Value);
                return result;
            default: throw new InvalidOperationException("不应出现的捕获快照类型：" + value.GetType());
        }
    }
    private static JsonObject Snapshot(PyRegexMatch match, int groups) => new()
    {
        ["group0"] = JsonValueOf(Call(match, "group")),
        ["groups"] = JsonValueOf(Call(match, "groups")),
        ["groupdict"] = JsonValueOf(Call(match, "groupdict")),
        ["spans"] = new JsonArray(Enumerable.Range(0, groups + 1)
            .Select(number => JsonValueOf(Call(match, "span", (long)number))).ToArray()),
    };

    [Theory]
    [MemberData(nameof(MatchCases))]
    public void FixedPythonMatchesKeepAllCapturesAndSpans(string label, bool bytes, string source,
        string subject, long flags, int groups, string expectedJson)
    {
        object input = Domain(subject, bytes);
        PyRegexPattern compiled = PyRegex.Compile(Domain(source, bytes), flags);
        Assert.Equal((long)groups, compiled.GetAttr("groups"));
        object? found = Call(compiled, "fullmatch", input);
        JsonNode? actual = null;
        if (found is not PyNone)
        {
            var match = Assert.IsType<PyRegexMatch>(found);
            Assert.Same(compiled, match.GetAttr("re")); Assert.Same(input, match.GetAttr("string"));
            actual = Snapshot(match, groups);
        }
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expectedJson), actual),
            label + (bytes ? "/bytes" : "/text") + " expected=" + expectedJson + " actual=" + actual?.ToJsonString());
    }

    [Theory]
    [MemberData(nameof(ErrorCases))]
    public void FixedPythonSyntaxErrorsKeepOriginalDomainAndLocation(string label, bool bytes,
        string source, long flags, string message, long position, long line, long column, string display)
    {
        object original = Domain(source, bytes);
        var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(original, flags));
        Assert.Equal("error", error.PyTypeName); Assert.Same(original, error.Pattern);
        Assert.Equal(message, error.MessageText); Assert.Equal(position, error.Position);
        Assert.Equal(line, error.LineNumber); Assert.Equal(column, error.ColumnNumber);
        Assert.True(error.PyMessage == display, label + " expected=" + display + " actual=" + error.PyMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpenSelfAndAncestorConditionsAreExplicitlyRejected(bool bytes)
    {
        foreach (string source in new[] { "(a(?(1)b|c))", "((a)(?(1)b|c))",
            "(?:(?P<n>a(?(n)b|c))d){2}", "(?:x(a(?(1)b|c))){2}", "(a(?(1)b|c)){2}" })
            Assert.Equal("re: unsupported conditional reference to an open group",
                Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain(source, bytes))).PyMessage);
    }

    [Fact]
    public void TextNonAsciiIdentifierTokensKeepTheirDeclaredBoundary()
    {
        foreach (string source in new[] { "(?(µ)a|b)", "()(?(१)a|b)", "()(?(１)a|b)", "()(?(©)a|b)",
            "(?P<µ>a)?(?(µ)b|c)", "(?P<𝔘>a)?(?(𝔘)b|c)" })
            Assert.Equal("re: unsupported non-ASCII group name",
                Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(source)).PyMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecimalTokenBoundaryCannotHideInvalidAsciiNames(bool bytes)
    {
        // 4300以上是Ryu明确NI，不把它当CP默认int转换的ValueError差分。
        string number = new('1', 4301);
        Assert.Equal("re: unsupported conditional group number digit limit",
            Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain("()(?(" + number + ")a|b)", bytes))).PyMessage);
        var mixed = Assert.Throws<PyRegexError>(() => PyRegex.Compile(Domain("()(?(" + number + "x)a|b)", bytes)));
        Assert.Equal("bad character in group name '" + number + "x'", mixed.MessageText);
        Assert.Equal(5L, mixed.Position);
        string leadingZero = new('0', 4299);
        var compiled = PyRegex.Compile(Domain("()(?(" + leadingZero + "1)a|b)", bytes));
        Assert.IsType<PyRegexMatch>(Call(compiled, "fullmatch", Domain("a", bytes)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConditionalDepthAndCombinedTranslationKeepExistingLimits(bool bytes)
    {
        string Nested(int depth) => "()" + string.Concat(Enumerable.Repeat("(?(1)", depth)) + new string(')', depth);
        Assert.IsType<PyRegexMatch>(Call(PyRegex.Compile(Domain(Nested(256), bytes)), "fullmatch", Domain("", bytes)));
        Assert.Throws<PyRecursionError>(() => PyRegex.Compile(Domain(Nested(257), bytes)));
        foreach (string source in new[] { "()(?(1)" + new string('a', 2200) + ")",
            "()(?(1)" + new string('a', 1100) + "|" + new string('b', 1100) + ")" })
            Assert.Equal("re: unsupported translated pattern size limit",
                Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain(source, bytes))).PyMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldNonconditionalPathAndSuccessfulCacheSurviveConditionalFailure(bool bytes)
    {
        object source = Domain("(a|b)+", bytes);
        var compiled = PyRegex.Compile(source);
        Assert.Throws<PyRegexError>(() => PyRegex.Compile(Domain("(?(2)a|b)[", bytes)));
        Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain("(a(?(1)b|c))", bytes)));
        Assert.Same(compiled, PyRegex.Compile(source));
        var matched = Assert.IsType<PyRegexMatch>(Call(compiled, "fullmatch", Domain("ab", bytes)));
        Assert.Equal(bytes ? "b'b'" : "'b'", PyRepr.Repr(Call(matched, "group", 1L)));
    }
}
