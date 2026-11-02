using System;
using System.Collections.Generic;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

// input 测试临时替换 sys 标准流；复用现有非并行集合并在 finally 还原。
[Collection("生成器帧栈")]
public class PyStandardStreamNewlineTests
{
    [Fact]
    public void CarriageReturnCompletesLineWithoutReadingOrPeekingAhead()
    {
        var reader = new StagedReader();
        var stream = Wrap(reader);
        reader.Add("abc\r");
        Assert.Equal("abc\n", Call(stream, "readline"));
        Assert.Equal(4, reader.ReadCount);
        reader.Add("\nnext\r");
        Assert.Equal("next\n", Call(stream, "readline"));
        Assert.Equal(10, reader.ReadCount);
        reader.Add("X\n");
        Assert.Equal("X\n", Call(stream, "readline"));
        reader.Complete = true;
        Assert.Equal("", Call(stream, "readline"));
    }

    [Fact]
    public void ZeroSizePreservesPendingLfAndNonLfSuccessor()
    {
        var reader = new StagedReader();
        var stream = Wrap(reader);
        reader.Add("\r");
        Assert.Equal("\n", Call(stream, "read", 1L));
        Assert.Equal("", Call(stream, "read", 0L));
        Assert.Equal("", Call(stream, "readline", 0L));
        Assert.Equal(1, reader.ReadCount);
        reader.Add("\nA\r");
        Assert.Equal("A", Call(stream, "read", 1L));
        Assert.Equal("\n", Call(stream, "readline", 1L));
        reader.Add("B\n");
        Assert.Equal("B\n", Call(stream, "read", 2L));
    }

    [Theory]
    [InlineData("\r\r\n", "\n\n")]
    [InlineData("\r\n\n", "\n\n")]
    [InlineData("a\rb\r\nc\n", "a\nb\nc\n")]
    [InlineData("", "")]
    [InlineData("\r", "\n")]
    [InlineData("\r\n", "\n")]
    public void MixedTerminatorsAndEofAreNormalizedWithoutPhantomLine(string text, string expected)
    {
        var reader = new StagedReader { Complete = true };
        reader.Add(text);
        var stream = Wrap(reader);
        Assert.Equal(expected, Call(stream, "read", -2L));
        Assert.Equal("", Call(stream, "readline"));
        Assert.Equal("", Call(stream, "read"));
    }

    [Fact]
    public void ReadExceptionDoesNotForgetPendingLf()
    {
        var reader = new StagedReader();
        var stream = Wrap(reader);
        reader.Add("\r");
        Assert.Equal("\n", Call(stream, "readline"));
        var failure = new InvalidOperationException("read failure");
        reader.NextFailure = failure;
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Call(stream, "readline")));
        reader.Add("\nkept\n");
        Assert.Equal("kept\n", Call(stream, "readline"));
    }

    [Fact]
    public void IteratorSharesDeferredLfAndCodePointCountingRemainsUnchanged()
    {
        var reader = new StagedReader();
        var stream = Wrap(reader);
        reader.Add("one\r");
        Assert.True(stream.MoveNext());
        Assert.Equal("one\n", stream.Current());
        reader.Add("\ntwo\r");
        Assert.True(stream.MoveNext());
        Assert.Equal("two\n", stream.Current());
        reader.Complete = true;
        Assert.False(stream.MoveNext());
        var unicode = Wrap(new StringReader("😀\r\nend\r"));
        Assert.Equal("😀", Call(unicode, "read", 1L));
        Assert.Equal("\n", Call(unicode, "readline", 1L));
        Assert.Equal("end\n", Call(unicode, "read"));
    }

    [Fact]
    public void InputFlushesPromptBeforeReadAndDistinguishesBlankLineFromEof()
    {
        PySystem.Initialize("newline-test.py", []);
        var sys = PyModules.Import("sys");
        object? oldInput = PySystem.Stdin, oldOutput = PySystem.Stdout;
        var writer = new FlushWriter();
        var reader = new StagedReader { BeforeRead = () => Assert.True(writer.Flushed) };
        try
        {
            sys.SetAttr("stdin", Wrap(reader));
            sys.SetAttr("stdout", new PyStandardStream("<stdout>", null, writer));
            reader.Add("first\r");
            Assert.Equal("first", PyOps.CallBuiltin("input", ["prompt>"], [], []));
            Assert.Equal("prompt>", writer.ToString());
            reader.Add("\n\r");
            Assert.Equal("", PyOps.CallBuiltin("input", [], [], []));
            reader.Complete = true;
            Assert.Throws<PyEOFError>(() => PyOps.CallBuiltin("input", [], [], []));
        }
        finally
        {
            sys.SetAttr("stdin", oldInput);
            sys.SetAttr("stdout", oldOutput);
        }
    }

    private static PyStandardStream Wrap(TextReader reader) => new("<stdin>", reader, null);
    private static object? Call(object stream, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(stream, name), args, [], []);

    // 无数据不是 EOF：过早 Read/任何 Peek 都立即失败，确定性地锁住不预读合同。
    private sealed class StagedReader : TextReader
    {
        private readonly Queue<char> _characters = new();
        public bool Complete { get; set; }
        public int ReadCount { get; private set; }
        public Exception? NextFailure { get; set; }
        public Action? BeforeRead { get; init; }
        public void Add(string text) { foreach (char character in text) _characters.Enqueue(character); }
        public override int Peek() => throw new InvalidOperationException("unexpected Peek");
        public override int Read()
        {
            BeforeRead?.Invoke();
            ReadCount++;
            if (NextFailure is Exception failure) { NextFailure = null; throw failure; }
            if (_characters.TryDequeue(out char character)) return character;
            if (Complete) return -1;
            throw new InvalidOperationException("read past available line");
        }
    }
    private sealed class FlushWriter : StringWriter
    {
        public bool Flushed { get; private set; }
        public override void Flush() { Flushed = true; base.Flush(); }
    }
}
