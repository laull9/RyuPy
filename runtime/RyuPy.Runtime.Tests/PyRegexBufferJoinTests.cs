// F1晚Join专项：原引用读取、buffer导出覆盖复制、确定性失败清理和原exact快路；不测GC/全局bytes驻留。
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexBufferJoinTests
{
    private static readonly Type BytesType = typeof(PyRegexPattern).Assembly.GetType("RyuPy.Runtime.PyRegexBytes")!;
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyByteArray A(string value) => new(Encoding.Latin1.GetBytes(value));
    private static PyRegexPattern P(string value) => PyRegex.Compile(B(value));
    private static PyMemoryView View(object source) => (PyMemoryView)PyMemoryView.Create([source], [], []);
    private static object? Call(PyNativeObject owner, string name, params object?[] args) => PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static object? Module(string name, params object?[] args) => PyOps.CallValue(PyRegex.CreateAttributes()[name], args, [], []);
    private static PyFunction Callback(Func<PyRegexMatch, object?> action) =>
        PyNativeFunctions.CreateVariadic("replace", args => action(Assert.IsType<PyRegexMatch>(Assert.Single(args))));
    private static string Text(object? value) => Encoding.Latin1.GetString(Assert.IsType<PyBytes>(value).Bytes);
    private static object Invoke(Type type, string method, object? owner, params object?[] args)
    {
        try { return type.GetMethod(method, BindingFlags.NonPublic | (owner is null ? BindingFlags.Static : BindingFlags.Instance))!.Invoke(owner, args)!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static PyBytes Join(List<object?> parts, PyBytes? empty = null) =>
        (PyBytes)Invoke(BytesType, "Join", null, parts, empty ?? B(""));
    private static PyBytes BufferJoin(List<object?> parts, Action action) =>
        (PyBytes)Invoke(BytesType, "JoinBufferParts", null, parts, B(""), action);
    private static int Exports(object owner) => (int)owner.GetType().GetField(
        owner is PyMemoryView ? "_regexExports" : "_exports", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static (object Result, PyByteArray Owner, PyMemoryView? View) Carrier(string kind, string content)
    {
        var data = A(kind == "slice" ? "!" + content + "?" : content);
        if (kind == "array") return (data, data, null);
        var view = View(kind.StartsWith("readonly", StringComparison.Ordinal) ? B(content) : data);
        if (kind is "c" or "readonly_c" or "slice")
        {
            var parent = view;
            view = kind == "slice" ? (PyMemoryView)parent.GetItem(new PySlice(1L, (long)data.Count - 1, 1L))
                : (PyMemoryView)Call(parent, "cast", "c")!;
            parent.Release();
        }
        return (view, data, view);
    }

    [Fact]
    public void AllExistingCarriersReturnBytesForExactAndMutableSubjects()
    {
        foreach (string kind in new[] { "array", "B", "c", "slice", "readonly_B", "readonly_c" })
        {
            var carrier = Carrier(kind, "X\u0080");
            try
            {
                foreach (object source in new object[] { B("a"), A("a") })
                {
                    object? output = Call(P("a"), "sub", Callback(_ => carrier.Result), source);
                    Assert.Equal("X\u0080", Text(output)); Assert.NotSame(carrier.Result, output);
                }
            }
            finally { carrier.View?.Release(); }
            carrier.Owner.Append(33L);
        }
    }

    [Fact]
    public void RepeatedMutableReferencesAreReadAfterAllCallbacks()
    {
        foreach (bool growing in new[] { false, true })
        foreach (bool mutableSubject in new[] { false, true })
        {
            var shared = A("X"); int calls = 0; object source = mutableSubject ? A("aa") : B("aa");
            var result = Assert.IsType<PyTuple>(Call(P("a"), "subn", Callback(_ =>
            {
                calls++; Assert.Equal(0, Exports(shared));
                if (growing) shared.Append(89L); else shared.SetItem(0L, 65L + calls);
                return shared;
            }), source));
            Assert.Equal(growing ? "XYYXYY" : "CC", Text(result.Items[0])); Assert.Equal(2L, result.Items[1]);
            Assert.Equal(2, calls); Assert.Equal(0, Exports(shared)); shared.Append(33L);
        }
    }

    [Fact]
    public void CallbackSubjectAliasIsReadAfterSourceLeaseEnds()
    {
        var data = A("aa"); var view = View(data); int calls = 0;
        object? output = Call(P("a"), "sub", Callback(match =>
        {
            calls++; Assert.Same(data, match.GetAttr("string")); Assert.Equal(0, Exports(view));
            data.SetItem(calls - 1, calls == 1 ? 120L : 121L); return view;
        }), data);
        Assert.Equal("xyxy", Text(output)); Assert.Equal(2, calls); Assert.Equal(0, Exports(view));
        Assert.Equal(1, Exports(data)); view.Release(); data.Append(33L);
    }

    [Fact]
    public void CollectedPartsReleaseSubjectLeaseBeforeResultAcquisition()
    {
        var data = A("aa"); var view = View(data);
        var collected = ((List<object?> Parts, long Changed))Invoke(typeof(PyRegexPattern),
            "CollectMutableSubstitutionParts", P("a"), Callback(_ => view), view, 0L, true, null, null);
        Assert.Equal(2L, collected.Changed); Assert.Equal(0, Exports(view)); Assert.Equal(1, Exports(data));
        Assert.All(collected.Parts, part => Assert.Same(view, part));
        Assert.Equal("aaaa", Text(BufferJoin(collected.Parts, () =>
        { Assert.Equal(2, Exports(view)); Assert.Equal(3, Exports(data)); })));
        Assert.Equal(0, Exports(view)); view.Release(); data.Append(33L);
    }

    [Fact]
    public void ReleasedResultsFailAtTheActualLateJoinIndex()
    {
        foreach (bool priorWrongType in new[] { false, true })
        {
            var returned = View(B("X")); var source = A(priorWrongType ? "aa" : "paa"); int calls = 0;
            var error = Assert.Throws<PyTypeError>(() => Call(P("a"), "sub", Callback(_ =>
            {
                if (++calls == 2) returned.Release();
                return priorWrongType && calls == 1 ? "bad" : returned;
            }), source));
            Assert.Equal(priorWrongType ? "sequence item 0: expected a bytes-like object, str found"
                : "sequence item 1: expected a bytes-like object, memoryview found", error.PyMessage);
            Assert.Equal(2, calls); Assert.Null(error.Context); source.Append(33L);
        }
    }

    [Fact]
    public void LaterUserExceptionWinsOverEarlierReleasedResult()
    {
        var data = A("aa"); var subject = View(data); var returned = View(B("X")); returned.Release();
        var marker = new PyRuntimeError("later callback"); int calls = 0;
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Call(P("a"), "sub", Callback(_ =>
        { if (++calls == 2) throw marker; return returned; }), subject)));
        Assert.Equal(2, calls); subject.Release(); data.Append(33L);
    }

    [Fact]
    public void EveryBufferLeaseSurvivesUntilCopyAndDuplicateViewsRemainIndependent()
    {
        var data = A("ab"); var view = View(data); int checkpoints = 0;
        PyBytes output = BufferJoin([B("<"), data, view, view, B(">")], () =>
        {
            checkpoints++; Assert.Equal(4, Exports(data)); Assert.Equal(2, Exports(view));
            Assert.Throws<PyBufferError>(() => data.Append(33L));
            // 仅内部检查点改值以验证CopyTo仍读取lease，不表示公开Join会调用用户代码。
            data.SetItem(0L, 90L);
        });
        Assert.Equal("<ZbZbZb>", Text(output)); Assert.Equal(1, checkpoints);
        Assert.Equal(1, Exports(data)); Assert.Equal(0, Exports(view)); view.Release(); data.Append(33L);
    }

    [Fact]
    public void FailedAcquisitionReleasesAllEarlierBufferExports()
    {
        var data = A("X"); var good = View(data); var deadOwner = A("Y"); var dead = View(deadOwner); dead.Release();
        Assert.Equal("sequence item 2: expected a bytes-like object, memoryview found",
            Assert.Throws<PyTypeError>(() => Join([data, good, dead])).PyMessage);
        Assert.Equal(1, Exports(data)); Assert.Equal(0, Exports(good)); Assert.Equal(0, Exports(deadOwner));
        good.Release(); data.Append(33L); deadOwner.Append(33L);
    }

    [Fact]
    public void OrdinaryBadPartRetainsItsPriorityAndUnwindsEarlierExports()
    {
        var data = A("X"); var dead = View(B("Y")); dead.Release();
        Assert.Equal("sequence item 1: expected a bytes-like object, int found",
            Assert.Throws<PyTypeError>(() => Join([data, 7L, dead])).PyMessage);
        Assert.Equal(0, Exports(data));
        Assert.Equal("sequence item 0: expected a bytes-like object, str found",
            Assert.Throws<PyTypeError>(() => Join(["bad", data, dead])).PyMessage);
        Assert.Equal(0, Exports(data)); data.Append(33L);
    }

    [Fact]
    public void ControlledFailureAfterAcquisitionPropagatesAndClearsEveryLease()
    {
        var data = A("X"); var view = View(data); var marker = new InvalidOperationException("copy checkpoint");
        Assert.Same(marker, Assert.Throws<InvalidOperationException>(() => BufferJoin([data, view, view], () =>
        { Assert.Equal(4, Exports(data)); Assert.Equal(2, Exports(view)); throw marker; })));
        Assert.Equal(1, Exports(data)); Assert.Equal(0, Exports(view));
        view.Release(); view.Release(); data.Append(33L);
        var fresh = View(data); Assert.Equal(1, Exports(data)); fresh.Release(); Assert.Equal(0, Exports(data));
    }

    [Fact]
    public void EmptyMutablePiecesAreValidatedAndUseOnlyTheProvidedLocalEmpty()
    {
        var empty = B("");
        foreach (string kind in new[] { "array", "B", "c", "slice", "readonly_B", "readonly_c" })
        {
            var carrier = Carrier(kind, "");
            Assert.Same(empty, Join([carrier.Result], empty)); Assert.NotSame(carrier.Result, empty);
            carrier.View?.Release(); carrier.Owner.Append(33L);
        }
        var dead = View(B("")); dead.Release();
        Assert.Equal("sequence item 0: expected a bytes-like object, memoryview found",
            Assert.Throws<PyTypeError>(() => Join([dead], empty)).PyMessage);
        // 这里核内部empty参数，不替换公共CP测试的字面b''，也不承诺全局bytes驻留。
        Assert.Same(empty, Join([], empty));
    }

    [Fact]
    public void ExactByteFastPathKeepsSinglePieceIdentityAndMixedStringError()
    {
        var exact = B("only"); Assert.Same(exact, Join([exact])); Assert.Equal("only!", Text(Join([exact, B("!")])));
        Assert.Same(exact, Call(P("a"), "sub", Callback(_ => exact), B("a")));
        Assert.Equal("sequence item 1: expected a bytes-like object, str found",
            Assert.Throws<PyTypeError>(() => Join([exact, "bad"])).PyMessage);
        Assert.Equal("sequence item 1: expected str instance, bytes found",
            Assert.Throws<PyTypeError>(() => Call(P("(a)"), "sub", @"\1", B("a"))).PyMessage);
    }

    [Fact]
    public void JoinOwnErrorPreservesAmbientContextAndCallerFrameWithoutRawViewError()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        var data = A("X"); var dead = View(B("Y")); dead.Release();
        try
        {
            PyTraceback.PushFrame("replacement.py", "join_caller"); PyTraceback.SetLine(23);
            var error = Assert.Throws<PyTypeError>(() => Join([data, dead]));
            Assert.Same(ambient, error.Context); Assert.Same(ambient, PyErrors.Current);
            Assert.Equal(new PyStackFrame("replacement.py", "join_caller", 23), Assert.Single(error.Frames));
            Assert.Equal(0, Exports(data)); data.Append(33L);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void ModuleAndSavedMethodsShareResultsAndKeepViewTemplateBoundaries()
    {
        var data = A("X"); var callback = Callback(_ => data); var pattern = P("a");
        Assert.Equal("XX", Text(Module("sub", B("a"), callback, B("aa"))));
        object? saved = pattern.GetAttr("subn");
        Assert.Equal("(b'XX', 2)", PyRepr.Repr(PyOps.CallValue(saved, [callback, A("aa")], [], [])));
        Assert.Equal("X", Text(Call(pattern, "sub", data, B("a"))));
        var view = View(B("X"));
        try
        {
            Assert.Throws<PyNotImplementedError>(() => Call(pattern, "sub", view, B("a")));
            var match = Assert.IsType<PyRegexMatch>(Call(pattern, "search", B("a")));
            Assert.Equal("X", Text(Call(match, "expand", data)));
            Assert.Throws<PyNotImplementedError>(() => Call(match, "expand", view));
            Assert.Equal("X", Text(Module("escape", data)));
            Assert.Equal("X", Text(Module("escape", view)));
        }
        finally { view.Release(); }
        data.Append(33L);
    }
}
