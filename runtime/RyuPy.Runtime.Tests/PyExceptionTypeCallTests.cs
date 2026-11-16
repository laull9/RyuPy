// 内建异常作为真实类型值调用：保CLR类型、原始args及延迟格式化，不走名字特判替身。
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyExceptionTypeCallTests
{
    [Theory]
    [InlineData("RuntimeError")]
    [InlineData("ValueError")]
    public void OrdinaryExceptionTypeIsCallableAndRetainsArguments(string name)
    {
        object type = PyOps.TypeObject(name);
        Assert.Equal(true, PyOps.CallValue(PyOps.BuiltinFunction("callable"), [type], [], []));
        var error = Assert.IsAssignableFrom<PyBaseException>(PyOps.CallValue(type, ["direct-failure"], [], []));
        Assert.Same(type, PyObject.TypeOf(error)); Assert.Equal("direct-failure", error.Message);
        Assert.Equal(new object?[] { "direct-failure" }, error.Args);
        if (name == "RuntimeError") Assert.IsType<PyRuntimeError>(error); else Assert.IsType<PyValueError>(error);
        var empty = Assert.IsAssignableFrom<PyBaseException>(PyOps.CallValue(type, [], [], []));
        Assert.Empty(empty.Args); Assert.Equal("", PyRepr.Str(empty));
        var none = Assert.IsAssignableFrom<PyBaseException>(PyOps.CallValue(type, [PyNone.Instance], [], []));
        Assert.Same(PyNone.Instance, Assert.Single(none.Args)); Assert.Equal("None", PyRepr.Str(none));
        var many = Assert.IsAssignableFrom<PyBaseException>(PyOps.CallValue(type, [1L, "two"], [], []));
        Assert.Equal("(1, 'two')", PyRepr.Str(many));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, [], ["message"], ["bad"]));
    }
    [Fact]
    public void DynamicExceptionBindingUsesTheActualGlobalValue()
    {
        object? result = null; string name = "exception_value_" + System.Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "e.py", 1, "/virtual/e.py", false,
            state => result = PyOps.CallValue(PyModuleInstances.LoadName(state, "E"), ["x"], [], []));
        var module = PyModuleInstances.Create(name); module.SetAttr("E", PyOps.TypeObject("RuntimeError"));
        PyModuleInstances.Execute(module); Assert.IsType<PyRuntimeError>(result);
        module.SetAttr("E", PyOps.TypeObject("ValueError")); PyModuleInstances.Execute(module); Assert.IsType<PyValueError>(result);
        module.SetAttr("E", 42L); Assert.Throws<PyTypeError>(() => PyModuleInstances.Execute(module));
    }
    [Fact]
    public void ArgumentIdentityAndMutableMessageArePreserved()
    {
        var list = new PyList([]);
        var error = Assert.IsType<PyRuntimeError>(PyOps.CallValue(PyOps.TypeObject("RuntimeError"), [list], [], []));
        Assert.Same(list, Assert.Single(error.Args)); Assert.Equal("[]", PyRepr.Str(error));
        list.Append(7L); Assert.Equal("[7]", PyRepr.Str(error));
    }
    [Fact]
    public void ExistingSpecialFactoriesKeepTheirOwnProtocol()
    {
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyOps.TypeObject("json.JSONDecodeError"), ["x"], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyOps.TypeObject("ValueError"), [], ["x"], [1L]));
        var warning = Assert.IsAssignableFrom<PyBaseException>(PyOps.CallValue(PyOps.TypeObject("UserWarning"), [1L, 2L], [], []));
        Assert.Equal(new object?[] { 1L, 2L }, warning.Args);
    }
    [Fact]
    public void SpecialArgumentFieldsAreNotFlattenedIntoMessages()
    {
        var key = Assert.IsType<PyKeyError>(PyOps.CallValue(PyOps.TypeObject("KeyError"), ["key"], [], []));
        Assert.Equal("'key'", PyRepr.Str(key)); Assert.Equal("key", Assert.Single(key.Args));
        var stop = Assert.IsType<PyStopIteration>(PyOps.CallValue(PyOps.TypeObject("StopIteration"), [PyNone.Instance, 2L], [], []));
        Assert.Equal(2, stop.Args.Length); Assert.Same(PyNone.Instance, stop.Args[0]); Assert.Equal("(None, 2)", PyRepr.Str(stop));
        var exit = Assert.IsType<PySystemExit>(PyOps.CallValue(PyOps.TypeObject("SystemExit"), [1L, 2L], [], []));
        Assert.Equal(new object?[] { 1L, 2L }, Assert.IsType<PyTuple>(exit.Code).Items);
        Assert.Equal("(1, 2)", PyRepr.Str(exit));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyOps.TypeObject("UnicodeDecodeError"), [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyOps.TypeObject("OSError"), [2L, "missing"], [], []));
    }

}
