// 后顾错误合同专项：变宽在完整语法成功后验证，元数据为None；未支持节点仍保调用现场。
using System.Text;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexLookbehindErrorTests
{
    private static object Domain(string text, bool bytes) => bytes ? new PyBytes(Encoding.Latin1.GetBytes(text)) : text;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VariableWidthHasNoInventedSourceLocation(bool bytes)
    {
        foreach (string body in new[] { "a*", "a+", "a?", "a{1,2}", "a{2,}", "a{,2}", "a|bb", "|a", "(?:ab|c){2}" })
        foreach (string sign in new[] { "=", "!" })
        {
            var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(Domain("(?<" + sign + body + ")x", bytes)));
            Assert.Equal("error", error.PyTypeName);
            Assert.Equal("look-behind requires fixed-width pattern", error.MessageText);
            Assert.Equal(error.MessageText, error.PyMessage); Assert.Equal(error.MessageText, Assert.Single(error.Args));
            Assert.IsType<PyNone>(error.Pattern); Assert.IsType<PyNone>(error.Position);
            Assert.IsType<PyNone>(error.LineNumber); Assert.IsType<PyNone>(error.ColumnNumber);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaterSyntaxErrorsWinOverDeferredWidths(bool bytes)
    {
        object pattern = Domain("(?<=a|bb)x[", bytes);
        var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(pattern));
        Assert.Same(pattern, error.Pattern); Assert.Equal("unterminated character set", error.MessageText);
        Assert.Equal(10L, error.Position); Assert.Equal(1L, error.LineNumber); Assert.Equal(11L, error.ColumnNumber);
        pattern = Domain("(?<=a+)x)", bytes);
        error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(pattern));
        Assert.Same(pattern, error.Pattern); Assert.Equal("unbalanced parenthesis", error.MessageText);
        Assert.Equal(8L, error.Position);
        pattern = Domain("(?<=a+)(?:", bytes);
        error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(pattern));
        Assert.Same(pattern, error.Pattern); Assert.Equal("missing ), unterminated subpattern", error.MessageText);
        Assert.Equal(7L, error.Position);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnterminatedLookbehindRetainsOriginalDomainAndOpeningPosition(bool bytes)
    {
        string source = (bytes ? "ÿ" : "😀") + "\n(?<=ab"; object original = Domain(source, bytes);
        var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(original));
        Assert.Same(original, error.Pattern); Assert.Equal("missing ), unterminated subpattern", error.MessageText);
        Assert.Equal(2L, error.Position); Assert.Equal(2L, error.LineNumber); Assert.Equal(1L, error.ColumnNumber);
    }

    [Theory]
    [InlineData("(?<=(a))b", "capture")]
    [InlineData("(?<!(?P<n>a))b", "capture")]
    [InlineData(@"(a)(?<=\1)b", "backreference")]
    [InlineData("(?P<n>a)(?<=(?P=n))b", "backreference")]
    [InlineData("(a)(?<=(?(1)a|b))c", "conditional")]
    [InlineData("(?<=a(?=b))b", "nested assertion")]
    [InlineData("(?<=a(?!c))b", "nested assertion")]
    [InlineData("(?<=(?<=a)b)c", "nested assertion")]
    [InlineData("(?<=(?<!a)b)c", "nested assertion")]
    [InlineData("(?<=^a)b", "anchor or boundary")]
    [InlineData("(?<=a$)b", "anchor or boundary")]
    [InlineData(@"(?<=\Aa)b", "anchor or boundary")]
    [InlineData(@"(?<=a\Z)b", "anchor or boundary")]
    [InlineData(@"(?<=\ba)b", "anchor or boundary")]
    [InlineData(@"(?<=\Ba)b", "anchor or boundary")]
    public void UnsupportedComplexBodiesKeepCurrentCallerAndContext(string source, string feature)
    {
        long depth = PyTraceback.CaptureDepth(); Assert.Equal(0L, depth);
        var ambient = new PyValueError("ambient lookbehind"); object? token = PyErrors.EnterHandler(ambient);
        PyTraceback.PushFrame("lookbehind_error.py", "caller"); PyTraceback.SetLine(37);
        try
        {
            foreach (bool bytes in new[] { false, true })
            {
                var error = Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain(source, bytes)));
                Assert.Equal("re: unsupported lookbehind " + feature, error.PyMessage);
                Assert.Equal(error.PyMessage, Assert.Single(error.Args)); Assert.Same(ambient, error.Context);
                Assert.Null(error.Cause); Assert.False(error.SuppressContextFlag);
                Assert.Equal(new PyStackFrame("lookbehind_error.py", "caller", 37), Assert.Single(error.Frames));
                Assert.Same(ambient, PyErrors.Current); Assert.Equal(depth + 1, PyTraceback.CaptureDepth());
            }
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.PopTo(depth + 1); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatArithmeticKeepsSyntaxPriorityAndExplicitResourceBoundary(bool bytes)
    {
        object invalid = Domain("(?<=a{2,1})", bytes);
        var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(invalid));
        Assert.Same(invalid, error.Pattern); Assert.Equal(6L, error.Position);
        Assert.Equal("min repeat greater than max repeat", error.MessageText);
        Assert.Equal("re: unsupported lookbehind repeat size limit",
            Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain("(?<=a{2147483648})", bytes))).PyMessage);
        object laterSyntax = Domain("(?<=a{2147483648})[", bytes);
        error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(laterSyntax));
        Assert.Same(laterSyntax, error.Pattern); Assert.Equal(18L, error.Position);
        Assert.Equal("unterminated character set", error.MessageText);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownEscapePointsAtBackslashAndKeepsCallerEvenAfterDeferredWidth(bool bytes)
    {
        long depth = PyTraceback.CaptureDepth(); Assert.Equal(0L, depth);
        var ambient = new PyValueError("ambient escape"); object? token = PyErrors.EnterHandler(ambient);
        PyTraceback.PushFrame("lookbehind_escape.py", "caller"); PyTraceback.SetLine(51);
        try
        {
            string prefix = (bytes ? "ÿ" : "😀") + "\n";
            foreach (var item in new[] { (@"\q", 0L, 1L, 1L), (@"[\q]", 1L, 1L, 2L), (@"(?<=\q)a", 4L, 1L, 5L),
                (@"(?<=a+)\q", 7L, 1L, 8L), (prefix + @"\q", 2L, 2L, 1L),
                (prefix + @"(?<=\q)a", 6L, 2L, 5L), (prefix + @"(?<=a+)\q", 9L, 2L, 8L) })
            {
                object original = Domain(item.Item1, bytes);
                var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(original));
                Assert.Equal(@"bad escape \q", error.MessageText); Assert.Same(original, error.Pattern);
                Assert.Equal(item.Item2, error.Position); Assert.Equal(item.Item3, error.LineNumber);
                Assert.Equal(item.Item4, error.ColumnNumber); Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("lookbehind_escape.py", "caller", 51), Assert.Single(error.Frames));
                Assert.Same(ambient, PyErrors.Current); Assert.Equal(depth + 1, PyTraceback.CaptureDepth());
            }
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.PopTo(depth + 1); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CumulativeWidthHasADeferredResourceGuardButZeroRepeatCancelsIt(bool bytes)
    {
        foreach (string source in new[] { "(?<=(?:a{65536}){65536})", "(?<=(?:a{65536}){65536}){0}" })
            Assert.Equal("re: unsupported lookbehind repeat size limit",
                Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain(source, bytes))).PyMessage);
        object malformed = Domain("(?<=(?:a{65536}){65536})[", bytes);
        var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(malformed));
        Assert.Equal("unterminated character set", error.MessageText); Assert.Same(malformed, error.Pattern);
        var zero = PyRegex.Compile(Domain("(?<=(?:(?:a{65536}){65536}){0})x", bytes));
        Assert.IsType<PyRegexMatch>(PyOps.CallValue(zero.GetAttr("fullmatch"), [Domain("x", bytes)], [], []));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeferredWidthErrorIsPreparedAtItsOwnCreationSite(bool bytes)
    {
        long depth = PyTraceback.CaptureDepth(); Assert.Equal(0L, depth);
        var ambient = new PyValueError("ambient width"); object? token = PyErrors.EnterHandler(ambient);
        PyTraceback.PushFrame("lookbehind_width.py", "caller"); PyTraceback.SetLine(61);
        try
        {
            var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(Domain("(?<=a*)x", bytes)));
            Assert.Same(ambient, error.Context); Assert.Null(error.Cause); Assert.False(error.SuppressContextFlag);
            Assert.Equal(new PyStackFrame("lookbehind_width.py", "caller", 61), Assert.Single(error.Frames));
            Assert.IsType<PyNone>(error.Pattern); Assert.IsType<PyNone>(error.Position);
            Assert.IsType<PyNone>(error.LineNumber); Assert.IsType<PyNone>(error.ColumnNumber);
            Assert.Equal("look-behind requires fixed-width pattern", Assert.Single(error.Args));
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(depth + 1, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.PopTo(depth + 1); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidRepeatRangeOutsideLookbehindWinsBeforeWidthValidation(bool bytes)
    {
        foreach (var item in new[] { ("(?<=a*)x{2,1}", 9L), ("x{2,1}(?<=a*)", 2L),
            ("x{0002,0001}(?<=a*)", 2L), ("x{2,1}", 2L) })
        {
            object original = Domain(item.Item1, bytes);
            var error = Assert.Throws<PyRegexError>(() => PyRegex.Compile(original));
            Assert.Equal("min repeat greater than max repeat", error.MessageText);
            Assert.Same(original, error.Pattern); Assert.Equal(item.Item2, error.Position);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailureDoesNotPoisonExistingCacheOrOrdinarySyntax(bool bytes)
    {
        object source = Domain("(?:a|b){2}", bytes); var compiled = PyRegex.Compile(source);
        Assert.Throws<PyRegexError>(() => PyRegex.Compile(Domain("(?<=a*)", bytes)));
        Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(Domain("(?<=(a))", bytes)));
        Assert.Same(compiled, PyRegex.Compile(source));
        Assert.IsType<PyRegexMatch>(PyOps.CallValue(compiled.GetAttr("fullmatch"), [Domain("ab", bytes)], [], []));
    }
}
