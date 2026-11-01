// 基础配置的状态副作用与根日志入口使用临时内存流，不写宿主标准流。
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyLoggingConfigTests
{
    private static object? Call(string name, object?[] args, string[] names, object?[] values) =>
        PyOps.CallValue(PyLogging.CreateAttributes()[name], args, names, values);
    private static object? Value(object stream) => PyOps.CallValue(PyObject.GetAttr(stream, "getvalue"), [], [], []);

    [Fact]
    public void ConfigurationReusesHandlersAndForceKeepsCallerStreamsOpen()
    {
        var root = (PyLogger)PyLogging.CreateAttributes()["root"]!;
        object? oldHandlers = root.GetAttr("handlers"), oldLevel = root.GetAttr("level");
        var first = PyIO.CreateStringIO([], [], [])!; var second = PyIO.CreateStringIO([], [], [])!;
        root.SetAttr("handlers", new PyList());
        try
        {
            Call("basicConfig", [], ["stream", "level"], [first, "INFO"]);
            Call("basicConfig", [], ["stream", "unknown"], [second, true]);
            Call("info", ["first"], [], []);
            Assert.Equal("INFO:root:first\n", Value(first)); Assert.Equal("", Value(second));
            Call("basicConfig", [], ["stream", "force", "format"], [second, true, "%(message)s"]);
            Call("info", [], ["msg"], ["second"]);
            Assert.Equal("second\n", Value(second)); Assert.Equal(false, PyObject.GetAttr(first, "closed"));
            Assert.Throws<PyValueError>(() => Call("basicConfig", [], ["force", "stream", "unknown"], [true, second, true]));
            Assert.Single(((PyList)root.GetAttr("handlers")!).Items);
            Assert.Throws<PyTypeError>(() => Call("basicConfig", [1L], [], []));
        }
        finally { root.SetAttr("handlers", oldHandlers); root.SetAttr("level", oldLevel); }
    }

    [Fact]
    public void LevelNamesAreBidirectionalAndRequireHashableInput()
    {
        Assert.Equal("WARNING", Call("getLevelName", [30L], [], []));
        Assert.Equal("WARNING", Call("getLevelName", [30.0], [], []));
        Assert.Equal(30L, Call("getLevelName", ["WARN"], [], []));
        Assert.Equal(50L, Call("getLevelName", ["FATAL"], [], []));
        Assert.Equal("Level True", Call("getLevelName", [true], [], []));
        Assert.Equal("Level None", Call("getLevelName", [null], [], []));
        Assert.Throws<PyTypeError>(() => Call("getLevelName", [new PyList()], [], []));
    }
}
