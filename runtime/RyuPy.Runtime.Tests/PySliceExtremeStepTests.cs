// 内部切片长度计数边界；公开 slice.indices() 的 BigInteger 协议另有测试，不在这里改写。
using System;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PySliceExtremeStepTests
{
    [Fact]
    public void CountsMatchBigIntegerReferenceWithoutOverflow()
    {
        foreach (long length in new long[] { 0, 1, 5, long.MaxValue })
        foreach (object? start in new object?[] { null, long.MinValue, -1L, 0L, 1L, long.MaxValue })
        foreach (object? stop in new object?[] { null, long.MinValue, -1L, 0L, 1L, long.MaxValue })
        foreach (long step in new long[] { long.MinValue, long.MinValue + 1, -2, -1, 1, 2, long.MaxValue - 1, long.MaxValue })
        {
            var (first, last, stride, count) = new PySlice(start, stop, step).Indices(length);
            BigInteger distance = stride > 0 ? (BigInteger)last - first : (BigInteger)first - last;
            BigInteger expected = distance <= 0 ? 0 : (distance + BigInteger.Abs(stride) - 1) / BigInteger.Abs(stride);
            Assert.Equal((long)expected, count);
        }
    }

    [Fact]
    public void HugeStepsSelectOnlyTheFirstReachableCodePoint()
    {
        foreach (long step in new long[] { long.MinValue, long.MinValue + 1, long.MaxValue - 1, long.MaxValue })
        {
            Assert.Equal(step < 0 ? "😀" : "A", PyText.Slice("A猫😀", new PySlice(null, null, step)));
            Assert.Equal("", PyText.Slice("", new PySlice(null, null, step)));
            Assert.Equal("", PyText.Slice("abc", new PySlice(1L, 1L, step)));
        }
    }
}
