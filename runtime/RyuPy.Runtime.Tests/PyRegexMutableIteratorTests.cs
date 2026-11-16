// mutable finditer专项：创建/耗尽租约、活内容、成功checkpoint、确定性清理及B2公共准备顺序；不测GC时机。
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexMutableIteratorTests
{
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyByteArray A(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyRegexPattern P(string value) => PyRegex.Compile(B(value));
    private static PyMemoryView View(object value) => (PyMemoryView)PyMemoryView.Create([value], [], []);
    private static object? Call(PyNativeObject owner, string name, params object?[] args) => PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static object Invoke(object owner, string method, params object?[] args)
    {
        try { return owner.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(owner, args)!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static IPyIterator Create(string pattern, object source, long start = 0, long end = long.MaxValue) =>
        (IPyIterator)Invoke(P(pattern), "CreateMutableBytesIterator", source, start, end);
    private static bool Step(IPyIterator iterator, Func<Regex, string, int, Match> executor) => (bool)Invoke(iterator, "Step", executor);
    private static object? Field(IPyIterator iterator, string name) => iterator.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(iterator);
    private static void Finish(IPyIterator iterator) => Assert.False((bool)Invoke(iterator, "Finish"));
    private static PyRegexMatch Next(IPyIterator iterator) { Assert.True(iterator.MoveNext()); return Assert.IsType<PyRegexMatch>(iterator.Current()); }
    private static string Text(object? value) => Encoding.Latin1.GetString(Assert.IsType<PyBytes>(value).Bytes);
    private static string Span(PyRegexMatch match) => PyRepr.Repr(Call(match, "span"));
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("IteratorIndex");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("__index__", _ => body()));
        return new PyInstance(cls);
    }
    private static void Locked(PyMemoryView view, int count = 1) => Assert.Equal(
        $"memoryview has {count} exported buffer{(count == 1 ? "" : "s")}", Assert.Throws<PyBufferError>(view.Release).PyMessage);

    [Fact]
    public void CreationAndLastYieldKeepEachExistingCarrierExportedUntilTrueExhaustion()
    {
        foreach (string kind in new[] { "array", "B", "c", "slice", "readonly" })
        {
            var data = A("!a?"); PyMemoryView? view = null; object source = data;
            if (kind != "array")
            {
                view = View(kind == "readonly" ? B("!a?") : data);
                if (kind is "c" or "slice")
                {
                    var parent = view;
                    view = kind == "c" ? (PyMemoryView)Call(parent, "cast", "c")!
                        : (PyMemoryView)parent.GetItem(new PySlice(1L, 2L, 1L));
                    parent.Release();
                }
                source = view;
            }
            var iterator = Create("(a)", source); Assert.IsType<PyNone>(iterator.Current());
            if (view is not null) Locked(view);
            if (kind != "readonly") Assert.Throws<PyBufferError>(() => data.Append(33L));
            var match = Next(iterator); Assert.Same(source, match.GetAttr("string")); Assert.Equal("a", Text(Call(match, "group")));
            Assert.Equal(kind == "slice" ? "(0, 1)" : "(1, 2)", Span(match));
            Assert.Equal(kind == "slice" ? 1L : 3L, match.GetAttr("endpos"));
            if (view is not null) Locked(view);
            if (kind != "readonly") Assert.Throws<PyBufferError>(() => data.Append(33L));
            Assert.False(iterator.MoveNext()); Assert.False(iterator.MoveNext()); Assert.IsType<PyNone>(iterator.Current());
            view?.Release(); data.Append(33L);
        }
    }

    [Fact]
    public void NoMatchEmptyAndReversedWindowsStillAcquireAtCreation()
    {
        foreach ((string pattern, string text, long start, long end) in new[]
            { ("z", "aaa", 0L, 99L), ("z", "", 0L, 99L), ("a", "aaa", 2L, 1L) })
        {
            var data = A(text); var iterator = Create(pattern, data, start, end); int executions = 0;
            Assert.Throws<PyBufferError>(() => data.Append(33L));
            Assert.False(Step(iterator, (regex, input, position) => { executions++; return regex.Match(input, position); }));
            Assert.Equal(start > end ? 0 : 1, executions); data.Append(33L);
            Assert.False(iterator.MoveNext());
        }
    }

    [Fact]
    public void FirstAndLaterNextReadWritesWhileOldCapturesStayLive()
    {
        var data = A("aXa"); var iterator = Create("a", data); data.SetItem(0L, 98L);
        var match = Next(iterator); Assert.Equal("(2, 3)", Span(match)); PyBytes snapshot = Assert.IsType<PyBytes>(Call(match, "group"));
        data.SetItem(2L, 99L); Assert.Equal("c", Text(Call(match, "group"))); Assert.Equal("a", Text(snapshot));
        Assert.Null(typeof(PyRegexMatch).GetField("_raw", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(match));
        Assert.False(iterator.MoveNext()); data.Append(33L);
        data = A("aXa"); iterator = Create("a", data); Assert.Equal("(0, 1)", Span(Next(iterator)));
        data.SetItem(1L, 97L); Assert.Equal("(1, 2)", Span(Next(iterator))); Assert.Equal("(2, 3)", Span(Next(iterator)));
        Assert.False(iterator.MoveNext()); data.Append(33L);
    }

    [Fact]
    public void EmptyMatchRetrySeesNewNonemptyAtTheSamePosition()
    {
        var data = A("b"); var iterator = Create("|a", data);
        Assert.Equal("(0, 0)", Span(Next(iterator))); data.SetItem(0L, 97L);
        Assert.Equal("(0, 1)", Span(Next(iterator))); Assert.Equal("(1, 1)", Span(Next(iterator)));
        Assert.Throws<PyBufferError>(() => data.Append(33L)); Assert.False(iterator.MoveNext()); data.Append(33L);
    }

    [Fact]
    public void FullPrefixRefreshIncludesWritesThroughBothAliasFormats()
    {
        foreach (string format in new[] { "B", "c" })
        {
            var data = A("ab"); var view = View(data); var alias = (PyMemoryView)Call(view, "cast", format)!;
            var iterator = Create(@"a|\bb", view); Assert.Equal("(0, 1)", Span(Next(iterator)));
            alias.SetItem(0L, format == "c" ? B(" ") : 32L);
            Assert.Equal("(1, 2)", Span(Next(iterator))); Assert.False(iterator.MoveNext());
            view.Release(); alias.Release(); data.Append(33L);
        }
    }

    [Fact]
    public void InitialBoundsRemainFixedAndWholePrefixAnchorsAreNotSlicedAway()
    {
        var data = A("zaaz"); var iterator = Create("a", data, 1L, 3L);
        foreach (string span in new[] { "(1, 2)", "(2, 3)" })
        {
            var match = Next(iterator); Assert.Equal(span, Span(match));
            Assert.Equal(1L, match.GetAttr("pos")); Assert.Equal(3L, match.GetAttr("endpos"));
        }
        Assert.False(iterator.MoveNext()); data.Append(33L);
        iterator = Create("^a", A("za"), 1L, 2L); Assert.False(iterator.MoveNext());
        iterator = Create("a$", A("zaZ"), -99L, 2L); var last = Next(iterator);
        Assert.Equal("(1, 2)", Span(last)); Assert.Equal(0L, last.GetAttr("pos")); Assert.Equal(2L, last.GetAttr("endpos"));
        Assert.False(iterator.MoveNext());
        iterator = Create("", A("a"), 1L << 40, 1L << 40);
        Assert.Equal("(1, 1)", Span(Next(iterator))); Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void TwoIteratorsAndChildViewsOnlyReleaseTheirOwnExports()
    {
        foreach (object owner in new object[] { A("ab"), B("ab") })
        {
            var view = View(owner); var one = Create(".", view); var two = Create(".", view); Locked(view, 2);
            while (one.MoveNext()) { } Locked(view, 1);
            var kept = Next(two); while (two.MoveNext()) { } view.Release();
            Assert.Throws<PyTypeError>(() => Call(kept, "group"));
            if (owner is PyByteArray array) array.Append(33L);
        }
        var data = A("!ab?"); var parent = View(data); var child = (PyMemoryView)parent.GetItem(new PySlice(1L, 3L, 1L));
        var iterator = Create(".", child); parent.Release(); Locked(child);
        Assert.Equal("(0, 1)", Span(Next(iterator))); data.SetItem(2L, 90L);
        var second = Next(iterator); Assert.Equal("Z", Text(Call(second, "group"))); Assert.Equal("(1, 2)", Span(second));
        Assert.False(iterator.MoveNext()); child.Release(); data.Append(33L);
    }

    [Fact]
    public void ExhaustionClearsStrongScanReferencesButSavedMatchesStillOwnTheirSubject()
    {
        var data = A("a"); var iterator = Create("a", data); var saved = Next(iterator);
        Assert.Same(saved, iterator.Current()); Assert.False(iterator.MoveNext());
        foreach (string name in new[] { "_current", "_lease", "_source", "_pattern" }) Assert.Null(Field(iterator, name));
        data.SetItem(0L, 90L); data.Append(33L); Assert.Same(data, saved.GetAttr("string")); Assert.Equal("Z", Text(Call(saved, "group")));
        Assert.False(Step(iterator, (_, _, _) => throw new InvalidOperationException("finished must not execute")));
    }

    [Fact]
    public void InitialBackendFailureKeepsLeaseAndRetriesFromFreshContent()
    {
        var data = A("aX"); var view = View(data); var iterator = Create("a", view); var marker = new PyRuntimeError("controlled first failure");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Step(iterator, (_, _, _) => throw marker)));
        Assert.IsType<PyNone>(iterator.Current()); Assert.Equal(0, Field(iterator, "_cursor")); Locked(view);
        data.SetItem(0L, 98L); data.SetItem(1L, 97L); Assert.Equal("(1, 2)", Span(Next(iterator)));
        Assert.False(iterator.MoveNext()); view.Release(); data.Append(33L);
    }

    [Fact]
    public void FailedRetryAfterCandidateAdvanceDoesNotCommitCursorOrReplaceCurrent()
    {
        var data = A("b"); var iterator = Create("|a", data); var first = Next(iterator);
        var marker = new PyValueError("after candidate advance"); int executions = 0; string? snapshot = null;
        Assert.Same(marker, Assert.Throws<PyValueError>(() => Step(iterator, (regex, input, position) =>
        {
            snapshot ??= input; Assert.Same(snapshot, input);
            if (++executions == 2) { Assert.Equal(1, position); throw marker; }
            Assert.Equal(0, position); return regex.Match(input, position);
        })));
        Assert.Equal(2, executions); Assert.Equal(0, Field(iterator, "_cursor")); Assert.Equal(true, Field(iterator, "_retry"));
        Assert.Same(first, iterator.Current()); Assert.Throws<PyBufferError>(() => data.Append(33L));
        data.SetItem(0L, 97L); Assert.Equal("(0, 1)", Span(Next(iterator))); Assert.Equal("(1, 1)", Span(Next(iterator)));
        Assert.False(iterator.MoveNext()); data.Append(33L);
    }

    [Fact]
    public void EachNextEncodesOnceWhileItsInternalRetryAndAdvanceReuseThatSnapshot()
    {
        var data = A("ab"); var iterator = Create("|z", data); string? previous = null; int executions = 0;
        for (int step = 0; step < 4; step++)
        {
            string? current = null;
            bool moved = Step(iterator, (regex, input, position) =>
            {
                if (current is null) { if (previous is not null) Assert.NotSame(previous, input); current = input; }
                else Assert.Same(current, input);
                executions++; return regex.Match(input, position);
            });
            Assert.Equal(step < 3, moved); previous = current;
        }
        Assert.Equal(6, executions); data.Append(33L);
    }

    [Fact]
    public void ReentryOwnErrorKeepsOuterGuardAndFailureCheckpoint()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        var data = A("aa"); var view = View(data); var iterator = Create("a", view);
        try
        {
            PyTraceback.PushFrame("mutable_iterator.py", "caller"); PyTraceback.SetLine(23);
            Assert.True(Step(iterator, (regex, input, position) =>
            {
                var error = Assert.Throws<PyValueError>(() => iterator.MoveNext());
                Assert.Equal("regular expression scanner already executing", error.PyMessage);
                Assert.Same(ambient, error.Context); Assert.Same(ambient, PyErrors.Current);
                Assert.Equal(new PyStackFrame("mutable_iterator.py", "caller", 23), Assert.Single(error.Frames));
                Assert.Equal(1, Field(iterator, "_executing")); return regex.Match(input, position);
            }));
            var saved = iterator.Current();
            Assert.Throws<PyValueError>(() => Step(iterator, (_, _, _) =>
            { iterator.MoveNext(); throw new InvalidOperationException("unreachable"); }));
            Assert.Equal(0, Field(iterator, "_executing")); Assert.Same(saved, iterator.Current()); Locked(view);
            Assert.Equal("(1, 2)", Span(Next(iterator))); Assert.False(iterator.MoveNext()); view.Release(); data.Append(33L);
        }
        finally { Finish(iterator); view.Release(); PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void ControlledCleanupOrdersAndRepeatedFinishNeverDoubleRelease()
    {
        foreach (bool viewFirst in new[] { true, false })
        {
            var data = A("ab"); var view = View(data); var iterator = Create(".", view);
            if (viewFirst)
            {
                Invoke(view, "ReleaseOwnedBuffer", true); Invoke(view, "ReleaseOwnedBuffer", true);
                Assert.Throws<PyBufferError>(() => data.Append(33L)); Finish(iterator); Finish(iterator);
            }
            else
            {
                Finish(iterator); Finish(iterator); Assert.Throws<PyBufferError>(() => data.Append(33L));
                Invoke(view, "ReleaseOwnedBuffer", true); Invoke(view, "ReleaseOwnedBuffer", true);
            }
            data.Append(33L); Assert.False(iterator.MoveNext());
            foreach (string name in new[] { "_lease", "_source", "_current" }) Assert.Null(Field(iterator, name));
        }
    }

    [Fact]
    public void NestedSubstitutionRetainsItsOwnLeaseWhenTheInnerIteratorExhausts()
    {
        var data = A("aT"); var view = View(data); PyRegexMatch? saved = null;
        var callback = PyNativeFunctions.CreateVariadic("replacement", _ =>
        {
            Locked(view); var iterator = Create("a", view); Locked(view, 2); saved = Next(iterator); Locked(view, 2);
            Assert.False(iterator.MoveNext()); Locked(view); data.SetItem(1L, 90L); return B("!");
        });
        Assert.Equal("!Z", Text(Call(P("a"), "sub", callback, view, 1L))); view.Release(); data.Append(33L);
        Assert.Same(view, saved!.GetAttr("string")); Assert.Throws<PyTypeError>(() => Call(saved, "group"));
    }

    [Fact]
    public void ConstructionFailureHasNoExportAndPublicFinditerUsesTheFactory()
    {
        var data = A("a"); var view = View(data); view.Release();
        Assert.Equal("expected string or bytes-like object, got 'memoryview'", Assert.Throws<PyTypeError>(() => Create("a", view, 2L, 1L)).PyMessage);
        data.Append(33L);
        var direct = Assert.IsAssignableFrom<IPyIterator>(Call(P("a"), "finditer", data));
        var module = Assert.IsAssignableFrom<IPyIterator>(PyOps.CallValue(PyRegex.CreateAttributes()["finditer"], [B("a"), data], [], []));
        foreach (var iterator in new[] { direct, module })
        { Assert.Equal("(0, 1)", Span(Next(iterator))); Assert.False(iterator.MoveNext()); }
        Assert.Equal("X!", Text(Call(P("a"), "sub", B("X"), data)));
    }

    [Fact]
    public void PublicIndicesRunBeforeAcquireAndFreezeTheResultingWindow()
    {
        var data = A("a"); var events = new List<string>();
        var start = Index(() => { events.Add("pos"); data.Append(97L); return 0L; });
        var end = Index(() => { events.Add("end"); data.Append(98L); return 99L; });
        var iterator = Assert.IsAssignableFrom<IPyIterator>(Call(P("a"), "finditer", data, start, end));
        Assert.Equal(new[] { "pos", "end" }, events); Assert.Throws<PyBufferError>(() => data.Append(33L));
        var first = Next(iterator); Assert.Equal(0L, first.GetAttr("pos")); Assert.Equal(3L, first.GetAttr("endpos"));
        Assert.Equal("(0, 1)", Span(first)); Assert.Equal("(1, 2)", Span(Next(iterator)));
        Assert.False(iterator.MoveNext()); Assert.Equal(new[] { "pos", "end" }, events); data.Append(33L);
        var releasedData = A("a"); var view = View(releasedData); events.Clear();
        Assert.Equal("expected string or bytes-like object, got 'memoryview'", Assert.Throws<PyTypeError>(() =>
            Call(P("a"), "finditer", view, Index(() => { events.Add("pos"); view.Release(); return 0L; }),
                Index(() => { events.Add("end"); releasedData.Append(97L); return 99L; }))).PyMessage);
        Assert.Equal(new[] { "pos", "end" }, events); releasedData.Append(33L);
    }

    [Fact]
    public void PublicBindingIndexAndCompileFailuresPrecedeAnyNewExport()
    {
        var data = A("a"); var pattern = P("a"); var events = new List<string>();
        var start = Index(() => { events.Add("pos"); return 0L; });
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(pattern.GetAttr("finditer"), [data, start], ["unknown"], [1L]));
        Assert.Empty(events); data.Append(33L);
        var marker = new PyRuntimeError("pos before end");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Call(pattern, "finditer", data,
            Index(() => { events.Add("bad-pos"); throw marker; }), Index(() => { events.Add("end"); return 99L; }))));
        Assert.Equal(new[] { "bad-pos" }, events); data.Append(33L); events.Clear();
        Assert.Throws<PyTypeError>(() => Call(pattern, "finditer", data, start, Index(() => { events.Add("bad-end"); return "bad"; })));
        Assert.Equal(new[] { "pos", "bad-end" }, events); data.Append(33L);
        Assert.Equal("cannot process flags argument with a compiled pattern", Assert.Throws<PyValueError>(() =>
            PyOps.CallValue(PyRegex.CreateAttributes()["finditer"], [pattern, data, 2L], [], [])).PyMessage);
        data.Append(33L);
    }

}
