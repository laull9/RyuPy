// 内存流协议与编码/字节游标回归；磁盘测试仅写独立临时目录并保证清理。
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyIOTests
{
    private static object? Call(PyIOStream stream, string name, params object?[] args) =>
        PyOps.CallValue(stream.GetAttr(name), args, [], []);
    private static PyIOStream Text(string value = "", object? newline = null) =>
        (PyIOStream)PyIO.CreateStringIO([value], ["newline"], [newline])!;
    private static PyIOStream Binary(params byte[] bytes) =>
        (PyIOStream)PyIO.CreateBytesIO([new PyBytes(bytes)], [], [])!;

    [Fact]
    public void StringMemoryUsesCodePointsAndTranslatesNewlines()
    {
        var text = Text("猫\r\n🎉\rlast");
        Assert.Equal("猫\n", Call(text, "readline"));
        Assert.Equal(2L, Call(text, "tell"));
        Assert.Equal("🎉", Call(text, "read", 1L));
        Assert.Equal(3L, Call(text, "tell"));
        Assert.Equal(0L, Call(text, "seek", 0L));
        Assert.Equal(1L, Call(text, "write", "好"));
        Assert.Equal("好\n🎉\nlast", Call(text, "getvalue"));
        Call(text, "seek", 3L);
        Assert.Equal(3L, Call(text, "truncate"));
        Assert.Equal("好\n🎉", Call(text, "getvalue"));
        Call(text, "seek", 5L); Call(text, "write", "!");
        Assert.Equal("好\n🎉\0\0!", Call(text, "getvalue"));
    }

    [Theory]
    [InlineData("", "a\r\n")]
    [InlineData("\n", "a\r\n")]
    [InlineData("\r", "a\r")]
    [InlineData("\r\n", "a\r\r\n")]
    public void StringMemoryKeepsExplicitNewlineRules(string newline, string first)
    {
        var text = Text("a\r\nb\nc", newline);
        Assert.Equal(first, Call(text, "readline"));
    }

    [Fact]
    public void BinaryMemorySupportsSeekWriteTruncateAndIteration()
    {
        var stream = Binary(97, 98, 10, 99);
        Assert.Equal(new byte[] { 97, 98, 10 }, Assert.IsType<PyBytes>(Call(stream, "readline")).Bytes);
        Assert.Equal(3L, Call(stream, "tell"));
        Call(stream, "seek", 0L, 2L);
        Assert.Equal(1L, Call(stream, "write", new PyByteArray([33])));
        Call(stream, "seek", 0L);
        Assert.True(stream.MoveNext());
        Assert.Equal(new byte[] { 97, 98, 10 }, Assert.IsType<PyBytes>(stream.Current()).Bytes);
        Assert.True(stream.MoveNext()); Assert.False(stream.MoveNext());
        Assert.Equal(100L, Call(stream, "truncate", 100L));
        Assert.Equal(5, Assert.IsType<PyBytes>(Call(stream, "getvalue")).Count);
        Call(stream, "truncate", 2L);
        Assert.Equal(5L, Call(stream, "tell"));
        Call(stream, "seek", -100L, 1L); Assert.Equal(0L, Call(stream, "tell"));
    }

    [Fact]
    public void ContextCloseAndWriteErrorsStayPythonErrors()
    {
        var stream = Binary();
        Assert.Same(stream, Call(stream, "__enter__"));
        Assert.Throws<PyTypeError>(() => Call(stream, "write", "abc"));
        Assert.False((bool)Call(stream, "__exit__", null, null, null)!);
        Assert.True(stream.Closed);
        Call(stream, "close");
        Assert.Throws<PyValueError>(() => Call(stream, "read"));
        Assert.Throws<PyValueError>(() => Call(stream, "flush"));
        Assert.Throws<PyValueError>(() => Call(stream, "getvalue"));
        Assert.Throws<PyValueError>(() => stream.MoveNext());
        Assert.Throws<PyTypeError>(() => Call(Text(), "write", new PyBytes([])));
    }

    [Fact]
    public void ReadLinesAndWriteLinesPreserveStreamingPrefix()
    {
        var stream = Text();
        var error = Assert.Throws<PyTypeError>(() => Call(stream, "writelines", new PyList(["a\n", 1L])));
        Assert.Equal("expected str, not int", error.PyMessage);
        Assert.Equal("a\n", Call(stream, "getvalue"));
        Call(stream, "write", "b\nc"); Call(stream, "seek", 0L);
        var lines = Assert.IsType<PyList>(Call(stream, "readlines", 1L)).Items;
        Assert.Equal(new object?[] { "a\n" }, lines);
        Assert.Equal("b\nc", Call(stream, "read"));
    }

    [Fact]
    public void FileEncodingPositionsAppendAndCleanup()
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "data.txt");
            var stream = Open(path, "w+", "utf-8", "\n");
            try
            {
                Assert.Equal(4L, Call(stream, "write", "猫🎉\nA"));
                Assert.Equal(9L, Call(stream, "tell"));
                Call(stream, "seek", 0L);
                Assert.Equal("猫", Call(stream, "read", 1L));
                object? cookie = Call(stream, "tell"); Assert.Equal(3L, cookie);
                Assert.Equal("🎉\n", Call(stream, "readline"));
                Call(stream, "seek", cookie); Assert.Equal("🎉", Call(stream, "read", 1L));
                Assert.Throws<PyNotImplementedError>(() => Call(stream, "seek", 1L));
                Call(stream, "flush"); Assert.Equal("猫🎉\nA", File.ReadAllText(path));
            }
            finally { stream.Close(); }
            var append = Open(path, "a+", "utf-8", "\n");
            try { Call(append, "seek", 0L); Call(append, "write", "!"); }
            finally { append.Close(); }
            Assert.Equal("猫🎉\nA!", File.ReadAllText(path));
        });
    }

    [Fact]
    public void StrictEncodingAndFilesystemFailuresDoNotLeakClr()
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "data");
            var ascii = Open(path, "w", "ascii");
            try { Assert.Throws<PyUnicodeEncodeError>(() => Call(ascii, "write", "猫")); }
            finally { ascii.Close(); }
            File.WriteAllBytes(path, [255]);
            var read = Open(path, "r", "ascii");
            try { Assert.Throws<PyUnicodeDecodeError>(() => Call(read, "read")); }
            finally { read.Close(); }
            var latin = Open(path, "r", "latin-1");
            try { Assert.Equal("ÿ", Call(latin, "read")); }
            finally { latin.Close(); }
            Assert.Throws<PyFileNotFoundError>(() => Open(path + "missing", "r"));
            Assert.Throws<PyFileExistsError>(() => Open(path, "x"));
            Assert.Throws<PyIsADirectoryError>(() => Open(directory, "r"));
            Assert.Throws<PyNotADirectoryError>(() => Open(Path.Combine(path, "child"), "r"));
            Assert.Throws<PyValueError>(() => Open(path, "rr"));
            Assert.Throws<PyLookupError>(() => Open(path, "w", "utf-16"));
            Assert.Equal(new byte[] { 255 }, File.ReadAllBytes(path));
        });
    }

    [Fact]
    public void UniversalNewlinesAndBinaryModeValidation()
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "data"); File.WriteAllText(path, "a\r\nb\rc\n");
            var universal = Open(path, "r");
            try { Assert.Equal("a\nb\nc\n", Call(universal, "read")); }
            finally { universal.Close(); }
            var preserved = Open(path, "r", "utf-8", "");
            try { Assert.Equal("a\r\n", Call(preserved, "readline")); Assert.Equal("b\r", Call(preserved, "readline")); }
            finally { preserved.Close(); }
            Assert.Throws<PyValueError>(() => Open(path, "rb", "utf-8"));
            Assert.Throws<PyNotImplementedError>(() => PyIO.Open([path], ["errors"], ["ignore"]));
        });
    }

    [Fact]
    public void UnbufferedBinaryAndLineBufferedTextExposeWritesBeforeExplicitFlush()
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "visible");
            var raw = (PyIOStream)PyIO.Open([path, "wb"], ["buffering"], [0L])!;
            try
            {
                Assert.Equal("FileIO", raw.TypeName);
                Call(raw, "write", new PyBytes([65]));
                Assert.Equal(new byte[] { 65 }, File.ReadAllBytes(path));
            }
            finally { raw.Close(); }
            var text = (PyIOStream)PyIO.Open([path, "w"], ["buffering", "newline"], [1L, "\n"])!;
            try
            {
                Assert.Equal(true, text.GetAttr("line_buffering"));
                Call(text, "write", "line\n");
                Assert.Equal("line\n", File.ReadAllText(path));
            }
            finally { text.Close(); }
        });
    }

    [Fact]
    public void NoneReadSizesAndHintsMeanUnlimited()
    {
        foreach (PyIOStream stream in new[] { Text("a\nb"), Binary(97, 10, 98) })
        {
            var lines = Assert.IsType<PyList>(Call(stream, "readlines", PyNone.Instance));
            Assert.Equal(2, lines.Count);
            Call(stream, "seek", 0L);
            object? all = Call(stream, "read", (object?)null);
            Assert.Equal(stream.TypeName == "StringIO" ? (object)"a\nb" : 3,
                all is PyBytes bytes ? bytes.Count : all);
            Call(stream, "seek", 0L);
            object? line = Call(stream, "readline", PyNone.Instance);
            Assert.Equal(stream.TypeName == "StringIO" ? (object)"a\n" : 2,
                line is PyBytes lineBytes ? lineBytes.Count : line);
        }
    }

    [Fact]
    public void FileCapabilitiesAndInvalidOpenOptionsDoNotTruncateExistingContent()
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "mode"); File.WriteAllText(path, "keep");
            foreach (string mode in new[] { "r", "rb" })
            {
                var stream = Open(path, mode);
                try
                {
                    Assert.Equal(true, Call(stream, "readable"));
                    Assert.Equal(false, Call(stream, "writable"));
                    Assert.Throws<PyUnsupportedOperation>(() => Call(stream, "write", mode == "r" ? "x" : new PyBytes([120])));
                    Assert.Throws<PyUnsupportedOperation>(() => Call(stream, "truncate", 0L));
                }
                finally { stream.Close(); }
            }
            Assert.Throws<PyValueError>(() => PyIO.Open([path, "w"], ["newline"], ["bad"]));
            Assert.Throws<PyNotImplementedError>(() => PyIO.Open([path, "w"], ["buffering"], [512L]));
            Assert.Throws<PyNotImplementedError>(() => PyIO.Open([path, "w"], ["opener"], [1L]));
            Assert.Equal("keep", File.ReadAllText(path));
        });
    }

    [Fact]
    public void ArgumentTypeErrorsKeepTheReadWriteClosedOrdering()
    {
        foreach (var stream in new[] { Text(), Binary() })
        {
            stream.Close();
            Assert.Throws<PyTypeError>(() => Call(stream, "read", "bad"));
            Assert.Throws<PyTypeError>(() => Call(stream, "readline", "bad"));
            if (stream.TypeName == "StringIO")
            {
                Assert.Throws<PyTypeError>(() => Call(stream, "write", 1L));
                Assert.Throws<PyTypeError>(() => Call(stream, "readlines", "bad"));
            }
            else
            {
                Assert.Throws<PyValueError>(() => Call(stream, "write", 1L));
                Assert.Throws<PyValueError>(() => Call(stream, "readlines", "bad"));
            }
        }
        WithDirectory(directory =>
        {
            foreach (string mode in new[] { "w", "wb" })
            {
                var stream = Open(Path.Combine(directory, "closed"), mode); stream.Close();
                Assert.Throws<PyTypeError>(() => Call(stream, "read", "bad"));
                Assert.Throws<PyTypeError>(() => Call(stream, "readline", "bad"));
                Assert.Throws<PyTypeError>(() => Call(stream, "write", 1L));
                Assert.Throws<PyTypeError>(() => Call(stream, "readlines", "bad"));
            }
        });
    }

    private static PyIOStream Open(string path, string mode, string? encoding = null, string? newline = null) =>
        (PyIOStream)PyIO.Open([path, mode], ["encoding", "newline"], [encoding, newline])!;
    private static void WithDirectory(Action<string> body)
    {
        string directory = Path.Combine(Path.GetTempPath(), "ryupy-io-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { body(directory); }
        finally { Directory.Delete(directory, true); }
    }
}
