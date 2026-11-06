// 正则解析器Unsupported冷现场：锁原拒绝条件/消息、现有帧/context与用户异常透传，不扩支持语法。
using System.Text;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexUnsupportedErrorTests
{
    private const string GroupFeature = "lookbehind, atomic, conditional, comment or scoped flags";
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static object? Call(PyNativeObject receiver, string name, params object?[] args) =>
        PyOps.CallValue(receiver.GetAttr(name), args, [], []);

    // 每个测试拥有两层现场并恢复旧ambient；禁止测试与其它进程级traceback测试并行。
    private sealed class Scene : IDisposable
    {
        private readonly long _depth = PyTraceback.CaptureDepth();
        private readonly object? _token;
        internal readonly PyValueError Ambient = new("ambient");
        internal Scene()
        {
            Assert.Equal(0L, _depth);
            _token = PyErrors.EnterHandler(Ambient);
            PyTraceback.PushFrame("outer_regex.py", "outer"); PyTraceback.SetLine(13);
            PyTraceback.PushFrame("inner_regex.py", "leaf"); PyTraceback.SetLine(29);
        }
        internal void CheckLive()
        {
            Assert.Same(Ambient, PyErrors.Current);
            Assert.Equal(_depth + 2, PyTraceback.CaptureDepth());
        }
        public void Dispose() { PyErrors.LeaveHandler(_token); PyTraceback.PopTo(_depth + 1); }
    }

    private static void CheckError(PyNotImplementedError error, string feature, Scene scene)
    {
        Assert.Equal("NotImplementedError", error.PyTypeName);
        Assert.Equal("re: unsupported " + feature, error.PyMessage);
        Assert.Equal(error.PyMessage, Assert.Single(error.Args));
        Assert.Same(scene.Ambient, error.Context);
        Assert.Null(error.Cause); Assert.False(error.SuppressContextFlag);
        // 这里不将CLR帮助方法伪装成Python帧；当前最内两帧逐字段锁定。
        Assert.Equal(2, error.Frames.Length);
        Assert.Equal(new PyStackFrame("inner_regex.py", "leaf", 29), error.Frames[0]);
        Assert.Equal(new PyStackFrame("outer_regex.py", "outer", 13), error.Frames[1]);
        scene.CheckLive();
    }

    [Theory]
    [InlineData("a++", 0, "possessive quantifier", true)]
    [InlineData("(?P<x>a)(?P=x)", 2, "case-insensitive backreference", true)]
    [InlineData("(?<=a)b", 0, "lookbehind", true)]
    [InlineData("(?<!a)b", 0, "lookbehind", true)]
    [InlineData("(?>a)", 0, GroupFeature, true)]
    [InlineData("(a(?(1)b|c))", 0, "conditional reference to an open group", true)]
    [InlineData("(?#x)a", 0, GroupFeature, true)]
    [InlineData("(?i:a)", 0, GroupFeature, true)]
    [InlineData("(?-i:a)", 0, GroupFeature, true)]
    [InlineData("a(?i)b", 0, GroupFeature, true)]
    [InlineData(@"(a)\1", 2, "case-insensitive backreference", true)]
    [InlineData(@"\N{LATIN SMALL LETTER A}", 0, "named Unicode escape", false)]
    [InlineData(@"[\1]", 0, "octal character class escape", true)]
    [InlineData("(?P<é>a)", 0, "non-ASCII group name", false)]
    [InlineData("(?P<a-b>x)", 0, "non-ASCII group name", false)]
    public void EveryParserRejectionKeepsItsMessageAndCurrentCaller(
        string expression, int flags, string feature, bool bytesShareRejection)
    {
        using var scene = new Scene();
        CheckError(Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(expression, (long)flags)), feature, scene);
        if (bytesShareRejection)
            CheckError(Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(B(expression), (long)flags)), feature, scene);
    }

    [Theory]
    [InlineData(2174)]
    [InlineData(2175)]
    public void TranslationSizeErrorsArePreparedAtBothExistingChecks(int length)
    {
        // 当前单个ASCII a译文92字符：2174个在循环后超限，2175个在下一次循环前超限。
        using var scene = new Scene();
        string expression = new('a', length);
        CheckError(Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(expression)), "translated pattern size limit", scene);
        CheckError(Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(B(expression))), "translated pattern size limit", scene);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedPreparationPreservesFirstNonemptySnapshotAndContext(bool bytes)
    {
        using var scene = new Scene();
        object pattern = bytes ? B("(?i:a)") : "(?i:a)";
        var error = Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(pattern));
        CheckError(error, GroupFeature, scene);
        PyStackFrame[] snapshot = error.Frames;
        PyTraceback.SetLine(99);
        long extra = PyTraceback.PushFrame("later.py", "later"); PyTraceback.SetLine(101);
        var later = new PyValueError("later ambient"); object? token = PyErrors.EnterHandler(later);
        try
        {
            Assert.Same(error, PyErrors.PrepareRaise(error));
            Assert.Same(snapshot, error.Frames); Assert.Same(scene.Ambient, error.Context);
            Assert.Equal(new PyStackFrame("inner_regex.py", "leaf", 29), error.Frames[0]);
            Assert.Same(later, PyErrors.Current); Assert.Equal(extra, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.PopTo(extra); }
        scene.CheckLive();
    }

    [Fact]
    public void SuccessCacheHitAndFailureRecoveryLeaveLiveStateUnchanged()
    {
        using var scene = new Scene();
        var pattern = PyRegex.Compile("regex-cold-error-success"); scene.CheckLive();
        Assert.Same(pattern, PyRegex.Compile("regex-cold-error-success")); scene.CheckLive();
        CheckError(Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile("a++")), "possessive quantifier", scene);
        Assert.Same(pattern, PyRegex.Compile(pattern)); scene.CheckLive();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UserIndexAndReplacementFailuresAreNeverPreparedOrReplaced(bool prepared)
    {
        using var scene = new Scene();
        var marker = new PyTypeError("user marker");
        var prior = new PyValueError("prior context"); var cause = new PyLookupError("cause");
        if (prepared)
        {
            marker.Context = prior; marker.WithCause(cause);
            long depth = PyTraceback.PushFrame("user_callback.py", "callback"); PyTraceback.SetLine(41);
            PyErrors.PrepareRaise(marker); PyTraceback.PopTo(depth);
        }
        PyStackFrame[] frames = marker.Frames;
        var cls = new PyClass("Flags");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("__index__", _ => throw marker));
        Assert.Same(marker, Assert.Throws<PyTypeError>(() => PyRegex.Compile("a", new PyInstance(cls))));
        var callback = PyNativeFunctions.CreateVariadic("replacement", _ => throw marker);
        Assert.Same(marker, Assert.Throws<PyTypeError>(() => Call(PyRegex.Compile("a"), "sub", callback, "a")));
        Assert.Same(marker, Assert.Throws<PyTypeError>(() => Call(PyRegex.Compile(B("a")), "sub", callback, B("a"))));
        Assert.Same(frames, marker.Frames);
        Assert.Same(prepared ? prior : null, marker.Context);
        Assert.Same(prepared ? cause : null, marker.Cause);
        Assert.Equal(prepared, marker.SuppressContextFlag);
        if (!prepared) Assert.Empty(marker.Frames);
        scene.CheckLive();
    }
}
