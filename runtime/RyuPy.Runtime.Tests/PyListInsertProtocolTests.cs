// insert专用类型级__index__协议；四入口回调顺序、异常原样传播与整数宽度。
using System.Numerics;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyListInsertProtocolTests
{
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("InsertIndex");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ => body()));
        return new PyInstance(cls);
    }

    private static object? Apply(string route, PyList list, object? index)
    {
        if (route == "direct") return PyOps.CallMethod(list, "insert", [index, "x"], [], []);
        if (route == "root") return PyOps.CallValue(PyObject.GetAttr(PyType.Get("list"), "insert"), [list, index, "x"], [], []);
        object? method = route == "saved" ? PyObject.GetAttr(list, "insert") : PyOps.PrepareMethod(list, "insert");
        return PyOps.CallValue(method, [index, "x"], [], []);
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("root")]
    [InlineData("saved")]
    [InlineData("prepared")]
    public void CallbackRunsOnceBeforeLengthAndPreservesItsException(string route)
    {
        var list = new PyList([1L, 2L, 3L]);
        int calls = 0;
        var index = Index(() => { calls++; list.Clear(); list.Append(7L); return -4294967296L; });
        Assert.Same(PyNone.Instance, Apply(route, list, index));
        Assert.Equal(1, calls);
        Assert.Equal(new object?[] { "x", 7L }, list.Items);
        var error = new PyValueError("index exploded");
        index = Index(() => { calls++; throw error; });
        Assert.Same(error, Assert.Throws<PyValueError>(() => Apply(route, list, index)));
        Assert.Equal(2, calls);
        Assert.Equal(new object?[] { "x", 7L }, list.Items);
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("root")]
    [InlineData("saved")]
    [InlineData("prepared")]
    public void RejectsNonIntegerAndOverflowWithoutMutation(string route)
    {
        var list = new PyList([1L]);
        Assert.Equal("__index__ returned non-int (type str)",
            Assert.Throws<PyTypeError>(() => Apply(route, list, Index(() => "bad"))).PyMessage);
        foreach (var wide in new[] { BigInteger.One << 100, -(BigInteger.One << 100) })
        {
            Assert.Equal("Python int too large to convert to C ssize_t",
                Assert.Throws<PyOverflowError>(() => Apply(route, list, wide)).PyMessage);
            Assert.Equal("Python int too large to convert to C ssize_t",
                Assert.Throws<PyOverflowError>(() => Apply(route, list, Index(() => wide))).PyMessage);
        }
        Assert.Equal(new object?[] { 1L }, list.Items);
    }
}
