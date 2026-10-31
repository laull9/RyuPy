using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>相等性与哈希规则的差分测试：期望值均以 CPython 3.12 的实际行为为准。</summary>
public class PyHashTests
{
    [Fact]
    public void NanIsNeverEqualToItself()
    {
        object nan = double.NaN;
        Assert.False(PyHash.Equal(nan, nan));
        Assert.False(PyHash.Equal(double.NaN, 1.0));
    }

    [Fact]
    public void NanKeyIsFoundByIdentityInDict()
    {
        // 与 CPython 一致：d[nan] 能取回自己（字典查找带指针快判），但 nan == nan 仍为假
        var dict = new PyDict();
        object nan = double.NaN;
        dict.Set(nan, 1L);
        Assert.True(dict.Contains(nan));
        Assert.Equal(1L, dict.Get(nan));

        dict.Set(double.NaN, 2L);
        Assert.Equal(2L, dict.Count);
    }

    [Fact]
    public void IntFloatEqualityIsExactBeyondDoublePrecision()
    {
        var justPastDouble = BigInteger.Parse("9007199254740993");
        Assert.False(PyHash.Equal(justPastDouble, 9007199254740992.0));
        Assert.True(PyHash.Equal(BigInteger.Parse("9007199254740992"), 9007199254740992.0));
        Assert.True(PyHash.Equal(2L, 2.0));
        Assert.False(PyHash.Equal(0.5, 1L));
    }

    [Fact]
    public void TrueEqualsOneEqualsOnePointZero()
    {
        Assert.True(PyHash.Equal(true, 1L));
        Assert.True(PyHash.Equal(true, 1.0));
        Assert.Equal(PyHash.Of(true), PyHash.Of(1L));
        Assert.Equal(PyHash.Of(1L), PyHash.Of(1.0));
    }

    [Fact]
    public void ContainersCompareByContentAndHashOnlyWhenImmutable()
    {
        Assert.True(PyHash.Equal(new PyList([1L, 2L]), new PyList([1L, 2L])));
        Assert.True(PyHash.Equal(new PyTuple([1L, "a"]), new PyTuple([1L, "a"])));
        Assert.False(PyHash.Equal(new PyTuple([1L]), new PyList([1L])));

        Assert.Throws<PyTypeError>(() => PyHash.Of(new PyList()));
        Assert.Throws<PyTypeError>(() => PyHash.Of(new PyDict()));
        Assert.Throws<PyTypeError>(() => PyHash.Of(new PySet()));
    }
}
