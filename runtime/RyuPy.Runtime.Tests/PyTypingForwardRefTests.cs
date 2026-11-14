// ForwardRef阶段A只验内部真实值、有限语法与准备错误；不是公开typing或CP/JIT应用通过。
// 测试侧反射访问internal入口，不为测试增加Python导出或生产反射路径。
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingForwardRefTests
{
    private static readonly Type Implementation = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyTypingForwardRef")!;
    private static object Enter(string method, params object?[] arguments)
    {
        try
        {
            return Implementation.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, arguments)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
    private static object Create(string value) => Enter("Create", value);
    private static object Invoke(object?[] args, string[]? names = null, object?[]? values = null) =>
        Enter("Invoke", args, names ?? [], values ?? []);
    private static object? Get(object value, string name) => PyObject.GetAttr(value, name);
    private static object? Method(object value, string name, params object?[] args) =>
        PyOps.CallValue(Get(value, name), args, [], []);
    private static bool IsInstance(object value, object type) => (bool)PyOps.CallBuiltin("isinstance", [value, type], [], [])!;

    [Fact]
    public void DefaultMetadataPreservesTheOriginalExpressionWithoutEvaluation()
    {
        string source = new string(new[] { 'M', 'i', 's', 's', 'i', 'n', 'g' });
        object value = Create(source);
        Assert.Same(source, Get(value, "__forward_arg__"));
        Assert.Same(PyNone.Instance, Get(value, "__forward_module__"));
        Assert.Equal(true, Get(value, "__forward_is_argument__"));
        Assert.Equal(false, Get(value, "__forward_is_class__"));
        Assert.Equal(false, Get(value, "__forward_evaluated__"));
        Assert.Same(PyNone.Instance, Get(value, "__forward_value__"));
        Assert.Equal("ForwardRef('Missing')", PyRepr.Repr(value));
        Assert.Equal(PyRepr.Repr(value), PyRepr.Str(value));
    }

    [Fact]
    public void FlagsRetainTheirRealValuesInsteadOfCallingTruth()
    {
        var argumentFlag = new PyList([1L]);
        var classFlag = new PyDict();
        object value = Invoke(["Thing", argumentFlag], ["is_class"], [classFlag]);
        Assert.Same(argumentFlag, Get(value, "__forward_is_argument__"));
        Assert.Same(classFlag, Get(value, "__forward_is_class__"));
        argumentFlag.Append(2L);
        Assert.Same(argumentFlag, Get(value, "__forward_is_argument__"));
    }

    [Fact]
    public void NativeTypeIsStableAndRealButNeitherTypingEntryIsExported()
    {
        object first = Create("Thing"), second = Create("Other");
        var type = Assert.IsType<PyNativeType>(PyObject.TypeOf(first));
        Assert.Same(type, PyObject.TypeOf(second));
        Assert.Same(type, Get(first, "__class__"));
        Assert.Same(PyType.Get("type"), PyObject.TypeOf(type));
        Assert.Equal("ForwardRef", type.Name);
        Assert.Equal("typing", type.Module);
        Assert.True(IsInstance(first, type));
        Assert.True(IsInstance(first, PyBaseObject.Type));
        Assert.False(IsInstance("Thing", type));
        Assert.False(IsInstance(first, PyType.Get("str")));
        Assert.Throws<PyNotImplementedError>(() => type.Invoke(["Thing"], [], []));
        Assert.False(PyTyping.CreateAttributes().ContainsKey("ForwardRef"));
        Assert.IsType<PyFunction>(PyTyping.CreateAttributes()["NamedTuple"]);
    }

    [Fact]
    public void FreshObjectsCompareAndHashByTextAndModuleNotFlags()
    {
        object first = Create("Thing");
        object second = Invoke(["Thing", false], ["is_class"], [true]);
        Assert.NotSame(first, second);
        Assert.True(PyHash.Equal(first, second));
        Assert.Equal(PyHash.Of(first), PyHash.Of(second));
        Assert.Equal(PyHash.Of(new PyTuple(["Thing", PyNone.Instance])), PyHash.Of(first));
        Assert.Equal(false, Method(first, "__ne__", second));
        Assert.Same(PyNotImplemented.Instance, Method(first, "__eq__", "Thing"));
        Assert.Same(PyNotImplemented.Instance, Method(first, "__ne__", "Thing"));
        var dictionary = new PyDict();
        dictionary.Set(first, 17L);
        Assert.Equal(17L, dictionary.Get(second));
    }

    [Fact]
    public void ModuleParticipatesInEqualityHashAndRepresentation()
    {
        object first = Invoke(["Thing", true, "other"]);
        object same = Invoke(["Thing"], ["module"], ["other"]);
        Assert.True(PyHash.Equal(first, same));
        Assert.False(PyHash.Equal(first, Create("Thing")));
        Assert.Equal("other", Get(first, "__forward_module__"));
        Assert.Equal("ForwardRef('Thing', module='other')", PyRepr.Repr(first));
        Assert.Equal(PyHash.Of(new PyTuple(["Thing", "other"])), PyHash.Of(first));
        Assert.Throws<PyNotImplementedError>(() => Invoke(["Thing", true, new PyList()]));
    }

    [Fact]
    public void SupportedNamesPathsAndTypeParametersOnlyValidateTheirSyntax()
    {
        foreach (string text in new[] { "Thing", "pkg.Thing", "_T_9", "type", "match", "case",
            "list[int]", "dict[str, list[int]]", "tuple[int, ...]", "Callable[[int, str], bool]",
            "Callable[[], None]", "A[B][C]", "A[B,]", "A | None", "pkg.A[B] | C[D]",
            "typing.Optional['Node']", "\"Thing\"", "''", "A[\"x\", 'y']", "A[ B,\tC ]" })
            Assert.Equal(text, Get(Create(text), "__forward_arg__"));
    }

    [Fact]
    public void KnownCpythonSyntaxErrorsAreExplicitlyUnsupportedNotFalseSuccess()
    {
        // 固定参考10中CP应SyntaxError；当前runtime无真实SyntaxError家族，因此保NI边界而非同名假异常。
        foreach (string text in new[] { "", "x y", "list[", "int;", "x = int", "**Ts" })
            Assert.Throws<PyNotImplementedError>(() => Create(text));
    }

    [Fact]
    public void LegalComplexExpressionsRemainUnsupportedWithoutEvaluation()
    {
        // 固定参考10合法域；NI不冒称它们在CP语法非法。
        foreach (string text in new[] { "(int, str)", "*Ts", "f()", "1 + 2", "lambda: Missing",
            "[x for x in Missing]", "None.foo", "Literal[1]", "r'Node'", "'N\\x6fde'", "'''Node'''" })
            Assert.Throws<PyNotImplementedError>(() => Create(text));
    }

    [Fact]
    public void TokenBoundariesAndUnsupportedWhitespaceNeverProducePartialSuccess()
    {
        foreach (string text in new[] { "Thing!", "Thing Name", "Thing\0", " Thing", "\tThing", "Thing\n",
            "A[\nB]", "A[]", "A[B,,C]", "A[B|]", "A[,B]", "A[B]C", "A[B]]", "A.None", "class", "A[if]",
            "Thing 'Other'", "'A''B'", "A[ ]" })
            Assert.Throws<PyNotImplementedError>(() => Create(text));
        Assert.Equal("Thing \t", Get(Create("Thing \t"), "__forward_arg__"));
    }

    [Fact]
    public void NonAsciiAndSurrogatesRemainUnsupportedNotInvalidIdentifiers()
    {
        foreach (string text in new[] { "变量", "pkg.Τύπος", "'节点'", "A\ud800", "A\udc00", "A\U0001F600" })
            Assert.Throws<PyNotImplementedError>(() => Create(text));
    }

    [Fact]
    public void BracketBoundaryCountsListsAndSubscriptsBeforePushing()
    {
        string depth200 = string.Concat(System.Linq.Enumerable.Repeat("A[", 200)) + "B" + new string(']', 200);
        Assert.Equal(depth200, Get(Create(depth200), "__forward_arg__"));
        string depth201 = "A[" + depth200 + "]";
        Assert.Contains("beyond 200", Assert.Throws<PyNotImplementedError>(() => Create(depth201)).PyMessage);
        string lists200 = "A[" + new string('[', 199) + "B" + new string(']', 200);
        Assert.Equal(lists200, Get(Create(lists200), "__forward_arg__"));
        Assert.Throws<PyNotImplementedError>(() => Create("A[" + new string('[', 200) + "B" + new string(']', 201)));
    }

    [Fact]
    public void LongBareNamesDoNotUseAnArbitraryLengthOrTokenCap()
    {
        string text = "T" + new string('x', 100_000);
        object value = Create(text);
        Assert.Same(text, Get(value, "__forward_arg__"));
        Assert.Equal(text, Get(Create(text), "__forward_arg__"));
    }

    [Fact]
    public void ReflectionAndMutationRejectWithoutChangingTheValue()
    {
        object value = Create("Thing");
        foreach (string name in new[] { "__forward_code__", "_evaluate", "__dict__", "__reduce__", "__or__" })
            Assert.Throws<PyNotImplementedError>(() => Get(value, name));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(value, "__forward_arg__", "Other"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "__forward_module__"));
        Assert.Equal("Thing", Get(value, "__forward_arg__"));
        Assert.Same(PyNone.Instance, Get(value, "__forward_module__"));
        Assert.Equal("ForwardRef('Thing')", Method(value, "__repr__"));
    }

    [Fact]
    public void InternalBinderPreservesDefaultsKeywordOnlyAndOwnErrors()
    {
        object value = Invoke([], ["arg"], ["Thing"]);
        Assert.Equal(true, Get(value, "__forward_is_argument__"));
        Assert.Throws<PyTypeError>(() => Invoke([]));
        Assert.Throws<PyTypeError>(() => Invoke(["Thing", true, PyNone.Instance, false]));
        Assert.Throws<PyTypeError>(() => Invoke(["Thing"], ["arg"], ["Other"]));
        Assert.Throws<PyTypeError>(() => Invoke(["Thing"], ["unknown"], [false]));
        Assert.Equal("Forward reference must be a string -- got 1", Assert.Throws<PyTypeError>(() => Invoke([1L])).PyMessage);
        Assert.Throws<PyTypeError>(() => Method(value, "__hash__", 1L));
        Assert.Throws<PyTypeError>(() => Method(value, "__eq__"));
    }

    [Fact]
    public void OwnedFailuresPrepareCallerAndAmbientWithoutCatchingUserErrors()
    {
        var ambient = new PyValueError("ambient");
        object? previous = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("forwardref_probe.py", "caller");
        PyTraceback.SetLine(23);
        try
        {
            object value = Create("Thing");
            var type = (PyNativeType)PyObject.TypeOf(value);
            foreach (Action action in new Action[] { () => Create("f()"), () => Invoke([]),
                () => Invoke([1L]), () => type.Invoke(["Thing"], [], []),
                () => PyObject.SetAttr(value, "x", 1L), () => PyObject.DelAttr(value, "x"),
                () => Method(value, "__hash__", 1L) })
            {
                PyBaseException error = Assert.ThrowsAny<PyBaseException>(action);
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("forwardref_probe.py", "caller", 23), error.Frames[0]);
            }
            var userError = new PyValueError("user repr");
            Assert.Same(userError, Assert.Throws<PyValueError>(() => Invoke([new BrokenRepr(userError)])));
            Assert.Empty(userError.Frames); // 自有入口不得把用户原错误包成调用者自己的TypeError/帧。
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    private sealed class BrokenRepr(PyValueError error) : PyNativeObject
    {
        public override string TypeName => "BrokenRepr";
        public override object? GetAttr(string name) => throw Missing(name);
        public override string PythonRepr() => throw error;
    }
}
