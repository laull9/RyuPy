using System;
using System.Numerics;
using System.Text;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>JSON API单测：大整数、字节编码、回调、错误位置与宿主异常隔离。</summary>
public class PyJsonTests
{
    private static object? Call(string name, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyJsonModule.CreateAttributes()[name], args, names ?? [], values ?? []);

    [Fact]
    public void ArbitraryIntegerAndUnicodeRoundTrip()
    {
        var value = new PyDict(); value.Set("😀", BigInteger.Pow(10, 30));
        string encoded = Assert.IsType<string>(Call("dumps", [value]));
        Assert.Equal("{\"\\ud83d\\ude00\": 1000000000000000000000000000000}", encoded);
        Assert.True(PyOps.ValuesEqual(value, Call("loads", [encoded])));
    }

    [Fact]
    public void ErrorPositionUsesCodePoints()
    {
        var error = Assert.Throws<PyJSONDecodeError>(() => Call("loads", ["[\"😀\",\n?]"]));
        Assert.Equal(6L, error.Pos); Assert.Equal(2L, error.LineNo); Assert.Equal(1L, error.ColNo);
        Assert.Equal("Expecting value", error.Msg);
    }

    [Fact]
    public void ByteInputDetectsBomAndEndian()
    {
        foreach (Encoding encoding in new Encoding[] { new UTF8Encoding(true), new UnicodeEncoding(false, true),
            new UnicodeEncoding(true, true), new UTF32Encoding(false, true), new UTF32Encoding(true, true) })
        {
            byte[] preamble = encoding.GetPreamble(), content = encoding.GetBytes("[123,\"猫\"]");
            var bytes = new byte[preamble.Length + content.Length]; preamble.CopyTo(bytes, 0); content.CopyTo(bytes, preamble.Length);
            Assert.Equal("[123, '猫']", PyRepr.Repr(Call("loads", [new PyBytes(bytes)])));
        }
        Assert.Throws<PyUnicodeDecodeError>(() => Call("loads", [new PyBytes([255])]));
    }

    [Fact]
    public void HooksPreserveDuplicatesAndNumericTokens()
    {
        var echo = PyNativeFunctions.Create("echo", ["value"], [null], 1, args => args[0]);
        Assert.Equal("[('a', '1'), ('a', '2')]", PyRepr.Repr(Call("loads", ["{\"a\":1,\"a\":2}"],
            ["parse_int", "object_pairs_hook"], [echo, echo])));
        Assert.Equal("NaN", Call("loads", ["NaN"], ["parse_constant"], [echo]));
    }

    [Fact]
    public void SortingMixedKeysRaisesPythonError()
    {
        var value = new PyDict(); value.Set("x", 1L); value.Set(2L, 3L);
        Assert.Throws<PyTypeError>(() => Call("dumps", [value], ["sort_keys"], [true]));
    }

    [Fact]
    public void CircularAndUnsupportedParametersAreExplicit()
    {
        var value = new PyList(); value.Items.Add(value);
        Assert.Throws<PyValueError>(() => Call("dumps", [value]));
        Assert.Throws<PyRecursionError>(() => Call("dumps", [value], ["check_circular"], [false]));
        Assert.Throws<PyNotImplementedError>(() => Call("loads", ["1"], ["cls"], [1L]));
        Assert.Throws<PyTypeError>(() => Call("dumps", [1L], ["unknown"], [true]));
    }
    [Fact]
    public void CustomFlagIsRejectedWithoutCallingTruthProtocol()
    {
        int calls = 0;
        var cls = new PyClass("Flag");
        cls.SetAttr("__bool__", PyNativeFunctions.Create("__bool__", ["self"], [null], 1,
            _ => { calls++; return true; }));
        Assert.Throws<PyNotImplementedError>(() => Call("dumps", [1L], ["sort_keys"], [new PyInstance(cls)]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void IntegerKeyAndAsciiControlUseSameScalarRules()
    {
        var dict = new PyDict(); dict.Set(BigInteger.Pow(10, 4300), 1L);
        Assert.Throws<PyValueError>(() => Call("dumps", [dict]));
        Assert.Equal("\"\\u007f\"", Call("dumps", ["\x7f"]));
        Assert.Equal("'\\x1c'", PyRepr.Repr("\x1c"));
        Assert.Equal("'\\u2028'", PyRepr.Repr("\u2028"));
        Assert.Equal("'😀'", PyRepr.Repr("😀"));
        Assert.Equal("'\\ud800'", PyRepr.Repr("\ud800"));
    }

}
