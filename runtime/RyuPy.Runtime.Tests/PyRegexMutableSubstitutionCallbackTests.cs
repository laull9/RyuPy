// mutable substitution阶段C2内部专项：活捕获、完整窗口刷新、重入独立租约与延迟结果检查。
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexMutableSubstitutionCallbackTests
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
    private static object Sub(PyRegexPattern pattern, object replacement, object source, long count = 0,
        bool tuple = false, Func<Regex, string, int, Match>? executor = null) =>
        Invoke(pattern, "SubstituteMutableBytesPrepared", replacement, source, count, tuple, true, null, executor);
    private static PyFunction Callback(Func<PyRegexMatch, object?> action) =>
        PyNativeFunctions.CreateVariadic("replace", args => action(Assert.IsType<PyRegexMatch>(Assert.Single(args))));
    private static string Text(object? value) => Encoding.Latin1.GetString(Assert.IsType<PyBytes>(value).Bytes);
    private static void Locked(PyMemoryView view, int count) => Assert.Equal(
        $"memoryview has {count} exported buffer{(count == 1 ? "" : "s")}", Assert.Throws<PyBufferError>(view.Release).PyMessage);

    [Fact]
    public void ReferenceMutationCopiesPrefixBeforeCallbackAndReadsLiveTail()
    {
        foreach (bool useView in new[] { false, true })
        {
            var data = A("paxaT"); PyMemoryView? view = useView ? View(data) : null; object source = (object?)view ?? data;
            var saved = new List<PyRegexMatch>(); var snapshots = new List<PyBytes>();
            var pattern = P("(a)");
            var result = Assert.IsType<PyTuple>(Sub(pattern, Callback(match =>
            {
                Assert.Same(source, match.GetAttr("string")); Assert.Same(pattern, match.GetAttr("re"));
                Assert.Equal(0L, match.GetAttr("pos")); Assert.Equal(5L, match.GetAttr("endpos"));
                saved.Add(match); snapshots.Add(Assert.IsType<PyBytes>(Call(match, "group")));
                Assert.Null(typeof(PyRegexMatch).GetField("_raw", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(match));
                Assert.Throws<PyBufferError>(() => data.Append(33L)); if (view is not null) Locked(view, 1);
                if (saved.Count == 1)
                    for (int index = 0; index < 5; index++) data.SetItem((long)index, (long)"QZabY"[index]);
                return B("#");
            }), source, tuple: true));
            Assert.Equal("p##bY", Text(result.Items[0])); Assert.Equal(2L, result.Items[1]);
            Assert.Equal(new[] { "a", "a" }, snapshots.Select(value => Text(value)));
            Assert.Equal(new[] { "Z", "a" }, saved.Select(match => Text(Call(match, "group"))));
            Assert.Equal(new[] { "(1, 2)", "(2, 3)" }, saved.Select(match => PyRepr.Repr(Call(match, "span"))));
            view?.Release(); data.Append(33L);
            if (view is not null) Assert.Throws<PyTypeError>(() => Call(saved[0], "group"));
        }
    }

    [Fact]
    public void CountOneReadsModifiedTailWithoutAnotherSearchAndKeepsWholeReplacementIdentity()
    {
        var data = A("aT"); var view = View(data); int executions = 0, callbacks = 0;
        var result = Assert.IsType<PyTuple>(Sub(P("a"), Callback(_ =>
        { callbacks++; data.SetItem(1L, 90L); return B("!"); }), view, 1L, true,
            (regex, text, pos) => { executions++; Assert.Equal(1, executions); return regex.Match(text, pos); }));
        Assert.Equal("!Z", Text(result.Items[0])); Assert.Equal(1L, result.Items[1]);
        Assert.Equal(1, callbacks); Assert.Equal(1, executions); view.Release(); data.Append(33L);
        PyBytes literal = B("same"); Assert.Same(literal, Sub(P("a"), Callback(_ => literal), A("a")));
    }

    [Fact]
    public void CallbackRefreshHappensOncePerEpochAndNotAfterFailedEmptyRetries()
    {
        var data = A("ab"); int callbacks = 0, previousEpoch = -1, executions = 0; string? previousInput = null;
        var result = Assert.IsType<PyTuple>(Sub(P("|z"), Callback(_ => { callbacks++; return B("-"); }), data,
            tuple: true, executor: (regex, text, pos) =>
            {
                executions++;
                if (previousEpoch == callbacks) Assert.Same(previousInput, text);
                else if (previousInput is not null) Assert.NotSame(previousInput, text);
                previousEpoch = callbacks; previousInput = text; return regex.Match(text, pos);
            }));
        Assert.Equal("-a-b-", Text(result.Items[0])); Assert.Equal(3L, result.Items[1]);
        Assert.Equal(3, callbacks); Assert.Equal(6, executions); data.Append(33L);
    }

    [Fact]
    public void FullPrefixAndSamePositionRetrySeeCallbackWrites()
    {
        // 这两个sub组合由后续应用首次CP-first核定，不冒充旧iterator标签的已跑sub结果。
        var boundaryData = A("ab"); int boundaries = 0;
        Assert.Equal("##", Text(Sub(P(@"a|\bb"), Callback(_ =>
        { if (++boundaries == 1) boundaryData.SetItem(0L, 32L); return B("#"); }), boundaryData)));
        Assert.Equal(2, boundaries); boundaryData.Append(33L);
        var retryData = A("b"); int empties = 0;
        var result = Assert.IsType<PyTuple>(Sub(P("|a"), Callback(_ =>
        { if (++empties == 1) retryData.SetItem(0L, 97L); return B("#"); }), retryData, tuple: true));
        Assert.Equal("###", Text(result.Items[0])); Assert.Equal(3L, result.Items[1]); retryData.Append(33L);
    }

    [Fact]
    public void NestedSearchAndSubOwnSeparateViewExports()
    {
        var data = A("aa"); var view = View(data); var retained = new List<PyRegexMatch>();
        var result = Sub(P("a"), Callback(outer =>
        {
            retained.Add(outer); Locked(view, 1);
            retained.Add(Assert.IsType<PyRegexMatch>(Call(P("a"), "search", view))); Locked(view, 1);
            Assert.Equal("ia", Text(Sub(P("a"), Callback(inner =>
            { retained.Add(inner); Locked(view, 2); return B("i"); }), view, 1L)));
            Locked(view, 1); return B("o");
        }), view, 1L);
        Assert.Equal("oa", Text(result)); Assert.Equal(3, retained.Count);
        view.Release(); data.Append(33L);
        foreach (var match in retained)
        { Assert.Same(view, match.GetAttr("string")); Assert.Throws<PyTypeError>(() => Call(match, "group")); }
    }

    [Fact]
    public void ChildViewAllowsParentSiblingReleaseAndAliasWritesDuringCallback()
    {
        foreach (string format in new[] { "B", "c" })
        {
            var data = A("!aT?"); var parent = View(data);
            var child = (PyMemoryView)parent.GetItem(new PySlice(1L, 3L, 1L));
            var sibling = (PyMemoryView)parent.GetItem(new PySlice(1L, 3L, 1L));
            var alias = (PyMemoryView)Call(child, "cast", format)!;
            var result = Sub(P("a"), Callback(match =>
            {
                Assert.Same(child, match.GetAttr("string")); Assert.Equal("(0, 1)", PyRepr.Repr(Call(match, "span")));
                parent.Release(); sibling.Release(); alias.SetItem(1L, format == "c" ? B("Z") : 90L); alias.Release();
                Locked(child, 1); Assert.Throws<PyBufferError>(() => data.Append(33L)); return B("!");
            }), child, 1L);
            Assert.Equal("!Z", Text(result)); child.Release(); data.Append(33L);
        }
    }

    [Fact]
    public void CallbackResultsJoinAfterAllCallbacksAndReleaseTheLease()
    {
        foreach (object bad in new object[] { "bad", 7L, A("bad"), View(B("bad")) })
        {
            var data = A("aa"); var view = View(data); int callbacks = 0;
            Func<object> action = () => Sub(P("a"), Callback(_ => ++callbacks == 1 ? bad : B("X")), view);
            if (bad is PyByteArray or PyMemoryView) Assert.Equal("badX", Text(action()));
            else Assert.Equal($"sequence item 0: expected a bytes-like object, {(bad is string ? "str" : "int")} found",
                Assert.Throws<PyTypeError>(() => action()).PyMessage);
            Assert.Equal(2, callbacks); view.Release(); data.Append(33L); if (bad is PyMemoryView badView) badView.Release();
        }
        var prefixed = A("pa");
        Assert.Equal("sequence item 1: expected a bytes-like object, str found",
            Assert.Throws<PyTypeError>(() => Sub(P("a"), Callback(_ => "bad"), prefixed)).PyMessage);
        prefixed.Append(33L);
    }

    [Fact]
    public void NoneDeletesAndCollectedMutableResultsKeepReferencesUntilJoin()
    {
        var result = Assert.IsType<PyTuple>(Sub(P("a"), Callback(_ => PyNone.Instance), A("aba"), tuple: true));
        Assert.Equal("b", Text(result.Items[0])); Assert.Equal(2L, result.Items[1]);
        var data = A("aa"); var view = View(data); var shared = A("X"); int callbacks = 0;
        var collected = ((List<object?> Parts, long Changed))Invoke(P("a"), "CollectMutableSubstitutionParts",
            Callback(_ => { callbacks++; shared.Append(89L); return shared; }), view, 0L, true, null, null);
        Assert.Equal(2, callbacks); Assert.Equal(2L, collected.Changed); Assert.Equal(2, collected.Parts.Count);
        Assert.All(collected.Parts, part => Assert.Same(shared, part)); Assert.Equal("XYY", Encoding.Latin1.GetString(shared.Bytes.ToArray()));
        view.Release(); data.Append(33L);
    }

    [Fact]
    public void LaterCallbackExceptionWinsEarlierBadResultAndDoesNotKeepMatchLease()
    {
        var data = A("aa"); var view = View(data); var marker = new PyValueError("later callback");
        var retained = new List<PyRegexMatch>();
        Assert.Same(marker, Assert.Throws<PyValueError>(() => Sub(P("a"), Callback(match =>
        { retained.Add(match); if (retained.Count == 2) throw marker; return "earlier bad result"; }), view)));
        Assert.Equal(2, retained.Count); view.Release(); data.Append(33L);
        Assert.Equal("(0, 1)", PyRepr.Repr(Call(retained[0], "span")));
        Assert.Throws<PyTypeError>(() => Call(retained[0], "group"));
    }

    [Fact]
    public void BackendFailureAfterCallbackReleasesWithoutRetryOrWrapping()
    {
        var data = A("aa"); var view = View(data); var marker = new PyRuntimeError("after callback backend");
        PyRegexMatch? retained = null; int executions = 0, callbacks = 0;
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Sub(P("a"), Callback(match =>
        { retained = match; callbacks++; data.SetItem(1L, 98L); return B("#"); }), view,
            executor: (regex, text, pos) =>
            { if (++executions == 2) { Assert.Equal("ab", text); throw marker; } return regex.Match(text, pos); })));
        Assert.Equal(1, callbacks); Assert.Equal(2, executions); Assert.NotNull(retained);
        view.Release(); data.Append(33L); Assert.Throws<PyTypeError>(() => Call(retained!, "group"));
    }

    [Fact]
    public void CallableObjectsAndHighOrderSlotBoundaryAreOnlyConsumedForMatches()
    {
        var cls = new PyClass("Replacement");
        cls.SetAttr("__call__", PyNativeFunctions.Create("__call__", ["self", "match"], [null, null], 2, a =>
        { Assert.IsType<PyRegexMatch>(a[1]); return B("!"); }));
        Assert.Equal("!!", Text(Sub(P("a"), new PyInstance(cls), A("aa"))));
        int gets = 0;
        var descriptorClass = new PyClass("CallDescriptor");
        descriptorClass.SetAttr("__get__", PyNativeFunctions.CreateVariadic("__get__", _ => { gets++; return B("unused"); }));
        var guardedClass = new PyClass("GuardedReplacement"); guardedClass.SetAttr("__call__", new PyInstance(descriptorClass));
        var guarded = new PyInstance(guardedClass);
        Assert.Equal("z", Text(Sub(P("a"), guarded, A("z"))));
        Assert.Equal("a", Text(Sub(P("a"), guarded, A("a"), -1L)));
        var data = A("a"); var view = View(data);
        Assert.Equal("re: bytes regex custom __call__ descriptor is not supported",
            Assert.Throws<PyNotImplementedError>(() => Sub(P("a"), guarded, view)).PyMessage);
        Assert.Equal(0, gets); view.Release(); data.Append(33L);
    }
}
