// 来源冷协议的具名白盒回归：手工载体只验证Runtime ABI，不能代替真实编译产物的MIR/PE门。
// globals仅使用精确str元数据键；不把此有限输入域外推为任意PyDict查找无用户回调。
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PySourceProvenanceTests
{
    private const string File = "source_protocol_unique.py";
    private static object? Call(string name, object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, names ?? [], values ?? []);
    private static object? Method(object? obj, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(obj, name), args, [], []);
    private static PyTypedDictType TypedDict() => Assert.IsType<PyTypedDictType>(
        PyOps.CallValue(PyTypedDict.Factory, ["Record", new PyDict()], [], []));
    private static object? Module() => TypedDict().GetAttr("__module__");
    private static PySignature Signature(object source, bool lexical = true)
    {
        var signature = PySignature.Create("body", [], [], []);
        PyOps.FunctionDefinitionWithSource(signature, lexical ? "body" : null, lexical ? 2 : 0, source);
        return signature;
    }
    private static PyFunction Function(PySignature signature, object? module)
    {
        var function = new PyFunction("body", signature, (_, _, _, _) => PyNone.Instance, []);
        PyOps.FunctionModule(function, module);
        return function;
    }
    private static (object Source, PyDict Globals) Legacy(string name = "entry", string file = File)
    {
        var globals = new PyDict(); globals.Set("__name__", name);
        object source = PyTraceback.CreateCodeSource(name, file, key => globals.TryGet(key, out var value)
            ? value : throw new PyAttributeError(key));
        return (source, globals);
    }
    private static PyModuleExecutionContext Context(object? name)
    {
        // 没有伪造实际编译单元；Execute(X,targetY)由独立真实宿主/PE用例覆盖。
        var state = new PyModuleInstanceState(null); state.Globals.Set("__name__", name);
        return new PyModuleExecutionContext(state, new PyDict());
    }
    private static void Frame(object? origin, Action action, string file = File, int line = 12)
    {
        long depth = PyTraceback.PushFrameWithOrigin(file, "caller", origin);
        try { PyTraceback.SetLine(line); action(); }
        finally { PyTraceback.PopTo(depth); }
    }
    private static void Capture(Action<PyList> action)
    {
        object? context = Call("catch_warnings", [], ["record"], [true]);
        var records = Assert.IsType<PyList>(Method(context, "__enter__"));
        try { action(records); }
        finally { Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); }
    }

    [Fact]
    public void NewAbiColdRegistrationPreservesOldAbiAndStaticCapabilities()
    {
        int reads = 0;
        object source = PyTraceback.CreateCodeSource("entry", File, _ => { reads++; return "live"; });
        var signature = Signature(source, false);
        object marker = PyTraceback.CreateModuleExecutionSourceWithCode(source, null);
        Assert.Equal(0, reads);
        Assert.Same(source, PyFunctionDefinition.Source(signature));
        Assert.Throws<PyRuntimeError>(() => PyOps.FunctionDefinitionWithSource(signature, null, 0, source));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(Function(signature, "x"), "__qualname__"));
        Frame(marker, () => { Assert.Equal("entry", Module()); Assert.Equal("entry", PyTraceback.GetCallerModule()); });
        Assert.Equal(0, reads);
        object raw = new PyList([1L]);
        Frame(PyTraceback.CreateModuleExecutionSource(raw), () =>
        {
            Assert.Same(raw, PyTraceback.GetCallerModule()); Assert.False(PyTraceback.CallerSource(1).Known);
        });
        var old = PySignature.Create("old", [], [], []); PyOps.FunctionDefinition(old, "old", 1);
        Frame(Function(old, raw), () =>
        {
            Assert.Same(raw, PyTraceback.GetCallerModule()); Assert.False(PyTraceback.CallerSource(1).Known);
        });
        Frame(signature, () => Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule()));
    }

    [Fact]
    public void NewCarriersRejectMissingOrInconsistentSources()
    {
        var (source, _) = Legacy();
        Frame(PySignature.Create("bare", [], [], []), () => Assert.Throws<PyRuntimeError>(() => Module()));
        Frame(Signature(source), () => Assert.Throws<PyRuntimeError>(() => Module()), "wrong_file.py");
        object instanceSource = PyTraceback.CreateCodeSource("x", File, null);
        Frame(Signature(instanceSource), () => Assert.Throws<PyRuntimeError>(() => Module()));
        Frame(Function(Signature(instanceSource), "x"), () => Assert.Throws<PyNotImplementedError>(() => Module()));
        var context = Context("y");
        var wrongOwner = PyModuleInstances.CreateOwnedFunction(context, "bad", Signature(source),
            (_, _, _, _, _) => PyNone.Instance, [], false, null);
        Frame(wrongOwner, () => Assert.Throws<PyRuntimeError>(() => Module()));
        Assert.Throws<PyRuntimeError>(() => PyTraceback.CreateModuleExecutionSourceWithCode(source, context));
        Assert.Throws<PyRuntimeError>(() => PyTraceback.CreateModuleExecutionSourceWithCode(instanceSource, null));
        Assert.Throws<PyRuntimeError>(() => PyOps.FunctionDefinitionWithSource(
            PySignature.Create("bad", [], [], []), null, 0, new object()));
    }

    [Fact]
    public void ModuleEntryAndActualFunctionsKeepSeparateMetadataAxes()
    {
        var (source, globals) = Legacy();
        var function = Function(Signature(source), "definition");
        globals.Set("__name__", "later");
        Frame(function, () =>
        {
            Assert.Equal("definition", Module());
            PyObject.SetAttr(function, "__module__", "reflection"); Assert.Equal("reflection", Module());
            Assert.Equal("reflection", PyTraceback.GetCallerModule());
        });
        object instanceSource = PyTraceback.CreateCodeSource("code_x", File, null);
        var context = Context("target_entry");
        object marker = PyTraceback.CreateModuleExecutionSourceWithCode(instanceSource, context);
        context.State.Globals.Set("__name__", "target_later");
        Frame(marker, () =>
        {
            Assert.Equal("target_entry", Module()); Assert.Equal("target_later", PyTraceback.GetCallerModule());
        });
        var owned = PyModuleInstances.CreateOwnedFunction(context, "owned", Signature(instanceSource),
            (_, _, _, _, _) => PyNone.Instance, [], false, null);
        PyOps.FunctionModule(owned, "owned_definition");
        Frame(owned, () => Assert.Equal("owned_definition", Module()));
    }

    [Fact]
    public void StaticTypedDictRetainsLegacyConsumer()
    {
        const string file = "static_td_protocol_unique.py";
        PyModules.Register("static_td_legacy_registration", "", file, () => { }, _ => PyNone.Instance, (_, _, _) => { });
        var (source, _) = Legacy("not_the_registered_name", file);
        Frame(Signature(source, false), () => Assert.Equal("static_td_legacy_registration", Module()), file);
        // 只保旧消费算法；不以source初名伪造实际可写function.Module。
    }

    [Fact]
    public void TypedDictKeepsRawIdentityAndGuardsBothDisplayPaths()
    {
        var (source, _) = Legacy(); var function = Function(Signature(source), null);
        Frame(function, () =>
        {
            foreach (object raw in new object[] { 7L, new PyList([1L]) })
            {
                PyObject.SetAttr(function, "__module__", raw); var typed = TypedDict();
                Assert.Same(raw, typed.GetAttr("__module__"));
                Assert.Throws<PyNotImplementedError>(() => PyRepr.Repr(typed));
                Assert.Throws<PyNotImplementedError>(() => PyTyping.Display(typed));
            }
            foreach (object raw in new object[] { PyNone.Instance, "", 0L, new PyList() })
            { PyObject.SetAttr(function, "__module__", raw); Assert.Equal("__main__", Module()); }
            PyObject.DelAttr(function, "__module__"); Assert.Equal("__main__", Module());
        });
    }

    [Fact]
    public void TypedDictAttributeErrorUsesPostCallbackEnvironmentWithoutAnotherTruthTest()
    {
        var (source, globals) = Legacy(); var function = Function(Signature(source), null);
        var child = new PyClass("AttributeChild") { BuiltinBase = "AttributeError" };
        Frame(function, () =>
        {
            foreach (object fallback in new object[] { "", 0L, new PyList(), new BoolProbe(() => throw new PyRuntimeError("second bool")) })
            {
                var probe = new BoolProbe(() =>
                {
                    globals.Set("__name__", fallback);
                    var error = new PyRuntimeError("python subclass"); error.BindPythonClass(child); throw error;
                });
                PyObject.SetAttr(function, "__module__", probe);
                Assert.Same(fallback, Module()); Assert.Equal(1, probe.Calls);
                if (fallback is BoolProbe untouched) Assert.Equal(0, untouched.Calls);
            }
            var missing = new BoolProbe(() => { globals.Delete("__name__"); throw new PyAttributeError("missing"); });
            PyObject.SetAttr(function, "__module__", missing); Assert.Equal("__main__", Module());
            Assert.Equal(1, missing.Calls);
        });
    }

    [Fact]
    public void TypedDictFallbackNoneIsExplicitPendingMetaclassBoundary()
    {
        // 此guard须与旧引擎/CP/新候选同组验收；若发现旧正确路径被拒绝，不得以单测绿放行。
        object source = PyTraceback.CreateCodeSource("x", File, null); var context = Context("entry");
        var probe = new BoolProbe(() => { context.State.Globals.Set("__name__", PyNone.Instance); throw new PyAttributeError("fallback"); });
        var function = PyModuleInstances.CreateOwnedFunction(context, "owned", Signature(source),
            (_, _, _, _, _) => PyNone.Instance, [], false, null);
        PyOps.FunctionModule(function, probe);
        Frame(function, () => Assert.Throws<PyNotImplementedError>(() => Module()));
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public void TypedDictNativeFailurePreservesFramesAndRestoresStack()
    {
        var (source, _) = Legacy(); var function = Function(Signature(source), null);
        var parentError = new PyRuntimeError("outer handler"); object? token = PyErrors.EnterHandler(parentError);
        try
        {
            Frame(function, () =>
            {
                long depth = PyTraceback.CaptureDepth();
                var native = new BoolProbe(() =>
                {
                    // 模拟真实编译回调的未附帧原生throw；不在测试代理提前Attach或Pop。
                    PyTraceback.PushFrame(File, "__bool__"); PyTraceback.SetLine(40);
                    return PyInt.ParseDecimal("not-an-integer");
                });
                PyObject.SetAttr(function, "__module__", native);
                var failure = Assert.Throws<PyValueError>(() => Module());
                Assert.Contains(failure.Frames, frame => frame.File == File && frame.Function == "__bool__" && frame.Line == 40);
                Assert.Contains(failure.Frames, frame => frame.File == File && frame.Function == "caller");
                Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(parentError, PyErrors.Current);
                Assert.Null(failure.Context); // Attach不能偷偷执行Prepare改挂异常上下文。
                var original = new PyRuntimeError("propagate");
                PyObject.SetAttr(function, "__module__", new BoolProbe(() => throw original));
                Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Module()));
                Assert.Equal(depth, PyTraceback.CaptureDepth());
                PyObject.SetAttr(function, "__module__", "recovered"); Assert.Equal("recovered", Module());
            });
        }
        finally { PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void KnownWarningNormalizesLiveNameBeforeConstructionOrStringification()
    {
        var (source, globals) = Legacy("__main__"); var marker = PyTraceback.CreateModuleExecutionSourceWithCode(source, null);
        Frame(marker, () => Capture(records =>
        {
            int constructions = 0;
            var category = new PyClass("ConstructedWarning")
            {
                BuiltinBase = "DeprecationWarning",
                ExceptionFactory = (_, args) => { constructions++; globals.Set("__name__", "after_constructor"); return new PyDeprecationWarning(args); },
            };
            Call("warn", ["construction", category]); Assert.Single(records.Items); Assert.Equal(1, constructions);
            globals.Set("__name__", "__main__");
            var existing = new ObservedWarning(() => globals.Set("__name__", "after_str"));
            Call("warn", [existing]); Assert.Equal(2, records.Count); Assert.Equal(1, existing.Calls);
            globals.Set("__name__", PyNone.Instance);
            Call("warn", ["none", category]); Call("warn", [existing]);
            Assert.Equal(1, constructions); Assert.Equal(1, existing.Calls); Assert.Equal(2, records.Count);
            Assert.Throws<PyTypeError>(() => Call("warn", ["bad category", PyType.Get("int")]));
            // 精确str/str子类payload参与默认Deprecation过滤；其它对象不调用bool/str。
            globals.Set("__name__", new PyStringInstance(new PyClass("NameSubclass"), "__main__"));
            Call("warn", ["subclass", PyType.Get("DeprecationWarning")]); Assert.Equal(3, records.Count);
            var invalidName = new BoolProbe(() => throw new PyRuntimeError("name bool"));
            globals.Set("__name__", invalidName); Call("warn", ["non-str", PyType.Get("DeprecationWarning")]);
            globals.Delete("__name__"); Call("warn", ["missing", PyType.Get("DeprecationWarning")]);
            Assert.Equal(0, invalidName.Calls); Assert.Equal(3, records.Count);
            Call("simplefilter", ["always"]);
            Call("warn", ["missing-visible"]); globals.Set("__name__", 7L); Call("warn", ["non-str-visible"]);
            Assert.Equal(5, records.Count);
        }));
    }

    [Fact]
    public void KnownWarningSelectsLocationAndNameFromTheSameStacklevel()
    {
        var (outer, _) = Legacy("__main__", "outer_source.py"); var (inner, _) = Legacy("hidden", "inner_source.py");
        Frame(Signature(outer), () => Frame(Function(Signature(inner), "not_globals"), () => Capture(records =>
        {
            Call("warn", ["outer", PyType.Get("DeprecationWarning"), 2L]); Assert.Single(records.Items);
            Assert.Equal("outer_source.py", PyObject.GetAttr(records.Items[0], "filename"));
            Assert.Equal(21L, PyObject.GetAttr(records.Items[0], "lineno"));
            Call("warn", ["inner", PyType.Get("DeprecationWarning")]); Assert.Single(records.Items);
            Call("simplefilter", ["always"]); Call("warn", ["outside", PyNone.Instance, 1000000L]);
            Assert.Equal("sys", PyObject.GetAttr(records.Items[1], "filename"));
            Assert.Equal(1L, PyObject.GetAttr(records.Items[1], "lineno"));
        }), "inner_source.py", 31), "outer_source.py", 21);
    }

    [Fact]
    public void KnownWarningRawMessagesKeepAlwaysIgnoreAndErrorProtocol()
    {
        var (source, _) = Legacy(); Frame(Signature(source), () => Capture(records =>
        {
            var text = new CountingText(); var unhashable = new PyList();
            Call("simplefilter", ["always"]); Call("warn", [text]); Call("warn", [unhashable]);
            Assert.Equal(0, text.Calls); Assert.Equal(2, records.Count);
            Assert.Same(unhashable, Assert.IsType<PyUserWarning>(PyObject.GetAttr(records.Items[1], "message")).Args[0]);
            Call("simplefilter", ["ignore"]); Call("warn", [unhashable]); Assert.Equal(2, records.Count);
            Call("simplefilter", ["error"]);
            var error = Assert.Throws<PyUserWarning>(() => Call("warn", [unhashable])); Assert.Same(unhashable, error.Args[0]);
            var existing = new PyUserWarning(["identity"]);
            Assert.Same(existing, Assert.Throws<PyUserWarning>(() => Call("warn", [existing])));
            Assert.Equal(0, text.Calls);
        }));
    }

    [Fact]
    public void KnownRegistryUsesEnvironmentIdentityAndSharedModuleOnceKeys()
    {
        var (source, globals) = Legacy(); object marker = PyTraceback.CreateModuleExecutionSourceWithCode(source, null);
        var signature = Signature(source); var function = Function(signature, "unrelated");
        Capture(records =>
        {
            Call("simplefilter", ["module"]); Call("simplefilter", ["once"], ["lineno"], [13L]);
            Frame(marker, () => Call("warn", ["mixed"]));
            Frame(signature, () => Call("warn", ["mixed"]), line: 13);
            globals.Set("__name__", "renamed"); Frame(function, () => Call("warn", ["mixed"]));
            Assert.Single(records.Items);
            object x = PyTraceback.CreateCodeSource("x", File, null), y = PyTraceback.CreateCodeSource("y", File, null);
            var a = Context("same_name"); var b = Context("same_name");
            Frame(PyTraceback.CreateModuleExecutionSourceWithCode(x, a), () => Call("warn", ["mixed"]));
            Frame(PyTraceback.CreateModuleExecutionSourceWithCode(y, a), () => Call("warn", ["mixed"]), line: 13);
            Frame(PyTraceback.CreateModuleExecutionSourceWithCode(x, b), () => Call("warn", ["mixed"]));
            Assert.Equal(3, records.Count);
            Call("simplefilter", ["module"]); Frame(signature, () => Call("warn", ["mixed"]));
            Assert.Equal(4, records.Count);
        });
        Capture(records =>
        {
            Call("simplefilter", ["default"]); Frame(marker, () => Call("warn", ["default"]));
            globals.Set("__name__", "again"); Frame(signature, () => Call("warn", ["default"]));
            Frame(function, () => Call("warn", ["default"]), line: 13); Assert.Equal(2, records.Count);
        });
    }

    [Fact]
    public void KnownRegistryDoesNotRootInstanceGlobals()
    {
        Capture(records =>
        {
            Call("simplefilter", ["default"]); WeakReference weak = EmitCollectableEnvironment();
            Assert.Single(records.Items); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Assert.False(weak.IsAlive);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference EmitCollectableEnvironment()
    {
        object source = PyTraceback.CreateCodeSource("collectable", File, null); var context = Context("collectable");
        object marker = PyTraceback.CreateModuleExecutionSourceWithCode(source, context);
        Frame(marker, () => Call("warn", ["ordinary text"])); return new WeakReference(context.State.Globals);
    }
    private sealed class BoolProbe(Func<object?> body) : PyNativeObject
    {
        internal int Calls;
        public override string TypeName => "BoolProbe";
        public override object? GetAttr(string name) => name == "__bool__"
            ? PyNativeFunctions.Create("__bool__", [], [], 0, _ => { Calls++; return body(); }) : throw Missing(name);
    }
    private sealed class CountingText : PyNativeObject
    {
        internal int Calls;
        public override string TypeName => "CountingText";
        public override object? GetAttr(string name) => throw Missing(name);
        public override string PythonStr() { Calls++; return "text"; }
    }
    private sealed class ObservedWarning(Action action) : PyDeprecationWarning(["existing"])
    {
        internal int Calls;
        public override string PyMessage { get { Calls++; action(); return "existing"; } }
    }
}
