using System;
using System.Collections.Generic;
using System.Text;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>退出表与异常报告共用全局系统状态，因此与帧栈用例串行运行。</summary>
[Collection("生成器帧栈")]
public class PyAtexitTests : IDisposable
{
    public PyAtexitTests() => PyAtexit.Clear();
    public void Dispose() => PyAtexit.Clear();
    private static object? Call(string name, params object?[] args) =>
        PyOps.CallValue(PyAtexit.CreateAttributes()[name], args, [], []);

    [Fact]
    public void RegisterKeepsIdentityAndArgumentsAndRunsLifo()
    {
        var log = new List<object?>();
        object item = new PyList([1L]);
        object callback = PyNativeFunctions.Create("f", ["item"], [null], 1, a => { log.Add(a[0]); return PyNone.Instance; });
        Assert.Same(callback, Call("register", callback, item));
        Call("register", callback, 2L);
        PyAtexit.RunExitFunctions();
        Assert.Equal(2L, log[0]); Assert.Same(item, log[1]);
        Assert.Equal(0L, Call("_ncallbacks"));
        Assert.Throws<PyTypeError>(() => Call("register", PyNone.Instance));
    }

    [Fact]
    public void UnregisterLeavesEmptySlotsAndReentryUsesCurrentTable()
    {
        var log = new List<string>(); int calls = 0;
        object older = PyNativeFunctions.Create("older", [], [], 0, _ => { log.Add("old"); return PyNone.Instance; });
        object newer = PyNativeFunctions.Create("newer", [], [], 0, _ =>
        {
            log.Add("new" + ++calls);
            if (calls == 1) PyAtexit.RunExitFunctions();
            return PyNone.Instance;
        });
        Call("register", older); Call("register", older); PyAtexit.Unregister(older);
        Assert.Equal(2L, Call("_ncallbacks"));
        PyAtexit.Clear(); Call("register", older); Call("register", newer);
        PyAtexit.RunExitFunctions();
        Assert.Equal(new[] { "new1", "new2", "old" }, log);
    }

    [Fact]
    public void CallbackFailuresDoNotStopLaterCallbacksOrLeakFramesAndContext()
    {
        PySystem.Initialize("atexit-test.py", []);
        PyModule sys = PyModules.Import("sys"); object? old = sys.GetAttr("stderr");
        var capture = new Capture(); sys.SetAttr("stderr", capture);
        object outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.CaptureDepth(); bool later = false;
        try
        {
            object older = PyNativeFunctions.Create("older", [], [], 0, _ =>
            {
                Assert.Same(outer, PyErrors.Reraise()); later = true; return PyNone.Instance;
            });
            object failure = PyNativeFunctions.Create("failure", [], [], 0, _ =>
            {
                PyTraceback.PushFrame("callback.py", "failure");
                throw new PySystemExit(7L).WithCause(new PyValueError("hidden cause"));
            });
            Call("register", older); Call("register", failure); PyAtexit.RunExitFunctions();
            Assert.True(later); Assert.Same(outer, PyErrors.Reraise());
            Assert.Equal(depth, PyTraceback.CaptureDepth());
            Assert.Contains("Exception ignored in atexit callback", capture.Text.ToString());
            Assert.Contains("SystemExit: 7", capture.Text.ToString());
            Assert.DoesNotContain("hidden cause", capture.Text.ToString());
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth + 1); sys.SetAttr("stderr", old); }
    }

    [Fact]
    public void BadReprCannotHideOriginalErrorOrAbortRegistry()
    {
        PySystem.Initialize("atexit-repr.py", []);
        PyModule sys = PyModules.Import("sys"); object? old = sys.GetAttr("stderr");
        var capture = new Capture(); sys.SetAttr("stderr", capture);
        try
        {
            var cls = new PyClass("BadReprCallback");
            cls.SetAttr("__call__", PyNativeFunctions.Create("__call__", ["self"], [null], 1,
                _ => throw new PyValueError("callback error")));
            cls.SetAttr("__repr__", PyNativeFunctions.Create("__repr__", ["self"], [null], 1,
                _ => throw new PyValueError("repr error")));
            Call("register", new PyInstance(cls)); PyAtexit.RunExitFunctions();
            Assert.Contains("<object repr() failed>", capture.Text.ToString());
            Assert.Contains("ValueError: callback error", capture.Text.ToString());
            Assert.Equal(0L, Call("_ncallbacks"));
        }
        finally { sys.SetAttr("stderr", old); }
    }

    private sealed class Capture : PyNativeObject
    {
        internal readonly StringBuilder Text = new();
        public override string TypeName => "AtexitCapture";
        public override object? GetAttr(string name) => name switch
        {
            "write" => PyNativeFunctions.Create(name, ["text"], [null], 1, a =>
            { string text = PyRepr.Str(a[0]); Text.Append(text); return (long)text.Length; }),
            "flush" => PyNativeFunctions.Create(name, [], [], 0, _ => PyNone.Instance),
            _ => throw Missing(name),
        };
    }
}
