// 显式provider导入现场的中性机制门：captured builtins活读、真实D/None与任意返回/原异常。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

public sealed class PyCompiledImportTests
{
    private sealed class Namespace : IPyModuleNamespace
    {
        internal readonly PyDict Values = new();
        public object ViewObject => Values;
        public bool TryGet(string name, out object? value) => Values.TryGet(name, out value);
        public void Set(string name, object? value) => Values.Set(name, value);
        public bool DeleteIfPresent(string name) => Values.DeleteIfPresent(name);
    }
    [Fact]
    public void ReadsTheActualCapturedHookOnEveryCallWithTheFrozenLocalsRole()
    {
        var ns = new Namespace(); var builtins = new PyDict();
        var context = new PyModuleExecutionContext(new PyModuleInstanceState(null, ns), builtins);
        object result = new(); var calls = new List<object?[]>();
        builtins.Set("__import__", PyNativeFunctions.CreateVariadic("hook", args => { calls.Add(args); return result; }));
        Assert.Same(result, PyModuleInstances.ImportName(context, "branch", 0, null, true));
        Assert.Same(ns.Values, calls[0][1]); Assert.Same(ns.Values, calls[0][2]);
        Assert.Same(PyNone.Instance, calls[0][3]); Assert.Equal(0L, calls[0][4]);
        ns.Set("__builtins__", new PyDict());
        Assert.Same(result, PyModuleInstances.ImportName(context, "leaf", 2, new object?[] { "member" }, false));
        Assert.Same(PyNone.Instance, calls[1][2]);
        Assert.Equal(new object?[] { "member" }, Assert.IsType<PyTuple>(calls[1][3]).Items);
        Assert.Equal(2L, calls[1][4]);
        builtins.Set("__import__", PyNativeFunctions.CreateVariadic("replacement", _ => 37L));
        Assert.Equal(37L, PyModuleInstances.ImportName(context, "branch", 0, null, true));
        Assert.Equal(2, calls.Count);
    }
    [Fact]
    public void MissingNoneAndThrowingHooksKeepTheirDistinctProtocolResults()
    {
        var builtins = new PyDict();
        var context = new PyModuleExecutionContext(new PyModuleInstanceState(null, new Namespace()), builtins);
        Assert.Equal("__import__ not found", Assert.Throws<PyImportError>(() => PyModuleInstances.ImportName(context, "a", 0, null, true)).Message);
        builtins.Set("__import__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => PyModuleInstances.ImportName(context, "a", 0, null, true));
        var failure = new PyValueError("hook failure");
        builtins.Set("__import__", PyNativeFunctions.CreateVariadic("failure", _ => throw failure));
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyModuleInstances.ImportName(context, "a", 0, null, true)));
    }
    [Fact]
    public void RequiresARealProviderNamespaceBeforeCallingAnyHook()
    {
        bool called = false; var builtins = new PyDict();
        builtins.Set("__import__", PyNativeFunctions.CreateVariadic("hook", _ => { called = true; return 1L; }));
        var context = new PyModuleExecutionContext(new PyModuleInstanceState(null), builtins);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ImportName(context, "a", 0, null, true));
        Assert.False(called);
    }
}
