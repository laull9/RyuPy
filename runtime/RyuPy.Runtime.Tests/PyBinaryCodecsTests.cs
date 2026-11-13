// 编码合同直接调用静态绑定，验证 BCL 结果、Python 解码状态、真实 buffer 与参数错误。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyBinaryCodecsTests
{
    private static readonly Dictionary<string, object?> Base64 = PyBase64.CreateAttributes();
    private static readonly Dictionary<string, object?> Binary = PyBinascii.CreateAttributes();
    private static PyBytes Bytes(string text) => new(Encoding.ASCII.GetBytes(text));
    private static object? Call(Dictionary<string, object?> module, string name, params object?[] args) =>
        PyOps.CallValue(module[name], args, [], []);
    private static byte[] Result(object? value) => Assert.IsType<PyBytes>(value).Bytes;

    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("f", "Zg==", "MY======", "66")]
    [InlineData("foobar", "Zm9vYmFy", "MZXW6YTBOI======", "666F6F626172")]
    public void RfcVectorsAndRoundTrips(string text, string encoded64, string encoded32, string encoded16)
    {
        foreach (var pair in new[] { ("64", encoded64), ("32", encoded32), ("16", encoded16) })
        {
            Assert.Equal(Bytes(pair.Item2).Bytes, Result(Call(Base64, "b" + pair.Item1 + "encode", Bytes(text))));
            Assert.Equal(Bytes(text).Bytes, Result(Call(Base64, "b" + pair.Item1 + "decode", pair.Item2)));
        }
    }

    [Fact]
    public void FullByteRangeAndRawCharacterViews()
    {
        byte[] data = new byte[256];
        for (int index = 0; index < data.Length; index++) data[index] = (byte)index;
        var view = (PyMemoryView)PyMemoryView.Create([new PyByteArray(data)], [], []);
        var characters = Assert.IsType<PyMemoryView>(PyOps.CallValue(view.GetAttr("cast"), ["c"], [], []));
        try
        {
            foreach (string radix in new[] { "16", "32", "64" })
                Assert.Equal(data, Result(Call(Base64, "b" + radix + "decode", Call(Base64, "b" + radix + "encode", characters))));
            view.Release();
            Assert.Throws<PyValueError>(() => Call(Base64, "b64encode", view));
            Assert.Equal(data, PyBinaryBuffer.Read(characters).ToArray());
        }
        finally { characters.Release(); view.Release(); }
    }

    [Theory]
    [InlineData("=", "Leading padding not allowed")]
    [InlineData("AAAA=", "Excess padding not allowed")]
    [InlineData("A=A", "Discontinuous padding not allowed")]
    [InlineData("AA==x", "Excess data after padding")]
    [InlineData("AA!", "Only base64 data is allowed")]
    [InlineData("AA=", "Incorrect padding")]
    public void StrictDecodeReportsPythonFailure(string text, string message)
    {
        var failure = Assert.Throws<PyBinasciiError>(() =>
            PyOps.CallValue(Binary["a2b_base64"], [text], ["strict_mode"], [true]));
        Assert.Equal(message, failure.Message);
        Assert.IsAssignableFrom<PyValueError>(failure);
    }

    [Fact]
    public void PermissivePaddingAndUnusedBitsFollowPython()
    {
        Assert.Equal(new byte[] { 0 }, Result(Call(Base64, "b64decode", "A=A==garbage")));
        Assert.Equal(new byte[] { 0 }, Result(Call(Base64, "b64decode", "AB==")));
        Assert.Equal(new byte[] { 102 }, Result(Call(Base64, "b32decode", "MZ======")));
        Assert.Empty(Result(Call(Base64, "b64decode", "====")));
        Assert.Throws<PyBinasciiError>(() => Call(Base64, "b32decode", "========"));
        Assert.Throws<PyBinasciiError>(() => Call(Base64, "b16decode", "gg"));
        Assert.Throws<PyAssertionError>(() => Call(Base64, "b64encode", Bytes("a"), Bytes("x")));
    }

    [Fact]
    public void HexAliasesGroupingAndCrcUnsignedMask()
    {
        Assert.Same(Binary["hexlify"], Binary["b2a_hex"]);
        Assert.Same(Binary["unhexlify"], Binary["a2b_hex"]);
        Assert.Equal(Bytes("61:6263").Bytes, Result(Call(Binary, "hexlify", Bytes("abc"), ":", 2L)));
        Assert.Equal(Bytes("6162:63").Bytes, Result(Call(Binary, "hexlify", Bytes("abc"), ":", -2L)));
        Assert.Equal(new byte[] { 54, 49, 255, 54, 50 }, Result(Call(Binary, "hexlify", Bytes("ab"), new PyBytes([255]))));
        Assert.Equal(0xcbf43926L, Call(Binary, "crc32", Bytes("123456789")));
        object? prefix = Call(Binary, "crc32", Bytes("1234"));
        Assert.Equal(0xcbf43926L, Call(Binary, "crc32", Bytes("56789"), prefix));
        Assert.Equal(Call(Binary, "crc32", Bytes("abc"), 0L), Call(Binary, "crc32", Bytes("abc"), BigInteger.One << 100));
        Assert.Throws<PyBinasciiError>(() => Call(Binary, "unhexlify", "0"));
        Assert.Throws<PyBinasciiError>(() => Call(Binary, "unhexlify", "gg"));
        Assert.Throws<PyValueError>(() => Call(Binary, "hexlify", Bytes("a"), "é"));
        Assert.Throws<PyOverflowError>(() => Call(Binary, "hexlify", Bytes("a"), ":", BigInteger.One << 100));
    }

    [Fact]
    public void BufferLeaseKeepsExportsAliveAndReleasesOnFailure()
    {
        var data = new PyByteArray([65]);
        var view = (PyMemoryView)PyMemoryView.Create([data], [], []);
        using (var lease = PyBinaryBuffer.Acquire(view))
        {
            Assert.Throws<PyBufferError>(() => view.Release());
            Assert.Throws<PyBufferError>(() => data.Append(66L));
            data.Bytes[0] = 67;
            Assert.Equal(new byte[] { 67 }, lease.Span.ToArray());
        }
        view.Release();
        data.Append(68L);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Binary["b2a_base64"], ["bad"], ["newline"], [true]));
        Assert.Throws<PyTypeError>(() => Call(Binary, "crc32", data, "bad"));
        data.Append(69L);
        Assert.Equal(new byte[] { 67, 68, 69 }, data.Bytes.ToArray());
    }

    [Fact]
    public void RejectsFakeBuffersUnicodeAndUnknownParameters()
    {
        foreach (object? invalid in new object?[] { "abc", new PyList([1L]), 1L, PyNone.Instance })
            foreach (string name in new[] { "b64encode", "b32encode", "b16encode" })
                Assert.Throws<PyTypeError>(() => Call(Base64, name, invalid));
        Assert.Throws<PyValueError>(() => Call(Base64, "b64decode", "é"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Binary["crc32"], [], ["data"], [Bytes("a")]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Base64["b64encode"], [Bytes("a")], ["unknown"], [true]));
        Assert.Empty(Result(PyOps.CallValue(Binary["b2a_base64"], [Bytes("")], ["newline"], [new PyList([])])));
    }
}
