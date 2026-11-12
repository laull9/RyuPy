// 当前异常模块 API 使用真实 handler/帧；默认格式与限帧测试分开，避免零帧掩盖缺口。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTracebackModuleTests
{
    private static object? Call(string name, params object?[] args) =>
        PyOps.CallValue(PyTracebackModule.CreateAttributes()[name], args, [], []);

    [Fact]
    public void CurrentExceptionAndCauseAreFormattedWithoutMutatingHistory()
    {
        var cause = new PyValueError("inner");
        var error = new PyTypeError("outer") {
            Frames = [new("file.py", "inner", 12), new("file.py", "outer", 7)] };
        error.WithCause(cause);
        object? previous = PyErrors.EnterHandler(error);
        try
        {
            string text = Assert.IsType<string>(Call("format_exc"));
            Assert.Contains("ValueError: inner\n", text);
            Assert.Contains("direct cause", text);
            Assert.Contains("  File \"file.py\", line 7, in outer\n  File \"file.py\", line 12, in inner\n", text);
            Assert.Equal("TypeError: outer\n", Call("format_exc", 0L, false));
            string first = Assert.IsType<string>(Call("format_exc", 1L, false));
            Assert.Contains("line 7", first); Assert.DoesNotContain("line 12", first);
            string last = Assert.IsType<string>(Call("format_exc", -1L, false));
            Assert.Contains("line 12", last); Assert.DoesNotContain("line 7", last);
            Assert.Same(error, PyErrors.Current); Assert.Equal(2, error.Frames.Length);
            Assert.Throws<PyTypeError>(() => Call("format_exc", "bad"));
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void NoExceptionAndWriterFailureDoNotFabricateExceptionState()
    {
        Assert.Null(PyErrors.Current);
        Assert.Equal("NoneType: None\n", Call("format_exc"));
        var writer = new Writer();
        Assert.Same(PyNone.Instance, Call("print_exc", PyNone.Instance, writer));
        Assert.Equal("NoneType: None\n", writer.Text);
        Assert.Equal(new[] { "NoneType: None\n", "" }, writer.Parts);
        int calls = 0;
        var cls = (PyClass)PyOps.MakeClass("ChainFlag", [], null);
        cls.SetAttr("__bool__", PyNativeFunctions.Create("__bool__", ["self"], [null], 1,
            _ => { calls++; return true; }));
        object? flag = cls.Invoke([], [], []);
        Assert.Equal("NoneType: None\n", Call("format_exc", 0L, flag));
        Assert.Equal(1, calls);
        Assert.Throws<PyTypeError>(() => Call("format_exc", "bad", flag));
        Assert.Equal(1, calls);
        writer.Parts.Clear();
        Assert.Throws<PyValueError>(() => Call("print_exc", 1.5, writer, flag));
        Assert.Equal(1, calls); Assert.Empty(writer.Parts);
        Assert.Same(PyNone.Instance, Call("print_exc", 0L, writer, flag));
        Assert.Equal(2, calls);
        Assert.Equal(new[] { "NoneType: None\n", "" }, writer.Parts);
        writer.Fail = true;
        Assert.Throws<PyRuntimeError>(() => Call("print_exc", PyNone.Instance, writer));
        Assert.Null(PyErrors.Current);
    }

#if !TRACE_ADAPTER_TEST
    [Fact]
    public void ProductObservationIsRejectedWithoutOpeningPropagation()
    {
        PyTraceback.Reset(); PyErrors.LeaveHandler(null); PyAtexit.Clear();
        PySystem.Initialize("traceback-module-test.py", []);
        PyTraceProduct.Enter(1);
        try
        {
            Assert.Throws<PyNotImplementedError>(() => Call("format_exc"));
            Assert.Null(PyErrors.Current);
            Assert.Equal(0, PyTraceback.CaptureDepth());
        }
        finally { PyTraceProduct.Exit(); }
    }
#endif

    private sealed class Writer : PyNativeObject
    {
        internal string Text = "";
        internal readonly System.Collections.Generic.List<string> Parts = new();
        internal bool Fail;
        public override string TypeName => "Writer";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, a =>
            {
                if (Fail) throw new PyRuntimeError("writer");
                Text += (string)a[0]!; Parts.Add((string)a[0]!); return PyNone.Instance;
            }) : throw Missing(name);
    }
}
