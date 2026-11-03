// 全部用例先替换 fake stdin，finally/Dispose 还原，绝不关闭测试宿主 Console 输入。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PySiteQuittersTests : IDisposable
{
    private readonly PyModule _sys;
    private readonly object? _originalInput;
    private readonly FakeInput _input = new();
    public PySiteQuittersTests()
    {
        PySystem.Initialize("quitter-test.py", []);
        _sys = PyModules.Import("sys");
        _originalInput = _sys.GetAttr("stdin");
        _sys.SetAttr("stdin", _input);
    }
    public void Dispose() => _sys.SetAttr("stdin", _originalInput);
    private static PyInstance Exit => Assert.IsType<PyInstance>(PyOps.BuiltinFunction("exit"));
    private static PyInstance Quit => Assert.IsType<PyInstance>(PyOps.BuiltinFunction("quit"));
    private static object? Call(object value, params object?[] args) => PyOps.CallValue(value, args, [], []);

    [Fact]
    public void HelpersAreStableDistinctInstancesOfOneRealClass()
    {
        PyInstance exit = Exit, quit = Quit;
        Assert.Same(exit, PyOps.BuiltinFunction("exit")); Assert.NotSame(exit, quit);
        Assert.Same(exit.Class, quit.Class); Assert.Same(exit.Class, PyObject.TypeOf(exit));
        Assert.Equal("Quitter", PyObject.GetAttr(exit.Class, "__name__"));
        Assert.Equal("_sitebuiltins", PyObject.GetAttr(exit.Class, "__module__"));
        Assert.Equal("<class '_sitebuiltins.Quitter'>", PyRepr.Repr(exit.Class));
        string eof = OperatingSystem.IsWindows() ? "Ctrl-Z plus Return" : "Ctrl-D (i.e. EOF)";
        Assert.Equal("exit", PyObject.GetAttr(exit, "name")); Assert.Equal(eof, PyObject.GetAttr(exit, "eof"));
        Assert.Equal("Use exit() or " + eof + " to exit", PyRepr.Repr(exit));
        Assert.Equal(PyRepr.Repr(exit), PyRepr.Str(exit));
        Assert.Equal("Use quit() or " + eof + " to exit", PyRepr.Repr(quit));
        Assert.Equal(0, _input.Calls);
    }

    [Fact]
    public void DefaultNoneAndExplicitCodesKeepSingleArgumentIdentity()
    {
        PySystemExit zero = Assert.Throws<PySystemExit>(() => Call(Exit));
        Assert.Same(PyNone.Instance, zero.Code); Assert.Same(PyNone.Instance, Assert.Single(zero.Args));
        foreach (object code in new object[] { PyNone.Instance, 7L, "bye", new PyList([]) })
        {
            PySystemExit error = Assert.Throws<PySystemExit>(() => Call(Exit, code));
            Assert.Same(code, error.Code); Assert.Same(code, Assert.Single(error.Args));
        }
        object value = new PyList([1L]);
        PySystemExit keyword = Assert.Throws<PySystemExit>(() => PyOps.CallValue(Quit, [], ["code"], [value]));
        Assert.Same(value, keyword.Code); Assert.Equal(6, _input.Calls);
    }

    [Fact]
    public void BinderErrorsPrecedeCloseAndKeepCallerFrame()
    {
        long depth = PyTraceback.PushFrame("quitter-test.py", "caller"); PyTraceback.SetLine(17);
        try
        {
            foreach (Func<object?> call in new Func<object?>[] {
                () => Call(Exit, 1L, 2L),
                () => PyOps.CallValue(Exit, [1L], ["code"], [2L]),
                () => PyOps.CallValue(Exit, [], ["status"], [1L]) })
            {
                PyTypeError error = Assert.Throws<PyTypeError>(() => call());
                Assert.NotEmpty(error.Frames); Assert.Equal(0, _input.Calls);
            }
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void CloseBaseExceptionsCannotReplaceContextOrLeakCallbackFrames()
    {
        var active = new PyRuntimeError("active"); object? previous = PyErrors.EnterHandler(active);
        long depth = PyTraceback.PushFrame("quitter-test.py", "caller");
        try
        {
            foreach (PyBaseException closeError in new PyBaseException[] {
                new PyValueError("close"), new PySystemExit("close"), new PyKeyboardInterrupt(), new PyGeneratorExit() })
            {
                _input.Action = () =>
                {
                    PyTraceback.PushFrame("callback.py", "close");
                    PyErrors.EnterHandler(closeError);
                    throw closeError;
                };
                object code = new PyList([]);
                PySystemExit error = Assert.Throws<PySystemExit>(() => Call(Exit, code));
                Assert.Same(code, error.Code); Assert.Same(active, error.Context);
                Assert.NotSame(closeError, error.Context); Assert.Same(active, PyErrors.Current);
                Assert.Equal(depth, PyTraceback.CaptureDepth());
                Assert.DoesNotContain(error.Frames, frame => frame.File == "callback.py");
            }
            Assert.Equal(4, _input.Calls);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void MissingCloseAndNoneStillRaiseRequestedExit()
    {
        foreach (object target in new object[] { PyNone.Instance, new PyInstance(new PyClass("WithoutClose")) })
        {
            _sys.SetAttr("stdin", target);
            PySystemExit error = Assert.Throws<PySystemExit>(() => Call(Exit, 3L));
            Assert.Equal(3L, error.Code);
        }
        Assert.Equal(0, _input.Calls);
    }

    [Fact]
    public void CloseMayReenterQuitterWithoutReplacingOuterCode()
    {
        object outer = new PyList([1L]), inner = new PyList([2L]);
        _input.Action = () => { if (_input.Calls == 1) Call(Quit, inner); };
        PySystemExit result = Assert.Throws<PySystemExit>(() => Call(Exit, outer));
        Assert.Same(outer, result.Code); Assert.Equal(2, _input.Calls);
    }

    [Fact]
    public void ReprReadsWritableObjectsAndConvertsInOrder()
    {
        PyInstance q = Exit; object? name = PyObject.GetAttr(q, "name"), eof = PyObject.GetAttr(q, "eof");
        var seen = new List<string>();
        try
        {
            PyObject.SetAttr(q, "name", new TextValue("leave", seen));
            PyObject.SetAttr(q, "eof", new TextValue("END", seen));
            Assert.Equal("Use leave() or END to exit", PyRepr.Repr(q));
            Assert.Equal(new[] { "leave", "END" }, seen);
        }
        finally { PyObject.SetAttr(q, "name", name); PyObject.SetAttr(q, "eof", eof); }
    }

    [Fact]
    public void DirectBuiltinAndSavedInstanceBothHonorClassCallRebinding()
    {
        PyInstance saved = Exit; PyClass type = saved.Class; object? original = PyObject.GetAttr(type, "__call__");
        object code = new PyList([]);
        try
        {
            type.SetAttr("__call__", PyNativeFunctions.Create("replacement", ["self", "code"], [null, PyNone.Instance], 1, a => a[1]));
            Assert.Same(code, PyOps.CallBuiltin("exit", [code], [], []));
            Assert.Same(code, Call(saved, code)); Assert.Same(code, PyOps.CallBuiltin("quit", [], ["code"], [code]));
            Assert.Equal(0, _input.Calls);
        }
        finally { type.SetAttr("__call__", original); }
    }

    [Fact]
    public void NativeClassMetadataRebindingDoesNotChangeOrdinaryClassDefaults()
    {
        PyClass type = Exit.Class; object? original = PyObject.GetAttr(type, "__module__");
        try
        {
            foreach (object value in new object[] { "other", PyNone.Instance, 42L })
            {
                type.SetAttr("__module__", value); Assert.Same(value, PyObject.GetAttr(type, "__module__"));
                Assert.Equal(value is string ? "<class 'other.Quitter'>" : "<class 'Quitter'>", type.ToString());
            }
            Assert.Throws<PyTypeError>(() => type.DeleteAttr("__module__"));
            var ordinary = new PyClass("Ordinary"); ordinary.SetAttr("__module__", "old-ignored-value");
            Assert.Equal("__main__", PyObject.GetAttr(ordinary, "__module__")); Assert.Equal("<class 'Ordinary'>", ordinary.ToString());
            Assert.NotNull(typeof(PyClass).GetConstructor([typeof(string)]));
        }
        finally { type.SetAttr("__module__", original); }
    }

    [Fact]
    public void RealTypeConstructorCreatesAnotherIndependentQuitter()
    {
        PyInstance created = Assert.IsType<PyInstance>(Exit.Class.Invoke(["leave", "END"], [], []));
        Assert.NotSame(Exit, created); Assert.Same(Exit.Class, created.Class);
        Assert.Equal("Use leave() or END to exit", PyRepr.Repr(created));
        Assert.Equal(0, _input.Calls);
    }

    private sealed class FakeInput : PyNativeObject
    {
        internal int Calls;
        internal Action? Action;
        public override string TypeName => "TestInput";
        public override object? GetAttr(string name) => name == "close"
            ? PyNativeFunctions.Create("close", [], [], 0, _ => { Calls++; Action?.Invoke(); return PyNone.Instance; })
            : throw Missing(name);
    }
    private sealed class TextValue(string text, List<string> seen) : PyNativeObject
    {
        public override string TypeName => "TextValue";
        public override object? GetAttr(string name) => throw Missing(name);
        public override string PythonStr() { seen.Add(text); return text; }
    }
}
