using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>普通 Enum 的定义顺序、同值身份、只读守卫与值查找，不依赖编译器特殊降级。</summary>
public class PyEnumTests
{
    private static PyClass Color()
    {
        var cls = (PyClass)PyEnum.Create("NativeColor", [PyEnum.CreateAttributes()["Enum"]], PyNone.Instance);
        cls.SetAttr("RED", 1L); cls.SetAttr("ALIAS", true); cls.SetAttr("BLUE", 2L);
        Assert.Same(cls, PyEnum.Finalize(cls, ["RED", "ALIAS", "BLUE"]));
        return cls;
    }
    [Fact]
    public void AliasesShareCanonicalObjectAndIterationSkipsThem()
    {
        PyClass cls = Color(); object red = PyEnum.GetItem(cls, "RED")!;
        Assert.Same(red, PyEnum.GetItem(cls, "ALIAS"));
        Assert.Same(red, PyEnum.Construct(cls, [1L], [], []));
        Assert.Same(red, PyEnum.Construct(cls, [red], [], []));
        Assert.Equal(2L, PyEnum.Length(cls));
        Assert.Equal(new object?[] { red, PyEnum.GetItem(cls, "BLUE") }, PyOps.ToSequence(cls));
        Assert.Equal(new object?[] { red, PyEnum.GetItem(cls, "BLUE") }, PyOps.ToSequence(PyEnum.Iterate(cls)));
        Assert.True(PyEnum.TryGetMemberAttr((PyInstance)red, "name", out object? name)); Assert.Equal("RED", name);
        Assert.Equal(PyHash.Of("RED"), PyEnum.MemberHash((PyInstance)red));
    }
    [Fact]
    public void MutationAndLookupErrorsArePythonExceptions()
    {
        PyClass cls = Color(); var red = (PyInstance)PyEnum.GetItem(cls, "RED")!;
        Assert.Throws<PyAttributeError>(() => PyEnum.GuardClassMutation(cls, "RED"));
        Assert.Throws<PyAttributeError>(() => PyEnum.GuardMemberMutation(red, "value"));
        Assert.Throws<PyValueError>(() => PyEnum.Construct(cls, [new PyList()], [], []));
        Assert.Throws<PyKeyError>(() => PyEnum.GetItem(cls, "missing"));
        Assert.Equal("NativeColor.RED", PyEnum.MemberRepr(red, false));
    }
    [Fact]
    public void MemberProxyDoesNotExposeMutableDictionaryProtocol()
    {
        PyClass cls = Color(); Assert.True(PyEnum.TryGetClassAttr(cls, "__members__", out object? proxy));
        Assert.IsType<PyMappingProxy>(proxy); Assert.False(proxy is IPyNativeMapping);
        Assert.Throws<PyAttributeError>(() => ((PyMappingProxy)proxy!).GetAttr("__setitem__"));
    }
}
