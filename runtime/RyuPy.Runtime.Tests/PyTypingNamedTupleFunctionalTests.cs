// C-functional真实公共Root入口的两遍工厂合同；不冒源码或项目已通过。
// 临时模块槽/来源均finally恢复；测试不执行CP或依赖占位factory。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingNamedTupleFunctionalTests
{
    private static readonly Type Core = typeof(PyClass).Assembly.GetType("RyuPy.Runtime.PyTypingNamedTuple")!;
    private static PyFunction Root => (PyFunction)Core.GetProperty("Root", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
    private static Dictionary<string, object?> Typing => PyTyping.CreateAttributes();
    private static PyTuple Pair(object? name, object? annotation) => new([name, annotation]);
    private static object? Get(object value, string name) => PyObject.GetAttr(value, name);
    private static PyClass Invoke(object?[] args, string[]? names = null, object?[]? values = null) =>
        (PyClass)Root.Invoke(args, names ?? [], values ?? [])!;
    private static PyClass Make(string name, object? fields) => Invoke([name, fields]);
    private static PyDict Annotations(PyClass cls) => (PyDict)Get(cls, "__annotations__")!;
    private static void WithRoot(Action body)
    {
        PyNativeModules.Register();
        bool had = Typing.TryGetValue("NamedTuple", out object? old);
        Typing["NamedTuple"] = Root;
        try { body(); }
        finally { if (had) Typing["NamedTuple"] = old; else Typing.Remove("NamedTuple"); }
    }
    private static void WithCaller(object? module, Action<PyFunction> body)
    {
        PyFunction caller = PyNativeFunctions.Create("caller", [], [], 0, _ => PyNone.Instance);
        PyOps.FunctionModule(caller, module);
        long depth = PyTraceback.PushFrameWithOrigin("namedtuple_functional.py", "caller", caller);
        PyTraceback.SetLine(19);
        try { body(caller); }
        finally { PyTraceback.PopTo(depth); }
    }
    private static void Supported(Action body) => WithRoot(() => WithCaller("caller_module", _ => body()));
    private static void WithTyping(string name, object? value, Action body)
    {
        bool had = Typing.TryGetValue(name, out object? old);
        Typing[name] = value;
        try { body(); }
        finally { if (had) Typing[name] = old; else Typing.Remove(name); }
    }

    [Fact]
    public void SignatureHandlesOmittedNoneAndPositionOnlyNamesAsFields()
    {
        Supported(() =>
        {
            Assert.Equal(0, ((PyTuple)Get(Invoke(["Empty"]), "_fields")!).Length);
            Assert.Equal(0, ((PyTuple)Get(Make("EmptyNone", PyNone.Instance), "_fields")!).Length);
            PyClass cls = Invoke(["Keywords"], ["typename", "fields"], [PyType.Get("int"), PyType.Get("str")]);
            Assert.Equal(new object?[] { "typename", "fields" }, ((PyTuple)Get(cls, "_fields")!).Items);
            Assert.Equal(new object?[] { 3L, "a" }, Assert.IsAssignableFrom<PyTuple>(cls.Invoke([3L, "a"], [], [])).Items);
            PyClass namedOptions = Invoke(["Options", PyNone.Instance], ["defaults", "module", "rename"], [PyType.Get("int"), PyType.Get("int"), PyType.Get("int")]);
            Assert.Equal(new object?[] { "defaults", "module", "rename" }, ((PyTuple)Get(namedOptions, "_fields")!).Items);
            Assert.Throws<PyTypeError>(() => Invoke([]));
            Assert.Throws<PyTypeError>(() => Invoke(["X", new PyList(), 1L]));
        });
    }

    [Fact]
    public void MixedArgumentsFailBeforeCallerAndFieldsProtocols()
    {
        WithRoot(() =>
        {
            var fields = new TraceSource(_ => [Pair("x", PyType.Get("int"))]);
            WithTyping("_caller", PyNone.Instance, () =>
                Assert.Equal("Either list of fields or keywords can be provided to NamedTuple, not both",
                    Assert.Throws<PyTypeError>(() => Invoke(["Mixed", fields], ["x"], [PyType.Get("int")])).PyMessage));
            Assert.Equal(0, fields.Iterations);
        });
    }

    [Fact]
    public void ReiterableAndSingleUseFieldsReallyHaveDifferentSecondPasses()
    {
        Supported(() =>
        {
            var pairs = new PyList([Pair("x", PyType.Get("int")), Pair("y", PyType.Get("str"))]);
            PyClass full = Make("Full", pairs), once = Make("Once", PyIter.Create(pairs));
            Assert.Equal(new object?[] { "x", "y" }, ((PyTuple)Get(full, "_fields")!).Items);
            Assert.Equal(((PyTuple)Get(full, "_fields")!).Items, ((PyTuple)Get(once, "_fields")!).Items);
            Assert.Equal(2, Annotations(full).Count);
            Assert.Equal(0, Annotations(once).Count);
            Assert.Equal(PyType.Get("int"), Annotations(full).Get("x"));
            Assert.Equal(new object?[] { 1L, 2L }, Assert.IsAssignableFrom<PyTuple>(once.Invoke([1L, 2L], [], [])).Items);
        });
    }

    [Fact]
    public void SecondPassReadsItsOwnNamesAndDoesNotAskForLengthHints()
    {
        Supported(() =>
        {
            var source = new TraceSource(pass => [Pair(pass == 1 ? "first" : "second", PyType.Get("int"))]);
            PyClass cls = Make("Changed", source);
            Assert.Equal(2, source.Iterations);
            Assert.Equal(0, source.HintRequests);
            Assert.Equal(new object?[] { "first" }, ((PyTuple)Get(cls, "_fields")!).Items);
            Assert.False(Annotations(cls).TryGet("first", out _));
            Assert.Same(PyType.Get("int"), Annotations(cls).Get("second"));
        });
    }

    [Fact]
    public void CallerModuleIsCapturedBeforeEitherFieldsIteration()
    {
        WithRoot(() => WithCaller("before", caller =>
        {
            var source = new TraceSource(_ => [Pair("x", PyType.Get("int"))], _ => PyOps.FunctionModule(caller, "after"));
            PyClass cls = Make("Snapshot", source);
            Assert.Equal("before", Get(cls, "__module__"));
            Assert.Equal("after", Get(caller, "__module__"));
            Assert.Equal(2, source.Iterations);
        }));
        foreach (object? module in new object?[] { "", PyNone.Instance, null })
            WithRoot(() => WithCaller(module, _ => Assert.Equal("__main__", Get(Make("Fallback", new PyList()), "__module__"))));
    }

    [Fact]
    public void ModuleTruthRunsBeforeFieldsAndPreservesOriginalValueOrError()
    {
        var trace = new List<string>();
        var module = new TruthModule(() => { trace.Add("bool"); return true; });
        WithRoot(() => WithCaller(module, _ =>
        {
            var source = new TraceSource(_ => [Pair("x", PyType.Get("int"))], pass => trace.Add("iter" + pass));
            Assert.Same(module, Get(Make("Truth", source), "__module__"));
            Assert.Equal(new[] { "bool", "iter1", "iter2" }, trace);
        }));
        var error = new PyValueError("module truth");
        var broken = new TruthModule(() => throw error);
        WithRoot(() => WithCaller(broken, _ =>
        {
            var source = new TraceSource(_ => []);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Make("BadTruth", source)));
            Assert.Equal(0, source.Iterations);
        }));
    }

    [Fact]
    public void PairErrorsInFirstPassPrecedeAnnotationAndTypenameChecks()
    {
        Supported(() =>
        {
            object?[] pairs = [Pair("x", "f()"), new PyTuple(["y", PyType.Get("int"), 1L])];
            Assert.Equal("too many values to unpack (expected 2)", Assert.Throws<PyValueError>(() => Invoke([42L, new PyList(pairs)])).PyMessage);
            Assert.Equal("not enough values to unpack (expected 2, got 1)", Assert.Throws<PyValueError>(() => Make("Short", new PyList([new PyList(["x"])]))).PyMessage);
            Assert.Equal("not enough values to unpack (expected 2, got 1)", Assert.Throws<PyValueError>(() => Make("Characters", "xy")).PyMessage);
        });
    }

    [Fact]
    public void ExactStringPairsAndConcreteBuiltinPairIteratorsUseRealUnpacking()
    {
        Supported(() =>
        {
            PyClass text = Make("TextPair", new PyList(["xy"]));
            Assert.Equal(new object?[] { "x" }, ((PyTuple)Get(text, "_fields")!).Items);
            Assert.Equal("y", Get(Annotations(text).Get("x")!, "__forward_arg__"));
            var pairIterator = PyIter.Create(Pair("x", PyType.Get("int")));
            PyClass once = Make("PairIterator", PyIter.Create(new PyList([pairIterator])));
            Assert.Equal(new object?[] { "x" }, ((PyTuple)Get(once, "_fields")!).Items);
            Assert.Equal(0, Annotations(once).Count);
            // set拆出独立迭代器后仍属既有无回调pair域；不假定集合具体遍历顺序。
            var setPair = new PySet(["x", "y"]);
            PyClass setOnce = Make("SetPairIterator", PyIter.Create(new PyList([PyIter.Create(setPair)])));
            object? setField = Assert.Single(((PyTuple)Get(setOnce, "_fields")!).Items);
            Assert.True(setPair.Contains(setField));
            Assert.Equal(0, Annotations(setOnce).Count);
            var unknown = new CallbackIterator(["x", PyType.Get("int")]);
            Assert.Throws<PyNotImplementedError>(() => Make("UnknownPair", new PyList([unknown])));
            Assert.Equal(0, unknown.Moves);
            Assert.Throws<PyNotImplementedError>(() => Make("ScalarPair", new PyList([1L])));
        });
    }

    [Fact]
    public void MetadataUsesSharedFreshAnnotationsAndRealEmptyDefaults()
    {
        Supported(() =>
        {
            var pairs = new PyList([Pair("none", PyNone.Instance), Pair("a", "Missing"), Pair("b", "Missing")]);
            PyClass cls = Make("Refs", pairs), other = Make("Refs", pairs);
            Assert.NotSame(cls, other);
            Assert.Same(cls, PyObject.TypeOf(cls.Invoke([1L, 2L, 3L], [], [])));
            Assert.Same(Annotations(cls), Get(Get(cls, "__new__")!, "__annotations__"));
            Assert.Equal(0, ((PyTuple)Get(Get(cls, "__new__")!, "__defaults__")!).Length);
            Assert.Same(PyType.Get("NoneType"), Annotations(cls).Get("none"));
            object a = Annotations(cls).Get("a")!, b = Annotations(cls).Get("b")!;
            Assert.NotSame(a, b); Assert.True(PyHash.Equal(a, b));
            Annotations(cls).Set("extra", PyType.Get("str"));
            Assert.Equal(3, ((PyTuple)Get(cls, "_fields")!).Length);
            Assert.Equal(3, Assert.IsAssignableFrom<PyTuple>(cls.Invoke([1L, 2L, 3L], [], [])).Length);
        });
    }

    [Fact]
    public void FactoryLookupFollowsNormalizationAndPrecedesRawNameConversion()
    {
        Supported(() =>
        {
            PyModule collections = PyModules.Import("collections");
            object? original = collections.GetAttr("namedtuple");
            collections.SetAttr("namedtuple", PyNone.Instance);
            try
            {
                Assert.Throws<PyTypeError>(() => Make("Special", new PyList([Pair("x", Typing["Union"])])));
                Assert.Contains("replaced collections.namedtuple", Assert.Throws<PyNotImplementedError>(() => Make("Guard", new PyList([Pair("x", PyType.Get("int"))]))).PyMessage);
                Assert.Contains("replaced collections.namedtuple", Assert.Throws<PyNotImplementedError>(() => Make("OnceRaw", PyIter.Create(new PyList([Pair(1L, PyType.Get("int"))])))).PyMessage);
                Assert.Contains("annotation keys", Assert.Throws<PyNotImplementedError>(() => Make("TwiceRaw", new PyList([Pair(1L, PyType.Get("int"))]))).PyMessage);
            }
            finally { collections.SetAttr("namedtuple", original); }
            Assert.Equal(0, ((PyTuple)Get(Make("Restored", new PyList()), "_fields")!).Length);
        });
    }

    [Fact]
    public void ExactNameValidationAndWithinClassStringIdentityAreReused()
    {
        Supported(() =>
        {
            Assert.Equal("Encountered duplicate field name: 'x'", Assert.Throws<PyValueError>(() => Make("D", new PyList([Pair("x", 1L), Pair("x", 2L)]))).PyMessage);
            Assert.Throws<PyValueError>(() => Make("D", new PyList([Pair("_bad", 1L)])));
            Assert.Throws<PyNotImplementedError>(() => Invoke([1L, new PyList()]));
            Assert.Contains("field str conversion", Assert.Throws<PyNotImplementedError>(() => Make("Raw", PyIter.Create(new PyList([Pair(1L, 1L)])))).PyMessage);
            string name = new string("Same".ToCharArray()), field = new string("Same".ToCharArray());
            PyClass cls = Make(name, new PyList([Pair(field, PyType.Get("int"))]));
            Assert.Same(name, ((PyTuple)Get(cls, "_fields")!).Items[0]);
        });
    }

    [Fact]
    public void OrigBasesReadsTheCurrentGlobalOnlyAtTheEnd()
    {
        Supported(() =>
        {
            var replacement = new PyList();
            var source = new TraceSource(_ => [Pair("x", PyType.Get("int"))], pass => { if (pass == 2) Typing["NamedTuple"] = replacement; });
            PyClass cls = Make("LateRoot", source);
            Assert.Same(replacement, ((PyTuple)Get(cls, "__orig_bases__")!).Items[0]);
            Typing["NamedTuple"] = PyNone.Instance;
            Assert.Same(PyNone.Instance, ((PyTuple)Get(Make("NoneRoot", new PyList()), "__orig_bases__")!).Items[0]);
            Typing.Remove("NamedTuple");
            Assert.Equal("name 'NamedTuple' is not defined", Assert.Throws<PyNameError>(() => Make("MissingRoot", new PyList())).PyMessage);
        });
    }

    [Fact]
    public void HelperOverridesRejectAtCallerAndFactoryConsumptionPoints()
    {
        Supported(() =>
        {
            foreach (string name in new[] { "_make_nmtuple", "_caller", "sys" })
            {
                var source = new TraceSource(_ => []);
                WithTyping(name, PyNone.Instance, () => Assert.Contains("typing." + name, Assert.Throws<PyNotImplementedError>(() => Make("Early", source)).PyMessage));
                Assert.Equal(0, source.Iterations);
            }
            var late = new TraceSource(_ => []);
            WithTyping("collections", PyNone.Instance, () => Assert.Contains("typing.collections", Assert.Throws<PyNotImplementedError>(() => Make("Late", late)).PyMessage));
            Assert.Equal(2, late.Iterations);
        });
    }

    [Fact]
    public void CachedSysOverrideIsGuardedWithoutCallingOrImportingFallbacks()
    {
        Supported(() => WithTemporarySys(new Dictionary<string, object?> { ["_getframemodulename"] = PyNone.Instance }, () =>
        {
            var source = new TraceSource(_ => []);
            Assert.Contains("sys._getframemodulename", Assert.Throws<PyNotImplementedError>(() => Make("Blocked", source)).PyMessage);
            Assert.Equal(0, source.Iterations);
        }));
        Supported(() => WithTemporarySys(new Dictionary<string, object?> { ["_getframe"] = PyNone.Instance }, () =>
            Assert.Equal("caller_module", Get(Make("Unaffected", new PyList()), "__module__"))));
    }

    [Fact]
    public void OwnFailuresKeepCallerContextAndUnknownOriginDoesNotIterate()
    {
        WithRoot(() =>
        {
            var source = new TraceSource(_ => []);
            long depth = PyTraceback.PushFrame("unknown.py", "unknown");
            try { Assert.Throws<PyNotImplementedError>(() => Make("Unknown", source)); }
            finally { PyTraceback.PopTo(depth); }
            Assert.Equal(0, source.Iterations);
        });
        Supported(() =>
        {
            var ambient = new PyValueError("ambient");
            object? previous = PyErrors.EnterHandler(ambient);
            try
            {
                PyTypeError error = Assert.Throws<PyTypeError>(() => Invoke(["Mixed", new PyList()], ["x"], [1L]));
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("namedtuple_functional.py", "caller", 19), error.Frames[0]);
            }
            finally { PyErrors.LeaveHandler(previous); }
        });
    }

    private sealed class TraceSource(Func<int, object?[]> rows, Action<int>? before = null) : PyNativeObject
    {
        internal int Iterations, HintRequests;
        public override string TypeName => "TraceSource";
        public override object? GetAttr(string name)
        {
            if (name == "__iter__") return PyNativeFunctions.Create(name, [], [], 0, _ =>
            {
                int pass = ++Iterations; before?.Invoke(pass); return new CallbackIterator(rows(pass));
            });
            if (name is "__len__" or "__length_hint__") { HintRequests++; throw new PyValueError("unexpected hint"); }
            throw Missing(name);
        }
    }
    private sealed class CallbackIterator(object?[] values) : IPyIterator
    {
        private int index = -1;
        internal int Moves;
        public bool MoveNext() { Moves++; return ++index < values.Length; }
        public object? Current() => values[index];
    }
    private sealed class TruthModule(Func<bool> body) : PyNativeObject
    {
        public override string TypeName => "TruthModule";
        public override object? GetAttr(string name) => name == "__bool__"
            ? PyNativeFunctions.Create(name, [], [], 0, _ => body()) : throw Missing(name);
    }

    private static void WithTemporarySys(Dictionary<string, object?> attributes, Action body)
    {
        // 测试侧临时替换注册/cache并完整恢复；不改PySystem._sys或现有模块的私有属性字典。
        var registry = (IDictionary)typeof(PyModules).GetField("Registry", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var cache = (IDictionary)typeof(PyModules).GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        bool registered = registry.Contains("sys"), cached = cache.Contains("sys");
        object? oldRegistration = registry["sys"], oldModule = cache["sys"];
        try
        {
            cache.Remove("sys");
            PyModules.RegisterNative("sys", attributes);
            _ = PyModules.Import("sys");
            body();
        }
        finally
        {
            if (registered) registry["sys"] = oldRegistration; else registry.Remove("sys");
            if (cached) cache["sys"] = oldModule; else cache.Remove("sys");
        }
    }
}
