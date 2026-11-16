// 共享warnings异常边界：只恢复被吞IO回调帧，提升为error时保留身份并登记活动异常。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyWarningsExceptionTests
{
    private static object? Call(string name, params object?[] args) => PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, [], []);
    private static object? Method(object? obj, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(obj, name), args, [], []);
    private static void Scoped(string action, Action body)
    {
        const string file = "warnings_exception_control.py";
        PyModules.Register("warnings_exception_control", "", file, () => { }, _ => PyNone.Instance, (_, _, _) => { });
        long frame = PyTraceback.PushFrame(file, "caller");
        object? context = Call("catch_warnings"); Method(context, "__enter__");
        try { Call("simplefilter", action); body(); }
        finally { Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); PyTraceback.PopTo(frame); }
    }
    [Fact]
    public void SuppressedIoWriteRestoresOnlyCallbackFramesAndKeepsActiveException()
    {
        PySystem.Initialize("warnings_exception_control.py", []);
        var sys = PyModules.Import("sys"); object? old = sys.GetAttr("stderr");
        var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            sys.SetAttr("stderr", new Sink(() =>
            {
                PyTraceback.PushFrame("writer.py", "write");
                throw (PyOSError)PyErrors.PrepareRaise(new PyOSError("sink"));
            }));
            Scoped("always", () =>
            {
                long depth = PyTraceback.CaptureDepth();
                for (int i = 0; i < 1500; i++)
                {
                    Call("warn", "ordinary warning");
                    Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(outer, PyErrors.Reraise());
                }
                var later = (PyValueError)PyErrors.PrepareRaise(new PyValueError("later"));
                Assert.DoesNotContain(later.Frames, f => f.Function == "write");
                Assert.Same(outer, later.Context);
            });
        }
        finally { sys.SetAttr("stderr", old); PyErrors.LeaveHandler(previous); }
    }
    [Fact]
    public void ErrorFilterPreservesWarningIdentityFramesAndNestedHandler()
    {
        Scoped("error", () =>
        {
            var warning = new PyUserWarning("warning"); PyErrors.PrepareRaise(warning); var frames = warning.Frames;
            var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
            try
            {
                var inner = new PyValueError("inner"); object? innerPrevious = PyErrors.EnterHandler(inner);
                try
                {
                    Assert.Same(warning, Assert.Throws<PyUserWarning>(() => Call("warn", warning)));
                    Assert.Same(inner, warning.Context); Assert.Same(frames, warning.Frames);
                    Assert.Same(inner, PyErrors.Reraise());
                }
                finally { PyErrors.LeaveHandler(innerPrevious); }
                var fresh = Assert.Throws<PyUserWarning>(() => Call("warn", "fresh"));
                Assert.Same(outer, fresh.Context); Assert.NotEmpty(fresh.Frames);
                Assert.Same(outer, PyErrors.Reraise());
            }
            finally { PyErrors.LeaveHandler(previous); }
        });
    }
    [Fact]
    public void NonIoWriteErrorPropagatesWithOriginalIdentityAndCallbackFrames()
    {
        PySystem.Initialize("warnings_exception_control.py", []);
        var sys = PyModules.Import("sys"); object? old = sys.GetAttr("stderr");
        var marker = new PyValueError("writer");
        try
        {
            sys.SetAttr("stderr", new Sink(() =>
            {
                PyTraceback.PushFrame("writer.py", "write"); throw (PyValueError)PyErrors.PrepareRaise(marker);
            }));
            Scoped("always", () =>
            {
                long depth = PyTraceback.CaptureDepth();
                Assert.Same(marker, Assert.Throws<PyValueError>(() => Call("warn", "ordinary warning")));
                Assert.Equal(depth + 1, PyTraceback.CaptureDepth());
                Assert.Contains(marker.Frames, f => f.Function == "write");
            });
        }
        finally { sys.SetAttr("stderr", old); }
    }
    private sealed class Sink(Action write) : PyNativeObject
    {
        public override string TypeName => "WarningsExceptionSink";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, _ => { write(); return 0L; }) : throw Missing(name);
    }
}
