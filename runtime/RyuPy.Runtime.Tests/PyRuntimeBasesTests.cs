using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>动态基类守卫按身份检查完整祖先链，不把非类/特殊布局伪装为普通继承。</summary>
public class PyRuntimeBasesTests
{
    private static PyClass Class(string name, params object?[] bases) =>
        (PyClass)PyOps.MakeClass(name, bases, null);

    [Fact]
    public void OrdinaryClassesPreserveIdentityAcrossSeveralAncestors()
    {
        PyClass first = Class("First");
        PyClass second = Class("Second", first);
        PyClass third = Class("Third", second);
        Assert.Same(first, PyRuntimeBases.Validate(first));
        Assert.Same(third, PyRuntimeBases.Validate(third));
        first.SetAttr("__dataclass_fields__", "not a real dataclass marker");
        Assert.Same(third, PyRuntimeBases.Validate(third));
    }

    [Fact]
    public void ValuesAndBuiltinTypesAreExplicitlyUnsupported()
    {
        foreach (object? value in new object?[] { null, PyNone.Instance, 7L, "Base", PyType.Get("list"), PyType.Get("ValueError") })
            Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(value));
    }

    [Fact]
    public void DataclassAndNativeAncestorsCannotHideBehindOrdinaryWrappers()
    {
        PyClass data = Class("Data");
        PyOps.DeclareDataclass(data, [], false);
        PyOps.ApplyDataclass(data, false);
        PyClass wrapper = Class("Wrapper", data);
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(Class("Child", wrapper)));
        PyClass native = Class("TestWrapper", PyUnittest.TestCase);
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(Class("TestChild", native)));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(PyEnum.CreateAttributes()["Enum"]));
    }

    [Fact]
    public void ExceptionFactoryIdentityIsRejectedEvenWithoutBuiltinName()
    {
        PyClass exception = Class("ExceptionLike");
        exception.ExceptionFactory = (_, _) => new PyValueError("unused");
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(Class("Wrapper", exception)));
    }
}
