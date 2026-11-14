// 六异常真实继承、原始args及显示时点；所有调用共享帧/context集合。
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyOptParseExceptionTests
{
    private static readonly string[] Names = ["OptParseError", "OptionError", "OptionConflictError", "OptionValueError", "BadOptionError", "AmbiguousOptionError"];
    private static PyBaseException Make(string name, object?[] args, string[]? names = null, object?[]? values = null)
        => PyOptParseExceptions.Construct(name, args, names ?? [], values ?? []);
    [Fact]
    public void ClrParentsAreRealAndCannotBeForgedByName()
    {
        Assert.IsAssignableFrom<PyOptParseError>(new PyOptionError("bad", "-x"));
        Assert.IsAssignableFrom<PyOptionError>(new PyOptionConflictError("bad", "-x"));
        Assert.IsAssignableFrom<PyOptParseError>(new PyOptionValueError("bad"));
        Assert.IsAssignableFrom<PyBadOptionError>(new PyAmbiguousOptionError("--f", new PyTuple(["--foo"])));
        Assert.False(new PyException("OptionError", "bad") is PyOptionError);
        bool caught = false;
        try { throw new PyAmbiguousOptionError("--f", new PyTuple(["--foo"])); }
        catch (PyBadOptionError) { caught = true; }
        Assert.True(caught);
    }
    [Fact]
    public void CanonicalMetadataAndPythonParentsAgree()
    {
        string[] parents = ["Exception", "OptParseError", "OptionError", "OptParseError", "OptParseError", "BadOptionError"];
        for (int i = 0; i < Names.Length; i++)
        {
            var type = PyType.Get(Names[i]);
            Assert.Same(type, PyOps.TypeObject("optparse." + Names[i]));
            Assert.Equal("optparse", PyObject.GetAttr(type, "__module__"));
            Assert.Equal($"<class 'optparse.{Names[i]}'>", type.ToString());
            Assert.True(PyExceptionHierarchy.IsSubclass(Names[i], parents[i]));
            Assert.Equal(true, PyOps.CallBuiltin("callable", [type], [], []));
        }
    }
    [Fact]
    public void RequiredArgumentsAreNotInventedForRaiseClass()
    {
        foreach (string name in Names)
        {
            Assert.Throws<PyTypeError>(() => Make(name, []));
            Assert.Throws<PyTypeError>(() => PyErrors.PrepareRaise(PyType.Get(name)));
        }
        Assert.Throws<PyTypeError>(() => PyExceptionTypes.Create("OptionError", "bad"));
        Assert.Equal("", PyExceptionTypes.Create("OptParseError", "").PyMessage);
    }
    [Fact]
    public void KeywordBindingDoesNotPopulateOriginalArgs()
    {
        var e = Make("OptionError", [], ["msg", "option"], ["bad", "-x"]);
        Assert.Empty(e.Args); Assert.Equal("OptionError()", PyRepr.Repr(e));
        Assert.Equal("option -x: bad", PyRepr.Str(e));
        var mixed = Make("OptionError", ["bad"], ["option"], ["-x"]);
        Assert.Equal("bad", Assert.Single(mixed.Args));
        Assert.Throws<PyTypeError>(() => Make("OptionError", ["bad", "-x"], ["msg"], ["again"]));
    }
    private static object Message(System.Func<object?> body)
    {
        var cls = (PyClass)PyOps.MakeClass("Message", [], null);
        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]), (_, _, _, _) => body(), []));
        return cls.Invoke([], [], [])!;
    }
    [Fact]
    public void OptionConvertsExactlyOnceAfterBindingAndMessageRemainsOriginal()
    {
        int calls = 0; object token = Message(() => { calls++; return "-x"; });
        Assert.Throws<PyTypeError>(() => Make("OptionError", [token, token, token]));
        Assert.Equal(0, calls);
        var e = Make("OptionError", [token, token]);
        Assert.Equal(1, calls); Assert.Same(token, e.Args[0]); Assert.Same(token, e.Args[1]);
        Assert.Same(token, PyObject.GetAttr(e, "msg"));
        Assert.Equal("-x", PyObject.GetAttr(e, "option_id"));
        Assert.Equal("option -x: -x", PyRepr.Str(e)); Assert.Equal(2, calls);
    }
    [Fact]
    public void NonStringMessageIsNotSilentlyCoerced()
    {
        var e = Make("OptParseError", [42L]);
        Assert.Throws<PyTypeError>(() => PyRepr.Str(e));
        Assert.Equal("OptParseError(42)", PyRepr.Repr(e));
        var option = Make("OptionError", [42L, ""]);
        Assert.Throws<PyTypeError>(() => PyRepr.Str(option));
        PyObject.SetAttr(option, "option_id", "-x");
        Assert.Equal("option -x: 42", PyRepr.Str(option));
    }
    [Fact]
    public void MutableFieldsControlStringButNotReprArgs()
    {
        var e = Make("OptionError", ["old", "-x"]);
        PyObject.SetAttr(e, "msg", "new"); PyObject.SetAttr(e, "option_id", "--new");
        Assert.Equal("option --new: new", PyRepr.Str(e));
        Assert.Equal("OptionError('old', '-x')", PyRepr.Repr(e));
        PyObject.DelAttr(e, "msg"); Assert.Throws<PyAttributeError>(() => PyRepr.Str(e));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(e, "args", new PyTuple(["new"])));
        Assert.Equal("old", e.Args[0]);
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(e, "__class__", PyType.Get("ValueError")));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(e, "__traceback__"));
        Assert.Same(PyType.Get("OptionError"), PyObject.GetAttr(e, "__class__"));
        PyObject.SetAttr(e, "custom", 9L); Assert.Equal(9L, PyObject.GetAttr(e, "custom"));
        PyObject.DelAttr(e, "custom"); Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(e, "custom"));
        var bad = new PyBadOptionError(new PyTuple(["-x"]));
        Assert.Equal("no such option: -x", PyRepr.Str(bad));
        PyObject.SetAttr(bad, "opt_str", new PyTuple(["-x", "-y"]));
        Assert.Throws<PyTypeError>(() => PyRepr.Str(bad));
        var sequence = new PyOptionError("old", "-x");
        object id = Message(() => { PyObject.SetAttr(sequence, "msg", "late"); return "changed"; });
        var truthClass = (PyClass)PyOps.MakeClass("Truth", [], null);
        truthClass.SetAttr("__bool__", new PyFunction("bool", PySignature.Create("bool", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { PyObject.SetAttr(sequence, "option_id", id); return true; }, []));
        PyObject.SetAttr(sequence, "option_id", truthClass.Invoke([], [], []));
        Assert.Equal("option changed: old", PyRepr.Str(sequence));
        Assert.Equal("late", PyObject.GetAttr(sequence, "msg"));
    }
    [Fact]
    public void AmbiguousChoicesKeepIdentityAndAreJoinedAtDisplay()
    {
        var choices = new PyList(["--foo"]);
        var e = Make("AmbiguousOptionError", ["--f", choices]);
        Assert.Same(choices, PyObject.GetAttr(e, "possibilities"));
        PyOps.CallMethod(choices, "append", ["--far"], [], []);
        Assert.Equal("ambiguous option: --f (--foo, --far?)", PyRepr.Str(e));
        PyOps.CallMethod(choices, "append", [1L], [], []);
        Assert.Throws<PyTypeError>(() => PyRepr.Str(e));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(e, "msg"));
    }
    [Fact]
    public void QualifiedTerminalNameDoesNotChangeShortReprOrType()
    {
        var e = Make("OptionValueError", ["bad"]);
        Assert.Equal("optparse.OptionValueError: bad", e.Render());
        Assert.Equal("OptionValueError('bad')", PyRepr.Repr(e));
        Assert.Equal("OptionValueError", PyObject.GetAttr(PyObject.TypeOf(e), "__name__"));
        Assert.Equal("ValueError: bad", new PyValueError("bad").Render());
    }
    [Fact]
    public void DynamicUserSubclassCannotPretendToHaveClrExceptionLayout()
    {
        foreach (string name in Names)
            Assert.Throws<PyTypeError>(() => PyOps.MakeClass("Child", [PyType.Get(name)], null));
        var failure = new PyValueError("from option str");
        var option = Message(() => throw failure);
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Make("OptionError", ["bad", option])));
    }
}
