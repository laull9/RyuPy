using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleMethodCaptureTests
{
    private static PyModule Module(object? value)
    {
        string name = "capture_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterNative(name, new Dictionary<string, object?> { ["f"] = value });
        return PyModules.Import(name);
    }

    [Fact]
    public void NonModulesKeepIdentityWithoutLookingUpAnAttribute()
    {
        var list = new PyList([]);
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("CaptureOrdinary", [], null));
        var instance = Assert.IsType<PyInstance>(cls.Invoke([], [], []));
        foreach (object? value in new object?[] { null, PyNone.Instance, list, cls, instance, "text", 42L })
            {
            Assert.False(PyOps.IsModule(value));
            Assert.Same(value, PyOps.CaptureModuleTarget(value, "missing"));
        }
    }

    [Fact]
    public void MissingLookupPrecedesArgumentsAndCapturesCallerContext()
    {
        PyModule module = Module(null); int arguments = 0;
        var outer = new PyValueError("active"); object? context = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("capture.py", "caller");
        try
        {
            PyTraceback.SetLine(19);
            var error = Assert.Throws<PyAttributeError>(() =>
            {
                object? captured = PyOps.CaptureModuleTarget(module, "missing");
                arguments++;
                PyPic.CallCapturedMethod1(3910, true, captured, "missing", 1L);
            });
            Assert.Equal(0, arguments); Assert.Same(outer, error.Context);
            Assert.Equal(new PyStackFrame("capture.py", "caller", 19), error.Frames[0]);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(context); }
    }

    [Fact]
    public void DistinctSnapshotsBypassEveryFixedArityAndArraySiteCache()
    {
        PyFunction old = PyNativeFunctions.CreateVariadic("old", args => "old:" + args.Length);
        PyFunction next = PyNativeFunctions.CreateVariadic("next", args => "next:" + args.Length);
        PyModule module = Module(old);
        Assert.True(PyOps.IsModule(module));
        object? before = PyOps.CaptureModuleTarget(module, "f");
        Assert.Same(old, before);
        module.SetAttr("f", next);
        object? after = PyOps.CaptureModuleTarget(module, "f");
        Assert.NotSame(before, after); Assert.Same(next, after);
        foreach (int site in new[] { 3911, -1 })
        {
            for (int repeat = 0; repeat < 3; repeat++)
            {
                foreach (var pair in new[] { (before, "old:"), (after, "next:") })
                {
                    Assert.Equal(pair.Item2 + "0", PyPic.CallCapturedMethod0(site, true, pair.Item1, "f"));
                    Assert.Equal(pair.Item2 + "1", PyPic.CallCapturedMethod1(site, true, pair.Item1, "f", 1L));
                    Assert.Equal(pair.Item2 + "2", PyPic.CallCapturedMethod2(site, true, pair.Item1, "f", 1L, 2L));
                    Assert.Equal(pair.Item2 + "3", PyPic.CallCapturedMethod3(site, true, pair.Item1, "f", 1L, 2L, 3L));
                    Assert.Equal(pair.Item2 + "4", PyOps.CallCapturedMethod(true, pair.Item1, "f", [1L, 2L, 3L, 4L], [], []));
                    Assert.Equal(pair.Item2 + "0", PyOps.CallCapturedMethod(true, pair.Item1, "f", [], [], []));
                }
            }
        }
    }

    [Fact]
    public void FlatteningReentryCannotOverwriteThePreviouslyCapturedCallable()
    {
        var events = new List<string>();
        PyFunction old = PyNativeFunctions.Create("old", ["a", "b"], [null, null], 2,
            args => { events.Add("old"); return (long)args[0]! + (long)args[1]!; });
        PyFunction next = PyNativeFunctions.CreateVariadic("next", _ => { events.Add("next"); return 99L; });
        PyModule module = Module(old);
        object? captured = PyOps.CaptureModuleTarget(module, "f");
        var keyword = new PyDict(); keyword.Set("b", 2L);
        var iterable = new Once(() =>
        {
            events.Add("star"); module.SetAttr("f", next);
            object? nested = PyOps.CaptureModuleTarget(module, "f");
            Assert.Equal(99L, PyPic.CallCapturedMethod0(3912, true, nested, "f"));
        });
        Assert.Equal(3L, PyOps.CallCapturedMethod(true, captured, "f", [PyOps.StarArg(iterable)], [null], [keyword]));
        Assert.Equal(new[] { "star", "next", "old" }, events);
    }

    [Fact]
    public void NonCallableIsCheckedOnlyAfterArgumentExpansionAndExpansionErrorsWin()
    {
        PyModule module = Module(42L); object? captured = PyOps.CaptureModuleTarget(module, "f");
        int calls = 0;
        Assert.Throws<PyTypeError>(() => PyOps.CallCapturedMethod(true, captured, "f",
            [PyOps.StarArg(new Once(() => calls++))], [], []));
        Assert.Equal(1, calls);
        var sentinel = new PyValueError("argument failed");
        Assert.Same(sentinel, Assert.Throws<PyValueError>(() => PyOps.CallCapturedMethod(true, captured, "f",
            [PyOps.StarArg(new Once(() => throw sentinel))], [], [])));
    }

    [Fact]
    public void CapturedNoneAndNonCallableTargetsNeverBecomeMethodReceivers()
    {
        foreach (object? value in new object?[] { null, PyNone.Instance, new PyList([]) })
        {
            PyModule module = Module(value);
            Assert.True(PyOps.IsModule(module));
            object? target = PyOps.CaptureModuleTarget(module, "f");
            Assert.Same(value, target);
            Assert.Throws<PyTypeError>(() => PyPic.CallCapturedMethod1(3914, true, target, "append", 1L));
        }
    }

    [Fact]
    public void FalseFlagPreservesOrdinaryInstancePicAndArrayCallingProtocol()
    {
        var marker = new PyList([]);
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("OrdinaryCapturePic", [], null));
        cls.SetAttr("method", PyNativeFunctions.Create("method", ["self", "a", "b", "c"],
            [null, 0L, 0L, 0L], 1, _ => marker));
        var instance = Assert.IsType<PyInstance>(cls.Invoke([], [], []));
        bool captured = PyOps.IsModule(instance);
        object? target = PyOps.CaptureModuleTarget(instance, "method");
        Assert.False(captured); Assert.Same(instance, target);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            Assert.Same(marker, PyPic.CallCapturedMethod0(3915, captured, target, "method"));
            Assert.Same(marker, PyPic.CallCapturedMethod1(3915, captured, target, "method", 1L));
            Assert.Same(marker, PyPic.CallCapturedMethod2(3915, captured, target, "method", 1L, 2L));
            Assert.Same(marker, PyPic.CallCapturedMethod3(3915, captured, target, "method", 1L, 2L, 3L));
            Assert.Same(marker, PyOps.CallCapturedMethod(captured, target, "method", [], ["c"], [3L]));
        }
    }

    [Fact]
    public void CapturedBodyRetainedArraysRemainIndependentAfterReturnAndReentry()
    {
        var retained = new List<object?[]>();
        PyFunction? function = null;
        bool nested = false;
        var signature = PySignature.Create("retains", ["a"], ["pos"], ["0"]);
        function = new PyFunction("retains", signature, (self, args, names, values) =>
        {
            retained.Add(args);
            object? original = args[0];
            if (!nested)
            {
                nested = true;
                Assert.Equal(99L, PyPic.CallCapturedMethod1(3916, true, function, "f", 99L));
                nested = false;
            }
            Assert.Same(original, args[0]);
            return args[0];
        }, [null], false);
        PyModule module = Module(function);
        object? target = PyOps.CaptureModuleTarget(module, "f");
        Assert.Same(function, target);
        Assert.Equal(10L, PyPic.CallCapturedMethod1(3916, true, target, "f", 10L));
        Assert.Equal(20L, PyPic.CallCapturedMethod2(3916, true, target, "f", 20L, 21L));
        Assert.Equal(30L, PyPic.CallCapturedMethod3(3916, true, target, "f", 30L, 31L, 32L));
        Assert.Equal(6, retained.Count);
        Assert.Equal(new object?[] { 10L }, retained[0]);
        Assert.Equal(new object?[] { 20L, 21L }, retained[2]);
        Assert.Equal(new object?[] { 30L, 31L, 32L }, retained[4]);
        for (int i = 0; i < retained.Count; i++)
            for (int j = i + 1; j < retained.Count; j++)
                Assert.NotSame(retained[i], retained[j]);
    }

    private sealed class Once(Action action) : IPyIterator
    {
        private bool consumed;
        public bool MoveNext()
        {
            if (consumed) return false;
            consumed = true; action(); return true;
        }
        public object Current() => 1L;
    }
}
