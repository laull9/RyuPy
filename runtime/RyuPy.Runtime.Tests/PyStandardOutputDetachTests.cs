using System;
using System.IO;
using System.Text;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyStandardOutputDetachTests
{
    private static object? Call(object target, string name, params object?[] args)
        => PyOps.CallValue(PyObject.GetAttr(target, name), args, [], []);
    [Fact]
    public void DetachFlushesThenReturnsSameBinaryAndInvalidatesWrapper()
    {
        var bytes = new MemoryStream();
        var writer = new StreamWriter(bytes, new UTF8Encoding(false), 1024, true);
        var binary = new PyStandardBinaryOutput(bytes, "<stdout>", () => false);
        var text = new PyStandardStream("<stdout>", writer, binary);
        Assert.Same(binary, text.GetAttr("buffer"));
        text.Write("雪"); Assert.Empty(bytes.ToArray());
        Assert.Same(binary, Call(text, "detach"));
        Assert.Equal(Encoding.UTF8.GetBytes("雪"), bytes.ToArray());
        Assert.Same(PyNone.Instance, text.GetAttr("buffer"));
        Assert.Equal("utf-8", text.GetAttr("encoding"));
        foreach (var name in new[] { "closed", "name" })
            Assert.Equal("underlying buffer has been detached", Assert.Throws<PyValueError>(() => text.GetAttr(name)).Message);
        foreach (var name in new[] { "flush", "close", "detach", "isatty", "readable", "writable" })
            Assert.Throws<PyValueError>(() => Call(text, name));
        Assert.Equal(4L, Call(binary, "write", new PyBytes([0, 255, 128, 10])));
        Assert.Equal(new byte[] { 233, 155, 170, 0, 255, 128, 10 }, bytes.ToArray());
        Assert.Throws<PyTypeError>(() => Call(binary, "write", "text"));
        Call(binary, "close"); Call(binary, "close");
        Assert.Equal(true, binary.GetAttr("closed"));
        Assert.Throws<PyValueError>(() => Call(binary, "flush"));
        Assert.True(bytes.CanWrite); // borrowed OS handle ownership is not transferred
    }
    [Fact]
    public void FailedDetachKeepsAttachedAndHostWriterWithoutRawRejects()
    {
        var bytes = new MemoryStream();
        var binary = new PyStandardBinaryOutput(bytes, "<stdout>", () => false);
        var text = new PyStandardStream("<stdout>", new BadWriter(), binary);
        Assert.Throws<PyOSError>(() => Call(text, "detach"));
        Assert.Same(binary, text.GetAttr("buffer"));
        Assert.Equal(false, text.GetAttr("closed"));
        Assert.Throws<PyNotImplementedError>(() => Call(new PyStandardStream("host", null, new StringWriter()), "detach"));
    }
    [Fact]
    public void ClosingBufferClosesAttachedTextAndTextCloseClosesBuffer()
    {
        var bytes = new MemoryStream();
        var binary = new PyStandardBinaryOutput(bytes, "<stdout>", () => false);
        var text = new PyStandardStream("<stdout>", new StringWriter(), binary);
        Call(binary, "close"); Assert.Equal(true, text.GetAttr("closed"));
        Assert.Throws<PyValueError>(() => text.Write("x"));
        var second = new PyStandardBinaryOutput(bytes, "<stdout>", () => false);
        var attached = new PyStandardStream("<stdout>", new StringWriter(), second);
        Call(attached, "close"); Assert.Equal(true, second.GetAttr("closed"));
    }
    private sealed class BadWriter : StringWriter
    { public override void Flush() => throw new IOException("flush failed"); }
}
