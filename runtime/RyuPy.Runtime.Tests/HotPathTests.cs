using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>
/// M2.12 热路径改造的运行时单元测试：溢出检测、装箱缓存、<c>is</c>、序列运算与排序。
/// </summary>
/// <remarks>
/// 这些路径都是「快路径 + 通用路径」的双实现，最容易出错的是边界：i64 的最小 / 最大值、
/// 小整数缓存的上下界、排序中相等元素的相对次序。e2e 用例覆盖常见写法，这里覆盖边界。
/// </remarks>
public class HotPathTests
{
    [Theory]
    [InlineData(long.MaxValue, 1L)]
    [InlineData(long.MinValue, -1L)]
    [InlineData(long.MaxValue, long.MaxValue)]
    public void AddOverflowPromotesToBigInteger(long x, long y)
    {
        object result = PyPic.Binary(0, PyOps.Add, x, y);
        Assert.Equal(new BigInteger(x) + y, Assert.IsType<BigInteger>(result));
    }

    [Theory]
    [InlineData(long.MinValue, 1L)]
    [InlineData(long.MaxValue, -1L)]
    public void SubtractOverflowPromotesToBigInteger(long x, long y)
    {
        object result = PyPic.Binary(0, PyOps.Sub, x, y);
        Assert.Equal(new BigInteger(x) - y, Assert.IsType<BigInteger>(result));
    }

    [Theory]
    [InlineData(long.MinValue, -1L)]
    [InlineData(4294967296L, 4294967296L)]
    [InlineData(-3037000500L, 3037000500L)]
    public void MultiplyOverflowPromotesToBigInteger(long x, long y)
    {
        object result = PyPic.Binary(0, PyOps.Mul, x, y);
        Assert.Equal(new BigInteger(x) * y, Assert.IsType<BigInteger>(result));
    }

    [Fact]
    public void ArithmeticAtBoundaryStaysLong()
    {
        Assert.Equal(long.MaxValue, PyPic.Binary(0, PyOps.Add, long.MaxValue - 1, 1L));
        Assert.Equal(long.MinValue, PyPic.Binary(0, PyOps.Sub, long.MinValue + 1, 1L));
        Assert.Equal(-3037000499L * 3037000499L, PyPic.Binary(0, PyOps.Mul, -3037000499L, 3037000499L));
    }

    [Fact]
    public void FloorDivAndModFastPathMatchPythonSigns()
    {
        Assert.Equal(3L, PyPic.Binary(0, PyOps.FloorDiv, 7L, 2L));
        Assert.Equal(-4L, PyPic.Binary(0, PyOps.FloorDiv, -7L, 2L));
        Assert.Equal(2L, PyPic.Binary(0, PyOps.Mod, -7L, 3L));
        Assert.Equal(-2L, PyPic.Binary(0, PyOps.Mod, 7L, -3L));
    }

    [Fact]
    public void TrueDivOfLargeIntsUsesExactPath()
    {
        // 2^53 + 1 无法精确转成 double：必须走通用路径，结果与 CPython 的正确舍入一致
        long big = (1L << 53) + 1;
        Assert.Equal(PyOps.Binary(PyOps.TrueDiv, big, 1L), PyPic.Binary(0, PyOps.TrueDiv, big, 1L));
        Assert.Throws<PyZeroDivisionError>(() => PyPic.Binary(0, PyOps.TrueDiv, 1L, 0L));
        Assert.Throws<PyZeroDivisionError>(() => PyPic.Binary(0, PyOps.TrueDiv, 1.0, 0.0));
    }

    [Fact]
    public void SmallIntsAreCachedWithinCPythonRange()
    {
        Assert.Same(PyOps.BoxInt(-5), PyOps.BoxInt(-5));
        Assert.Same(PyOps.BoxInt(256), PyOps.BoxInt(256));
        Assert.NotSame(PyOps.BoxInt(257), PyOps.BoxInt(257));
        Assert.NotSame(PyOps.BoxInt(-6), PyOps.BoxInt(-6));
        Assert.Same(PyOps.BoxBool(true), PyOps.TrueObject);
        Assert.Same(PyOps.BoxBool(false), PyOps.FalseObject);
    }

    [Fact]
    public void IsComparesSingletonsByValue()
    {
        // 运行时其它位置可能各自装箱 bool / 小整数；`is` 不能依赖装箱来源
        object freshTrue = true;
        Assert.True(PyOps.Is(freshTrue, PyOps.TrueObject));
        Assert.False(PyOps.Is(freshTrue, PyOps.FalseObject));
        Assert.True(PyOps.Is((object)7L, (object)7L));
        Assert.False(PyOps.Is((object)1000L, (object)1000L));
        Assert.False(PyOps.Is((object)1L, (object)true));
    }

    [Fact]
    public void InPlaceAddOnListKeepsIdentity()
    {
        var list = new PyList(new object?[] { 1L });
        object result = PyOps.Binary(PyOps.Add | PyOps.InPlaceFlag, list, new PyTuple([2L, 3L]));
        Assert.Same(list, result);
        Assert.Equal(new object?[] { 1L, 2L, 3L }, list.Items);
    }

    [Fact]
    public void InPlaceErrorMessageUsesAugmentedOperator()
    {
        var error = Assert.Throws<PyTypeError>(
            () => PyOps.Binary(PyOps.Sub | PyOps.InPlaceFlag, 1L, "a")
        );
        Assert.Contains("-=", error.Message);
    }

    [Fact]
    public void SequenceRepeatHandlesZeroNegativeAndBool()
    {
        Assert.Equal("", PyOps.Binary(PyOps.Mul, "ab", -3L));
        Assert.Equal("abab", PyOps.Binary(PyOps.Mul, 2L, "ab"));
        var repeated = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList(new object?[] { 0L }), true));
        Assert.Single(repeated.Items);
        Assert.Throws<PyTypeError>(() => PyOps.Binary(PyOps.Mul, "ab", 1.5));
    }

    [Fact]
    public void SortIsStableForEqualKeysInBothDirections()
    {
        // 键相等的元素必须保持输入顺序，reverse 也一样
        var items = new List<object?>();
        for (long i = 0; i < 200; i++)
        {
            items.Add(new PyTuple([i % 3, i]));
        }
        var ascending = new List<object?>(items);
        PySort.SortBy(ascending, reverse: false, item => ((PyTuple)item!).Items[0]);
        var descending = new List<object?>(items);
        PySort.SortBy(descending, reverse: true, item => ((PyTuple)item!).Items[0]);
        AssertStable(ascending, expectedFirstKey: 0L);
        AssertStable(descending, expectedFirstKey: 2L);
    }

    private static void AssertStable(List<object?> sorted, long expectedFirstKey)
    {
        Assert.Equal(expectedFirstKey, ((PyTuple)sorted[0]!).Items[0]);
        for (int i = 1; i < sorted.Count; i++)
        {
            var previous = ((PyTuple)sorted[i - 1]!).Items;
            var current = ((PyTuple)sorted[i]!).Items;
            if (Equals(previous[0], current[0]))
            {
                Assert.True((long)previous[1]! < (long)current[1]!, "相等键的原始顺序被打乱");
            }
        }
    }
}
