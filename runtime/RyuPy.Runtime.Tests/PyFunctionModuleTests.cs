// 函数module是真实成员：定义时捕获、写删及普通属性字典互不混淆。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyFunctionModuleTests
{
    private static PyFunction Function() => new("probe",
        PySignature.Create("probe", [], [], []), (_, _, _, _) => PyNone.Instance, []);

    [Fact]
    public void CaptureKeepsValueIdentityAndOldFunctionAlias()
    {
        var old = Function();
        var alias = old;
        object first = new PyList();
        object second = new PyDict();
        PyOps.FunctionModule(old, first);
        var fresh = Function();
        PyOps.FunctionModule(fresh, second);
        Assert.Same(first, PyObject.GetAttr(alias, "__module__"));
        Assert.Same(second, PyObject.GetAttr(fresh, "__module__"));
        PyObject.SetAttr(old, "__module__", second);
        Assert.Same(second, PyObject.GetAttr(alias, "__module__"));
    }

    [Fact]
    public void AssignmentDeletionAndDictionaryStaySeparate()
    {
        var function = Function();
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__module__"));
        object[] values = ["module.name", "", 37L, false, new PyList(), PyNone.Instance];
        foreach (object value in values)
        {
            PyObject.SetAttr(function, "__module__", value);
            Assert.Same(value, PyObject.GetAttr(function, "__module__"));
        }
        object actual = new PyDict();
        PyObject.SetAttr(function, "__module__", actual);
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__"));
        attributes.Set("__module__", "shadow");
        Assert.Same(actual, PyObject.GetAttr(function, "__module__"));
        var replacement = new PyDict();
        replacement.Set("__module__", "replacement shadow");
        PyObject.SetAttr(function, "__dict__", replacement);
        Assert.Same(actual, PyObject.GetAttr(function, "__module__"));
        PyObject.DelAttr(function, "__module__");
        PyObject.DelAttr(function, "__module__");
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__module__"));
        Assert.Equal("replacement shadow", replacement.Get("__module__"));
    }

    [Fact]
    public void CaptureDoesNotInvokeUserProtocolsAndChecksTarget()
    {
        var function = Function();
        var value = new Poison();
        PyOps.FunctionModule(function, value);
        Assert.Same(value, PyObject.GetAttr(function, "__module__"));
        PyObject.SetAttr(function, "__module__", value);
        Assert.Same(value, PyObject.GetAttr(function, "__module__"));
        Assert.Equal(0, value.Reads);
        Assert.Throws<PyTypeError>(() => PyOps.FunctionModule(new PyList(), value));
        Assert.Equal(0, value.Reads);
        PyOps.FunctionModule(function, null);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__module__"));
    }

    private sealed class Poison : PyNativeObject
    {
        internal int Reads;
        public override string TypeName => "ModulePoison";
        public override object? GetAttr(string name)
        {
            Reads++;
            throw new PyValueError("module protocols must not run");
        }
        public override string PythonRepr() => throw new PyValueError("module repr must not run");
        public override string PythonStr() => throw new PyValueError("module str must not run");
    }
}
