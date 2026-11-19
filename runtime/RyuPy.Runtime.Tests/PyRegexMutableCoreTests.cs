// mutable regex阶段A内部专项：原对象身份、按访问时捕获、二级导出与确定性收尾；不测GC时机。
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexMutableCoreTests
{
    private static readonly Type BufferType = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBufferSubject")!;
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyByteArray A(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyMemoryView View(object source) => (PyMemoryView)PyMemoryView.Create([source], [], []);
    private static object Invoke(Type type, string name, object? owner, params object?[] args)
    {
        try
        {
            return type.GetMethod(name, BindingFlags.NonPublic |
                (owner is null ? BindingFlags.Static : BindingFlags.Instance))!.Invoke(owner, args)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static object Apply(PyRegexPattern pattern, object source, string method = "search", long start = 0, long end = long.MaxValue) =>
        Invoke(typeof(PyRegexPattern), "ApplyMutableBytes", pattern, method, source, start, end);
    private static PyRegexMatch Match(object source, string pattern = "(?P<x>bc)(z)?", string method = "search") =>
        Assert.IsType<PyRegexMatch>(Apply(PyRegex.Compile(B(pattern)), source, method));
    private static object? Call(PyNativeObject owner, string name, params object?[] args) => PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static byte[] Bytes(object? result) => Assert.IsType<PyBytes>(result).Bytes;
    private static object Acquire(object source) => Invoke(BufferType, "Acquire", null, source);
    private static void Dispose(object lease) => ((IDisposable)lease).Dispose();
    private static void FinalizeView(PyMemoryView view) => Invoke(typeof(PyMemoryView), "ReleaseOwnedBuffer", view, true);
    private static PyInstance Index(Func<object?> action)
    {
        var cls = new PyClass("RegexIndex");
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }

    [Fact]
    public void EagerInputsRetainIdentityAndUseViewRelativeByteOffsets()
    {
        var data = A("!a\u0080b?"); var whole = View(data); var chars = (PyMemoryView)Call(whole, "cast", "c")!;
        var slice = (PyMemoryView)whole.GetItem(new PySlice(1L, 4L, 1L)); var readOnly = View(B("!a\u0080b?"));
        try
        {
            foreach (object source in new object[] { data, whole, chars, slice, readOnly })
            {
                var pattern = PyRegex.Compile(B("(?P<x>a)(\u0080)(z)?"));
                var match = Assert.IsType<PyRegexMatch>(Apply(pattern, source));
                Assert.Same(source, match.GetAttr("string")); Assert.Same(pattern, match.GetAttr("re"));
                Assert.Equal(B("a\u0080").Bytes, Bytes(Call(match, "group")));
                Assert.Equal(source == slice ? "(0, 2)" : "(1, 3)", PyRepr.Repr(Call(match, "span")));
                Assert.Equal(source == slice ? 3L : 5L, match.GetAttr("endpos"));
                Assert.Equal("[(b'a', b'\\x80', b'')]", PyRepr.Repr(Apply(pattern, source, "findall")));
                foreach (string operation in new[] { "match", "fullmatch" })
                {
                    var full = Match(source, ".*", operation);
                    Assert.Equal(source == slice ? B("a\u0080b").Bytes : B("!a\u0080b?").Bytes, Bytes(Call(full, "group")));
                    Assert.NotSame(source, Call(full, "group"));
                }
            }
        }
        finally { whole.Release(); chars.Release(); slice.Release(); readOnly.Release(); }
        data.Append(33L);
    }

    [Fact]
    public void RetainedMatchReadsCurrentBytesButNeverRetainsHostSnapshotOrLease()
    {
        var data = A("abcd"); var match = Match(data); var saved = Bytes(Call(match, "group"));
        Assert.Null(typeof(PyRegexMatch).GetField("_raw", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(match));
        data.SetItem(1L, 88L);
        Assert.Equal(B("Xc").Bytes, Bytes(Call(match, "group"))); Assert.Equal(B("bc").Bytes, saved);
        Assert.Equal(B("<Xc>").Bytes, Bytes(Call(match, "expand", B(@"<\1>"))));
        data.Append(33L); data.DelItem(new PySlice(2L, PyNone.Instance, 1L));
        Assert.Equal(B("X").Bytes, Bytes(Call(match, "group")));
        data.DelItem(new PySlice(PyNone.Instance, PyNone.Instance, 1L));
        Assert.Empty(Bytes(Call(match, "group"))); Assert.IsType<PyNone>(Call(match, "group", 2L));
        data.Extend(B("12345"));
        Assert.Equal(B("23").Bytes, Bytes(Call(match, "group")));
        Assert.Equal("(1, 3)", PyRepr.Repr(Call(match, "span"))); Assert.Equal(4L, match.GetAttr("endpos"));
    }

    [Fact]
    public void ReleasedViewOnlyBlocksParticipatingCapturesAndTheirConsumers()
    {
        var data = A("ab"); var view = View(data); var match = Match(view, "(a)(z)?");
        view.Release();
        Assert.Same(view, match.GetAttr("string")); Assert.Equal("(0, 1)", PyRepr.Repr(Call(match, "span")));
        Assert.IsType<PyNone>(Call(match, "group", 2L));
        Assert.Equal(B("literal").Bytes, Bytes(Call(match, "expand", B("literal"))));
        Assert.Equal(B("<>").Bytes, Bytes(Call(match, "expand", B(@"<\2>"))));
        foreach (Action action in new Action[] { () => Call(match, "group"), () => match.PythonRepr(),
            () => Call(match, "expand", B(@"<\1>")), () => Apply(PyRegex.Compile(B("a")), view) })
            Assert.Equal("expected string or bytes-like object, got 'memoryview'", Assert.Throws<PyTypeError>(action).PyMessage);
        data.Append(33L);
    }

    [Fact]
    public void SuccessfulEmptyCaptureStillChecksReleasedView()
    {
        var view = View(B("a")); var match = Match(view, "()"); view.Release();
        Assert.Throws<PyTypeError>(() => Call(match, "group", 1L));
        Assert.Equal("(0, 0)", PyRepr.Repr(Call(match, "span", 1L)));
    }

    [Fact]
    public void GroupIndicesReadAfterEachProtocolAndPropagateOriginalExceptions()
    {
        var data = A("ab"); var match = Match(data, "(a)(b)");
        var second = Index(() => { data.SetItem(0L, 88L); return 2L; });
        Assert.Equal("(b'a', b'b', b'X')", PyRepr.Repr(Call(match, "group", 1L, second, 1L)));
        data = A("abcd"); match = Match(data);
        var shrink = Index(() => { data.DelItem(new PySlice(2L, PyNone.Instance, 1L)); return 1L; });
        Assert.Equal(B("b").Bytes, Bytes(Call(match, "group", shrink)));
        var marker = new PyValueError("original-index");
        Assert.Same(marker, Assert.Throws<PyValueError>(() => Call(match, "group", Index(() => throw marker))));
        data.Append(33L);
        var view = View(A("a")); var viewed = Match(view, "(a)");
        Assert.Throws<PyTypeError>(() => Call(viewed, "group", Index(() => { view.Release(); return 1L; })));
    }

    [Fact]
    public void MissingGroupsPreserveDefaultAndNamesWithoutAcquiringReleasedBuffer()
    {
        var view = View(B("b")); var match = Match(view, "(?P<x>a)?b"); view.Release();
        var missing = new PyList();
        Assert.Same(missing, Assert.Single(Assert.IsType<PyTuple>(Call(match, "groups", missing)).Items));
        Assert.Same(missing, Assert.IsType<PyDict>(Call(match, "groupdict", missing)).Get("x"));
        Assert.Equal("(-1, -1)", PyRepr.Repr(Call(match, "span", "x")));
    }

    [Fact]
    public void EagerNoMatchEmptyAndClampedRangesReleaseTheirLeases()
    {
        var pattern = PyRegex.Compile(B("a"));
        foreach (string method in new[] { "search", "match", "fullmatch", "findall" })
        foreach ((string text, long start, long end) in new[] { ("b", 0L, 99L), ("", 0L, 0L), ("aaa", 2L, 1L) })
        {
            var data = A(text); var view = View(data);
            object result = Apply(pattern, view, method, start, end);
            if (method == "findall") Assert.Empty(Assert.IsType<PyList>(result).Items);
            else Assert.IsType<PyNone>(result);
            view.Release(); data.Append(33L);
        }
        var source = A("ab"); var selected = Assert.IsType<PyRegexMatch>(Apply(PyRegex.Compile(B(".")), source, "search", -99, 1));
        Assert.Equal(0L, selected.GetAttr("pos")); Assert.Equal(1L, selected.GetAttr("endpos"));
    }

    [Fact]
    public void EagerFindallRetainsPythonEmptyThenNonemptyAdvance()
    {
        var source = A("ab");
        Assert.Equal("[b'', b'a', b'', b'']", PyRepr.Repr(Apply(PyRegex.Compile(B("|a")), source, "findall")));
        source.Append(33L);
    }

    [Fact]
    public void ViewExportsAreSpecificAndReleaseFailureKeepsTheViewAlive()
    {
        foreach (object owner in new object[] { A("ab"), B("ab") })
        {
            var view = View(owner); object one = Acquire(view), two = Acquire(view);
            Assert.Equal("memoryview has 2 exported buffers", Assert.Throws<PyBufferError>(view.Release).PyMessage);
            Assert.Equal(B("ab").Bytes, view.CopyBuffer());
            Dispose(one); Dispose(one);
            Assert.Equal("memoryview has 1 exported buffer", Assert.Throws<PyBufferError>(view.Release).PyMessage);
            Dispose(two); view.Release(); view.Release();
        }
        var data = A("!ab?"); var parent = View(data); var child = (PyMemoryView)parent.GetItem(new PySlice(1L, 3L, 1L));
        object lease = Acquire(child); parent.Release();
        Assert.Throws<PyBufferError>(child.Release); data.SetItem(2L, 90L);
        Assert.Equal(B("aZ").Bytes, Bytes(Invoke(lease.GetType(), "CopyRange", lease, 0, 2)));
        Dispose(lease); child.Release(); data.Append(33L);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EitherFinalizerCleanupOrderReleasesOnlyItsOwnExporterLease(bool viewFirst)
    {
        var data = A("ab"); var view = View(data); object lease = Acquire(view);
        if (viewFirst)
        {
            FinalizeView(view); FinalizeView(view);
            Assert.Throws<PyBufferError>(() => data.Append(33L));
            Assert.Equal(B("ab").Bytes, Bytes(Invoke(lease.GetType(), "CopyRange", lease, 0, 2)));
            Dispose(lease); Dispose(lease);
        }
        else
        {
            Dispose(lease); Dispose(lease);
            Assert.Throws<PyBufferError>(() => data.Append(33L));
            FinalizeView(view); FinalizeView(view);
        }
        data.Append(33L); view.Release();
        var fresh = View(data); Assert.Throws<PyBufferError>(() => data.Append(33L)); fresh.Release(); data.Append(33L);
    }

    [Fact]
    public void FailedAcquisitionAndExceptionScopeDoNotLeaveOrDoubleReleaseCounters()
    {
        var data = A("ab"); var view = View(data); view.Release();
        Assert.Throws<PyTypeError>(() => Acquire(view)); data.Append(33L);
        var fresh = View(data); var marker = new InvalidOperationException("controlled lease scope");
        Assert.Same(marker, Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using var lease = (IDisposable)Acquire(fresh); throw marker;
        })));
        fresh.Release(); data.Append(33L);
    }

    [Fact]
    public void PartiallyInitializedFinalizableObjectsHaveNoCountersToRelease()
    {
        // 模拟字段初始化分配失败后的默认字段；不制造OOM，也不要求实际GC何时调finalizer。
        var partialView = (PyMemoryView)RuntimeHelpers.GetUninitializedObject(typeof(PyMemoryView));
        FinalizeView(partialView); FinalizeView(partialView);
        Type leaseType = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBufferLease")!;
        var partialLease = (IDisposable)RuntimeHelpers.GetUninitializedObject(leaseType);
        partialLease.Dispose(); partialLease.Dispose();
        GC.SuppressFinalize(partialView); GC.SuppressFinalize(partialLease);
    }

    [Fact]
    public void ExportedViewReleaseOwnErrorPreservesCallerFrameAndAmbientContext()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        var view = View(B("a")); object lease = Acquire(view);
        try
        {
            PyTraceback.PushFrame("mutable_regex.py", "caller"); PyTraceback.SetLine(17);
            var error = Assert.Throws<PyBufferError>(() => Call(view, "release"));
            Assert.Same(ambient, error.Context); Assert.Same(ambient, PyErrors.Current);
            Assert.Equal(new PyStackFrame("mutable_regex.py", "caller", 17), Assert.Single(error.Frames));
            Assert.Equal(1L, PyTraceback.CaptureDepth()); Assert.Equal(B("a").Bytes, view.CopyBuffer());
        }
        finally { Dispose(lease); view.Release(); PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void StageAEagerHelperRejectsSubstitutionAndDeferredMethodsBeforeAcquiringAnyBuffer()
    {
        var data = A("a"); var view = View(data); var pattern = PyRegex.Compile(B("a"));
        foreach (string method in new[] { "finditer", "sub", "subn" })
            Assert.Throws<PyNotImplementedError>(() => Apply(pattern, view, method));
        view.Release(); data.Append(33L);
    }
}
