// 同源未执行查询必须产生不同但相等的loader/spec；比较可变实例字段而非把来源当对象身份。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyCompiledModuleLoaderTests
{
    private static object Loader(string file)
    {
        string name = "loader_protocol_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterSourceLocated(name, "", "subject.py", 1, file, false, false,
            () => { }, key => throw new PyAttributeError(key), PyModules.SetDynamic);
        return PyModules.Import(name).GetAttr("__loader__")!;
    }

    [Fact]
    public void SeparateLoadersCompareAllCurrentFieldsAndHashNameAndPath()
    {
        object first = Loader("/virtual/subject.py"), second = Loader("/virtual/subject.py");
        PyObject.SetAttr(second, "name", PyObject.GetAttr(first, "name"));
        Assert.NotSame(first, second);
        Assert.True(PyOps.Compare(PyOps.Eq, first, second));
        Assert.Equal(PyHash.Of(first), PyHash.Of(second));
        PyObject.SetAttr(first, "extra", 1L);
        Assert.False(PyOps.Compare(PyOps.Eq, first, second));
        PyObject.SetAttr(second, "extra", 1L);
        Assert.True(PyOps.Compare(PyOps.Eq, first, second));
        PyObject.DelAttr(second, "extra");
        Assert.False(PyOps.Compare(PyOps.Eq, first, second));
        PyObject.DelAttr(first, "extra");
        Assert.True(PyOps.Compare(PyOps.Eq, first, second));
        Assert.Same(PyNone.Instance, PyOps.CallMethod(first, "create_module", [PyNone.Instance], [], []));
        Assert.NotNull(PyObject.GetAttr(first, "exec_module"));
    }

    [Fact]
    public void ModuleSpecAcceptsOnlyTheUnchangedBuiltinBooleanComparison()
    {
        PyNativeModules.Register();
        object type = PyModules.ImportAttr("importlib.machinery", "ModuleSpec")!;
        object first = Loader("/virtual/subject.py"), second = Loader("/virtual/subject.py");
        PyObject.SetAttr(second, "name", PyObject.GetAttr(first, "name"));
        object a = PyOps.CallValue(type, ["subject", first], [], [])!;
        object b = PyOps.CallValue(type, ["subject", second], [], [])!;
        Assert.True(PyOps.Compare(PyOps.Eq, a, b));
        var loaderType = Assert.IsType<PyClass>(PyObject.TypeOf(first));
        object original = PyObject.GetAttr(loaderType, "__eq__")!;
        int calls = 0;
        try
        {
            loaderType.SetAttr("__eq__", PyNativeFunctions.Create("custom", ["self", "other"],
                [null, null], 2, _ => { calls++; return new PyList([]); }));
            Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, a, b));
            Assert.Equal(0, calls);
        }
        finally { loaderType.SetAttr("__eq__", original); }
    }

    [Fact]
    public void SameNanFieldUsesDictionaryIdentityBeforeValueEquality()
    {
        object first = Loader("/virtual/subject.py"), second = Loader("/virtual/subject.py");
        PyObject.SetAttr(second, "name", PyObject.GetAttr(first, "name"));
        object nan = double.NaN;
        PyObject.SetAttr(first, "marker", nan); PyObject.SetAttr(second, "marker", nan);
        Assert.True(PyOps.Compare(PyOps.Eq, first, second));
        PyObject.SetAttr(second, "marker", (object)double.NaN);
        Assert.False(PyOps.Compare(PyOps.Eq, first, second));
    }

    [Fact]
    public void CustomFieldComparisonCannotRunThenHideItsReentrantMutation()
    {
        object first = Loader("/virtual/subject.py"), second = Loader("/virtual/subject.py");
        int calls = 0;
        var cls = new PyClass("ChangingLoaderName");
        cls.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2,
            _ => { calls++; PyObject.SetAttr(first, "path", "/changed.py"); return true; }));
        object a = PyOps.CallValue(cls, [], [], [])!, b = PyOps.CallValue(cls, [], [], [])!;
        PyObject.SetAttr(first, "name", a); PyObject.SetAttr(second, "name", b);
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, first, second));
        Assert.Equal(0, calls); Assert.Equal("/virtual/subject.py", PyObject.GetAttr(first, "path"));
        // 字典值相同身份不调用__eq__，即使该对象拥有用户比较协议。
        PyObject.SetAttr(second, "name", a);
        Assert.True(PyOps.Compare(PyOps.Eq, first, second)); Assert.Equal(0, calls);
    }
}
