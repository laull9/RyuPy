// 显式实例普通类入口：hook选择与类体名字环境分开，禁止借用traceback的ambient模块来源。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyInstanceClassTests
{
    private static (PyModule Module, object Context) Instance()
    {
        object? context = null; string name = "class_owner_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "class.py", 1, "/virtual/class.py", false,
            value => context = value);
        var module = PyModuleInstances.Create(name); PyModuleInstances.Execute(module);
        return (module, context!);
    }
    private static PyDict Globals(PyModule module) => Assert.IsType<PyDict>(module.GetAttr("__dict__"));

    [Fact]
    public void DefaultClassHookIsStableCallableButDirectProtocolIsExplicitlyUnsupported()
    {
        var (a, context) = Instance(); var (b, _) = Instance();
        var first = Assert.IsType<PyDict>(a.GetAttr("__builtins__")).Get("__build_class__");
        var second = Assert.IsType<PyDict>(b.GetAttr("__builtins__")).Get("__build_class__");
        Assert.Same(first, second); Assert.Equal(true, PyOps.CallBuiltin("callable", [first], [], []));
        PyModuleInstances.ClassGuard(context);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(first, [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(first, [], ["metaclass"], [PyNone.Instance]));
    }

    [Fact]
    public void ClassGuardConsumesFrameBuiltinsNotGlobalsAndNeverInvokesReplacement()
    {
        var (module, context) = Instance();
        module.SetAttr("__build_class__", PyNone.Instance);
        module.SetAttr("__builtins__", new PyDict());
        PyModuleInstances.ClassGuard(context);
        object missing = PyModuleInstances.CaptureDefinitionContext(context);
        Assert.Throws<PyNameError>(() => PyModuleInstances.ClassGuard(missing));
        int calls = 0;
        var hook = PyNativeFunctions.CreateVariadic("hook", _ => { calls++; return PyNone.Instance; });
        var replacement = Assert.IsType<PyDict>(module.GetAttr("__builtins__"));
        replacement.Set("__build_class__", hook);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ClassGuard(missing));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ClassMetadataUsesCapturedClassFrameWithGlobalsFirst()
    {
        var (module, outer) = Instance();
        var oldBuiltins = new PyDict(); oldBuiltins.Set("__name__", "old builtin name");
        module.SetAttr("__builtins__", oldBuiltins);
        object captured = PyModuleInstances.CaptureDefinitionContext(outer);
        var newer = new PyDict(); newer.Set("__name__", "new builtin name");
        module.SetAttr("__builtins__", newer);
        Globals(module).Delete("__name__");
        var first = Assert.IsType<PyClass>(PyModuleInstances.CreateClass(captured, "C", []));
        Assert.Equal("old builtin name", first.Attributes["__module__"]);
        var marker = new PyList([]); module.SetAttr("__name__", marker);
        var second = Assert.IsType<PyClass>(PyModuleInstances.CreateClass(captured, "D", []));
        Assert.Same(marker, second.Attributes["__module__"]);
        Assert.Equal("D", second.Attributes["__qualname__"]);
        Globals(module).Delete("__name__"); oldBuiltins.Delete("__name__");
        Assert.Throws<PyNameError>(() => PyModuleInstances.CreateClass(captured, "Missing", []));
    }

    [Fact]
    public void DefaultBuiltinsNameIsTheRealClassFallback()
    {
        var (module, context) = Instance(); Globals(module).Delete("__name__");
        var cls = Assert.IsType<PyClass>(PyModuleInstances.CreateClass(context, "Default", []));
        Assert.Equal("builtins", cls.Attributes["__module__"]);
        Assert.Same(PyNone.Instance, PyModuleInstances.DefinitionModule(context));
    }

    [Fact]
    public void ExplicitClassContextDoesNotUseMutableFunctionModuleOrAmbientSource()
    {
        var (module, context) = Instance(); module.SetAttr("__name__", "actual globals");
        var signature = PySignature.Create("f", [], [], []);
        var fn = PyModuleInstances.CreateOwnedFunction(context, "f", signature,
            (_, _, _, _, _) => PyNone.Instance, [], false, null);
        PyObject.SetAttr(fn, "__module__", "unrelated metadata");
        long mark = PyTraceback.PushFrameWithOrigin("class.py", "f", fn);
        try
        {
            var cls = Assert.IsType<PyClass>(PyModuleInstances.CreateClass(context, "Nested", []));
            Assert.Equal("actual globals", cls.Attributes["__module__"]);
        }
        finally { PyTraceback.PopTo(mark); }
    }

    [Fact]
    public void OnlyOrdinaryClassLayoutsAreAccepted()
    {
        var (_, context) = Instance();
        var plain = Assert.IsType<PyClass>(PyModuleInstances.CreateClass(context, "Base", []));
        var derived = Assert.IsType<PyClass>(PyModuleInstances.CreateClass(context, "Derived", [plain]));
        Assert.Same(plain, derived.BaseClass);
        Assert.IsType<PyClass>(PyModuleInstances.CreateClass(context, "ObjectBase", [PyOps.TypeObject("object")]));
        foreach (var value in new[] { PyOps.TypeObject("str"), PyOps.TypeObject("list"), PyOps.TypeObject("dict"), PyOps.TypeObject("ValueError") })
            Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.CreateClass(context, "Special", [value]));
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.CreateClass(context, "Multiple", [plain, derived]));
    }
}
