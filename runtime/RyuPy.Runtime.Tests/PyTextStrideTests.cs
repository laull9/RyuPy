// 字符串步进切片回归：按码点抽样，并保留原输入中的配对及孤立代理项。
using System;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyTextStrideTests
{
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("A😀猫𝄞Z")]
    [InlineData("\ud800a\udc00b\ud800\ud800\udc00\udc00z")]
    public void EverySmallWindowMatchesIndependentCodePointSelection(string text)
    {
        // 参考实现先独立拆码点，不复用待测 OffsetOf 或 Slice 的游标算法。
        string[] points = CodePoints(text);
        object?[] bounds = [null, -20L, -5L, -2L, -1L, 0L, 1L, 2L, 5L, 20L];
        foreach (object? start in bounds)
        foreach (object? stop in bounds)
        foreach (long step in new long[] { -20, -3, -2, -1, 1, 2, 3, 20 })
        {
            var slice = new PySlice(start, stop, step);
            var (first, _, stride, count) = slice.Indices(points.Length);
            string expected = string.Concat(Enumerable.Range(0, (int)count)
                .Select(i => points[(int)(first + i * stride)]));
            Assert.Equal(expected, PyText.Slice(text, slice));
        }
    }

    [Fact]
    public void LongReverseAndSparseStepsPreserveCodePoints()
    {
        string text = string.Concat(Enumerable.Repeat("A😀猫𝄞Z", 2048));
        string[] points = CodePoints(text);
        Assert.Equal(string.Concat(points.Reverse()), PyText.Slice(text, new PySlice(null, null, -1L)));
        Assert.Equal(text, PyText.Slice(PyText.Slice(text, new PySlice(null, null, -1L)), new PySlice(null, null, -1L)));
        Assert.Equal("Z", PyText.Slice(text, new PySlice(null, null, -1000000L)));
        Assert.Throws<PyValueError>(() => PyText.Slice(text, new PySlice(null, null, 0L)));
    }

    [Fact]
    public void ExistingNativeIdentityContractIsPreserved()
    {
        // 这是当前原生运行时的引用合同，不宣称 CPython 对所有字符串都承诺相同身份。
        string text = new("A😀猫Z".ToCharArray());
        Assert.Same(string.Empty, PyText.Slice(text, new PySlice(1L, 1L, 2L)));
        Assert.Same(string.Empty, PyText.Slice(string.Empty, new PySlice(null, null, -1L)));
        Assert.Same(text, PyText.Slice(text, new PySlice(null, null, 1L)));
        foreach (string single in new[] { new string('A', 1), new string("😀".ToCharArray()) })
        {
            Assert.Same(single, PyText.Slice(single, new PySlice(null, null, 1L)));
            string stepped = PyText.Slice(single, new PySlice(null, null, -1L));
            Assert.Equal(single, stepped);
            Assert.NotSame(single, stepped);
            Assert.NotSame(stepped, PyText.Slice(single, new PySlice(null, null, -1L)));
        }
        string first = PyText.Slice(text, new PySlice(null, null, 2L));
        Assert.Equal("A猫", first);
        Assert.NotSame(text, first);
        Assert.NotSame(first, PyText.Slice(text, new PySlice(null, null, 2L)));
    }

    private static string[] CodePoints(string text)
    {
        var points = new System.Collections.Generic.List<string>();
        for (int i = 0; i < text.Length; i++)
        {
            int width = char.IsHighSurrogate(text[i]) && i + 1 < text.Length
                && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
            points.Add(text.Substring(i, width));
            i += width - 1;
        }
        return points.ToArray();
    }
}
