using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>绑定状态与槽位值分离；内部名字和外部模块属性使用各自的 Python 异常。</summary>
public class PyGlobalBindingTests
{
    [Fact]
    public void TypedBindingCheckUsesExplicitFlag()
    {
        PyOps.CheckGlobalBound(true, "zero");
        PyModules.CheckBound(true, "sample", "false_value");
        Assert.Equal("name 'missing' is not defined",
            Assert.Throws<PyNameError>(() => PyOps.CheckGlobalBound(false, "missing")).PyMessage);
        Assert.Equal("module 'sample' has no attribute 'missing'",
            Assert.Throws<PyAttributeError>(() => PyModules.CheckBound(false, "sample", "missing")).PyMessage);
    }

    [Theory]
    [InlineData("locals")]
    [InlineData("len")]
    public void UnboundBuiltinFallbackIsExplicitRatherThanFalseAbsence(string name)
    {
        Assert.Throws<PyNotImplementedError>(() => PyOps.CheckGlobalBound(false, name));
        Assert.Throws<PyNotImplementedError>(() => PyErrors.LoadName(PyErrors.Unbound(), name));
        PyOps.CheckGlobalBound(true, name);
        Assert.Same(PyNone.Instance, PyErrors.LoadName(PyNone.Instance, name));
        Assert.Throws<PyAttributeError>(() => PyModules.LoadAttribute(null, "sample", name));
    }

    [Fact]
    public void EllipsisFallbackDoesNotReplaceAnExistingBindingOrModuleAttribute()
    {
        Assert.Same(PyEllipsis.Instance, PyErrors.LoadName(null, "Ellipsis"));
        Assert.Same(PyEllipsis.Instance, PyErrors.LoadName(PyErrors.Unbound(), "Ellipsis"));
        Assert.Same(PyNone.Instance, PyErrors.LoadName(PyNone.Instance, "Ellipsis"));
        object bound = 1L;
        Assert.Same(bound, PyErrors.LoadName(bound, "Ellipsis"));
        Assert.Throws<PyAttributeError>(() => PyModules.LoadAttribute(null, "sample", "Ellipsis"));
    }

    [Fact]
    public void DynamicModuleAttributeRejectsOnlyUnboundSentinels()
    {
        Assert.Throws<PyAttributeError>(() => PyModules.LoadAttribute(null, "sample", "missing"));
        Assert.Throws<PyAttributeError>(() => PyModules.LoadAttribute(PyErrors.Unbound(), "sample", "missing"));
        Assert.Same(PyNone.Instance, PyModules.LoadAttribute(PyNone.Instance, "sample", "present"));
        object zero = 0L;
        Assert.Same(zero, PyModules.LoadAttribute(zero, "sample", "present"));
        object value = new object();
        Assert.Same(value, PyModules.LoadAttribute(value, "sample", "present"));
    }
}
