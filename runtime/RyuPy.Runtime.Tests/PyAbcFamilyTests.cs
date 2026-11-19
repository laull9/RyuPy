// ABC身份、父图、抽象冻结和view槽位回归；只通过真实canonical类，不接受同名代理。
using System;
using System.IO;
using System.Linq;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyAbcFamilyTests
{
    private static PyClass Root(string name) => Assert.IsType<PyClass>(PyAbc.CreateAttributes()[name]);
    private static object? Check(string name, object? value, object? type) => PyOps.CallBuiltin(name, [value, type], [], []);
    [Fact]
    public void CanonicalGraphAndTypingRemainRealWhenExportIsRebound()
    {
        var sequence = Root("Sequence");
        Assert.Equal(new[] { "Sequence", "Reversible", "Collection", "Sized", "Iterable", "Container", "object" },
            sequence.MroTuple().Items.Select(x => x is PyClass c ? c.Name : ((PyType)x!).Name));
        Assert.Same(PyBaseObject.Type, Root("Iterable").DirectBasesTuple().Items.Single());
        Assert.Equal(true, Check("isinstance", new PyList([]), sequence));
        object alias = PyOps.GetItem(sequence, PyType.Get("int"))!;
        Assert.Same(sequence, PyObject.GetAttr(alias, "__origin__"));
        Assert.Throws<PyTypeError>(() => PyOps.GetItem(Root("Sized"), PyType.Get("int")));
        Assert.Throws<PyTypeError>(() => PyOps.GetItem(Root("Callable"), PyType.Get("int")));
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Root("Callable"), new PyTuple([new PyList([PyType.Get("int")]), PyType.Get("str")])));
        var exports = PyAbc.CreateAttributes(); object? saved = exports["Sequence"];
        try
        {
            exports["Sequence"] = new PyClass("Sequence");
            Assert.Same(sequence, PyAbc.RootType("Sequence"));
            Assert.Same(sequence, PyTyping.Origin("Sequence"));
            Assert.Equal(true, Check("issubclass", PyType.Get("list"), sequence));
        }
        finally { exports["Sequence"] = saved; }
        string? root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "tests/fixtures/abc/canonical_names.txt"))) root = Path.GetDirectoryName(root);
        Assert.NotNull(root);
        Assert.Equal(File.ReadAllLines(Path.Combine(root, "tests/fixtures/abc/canonical_names.txt")).Order(), exports.Keys.Order());
    }
    [Fact]
    public void AbstractnessIsFrozenAndUserSubclassRemainsNominal()
    {
        var cls = (PyClass)PyOps.PrepareOrdinaryMiClass("Numbers", [Root("Sequence")]);
        PyAbc.Method(cls, "__len__", ["self"], _ => 3L);
        PyAbc.Method(cls, "__getitem__", ["self", "index"], a => PyOps.GetItem(new PyTuple([10L, 20L, 30L]), a[1]));
        PyOps.FinalizeOrdinaryClass(cls);
        object? value = cls.Invoke([], [], []);
        Assert.Equal(new object?[] { 10L, 20L, 30L }, PyOps.ToSequence(value));
        object? iterator = PyAbc.Call(value, "__iter__");
        foreach (string control in new[] { "send", "throw", "close" })
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(iterator, control));
        Assert.Equal(2L, PyAbc.Call(value, "index", 30L));
        Assert.Equal(1L, PyAbc.Call(value, "count", 20L));
        Assert.Equal(false, Check("isinstance", new PyList([]), cls));
        Assert.Equal(false, Check("issubclass", PyType.Get("list"), cls));
        var incomplete = (PyClass)PyOps.PrepareOrdinaryMiClass("Incomplete", [Root("Sequence")]);
        PyOps.FinalizeOrdinaryClass(incomplete);
        Assert.Throws<PyTypeError>(() => incomplete.Invoke([], [], []));
        PyAbc.Method(incomplete, "__len__", ["self"], _ => 0L);
        PyAbc.Method(incomplete, "__getitem__", ["self", "index"], _ => throw new PyIndexError("end"));
        Assert.Throws<PyTypeError>(() => incomplete.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => incomplete.SetAttr("__abstractmethods__", new PyFrozenSet([])));
    }
    [Fact]
    public void ConcreteIntermediateRebindingDoesNotReviveAncestorAbstractness()
    {
        var iterable = Root("Iterable"); object? original = iterable.Attributes["__iter__"];
        var concrete = (PyClass)PyOps.PrepareOrdinaryMiClass("Concrete", [iterable]);
        PyAbc.Method(concrete, "__iter__", ["self"], _ => PyIter.Create(new PyTuple([])));
        PyOps.FinalizeOrdinaryClass(concrete); concrete.SetAttr("__iter__", original);
        var child = (PyClass)PyOps.PrepareOrdinaryMiClass("Child", [concrete]); PyOps.FinalizeOrdinaryClass(child);
        Assert.Empty(child.AbcAbstractNames!); Assert.NotNull(child.Invoke([], [], []));
        var extra = (PyClass)PyOps.PrepareOrdinaryMiClass("Extra", [concrete]); extra.SetAttr("extra", original); PyOps.FinalizeOrdinaryClass(extra);
        Assert.Equal(new[] { "extra" }, extra.AbcAbstractNames); Assert.Throws<PyTypeError>(() => extra.Invoke([], [], []));
        var ordinary = (PyClass)PyOps.MakeClass("Ordinary", [], null); ordinary.SetAttr("extra", original); PyOps.FinalizeOrdinaryClass(ordinary);
        Assert.Null(ordinary.AbcAbstractNames); Assert.NotNull(ordinary.Invoke([], [], []));
    }
    [Fact]
    public void ViewsKeepMappingLiveAndOwnRealMappingSlot()
    {
        var map = new PyDict(); map.Set("a", 1L);
        var items = (PyInstance)Root("ItemsView").Invoke([map], [], [])!;
        var keys = (PyInstance)Root("KeysView").Invoke([map], [], [])!;
        var values = (PyInstance)Root("ValuesView").Invoke([map], [], [])!;
        Assert.Same(map, PyObject.GetAttr(items, "_mapping")); Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(items, "__dict__"));
        map.Set("b", 2L); Assert.Equal(2L, PyOps.Len(items));
        Assert.Equal(new object?[] { "a", "b" }, PyOps.ToSequence(keys)); Assert.Equal(new object?[] { 1L, 2L }, PyOps.ToSequence(values));
        Assert.True(PyOps.Contains(items, new PyTuple(["b", 2L]))); Assert.False(PyOps.Contains(items, new PyTuple(["b", 3L])));
        Assert.Equal(true, Check("isinstance", items, Root("Set")));
    }
}
