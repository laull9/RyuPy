// html.escape 的真实入口专项：绑定、逐次属性读取、迟真值、身份及精确未支持边界。
// 只锁本 API；bytes.replace/高阶 property 真值等共享旧债不改写成 CP 通过。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyHtmlTests
{
    private static object Function => PyHtml.CreateAttributes()["escape"]!;
    private static object? Call(params object?[] args) => PyOps.CallValue(Function, args, [], []);
    private static object? Invoke(object?[] args, string[] names, object?[] values) => PyOps.CallValue(Function, args, names, values);
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static PyInstance Instance(string name, params (string Name, Func<object?[], object?> Body)[] methods)
    {
        var cls = new PyClass(name);
        foreach (var method in methods) cls.SetAttr(method.Name, Method(method.Name, method.Body));
        return new PyInstance(cls);
    }

    [Fact]
    public void ExactStringsPreserveNoMatchIdentityAndUseTheFiveOrderedReplacements()
    {
        string plain = new("ordinary payload".ToCharArray());
        string quotes = new("quotes\"'".ToCharArray());
        Assert.Same(plain, Call(plain)); Assert.Same(quotes, Call(quotes, false)); Assert.Same("", Call(""));
        Assert.Equal("&amp;&lt;&gt;&quot;&#x27;", Call("&<>\"'"));
        Assert.Equal("&amp;&lt;&gt;\"'", Call("&<>\"'", false));
        Assert.Equal("&amp;amp;", Call("&amp;"));
        Assert.Equal("😀\0中文&amp;\ud800\n", Call("😀\0中文&\ud800\n"));
        Assert.Equal("'&lt;", Invoke([], ["quote", "s"], [false, "'<"]));
        int truth = 0;
        object quote = Instance("Quote", ("__bool__", _ => { truth++; return true; }));
        Assert.Same(plain, Call(plain, quote)); Assert.Equal(1, truth);
    }

    [Fact]
    public void BindingPrecedesAllBodyProtocolsAndKeepsKeywordErrorOrder()
    {
        int callbacks = 0;
        object source = Instance("Input", ("replace", _ => { callbacks++; return PyNone.Instance; }));
        object quote = Instance("Quote", ("__bool__", _ => { callbacks++; return true; }));
        var cases = new (object?[] Args, string[] Names, object?[] Values, string Message)[]
        {
            ([], [], [], "escape() missing 1 required positional argument: 's'"),
            ([], ["quote"], [quote], "escape() missing 1 required positional argument: 's'"),
            ([], ["bad"], [source], "escape() got an unexpected keyword argument 'bad'"),
            ([source, quote, 0L], [], [], "escape() takes from 1 to 2 positional arguments but 3 were given"),
            ([source, quote, 0L], ["bad"], [0L], "escape() got an unexpected keyword argument 'bad'"),
            ([source, quote, 0L], ["s"], [0L], "escape() got multiple values for argument 's'"),
            ([source, quote, 0L], ["quote"], [0L], "escape() got multiple values for argument 'quote'"),
            ([source], ["s", "bad"], [0L, 0L], "escape() got multiple values for argument 's'"),
            ([source], ["bad", "s"], [0L, 0L], "escape() got an unexpected keyword argument 'bad'"),
        };
        foreach (var item in cases)
            Assert.Equal(item.Message, Assert.Throws<PyTypeError>(() => Invoke(item.Args, item.Names, item.Values)).PyMessage);
        Assert.Equal(0, callbacks);
    }

    [Fact]
    public void EachPropertyLookupUsesThePreviousResultAndPreservesArbitraryFinalIdentity()
    {
        foreach (int count in new[] { 3, 5 })
        {
            var log = new List<string>(); var nodes = new PyInstance[count];
            var terminal = new PyList();
            for (int index = 0; index < count; index++) nodes[index] = new PyInstance(new PyClass("Node" + index));
            string[] old = ["&", "<", ">", "\"", "'"];
            string[] replacements = ["&amp;", "&lt;", "&gt;", "&quot;", "&#x27;"];
            for (int index = 0; index < count; index++)
            {
                int step = index;
                nodes[step].Class.SetAttr("replace", new PyProperty(Method("getter", args =>
                {
                    Assert.Same(nodes[step], args[0]); log.Add("get:" + step);
                    return Method("replace", arguments =>
                    {
                        Assert.Equal<object?>([old[step], replacements[step]], arguments); log.Add("call:" + step);
                        return step + 1 < count ? nodes[step + 1] : terminal;
                    });
                })));
            }
            Assert.Same(terminal, Call(nodes[0], count == 5));
            var expected = new List<string>();
            for (int index = 0; index < count; index++) { expected.Add("get:" + index); expected.Add("call:" + index); }
            Assert.Equal(expected, log);
        }
        int calls = 0; PyInstance? source = null;
        source = Instance("NoneEnd", ("replace", _ => ++calls == 3 ? PyNone.Instance : source));
        Assert.Same(PyNone.Instance, Call(source, false)); Assert.Equal(3, calls);
    }

    [Fact]
    public void QuoteRunsAfterThreeReplacementsAndCanChangeTheNextTwoLookups()
    {
        var log = new List<string>(); PyInstance? source = null;
        source = Instance("Mutable", ("replace", args => { log.Add("old:" + args[1]); return source; }));
        object quote = Instance("Quote",
            ("__bool__", _ =>
            {
                log.Add("truth");
                PyObject.SetAttr(source!, "replace", Method("new", args =>
                {
                    Assert.Equal(2, args.Length); log.Add("new:" + args[0]); return source;
                }));
                return true;
            }),
            ("__len__", _ => throw new InvalidOperationException("unused len")));
        Assert.Same(source, Call(source, quote));
        Assert.Equal<string>(["old:&", "old:<", "old:>", "truth", "new:\"", "new:'"], log);
        log.Clear(); int lengths = 0;
        object falseQuote = Instance("LengthQuote", ("__len__", _ => { lengths++; return 0L; }));
        Assert.Same(source, Call(source, falseQuote)); Assert.Equal(1, lengths);
        Assert.Equal<string>(["new:&", "new:<", "new:>"], log);
    }

    [Fact]
    public void StringSubclassesUseRealOverridesOrThePlainStringRootMethod()
    {
        var inherited = (PyClass)PyOps.MakeClass("InheritedText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(inherited);
        object value = inherited.Invoke(["plain"], [], [])!;
        object result = Call(value)!; Assert.IsType<string>(result); Assert.NotSame(value, result); Assert.Equal("plain", result);
        var overridden = (PyClass)PyOps.MakeClass("OverriddenText", [PyType.Get("str")], null); int calls = 0;
        overridden.SetAttr("replace", Method("replace", args => { calls++; return args[0]; }));
        PyOps.FinalizeOrdinaryClass(overridden); value = overridden.Invoke(["&<>"], [], [])!;
        Assert.Same(value, Call(value)); Assert.Equal(5, calls);
        overridden.SetAttr("replace", Method("replace", _ => { calls++; return "<>'"; }));
        Assert.Equal("&lt;&gt;&#x27;", Call(value)); Assert.Equal(6, calls);
    }

    [Fact]
    public void OrdinaryGetattrAndInstanceCallableAreNotMistakenForDescriptors()
    {
        int getters = 0, calls = 0, conversions = 0; PyInstance? source = null;
        source = Instance("Fallback",
            ("__getattr__", args =>
            {
                Assert.Equal("replace", args[1]); getters++;
                return Method("replace", values => { Assert.Equal(2, values.Length); calls++; return source; });
            }),
            ("__str__", _ => { conversions++; return "wrong"; }));
        Assert.Same(source, Call(source)); Assert.Equal(5, getters); Assert.Equal(5, calls); Assert.Equal(0, conversions);
        object callable = Instance("Callable", ("__call__", args => { Assert.Equal(3, args.Length); calls++; return source; }),
            ("__get__", _ => throw new InvalidOperationException("instance value must not bind")));
        PyObject.SetAttr(source, "replace", callable);
        Assert.Same(source, Call(source, false)); Assert.Equal(5, getters); Assert.Equal(8, calls);
    }

    [Fact]
    public void MissingAndNoncallableReplacementsFailAtTheActualStepBeforeQuote()
    {
        int calls = 0, truths = 0;
        object quote = Instance("Quote", ("__bool__", _ => { truths++; return true; }));
        object source = Instance("Stops", ("replace", _ => { calls++; return PyNone.Instance; }));
        Assert.Equal("'NoneType' object has no attribute 'replace'", Assert.Throws<PyAttributeError>(() => Call(source, quote)).PyMessage);
        Assert.Equal(1, calls); Assert.Equal(0, truths);
        var raw = new PyInstance(new PyClass("Raw")); PyObject.SetAttr(raw, "replace", 1L);
        Assert.Equal("'int' object is not callable", Assert.Throws<PyTypeError>(() => Call(raw, quote)).PyMessage);
        Assert.Equal(0, truths);
        // 明确记录旧协议缺失，不把本轮结果当 CP 的 bytes TypeError。
        Assert.Throws<PyAttributeError>(() => Call(new PyBytes([65])));
    }

    [Fact]
    public void BinderPreparesCallerAndAmbientWhileUserErrorsRemainTheOriginalObjects()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("html_probe.py", "caller"); PyTraceback.SetLine(23);
            PyTypeError binding = Assert.Throws<PyTypeError>(() => Call());
            Assert.Same(ambient, binding.Context); Assert.Equal(new PyStackFrame("html_probe.py", "caller", 23), Assert.Single(binding.Frames));
            var original = new PyRuntimeError("user marker"); PyErrors.PrepareRaise(original);
            object source = Instance("Raises", ("replace", _ => throw original));
            Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Call(source)));
            Assert.Same(ambient, original.Context); Assert.Equal(new PyStackFrame("html_probe.py", "caller", 23), Assert.Single(original.Frames));
            var rawError = new PyTypeError("unprepared body marker");
            source = Instance("RawRaises", ("replace", _ => throw rawError));
            Assert.Same(rawError, Assert.Throws<PyTypeError>(() => Call(source))); Assert.Null(rawError.Context); Assert.Empty(rawError.Frames);
            int replacements = 0; PyInstance? repeated = null;
            repeated = Instance("Repeated", ("replace", _ => { replacements++; return repeated; }));
            object quote = Instance("Quote", ("__bool__", _ => throw original));
            Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Call(repeated, quote))); Assert.Equal(3, replacements);
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void CustomDescriptorGuardRespectsActiveInstanceShadowingAndDataPrecedence()
    {
        int callbacks = 0; PyInstance? source = null;
        var descriptor = Instance("Descriptor",
            ("__get__", _ => { callbacks++; return PyNone.Instance; }),
            ("__call__", _ => { callbacks++; return source; }));
        source = new PyInstance(new PyClass("Receiver")); source.Class.SetAttr("replace", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Call(source)); Assert.Equal(0, callbacks);
        PyObject.SetAttr(source, "replace", Method("shadow", _ => source));
        Assert.Same(source, Call(source)); Assert.Equal(0, callbacks);
        // 无 __call__ 的非数据描述符也必须允许被实例值遮蔽。
        Assert.True(descriptor.Class.DeleteAttr("__call__"));
        Assert.Same(source, Call(source)); Assert.Equal(0, callbacks);
        descriptor.Class.SetAttr("__set__", Method("set", _ => { callbacks++; return PyNone.Instance; }));
        Assert.Throws<PyNotImplementedError>(() => Call(source)); Assert.Equal(0, callbacks);
        // 同名槽在其它实例存在不等于本实例活动；不能误跳过类 descriptor。
        Assert.True(descriptor.Class.DeleteAttr("__set__")); descriptor.Class.SetAttr("__call__", Method("call", _ => source));
        var sibling = new PyInstance(source.Class);
        Assert.Throws<PyNotImplementedError>(() => Call(sibling));
    }

    [Fact]
    public void TruthDescriptorGuardIsLateAndDoesNotInspectAnUnusedLengthSlot()
    {
        int replacements = 0, callbacks = 0; PyInstance? source = null;
        source = Instance("Source", ("replace", _ => { replacements++; return source; }));
        object descriptor = Instance("Descriptor", ("__get__", _ => { callbacks++; return PyNone.Instance; }),
            ("__call__", _ => { callbacks++; return true; }));
        var quote = new PyInstance(new PyClass("Quote")); quote.Class.SetAttr("__len__", descriptor);
        quote.Class.SetAttr("__bool__", Method("bool", _ => false));
        Assert.Same(source, Call(source, quote)); Assert.Equal(3, replacements); Assert.Equal(0, callbacks);
        quote.Class.SetAttr("__bool__", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Call(source, quote)); Assert.Equal(6, replacements); Assert.Equal(0, callbacks);
        Assert.True(quote.Class.DeleteAttr("__bool__"));
        Assert.Throws<PyNotImplementedError>(() => Call(source, quote)); Assert.Equal(9, replacements); Assert.Equal(0, callbacks);
        // property truth 是共享旧失败，不新增误成功或提前 getter；普通 replace property 在前面独立验收。
        quote.Class.SetAttr("__bool__", new PyProperty(Method("get_bool", _ => { callbacks++; return Method("bool", _ => true); })));
        Assert.Throws<PyTypeError>(() => Call(source, quote)); Assert.Equal(12, replacements); Assert.Equal(0, callbacks);
    }

    [Fact]
    public void GetattrDescriptorGuardOnlyRunsWhenTheFallbackWouldBeUsed()
    {
        int callbacks = 0; PyInstance? source = null;
        object descriptor = Instance("FallbackDescriptor", ("__get__", _ => { callbacks++; return PyNone.Instance; }),
            ("__call__", _ => { callbacks++; return PyNone.Instance; }));
        source = new PyInstance(new PyClass("Source")); source.Class.SetAttr("__getattr__", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Call(source)); Assert.Equal(0, callbacks);
        source.Class.SetAttr("replace", Method("replace", _ => source));
        Assert.Same(source, Call(source, false)); Assert.Equal(0, callbacks);
        Assert.True(source.Class.DeleteAttr("replace")); PyObject.SetAttr(source, "replace", Method("replace", _ => source));
        Assert.Same(source, Call(source, false)); Assert.Equal(0, callbacks);
    }

    [Fact]
    public void StableProtectedFunctionUsesRealModuleRegistrationAndSavedAliases()
    {
        object alias = Function; Assert.Same(alias, Function);
        Assert.Same(PyType.Get("function"), PyObject.TypeOf(alias)); Assert.Same(PyObject.TypeOf(alias), PyObject.GetAttr(alias, "__class__"));
        foreach (string name in new[] { "__name__", "__qualname__", "__module__", "__defaults__" })
            Assert.Same(PyObject.GetAttr(alias, name), PyObject.GetAttr(alias, name));
        Assert.Equal("escape", PyObject.GetAttr(alias, "__name__")); Assert.Equal("html", PyObject.GetAttr(alias, "__module__"));
        Assert.Equal(true, Assert.Single(Assert.IsType<PyTuple>(PyObject.GetAttr(alias, "__defaults__")).Items));
        foreach (string name in new[] { "__dict__", "__code__", "__globals__", "__doc__", "__get__" })
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(alias, name));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(alias, "__defaults__", new PyTuple([false])));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(alias, "__module__"));
        var cls = new PyClass("Borrower"); cls.SetAttr("escape", alias);
        Assert.Same(alias, PyObject.GetAttr(cls, "escape")); Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(new PyInstance(cls), "escape"));
        PyNativeModules.Register(); PyModule module = PyModules.Import("html"); object? original = module.GetAttr("escape");
        try
        {
            Assert.Same(alias, original); module.SetAttr("escape", 7L); Assert.Equal(7L, module.GetAttr("escape"));
            Assert.Equal("&lt;", PyOps.CallValue(alias, ["<"], [], []));
            Assert.Throws<PyAttributeError>(() => module.GetAttr("unescape")); Assert.Throws<PyAttributeError>(() => module.GetAttr("entities"));
        }
        finally { module.SetAttr("escape", original); }
    }
}
