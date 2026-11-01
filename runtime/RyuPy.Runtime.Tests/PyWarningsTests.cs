using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>警告过滤、真实异常身份、上下文恢复、调用位置和当前stderr的原生回归。</summary>
[Collection("生成器帧栈")]
public class PyWarningsTests
{
    private static object? Call(string name, object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, names ?? [], values ?? []);
    private static object? Method(object? obj, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(obj, name), args, [], []);
    private static void Capture(Action<PyList> body)
    {
        object? context = Call("catch_warnings", [], ["record"], [true]);
        var records = Assert.IsType<PyList>(Method(context, "__enter__"));
        try { body(records); }
        finally { Assert.Equal(false, Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance)); }
    }
    private static void Frame(string module, string file, Action action)
    {
        PyModules.Register(module, "", file, () => { }, _ => PyNone.Instance, (_, _, _) => { });
        long depth = PyTraceback.PushFrame(file, "caller");
        try { PyTraceback.SetLine(12); action(); }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void RecordKeepsRawArgumentsAndExistingWarningIdentity()
    {
        Capture(records =>
        {
            Call("simplefilter", ["always"]);
            Call("warn", [3L]);
            var warning = Assert.IsType<PyUserWarning>(PyObject.GetAttr(records.Items[0], "message"));
            Assert.Equal(3L, warning.Args[0]);
            var existing = new PyRuntimeWarning(["existing"]);
            Call("warn", [existing, PyType.Get("int")]);
            Assert.Same(existing, PyObject.GetAttr(records.Items[1], "message"));
            Assert.Same(PyType.Get("RuntimeWarning"), PyObject.GetAttr(records.Items[1], "category"));
            Assert.Throws<PyTypeError>(() => Call("warn", ["bad", PyType.Get("int")]));
        });
    }

    [Fact]
    public void ErrorRaisesSameWarningAndFiltersRestoreAfterNestedContext()
    {
        Capture(records =>
        {
            Call("simplefilter", ["always"]);
            var existing = new PyUserWarning(["failure"]);
            Capture(_ =>
            {
                Call("simplefilter", ["error"]);
                Assert.Same(existing, Assert.Throws<PyUserWarning>(() => Call("warn", [existing])));
            });
            Call("warn", [existing]);
            Assert.Single(records.Items);
        });
    }

    [Fact]
    public void DefaultModuleOnceAndFilterInvalidationHaveDifferentKeys()
    {
        Frame("warning_keys", "warning_keys_unique.py", () => Capture(records =>
        {
            Call("simplefilter", ["default"]);
            Call("warn", ["same"]); Call("warn", ["same"]);
            Assert.Single(records.Items);
            PyTraceback.SetLine(13); Call("warn", ["same"]);
            Assert.Equal(2, records.Count);
            Call("simplefilter", ["module"]);
            Call("warn", ["same"]); PyTraceback.SetLine(14); Call("warn", ["same"]);
            Assert.Equal(3, records.Count);
            Call("simplefilter", ["once"]);
            Call("warn", ["same"]);
            Frame("warning_other", "warning_other_unique.py", () => Call("warn", ["same"]));
            Assert.Equal(5, records.Count);
            Call("simplefilter", ["ignore"]); Call("warn", ["ignored"]);
            Assert.Equal(5, records.Count);
        }));
    }

    [Fact]
    public void DefaultDeprecationAndStacklevelUseActualRegisteredModule()
    {
        Frame("warning_imported", "warning_imported_unique.py", () => Capture(records =>
        {
            Call("warn", ["hidden", PyType.Get("DeprecationWarning")]);
            Assert.Empty(records.Items);
            Call("simplefilter", ["always"]);
            Frame("warning_nested", "warning_nested_unique.py", () => Call("warn", ["caller", PyNone.Instance, 2L]));
            Assert.Equal("warning_imported_unique.py", PyObject.GetAttr(records.Items[0], "filename"));
            Assert.Equal(12L, PyObject.GetAttr(records.Items[0], "lineno"));
            Call("warn", ["outside", PyNone.Instance, 1000000L]);
            Assert.Equal("sys", PyObject.GetAttr(records.Items[1], "filename"));
            Assert.Equal(1L, PyObject.GetAttr(records.Items[1], "lineno"));
        }));
    }

    [Fact]
    public void NativeWarningConstructionDoesNotStringifyItsArgument()
    {
        var arg = new CountingText();
        var warning = new PyUserWarning([arg]);
        Assert.Equal(0, arg.Calls);
        Assert.Equal("text", warning.PyMessage);
        Assert.Equal(1, arg.Calls);
        Assert.Same(arg, warning.Args[0]);
    }

    [Fact]
    public void FirstClassWarningFactoryPreservesManyArgumentsWithoutStringification()
    {
        var text = new CountingText();
        object?[] args = [text, 3L];
        var warning = Assert.IsType<PyUserWarning>(PyOps.CallValue(PyType.Get("UserWarning"), args, [], []));
        Assert.Equal(0, text.Calls);
        Assert.Same(text, warning.Args[0]);
        Assert.Equal(3L, warning.Args[1]);
        args[1] = 4L;
        Assert.Equal(3L, warning.Args[1]);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyType.Get("UserWarning"), [], ["message"], ["bad"]));
    }

    [Fact]
    public void FilteringKeepsRawMessageWithoutEagerStringification()
    {
        Capture(records =>
        {
            Call("simplefilter", ["always"]);
            var text = new CountingText();
            Call("warn", [text]);
            Assert.Equal(0, text.Calls);
            Call("warn", [new PyList()]);
            Assert.Equal(2, records.Count);
            Call("simplefilter", ["default"]);
            Assert.Throws<PyTypeError>(() => Call("warn", [new PyList()]));
            Call("warn", [3L]);
            Call("warn", [3.0]);
            Assert.Equal(3, records.Count);
        });
    }

    [Fact]
    public void ContextCannotBeEnteredTwiceAndSupportsActionKeyword()
    {
        object? context = Call("catch_warnings", [], ["record", "action"], [true, "always"]);
        var records = Assert.IsType<PyList>(Method(context, "__enter__"));
        try
        {
            Call("warn", ["x"]);
            Assert.Single(records.Items);
            Assert.Throws<PyRuntimeError>(() => Method(context, "__enter__"));
        }
        finally { Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); }
    }

    [Fact]
    public void FilterAppendAndUnsupportedScopeRemainExplicit()
    {
        Capture(records =>
        {
            Call("simplefilter", ["ignore"]);
            Call("simplefilter", ["always"], ["append"], [true]);
            Call("warn", ["hidden"]);
            Assert.Empty(records.Items);
            Assert.Throws<PyNotImplementedError>(() => Call("warn", ["source"], ["source"], [new PyList()]));
            Assert.Throws<PyNotImplementedError>(() => Call("warn", ["skip"], ["skip_file_prefixes"], [new PyTuple(["pkg/"])]));
            Assert.Throws<PyNotImplementedError>(() => PyWarnings.BeforeSet("showwarning", PyNone.Instance));
            Assert.Throws<PyAssertionError>(() => Call("simplefilter", ["invalid"]));
            Call("simplefilter", ["ignore"], ["lineno"], [System.Numerics.BigInteger.One << 100]);
            Assert.Throws<PyAssertionError>(() => Call("simplefilter", ["ignore"], ["lineno"], [-(System.Numerics.BigInteger.One << 100)]));
        });
    }

    [Fact]
    public void CurrentStderrIsUsedAndNonIoWriteErrorsPropagate()
    {
        PySystem.Initialize("warning_tests.py", []);
        var sys = PyModules.Import("sys");
        object? previous = sys.GetAttr("stderr");
        var output = new Output();
        object? context = Call("catch_warnings", [], ["action"], ["always"]);
        Method(context, "__enter__");
        try
        {
            sys.SetAttr("stderr", output);
            var text = new CountingText();
            Call("warn", [text]);
            Assert.Equal(1, text.Calls);
            Assert.Contains("UserWarning: text", output.Text);
            output.Fail = true;
            Assert.Throws<PyValueError>(() => Call("warn", ["write failure"]));
            sys.SetAttr("stderr", PyNone.Instance);
            Call("warn", [text]);
            Assert.Equal(1, text.Calls);
        }
        finally
        {
            sys.SetAttr("stderr", previous);
            Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance);
        }
    }

    private sealed class Output : PyNativeObject
    {
        internal string Text = "";
        internal bool Fail;
        public override string TypeName => "Output";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, args =>
            {
                if (Fail) throw new PyValueError("write failed");
                Text += (string)args[0]!;
                return (long)((string)args[0]!).Length;
            }) : throw Missing(name);
    }

    private sealed class CountingText : PyNativeObject
    {
        internal int Calls;
        public override string TypeName => "CountingText";
        public override object? GetAttr(string name) => throw Missing(name);
        public override string PythonStr() { Calls++; return "text"; }
    }
}
