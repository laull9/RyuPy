using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>二进制前后缀的边界、候选短路、用户索引顺序和显式未支持的resize重入。</summary>
public class PyBinaryAffixTests
{
    private static PyBytes Bytes(string value) => new(System.Text.Encoding.ASCII.GetBytes(value));
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("AffixIndex");
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => body()));
        return new PyInstance(cls);
    }

    [Fact]
    public void ResizeThenRestoreCannotBypassGuardButNoOpsDoNotTripIt()
    {
        var source = new PyByteArray([97, 98]);
        Assert.Throws<PyNotImplementedError>(() => PyBinaryAffix.Match(source, "startswith", [Bytes("a"),
            Index(() => { source.Append(99L); return 0L; }),
            Index(() => { source.DelItem(2L); return 2L; })]));
        Assert.Equal(2, source.Count);
        foreach (int op in new[] { PyOps.Add, PyOps.Mul })
        {
            Assert.Throws<PyNotImplementedError>(() => PyBinaryAffix.Match(source, "startswith", [Bytes("a"), Index(() =>
            {
                PyOps.InPlaceBinary(op, source, op == PyOps.Add ? Bytes("ab") : 2L);
                source.DelItem(new PySlice(2L, PyNone.Instance, PyNone.Instance));
                return 0L;
            })]));
        }
        Assert.Throws<PyNotImplementedError>(() => PyBinaryAffix.Match(source, "startswith", [Bytes("a"),
            Index(() => { source.DelItem(1L); source.Append(98L); return 0L; })]));
        Assert.True(PyBinaryAffix.Match(source, "startswith", [Bytes("a"), Index(() =>
        {
            source.Extend(new PyBytes([]));
            source.DelItem(new PySlice(0L, 0L, PyNone.Instance));
            PyOps.InPlaceBinary(PyOps.Mul, source, 1L);
            return 0L;
        })]));
    }

    [Fact]
    public void BoundsAndTupleCandidatesKeepShortCircuitOrder()
    {
        var source = Bytes("abcd");
        Assert.True(PyBinaryAffix.Match(source, "startswith", [new PyTuple([Bytes("ab"), 42L])]));
        Assert.Throws<PyTypeError>(() => PyBinaryAffix.Match(source, "startswith", [new PyTuple([Bytes("xx"), 42L])]));
        Assert.False(PyBinaryAffix.Match(source, "endswith", [new PyTuple([])]));
        Assert.True(PyBinaryAffix.Match(source, "startswith", [Bytes(""), 4L]));
        Assert.False(PyBinaryAffix.Match(source, "startswith", [Bytes(""), 5L]));
        Assert.True(PyBinaryAffix.Match(source, "endswith", [Bytes("cd"), -(BigInteger.One << 100), BigInteger.One << 100]));
        string events = "";
        Assert.Throws<PyTypeError>(() => PyBinaryAffix.Match(source, "startswith", [42L,
            Index(() => { events += "start"; return 0L; }), Index(() => { events += "end"; return 4L; })]));
        Assert.Equal("startend", events);
    }

    [Fact]
    public void RawCharacterViewWorksButReleasedUnusedCandidateIsNotRead()
    {
        var view = (PyMemoryView)PyMemoryView.Create([Bytes("cd")], [], []);
        var chars = (PyMemoryView)PyOps.CallValue(view.GetAttr("cast"), ["c"], [], [])!;
        Assert.True(PyBinaryAffix.Match(Bytes("abcd"), "endswith", [chars]));
        chars.Release(); view.Release();
        Assert.True(PyBinaryAffix.Match(Bytes("abcd"), "startswith", [new PyTuple([Bytes("ab"), chars])]));
        Assert.Throws<PyValueError>(() => PyBinaryAffix.Match(Bytes("abcd"), "startswith", [chars]));
    }

    [Fact]
    public void SameLengthWriteIsVisibleButResizeHasExplicitBoundaryAfterBothIndexes()
    {
        var source = new PyByteArray([97, 98]);
        Assert.True(PyBinaryAffix.Match(source, "startswith", [Bytes("x"), Index(() => { source.SetItem(0L, 120L); return 0L; })]));
        bool endCalled = false;
        Assert.Throws<PyNotImplementedError>(() => PyBinaryAffix.Match(source, "startswith", [Bytes("x"),
            Index(() => { source.Append(99L); return 0L; }), Index(() => { endCalled = true; return 3L; })]));
        Assert.True(endCalled);
        Assert.Equal(3, source.Count);
        var expected = new PyValueError("original index failure");
        Assert.Same(expected, Assert.Throws<PyValueError>(() => PyBinaryAffix.Match(source, "startswith", [Bytes("x"),
            Index(() => { source.Append(100L); return 0L; }), Index(() => throw expected)])));
    }
}
