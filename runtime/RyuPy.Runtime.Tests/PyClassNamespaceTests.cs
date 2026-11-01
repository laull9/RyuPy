using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>类体存在性检查只观察自身绑定，不因继承或描述符执行用户代码。</summary>
public class PyClassNamespaceTests
{
    [Fact]
    public void ContainsDistinguishesOwnNoneFromInheritedAndMissing()
    {
        var parent = new PyClass("Parent");
        parent.SetAttr("inherited", 3L);
        var current = (PyClass)PyOps.MakeClass("Current", [parent], null);
        current.SetAttr("present", PyNone.Instance);
        Assert.Equal(3L, PyObject.GetAttr(current, "inherited"));
        Assert.True(PyOps.ClassNamespaceContains(current, "present"));
        Assert.False(PyOps.ClassNamespaceContains(current, "inherited"));
        Assert.False(PyOps.ClassNamespaceContains(current, "missing"));
        Assert.Same(PyNone.Instance, PyOps.ClassNamespaceGet(current, "present"));
        Assert.Throws<PyNameError>(() => PyOps.ClassNamespaceGet(current, "inherited"));
    }

    [Fact]
    public void ContainsAndRawGetNeverInvokeAProperty()
    {
        int calls = 0;
        var getter = new PyFunction("value",
            PySignature.Create("value", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { calls++; return 42L; }, []);
        var property = new PyProperty(getter);
        var current = new PyClass("Current");
        current.SetAttr("value", property);
        Assert.True(PyOps.ClassNamespaceContains(current, "value"));
        Assert.Same(property, PyOps.ClassNamespaceGet(current, "value"));
        Assert.Equal(0, calls);
        Assert.Throws<PyTypeError>(() => PyOps.ClassNamespaceContains(PyNone.Instance, "value"));
    }
}
