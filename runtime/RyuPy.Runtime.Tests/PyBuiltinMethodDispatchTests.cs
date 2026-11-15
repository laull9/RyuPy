// 小参数PIC内建分派的身份、错误与星号参数保护；正文仍由原容器实现负责。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

public class PyBuiltinMethodDispatchTests
{
    [Fact]
    public void AppendPopAndDictionaryGetKeepReferences()
    {
        var list = new PyList(); object marker = new();
        Assert.Same(PyNone.Instance, PyPic.CallMethod1(3900, list, "append", marker));
        Assert.Same(marker, PyPic.CallMethod0(3901, list, "pop"));
        Assert.Throws<PyIndexError>(() => PyPic.CallMethod0(3901, list, "pop"));
        var dict = new PyDict(); dict.Set("x", marker);
        Assert.Same(marker, PyPic.CallMethod1(3902, dict, "get", "x"));
        Assert.Same(PyNone.Instance, PyPic.CallMethod1(3902, dict, "get", "missing"));
        Assert.Same(marker, PyPic.CallMethod2(3903, dict, "get", "missing", marker));
    }
    [Fact]
    public void SetPreservesPythonEqualityAndUnhashableErrors()
    {
        var set = new PySet();
        Assert.Same(PyNone.Instance, PyPic.CallMethod1(3904, set, "add", 1L));
        PyPic.CallMethod1(3904, set, "add", true);
        Assert.Equal(1, set.Count);
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod1(3904, set, "add", new PyList()));
    }
    [Fact]
    public void WrongArityStillUsesOriginalBindingErrors()
    {
        var list = new PyList();
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod0(3905, list, "append"));
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod2(3905, list, "append", 1L, 2L));
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod2(3906, list, "pop", 0L, 1L));
        Assert.Equal(0, list.Count);
    }
}
