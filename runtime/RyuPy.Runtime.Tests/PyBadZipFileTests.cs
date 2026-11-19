// 类型身份、CLR继承和延迟参数消息分别锁定，不以字符串名称替代真实异常。
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyBadZipFileTests
{
    private sealed class Child(object?[] args) : PyBadZipFile("Child", "", args);
    [Fact]
    public void CanonicalTypeAndRealClrHierarchyAgree()
    {
        var error = new PyBadZipFile("bad");
        Assert.IsAssignableFrom<PyException>(error);
        Assert.Same(PyType.Get("BadZipFile"), PyObject.TypeOf(error));
        Assert.Same(PyType.Get("BadZipFile"), PyOps.TypeObject("zipfile.BadZipFile"));
        Assert.True(PyExceptionHierarchy.IsSubclass("BadZipFile", "Exception"));
        Assert.False(PyExceptionHierarchy.IsSubclass("BadZipFile", "ValueError"));
        Assert.False(PyExceptionHierarchy.IsSubclass("BadZipFile", "OSError"));
        Assert.Equal("zipfile", PyObject.GetAttr(PyType.Get("BadZipFile"), "__module__"));
        Assert.Equal("<class 'zipfile.BadZipFile'>", PyType.Get("BadZipFile").ToString());
    }
    [Fact]
    public void CallableConstructsAllArgumentsWithoutInventingEmptyMessage()
    {
        object token = new();
        var error = Assert.IsType<PyBadZipFile>(PyOps.CallValue(PyType.Get("BadZipFile"), [token, 2L], [], []));
        Assert.Same(token, error.Args[0]); Assert.Equal(2L, error.Args[1]);
        Assert.Empty(Assert.IsType<PyBadZipFile>(PyOps.CallValue(PyType.Get("BadZipFile"), [], [], [])).Args);
        Assert.Equal("", Assert.Single(Assert.IsType<PyBadZipFile>(PyOps.CallValue(PyType.Get("BadZipFile"), [""], [], [])).Args));
        Assert.IsType<PyBadZipFile>(PyExceptionTypes.Create("BadZipFile", ""));
        Assert.Empty(Assert.IsType<PyBadZipFile>(PyErrors.PrepareRaise(PyType.Get("BadZipFile"))).Args);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyType.Get("BadZipFile"), [], ["message"], ["bad"]));
    }
    [Fact]
    public void ParentAndGeneratedSubclassPolicyDoNotFormatArgumentsEarly()
    {
        int calls = 0;
        var cls = (PyClass)PyOps.MakeClass("Message", [], null);
        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { calls++; return "message"; }, []));
        object message = cls.Invoke([], [], [])!;
        var error = (PyBadZipFile)PyBadZipFile.Construct([message], []);
        Assert.Equal("", PyErrors.UserExceptionMessageForBase("zipfile.BadZipFile", [message]));
        var child = new Child([message]);
        Assert.Equal(0, calls);
        Assert.Equal("message", error.PyMessage);
        Assert.Equal("message", child.PyMessage);
        Assert.Equal(2, calls);
    }
    [Fact]
    public void CatchUsesActualClrSubtypeRatherThanMatchingName()
    {
        bool caught = false;
        try { throw new Child(["bad"]); }
        catch (PyBadZipFile) { caught = true; }
        Assert.True(caught);
        Assert.False(new PyException("BadZipFile", "fake") is PyBadZipFile);
    }
}
