// SETUP_ANNOTATIONS的后端协议门：缺键才创建、已有任意对象保留，真实异常不包装。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;
public sealed class PyModuleAnnotationsTests
{
    private sealed class Namespace : IPyModuleNamespace, IPyModuleDictionaryNamespace
    {
        internal readonly PyDict Data = new();
        internal readonly object Created = new PyDict();
        internal int Creates;
        internal PyBaseException? Failure;
        public object ViewObject => Data;
        public bool TryGet(string name, out object? value) => Data.TryGet(name, out value);
        public void Set(string name, object? value)
        { if (Failure is not null) throw Failure; Data.Set(name, value); }
        public bool DeleteIfPresent(string name) => Data.DeleteIfPresent(name);
        public object CreateDictionary() { Creates++; return Created; }
    }
    [Fact]
    public void CreatesOnceInTheTargetNamespace()
    {
        var storage = new Namespace();
        var context = new PyModuleExecutionContext(new PyModuleInstanceState(null, storage), new PyDict());
        PyModuleInstances.SetupAnnotations(context); PyModuleInstances.SetupAnnotations(context);
        Assert.Equal(1, storage.Creates);
        Assert.Same(storage.Created, storage.Data.Get("__annotations__"));
    }
    [Fact]
    public void ExistingNoneAndOtherObjectsRemainAuthoritative()
    {
        foreach (object value in new object[] { PyNone.Instance, 7L, new object() })
        {
            var storage = new Namespace(); storage.Data.Set("__annotations__", value);
            var context = new PyModuleExecutionContext(new PyModuleInstanceState(null, storage), new PyDict());
            PyModuleInstances.SetupAnnotations(context);
            Assert.Same(value, storage.Data.Get("__annotations__")); Assert.Equal(0, storage.Creates);
        }
    }
    [Fact]
    public void PreservesBackendFailureIdentity()
    {
        var storage = new Namespace { Failure = new PyRuntimeError("write-failure") };
        var context = new PyModuleExecutionContext(new PyModuleInstanceState(null, storage), new PyDict());
        Assert.Same(storage.Failure, Assert.Throws<PyRuntimeError>(() => PyModuleInstances.SetupAnnotations(context)));
    }
}
