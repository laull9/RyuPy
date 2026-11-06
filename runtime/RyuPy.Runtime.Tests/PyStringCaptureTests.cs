// 真实str子类的提前方法捕获专项：模拟生成代码的捕获→求参→调用链，不把普通CallMethod冒充前捕获。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringCaptureTests
{
    private static PyFunction Method(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("capture_test", body);
    private static PyClass TextClass()
    {
        var cls = (PyClass)PyOps.MakeClass("CaptureText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static object Text(PyClass cls, string text = "abc") => cls.Invoke([text], [], [])!;
    private static object? CaptureThenArguments(object receiver, string name, Func<object?[]> evaluate)
    {
        bool captured = PyOps.IsModule(receiver);
        object? target = PyOps.CaptureModuleTarget(receiver, name);
        object?[] args = evaluate();
        return PyOps.CallCapturedMethod(captured, target, name, args, [], []);
    }

    [Fact]
    public void OnlyRealStringInstancesJoinTheExistingCaptureDomain()
    {
        PyClass cls = TextClass(); object text = Text(cls);
        Assert.True(PyOps.IsModule(text));
        Assert.Same(text, PyObject.GetAttr(PyOps.CaptureModuleTarget(text, "startswith"), "__self__"));
        foreach (object ordinary in new object[]
        {
            "abc", new PyInstance(new PyClass("Plain")), cls, new PyClass("Other"),
            new PyList([]), new PyTuple([]), new PyBytes([]), new PyByteArray([]),
        })
        {
            Assert.False(PyOps.IsModule(ordinary));
            Assert.Same(ordinary, PyOps.CaptureModuleTarget(ordinary, "unqueried"));
        }
    }

    [Fact]
    public void AffixPropertyCapturesBeforeArgumentsReplaceItsTarget()
    {
        PyClass cls = TextClass(); object receiver = Text(cls);
        var events = new List<string>();
        object? target = PyObject.GetAttr("abc", "startswith");
        cls.SetAttr("startswith", new PyProperty(Method(_ => { events.Add("get"); return target; })));
        Assert.Equal(true, CaptureThenArguments(receiver, "startswith", () =>
        {
            events.Add("argument"); target = PyObject.GetAttr("zzz", "startswith");
            return ["a", 0L, 3L];
        }));
        Assert.Equal(new[] { "get", "argument" }, events);
        Assert.Equal(false, CaptureThenArguments(receiver, "startswith", () => ["a"]));
    }

    [Fact]
    public void NonAffixPropertiesUseTheSameEarlyCaptureRule()
    {
        PyClass cls = TextClass(); object receiver = Text(cls);
        var events = new List<string>();
        object? target = Method(args => { events.Add("old-body"); return args[0]; });
        cls.SetAttr("upper", new PyProperty(Method(_ => { events.Add("get"); return target; })));
        Assert.Equal("value", CaptureThenArguments(receiver, "upper", () =>
        {
            events.Add("argument"); target = Method(_ => "new-body"); return ["value"];
        }));
        Assert.Equal(new[] { "get", "argument", "old-body" }, events);
        Assert.Equal("new-body", CaptureThenArguments(receiver, "upper", () => []));
    }

    [Fact]
    public void InheritedAndSavedTargetsSurviveLaterClassReplacement()
    {
        PyClass cls = TextClass(); object receiver = Text(cls, "ab");
        object? saved = PyObject.GetAttr(receiver, "upper");
        object? prepared = PyOps.PrepareMethod(receiver, "upper");
        Assert.Equal("AB", CaptureThenArguments(receiver, "upper", () =>
        {
            cls.SetAttr("upper", Method(_ => "changed")); return [];
        }));
        Assert.Equal("changed", CaptureThenArguments(receiver, "upper", () => []));
        Assert.Equal("AB", PyOps.CallValue(saved, [], [], []));
        Assert.Equal("AB", PyOps.CallValue(prepared, [], [], []));
        Assert.Equal("AB", PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), "upper"), [receiver], [], []));
        PyObject.SetAttr(receiver, "upper", Method(_ => "instance"));
        Assert.Equal("instance", CaptureThenArguments(receiver, "upper", () => []));
    }

    [Fact]
    public void CapturingANonCallableStillEvaluatesArgumentsBeforeTheCallError()
    {
        PyClass cls = TextClass(); object receiver = Text(cls);
        var events = new List<string>();
        cls.SetAttr("upper", new PyProperty(Method(_ => { events.Add("get"); return 7L; })));
        var error = Assert.Throws<PyTypeError>(() => CaptureThenArguments(receiver, "upper", () =>
        {
            events.Add("argument"); return [1L];
        }));
        Assert.Equal("'int' object is not callable", error.PyMessage);
        Assert.Equal(new[] { "get", "argument" }, events);
    }

    [Fact]
    public void OwnedLookupErrorsPrecedeArgumentsAndCarryCallerContext()
    {
        PyClass cls = TextClass(); object receiver = Text(cls);
        cls.SetAttr("upper", new PyProperty(null));
        long depth = PyTraceback.PushFrame("capture_test.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        int arguments = 0;
        try
        {
            PyTraceback.SetLine(93);
            foreach (string name in new[] { "missing", "upper" })
            {
                var error = Assert.Throws<PyAttributeError>(() => CaptureThenArguments(receiver, name, () =>
                {
                    arguments++; return [1L];
                }));
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("capture_test.py", "caller", 93), error.Frames[0]);
            }
            Assert.Equal(0, arguments);
            // 普通实例保持false捕获及原自造错误路径，不顺手给它新增异常准备。
            var ordinary = new PyInstance(new PyClass("Plain"));
            var original = Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(ordinary, "missing"));
            Assert.Empty(original.Frames); Assert.Null(original.Context);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void UserGetterAndGetattrFailuresKeepTheirEmptyContextAndExistingFrames()
    {
        PyClass propertyClass = TextClass(), fallbackClass = TextClass();
        object propertyReceiver = Text(propertyClass), fallbackReceiver = Text(fallbackClass);
        long depth = PyTraceback.PushFrame("original_getter.py", "body"); PyTraceback.SetLine(11);
        var user = new PyValueError("user marker"); var cause = new PyKeyError("cause");
        user.WithCause(cause); PyErrors.PrepareRaise(user); var frames = user.Frames;
        Assert.Null(user.Context);
        propertyClass.SetAttr("startswith", new PyProperty(Method(_ => throw user)));
        fallbackClass.SetAttr("__getattr__", Method(_ => throw user));
        var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
        int arguments = 0;
        try
        {
            PyTraceback.SetLine(99);
            foreach (var pair in new[] { (propertyReceiver, "startswith"), (fallbackReceiver, "missing") })
            {
                Assert.Same(user, Assert.Throws<PyValueError>(() => CaptureThenArguments(pair.Item1, pair.Item2, () =>
                {
                    arguments++; return ["a"];
                })));
                Assert.Same(frames, user.Frames); Assert.Null(user.Context);
                Assert.Same(cause, user.Cause); Assert.True(user.SuppressContextFlag);
            }
            Assert.Equal(0, arguments);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void GetattrSuppliesTheOriginalCallableBeforeArgumentsChangeItsResult()
    {
        PyClass cls = TextClass(); object receiver = Text(cls);
        var events = new List<string>();
        object? target = Method(args => "old:" + args[0]);
        cls.SetAttr("__getattr__", Method(args => { events.Add("get:" + args[1]); return target; }));
        Assert.Equal("old:value", CaptureThenArguments(receiver, "custom", () =>
        {
            events.Add("argument"); target = Method(_ => "new"); return ["value"];
        }));
        Assert.Equal(new[] { "get:custom", "argument" }, events);
        Assert.Equal("new", CaptureThenArguments(receiver, "custom", () => []));
    }

    [Fact]
    public void CapturedNonemptyArgumentsRemainIndependentAcrossCalls()
    {
        var seen = new List<object?[]>();
        var signature = PySignature.Create("retain", ["args"], ["varpos"], ["0"]);
        var target = new PyFunction("retain", signature, (self, args, names, values) =>
        {
            seen.Add(args); return args[0];
        }, [], false);
        PyClass cls = TextClass(); object receiver = Text(cls);
        cls.SetAttr("upper", new PyProperty(Method(_ => target)));
        for (int i = 0; i < 2; i++)
        {
            bool captured = PyOps.IsModule(receiver);
            object? callable = PyOps.CaptureModuleTarget(receiver, "upper");
            Assert.Equal((long)i, PyPic.CallCapturedMethod3(3862, captured, callable, "upper", (long)i, 2L, 3L));
        }
        Assert.NotSame(seen[0], seen[1]);
        Assert.Equal(new object?[] { 0L, 2L, 3L }, seen[0]);
        Assert.Equal(new object?[] { 1L, 2L, 3L }, seen[1]);
    }
}
