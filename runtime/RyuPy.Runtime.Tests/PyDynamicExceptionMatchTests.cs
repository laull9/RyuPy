// 动态except窄域合同：真实身份、全tuple校验、原生异常族与错误上下文，不依赖生产编译器接线。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyDynamicExceptionMatchTests
{
    private static readonly string[] NativeNames =
    [
        "BaseException",
        "Exception",
        "Warning",
        "UserWarning",
        "DeprecationWarning",
        "PendingDeprecationWarning",
        "RuntimeWarning",
        "SyntaxWarning",
        "FutureWarning",
        "ImportWarning",
        "UnicodeWarning",
        "BytesWarning",
        "ResourceWarning",
        "SystemExit",
        "KeyboardInterrupt",
        "GeneratorExit",
        "ArithmeticError",
        "ZeroDivisionError",
        "OverflowError",
        "LookupError",
        "IndexError",
        "KeyError",
        "ValueError",
        "JSONDecodeError",
        "error",
        "BadZipFile",
        "GetoptError",
        "OptParseError",
        "OptionError",
        "OptionConflictError",
        "OptionValueError",
        "BadOptionError",
        "AmbiguousOptionError",
        "UnicodeError",
        "UnicodeDecodeError",
        "UnicodeEncodeError",
        "TypeError",
        "NameError",
        "UnboundLocalError",
        "AttributeError",
        "FrozenInstanceError",
        "RuntimeError",
        "NotImplementedError",
        "RecursionError",
        "AssertionError",
        "ImportError",
        "ModuleNotFoundError",
        "OSError",
        "UnsupportedOperation",
        "IsADirectoryError",
        "NotADirectoryError",
        "FileNotFoundError",
        "FileExistsError",
        "PermissionError",
        "StopIteration",
        "StopAsyncIteration",
        "EOFError",
        "BufferError",
    ];

    private static PyBaseException Native(string name) => name switch
    {
        "JSONDecodeError" => new PyJSONDecodeError("bad", "", 0),
        "error" => new PyRegexError("bad", "", 0),
        "OptParseError" => new PyOptParseError("bad"),
        "OptionError" => new PyOptionError("bad", PyNone.Instance),
        "OptionConflictError" => new PyOptionConflictError("bad", PyNone.Instance),
        "OptionValueError" => new PyOptionValueError("bad"),
        "BadOptionError" => new PyBadOptionError("--bad"),
        "AmbiguousOptionError" => new PyAmbiguousOptionError("--bad", new PyTuple([])),
        _ => PyExceptionTypes.Create(name, "bad"),
    };

    [Fact]
    public void AllRegisteredNativeClassesMatchOnlyActualAncestorFamilies()
    {
        // 全矩阵同时检查正向祖先与反向/旁系非匹配，避免宽父类吞掉具体类。
        foreach (string actual in NativeNames)
        {
            PyBaseException original = Native(actual);
            Assert.Equal(actual, original.PyTypeName);
            foreach (string target in NativeNames)
                Assert.Equal(PyExceptionHierarchy.IsSubclass(actual, target),
                    PyErrors.MatchDynamicException(original, PyType.Get(target)));
        }
    }

    [Fact]
    public void NativeDisplayNamesCannotForgeAClassIdentity()
    {
        var forged = new PyException("ValueError", "display only");
        Assert.False(PyErrors.MatchDynamicException(forged, PyType.Get("ValueError")));
        Assert.True(PyErrors.MatchDynamicException(forged, PyType.Get("Exception")));
        Assert.False(PyErrors.MatchDynamicException(new PyValueError("real"), PyType.Get("JSONDecodeError")));
        Assert.Throws<PyTypeError>(() => PyErrors.MatchDynamicException(forged, PyType.Get("made_up_exception")));
        Assert.Throws<PyTypeError>(() => PyErrors.MatchDynamicException(forged, new PyClass("ValueError")));
    }

    private static PyClass ExceptionClass(string name, object parent, PyExceptionFactory? factory = null) =>
        (PyClass)PyOps.MakeClass(name, [parent], factory ?? new PyExceptionFactory(
            (cls, args) => new PyException(cls.Name, "", args)));

    [Fact]
    public void UserClassesUseReferencesAndPythonAncestryNotClrLayout()
    {
        PyClass left = ExceptionClass("Same", PyType.Get("ValueError"));
        PyClass right = ExceptionClass("Same", PyType.Get("ValueError"));
        PyClass child = ExceptionClass("Child", left);
        var original = (PyBaseException)child.Invoke([], [], [])!;
        Assert.Same(child, original.PythonClass);
        Assert.True(PyErrors.MatchDynamicException(original, child));
        Assert.True(PyErrors.MatchDynamicException(original, left));
        Assert.False(PyErrors.MatchDynamicException(original, right));
        Assert.True(PyErrors.MatchDynamicException(original, PyType.Get("ValueError")));
        Assert.True(PyErrors.MatchDynamicException(original, PyType.Get("Exception")));
        Assert.False(PyErrors.MatchDynamicException(original, PyType.Get("TypeError")));
        // 无PythonClass的原生实例绝不按显示同名去匹配用户类。
        PyClass namedValueError = ExceptionClass("ValueError", PyType.Get("ValueError"));
        Assert.False(PyErrors.MatchDynamicException(new PyValueError("native"), namedValueError));
        left.SetAttr("__module__", "reexport");
        child.SetAttr("__module__", "another_module");
        Assert.True(PyErrors.MatchDynamicException(original, left));
        Assert.False(PyErrors.MatchDynamicException(original, right));
    }

    [Fact]
    public void UserBaseExceptionAndUnsupportedOperationRetainSpecialAncestry()
    {
        PyClass exitClass = ExceptionClass("Exit", PyType.Get("SystemExit"));
        var exit = (PyBaseException)exitClass.Invoke([], [], [])!;
        Assert.True(PyErrors.MatchDynamicException(exit, PyType.Get("BaseException")));
        Assert.True(PyErrors.MatchDynamicException(exit, PyType.Get("SystemExit")));
        Assert.False(PyErrors.MatchDynamicException(exit, PyType.Get("Exception")));
        PyClass unsupportedClass = ExceptionClass("Unsupported", PyType.Get("UnsupportedOperation"));
        var unsupported = (PyBaseException)unsupportedClass.Invoke([], [], [])!;
        Assert.True(PyErrors.MatchDynamicException(unsupported, PyType.Get("OSError")));
        Assert.True(PyErrors.MatchDynamicException(unsupported, PyType.Get("ValueError")));
    }

    [Fact]
    public void TupleValidationNeverShortCircuitsOrRecurses()
    {
        var original = new PyValueError("original");
        object match = PyType.Get("ValueError"), miss = PyType.Get("TypeError");
        Assert.False(PyErrors.MatchDynamicException(original, new PyTuple([])));
        Assert.True(PyErrors.MatchDynamicException(original, new PyTuple([miss, match])));
        foreach (object? invalid in new object?[] { null, PyNone.Instance, 7L, original,
            new PyClass("Plain"), PyType.Get("int"), new PyTuple([match]), new object?[] { match } })
        {
            foreach (object first in new[] { match, miss })
                Assert.Throws<PyTypeError>(() => PyErrors.MatchDynamicException(original, new PyTuple([first, invalid])));
            Assert.Throws<PyTypeError>(() => PyErrors.MatchDynamicException(original, new PyTuple([invalid, match])));
        }
        Assert.Throws<PyTypeError>(() => PyErrors.MatchDynamicException(original, original));
    }

    [Fact]
    public void InvalidTargetPreparesOnlyNewErrorAndLeavesAmbientOwnedByCaller()
    {
        var original = new PyValueError("original");
        object? previous = PyErrors.EnterHandler(original);
        long depth = PyTraceback.PushFrame("dynamic-except.py", "dispatch");
        PyTraceback.SetLine(17);
        try
        {
            PyTypeError error = Assert.Throws<PyTypeError>(() => PyErrors.MatchDynamicException(original, 7L));
            Assert.Equal("catching classes that do not inherit from BaseException is not allowed", error.PyMessage);
            Assert.Same(original, error.Context);
            Assert.Equal(new PyStackFrame("dynamic-except.py", "dispatch", 17), error.Frames[0]);
            Assert.Same(original, PyErrors.Current);
            Assert.Null(original.Context);
            Assert.Empty(original.Frames);
        }
        finally
        {
            PyTraceback.PopTo(depth);
            PyErrors.LeaveHandler(previous);
        }
    }

    [Fact]
    public void MatchingDoesNotRunFactoriesOrChangeOriginalState()
    {
        int calls = 0;
        PyClass cls = ExceptionClass("Quiet", PyType.Get("ValueError"), (owner, args) =>
        {
            calls++;
            return new PyException(owner.Name, "", args);
        });
        var original = (PyBaseException)cls.Invoke([new object()], [], [])!;
        var cause = new PyKeyError("cause");
        var context = new PyTypeError("context");
        original.WithCause(cause);
        original.Context = context;
        original.UserAttributes["marker"] = new object();
        object marker = original.UserAttributes["marker"]!;
        object?[] args = original.Args;
        PyStackFrame[] frames = original.Frames;
        Assert.True(PyErrors.MatchDynamicException(original, cls));
        Assert.True(PyErrors.MatchDynamicException(original, PyType.Get("ValueError")));
        Assert.False(PyErrors.MatchDynamicException(original, PyType.Get("OSError")));
        Assert.Equal(1, calls);
        Assert.Same(args, original.Args);
        Assert.Same(frames, original.Frames);
        Assert.Same(cause, original.Cause);
        Assert.Same(context, original.Context);
        Assert.Same(marker, original.UserAttributes["marker"]);
        Assert.True(original.SuppressContextFlag);
    }

    private sealed class ExplosiveMessage : PyValueError
    {
        public ExplosiveMessage() : base("quiet") { }
        public override string PyMessage => throw new InvalidOperationException("message must not run");
    }

    [Fact]
    public void NativeMatchingNeverFormatsTheException()
    {
        var original = new ExplosiveMessage();
        Assert.True(PyErrors.MatchDynamicException(original, PyType.Get("ValueError")));
        Assert.False(PyErrors.MatchDynamicException(original, PyType.Get("TypeError")));
    }
}
