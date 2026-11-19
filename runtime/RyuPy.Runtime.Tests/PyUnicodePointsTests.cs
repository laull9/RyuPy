// 精确代理码点边界与CLR intern隔离；测试真实Runtime通用入口而非复制实现。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;
public sealed class PyUnicodePointsTests
{
    [Fact]
    public void ExplicitPairsNeverPolluteInternedEmoji()
    {
        string emoji = "😀";
        string pair = PyOps.UnicodeFromCodePoints("\0\ud83d\0\ude00");
        Assert.False(ReferenceEquals(pair, emoji));
        Assert.Equal(2, PyText.Length(pair));
        Assert.Equal(1, PyText.Length(emoji));
        Assert.False(PyHash.Equal(pair, emoji));
        Assert.Equal("'\\ud83d\\ude00'", PyRepr.Quote(pair));
        Assert.Equal("'😀'", PyRepr.Quote(emoji));
        var dict = new PyDict(); dict.Set(pair, 1L); dict.Set(emoji, 2L);
        Assert.Equal(2, dict.Count);
    }
    [Fact]
    public void ConcatSliceRepeatAndJoinRetainBoundaries()
    {
        string pair = Builtins.StrConcat("\ud83d", "\ude00");
        Assert.Equal(2, PyText.Length(pair));
        Assert.Equal(0xd83d, PyText.At(pair, 0)[0]);
        Assert.Equal(4, PyText.Length(PyUnicodePoints.Repeat(pair, 2)));
        Assert.Equal(2, PyText.Length(PyUnicodePoints.Join("", ["\ud83d", "\ude00"])));
        Assert.Throws<PyUnicodeEncodeError>(() => PyEncoding.Encode(pair, "utf-8"));
        Assert.Equal(new byte[] {240,159,152,128}, PyEncoding.Encode("😀", "utf-8"));
    }
    [Fact]
    public void MalformedNeutralInputHasPythonErrors()
    {
        Assert.Throws<PyValueError>(() => PyOps.UnicodeFromCodePoints("x"));
        Assert.Throws<PyValueError>(() => PyOps.UnicodeFromCodePoints("\u0011\0"));
    }
}
