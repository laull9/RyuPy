// 声明 ABI 帧单测：精确数值、严格编码、快照隔离、固定内存释放与异常准备。
using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNativeAbiTests
{
    [Theory]
    [InlineData("i8", 8, true)]
    [InlineData("u8", 8, false)]
    [InlineData("i16", 16, true)]
    [InlineData("u16", 16, false)]
    [InlineData("i32", 32, true)]
    [InlineData("u32", 32, false)]
    [InlineData("i64", 64, true)]
    [InlineData("u64", 64, false)]
    [InlineData("isize", 64, true)]
    [InlineData("usize", 64, false)]
    public void ExactIntegerRangeRejectsBothEdgesAndBool(string kind, int width, bool signed)
    {
        if (kind is "isize" or "usize") width = IntPtr.Size * 8;
        BigInteger limit = BigInteger.One << (signed ? width - 1 : width);
        BigInteger minimum = signed ? -limit : BigInteger.Zero;
        foreach (BigInteger value in new[] { minimum, BigInteger.Zero, limit - 1 })
        {
            using var frame = PyNativeCall.Prepare([value], kind, "value", "echo");
            Assert.Equal(value, signed ? new BigInteger(frame.Signed(0)) : new BigInteger(frame.Unsigned(0)));
        }
        foreach (BigInteger value in new[] { minimum - 1, limit, BigInteger.One << 200 })
            Assert.Throws<PyOverflowError>(() => PyNativeCall.Prepare([value], kind, "value", "echo"));
        foreach (object? value in new object?[] { true, 1.0, "1", PyNone.Instance, null, (uint)1 })
            Assert.Throws<PyTypeError>(() => PyNativeCall.Prepare([value], kind, "value", "echo"));
    }

    [Fact]
    public void FloatsBooleansAndResultsKeepExactProtocol()
    {
        foreach (double value in new[] { -0.0, double.Epsilon, double.NaN, double.PositiveInfinity, 0.1 })
        {
            using var frame = PyNativeCall.Prepare([value], "f32", "value", "echo");
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(frame.Floating(0)));
        }
        Assert.Throws<PyOverflowError>(() => PyNativeCall.Prepare([double.MaxValue], "f32", "value", "echo"));
        Assert.Throws<PyTypeError>(() => PyNativeCall.Prepare([1L], "f64", "value", "echo"));
        using var boolean = PyNativeCall.Prepare([true], "bool8", "value", "echo");
        Assert.Equal((byte)1, boolean.Boolean(0));
        Assert.Throws<PyTypeError>(() => PyNativeCall.Prepare([1], "bool8", "value", "echo"));
        Assert.Equal(new BigInteger(ulong.MaxValue), PyNativeBindings.BoxUnsigned(ulong.MaxValue));
        Assert.Equal(long.MaxValue, PyNativeBindings.BoxUnsigned((ulong)long.MaxValue));
        Assert.False(Assert.IsType<bool>(PyNativeBindings.BoxBoolean(0)));
        Assert.True(Assert.IsType<bool>(PyNativeBindings.BoxBoolean(1)));
        Assert.Throws<PyValueError>(() => PyNativeBindings.BoxBoolean(2));
        Assert.Throws<PyValueError>(() => PyNativeBindings.BoxBoolean(255));
    }

    [Fact]
    public void StrictUtf8HasByteLengthEmbeddedNulAndNoReplacementFallback()
    {
        using var frame = PyNativeCall.Prepare(["中\0🌍"], "utf8", "text", "read");
        byte[] expected = [0xe4, 0xb8, 0xad, 0, 0xf0, 0x9f, 0x8c, 0x8d];
        Assert.Equal((nuint)expected.Length, frame.Length(0));
        byte[] actual = new byte[expected.Length];
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Marshal.Copy(frame.Pointer(0), actual, 0, actual.Length);
        Assert.Equal(expected, actual);
        foreach (string text in new[] { "\ud800", "\udfff", "a\ud800b" })
            Assert.Throws<PyUnicodeEncodeError>(() => PyNativeCall.Prepare([text], "utf8", "text", "read"));
        Assert.Throws<PyTypeError>(() => PyNativeCall.Prepare([new PyBytes([1])], "utf8", "text", "read"));
    }

    [Fact]
    public void ByteSnapshotsNeverExposePythonStorageAndDisposeFreesAllPins()
    {
        var bytes = new PyBytes([0, 255, 128]);
        var frame = PyNativeCall.Prepare([bytes, ""], "bytes,utf8", "data,text", "read");
        nint pointer = frame.Pointer(0);
        bytes.Bytes[0] = 42;
        Assert.Equal(0, Marshal.ReadByte(pointer));
        Marshal.WriteByte(pointer, 7);
        Assert.Equal(42, bytes.Bytes[0]);
        Assert.NotEqual(nint.Zero, frame.Pointer(1));
        Assert.Equal((nuint)0, frame.Length(1));
        frame.Dispose(); frame.Dispose();
        Assert.Throws<ObjectDisposedException>(() => frame.Pointer(0));
        // 单测检查 CLR 句柄已释放；生产路径无反射、动态生成或测试计数器。
        var pins = Assert.IsType<GCHandle[]>(typeof(PyNativeCall).GetField("pins", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(frame));
        Assert.All(pins, pin => Assert.False(pin.IsAllocated));
    }

    [Fact]
    public void ValidationAndReturnErrorsCarryCallerTraceAndContext()
    {
        var outer = new PyValueError("outer");
        object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("abi.py", "caller");
        try
        {
            PyTraceback.SetLine(23);
            var conversion = Assert.Throws<PyTypeError>(() => PyNativeCall.Prepare(
                [new PyBytes([1]), true], "bytes,i32", "data,value", "call"));
            var result = Assert.Throws<PyValueError>(() => PyNativeBindings.BoxBoolean(255));
            foreach (PyBaseException error in new PyBaseException[] { conversion, result })
            {
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("abi.py", "caller", 23), error.Frames[0]);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }
}
