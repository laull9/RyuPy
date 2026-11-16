using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>动态基类守卫按身份检查完整祖先链，不把非类/特殊布局伪装为普通继承。</summary>
[Collection("生成器帧栈")]
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
    public void ValuesAndUnsupportedBuiltinTypesAreExplicitlyRejected()
    {
        foreach (object? value in new object?[] { null, PyNone.Instance, 7L, "Base", PyType.Get("tuple"), PyType.Get("ValueError") })
            Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(value));
    }

    [Fact]
    public void CanonicalListBasePreservesItsIdentity()
    {
        Assert.Same(PyType.Get("list"), PyRuntimeBases.Validate(PyType.Get("list")));
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

    private static void FailureHasCallerAndAmbientContext(object? value)
    {
        long depth = PyTraceback.PushFrame("runtime_base_test.py", "caller");
        var outer = new PyValueError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(37);
            var error = Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(value));
            Assert.Equal("runtime bases currently require an ordinary user class; special classes and metaclass protocols are not supported", error.PyMessage);
            Assert.Same(outer, error.Context);
            Assert.Equal(new PyStackFrame("runtime_base_test.py", "caller", 37), error.Frames[0]);
            var frames = error.Frames;
            PyTraceback.SetLine(99);
            Assert.Same(error, PyErrors.PrepareRaise(error));
            Assert.Same(frames, error.Frames);
            Assert.Same(outer, error.Context);
            Assert.Equal(37, error.Frames[0].Line);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void NonClassGuardPreparesItsOwnErrorAtCaller()
    {
        FailureHasCallerAndAmbientContext(7L);
        // ADR0106开放canonical dict；tuple仍代表尚未开放的原生布局。
        FailureHasCallerAndAmbientContext(PyType.Get("tuple"));
    }

    [Fact]
    public void SpecialAncestorGuardPreparesItsOwnErrorAtCaller()
    {
        PyClass special = Class("Special");
        special.ExceptionFactory = (_, _) => new PyValueError("unused");
        FailureHasCallerAndAmbientContext(Class("Wrapper", special));
    }
}
