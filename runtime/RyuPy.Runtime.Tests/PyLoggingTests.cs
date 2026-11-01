// 日志竖切验收：只在开启等级后格式化，传播不重新应用父 logger 等级，流归调用方所有。
using System;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyLoggingTests
{
    private static object? Call(object obj, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(obj, name), args, [], []);
    private static object Create(string name, params object?[] args) =>
        PyOps.CallValue(PyLogging.CreateAttributes()[name], args, [], [])!;
    private static object Logger() => Create("getLogger", "unit." + Guid.NewGuid().ToString("N"));
    private static object Buffer() => PyIO.CreateStringIO([], [], [])!;

    [Fact]
    public void FilteredMessagesNeverInvokeStringConversionOrValidateAdvancedOptions()
    {
        object logger = Logger(); Call(logger, "setLevel", 20L); PyObject.SetAttr(logger, "propagate", false);
        var cls = (PyClass)PyOps.MakeClass("Bad", [], null);
        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => throw new PyValueError("must not format"), []));
        object bad = cls.Invoke([], [], [])!;
        PyOps.CallValue(PyObject.GetAttr(logger, "debug"), [bad], ["unsupported"], [true]);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyObject.GetAttr(logger, "info"), [bad], ["unsupported"], [true]));
    }

    [Fact]
    public void PercentFormattingAndCallerOwnedStreamWork()
    {
        object stream = Buffer(), logger = Logger(), handler = Create("StreamHandler", stream);
        Call(handler, "setFormatter", Create("Formatter", "%(levelname)s:%(message)s"));
        PyObject.SetAttr(logger, "propagate", false); Call(logger, "setLevel", "INFO"); Call(logger, "addHandler", handler);
        Call(logger, "info", "count=%d %s %%", 3L, "cat");
        var mapping = new PyDict(); mapping.Set("name", "猫"); Call(logger, "warning", "%(name)r", mapping);
        Assert.Equal("INFO:count=3 cat %\nWARNING:'猫'\n", Call(stream, "getvalue"));
        Call(handler, "close"); Assert.Equal(false, PyObject.GetAttr(stream, "closed"));
    }

    [Fact]
    public void LateParentIdentityAndHandlerThresholdAreIndependent()
    {
        string name = "parent_" + Guid.NewGuid().ToString("N");
        object child = Create("getLogger", name + ".child"), parent = Create("getLogger", name);
        Assert.Same(child, Create("getLogger", name + ".child")); Assert.Same(parent, PyObject.GetAttr(child, "parent"));
        object stream = Buffer(), handler = Create("StreamHandler", stream);
        Call(parent, "setLevel", 50L); PyObject.SetAttr(parent, "propagate", false); Call(parent, "addHandler", handler);
        Call(child, "setLevel", 10L); Call(handler, "setLevel", 20L);
        Call(child, "debug", "hidden"); Call(child, "info", "visible");
        Assert.Equal("visible\n", Call(stream, "getvalue"));
    }

    [Fact]
    public void EmitCapturesStreamAfterFormatAndFlushReadsCurrentStream()
    {
        object replacement = Buffer(); object? handler = null; int writes = 0;
        var cls = (PyClass)PyOps.MakeClass("Sink", [], null);
        cls.SetAttr("write", new PyFunction("write", PySignature.Create("write", ["self", "text"], ["pos", "pos"], ["0", "0"]),
            (_, _, _, _) => { writes++; PyObject.SetAttr(handler, "stream", replacement); return null; }, []));
        cls.SetAttr("flush", new PyFunction("flush", PySignature.Create("flush", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => throw new PyValueError("old stream must not flush"), []));
        handler = Create("StreamHandler", cls.Invoke([], [], []));
        object logger = Logger(); Call(logger, "setLevel", 10L); PyObject.SetAttr(logger, "propagate", false); Call(logger, "addHandler", handler);
        Call(logger, "info", "message"); Assert.Equal(1, writes);
        Assert.Same(replacement, PyObject.GetAttr(handler, "stream"));
    }
    [Fact]
    public void EmitHandlesOrdinaryFlushErrorsButRecursionEscapesAndOuterStateSurvives()
    {
        var outer = new PyKeyError("outer"); object? token = PyErrors.EnterHandler(outer);
        try
        {
            foreach (bool recursion in new[] { false, true })
            {
                var cls = (PyClass)PyOps.MakeClass("FlushFailure", [], null);
                cls.SetAttr("write", PyNativeFunctions.Create("write", ["self", "text"], [null, null], 2, _ => null));
                cls.SetAttr("flush", PyNativeFunctions.Create("flush", ["self"], [null], 1,
                    _ => throw (recursion ? (PyBaseException)new PyRecursionError("recursive") : new PyValueError("flush failed"))));
                object handler = Create("StreamHandler", cls.Invoke([], [], []));
                object logger = Logger(); Call(logger, "setLevel", 10L); PyObject.SetAttr(logger, "propagate", false);
                Call(logger, "addHandler", handler);
                if (recursion) Assert.Throws<PyRecursionError>(() => Call(logger, "info", "message"));
                else Call(logger, "info", "message");
                Assert.Same(outer, PyErrors.Reraise());
                if (!recursion) Assert.Throws<PyValueError>(() => Call(handler, "flush"));
            }
        }
        finally { PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void FormatterValidationAndMessageBindingHappenAtTheirCorrectBoundaries()
    {
        foreach (string invalid in new[] { "literal", "%s", "%%", "%(message)q" })
            Assert.Throws<PyValueError>(() => Create("Formatter", invalid));
        object stream = Buffer(), handler = Create("StreamHandler", stream), logger = Logger();
        Call(handler, "setFormatter", Create("Formatter", "")); Call(logger, "addHandler", handler);
        PyObject.SetAttr(logger, "propagate", false); Call(logger, "setLevel", 20L);
        PyOps.CallValue(PyObject.GetAttr(logger, "info"), [], ["msg"], ["keyword"]);
        Assert.Equal("keyword\n", Call(stream, "getvalue"));
        Assert.Throws<PyTypeError>(() => Call(logger, "debug"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(logger, "debug"), ["one"], ["msg"], ["two"]));
        PyOps.CallValue(PyObject.GetAttr(logger, "debug"), [], ["msg", "unsupported"], ["hidden", true]);
    }

}
