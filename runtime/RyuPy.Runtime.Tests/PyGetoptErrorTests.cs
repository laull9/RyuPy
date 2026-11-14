// getopt异常对象与CP3.12.13原流对照；不把解析器测试或类型名伪装当真实catch证明。
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyGetoptErrorTests
{
    private static PyGetoptError Make(object?[] args, string[]? names = null, object?[]? values = null) =>
        Assert.IsType<PyGetoptError>(PyOps.CallValue(PyType.Get("GetoptError"), args, names ?? [], values ?? []));

    [Fact]
    public void RealClrTypeMetadataAndParentAgree()
    {
        var type = PyType.Get("GetoptError");
        Assert.Same(type, PyOps.TypeObject("getopt.GetoptError"));
        Assert.Same(type, PyOps.TypeObject("getopt.error"));
        var error = Make(["bad"]);
        Assert.Same(type, PyObject.TypeOf(error));
        Assert.Equal("getopt", PyObject.GetAttr(type, "__module__"));
        Assert.Equal("GetoptError", PyObject.GetAttr(type, "__name__"));
        Assert.Equal("<class 'getopt.GetoptError'>", type.ToString());
        Assert.Equal("", PyObject.GetAttr(type, "msg"));
        Assert.Equal("", PyObject.GetAttr(type, "opt"));
        Assert.True(PyExceptionHierarchy.IsSubclass("GetoptError", "Exception"));
        Assert.True(PyExceptionHierarchy.IsSubclass("GetoptError", "BaseException"));
        Assert.False(PyExceptionHierarchy.IsSubclass("GetoptError", "ValueError"));
        Assert.Equal(true, PyOps.CallBuiltin("callable", [type], [], []));
        Assert.IsAssignableFrom<PyException>(error);
        Assert.False(new PyException("GetoptError", "bad") is PyGetoptError);
        bool caught = false;
        try { throw error; } catch (PyGetoptError) { caught = true; }
        Assert.True(caught);
        Assert.Throws<PyTypeError>(() => PyOps.MakeClass("Child", [type], null));
    }

    [Fact]
    public void BoundArgumentsAlwaysBecomeTwoArgsWithoutCoercion()
    {
        object msg = new PyList(["msg"]); object opt = new PyList(["opt"]);
        foreach (var error in new[] { Make([msg, opt]), Make([], ["opt", "msg"], [opt, msg]), Make([msg], ["opt"], [opt]) })
        {
            Assert.Equal(2, error.Args.Length);
            Assert.Same(msg, error.Args[0]); Assert.Same(opt, error.Args[1]);
            Assert.Same(msg, PyObject.GetAttr(error, "msg")); Assert.Same(opt, PyObject.GetAttr(error, "opt"));
        }
        var single = Make(["bad"]);
        Assert.Equal(new object?[] { "bad", "" }, single.Args);
        Assert.Equal(new object?[] { "bad", "" }, Make([], ["msg"], ["bad"]).Args);
        Assert.Equal("GetoptError('bad', '')", PyRepr.Repr(single));
        Assert.Equal("getopt.GetoptError: bad", single.Render());
        Assert.Equal(new object?[] { "", "" }, PyExceptionTypes.Create("GetoptError", "").Args);
    }

    [Fact]
    public void InitBindingErrorsIncludeImplicitSelfExactly()
    {
        Assert.Equal("GetoptError.__init__() missing 1 required positional argument: 'msg'",
            Assert.Throws<PyTypeError>(() => Make([])).PyMessage);
        Assert.Equal("GetoptError.__init__() takes from 2 to 3 positional arguments but 4 were given",
            Assert.Throws<PyTypeError>(() => Make([1L, 2L, 3L])).PyMessage);
        Assert.Equal("GetoptError.__init__() got an unexpected keyword argument 'unknown'",
            Assert.Throws<PyTypeError>(() => Make([1L], ["unknown"], [2L])).PyMessage);
        Assert.Equal("GetoptError.__init__() got multiple values for argument 'msg'",
            Assert.Throws<PyTypeError>(() => Make([1L], ["msg"], [2L])).PyMessage);
        Assert.Throws<PyTypeError>(() => PyErrors.PrepareRaise(PyType.Get("GetoptError")));
    }

    [Fact]
    public void FieldsMutateIndependentlyAndDeletionFallsBackToClassDefaults()
    {
        var error = Make(["original", "a"]);
        PyObject.SetAttr(error, "msg", "changed"); PyObject.SetAttr(error, "opt", "b");
        Assert.Equal("changed", PyRepr.Str(error));
        Assert.Equal(new object?[] { "original", "a" }, error.Args);
        PyObject.DelAttr(error, "msg"); PyObject.DelAttr(error, "opt");
        Assert.Equal("", PyObject.GetAttr(error, "msg")); Assert.Equal("", PyObject.GetAttr(error, "opt"));
        Assert.Equal("", PyRepr.Str(error));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(error, "msg"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(error, "args", new PyTuple(["new"])));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(error, "__dict__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(error, "__class__", PyType.Get("ValueError")));
        Assert.Same(error, PyErrors.PrepareRaise(error));
    }

    [Fact]
    public void DirectStringSlotPreservesRawObjectAndPublicStrChecksIt()
    {
        var error = Make([7L, PyNone.Instance]);
        Assert.Equal(7L, PyObject.InvokeMethod(error, "__str__", [], [], []));
        // 覆盖编译器实际无参PIC/captured冷回退，不能仅测InvokeMethod。
        Assert.Equal(7L, PyOps.CallMethod(error, "__str__", [], [], []));
        Assert.Equal(7L, PyPic.CallMethod0(-1, error, "__str__"));
        Assert.Equal(7L, PyPic.CallCapturedMethod0(-1, false, error, "__str__"));
        Assert.Equal(7L, PyOps.CallCapturedMethod(false, error, "__str__", [], [], []));
        object? saved = PyOps.PrepareMethod(error, "__str__");
        Assert.Equal(7L, PyOps.CallValue(saved, [], [], []));
        Assert.Equal(7L, PyPic.CallCapturedMethod0(-1, true, saved, "__str__"));
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod1(-1, error, "__str__", 1L));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(error, "__str__", [], ["extra"], [1L]));
        Assert.Equal("__str__ returned non-string (type int)", Assert.Throws<PyTypeError>(() => PyRepr.Str(error)).PyMessage);
        var cls = (PyClass)PyOps.MakeClass("Message", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object text = cls.Invoke(["value"], [], [])!;
        PyObject.SetAttr(error, "msg", text);
        Assert.Same(text, PyObject.InvokeMethod(error, "__str__", [], [], []));
        Assert.Same(text, PyRepr.StrObject(error));
        Assert.Same(text, PyOps.CallBuiltin("str", [error], [], []));
        Assert.Equal("value", error.PyMessage);
        Assert.Same(text, PyOps.CallValue(saved, [], [], []));
        Assert.Equal("other", PyPic.CallMethod0(-1, Make(["other"]), "__str__"));
        // 实例可调用覆盖不自动补self；保存的旧方法仍读取原接收者当前msg。
        object marker = new PyList(["replacement"]);
        var replacement = PyNativeFunctions.Create("replacement", [], [], 0, _ => marker);
        PyObject.SetAttr(error, "__str__", replacement);
        Assert.Same(replacement, PyOps.PrepareMethod(error, "__str__"));
        Assert.Same(marker, PyPic.CallMethod0(-1, error, "__str__"));
        Assert.Same(text, PyOps.CallValue(saved, [], [], []));
        Assert.Same(text, PyRepr.StrObject(error));
        // 实例同名属性不改变类型的隐式特殊槽查找。
        PyObject.SetAttr(error, "__str__", 42L);
        Assert.Equal(42L, PyObject.GetAttr(error, "__str__"));
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod0(-1, error, "__str__"));
        Assert.Same(text, PyOps.CallValue(saved, [], [], []));
        PyObject.DelAttr(error, "__str__");
        Assert.Same(text, PyPic.CallMethod0(-1, error, "__str__"));
        Assert.Throws<PyAttributeError>(() => PyPic.CallMethod0(-1, error, "missing"));
        Assert.Same(text, PyRepr.StrObject(error));
    }
}
