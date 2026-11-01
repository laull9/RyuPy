using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>Literal真实值元数据、类型敏感集合和显式拒绝用户值协议。</summary>
public class PyTypingLiteralTests
{
    private static object? Form => PyTyping.CreateAttributes()["Literal"];
    private static object? Literal(params object?[] values) => PyOps.GetItem(Form, new PyTuple(values));
    private static object? Call(string name, object? value) =>
        PyOps.CallValue(PyTyping.CreateAttributes()[name], [value], [], []);

    [Fact]
    public void OriginArgumentsFlattenAndReprPreserveFirstOccurrence()
    {
        object? alias = Literal(Literal(1L, "red"), true, 1L, null, PyNone.Instance);
        Assert.Equal("typing.Literal[1, 'red', True, None]", PyRepr.Repr(alias));
        Assert.Same(Form, Call("get_origin", alias));
        Assert.Same(PyNone.Instance, Call("get_origin", Form));
        Assert.Equal("(1, 'red', True, None)", PyRepr.Repr(Call("get_args", alias)));
        Assert.Equal("typing.Literal[()]", PyRepr.Repr(Literal()));
        Assert.Equal("()", PyRepr.Repr(Call("get_args", Literal())));
    }

    [Fact]
    public void EqualityHashAndCacheAreTypeSensitiveButOrderIndependent()
    {
        object? left = Literal(true, 1L, false, 0L, "x", PyNone.Instance);
        object? right = Literal(PyNone.Instance, "x", 0L, false, 1L, true);
        Assert.True(PyHash.Equal(left, right));
        Assert.Equal(PyHash.Of(left), PyHash.Of(right));
        Assert.False(PyHash.Equal(Literal(true), Literal(1L)));
        Assert.False(PyHash.Equal(Literal(false), Literal(0L)));
        Assert.Same(Literal(1L), Literal(new BigInteger(1)));
        Assert.Same(Literal("x", 1L), Literal("x", 1L));
        Assert.NotSame(Literal(true), Literal(1L));
    }

    [Fact]
    public void UnsupportedValuesNeverInvokeUserEqualityOrHash()
    {
        var cls = new PyClass("CustomLiteralValue");
        int calls = 0;
        cls.SetAttr("__eq__", PyRuntimeDataclassTests.Fn("__eq__", ["self", "other"], _ => { calls++; return true; }));
        cls.SetAttr("__hash__", PyRuntimeDataclassTests.Fn("__hash__", ["self"], _ => { calls++; return 1L; }));
        Assert.Throws<PyNotImplementedError>(() => Literal(new PyInstance(cls)));
        Assert.Throws<PyNotImplementedError>(() => Literal(1.0));
        Assert.Equal(0, calls);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Form, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Literal(1L), [], [], []));
    }
}
