// as_file的真实Path退化分支与contextlib公开协议；所有测试复用原生成器与异常上下文设施。
using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyResourcesAsFileTests
{
    private static object? Invoke(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);
    private static object? Call(object? obj, string name, params object?[] args) => Invoke(PyObject.GetAttr(obj, name), args);
    private static PyGeneratorContext Context(PyPath path) => Assert.IsType<PyGeneratorContext>(
        Invoke(PyImportlibResources.CreateAttributes()["as_file"], path));

    [Fact]
    public void ExportIsStableAndPathDirectoryAndAbsentPathPreserveIdentityWithoutIo()
    {
        Assert.Same(PyImportlibResources.CreateAttributes()["as_file"], PyImportlibResources.CreateAttributes()["as_file"]);
        string root = Path.Combine(Path.GetTempPath(), "ryupy-as-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string file = Path.Combine(root, "data.txt"); File.WriteAllText(file, "原始资源");
        try
        {
            foreach (string value in new[] { root, file, Path.Combine(root, "absent", "data.txt") })
            {
                var path = new PyPath(value); var context = Context(path);
                Assert.Same(path, Call(context, "__enter__"));
                Assert.Equal(false, Call(context, "__exit__", null, null, null));
                Assert.Equal(false, Call(context, "__exit__", null, null, null));
                Assert.Throws<PyAttributeError>(() => Call(context, "__enter__"));
            }
            Assert.Equal("原始资源", File.ReadAllText(file));
            Assert.False(Directory.Exists(Path.Combine(root, "absent")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ArgumentsPropertiesAndBoundMethodsDescribeActualProtocol()
    {
        var path = new PyPath("missing"); var context = Context(path);
        Assert.Same(path, Assert.IsType<PyTuple>(PyObject.GetAttr(context, "args")).Items[0]);
        Assert.Equal(0, Assert.IsType<PyDict>(PyObject.GetAttr(context, "kwds")).Count);
        Assert.Equal("_", PyObject.GetAttr(PyObject.GetAttr(context, "func"), "__name__"));
        Assert.IsType<string>(PyObject.GetAttr(context, "__doc__"));
        var generator = Assert.IsType<PyGenerator>(PyObject.GetAttr(context, "gen"));
        object? enter = PyObject.GetAttr(context, "__enter__"), exit = PyObject.GetAttr(context, "__exit__");
        Assert.Same(context, PyObject.GetAttr(enter, "__self__"));
        Assert.Same(context, PyObject.GetAttr(exit, "__self__"));
        Assert.True(PyHash.Equal(enter, PyObject.GetAttr(context, "__enter__")));
        Assert.False(PyHash.Equal(enter, exit));
        Assert.Equal(PyHash.Of(enter), PyHash.Of(PyObject.GetAttr(context, "__enter__")));
        Assert.Same(PyObject.GetAttr(enter, "__func__"), PyObject.GetAttr(PyObject.GetAttr(context, "__enter__"), "__func__"));
        Assert.Same(path, Invoke(enter));
        Assert.Same(generator, PyObject.GetAttr(context, "gen"));
        foreach (string name in new[] { "args", "kwds", "func" }) Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(context, name));
        Assert.Equal(false, PyOps.CallValue(exit, [], ["typ", "value", "traceback"], [null, null, null]));
    }

    [Fact]
    public void ExitPreservesOriginalExceptionIdentitiesIncludingStopIteration()
    {
        foreach (PyBaseException error in new PyBaseException[] {
            new PyValueError("body"), new PyStopIteration("body"), new PyRuntimeError("body"), new PyKeyboardInterrupt("body") })
        {
            var context = Context(new PyPath("missing"));
            Call(context, "__enter__");
            Assert.Equal(false, Call(context, "__exit__", PyObject.TypeOf(error), error, null));
        }
    }

    [Fact]
    public void OutOfOrderAndDirectGeneratorConsumptionUseContextlibErrors()
    {
        var context = Context(new PyPath("missing"));
        Assert.Equal("generator didn't stop", Assert.Throws<PyRuntimeError>(() => Call(context, "__exit__", null, null, null)).PyMessage);
        Assert.Equal("generator didn't yield", Assert.Throws<PyRuntimeError>(() => Call(context, "__enter__")).PyMessage);
        context = Context(new PyPath("missing"));
        Assert.IsType<PyGenerator>(PyObject.GetAttr(context, "gen")).Next(null, false);
        Assert.Equal("generator didn't yield", Assert.Throws<PyRuntimeError>(() => Call(context, "__enter__")).PyMessage);
    }

    [Fact]
    public void PublicDictionaryAndMutableGeneratorActuallyControlTheProtocol()
    {
        var original = new PyPath("one"); var replacement = new PyPath("two");
        var context = Context(original);
        var clone = Assert.IsType<PyGeneratorContext>(Call(context, "_recreate_cm"));
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(context, "__dict__"));
        attributes.Set("args", new PyTuple([replacement]));
        var recreated = Assert.IsType<PyGeneratorContext>(Call(context, "_recreate_cm"));
        Assert.Same(replacement, Call(recreated, "__enter__"));
        Call(recreated, "__exit__", null, null, null);
        PyObject.SetAttr(context, "gen", PyObject.GetAttr(clone, "gen"));
        Assert.Same(original, Call(context, "__enter__"));
        Call(context, "__exit__", null, null, null);
        Assert.False(attributes.Contains("args"));
        Assert.Equal("'_GeneratorContextManager' object has no attribute 'func'",
            Assert.Throws<PyAttributeError>(() => Call(context, "_recreate_cm")).PyMessage);
        context = Context(original);
        PyObject.DelAttr(context, "kwds");
        Assert.Equal("'_GeneratorContextManager' object has no attribute 'kwds'",
            Assert.Throws<PyAttributeError>(() => Call(context, "__enter__")).PyMessage);
        Assert.False(Assert.IsType<PyDict>(PyObject.GetAttr(context, "__dict__")).Contains("args"));
    }

    [Fact]
    public void DecoratorRecreatesAndDoesNotConsumeTemplate()
    {
        var context = Context(new PyPath("missing"));
        var function = PyNativeFunctions.Create("body", ["value"], [null], 1, a => a[0]);
        PyObject.SetAttr(function, "__qualname__", "body");
        object? decorated = Call(context, "__call__", function);
        Assert.Equal(1L, Invoke(decorated, 1L)); Assert.Equal(2L, Invoke(decorated, 2L));
        Assert.IsType<PyTuple>(PyObject.GetAttr(context, "args"));
        Call(context, "__enter__"); Call(context, "__exit__", null, null, null);
        Assert.Throws<PyAttributeError>(() => Invoke(decorated, 3L));
    }

    [Fact]
    public void InvalidInputsAndNonFilesystemTraversablesFailExplicitly()
    {
        Assert.Equal("as_file requires at least 1 positional argument",
            Assert.Throws<PyTypeError>(() => PyImportlibResources.AsFile([], ["path"], [new PyPath("x")])).PyMessage);
        foreach (object? value in new object?[] { 42L, "x", PyNone.Instance })
            Assert.Throws<PyAttributeError>(() => PyImportlibResources.AsFile([value], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyImportlibResources.AsFile([new Traversable()], [], []));
        var context = Context(new PyPath("x"));
        Assert.Equal("_GeneratorContextManager.__enter__() takes 1 positional argument but 2 were given",
            Assert.Throws<PyTypeError>(() => Call(context, "__enter__", 1L)).PyMessage);
        Assert.Equal("_GeneratorContextManager.__exit__() missing 3 required positional arguments: 'typ', 'value', and 'traceback'",
            Assert.Throws<PyTypeError>(() => Call(context, "__exit__")).PyMessage);
    }

    private sealed class Traversable : PyNativeObject
    {
        public override string TypeName => "Traversable";
        public override object? GetAttr(string name) => name == "is_dir"
            ? PyNativeFunctions.Create(name, [], [], 0, _ => false) : throw Missing(name);
    }
}
