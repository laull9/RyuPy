// count 的真实导出与消费者合同；旧工具、map、生成器及关闭 IO 作为共享入口守卫。
using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCountProtocolTests
{
    private static PyNativeType Root => Assert.IsType<PyNativeType>(PyItertools.CreateAttributes()["count"]);
    private static object New(params object?[] args) => Call(Root, args)!;
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);
    private static object Tool(string name, params object?[] args) => Call(PyItertools.CreateAttributes()[name], args)!;

    [Fact]
    public void ExportIsARealStableTypeWithExactCarrierAcceptance()
    {
        PyNativeType root = Root; object value = New(10L, 2L);
        Assert.Same(root, Root); Assert.Same(PyType.Get("type"), PyObject.TypeOf(root));
        Assert.Equal("count", root.GetAttr("__name__")); Assert.Equal("count", root.GetAttr("__qualname__"));
        Assert.Equal("itertools", root.GetAttr("__module__")); Assert.Equal("<class 'itertools.count'>", PyRepr.Repr(root));
        Assert.Same(root, PyObject.TypeOf(value)); Assert.Same(root, PyObject.GetAttr(value, "__class__"));
        Assert.Equal(true, Builtin("isinstance", value, root)); Assert.Equal(true, Builtin("isinstance", value, PyType.Get("object")));
        Assert.Equal(true, Builtin("issubclass", root, PyType.Get("object"))); Assert.Equal(true, Builtin("issubclass", root, root));
        Assert.False(root.Accepts(new Pretender())); Assert.False(root.Accepts(Tool("repeat", 1L)));
    }

    [Fact]
    public void SavedRootSurvivesModuleRebindingAndNativeRegistryNameCollisions()
    {
        PyNativeModules.Register(); PyModule module = PyModules.Import("itertools");
        object? original = module.GetAttr("count"); PyNativeType saved = Assert.IsType<PyNativeType>(original);
        object value = Call(saved, 30L)!;
        // 仅测试改全局注册表后仍使用真实 root，finally 恢复原表，避免污染其它原生类型测试。
        var registry = (ConcurrentDictionary<string, PyNativeType>)typeof(PyNativeType)
            .GetField("Types", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        registry.TryGetValue("count", out PyNativeType? shortBefore);
        registry.TryGetValue("itertools.count", out PyNativeType? fullBefore);
        try
        {
            module.SetAttr("count", "replacement"); Assert.Equal("replacement", module.GetAttr("count"));
            _ = new PyNativeType("count", "unrelated", null, _ => false);
            _ = new PyNativeType("itertools.count", "unrelated", null, _ => false);
            Assert.Same(saved, PyObject.TypeOf(value)); Assert.Same(saved, PyObject.GetAttr(value, "__class__"));
            Assert.Equal(true, Builtin("isinstance", value, saved)); Assert.Equal(30L, Builtin("next", value));
            object later = Call(saved, 40L)!;
            Assert.Same(saved, PyObject.TypeOf(later)); Assert.Equal(40L, Builtin("next", later));
        }
        finally
        {
            module.SetAttr("count", original);
            if (shortBefore is null) registry.TryRemove("count", out _); else registry["count"] = shortBefore;
            if (fullBefore is null) registry.TryRemove("itertools.count", out _); else registry["itertools.count"] = fullBefore;
        }
    }

    [Fact]
    public void IterNextExplicitMethodsAndLoopAbiShareOneState()
    {
        object value = New(10L, 2L);
        Assert.Same(value, Builtin("iter", value)); Assert.Same(value, PyIter.Create(value));
        Assert.Same(value, Call(PyObject.GetAttr(value, "__iter__")));
        object? read = PyObject.GetAttr(value, "__next__");
        Assert.Equal(10L, Builtin("next", value)); Assert.Equal(12L, Call(read));
        Assert.Equal(14L, Builtin("next", value, "fallback"));
        object loop = PyOps.IterStart(value); Assert.Same(value, loop);
        Assert.True(PyOps.IterNextOk(loop)); Assert.Equal(16L, PyOps.IterValue(loop));
        Assert.Equal(18L, Call(read)); Assert.Equal("count(20, 2)", PyRepr.Repr(value));
    }

    [Fact]
    public void ExplicitMethodErrorsDoNotAdvanceAndHaveNoPublicCounterFieldsOrLength()
    {
        object value = New(100L);
        foreach (string name in new[] { "__iter__", "__next__" })
        {
            object? method = PyObject.GetAttr(value, name);
            Assert.Throws<PyTypeError>(() => Call(method, 1L));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, [], ["bad"], [1L]));
            Assert.Equal("count(100)", PyRepr.Repr(value));
        }
        foreach (string name in new[] { "start", "step", "__length_hint__" })
        {
            string message = $"'itertools.count' object has no attribute '{name}'";
            Assert.Equal(message, Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, name)).PyMessage);
            Assert.Equal(message, Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, name, 1L)).PyMessage);
            Assert.Equal(message, Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(value, name)).PyMessage);
        }
        Assert.Equal("object of type 'itertools.count' has no len()",
            Assert.Throws<PyTypeError>(() => Builtin("len", value)).PyMessage);
        Assert.Equal(100L, Builtin("next", value));
    }

    [Fact]
    public void IsliceZeroDoesNotAdvanceAndBoundedConsumptionNeverClosesCount()
    {
        object value = New(5L, -2L);
        Assert.Empty(PyOps.ToSequence(Tool("islice", value, 0L)));
        Assert.Equal(5L, Builtin("next", value));
        Assert.Equal<object?>([3L, 1L, -1L], PyOps.ToSequence(Tool("islice", value, 3L)));
        Assert.Equal(-3L, Builtin("next", value));
        object zero = New(BigInteger.Pow(10, 80), 0L);
        Assert.Equal(4, PyOps.ToSequence(Tool("islice", zero, 4L)).Length);
        Assert.Equal(BigInteger.Pow(10, 80), Builtin("next", zero));
    }

    [Fact]
    public void ExistingToolsAndMapKeepTheirOriginalBoundaries()
    {
        var item = new PyList([1L]);
        Assert.All(PyOps.ToSequence(Tool("repeat", item, 3L)), value => Assert.Same(item, value));
        Assert.Empty(PyOps.ToSequence(Tool("repeat", item, -1L)));
        object chain = Tool("chain", new PyTuple([1L]), new PyTuple([2L]));
        Assert.Equal<object?>([1L, 2L], PyOps.ToSequence(chain));
        Assert.Equal("done", Builtin("next", chain, "done"));
        object[] args = [new PyTuple([1L, 2L]), new PyTuple([3L])];
        object longest = Tool("zip_longest", args);
        Assert.Equal<object?>([1L, 3L], Assert.IsType<PyTuple>(Builtin("next", longest)).Items);
        Assert.Equal<object?>([2L, PyNone.Instance], Assert.IsType<PyTuple>(Builtin("next", longest)).Items);
        Assert.Equal("done", Builtin("next", longest, "done"));
        var identity = PyNativeFunctions.Create("identity", ["x"], [null], 1, values => values[0]);
        object map = Builtin("map", identity, new PyTuple([7L, 8L]))!;
        Assert.Equal(7L, Builtin("next", map)); Assert.Equal(8L, Builtin("next", map));
        Assert.Equal("done", Builtin("next", map, "done"));
    }

    [Fact]
    public void GeneratorTerminationAndClosedIoIterationAreStillChecked()
    {
        int calls = 0;
        var generator = new PyGenerator(raw =>
        {
            calls++; var frame = (object?[])raw;
            frame[0] = calls == 1 ? 1L : -1L;
            return calls == 1 ? 9L : null;
        }, [0L, null, null, null], "count_guard_generator.py", "values");
        Assert.Same(generator, Builtin("iter", generator)); Assert.Equal(9L, Builtin("next", generator));
        Assert.Equal("done", Builtin("next", generator, "done"));
        Assert.Equal("done", Builtin("next", generator, "done")); Assert.Equal(2, calls);
        var stream = Assert.IsAssignableFrom<PyIOStream>(PyIO.CreateStringIO(["a\nb\n"], [], []));
        Assert.Same(stream, Builtin("iter", stream)); Assert.Equal("a\n", Builtin("next", stream));
        stream.Close();
        Assert.Throws<PyValueError>(() => Builtin("iter", stream));
        Assert.Throws<PyValueError>(() => Builtin("next", stream, "fallback"));
    }

    [Fact]
    public void UnsupportedReflectionAndMutationDoNotPretendToSucceed()
    {
        object value = New(10L, 2L);
        foreach (string name in new[] { "__reduce__", "__reduce_ex__", "__setstate__" })
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(value, name));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(Root));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(value, "__class__", PyType.Get("object")));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "__next__"));
        Assert.Throws<PyNotImplementedError>(() => Call(PyCopy.CreateAttributes()["copy"], value));
        Assert.Throws<PyNotImplementedError>(() => Call(PyCopy.CreateAttributes()["deepcopy"], value));
        Assert.Same(Root, PyObject.GetAttr(value, "__class__")); Assert.Equal(10L, Builtin("next", value));
    }

    [Fact]
    public void OwnErrorsKeepCallerFramesAndAmbientContextWithoutAnArtificialCountFrame()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("count_contract.py", "caller"); PyTraceback.SetLine(31);
            PyTypeError binding = Assert.Throws<PyTypeError>(() => PyOps.CallValue(Root, [], ["bad"], [1L]));
            PyAttributeError missing = Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(New(), "step"));
            PyOverflowError overflow = Assert.Throws<PyOverflowError>(() => Builtin("next", New(BigInteger.Pow(10, 400), 1.0)));
            foreach (PyBaseException error in new PyBaseException[] { binding, missing, overflow })
            {
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("count_contract.py", "caller", 31), Assert.Single(error.Frames));
            }
            Assert.Equal(1L, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    private sealed class Pretender : PyToolIterator
    {
        public override string TypeName => "itertools.count";
        public override bool MoveNext() => true;
        public override object? Current() => 1L;
    }
}
