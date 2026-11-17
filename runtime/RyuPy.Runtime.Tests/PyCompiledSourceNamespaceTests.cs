// 完整源码模块工厂机制门：真实后端唯一身份、跨实例隔离与缺少能力时的执行前拒绝。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCompiledSourceNamespaceTests
{
    [Fact]
    public void SourceModuleUsesOneBackendForExecutionAttributesAndArbitraryExternalNames()
    {
        var provider = new Provider();
        PyProvider.Install(provider);
        try
        {
            string name = "source_namespace_" + Guid.NewGuid().ToString("N");
            object? captured = null;
            int calls = 0;
            PyModules.RegisterStockModuleSourceLocated(name, "", "unit.py", 1, "/virtual/unit.py", false,
                state => { calls++; captured = state; PyModuleInstances.StoreName(state, "body", 17L); });
            Assert.Empty(provider.Created);
            PyModule first = PyModuleInstances.Create(name);
            Assert.Equal(0, calls);
            var storage = provider.Created.Single();
            Assert.Same(storage.Module, first.ForeignObject);
            Assert.Same(storage.Globals.ViewObject, first.GetAttr("__dict__"));
            Assert.Equal("retained", first.GetAttr("initial"));
            PyModuleInstances.Execute(first);
            Assert.Equal(17L, first.GetAttr("body"));
            var dictionary = (PyDict)storage.Globals.ViewObject;
            dictionary.Set("body", "external replacement");
            dictionary.Set("len", 23L);
            Assert.Equal("external replacement", PyModuleInstances.LoadName(captured!, "body"));
            Assert.Equal(23L, PyModuleInstances.LoadName(captured!, "len"));
            first.SetAttr("runtime_attribute", 29L);
            Assert.Equal(29L, dictionary.Get("runtime_attribute"));
            dictionary.Delete("body");
            Assert.Throws<PyNameError>(() => PyModuleInstances.LoadName(captured!, "body"));
            PyModule second = PyModuleInstances.Create(name);
            Assert.NotSame(first.GetAttr("__dict__"), second.GetAttr("__dict__"));
            Assert.Throws<PyAttributeError>(() => second.GetAttr("runtime_attribute"));
            Assert.Equal(2, provider.Published);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void MissingSourceNamespaceCapabilityRejectsBeforeCallingTheCompiledBody()
    {
        string name = "source_namespace_required_" + Guid.NewGuid().ToString("N");
        bool executed = false;
        PyModules.RegisterStockModuleSourceLocated(name, "", "unit.py", 1, "/virtual/unit.py", false,
            _ => executed = true);
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.Create(name));
        Assert.False(executed);
    }

    private sealed class Namespace : IPyModuleNamespace
    {
        private readonly PyDict dictionary = new();
        internal Namespace() => dictionary.Set("initial", "retained");
        public object ViewObject => dictionary;
        public bool TryGet(string name, out object? value) => dictionary.TryGet(name, out value);
        public void Set(string name, object? value) => dictionary.Set(name, value);
        public bool DeleteIfPresent(string name) => dictionary.DeleteIfPresent(name);
    }

    private sealed class Provider : IPyModuleProvider, IPyCompiledSourceNamespaceProvider
    {
        internal readonly List<PyCompiledModuleNamespace> Created = [];
        internal int Published;
        public PyCompiledModuleNamespace CreateModuleNamespace(string name)
        {
            var storage = new PyCompiledModuleNamespace(new Namespace(), new PyForeignModuleStateTests.ForeignModule());
            Created.Add(storage);
            return storage;
        }
        public void PublishModuleNamespace(PyModule module, PyCompiledModuleNamespace storage)
        {
            Assert.Same(storage.Module, module.ForeignObject);
            Assert.Same(storage.Globals.ViewObject, module.GetAttr("__dict__"));
            Published++;
        }
        public bool Owns(string name) => false;
        public object? SystemAttribute(string name) => throw new NotSupportedException();
        public PyModule Import(string name) => throw new NotSupportedException();
        public void Registered(string name) { }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }
    }
}
