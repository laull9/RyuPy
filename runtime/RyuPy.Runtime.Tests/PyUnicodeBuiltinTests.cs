using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyUnicodeBuiltinTests
{
    private static object? Call(string name, object? value) => PyOps.CallBuiltin(name, [value], [], []);

    [Theory]
    [InlineData(0L)]
    [InlineData(65L)]
    [InlineData(0x6c49L)]
    [InlineData(0x1f389L)]
    [InlineData(0xd800L)]
    [InlineData(0xdfffL)]
    [InlineData(0x10ffffL)]
    public void OrdChrRoundTripUsesCodePointsIncludingLoneSurrogates(long codePoint)
    {
        var text = Assert.IsType<string>(Call("chr", codePoint));
        Assert.Equal(1L, PyText.Length(text));
        Assert.Equal(codePoint, Call("ord", text));
    }

    [Fact]
    public void OrdAcceptsOnlyOneCharacterOrByte()
    {
        Assert.Equal(255L, Call("ord", new PyBytes([255])));
        Assert.Equal(128L, Call("ord", new PyByteArray([128])));
        foreach (object value in new object[] { "", "ab", "e\u0301", "🎉x", new PyBytes([]), new PyByteArray([1, 2]) })
            Assert.Throws<PyTypeError>(() => Call("ord", value));
        foreach (object? value in new object?[] { null, 65L, true, new PyList([65L]) })
            Assert.Throws<PyTypeError>(() => Call("ord", value));
    }

    [Fact]
    public void ChrSeparatesUnicodeRangeFromCIntOverflow()
    {
        foreach (object value in new object[] { -1L, 0x110000L, (long)int.MinValue, (long)int.MaxValue })
            Assert.Throws<PyValueError>(() => Call("chr", value));
        foreach (object value in new object[] { (long)int.MaxValue + 1, (long)int.MinValue - 1, BigInteger.One << 100 })
            Assert.Throws<PyOverflowError>(() => Call("chr", value));
        Assert.Equal("\u0001", Call("chr", true));
        Assert.Equal("A", Call("chr", new BigInteger(65)));
        foreach (object? value in new object?[] { null, 65.0, "65" })
            Assert.Throws<PyTypeError>(() => Call("chr", value));
    }

    [Fact]
    public void ChrCallsIndexExactlyOnceAndRejectsNonIntegerResults()
    {
        int calls = 0;
        object value = Indexed(() => { calls++; return 0x1f389L; });
        Assert.Equal("🎉", Call("chr", value));
        Assert.Equal(1, calls);
        foreach (object? result in new object?[] { 65.0, "65", null, Indexed(() => 65L) })
            Assert.Throws<PyTypeError>(() => Call("chr", Indexed(() => result)));
        Assert.Throws<PyValueError>(() => Call("chr", Indexed(() => throw new PyValueError("index failed"))));
        Assert.Throws<PyOverflowError>(() => Call("chr", Indexed(() => BigInteger.One << 100)));
    }

    [Theory]
    [InlineData("ord")]
    [InlineData("chr")]
    public void BuiltinsValidateArityAndRejectKeywords(string name)
    {
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin(name, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin(name, [1L, 2L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin(name, [], ["value"], [65L]));
    }

    private static object Indexed(Func<object?> getIndex)
    {
        var cls = (PyClass)PyOps.MakeClass("IndexValue", [], null);
        cls.SetAttr("__index__", new PyFunction("__index__", PySignature.Create(
            "__index__", ["self"], ["pos"], ["0"]), (_, _, _, _) => getIndex(), []));
        return cls.Invoke([], [], [])!;
    }
}
