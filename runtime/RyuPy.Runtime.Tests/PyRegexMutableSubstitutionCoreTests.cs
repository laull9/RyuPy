// mutable substitution阶段C1内部专项：模板/片段、空匹配、count与确定性释放，并核C3公共接线。
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexMutableSubstitutionCoreTests
{
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyByteArray A(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyRegexPattern P(string value) => PyRegex.Compile(B(value));
    private static PyMemoryView View(object value) => (PyMemoryView)PyMemoryView.Create([value], [], []);
    private static object? Call(PyNativeObject owner, string name, params object?[] args) => PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static object Invoke(PyRegexPattern pattern, string method, params object?[] args)
    {
        try { return typeof(PyRegexPattern).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pattern, args)!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static object Template(PyRegexPattern pattern, object value)
    {
        Type type = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBytesTemplate")!;
        try
        {
            return type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                [typeof(PyRegexPattern), typeof(object)], null)!.Invoke([pattern, value]);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static object Sub(PyRegexPattern pattern, object? replacement, object source, long count = 0,
        bool tuple = false, object? template = null, Func<Regex, string, int, Match>? executor = null) =>
        Invoke(pattern, "SubstituteMutableBytesPrepared", replacement, source, count, tuple, false, template, executor);
    private static (List<object?> Parts, long Changed) Collect(PyRegexPattern pattern, object? replacement,
        object source, long count = 0, object? template = null, Func<Regex, string, int, Match>? executor = null) =>
        ((List<object?> Parts, long Changed))Invoke(pattern, "CollectMutableSubstitutionParts", replacement,
            source, count, false, template, executor);
    private static string Text(object? value) => Encoding.Latin1.GetString(Assert.IsType<PyBytes>(value).Bytes);

    [Fact]
    public void LiteralReplacementUsesBytesForAllExistingMutableCarriers()
    {
        var data = A("!a\u0080a?"); var whole = View(data);
        var chars = (PyMemoryView)Call(whole, "cast", "c")!;
        var slice = (PyMemoryView)whole.GetItem(new PySlice(1L, 4L, 1L));
        var readOnly = View(B("!a\u0080a?"));
        try
        {
            foreach (object source in new object[] { data, whole, chars, slice, readOnly })
            {
                var result = Assert.IsType<PyTuple>(Sub(P("a"), B("X"), source, tuple: true));
                Assert.Equal(source == slice ? "X\u0080X" : "!X\u0080X?", Text(result.Items[0]));
                Assert.Equal(2L, result.Items[1]); Assert.NotSame(source, result.Items[0]);
            }
        }
        finally { whole.Release(); chars.Release(); slice.Release(); readOnly.Release(); }
        data.Append(33L);
        Assert.Equal("!X\u0080X?", Text(Call(P("a"), "sub", B("X"), B("!a\u0080a?"))));
    }

    [Fact]
    public void NamedNumericAndMissingTemplateGroupsUseExistingPythonParser()
    {
        var pattern = P("(?P<first>a)(b)?"); var data = A("a ab");
        object template = Template(pattern, B(@"<\g<first>|\2|\g<0>>"));
        Assert.Equal("<a||a> <a|b|ab>", Text(Sub(pattern, B("unused"), data, template: template)));
        data.Append(33L);
        Assert.Equal(@"$1 $1", Text(Sub(P("a"), B("$1"), A("a a"))));
        var escapes = P("a");
        Assert.Equal(new byte[] { 0, 255, 10, 92 }, Assert.IsType<PyBytes>(Sub(escapes, B("unused"), A("a"),
            template: Template(escapes, B(@"\000\377\n\\")))).Bytes);
    }

    [Fact]
    public void NoMatchNegativeCountAndEmptySourcesReturnIndependentBytes()
    {
        var pattern = P("a");
        foreach ((string input, long count) in new[] { ("zz", 0L), ("aaa", -1L), ("", 0L), ("", -1L) })
        {
            var data = A(input); int executions = 0;
            var result = Assert.IsType<PyTuple>(Sub(pattern, B("X"), data, count, true, executor: (regex, text, pos) =>
            { executions++; return regex.Match(text, pos); }));
            Assert.Equal(input, Text(result.Items[0])); Assert.Equal(0L, result.Items[1]);
            Assert.NotSame(data, result.Items[0]); data.Append(33L); Assert.Equal(input, Text(result.Items[0]));
            if (count < 0) Assert.Equal(0, executions);
        }
        var released = View(B("")); released.Release();
        Assert.Equal("expected string or bytes-like object, got 'memoryview'",
            Assert.Throws<PyTypeError>(() => Sub(pattern, B("X"), released, -1L)).PyMessage);
    }

    [Fact]
    public void PositiveCountStopsBeforeAnyFurtherEngineAttempt()
    {
        var data = A("aaa"); int executions = 0;
        var result = Assert.IsType<PyTuple>(Sub(P("a"), B("X"), data, 1L, true, executor: (regex, text, pos) =>
        {
            executions++;
            if (executions > 1) throw new InvalidOperationException("must not rescan after count");
            return regex.Match(text, pos);
        }));
        Assert.Equal("Xaa", Text(result.Items[0])); Assert.Equal(1L, result.Items[1]); Assert.Equal(1, executions);
        data.Append(33L);
        Assert.Equal("XXX", Text(Sub(P("a"), B("X"), A("aaa"), 1L << 40)));
    }

    [Fact]
    public void EmptyRetryAndCursorAdvanceReuseOneEncodedInputWithoutCallbacks()
    {
        // 精确bytes既有CP空匹配向量；mutable新组合在后续应用CP-first继续核定。
        foreach ((string pattern, string expected, long count) in new[]
            { (".*?", "-----", 5L), ("|a", "-----", 5L), ("a*", "--", 2L), ("(?=a)", "-a-a", 2L) })
        {
            string? firstInput = null; int executions = 0;
            var data = A("aa"); var result = Assert.IsType<PyTuple>(Sub(P(pattern), B("-"), data,
                tuple: true, executor: (regex, text, pos) =>
                {
                    executions++; firstInput ??= text; Assert.Same(firstInput, text);
                    return regex.Match(text, pos);
                }));
            Assert.Equal(expected, Text(result.Items[0])); Assert.Equal(count, result.Items[1]);
            Assert.True(executions > 0); data.Append(33L);
        }
    }

    [Fact]
    public void CollectReturnsOnlyAfterLeaseReleaseAndPreservesOriginalLiteralReference()
    {
        var data = A("a"); var view = View(data); PyBytes literal = B("only");
        var (parts, changed) = Collect(P("a"), literal, view);
        Assert.Equal(1L, changed); Assert.Same(literal, Assert.Single(parts));
        view.Release(); data.Append(33L);
        Assert.Same(literal, Sub(P("a"), literal, A("a")));
        var empty = Assert.IsType<PyBytes>(Sub(P("a"), B(""), A("aa"))); Assert.Empty(empty.Bytes);
    }

    [Fact]
    public void LateTypeErrorAndTemplateExpansionErrorDoNotLeakTheViewLease()
    {
        foreach (bool expand in new[] { false, true })
        {
            var data = A("a"); var view = View(data); var pattern = P("(a)");
            object? template = expand ? Template(pattern, @"\1") : null;
            var error = Assert.Throws<PyTypeError>(() => Sub(pattern, expand ? "unused" : "X", view, template: template));
            Assert.Equal(expand ? "sequence item 1: expected str instance, bytes found"
                : "sequence item 0: expected a bytes-like object, str found", error.PyMessage);
            view.Release(); data.Append(33L);
        }
        var literalData = A("a"); var literalView = View(literalData);
        var collected = Collect(P("a"), "bad", literalView);
        Assert.Equal("bad", Assert.Single(collected.Parts)); literalView.Release(); literalData.Append(33L);
    }

    [Fact]
    public void ControlledBackendFailureReleasesBeforeRethrowingTheSameException()
    {
        var data = A("aa"); var view = View(data); var marker = new PyRuntimeError("controlled backend fault");
        int calls = 0;
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Sub(P("a"), B("X"), view,
            executor: (regex, text, pos) =>
            {
                Assert.Throws<PyBufferError>(() => data.Append(33L));
                Assert.Equal("memoryview has 1 exported buffer", Assert.Throws<PyBufferError>(view.Release).PyMessage);
                if (++calls == 2) throw marker;
                return regex.Match(text, pos);
            })));
        Assert.Equal(2, calls); view.Release(); data.Append(33L);
    }

    [Fact]
    public void PublicMutableSubstitutionAndSavedFinditerUseTheirPreparedPaths()
    {
        var pattern = P("a"); var data = A("aa"); var view = View(data);
        object? savedSub = pattern.GetAttr("sub"), savedSubn = pattern.GetAttr("subn"), savedFinditer = pattern.GetAttr("finditer");
        foreach (object source in new object[] { data, view })
        {
            Assert.Equal("XX", Text(PyOps.CallValue(savedSub, [B("X"), source], [], [])));
            Assert.Equal("(b'Xa', 1)", PyRepr.Repr(PyOps.CallValue(savedSubn, [B("X"), source, 1L], [], [])));
            var callback = PyNativeFunctions.CreateVariadic("replace", args =>
            { Assert.Same(source, Assert.IsType<PyRegexMatch>(args[0]).GetAttr("string")); return B("!"); });
            Assert.Equal("!!", Text(Call(pattern, "sub", callback, source)));
            var iterator = Assert.IsAssignableFrom<IPyIterator>(PyOps.CallValue(savedFinditer, [source], [], []));
            Assert.Same(iterator, PyIter.Create(iterator)); int matched = 0;
            while (iterator.MoveNext()) { matched++; Assert.Same(source, Assert.IsType<PyRegexMatch>(iterator.Current()).GetAttr("string")); }
            Assert.Equal(2, matched);
        }
        view.Release(); data.Append(33L);
    }
}
