// 原生 builtins 冷协议的运行时机制门：捕获先于执行，活查与规范 callable 守卫分别验证。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCompiledBuiltinsTests
{
    [Fact]
    public void EntryCapturesCanonicalBuiltinsBeforeExecutingAndKeepsTheOriginalEnvironment()
    {
        var provider = new Provider();
        PyProvider.Install(provider);
        try
        {
            object? context = null;
            string name = "compiled_builtins_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterStockEntrySourceLocated(name, "", "entry.py", 1, "/virtual/entry.py", false,
                state => { context = state; Assert.Same(provider.Builtins.ViewObject, PyModuleInstances.LoadName(state, "__builtins__")); });
            var module = PyModuleInstances.Create(name);
            PyModuleInstances.Execute(module);
            Assert.Same(provider.Builtins.ViewObject, module.GetAttr("__builtins__"));
            Assert.Equal(7L, PyModuleInstances.LoadName(context!, "marker"));
            provider.Builtins.Values["marker"] = 9L;
            Assert.Equal(9L, PyModuleInstances.LoadName(context!, "marker"));
            module.SetAttr("__builtins__", new PyDict());
            Assert.Equal(9L, PyModuleInstances.LoadName(context!, "marker"));
            Assert.Same(module.GetAttr("__dict__"), PyModuleInstances.CallDirectGlobals(context!, provider.Globals));
            PyModuleInstances.ImportGuard(context!);
            provider.Builtins.Values["__import__"] = new object();
            Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ImportGuard(context!));
            provider.Builtins.Values.Remove("__import__");
            Assert.Throws<PyImportError>(() => PyModuleInstances.ImportGuard(context!));
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void DisabledCapabilityKeepsTheExistingManagedEnvironment()
    {
        var provider = new Provider { Enabled = false };
        PyProvider.Install(provider);
        try
        {
            string name = "managed_builtins_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterStockEntrySourceLocated(name, "", "entry.py", 1, "/virtual/entry.py", false,
                state => Assert.IsType<PyDict>(PyModuleInstances.LoadName(state, "__builtins__")));
            PyModuleInstances.Execute(PyModuleInstances.Create(name));
        }
        finally { PyProvider.Remove(provider); }
    }

    private sealed class Namespace : IPyModuleNamespace
    {
        public object ViewObject { get; } = new();
        internal Dictionary<string, object?> Values { get; } = new();
        public bool TryGet(string name, out object? value) => Values.TryGetValue(name, out value);
        public void Set(string name, object? value) => Values[name] = value;
        public bool DeleteIfPresent(string name) => Values.Remove(name);
    }

    private sealed class Provider : IPyModuleProvider, IPyCompiledModuleNamespaceProvider, IPyCompiledBuiltinsProvider
    {
        internal bool Enabled = true;
        internal readonly Namespace Builtins = new();
        internal readonly object Globals = new(), Importer = new();
        internal Provider()
        { Builtins.Values["globals"] = Globals; Builtins.Values["__import__"] = Importer; Builtins.Values["marker"] = 7L; }
        public bool TryGetCompiledBuiltins(out IPyModuleNamespace? builtins)
        { builtins = Enabled ? Builtins : null; return Enabled; }
        public bool TryGetCompiledBuiltinsNamespace(object value, out IPyModuleNamespace? builtins)
        { builtins = ReferenceEquals(value, Builtins.ViewObject) ? Builtins : null; return builtins is not null; }
        public bool IsCanonicalCompiledBuiltin(object? value, string name) => Enabled && ReferenceEquals(value,
            name == "globals" ? Globals : name == "__import__" ? Importer : null);
        public PyCompiledModuleNamespace CreateEntryNamespace(string name) =>
            new(new Namespace(), new PyForeignModuleStateTests.ForeignModule());
        public void PublishEntryNamespace(PyModule module, PyCompiledModuleNamespace storage) { }
        public bool Owns(string name) => false;
        public object? SystemAttribute(string name) => throw new NotSupportedException();
        public PyModule Import(string name) => throw new NotSupportedException();
        public void Registered(string name) { }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }
    }
}
