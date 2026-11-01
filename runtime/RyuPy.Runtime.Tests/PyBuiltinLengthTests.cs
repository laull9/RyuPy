using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>内建长度的直接/PIC/一等绑定调用一致；绑定保留接收者而非缓存长度。</summary>
public class PyBuiltinLengthTests
{
    [Fact]
    public void EveryAlreadySizedBuiltinSupportsAllCallPathsAndStrictArity()
    {
        var dict = new PyDict(); dict.Set("a", 1L);
        object[] values = [new PyList([1L, 2L]), new PyTuple([1L, 2L]), dict, new PySet([1L, 2L]),
            new PyFrozenSet([1L, 2L]), new PyDictView(dict, PyDictViewKind.Keys), new PyBytes([1, 2]),
            new PyByteArray([1, 2]), "😀a"];
        foreach (object value in values)
        {
            object? method = PyObject.GetAttr(value, "__len__");
            Assert.Equal(PyOps.Len(value), PyOps.CallValue(method, [], [], []));
            Assert.Equal(PyOps.Len(value), PyOps.CallMethod(value, "__len__", [], [], []));
            Assert.Equal(PyOps.Len(value), PyPic.CallMethod(777, value, "__len__", [], [], []));
            Assert.Same(value, PyObject.GetAttr(method, "__self__"));
            Assert.Equal("method-wrapper", PyRepr.TypeName(method));
            Assert.Same(PyObject.TypeOf(value), PyObject.GetAttr(method, "__objclass__"));
            Assert.Same(PyType.Get("method-wrapper"), PyObject.TypeOf(method));
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(method, "__module__"));
            Assert.True(PyHash.Equal(method, PyObject.GetAttr(value, "__len__")));
            Assert.Equal(PyHash.Of(method), PyHash.Of(PyObject.GetAttr(value, "__len__")));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, [1L], [], []));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, [], ["bad"], [1L]));
            Assert.Throws<PyTypeError>(() => PyOps.CallMethod(value, "__len__", [1L], [], []));
            Assert.Throws<PyTypeError>(() => PyOps.CallMethod(value, "__len__", [], ["bad"], [1L]));
        }
    }

    [Fact]
    public void BoundMethodsObserveReceiverAndDictionaryViewMutation()
    {
        var list = new PyList();
        object? method = PyObject.GetAttr(list, "__len__");
        Assert.Equal(0L, PyOps.CallValue(method, [], [], []));
        list.Items.Add(1L);
        Assert.Equal(1L, PyOps.CallValue(method, [], [], []));
        Assert.False(PyHash.Equal(method, PyObject.GetAttr(new PyList([1L]), "__len__")));
        var dict = new PyDict(); var view = new PyDictView(dict, PyDictViewKind.Keys);
        method = PyObject.GetAttr(view, "__len__"); dict.Set("a", 1L);
        Assert.Equal(1L, PyOps.CallValue(method, [], [], []));
    }
}
