// 双父异常不能只验证名字；CLR主链、Python父图、构造身份与流能力错误分别锁定。
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyUnsupportedOperationTests
{
    [Fact]
    public void PythonHierarchyContainsBothParentsWithoutChangingOtherOsErrors()
    {
        Assert.True(PyExceptionHierarchy.IsSubclass("UnsupportedOperation", "OSError"));
        Assert.True(PyExceptionHierarchy.IsSubclass("UnsupportedOperation", "ValueError"));
        Assert.False(PyExceptionHierarchy.IsSubclass("OSError", "ValueError"));
        Assert.IsAssignableFrom<PyOSError>(new PyUnsupportedOperation("operation"));
    }
    [Fact]
    public void ConstructionPreservesArgumentAndDefersStringConversion()
    {
        int calls = 0;
        var cls = (PyClass)PyOps.MakeClass("Message", [], null);
        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { calls++; return "message"; }, []));
        object message = cls.Invoke([], [], [])!;
        var error = (PyUnsupportedOperation)PyUnsupportedOperation.Construct([message], []);
        Assert.Equal(0, calls); Assert.Same(message, error.Args[0]);
        Assert.Equal("message", error.PyMessage); Assert.Equal(1, calls);
        Assert.Empty(((PyUnsupportedOperation)PyUnsupportedOperation.Construct([], [])).Args);
        Assert.Throws<PyNotImplementedError>(() => PyUnsupportedOperation.Construct([1L, "message"], []));
        Assert.Throws<PyTypeError>(() => PyUnsupportedOperation.Construct([], ["message"]));
    }
    [Fact]
    public void MemoryFilenoIsUnsupportedEvenAfterClose()
    {
        foreach (object stream in new[] { PyIO.CreateStringIO([], [], [])!, PyIO.CreateBytesIO([], [], [])! })
        {
            object? method = PyObject.GetAttr(stream, "fileno");
            Assert.Throws<PyUnsupportedOperation>(() => PyOps.CallValue(method, [], [], []));
            PyOps.CallValue(PyObject.GetAttr(stream, "close"), [], [], []);
            Assert.Throws<PyUnsupportedOperation>(() => PyOps.CallValue(method, [], [], []));
        }
    }
}
