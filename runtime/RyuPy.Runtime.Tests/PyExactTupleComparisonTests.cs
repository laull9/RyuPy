// 精确tuple数组比较与派生协议、身份快捷、短路及数值跨型的回归。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

public class PyExactTupleComparisonTests
{
    [Fact]
    public void PreservesNaNIdentityAndNumericEquality()
    {
        object nan = double.NaN;
        Assert.True(PyHash.Equal(new PyTuple([nan]), new PyTuple([nan])));
        Assert.False(PyHash.Equal(new PyTuple([nan]), new PyTuple([double.NaN])));
        Assert.True(PyHash.Equal(new PyTuple([1L, true, 2.0]), new PyTuple([1.0, 1L, 2L])));
    }
    [Fact]
    public void PreservesOuterAndElementIdentityAndNestedValues()
    {
        var same = new PyTuple([new object()]);
        Assert.True(PyHash.Equal(same, same));
        Assert.True(PyHash.Equal(new PyTuple([1L, new PyTuple([2L])]), new PyTuple([1L, new PyTuple([2L])])));
        Assert.False(PyHash.Equal(new PyTuple([1L]), new PyTuple([1L, 2L])));
        Assert.False(PyHash.Equal(new PyTuple([1L]), new PyTuple([2L])));
    }
    [Fact]
    public void EqualTupleKeyReusesExistingDictionaryEntry()
    {
        var dict = new PyDict();
        var first = new PyTuple(["u", "buy"]);
        dict.Set(first, 1L); dict.Set(new PyTuple(["u", "buy"]), 2L);
        Assert.Equal(1, dict.Count); Assert.Same(first, Assert.Single(dict.Keys));
        Assert.Equal(2L, dict.Get(new PyTuple(["u", "buy"])));
    }
}
