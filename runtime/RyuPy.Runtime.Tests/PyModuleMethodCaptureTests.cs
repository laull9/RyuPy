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
            Assert.Same(value, PyOps.CaptureModuleMethod(value, "missing"));
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
                object? captured = PyOps.CaptureModuleMethod(module, "missing");
                arguments++;
                PyPic.CallMethod1(3910, captured, "missing", 1L);
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
        object? before = PyOps.CaptureModuleMethod(module, "f");
        module.SetAttr("f", next);
        object? after = PyOps.CaptureModuleMethod(module, "f");
        Assert.NotSame(before, after);
        foreach (int site in new[] { 3911, -1 })
        {
            for (int repeat = 0; repeat < 3; repeat++)
            {
                foreach (var pair in new[] { (before, "old:"), (after, "next:") })
                {
                    Assert.Equal(pair.Item2 + "0", PyPic.CallMethod0(site, pair.Item1, "f"));
                    Assert.Equal(pair.Item2 + "1", PyPic.CallMethod1(site, pair.Item1, "f", 1L));
                    Assert.Equal(pair.Item2 + "2", PyPic.CallMethod2(site, pair.Item1, "f", 1L, 2L));
                    Assert.Equal(pair.Item2 + "3", PyPic.CallMethod3(site, pair.Item1, "f", 1L, 2L, 3L));
                    Assert.Equal(pair.Item2 + "4", PyPic.CallMethod(site, pair.Item1, "f", [1L, 2L, 3L, 4L], [], []));
                    Assert.Equal(pair.Item2 + "0", PyOps.CallMethod(pair.Item1, "f", [], [], []));
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
        object? captured = PyOps.CaptureModuleMethod(module, "f");
        var keyword = new PyDict(); keyword.Set("b", 2L);
        var iterable = new Once(() =>
        {
            events.Add("star"); module.SetAttr("f", next);
            object? nested = PyOps.CaptureModuleMethod(module, "f");
            Assert.Equal(99L, PyPic.CallMethod0(3912, nested, "f"));
        });
        Assert.Equal(3L, PyPic.CallMethod(3912, captured, "f", [PyOps.StarArg(iterable)], [null], [keyword]));
        Assert.Equal(new[] { "star", "next", "old" }, events);
    }

    [Fact]
    public void NonCallableIsCheckedOnlyAfterArgumentExpansionAndExpansionErrorsWin()
    {
        PyModule module = Module(42L); object? captured = PyOps.CaptureModuleMethod(module, "f");
        int calls = 0;
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod(3913, captured, "f",
            [PyOps.StarArg(new Once(() => calls++))], [], []));
        Assert.Equal(1, calls);
        var sentinel = new PyValueError("argument failed");
        Assert.Same(sentinel, Assert.Throws<PyValueError>(() => PyOps.CallMethod(captured, "f",
            [PyOps.StarArg(new Once(() => throw sentinel))], [], [])));
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
