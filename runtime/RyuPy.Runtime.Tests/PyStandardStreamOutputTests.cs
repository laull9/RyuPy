// 标准文本输出仅在原生包装器翻译换行；纯平台策略与真实 write/报告入口分别验证。
using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyStandardStreamOutputTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("plain😀", "plain😀")]
    [InlineData("\n", "\r\n")]
    [InlineData("\r", "\r")]
    [InlineData("\r\n", "\r\r\n")]
    [InlineData("a\nb\r\nc\rd\n", "a\r\nb\r\r\nc\rd\r\n")]
    [InlineData("😀\n", "😀\r\n")]
    public void WindowsTranslatesEveryLfAndUnixPreservesOriginal(string input, string windows)
    {
        Assert.Equal(windows, PyStandardStream.TranslateOutput(input, true));
        Assert.Same(input, PyStandardStream.TranslateOutput(input, false));
        if (!input.Contains('\n')) Assert.Same(input, PyStandardStream.TranslateOutput(input, true));
    }

    [Fact]
    public void WriteCountsOriginalCodePointsAndCallsWriterOnce()
    {
        var writer = new RecordingWriter { NewLine = "WRITER-SETTING" };
        var stream = new PyStandardStream("<stdout>", null, writer);
        Assert.Equal(4L, Call(stream, "write", "😀\r\nX"));
        Assert.Equal(1, writer.Writes);
        Assert.Equal(PyStandardStream.TranslateOutput("😀\r\nX", OperatingSystem.IsWindows()), writer.ToString());
        writer.GetStringBuilder().Clear();
        stream.Write("\r");
        stream.Write("\n");
        Assert.Equal(3, writer.Writes);
        Assert.Equal(OperatingSystem.IsWindows() ? "\r\r\n" : "\r\n", writer.ToString());
        Call(stream, "close");
        Assert.Throws<PyValueError>(() => stream.Write("\n"));
        Assert.Equal(3, writer.Writes);
        var reader = new PyStandardStream("<stdin>", new StringReader(""), null);
        Assert.Throws<PyUnsupportedOperation>(() => reader.Write("\n"));
    }

    [Fact]
    public void ReportUsesExactlyOneTranslationAndReplacementStreamReceivesRawText()
    {
        PySystem.Initialize("standard-output-test.py", []);
        var sys = PyModules.Import("sys");
        object? previous = PySystem.Stderr, previousOutput = PySystem.Stdout;
        var error = new PyValueError("line\nnext");
        string raw = PyTraceback.Format(error);
        Assert.Contains("\n", raw);
        Assert.DoesNotContain("\r", raw);
        try
        {
            var writer = new RecordingWriter();
            sys.SetAttr("stderr", new PyStandardStream("<stderr>", null, writer));
            PyTraceback.Report(error);
            Assert.Equal(1, writer.Writes);
            Assert.Equal(PyStandardStream.TranslateOutput(raw, OperatingSystem.IsWindows()), writer.ToString());
            string? received = null;
            var cls = new PyClass("Output");
            cls.SetAttr("write", PyNativeFunctions.Create("write", ["self", "text"], [null, null], 2,
                args => { received = Assert.IsType<string>(args[1]); return PyNone.Instance; }));
            cls.SetAttr("flush", PyNativeFunctions.Create("flush", ["self"], [null], 1, _ => PyNone.Instance));
            var replacement = new PyInstance(cls);
            sys.SetAttr("stderr", replacement);
            sys.SetAttr("stdout", replacement);
            PyTraceback.Report(error);
            Assert.Equal(raw, received);
            PySystem.Write(PySystem.Stderr, raw);
            Assert.Same(raw, received);
            received = null;
            Builtins.WriteStr(raw);
            Assert.Same(raw, received);
        }
        finally
        {
            sys.SetAttr("stderr", previous);
            sys.SetAttr("stdout", previousOutput);
        }
    }

    private static object? Call(object stream, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(stream, name), args, [], []);
    private sealed class RecordingWriter : StringWriter
    {
        public int Writes { get; private set; }
        public override void Write(string? value) { Writes++; base.Write(value); }
    }
}
