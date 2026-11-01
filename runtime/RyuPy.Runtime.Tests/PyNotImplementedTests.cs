using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>拒绝哨兵必须继续反射分派，而不是被当作 True；公开值保持单例和 3.12 警告行为。</summary>
public class PyNotImplementedTests
{
    [Fact]
    public void ComparisonContinuesAfterNotImplemented()
    {
        var left = new PyClass("Left"); var right = new PyClass("Right");
        int calls = 0;
        left.SetAttr("__lt__", PyNativeFunctions.Create("__lt__", ["self", "other"], [null, null], 2, _ => PyNotImplemented.Instance));
        right.SetAttr("__gt__", PyNativeFunctions.Create("__gt__", ["self", "other"], [null, null], 2, _ => { calls++; return false; }));
        Assert.False(PyOps.Compare(PyOps.Lt, new PyInstance(left), new PyInstance(right)));
        Assert.Equal(1, calls);
        right.SetAttr("__gt__", PyNativeFunctions.Create("__gt__", ["self", "other"], [null, null], 2, _ => PyNotImplemented.Instance));
        Assert.Throws<PyTypeError>(() => PyOps.Compare(PyOps.Lt, new PyInstance(left), new PyInstance(right)));
    }

    [Fact]
    public void PublicValueAndTypeConstructionKeepIdentity()
    {
        Assert.Same(PyNotImplemented.Instance, PyOps.BuiltinFunction("NotImplemented"));
        Assert.Equal("NotImplemented", PyRepr.Repr(PyNotImplemented.Instance));
        Assert.Equal("NotImplementedType", PyRepr.TypeName(PyNotImplemented.Instance));
        Assert.Same(PyNotImplemented.Instance, PyOps.CallValue(PyType.Get("NotImplementedType"), [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("NotImplemented", [], [], []));
    }

    [Fact]
    public void BooleanConversionUsesWarningPolicy()
    {
        var context = PyOps.CallValue(PyWarnings.CreateAttributes()["catch_warnings"], [], ["record", "action"], [true, "always"]);
        var records = Assert.IsType<PyList>(PyOps.CallValue(PyObject.GetAttr(context, "__enter__"), [], [], []));
        try
        {
            Assert.True(PyOps.Truthy(PyNotImplemented.Instance));
            Assert.Equal(1, records.Count);
            Assert.Equal(PyType.Get("DeprecationWarning"), PyObject.GetAttr(records.Items[0], "category"));
        }
        finally { PyOps.CallValue(PyObject.GetAttr(context, "__exit__"), [PyNone.Instance, PyNone.Instance, PyNone.Instance], [], []); }
    }
}
