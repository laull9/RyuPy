using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>数据类默认值与工厂时机回归；端到端另验证编译器的类体顺序。</summary>
public class PyDataclassTests
{
    private static object? Field(string name, object? value) =>
        PyOps.CallValue(PyDataclasses.CreateAttributes()["field"], [], [name], [value]);

    [Fact]
    public void FactoryRunsOnlyForMissingConstructorArguments()
    {
        int count = 0;
        var factory = PyNativeFunctions.Create("factory", [], [], 0, _ => { count++; return new PyList(); });
        var cls = new PyClass("Record");
        cls.SetAttr("items", Field("default_factory", factory));
        PyOps.DeclareDataclass(cls, ["items"], false);
        PyOps.ApplyDataclass(cls, false);
        Assert.Equal(0, count);
        object? first = PyOps.CallValue(cls, [], [], []);
        object? second = PyOps.CallValue(cls, [], [], []);
        Assert.Equal(2, count);
        Assert.NotSame(PyObject.GetAttr(first, "items"), PyObject.GetAttr(second, "items"));
        object? missing = PyDataclasses.CreateAttributes()["MISSING"];
        var explicitValue = PyOps.CallValue(cls, [missing], [], []);
        Assert.Same(missing, PyObject.GetAttr(explicitValue, "items"));
        Assert.Equal(2, count);
        Assert.Equal("Record(items=[])", PyRepr.Repr(first));
        Assert.True(PyHash.Equal(first, second));
        Assert.Throws<PyTypeError>(() => PyHash.Of(first));
    }

    [Fact]
    public void DefaultReferenceIsSharedAndFactoryConflictsFailImmediately()
    {
        object value = new PyClass("Shared");
        var cls = new PyClass("Record");
        cls.SetAttr("item", value);
        PyOps.DeclareDataclass(cls, ["item"], false);
        PyOps.ApplyDataclass(cls, false);
        Assert.Same(value, PyObject.GetAttr(PyOps.CallValue(cls, [], [], []), "item"));
        Assert.Throws<PyValueError>(() => PyOps.CallValue(PyDataclasses.CreateAttributes()["field"], [],
            ["default", "default_factory"], [PyNone.Instance, PyType.Get("list")]));
    }

    [Fact]
    public void MutableDefaultAndRequiredAfterOptionalFailAtDecoration()
    {
        var mutable = new PyClass("Mutable");
        mutable.SetAttr("items", new PyList());
        PyOps.DeclareDataclass(mutable, ["items"], false);
        Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(mutable, false));
        var ordering = new PyClass("Ordering");
        ordering.SetAttr("first", 1L);
        PyOps.DeclareDataclass(ordering, ["first", "second"], false);
        Assert.Throws<PyTypeError>(() => PyOps.ApplyDataclass(ordering, false));
    }
}
