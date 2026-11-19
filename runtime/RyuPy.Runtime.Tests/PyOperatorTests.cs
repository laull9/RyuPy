// operator 的签名、别名和共享富比较入口回归。
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

public class PyOperatorTests
{
    private static object? Call(string name, params object?[] args) =>
        PyOps.CallValue(PyOperator.CreateAttributes()[name], args, [], []);
    [Fact]
    public void InplaceRetainsListIdentityButAddDoesNot()
    {
        var left = new PyList([1L]);
        Assert.Same(left, Call("iadd", left, new PyTuple([2L])));
        Assert.Equal(new object?[] { 1L, 2L }, left.Items);
        Assert.NotSame(left, Call("add", left, new PyList([3L])));
        Assert.Equal(3L, Call("add", 1L, 2L));
    }
    [Fact]
    public void ComparisonAndPositionalSignatures()
    {
        foreach (string name in new[] { "add", "iadd", "eq", "ne", "lt", "le", "gt", "ge" })
        {
            Assert.Throws<PyTypeError>(() => Call(name, 1L));
            Assert.Throws<PyTypeError>(() => Call(name, 1L, 2L, 3L));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyOperator.CreateAttributes()[name], [], ["a", "b"], [1L, 2L]));
        }
        Assert.Equal(true, Call("eq", 2L, 2L));
        Assert.Equal(false, Call("ne", 2L, 2L));
        Assert.Equal(true, Call("lt", 1L, 2L));
        Assert.Equal(true, Call("le", 2L, 2L));
        Assert.Equal(true, Call("gt", 2L, 1L));
        Assert.Equal(true, Call("ge", 2L, 2L));
    }
    [Fact]
    public void RawComparisonPreservesValueWhileBoolAbiUsesTruth()
    {
        var cls = new PyClass("Comparison");
        var marker = new PyList([42L]);
        cls.Attributes["__eq__"] = PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2, _ => marker);
        var left = new PyInstance(cls);
        Assert.Same(marker, Call("eq", left, left));
        Assert.True(PyOps.Compare(PyOps.Eq, left, left));
        Assert.Equal(false, Call("ne", left, left));
    }
    [Fact]
    public void SliceMixedProtectionRunsBeforeOrdinaryUserCallback()
    {
        int calls = 0;
        var cls = new PyClass("Foreign");
        foreach (string name in new[] { "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__" })
            cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => { calls++; return true; }));
        var foreign = new PyInstance(cls);
        object slice = PyOps.CallValue(PySlice.Type, [2L], [], [])!;
        foreach (string name in new[] { "eq", "ne", "lt", "le", "gt", "ge" })
        {
            Assert.Throws<PyNotImplementedError>(() => Call(name, slice, foreign));
            Assert.Throws<PyNotImplementedError>(() => Call(name, foreign, slice));
        }
        Assert.Equal(0, calls);
    }
    [Theory]
    [InlineData("keys")]
    [InlineData("items")]
    [InlineData("values")]
    public void DictViewKeepsItsOwnObjectResultRejection(string kind)
    {
        var dictClass = (PyClass)PyOps.MakeClass("ViewDict", [PyType.Get("dict")], null);
        PyOps.FinalizeOrdinaryClass(dictClass);
        object mapping = dictClass.Invoke([], [], [])!;
        object view = PyOps.CallMethod(mapping, kind, [], [], [])!;
        int calls = 0;
        var cls = new PyClass("Foreign");
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { calls++; return new PyList([1L]); }));
        var foreign = new PyInstance(cls);
        Assert.Throws<PyNotImplementedError>(() => Call("eq", view, foreign));
        Assert.Throws<PyNotImplementedError>(() => Call("eq", foreign, view));
        Assert.Equal(false, Call("ne", view, foreign));
        Assert.Equal(false, Call("ne", foreign, view));
        Assert.Equal(4, calls);
    }

}
