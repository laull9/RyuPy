// 向量下标快路径与独立标量定义对照，覆盖合法配对及孤立代理项。
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyTextIndexTests
{
    private static void Check(string text)
    {
        var offsets = new List<int>();
        for (int i = 0; i < text.Length; i++)
        {
            offsets.Add(i);
            if (text[i] >= '\ud800' && text[i] <= '\udbff' && i + 1 < text.Length
                && text[i + 1] >= '\udc00' && text[i + 1] <= '\udfff') i++;
        }
        offsets.Add(text.Length);
        int length = offsets.Count - 1;
        for (int i = 0; i <= length; i++) Assert.Equal(offsets[i], PyText.OffsetOf(text, i));
        foreach (long invalid in new[] { -1L, long.MinValue, long.MaxValue, length + 1L })
            Assert.Equal(-1, PyText.OffsetOf(text, invalid));
        for (int i = 0; i < length; i++)
        {
            string expected = text.Substring(offsets[i], offsets[i + 1] - offsets[i]);
            Assert.Equal(expected, PyText.At(text, i));
            Assert.Equal(expected, PyText.At(text, i - length));
        }
        foreach (long invalid in new[] { long.MinValue, long.MaxValue, (long)length, -length - 1L })
            Assert.Throws<PyIndexError>(() => PyText.At(text, invalid));
        var points = offsets.Zip(offsets.Skip(1), (a, b) => text.Substring(a, b - a)).ToArray();
        foreach (long step in new long[] { -3, -1, 1, 2 })
            foreach (long start in new long[] { -length - 2, -1, 0, length / 2, length + 2 })
            {
                var slice = new PySlice(start, null, step);
                var (first, _, stride, count) = slice.Indices(length);
                var expected = new System.Text.StringBuilder();
                for (long n = 0, i = first; n < count; n++, i += stride) expected.Append(points[i]);
                Assert.Equal(expected.ToString(), PyText.Slice(text, slice));
            }
    }

    [Fact]
    public void BoundariesAndIsolatedSurrogates()
    {
        foreach (string text in new[] { "", "abc", "中文é", "\ud800", "\udc00", "\ud800\ud800\udc00", "\udc00\ud800" }) Check(text);
        for (int n = 0; n < 130; n++) Check(new string('中', n) + "\ud83d\ude00" + "xyz\ud800");
    }

    [Fact]
    public void RandomUtf16MatchesIndependentScalarOffsets()
    {
        var random = new Random(1729);
        for (int sample = 0; sample < 300; sample++)
        {
            var chars = new char[random.Next(0, 128)];
            for (int i = 0; i < chars.Length; i++) chars[i] = (char)random.Next(0, 65536);
            Check(new string(chars));
        }
    }
}
