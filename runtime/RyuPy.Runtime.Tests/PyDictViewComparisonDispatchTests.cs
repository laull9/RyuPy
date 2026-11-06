// 审查反例view_foreign的独立回归：三类视图均保外部eq/ne调用次数、反射和容器求真。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictViewComparisonDispatchTests
{
    private static object View(string kind)
    {
        var cls = (PyClass)PyOps.MakeClass("ViewDict", [PyType.Get("dict")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object value = cls.Invoke([], [], [])!;
        PyOps.SetItem(value, "a", 1L);
        return PyOps.CallMethod(value, kind, [], [], [])!;
    }

    [Theory]
    [InlineData("keys")]
    [InlineData("items")]
    [InlineData("values")]
    public void ForeignEqAndNeAreCalledExactlyOnceFromEitherSide(string kind)
    {
        var log = new List<string>(); var cls = new PyClass("Foreign");
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { log.Add("eq"); return true; }));
        cls.SetAttr("__ne__", PyNativeFunctions.CreateVariadic("ne", _ => { log.Add("ne"); return false; }));
        var foreign = new PyInstance(cls); object view = View(kind);
        Assert.True(PyOps.Compare(PyOps.Eq, view, foreign));
        Assert.True(PyOps.Compare(PyOps.Eq, foreign, view));
        Assert.False(PyOps.Compare(PyOps.Ne, view, foreign));
        Assert.False(PyOps.Compare(PyOps.Ne, foreign, view));
        Assert.Equal(new[] { "eq", "eq", "ne", "ne" }, log);
    }

    [Theory]
    [InlineData("keys")]
    [InlineData("items")]
    [InlineData("values")]
    public void ExplicitNotImplementedNeDoesNotCallEqAgain(string kind)
    {
        int eq = 0, ne = 0; var cls = new PyClass("Reject");
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { eq++; return PyNotImplemented.Instance; }));
        cls.SetAttr("__ne__", PyNativeFunctions.CreateVariadic("ne", _ => { ne++; return PyNotImplemented.Instance; }));
        object view = View(kind); var foreign = new PyInstance(cls);
        Assert.False(PyOps.Compare(PyOps.Eq, view, foreign));
        Assert.False(PyOps.Compare(PyOps.Eq, foreign, view));
        Assert.True(PyOps.Compare(PyOps.Ne, view, foreign));
        Assert.True(PyOps.Compare(PyOps.Ne, foreign, view));
        Assert.Equal(2, eq); Assert.Equal(2, ne);
    }

    [Theory]
    [InlineData("keys")]
    [InlineData("items")]
    [InlineData("values")]
    public void MissingNeUsesEqTruthinessButExpressionEqCannotLoseObjectResult(string kind)
    {
        int calls = 0; var cls = new PyClass("ObjectResult");
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { calls++; return new PyList([1L]); }));
        object view = View(kind); var foreign = new PyInstance(cls);
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, view, foreign));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, foreign, view));
        Assert.False(PyOps.Compare(PyOps.Ne, view, foreign));
        Assert.False(PyOps.Compare(PyOps.Ne, foreign, view));
        Assert.Equal(4, calls);
        // 容器内部的RichCompareBool可以对对象结果求真，不继承表达式bool ABI的NI边界。
        Assert.True(PyHash.Equal(view, foreign)); Assert.True(PyHash.Equal(foreign, view));
        Assert.Equal(6, calls);
    }

    [Fact]
    public void ValuesIdentityAndForeignExceptionsRemainIntact()
    {
        object view = View("values"), other = View("values");
        Assert.True(PyOps.Compare(PyOps.Eq, view, view));
        Assert.False(PyOps.Compare(PyOps.Ne, view, view));
        Assert.False(PyOps.Compare(PyOps.Eq, view, other));
        Assert.True(PyOps.Compare(PyOps.Ne, view, other));
        var error = new PyValueError("foreign"); var cls = new PyClass("Throwing");
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => throw error));
        Assert.Same(error, Assert.Throws<PyValueError>(() => PyOps.Compare(PyOps.Eq, view, new PyInstance(cls))));
    }
}
