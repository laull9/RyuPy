// 原生调用现场只在provider冷路读取同一可信来源；定义点builtins与真实globals各自保持身份。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyNativeCallContextTests
{
    private const string File = "native_context.py";
    private static PyFunction Function(PyModuleExecutionContext context, object source)
    {
        var signature = PySignature.Create("read", [], [], []);
        PyOps.FunctionDefinitionWithSource(signature, "read", 1, source);
        return PyModuleInstances.CreateOwnedFunction(context, "read", signature,
            (_, _, _, _, _) => PyNone.Instance, [], false, null);
    }
    private static void Check(object origin, object globals, object builtins)
    {
        long depth = PyTraceback.PushFrameWithOrigin(File, "read", origin);
        try
        {
            Assert.Equal(1, PyTraceback.CompiledNativeCallSite(out object? actualGlobals,
                out object? actualBuiltins, out string? file));
            Assert.Same(globals, actualGlobals); Assert.Same(builtins, actualBuiltins); Assert.Equal(File, file);
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void FunctionsAndModuleSourcesRetainTheirOwnCapturedBuiltinsAfterRebindingAndDeletion()
    {
        var state = new PyModuleInstanceState(null);
        var first = new PyDict(); var second = new PyDict();
        var context = new PyModuleExecutionContext(state, first);
        object source = PyTraceback.CreateCodeSource("module", File, null);
        object module = PyTraceback.CreateModuleExecutionSourceWithCode(source, context);
        var earlier = Function(context, source);
        state.Set("__builtins__", second);
        var later = Function((PyModuleExecutionContext)PyModuleInstances.CaptureDefinitionContext(context), source);
        Check(module, state.View, first); Check(earlier, state.View, first); Check(later, state.View, second);
        state.DeleteIfPresent("__builtins__");
        Check(module, state.View, first); Check(earlier, state.View, first); Check(later, state.View, second);
        var inherited = Function((PyModuleExecutionContext)PyModuleInstances.CaptureDefinitionContext(
            PyFunctionOwner.GetContext(later)), source);
        Check(inherited, state.View, second);
    }

    [Fact]
    public void LegacyAndUnknownSourcesNeverAcquireBuiltinsFromMutableGlobals()
    {
        object code = PyTraceback.CreateCodeSource("module", File, _ => throw new Exception("不得调用getter"));
        long depth = PyTraceback.PushFrameWithOrigin(File, "module", PyTraceback.CreateModuleExecutionSourceWithCode(code, null));
        try
        {
            Assert.Equal(2, PyTraceback.CompiledNativeCallSite(out object? globals, out object? builtins, out _));
            Assert.Null(globals); Assert.Null(builtins);
        }
        finally { PyTraceback.PopTo(depth); }
        var state = new PyModuleInstanceState(null); state.Set("__builtins__", new PyDict());
        code = PyTraceback.CreateCodeSource("module", File, null);
        var old = new ModuleExecutionSource(state, (CompiledCodeSource)code, state.Globals, "module");
        depth = PyTraceback.PushFrameWithOrigin(File, "module", old);
        try
        {
            Assert.Equal(3, PyTraceback.CompiledNativeCallSite(out object? globals, out object? builtins, out _));
            Assert.Null(globals); Assert.Null(builtins);
            Assert.Equal(1, PyTraceback.CompiledNativeCallSite(out globals, out _)); // 旧两参ABI保原合同。
            Assert.Same(state.View, globals);
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void ExecutionRequestCarriesTheCapturedIdentityAndConsumesItsBodyExactlyOnce()
    {
        object provider = new(), globals = new(), builtins = new(); int calls = 0;
        var request = new PyCompiledExecutionRequest(provider, globals, builtins, File, () => calls++);
        Assert.Same(globals, request.Globals); Assert.Same(builtins, request.Builtins);
        Assert.False(request.TryConsume(new object(), out _));
        Assert.True(request.TryConsume(provider, out Action? body)); body!();
        Assert.Equal(1, calls); Assert.False(request.TryConsume(provider, out _));
    }

    [Fact]
    public void ColdFunctionSelectionReturnsBackendViewsWithoutReadingMutableDictionaryEntries()
    {
        var globals = new ViewNamespace(); var builtins = new ViewNamespace();
        var state = new PyModuleInstanceState(null, globals);
        var context = new PyModuleExecutionContext(state, builtins);
        object source = PyTraceback.CreateCodeSource("module", File, null);
        Check(Function(context, source), globals.ViewObject, builtins.ViewObject);
    }

    private sealed class ViewNamespace : IPyModuleNamespace
    {
        public object ViewObject { get; } = new();
        public bool TryGet(string name, out object? value) => throw new Exception("冷来源不得读取D中的当前键");
        public void Set(string name, object? value) => throw new Exception();
        public bool DeleteIfPresent(string name) => throw new Exception();
    }

    [Fact]
    public void PureCilOwnedFunctionCallsDoNotConsultTheOptionalExecutionProvider()
    {
        var provider = new CountingProvider(); PyProvider.Install(provider);
        try
        {
            var state = new PyModuleInstanceState(null); var builtins = new PyDict(); builtins.Set("value", 7L);
            var context = new PyModuleExecutionContext(state, builtins);
            var function = PyModuleInstances.CreateOwnedFunction(context, "read", PySignature.Create("read", [], [], []),
                (actual, _, _, _, _) => { Assert.Same(context, actual); return PyModuleInstances.LoadName(actual, "value"); },
                [], false, null);
            for (int index = 0; index < 10000; index++) Assert.Equal(7L, function.Invoke([], [], []));
            Assert.Equal(0, provider.Calls);
        }
        finally { PyProvider.Remove(provider); }
    }
    private sealed class CountingProvider : IPyModuleProvider, IPyCompiledExecutionProvider
    {
        internal int Calls;
        public void ExecuteCompiledModule(PyCompiledExecutionRequest request) { Calls++; throw new Exception(); }
        public bool Owns(string name) { Calls++; return false; }
        public object? SystemAttribute(string name) { Calls++; throw new Exception(); }
        public PyModule Import(string name) { Calls++; throw new Exception(); }
        public void Registered(string name) { Calls++; }
        public void Initializing(PyModule module) { Calls++; }
        public void Failed(string name) { Calls++; }
    }
}

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void NativeCallContextHasExactFourColdAbiEntriesIncludingThePreservedOverload()
    {
        string[] expected =
        [
            "M:RyuPy.Runtime.PyTraceback:CompiledNativeCallSite:150:[0;0;2](Object&,String&)->Int32:",
            "M:RyuPy.Runtime.PyTraceback:CompiledNativeCallSite:150:[0;0;3](Object&,Object&,String&)->Int32:",
            "M:RyuPy.Runtime.PyCompiledExecutionRequest:get_Builtins:2182:[32;0;0]()->Object:",
            "P:RyuPy.Runtime.PyCompiledExecutionRequest:Builtins:0:[40;0;0]()->Object",
        ];
        string[] actual = PublicAbi(typeof(PyTraceback).Assembly.Location).Where(item =>
            item.StartsWith("M:RyuPy.Runtime.PyTraceback:CompiledNativeCallSite:", StringComparison.Ordinal)
            || item.StartsWith("M:RyuPy.Runtime.PyCompiledExecutionRequest:get_Builtins:", StringComparison.Ordinal)
            || item.StartsWith("P:RyuPy.Runtime.PyCompiledExecutionRequest:Builtins:", StringComparison.Ordinal)).ToArray();
        Assert.Equal(4, actual.Length); Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
        Assert.False(typeof(PyCompiledExecutionRequest).GetProperty("Builtins")!.CanWrite);
    }
}
