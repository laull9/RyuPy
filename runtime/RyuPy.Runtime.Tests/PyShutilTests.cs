// 流复制的公开绑定与回调时序；原生模块注册及文件测试串行，避免共享状态干扰。
using System;
using System.Collections.Generic;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyShutilTests
{
    private sealed class Object(Func<string, object?> get) : PyNativeObject
    {
        public override string TypeName => "test_stream";
        public override object? GetAttr(string name) => get(name);
    }
    private static PyFunction Fn(string name, Func<object?[], object?> body, int count = 1) =>
        PyNativeFunctions.Create(name, count == 0 ? [] : ["value"], count == 0 ? [] : [null], count, body);
    private static object Source(Func<object?, object?> read) => new Object(name => name == "read"
        ? Fn("read", a => read(a[0])) : throw new PyAttributeError(name));
    private static object Sink(Action<object?> write) => new Object(name => name == "write"
        ? Fn("write", a => { write(a[0]); return 0L; }) : throw new PyAttributeError(name));
    private static object? Copy(object source, object destination, object? length = null) =>
        PyOps.CallValue(PyShutil.CreateAttributes()["copyfileobj"], [source, destination, length ?? 0L], [], []);
    private static object? Method(object value, string name, params object?[] args) =>
        PyOps.CallMethod(value, name, args, [], []);

    [Fact]
    public void NativeModuleExportsOnlyTheImplementedSurface()
    {
        PySystem.Initialize("shutil-tests.py", []); PyNativeModules.Register();
        var module = PyModules.Import("shutil");
        Assert.IsType<PyFunction>(module.GetAttr("copyfileobj"));
        Assert.Equal(OperatingSystem.IsWindows() ? 1048576L : 65536L, module.GetAttr("COPY_BUFSIZE"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("copy"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("copyfile"));
    }
    [Fact]
    public void AllKeywordSignatureAndBindingErrorsPrecedeCallbacks()
    {
        object? function = PyShutil.CreateAttributes()["copyfileobj"]; int reads = 0;
        object src = Source(_ => { reads++; return ""; }), dst = Sink(_ => { });
        Assert.Same(PyNone.Instance, PyOps.CallValue(function, [], ["fsrc", "fdst", "length"], [src, dst, 3L]));
        Assert.Equal(1, reads);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [src], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [src, dst, 1L, 2L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [src, dst], ["fsrc"], [src]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [src, dst], ["unknown"], [1L]));
        Assert.Equal(1, reads);
    }
    [Fact]
    public void SavedFunctionReadsTheCurrentModuleDefaultWithoutTestingItAgain()
    {
        string name = "shutil_test_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterNative(name, PyShutil.CreateAttributes()); var module = PyModules.Import(name);
        object? saved = module.GetAttr("copyfileobj");
        object replacement = new Object(_ => throw new PyValueError("default must not be tested"));
        module.SetAttr("COPY_BUFSIZE", replacement);
        object src = Source(length => { Assert.Same(replacement, length); return ""; });
        Assert.Same(PyNone.Instance, PyOps.CallValue(saved, [src, Sink(_ => { })], [], []));
        int truthCalls = 0;
        object falseLength = new Object(key => key == "__bool__" ? Fn("truth", _ => { truthCalls++; return false; }, 0)
            : throw new PyAttributeError(key));
        PyOps.CallValue(saved, [src, Sink(_ => { }), falseLength], [], []);
        Assert.Equal(1, truthCalls);
    }
    [Fact]
    public void LengthTruthThenReadAndWriteLookupOccurOnceInOrder()
    {
        var log = new List<string>();
        object length = new Object(name => name == "__bool__" ? Fn("truth", _ => { log.Add("length"); return true; }, 0)
            : throw new PyAttributeError(name));
        int calls = 0;
        object src = new Object(name => { log.Add(name); return Fn("read", a =>
            { Assert.Same(length, a[0]); log.Add("callread"); return calls++ == 0 ? "x" : ""; }); });
        object dst = new Object(name => { log.Add(name); return Fn("write", _ => { log.Add("callwrite"); return null; }); });
        Copy(src, dst, length);
        Assert.Equal(new[] { "length", "read", "write", "callread", "callwrite", "callread" }, log);
    }
    [Fact]
    public void BufferTruthAndIdentityArePreservedAndWriteResultIsIgnored()
    {
        int truth = 0, reads = 0, writes = 0;
        object buffer = new Object(name => name == "__bool__" ? Fn("truth", _ => { truth++; return true; }, 0)
            : throw new PyAttributeError(name));
        Copy(Source(_ => reads++ == 0 ? buffer : null), Sink(value => { Assert.Same(buffer, value); writes++; }));
        Assert.Equal(1, truth); Assert.Equal(2, reads); Assert.Equal(1, writes);
    }
    [Fact]
    public void NegativeLengthAndShortReadsContinueUntilFalse()
    {
        var values = new Queue<object?>(["a", "b", ""]); var output = new List<object?>();
        Copy(Source(length => { Assert.Equal(-1L, length); return values.Dequeue(); }), Sink(output.Add), -1L);
        Assert.Equal(new object?[] { "a", "b" }, output); Assert.Empty(values);
    }
    [Fact]
    public void CallbacksAreCachedEvenWhenReadReplacesDestinationWrite()
    {
        int old = 0, changed = 0, reads = 0;
        object? write = Fn("write", _ => { old++; return null; });
        object dst = new Object(_ => write);
        Copy(Source(_ => { write = Fn("newwrite", _ => { changed++; return null; }); return reads++ == 0 ? "x" : ""; }), dst);
        Assert.Equal(1, old); Assert.Equal(0, changed);
    }
    [Fact]
    public void ErrorsKeepIdentityAndDoNotTriggerCleanupOrFurtherCallbacks()
    {
        var error = new PyValueError("callback"); int writes = 0;
        Assert.Same(error, Assert.Throws<PyValueError>(() => Copy(Source(_ => throw error), Sink(_ => writes++))));
        Assert.Equal(0, writes);
        int reads = 0;
        Assert.Same(error, Assert.Throws<PyValueError>(() => Copy(Source(_ => { reads++; return "x"; }), Sink(_ => throw error))));
        Assert.Equal(1, reads);
        object bad = new Object(_ => throw error);
        Assert.Same(error, Assert.Throws<PyValueError>(() => Copy(bad, Sink(_ => writes++))));
        object truth = new Object(name => name == "__bool__" ? Fn("truth", _ => throw error, 0) : throw new PyAttributeError(name));
        Assert.Same(error, Assert.Throws<PyValueError>(() => Copy(Source(_ => truth), Sink(_ => writes++))));
        Assert.Equal(0, writes);
        Assert.Same(error, Assert.Throws<PyValueError>(() => Copy(Source(_ => { reads++; return ""; }), bad)));
        Assert.Equal(1, reads);
        Assert.Same(error, Assert.Throws<PyValueError>(() => Copy(bad, bad, truth)));
    }
    [Fact]
    public void MemoryStreamsCopyFromCurrentPositionAndRemainOpen()
    {
        object source = PyIO.CreateBytesIO([new PyBytes([1, 2, 3])], [], [])!;
        object target = PyIO.CreateBytesIO([], [], [])!;
        Method(source, "seek", 1L); Copy(source, target, 1L);
        Assert.Equal(new byte[] { 2, 3 }, Assert.IsType<PyBytes>(Method(target, "getvalue")).Bytes);
        Assert.False(((PyIOStream)source).Closed); Assert.False(((PyIOStream)target).Closed);
        object text = PyIO.CreateStringIO(["abc"], [], [])!, output = PyIO.CreateStringIO([], [], [])!;
        Copy(text, output, 2L); Assert.Equal("abc", Method(output, "getvalue"));
    }
    [Fact]
    public void RealFilesUseExistingIoAndAreNotClosedByCopy()
    {
        string root = Path.Combine(Path.GetTempPath(), "shutil_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        PyIOStream? source = null, destination = null;
        try
        {
            string input = Path.Combine(root, "input"), output = Path.Combine(root, "output");
            File.WriteAllBytes(input, [4, 5, 6]);
            source = (PyIOStream)PyIO.Open([input, "rb"], [], [])!;
            destination = (PyIOStream)PyIO.Open([output, "wb"], [], [])!;
            Copy(source, destination, 1L);
            Assert.False(source.Closed); Assert.False(destination.Closed);
            Method(destination, "flush"); Assert.Equal(new byte[] { 4, 5, 6 }, File.ReadAllBytes(output));
        }
        finally { source?.Close(); destination?.Close(); Directory.Delete(root, true); }
    }
}
