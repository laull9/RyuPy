// overload的绑定与消费阶段先行合同；NI负测试独立于完整CP正例，不能据此计入CP通过数。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed partial class PyTypingOverloadTests
{
    [Theory]
    [InlineData("overload")]
    [InlineData("get_overloads")]
    public void UnaryBindingHandlesKeywordsBeforeExtraPositionals(string name)
    {
        object? target = Export(name); PyFunction function = Function();
        string Error(object?[] args, string[] names, object?[] values) =>
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(target, args, names, values)).PyMessage;
        Assert.Equal($"{name}() missing 1 required positional argument: 'func'", Error([], [], []));
        Assert.Equal($"{name}() takes 1 positional argument but 2 were given", Error([function, function], [], []));
        Assert.Equal($"{name}() got an unexpected keyword argument 'bad'", Error([function, function], ["bad"], [3L]));
        Assert.Equal($"{name}() got multiple values for argument 'func'", Error([function, function], ["func"], [function]));
        Assert.Equal($"{name}() got an unexpected keyword argument 'bad'", Error([], ["bad"], [function]));
        Changed("getattr", 42L, () => Assert.Equal($"{name}() got an unexpected keyword argument 'bad'", Error([], ["bad"], [function])));
        Assert.NotNull(PyOps.CallValue(target, [], ["func"], [function]));
    }

    [Fact]
    public void ClearBindingPrecedesRegistryAndUsesExactMessages()
    {
        object? clear = Export("clear_overloads");
        Changed("_overload_registry", 1L, () =>
        {
            Assert.Equal("clear_overloads() takes 0 positional arguments but 1 was given",
                Assert.Throws<PyTypeError>(() => Call(clear, 1L)).PyMessage);
            Assert.Equal("clear_overloads() got an unexpected keyword argument 'bad'",
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(clear, [1L], ["bad"], [1L])).PyMessage);
        });
    }

    [Fact]
    public void UnhashableModuleFailsEvenForEmptyRegistryAndUnknownHashNeverRuns()
    {
        PyFunction function = Function();
        foreach (object key in new object[] { new PyList(), new PyDict() })
        {
            PyObject.SetAttr(function, "__module__", key);
            string expected = $"unhashable type: '{PyRepr.TypeName(key)}'";
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Register(function)).PyMessage);
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Query(function)).PyMessage);
            Assert.Equal(0, Registry.Dictionary.Count);
        }
        var cls = new PyClass("HashKey"); int calls = 0;
        cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 9L; }));
        PyObject.SetAttr(function, "__module__", new PyInstance(cls));
        Assert.Throws<PyNotImplementedError>(() => Register(function));
        Assert.Throws<PyNotImplementedError>(() => Query(function));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void UnknownNativeCodePreservesBothSuccessfulBucketsWithoutFakeLineZero()
    {
        PyFunction native = PyNativeFunctions.Create("native", [], [], 0, _ => null);
        PyObject.SetAttr(native, "__module__", "partial_module");
        PyObject.SetAttr(native, "__qualname__", "native");
        Assert.Throws<PyNotImplementedError>(() => Register(native));
        var module = Assert.IsType<PyDefaultDict>(Registry.Dictionary.Get("partial_module"));
        var lines = Assert.IsType<PyDict>(module.Dictionary.Get("native"));
        Assert.Equal(0, lines.Count);
        Assert.Empty(Query(native).Items);
        Assert.Throws<PyNotImplementedError>(() => Register(PyOps.BuiltinFunction("len")));
    }

    [Fact]
    public void UnknownQualnameLeavesOnlyModulePrefix()
    {
        PyFunction native = PyNativeFunctions.Create("native", [], [], 0, _ => null);
        PyObject.SetAttr(native, "__module__", "qualname_unknown");
        Assert.Throws<PyNotImplementedError>(() => Register(native));
        Assert.Equal(0, Assert.IsType<PyDefaultDict>(Registry.Dictionary.Get("qualname_unknown")).Dictionary.Count);
    }

    [Fact]
    public void HelperGuardsRunOnlyAtActualConsumptionPhase()
    {
        PyFunction function = Function();
        Changed("AttributeError", 42L, () =>
        {
            Assert.Same(Export("_overload_dummy"), Register(function));
            Assert.Throws<PyNotImplementedError>(() => Register(PyNone.Instance));
            PyObject.SetAttr(function, "__module__", new PyList());
            Assert.Throws<PyNotImplementedError>(() => Register(function));
            PyObject.SetAttr(function, "__module__", "overload_contract");
        });
        Changed("list", 42L, () =>
        {
            Assert.Throws<PyNotImplementedError>(() => Query(function));
            Assert.Empty(Query(Function("missing")).Items);
            Assert.Empty(Query(Function(module: "missing_module")).Items);
            Assert.Same(PyNone.Instance, Clear());
        });
        Changed("getattr", 42L, () =>
        {
            Assert.Throws<PyNotImplementedError>(() => Register(function));
            Assert.Throws<PyNotImplementedError>(() => Query(function));
            Assert.Same(PyNone.Instance, Clear());
        });
        Changed("NotImplementedError", 42L, () =>
        {
            Assert.Same(Export("_overload_dummy"), Register(function));
            Assert.Contains("NotImplementedError", Assert.Throws<PyNotImplementedError>(() => Call(Export("_overload_dummy"))).PyMessage);
        });
    }

    [Fact]
    public void ChangedDefaultFactoriesAreIgnoredOnHitsAndRejectedOnlyOnMisses()
    {
        PyFunction function = Function(); Register(function);
        var module = Assert.IsType<PyDefaultDict>(Registry.Dictionary.Get("overload_contract"));
        object? original = Get(Registry, "default_factory");
        PyObject.SetAttr(Registry, "default_factory", 42L);
        PyObject.SetAttr(module, "default_factory", 42L);
        try
        {
            Register(function); Assert.Same(function, Query(function).Items[0]);
            Assert.Throws<PyNotImplementedError>(() => Register(Function("new_name")));
            Assert.Throws<PyNotImplementedError>(() => Register(Function(module: "new_module")));
            Assert.False(Registry.Dictionary.Contains("new_module"));
            Assert.False(module.Dictionary.Contains("new_name"));
        }
        finally { PyObject.SetAttr(Registry, "default_factory", original); }
    }

    [Fact]
    public void PartialKeywordsAreConsumedOnlyOnMissingModule()
    {
        PyFunction function = Function(); Register(function);
        var factory = Assert.IsType<PyPartial>(Get(Registry, "default_factory"));
        var keywords = Assert.IsType<PyDict>(Get(factory, "keywords"));
        keywords.Set("unexpected", 1L);
        try
        {
            Register(function); Assert.Same(function, Query(function).Items[0]);
            Assert.Throws<PyNotImplementedError>(() => Register(Function(module: "new_module")));
        }
        finally { keywords.Clear(); }
    }

    [Fact]
    public void NoneFactoryHasRealKeyErrorAndClearIgnoresFactoryAndKeys()
    {
        object? original = Get(Registry, "default_factory");
        PyObject.SetAttr(Registry, "default_factory", PyNone.Instance);
        try
        {
            Assert.Throws<PyKeyError>(() => Register(Function()));
            Changed("getattr", 42L, () => Changed("list", 42L, () => Assert.Same(PyNone.Instance, Clear())));
        }
        finally { PyObject.SetAttr(Registry, "default_factory", original); }
    }

    [Fact]
    public void MissingDummySlotDoesNotRollBackCompletedRegistration()
    {
        PyFunction function = Function(); Typing.Remove("_overload_dummy");
        Assert.Throws<PyNameError>(() => Register(function));
        Assert.Same(function, Query(function).Items[0]);
    }

    [Fact]
    public void ExternalKeyChangesInvalidateColdValidationAndClearNeedsNoHash()
    {
        PyFunction function = Function(); Register(function);
        var module = Assert.IsType<PyDefaultDict>(Registry.Dictionary.Get("overload_contract"));
        var cls = new PyClass("ExternalKey"); int calls = 0;
        cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 9L; }));
        var unknown = new PyInstance(cls);
        module.Dictionary.Set(unknown, new PyDict()); calls = 0;
        Assert.Throws<PyNotImplementedError>(() => Register(function));
        Assert.Throws<PyNotImplementedError>(() => Query(function));
        Assert.Equal(0, calls);
        module.Dictionary.Delete(unknown); calls = 0;
        Assert.Same(Export("_overload_dummy"), Register(function));
        Assert.Same(function, Query(function).Items[0]);
        Registry.Dictionary.Set(unknown, module); calls = 0;
        Assert.Same(PyNone.Instance, Clear());
        Assert.Equal(0, calls);
        Assert.Same(Export("_overload_dummy"), Register(function));
        Assert.Same(function, Query(function).Items[0]);
    }


    [Fact]
    public void CanonicalHelperAliasesWorkAndUnknownUnwrapStaysOutsideExceptionHandler()
    {
        PyFunction function = Function();
        Changed("getattr", PyOps.BuiltinFunction("getattr"), () => Register(function));
        Changed("list", PyType.Get("list"), () => Assert.Same(function, Query(function).Items[0]));
        Changed("AttributeError", PyType.Get("AttributeError"), () => Register(PyNone.Instance));
        Changed("NotImplementedError", PyType.Get("NotImplementedError"), () =>
            Assert.Equal(DummyMessage, Assert.Throws<PyNotImplementedError>(() => Call(Export("_overload_dummy"))).PyMessage));
        var unknown = new PyInstance(new PyClass("UnknownAttributes"));
        Changed("AttributeError", 42L, () =>
            Assert.Contains("custom __func__", Assert.Throws<PyNotImplementedError>(() => Register(unknown)).PyMessage));
        // __func__指向wrapper时不再解包；其完整metadata属于明确未实现域。
        PyObject.SetAttr(function, "__func__", new PyClassMethod(Function("other")));
        Assert.Contains("__module__", Assert.Throws<PyNotImplementedError>(() => Register(function)).PyMessage);
    }


    [Fact]
    public void WrapperFuncCallsAndPreparedCaptureUseRealTargetBeforeArgumentExpansion()
    {
        int calls = 0;
        PyFunction target = PyNativeFunctions.Create("wrapped", ["value"], [null], 1,
            args => { calls++; return args[0]; });
        PyObject.SetAttr(target, "__func__", Function("spoof"));
        foreach (object wrapper in new object[] { new PyClassMethod(target), new PyStaticMethod(target) })
        {
            int before = calls;
            object? prepared = PyOps.PrepareMethod(wrapper, "__func__");
            Assert.Same(target, prepared); Assert.Equal(before, calls);
            Assert.Equal("prepared", Call(prepared, "prepared"));
            Assert.Equal("keyword", PyOps.CallMethod(wrapper, "__func__", [], ["value"], ["keyword"]));
            Assert.Equal("star", PyOps.CallMethod(wrapper, "__func__", [PyOps.StarArg(new PyList(["star"]))], [], []));
            // 缺属性先于*展开失败；存在的真实属性则进入正常展开而不调用函数体。
            Assert.Throws<PyAttributeError>(() => PyOps.CallMethod(wrapper, "absent", [PyOps.StarArg(1L)], [], []));
            Assert.Throws<PyTypeError>(() => PyOps.CallMethod(wrapper, "__func__", [PyOps.StarArg(1L)], [], []));
            Assert.Equal(before + 3, calls);
        }
    }

}
