// 遵循 ADR-0181/0182 与 Python 规范：测试 isinstance/issubclass 递归元组短路与基类原语
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyBuiltinTypeConformanceTests
{
    private static object? Call(object? callable, params object?[] args) =>
        PyOps.CallValue(callable, args, [], []);

    [Fact]
    public void IsInstanceSupportsNestedTupleAndShortCircuits()
    {
        var isinstanceFunc = PyOps.BuiltinFunction("isinstance");
        var intType = PyOps.TypeObject("int");
        var strType = PyOps.TypeObject("str");
        var floatType = PyOps.TypeObject("float");

        // 标量测试
        Assert.Equal(true, Call(isinstanceFunc, 42L, intType));
        Assert.Equal(false, Call(isinstanceFunc, "hello", intType));

        // 扁平元组
        var tuple1 = new PyTuple([strType, intType]);
        Assert.Equal(true, Call(isinstanceFunc, 42L, tuple1));
        Assert.Equal(true, Call(isinstanceFunc, "world", tuple1));
        Assert.Equal(false, Call(isinstanceFunc, 1.5, tuple1));

        // 嵌套元组 ((float, (int,)), str)
        var innerTuple = new PyTuple([intType]);
        var midTuple = new PyTuple([floatType, innerTuple]);
        var outerTuple = new PyTuple([midTuple, strType]);
        Assert.Equal(true, Call(isinstanceFunc, 42L, outerTuple));
        Assert.Equal(true, Call(isinstanceFunc, 3.14, outerTuple));
        Assert.Equal(true, Call(isinstanceFunc, "abc", outerTuple));
        Assert.Equal(false, Call(isinstanceFunc, null, outerTuple));

        // 短路语义测试：若前面的类型匹配成功，即使后续元组元素为非类型（如 123），也应直接返回 true
        var shortCircuitTuple = new PyTuple([intType, 123L]);
        Assert.Equal(true, Call(isinstanceFunc, 42L, shortCircuitTuple));

        // 若前面的类型未匹配，遇到非类型则应按 Python 规范抛出 PyTypeError
        var failingTuple = new PyTuple([strType, 123L]);
        Assert.Throws<PyTypeError>(() => Call(isinstanceFunc, 42L, failingTuple));
    }

    [Fact]
    public void IsSubclassSupportsNestedTupleAndShortCircuits()
    {
        var issubclassFunc = PyOps.BuiltinFunction("issubclass");
        var intType = PyOps.TypeObject("int");
        var objectType = PyOps.TypeObject("object");
        var strType = PyOps.TypeObject("str");

        // int 是 object 的子类
        Assert.Equal(true, Call(issubclassFunc, intType, objectType));
        Assert.Equal(false, Call(issubclassFunc, objectType, intType));

        // 嵌套元组
        var nested = new PyTuple([strType, new PyTuple([objectType])]);
        Assert.Equal(true, Call(issubclassFunc, intType, nested));

        // 短路语义测试
        var shortCircuitTuple = new PyTuple([objectType, 999L]);
        Assert.Equal(true, Call(issubclassFunc, intType, shortCircuitTuple));

        var failingTuple = new PyTuple([strType, 999L]);
        Assert.Throws<PyTypeError>(() => Call(issubclassFunc, intType, failingTuple));
    }

    [Fact]
    public void OrdinaryClassExposesMroTuple()
    {
        // 构造一个普通类
        var cls = new PyClass("SampleOrdinaryClass");
        var mroObj = PyObject.GetAttr(cls, "__mro__");
        var mroTuple = Assert.IsType<PyTuple>(mroObj);

        // 继承链包含自身与 object
        Assert.True(mroTuple.Length >= 2);
        Assert.Same(cls, mroTuple.GetItem(0));
        Assert.Same(PyOps.TypeObject("object"), mroTuple.GetItem(mroTuple.Length - 1));
    }

    [Fact]
    public void ObjectSetAttrAndDelAttrOperateDirectlyOnInstance()
    {
        // 构造类及其实例
        var cls = new PyClass("CustomHolder");
        var instance = PyOps.CallValue(cls, [], [], []);

        // 使用 object.__setattr__ 基类方法直接设置属性
        var objectType = PyOps.TypeObject("object");
        var setattrFunc = PyObject.GetAttr(objectType, "__setattr__");
        Call(setattrFunc, instance, "test_field", "value_123");

        Assert.Equal("value_123", PyObject.GetAttr(instance, "test_field"));

        // 使用 object.__delattr__ 基类方法删除属性
        var delattrFunc = PyObject.GetAttr(objectType, "__delattr__");
        Call(delattrFunc, instance, "test_field");

        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "test_field"));
    }
}
