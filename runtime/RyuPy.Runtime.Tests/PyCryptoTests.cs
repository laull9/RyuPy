// 摘要和 HMAC 合同覆盖固定向量、增量复制、密钥快照、参数边界与失败后状态保持。
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyCryptoTests
{
    private static readonly Dictionary<string, object?> Hashes = PyHashlib.CreateAttributes();
    private static readonly Dictionary<string, object?> Mac = PyHmac.CreateAttributes();
    private static PyBytes Bytes(string text) => new(Encoding.ASCII.GetBytes(text));
    private static object? Call(Dictionary<string, object?> module, string name, params object?[] args) =>
        PyOps.CallValue(module[name], args, [], []);
    private static object? Method(PyHashState state, string name, params object?[] args) =>
        PyOps.CallValue(state.GetAttr(name), args, [], []);

    [Theory]
    [InlineData("sha256", 32, 64, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("sha512", 64, 128, "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f")]
    [InlineData("sha1", 20, 64, "a9993e364706816aba3e25717850c26c9cd0d89d")]
    [InlineData("md5", 16, 64, "900150983cd24fb0d6963f7d28e17f72")]
    public void KnownDigestIncrementalCopyAndMetadata(string name, int size, int block, string expected)
    {
        var state = Assert.IsType<PyHashState>(Call(Hashes, name, Bytes("ab")));
        var branch = Assert.IsType<PyHashState>(Method(state, "copy"));
        Assert.Equal(name, state.GetAttr("name"));
        Assert.Equal((long)size, state.GetAttr("digest_size"));
        Assert.Equal((long)block, state.GetAttr("block_size"));
        Assert.Same(PyNone.Instance, Method(state, "update", Bytes("c")));
        Assert.Equal(expected, Method(state, "hexdigest"));
        Assert.Equal(Convert.FromHexString(expected), Assert.IsType<PyBytes>(Method(state, "digest")).Bytes);
        Assert.Equal(expected, Method(state, "hexdigest"));
        Method(branch, "update", Bytes("d"));
        Assert.NotEqual(Method(state, "hexdigest"), Method(branch, "hexdigest"));
        Assert.Equal(expected, Method(Assert.IsType<PyHashState>(Call(Hashes, "new", name, Bytes("abc"))), "hexdigest"));
        Assert.Throws<PyTypeError>(() => Method(state, "update", "bad"));
        Assert.Equal(expected, Method(state, "hexdigest"));
    }

    [Theory]
    [InlineData("sha256", "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8")]
    [InlineData("sha512", "b42af09057bac1e2d41708e48a902e09b5ff7f12ab428a4fe86653c73dd248fb82f948a549f7b791a5b41915ee4d1ec3935357e4e2317250d0372afa2ebeeb3a")]
    [InlineData("sha1", "de7c9b85b8b78aa6bc8a7a36f70a90701c9db4d9")]
    [InlineData("md5", "80070713463e7749b90c2dc24911e275")]
    public void KnownMacWithConstructorAndName(string name, string expected)
    {
        PyBytes message = Bytes("The quick brown fox jumps over the lazy dog");
        var state = Assert.IsType<PyHashState>(Call(Mac, "new", Bytes("key"), message, Hashes[name]));
        Assert.Equal(expected, Method(state, "hexdigest"));
        Assert.Equal(Convert.FromHexString(expected), Assert.IsType<PyBytes>(Call(Mac, "digest", Bytes("key"), message, name)).Bytes);
        var branch = Assert.IsType<PyHashState>(Method(state, "copy"));
        Method(branch, "update", Bytes("!"));
        Assert.NotEqual(Method(state, "hexdigest"), Method(branch, "hexdigest"));
        Assert.Equal(expected, Method(state, "hexdigest"));
    }

    [Fact]
    public void KeySnapshotKeywordMessageAndReleasedBuffer()
    {
        var key = new PyByteArray([107, 101, 121]);
        var state = Assert.IsType<PyHashState>(Call(Mac, "new", key, PyNone.Instance, "sha256"));
        key.Bytes[0] = 0;
        Assert.Same(PyNone.Instance, PyOps.CallValue(state.GetAttr("update"), [], ["msg"], [Bytes("abc")]));
        Assert.Equal("9c196e32dc0175f86f4b1cb89289d6619de6bee699e4c378e68309ed97a1a6ab", Method(state, "hexdigest"));
        var view = (PyMemoryView)PyMemoryView.Create([Bytes("def")], [], []);
        view.Release();
        Assert.Throws<PyValueError>(() => Method(state, "update", view));
        Assert.Throws<PyTypeError>(() => Call(Mac, "new", view, Bytes("abc"), "sha256"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Assert.IsType<PyHashState>(Call(Hashes, "sha256")).GetAttr("update"), [], ["data"], [Bytes("a")]));
    }

    [Fact]
    public void ExplicitUnsupportedAlgorithmsAndParameters()
    {
        Assert.Throws<PyValueError>(() => Call(Hashes, "new", "sha3_256"));
        Assert.Throws<PyValueError>(() => Call(Mac, "digest", Bytes("a"), Bytes("b"), "shake_128"));
        Assert.Throws<PyTypeError>(() => Call(Mac, "new", Bytes("a")));
        Assert.Throws<PyTypeError>(() => Call(Hashes, "new", 1L));
        Assert.Throws<PyNotImplementedError>(() => Call(Mac, "new", Bytes("a"), Bytes("b"), PyNativeFunctions.Create("custom", [], [], 0, _ => PyNone.Instance)));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Hashes["sha256"], [Bytes("a"), true], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Hashes["sha256"], [], ["unknown"], [true]));
        Assert.Equal("sha256", Assert.IsType<PyHashState>(Call(Hashes, "new", "SHA-256")).GetAttr("name"));
    }

    [Fact]
    public void CompareDigestUsesOnlyAsciiOrRealBuffers()
    {
        Assert.Equal(true, Call(Mac, "compare_digest", Bytes("abc"), new PyByteArray([97, 98, 99])));
        Assert.Equal(true, Call(Mac, "compare_digest", "abc", "abc"));
        Assert.Equal(false, Call(Mac, "compare_digest", Bytes("a"), Bytes("ab")));
        Assert.Equal(false, Call(Mac, "compare_digest", "a", "b"));
        Assert.Equal(true, Call(Mac, "compare_digest", Bytes(""), Bytes("")));
        Assert.Throws<PyTypeError>(() => Call(Mac, "compare_digest", "é", "é"));
        Assert.Throws<PyTypeError>(() => Call(Mac, "compare_digest", Bytes("a"), "a"));
        Assert.Throws<PyTypeError>(() => Call(Mac, "compare_digest", new PyList([1L]), new PyList([1L])));
    }
}
