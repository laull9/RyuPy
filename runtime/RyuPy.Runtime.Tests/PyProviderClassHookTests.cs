// provider规范class hook按实际对象身份判定；全局同名覆盖与真正替身不能污染frame builtins。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyProviderClassHookTests
{
    private sealed class Provider : IPyModuleProvider, IPyCompiledBuiltinsProvider
    {
        internal readonly object Canonical = new();
        internal bool Enabled = true;
        public bool IsCanonicalCompiledBuiltin(object? value, string name) => Enabled
            && name == "__build_class__" && ReferenceEquals(value, Canonical);
        public bool TryGetCompiledBuiltins(out IPyModuleNamespace? builtins) { builtins = null; return false; }
        public bool TryGetCompiledBuiltinsNamespace(object value, out IPyModuleNamespace? builtins) { builtins = null; return false; }
        public bool Owns(string name) => false;
        public object? SystemAttribute(string name) => null;
        public PyModule Import(string name) => throw new NotSupportedException();
        public void Registered(string name) { }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }
    }
    private static object Context(PyDict builtins)
    {
        var state = new PyModuleInstanceState(null);
        state.Set("__build_class__", new object());
        return new PyModuleExecutionContext(state, builtins);
    }
    [Fact]
    public void CanonicalProviderHookIsAdmittedWithoutCallingIt()
    {
        var provider = new Provider(); PyProvider.Install(provider);
        try
        {
            var builtins = new PyDict(); builtins.Set("__build_class__", provider.Canonical);
            PyModuleInstances.ClassGuard(Context(builtins));
        }
        finally { PyProvider.Remove(provider); }
    }
    [Fact]
    public void ReplacementHookNeverGetsInvoked()
    {
        var provider = new Provider(); PyProvider.Install(provider);
        try
        {
            int called = 0;
            var replacement = PyNativeFunctions.CreateVariadic("__build_class__", _ => { called++; return PyNone.Instance; });
            var builtins = new PyDict(); builtins.Set("__build_class__", replacement);
            Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ClassGuard(Context(builtins)));
            Assert.Equal(0, called);
        }
        finally { PyProvider.Remove(provider); }
    }
    [Fact]
    public void MissingAndDisabledProofKeepTheExistingErrors()
    {
        var provider = new Provider { Enabled = false }; PyProvider.Install(provider);
        try
        {
            var builtins = new PyDict();
            Assert.Throws<PyNameError>(() => PyModuleInstances.ClassGuard(Context(builtins)));
            builtins.Set("__build_class__", provider.Canonical);
            Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ClassGuard(Context(builtins)));
            PyModuleInstances.ClassGuard(Context(PyModuleBuiltins.Default));
        }
        finally { PyProvider.Remove(provider); }
    }
}
