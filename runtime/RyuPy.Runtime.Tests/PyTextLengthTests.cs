// 码点长度快路径合同：向量扫描不得改变配对、孤立代理项或边界语义。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyTextLengthTests
{
    [Fact]
    public void ExactCodePointSemantics()
    {
        // 孤立代理不能放InlineData：自定义属性UTF8序列化会先替换它们，测试须运行时构造。
        foreach (var (text, expected) in new (string, long)[]
        {
            ("", 0), ("abc", 3), ("中文é", 3), ("\ud83d\ude00", 1),
            ("\ud800", 1), ("\udc00", 1), ("\ud800\ud800\udc00", 2), ("\udc00\ud800", 2),
        }) Assert.Equal(expected, PyText.Length(text));
    }

    [Fact]
    public void PairsAcrossVectorBoundariesAndLargeBmpRuns()
    {
        for (int offset = 0; offset < 130; offset++)
        {
            string text = new string('中', offset) + "\ud83d\ude00" + new string('x', 257);
            Assert.Equal(offset + 258L, PyText.Length(text));
            Assert.Equal(offset + 259L, PyText.Length(text + "\ud800"));
        }
        Assert.Equal(100_000L, PyText.Length(new string('a', 100_000)));
    }

    [Fact]
    public void MixedUtf16MatchesIndependentScalarDefinition()
    {
        var random = new Random(1729);
        for (int sample = 0; sample < 1000; sample++)
        {
            var chars = new char[random.Next(0, 1024)];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)random.Next(0, 65536);
            long expected = 0;
            for (int i = 0; i < chars.Length; i++, expected++)
                if (chars[i] >= '\ud800' && chars[i] <= '\udbff' && i + 1 < chars.Length
                    && chars[i + 1] >= '\udc00' && chars[i + 1] <= '\udfff') i++;
            Assert.Equal(expected, PyText.Length(new string(chars)));
        }
    }
}
