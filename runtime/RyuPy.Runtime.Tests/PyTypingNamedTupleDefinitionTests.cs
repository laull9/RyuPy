// 真实root/单父来源及namespace终结；宿主来源测试不冒源码应用验证。
// 所有typing/collections修改finally恢复，不执行CP源码或伪造annotation回调成功域。
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingNamedTupleDefinitionTests
{
    private static readonly Type Implementation = typeof(PyClass).Assembly.GetType("RyuPy.Runtime.PyTypingNamedTuple")!;
    internal static PyFunction Root => (PyFunction)Implementation.GetProperty("Root", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
    internal static Dictionary<string, object?> Typing => PyTyping.CreateAttributes();
    internal static object? Get(object? value, string name) => PyObject.GetAttr(value, name);
    internal static object? Call(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);
    internal static PyClass Begin(string name = "Record") => (PyClass)PyOps.MakeClass(name, [Root], null);
    internal static PyDict Annotations(PyClass cls) => (PyDict)PyOps.ClassNamespaceGet(cls, "__annotations__")!;
    internal static void WithRoot(Action action)
    {
        PyNativeModules.Register();
        bool existed = Typing.TryGetValue("NamedTuple", out object? old);
        Typing["NamedTuple"] = Root;
        try { action(); }
        finally { if (existed) Typing["NamedTuple"] = old; else Typing.Remove("NamedTuple"); }
    }
    internal static void InModule(Func<object?> moduleName, Action action)
    {
        string key = "namedtuple_definition_" + Guid.NewGuid().ToString("N");
        PyModules.Register(key, "", "namedtuple_definition.py", () => { }, _ => moduleName(), (_, _, _) => { });
        long depth = PyTraceback.CaptureDepth();
        try
        {
            PyTraceback.PushFrameWithOrigin("namedtuple_definition.py", "<module>", PyTraceback.CreateModuleExecutionSource(key));
            action();
        }
        finally { PyTraceback.PopTo(depth + 1); }
    }
    internal static void Supported(Action action) => WithRoot(() => InModule(() => "source_module", action));

    [Fact]
    public void PublicRootIsRealFunctionWithFunctionalCallAndFiniteMroBoundary()
    {
        PyNativeModules.Register();
        Assert.Same(Root, Typing["NamedTuple"]);
        PyFunction root = Root;
        Assert.Same(Root, Typing["NamedTuple"]);
        Assert.Same(PyType.Get("function"), PyObject.TypeOf(root));
        Assert.Equal("typing", Get(root, "__module__"));
        Assert.IsType<PyFunction>(Get(root, "__mro_entries__"));
        InModule(() => "source_module", () =>
        {
            PyClass empty = Assert.IsType<PyClass>(Call(root, "X", new PyList()));
            Assert.Empty(Assert.IsAssignableFrom<PyTuple>(empty.Invoke([], [], [])).Items);
        });
        Assert.Throws<PyTypeError>(() => Call(root));
        WithRoot(() => Assert.Throws<PyNotImplementedError>(() => Call(Get(root, "__mro_entries__"), new PyTuple([root]))));
        Assert.Same(Root, Typing["NamedTuple"]);
    }

    [Fact]
    public void CurrentRootAndMroRebindingAreCheckedBeforeCreatingNamespace()
    {
        Supported(() =>
        {
            PyFunction root = Root;
            Typing["NamedTuple"] = PyBaseObject.Create([], []);
            Assert.Throws<PyAssertionError>(() => Begin());
            Typing["NamedTuple"] = root;
            object? mro = Get(root, "__mro_entries__");
            try
            {
                PyObject.SetAttr(root, "__mro_entries__", PyNone.Instance);
                Assert.Throws<PyNotImplementedError>(() => Begin());
            }
            finally { PyObject.SetAttr(root, "__mro_entries__", mro); }
            PyClass cls = Begin();
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.IsAssignableFrom<PyTuple>(cls.Invoke([], [], []));
        });
    }

    [Fact]
    public void ClassSourceUsesCurrentGlobalsAndPreservesExplicitNonNoneModule()
    {
        WithRoot(() =>
        {
            object? current = "before";
            InModule(() => current, () =>
            {
                current = "after";
                PyClass first = Begin("First");
                current = "later";
                PyOps.FinalizeOrdinaryClass(first);
                Assert.Equal("after", Get(first, "__module__"));
                var token = new PyList();
                PyClass second = Begin("Second");
                second.SetAttr("__module__", token);
                second.SetAttr("__doc__", "custom doc");
                second.SetAttr("__qualname__", "Outer.Second");
                second.SetAttr("__orig_bases__", new PyTuple([PyType.Get("int")]));
                PyOps.FinalizeOrdinaryClass(second);
                Assert.Same(token, Get(second, "__module__"));
                Assert.Equal("custom doc", Get(second, "__doc__"));
                Assert.Equal("Outer.Second", Get(second, "__qualname__"));
                Assert.Same(Root, Assert.IsType<PyTuple>(Get(second, "__orig_bases__")).Items[0]);
            });
        });
    }

    [Fact]
    public void EmptyModuleIsPreservedButNoneFallbackIsExplicitlyUnsupported()
    {
        Supported(() =>
        {
            PyClass empty = Begin("Empty");
            empty.SetAttr("__module__", "");
            PyOps.FinalizeOrdinaryClass(empty);
            Assert.Equal("", Get(empty, "__module__"));
            PyClass none = Begin("NoneModule");
            none.SetAttr("__module__", PyNone.Instance);
            Assert.Contains("module=None", Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(none)).Message);
            Assert.Throws<PyNotImplementedError>(() => none.Invoke([], [], []));
        });
    }

    [Fact]
    public void FinalAnnotationsDefineFieldsAndFreshSharedTypedMetadata()
    {
        Supported(() =>
        {
            PyClass cls = Begin();
            var input = new PyDict();
            input.Set("x", PyType.Get("int"));
            input.Set("y", "Missing");
            input.Set("x", PyType.Get("str"));
            var shared = new PyList();
            cls.SetAttr("__annotations__", input);
            cls.SetAttr("y", shared);
            cls.SetAttr("ordinary", 12L);
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.Equal<object?>(["x", "y"], Assert.IsType<PyTuple>(Get(cls, "_fields")).Items);
            var output = Assert.IsType<PyDict>(Get(cls, "__annotations__"));
            Assert.NotSame(input, output);
            Assert.Same(output, Get(Get(cls, "__new__"), "__annotations__"));
            Assert.Same(PyType.Get("str"), output.Get("x"));
            Assert.Equal("Missing", Get(output.Get("y"), "__forward_arg__"));
            output.Set("extra", PyType.Get("float"));
            Assert.Equal(2, Assert.IsType<PyTuple>(Get(cls, "_fields")).Length);
            var instance = Assert.IsAssignableFrom<PyTuple>(cls.Invoke([9L], [], []));
            Assert.Same(shared, Get(instance, "y"));
            Assert.Equal(12L, Get(instance, "ordinary"));
            Assert.Same(shared, Assert.IsType<PyTuple>(Get(Get(cls, "__new__"), "__defaults__")).Items[0]);
        });
    }

    [Fact]
    public void EmptyDefaultsAndSchemaLocalNameIdentityArePreserved()
    {
        Supported(() =>
        {
            string typeName = new(new[] { 'S', 'a', 'm', 'e' });
            string fieldName = new(new[] { 'S', 'a', 'm', 'e' });
            Assert.NotSame(typeName, fieldName);
            PyClass cls = Begin(typeName);
            Annotations(cls).Set(fieldName, PyType.Get("int"));
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.Same(typeName, Assert.IsType<PyTuple>(Get(cls, "_fields")).Items[0]);
            Assert.Empty(Assert.IsType<PyTuple>(Get(Get(cls, "__new__"), "__defaults__")).Items);
            PyOps.DeclareDataclass(cls, ["Same"], false);
            Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(cls, false));
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(cls));
            var collection = (PyClass)PyOps.CallValue(PyModules.Import("collections").GetAttr("namedtuple"),
                ["Plain", "x"], ["module"], ["source_module"])!;
            Assert.Throws<PyNotImplementedError>(() => PyOps.DeclareDataclass(collection, ["x"], false));
        });
    }

    [Fact]
    public void OrdinarySameNameBaseDoesNotEnterTypingProtocol()
    {
        var ordinary = new PyClass("NamedTuple");
        PyOps.FinalizeOrdinaryClass(ordinary);
        PyClass child = (PyClass)PyOps.MakeClass("Child", [ordinary], null);
        PyOps.FinalizeOrdinaryClass(child);
        Assert.IsType<PyInstance>(child.Invoke([], [], []));
    }
}
