// ZIP尾识别的公开入口与用户流协议；共享注册表和帧状态按既有collection串行。
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyZipfileTests
{
    private sealed class Duck(Func<string, object?> get) : PyNativeObject
    {
        public override string TypeName => "zip_stream";
        public override object? GetAttr(string name) => get(name);
    }
    private static PyFunction Function(string name, Func<object?[], object?> call) =>
        new(name, PySignature.Create(name, [], [], []), (_, a, _, _) => call(a), [], false);
    private static object Stream(Func<object?[], object?> read, Func<object?>? tell = null,
        Action<object?[]>? seek = null, Action<string>? lookup = null) => new Duck(name =>
    {
        lookup?.Invoke(name);
        return name switch
        {
            "read" => Function(name, read),
            "tell" => Function(name, _ => tell is null ? 22L : tell()),
            "seek" => Function(name, a => { seek?.Invoke(a); return null; }),
            _ => throw new PyAttributeError(name),
        };
    });
    private static object? Function() => PyZipfile.CreateAttributes()["is_zipfile"];
    private static bool Check(object? value) => Assert.IsType<bool>(PyOps.CallValue(Function(), [value], [], []));
    private static PyIOBinary Memory(byte[] data) => Assert.IsType<PyIOBinary>(PyIO.CreateBytesIO([new PyBytes(data)], [], []));
    private static byte[] End(int comment = 0)
    {
        var data = new byte[22]; U32(data, 0, 0x06054b50); BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(20), (ushort)comment); return data;
    }
    private static byte[] Join(params byte[][] pieces)
    { using var stream = new MemoryStream(); foreach (var part in pieces) stream.Write(part); return stream.ToArray(); }
    private static void U32(byte[] data, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
    private static void U64(byte[] data, int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset), value);
    private static byte[] Zip64(int prefix = 0)
    {
        var record = new byte[56]; U32(record, 0, 0x06064b50); U64(record, 4, 44);
        var locator = new byte[20]; U32(locator, 0, 0x07064b50); U32(locator, 16, 1);
        return Join(new byte[prefix], record, locator, End());
    }

    [Fact]
    public void ModuleAndSignatureExposeRealFiniteApi()
    {
        PySystem.Initialize("zip-tests.py", []); PyNativeModules.Register();
        var module = PyModules.Import("zipfile");
        Assert.Same(PyType.Get("BadZipFile"), module.GetAttr("BadZipFile"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("ZipFile"));
        Assert.Throws<PyNotImplementedError>(() => module.SetAttr("BadZipFile", PyType.Get("Exception")));
        Assert.True(Assert.IsType<bool>(PyOps.CallValue(Function(), [], ["filename"], [Memory(End())])));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Function(), [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Function(), [Memory(End()), 0L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Function(), [Memory(End())], ["filename"], [0L]));
    }
    [Fact]
    public void PlainAndEmptyFilesAreFalseButEndRecordAloneIsTrue()
    {
        Assert.False(Check(Memory([]))); Assert.False(Check(Memory([1, 2, 3])));
        Assert.True(Check(Memory(End())));
        var unusual = End(); U32(unusual, 4, uint.MaxValue); U32(unusual, 12, uint.MaxValue);
        Assert.True(Check(Memory(unusual)));
    }
    [Fact]
    public void CommentMayBeShortAndLastSignatureWins()
    {
        Assert.True(Check(Memory(Join(End(99), new byte[] { 65 }))));
        Assert.False(Check(Memory(Join(End(5), new byte[] { 80, 75, 5, 6 }))));
        Assert.True(Check(Memory(Join(End(1), new byte[] { 65, 66, 67 }))));
    }
    [Fact]
    public void SearchUsesTheActual65557ByteTailWindow()
    {
        Assert.True(Check(Memory(Join(End(65535), new byte[65535]))));
        Assert.False(Check(Memory(Join(End(65535), new byte[65536]))));
    }
    [Fact]
    public void ValidZip64AndPrependedFallbackAreAccepted()
    {
        Assert.True(Check(Memory(Zip64()))); Assert.True(Check(Memory(Zip64(9))));
    }
    [Fact]
    public void Zip64LocatorRejectsDisksAndUnsignedOffsetsWithoutOverflow()
    {
        var disks = Zip64(); U32(disks, 60, 1); Assert.False(Check(Memory(disks)));
        var count = Zip64(); U32(count, 72, 2); Assert.False(Check(Memory(count)));
        var huge = Zip64(); U64(huge, 64, ulong.MaxValue); Assert.False(Check(Memory(huge)));
    }
    [Fact]
    public void Zip64RecordSignatureSizeAndDirectoryRelationAreChecked()
    {
        var signature = Zip64(); signature[0] = 0; Assert.False(Check(Memory(signature)));
        var size = Zip64(); U64(size, 4, ulong.MaxValue); Assert.False(Check(Memory(size)));
        var offset = Zip64(); U64(offset, 48, ulong.MaxValue); U64(offset, 40, ulong.MaxValue);
        Assert.False(Check(Memory(offset)));
    }
    [Fact]
    public void BorrowedPositionIsNotRestoredAndStreamRemainsOpen()
    {
        var simple = Memory(End()); Assert.True(Check(simple)); Assert.False(simple.Closed);
        Assert.Equal(22L, PyOps.CallMethod(simple, "tell", [], [], []));
        var prefix = Memory(Join(new byte[100], End())); Assert.True(Check(prefix));
        Assert.Equal(100L, PyOps.CallMethod(prefix, "tell", [], [], [])); Assert.False(prefix.Closed);
        var wide = Memory(Zip64()); Assert.True(Check(wide)); Assert.Equal(56L, PyOps.CallMethod(wide, "tell", [], [], []));
    }
    [Fact]
    public void DynamicLookupOrderDoesNotCacheTheReadProbe()
    {
        var log = new List<string>();
        var stream = Stream(a => { log.Add("read:" + a[0]); return new PyBytes(End()); },
            () => { log.Add("tellcall"); return 22L; },
            a => log.Add("seek:" + a.Length), name => log.Add("get:" + name));
        Assert.True(Check(stream));
        Assert.Equal(new[] { "get:read", "get:seek", "seek:2", "get:tell", "tellcall", "get:seek", "seek:2", "get:read", "read:22" }, log);
    }
    [Fact]
    public void ActualBytearrayAndOversizedResponsesAreNotTruncated()
    {
        Assert.True(Check(Stream(_ => new PyByteArray(End()))));
        int reads = 0;
        Assert.True(Check(Stream(_ => { reads++; return new PyBytes(Join(End(), new byte[70000])); })));
        Assert.Equal(2, reads);
        Assert.False(Check(Stream(_ => new PyBytes(End()), () => BigInteger.One << 80)));
    }
    [Fact]
    public void NoneAndTextResponsesKeepStageSpecificErrors()
    {
        Assert.Throws<PyTypeError>(() => Check(Stream(_ => null)));
        int reads = 0;
        Assert.Throws<PyAttributeError>(() => Check(Stream(_ => reads++ == 0 ? new PyBytes([]) : null)));
        reads = 0;
        Assert.Throws<PyTypeError>(() => Check(Stream(_ => { reads++; return "text"; })));
        Assert.Equal(2, reads);
        int recordReads = 0, seeks = 0;
        byte[] locator = Zip64().AsSpan(56, 20).ToArray();
        var zip64 = Stream(a =>
        {
            recordReads++;
            return (long)a[0]! switch { 22 => new PyBytes(End()), 20 => new PyBytes(locator), _ => new string('x', 56) };
        }, () => 98L, _ => seeks++);
        Assert.Throws<PyTypeError>(() => Check(zip64));
        Assert.Equal(3, recordReads); Assert.Equal(4, seeks);
    }
    [Fact]
    public void SwallowedErrorsRestoreFramesAndAmbientContext()
    {
        long depth = PyTraceback.CaptureDepth(); var outer = new PyValueError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var error = new PyOSError("read");
            Assert.False(Check(Stream(_ => { PyTraceback.PushFrame("callback.py", "read"); PyTraceback.SetLine(7); throw error; })));
            Assert.Same(outer, PyErrors.Current); Assert.Equal(depth, PyTraceback.CaptureDepth());
            Assert.NotEmpty(error.Frames);
            Assert.False(Check(Stream(_ => throw new PyBadZipFile("user"))));
            var original = new PyValueError("closed");
            Assert.Same(original, Assert.Throws<PyValueError>(() => Check(Stream(_ => throw original))));
            PyTraceback.PushFrame("caller.py", "check_zip"); PyTraceback.SetLine(19);
            var ownError = Assert.Throws<PyTypeError>(() => Check(Stream(_ => null)));
            Assert.Same(outer, ownError.Context);
            Assert.Contains(ownError.Frames, frame => frame.File == "caller.py" && frame.Line == 19);
        }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(previous); }
    }
    [Fact]
    public void AttributeProbeFailureRestoresBeforePathConversion()
    {
        long depth = PyTraceback.CaptureDepth();
        var value = new Duck(name =>
        {
            if (name == "read") { PyTraceback.PushFrame("probe.py", "read"); PyTraceback.SetLine(3); throw new PyAttributeError(name); }
            throw new PyAttributeError(name);
        });
        try { Assert.Throws<PyTypeError>(() => Check(value)); Assert.Equal(depth, PyTraceback.CaptureDepth()); }
        finally { PyTraceback.PopTo(depth + 1); }
    }
    [Fact]
    public void OwnedPathClosesAndMissingPathReturnsFalse()
    {
        string file = Path.Combine(Path.GetTempPath(), "zip_" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllBytes(file, End()); Assert.True(Check(file));
            using (var exclusive = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.Equal(22, exclusive.Length);
            File.WriteAllBytes(file, [1, 2]); Assert.False(Check(file));
        }
        finally { File.Delete(file); }
        Assert.False(Check(file));
    }
}
