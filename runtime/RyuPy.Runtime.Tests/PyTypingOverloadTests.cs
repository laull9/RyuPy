// overload有限竖切的先行合同：真实来源、三层存储、原对象与dummy身份；不公开或伪造code。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed partial class PyTypingOverloadTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    private readonly Dictionary<string, object?> saved = new(PyTyping.CreateAttributes());
    private static Dictionary<string, object?> Typing => PyTyping.CreateAttributes();
    private static object? Export(string name) => Typing[name];
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static object? Register(object? value) => Call(Export("overload"), value);
    private static PyList Query(object? value) => Assert.IsType<PyList>(Call(Export("get_overloads"), value));
    private static object? Clear() => Call(Export("clear_overloads"));
    private static object? Get(object? value, string name) => PyObject.GetAttr(value, name);
    private static PyDefaultDict Registry => Assert.IsType<PyDefaultDict>(Export("_overload_registry"));
    private const string DummyMessage = "You should not call an overloaded function. A series of @overload-decorated functions outside a stub module should always be followed by an implementation that is not @overload-ed.";

    public PyTypingOverloadTests()
    {
        if (Typing.TryGetValue("_overload_registry", out object? registry) && registry is PyDefaultDict map)
            map.Dictionary.Clear();
    }

    public void Dispose()
    {
        Typing.Clear();
        foreach (var pair in saved) Typing[pair.Key] = pair.Value;
        if (Typing.TryGetValue("_overload_registry", out object? registry) && registry is PyDefaultDict map)
            map.Dictionary.Clear();
        frame.Dispose();
    }

    private static PyFunction Function(string qualified = "target", long line = 10, object? module = null, object? result = null)
    {
        PySignature signature = PySignature.Create("backend_name", [], [], []);
        PyOps.FunctionDefinition(signature, qualified, line);
        var function = new PyFunction("visible", signature, (_, _, _, _) => result ?? PyNone.Instance, [], false);
        PyObject.SetAttr(function, "__module__", module ?? "overload_contract");
        return function;
    }

    private static void Changed(string name, object? value, Action body)
    {
        bool existed = Typing.TryGetValue(name, out object? previous);
        Typing[name] = value;
        try { body(); }
        finally { if (existed) Typing[name] = previous; else Typing.Remove(name); }
    }

    [Fact]
    public void StableFunctionsAndSharedDummyHaveRealCanonicalMetadata()
    {
        foreach (string name in new[] { "overload", "get_overloads", "clear_overloads", "_overload_dummy" })
        {
            object? function = Export(name);
            Assert.IsType<PyFunction>(function);
            Assert.Same(function, Export(name));
            Assert.Equal(name, Get(function, "__name__"));
            Assert.Equal(name, Get(function, "__qualname__"));
            Assert.Equal("typing", Get(function, "__module__"));
        }
        PyFunction first = Function(line: 20), second = Function(line: 30);
        Assert.Same(Export("_overload_dummy"), Register(first));
        Assert.Same(Register(first), Register(second));
        Assert.Equal(DummyMessage, Assert.Throws<PyNotImplementedError>(() => Call(Export("_overload_dummy"))).PyMessage);
        Assert.Equal(DummyMessage, Assert.Throws<PyNotImplementedError>(() =>
            PyOps.CallValue(Export("_overload_dummy"), [1L, 2L], ["x"], [3L])).PyMessage);
    }

    [Fact]
    public void RegistryUsesActualDefaultDictPartialAndOrderedDictionary()
    {
        PyDefaultDict registry = Registry;
        var factory = Assert.IsType<PyPartial>(Get(registry, "default_factory"));
        Assert.Same(PyCollections.CreateAttributes()["defaultdict"], Get(factory, "func"));
        Assert.Equal(new object?[] { PyType.Get("dict") }, Assert.IsType<PyTuple>(Get(factory, "args")).Items);
        PyFunction function = Function(line: 101);
        Register(function);
        var module = Assert.IsType<PyDefaultDict>(registry.Dictionary.Get("overload_contract"));
        Assert.Same(PyType.Get("dict"), Get(module, "default_factory"));
        var lines = Assert.IsType<PyDict>(module.Dictionary.Get("target"));
        Assert.Same(function, lines.Get(101L));
        Assert.Single(lines.Entries());
    }

    [Fact]
    public void RepeatedLineReplacesWithoutMovingAndQueriesAreIndependentSnapshots()
    {
        PyFunction later = Function(line: 90, result: "later"), earlier = Function(line: 10, result: "earlier");
        PyFunction replacement = Function(line: 90, result: "replacement");
        Register(later); Register(earlier);
        PyList before = Query(later);
        Register(replacement);
        PyList after = Query(later);
        Assert.Equal(new object?[] { later, earlier }, before.Items);
        Assert.Equal(new object?[] { replacement, earlier }, after.Items);
        Assert.NotSame(after, Query(later));
        after.Items.Clear();
        Assert.Equal(2, Query(later).Count);
        Assert.Same(PyNone.Instance, Clear());
        Assert.Empty(Registry.Dictionary.Entries());
        Assert.Empty(Query(later).Items);
        Assert.NotSame(Query(later), Query(later));
        Assert.Equal("later", Call(before.Items[0]));
    }

    [Fact]
    public void MutableModuleQualnameAndNameRemainSeparateFromImmutableLine()
    {
        PyFunction first = Function(), other = Function(line: 12);
        PyObject.SetAttr(first, "__module__", PyNone.Instance);
        PyObject.SetAttr(other, "__module__", PyNone.Instance);
        Register(first); Register(other);
        PyObject.SetAttr(first, "__name__", "ignored");
        Assert.Equal(2, Query(first).Count);
        PyObject.SetAttr(first, "__module__", 7L);
        Assert.Empty(Query(first).Items);
        Register(first);
        Assert.Same(first, Query(first).Items[0]);
        PyObject.SetAttr(first, "__qualname__", "changed");
        Assert.Empty(Query(first).Items);
        Register(first);
        Assert.Same(first, Query(first).Items[0]);
        PyObject.SetAttr(first, "__qualname__", "target");
        PyObject.DelAttr(first, "__module__");
        Assert.Equal(2, Query(first).Count);
    }

    [Fact]
    public void OneLayerUnwrapStoresOriginalAndBoundFuncCannotBeSpoofed()
    {
        var cls = new PyClass("Owner");
        PyFunction function = Function("Owner.method", 30), other = Function("other", 40);
        var bound = new PyBoundMethod(cls, new PyInstance(cls), function);
        var cm = new PyClassMethod(function);
        var sm = new PyStaticMethod(function);
        PyObject.SetAttr(function, "__func__", other);
        Assert.Same(function, Get(bound, "__func__"));
        Assert.Same(function, Get(cm, "__func__"));
        Assert.Same(function, Get(sm, "__func__"));
        Register(bound);
        Assert.Same(bound, Query(cm).Items[0]);
        Register(cm); Assert.Same(cm, Query(sm).Items[0]);
        Register(sm); Assert.Same(sm, Query(bound).Items[0]);
        // 普通函数自身的普通__func__确实参与一次getattr，不递归解包装。
        Register(function);
        Assert.Same(function, Query(other).Items[0]);
        Assert.Same(sm, Query(bound).Items[0]);
    }

    [Fact]
    public void MissingKnownValuesReturnDummyButQueriesStillRaiseAttributeError()
    {
        foreach (object? value in new object?[] { PyNone.Instance, 1L, true, "x", new PyList(), new PyDict(), new PyTuple([]) })
        {
            Assert.Same(Export("_overload_dummy"), Register(value));
            Assert.Throws<PyAttributeError>(() => Query(value));
        }
        Assert.Equal(0, Registry.Dictionary.Count);
    }

    [Fact]
    public void SavedExportsAndCurrentDummySlotAreIndependent()
    {
        object? overload = Export("overload"), get = Export("get_overloads"), clear = Export("clear_overloads");
        PyFunction function = Function();
        Typing["overload"] = 1L; Typing["get_overloads"] = 2L; Typing["clear_overloads"] = 3L;
        var replacement = new PyList();
        Changed("_overload_dummy", replacement, () =>
        {
            Assert.Same(replacement, Call(overload, function));
            Assert.Same(function, Assert.IsType<PyList>(Call(get, function)).Items[0]);
        });
        Assert.Same(PyNone.Instance, Call(clear));
        Assert.Empty(Assert.IsType<PyList>(Call(get, function)).Items);
    }

    [Fact]
    public void ExistingPlainDictionaryReplacementReceivesRealWritesAndClear()
    {
        PyFunction function = Function();
        var lines = new PyDict(); var module = new PyDict(); var replacement = new PyDict();
        module.Set("target", lines); replacement.Set("overload_contract", module);
        Changed("_overload_registry", replacement, () =>
        {
            Register(function); Assert.Same(function, lines.Get(10L));
            Assert.Same(function, Query(function).Items[0]);
            Assert.Same(PyNone.Instance, Clear()); Assert.Equal(0, replacement.Count);
            Assert.Throws<PyKeyError>(() => Register(function));
        });
        Assert.Equal(0, Registry.Dictionary.Count);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<PyFunction> RegisterWithoutCallerRoot()
    {
        PyFunction function = Function(result: "held by registry");
        Register(function);
        return new WeakReference<PyFunction>(function);
    }

    [Fact]
    public void RegistryKeepsOriginalFunctionAliveUntilExplicitClear()
    {
        WeakReference<PyFunction> reference = RegisterWithoutCallerRoot();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.True(reference.TryGetTarget(out PyFunction? function));
        PyList snapshot = Query(function);
        Assert.Same(function, snapshot.Items[0]);
        Clear();
        Assert.Equal("held by registry", Call(snapshot.Items[0]));
    }

}
