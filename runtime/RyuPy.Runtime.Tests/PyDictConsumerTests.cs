// dict 派生消费者各自选择公开协议：JSON/items、Counter空根复制、proxy委托不能互相套用。
using System;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictConsumerTests
{
    private static PyClass Class(Action<PyClass>? configure = null)
    {
        var cls = (PyClass)PyOps.MakeClass("ConsumerDict", [PyType.Get("dict")], null);
        configure?.Invoke(cls); PyOps.FinalizeOrdinaryClass(cls); return cls;
    }
    private static PyInstance New(PyClass cls, bool filled = true)
    {
        var source = new PyDict(); if (filled) source.Set("real", 2L);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke([source], [], []));
    }
    private static PyFunction Function(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static object? Method(object value, string name, params object?[] args) => PyOps.CallMethod(value, name, args, [], []);
    private static object? Json(object value, string[]? names = null, object?[]? args = null) =>
        PyOps.CallValue(PyJsonModule.CreateAttributes()["dumps"], [value], names ?? [], args ?? []);

    [Fact]
    public void JsonUsesPublicItemsOnlyForNonEmptyDictionary()
    {
        int calls = 0;
        var cls = Class(c => c.SetAttr("items", Function("items", _ =>
        { calls++; return new PyList([new PyTuple(["virtual", 3L])]); })));
        var value = New(cls);
        Assert.Equal("{\"virtual\": 3}", Json(value)); Assert.Equal(1, calls);
        Assert.Equal("{\n \"virtual\": 3\n}", Json(value, ["indent"], [1L])); Assert.Equal(2, calls);
        Assert.Equal("{}", Json(New(cls, false))); Assert.Equal(2, calls);
    }

    [Fact]
    public void JsonKeepsOwnerCyclesAndLiveDefaultItems()
    {
        var value = New(Class()); PyOps.SetItem(value, "self", value);
        Assert.Equal("Circular reference detected", Assert.Throws<PyValueError>(() => Json(value)).Message);
        PyOps.DelItem(value, "self"); Assert.Equal("{\"real\": 2}", Json(value));
    }

    [Fact]
    public void JsonPropagatesSameItemsFailureAndSupportsSortedOverrides()
    {
        var failure = new PyValueError("items failed");
        var cls = Class(c => c.SetAttr("items", Function("items", _ => throw failure)));
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Json(New(cls))));
        cls.SetAttr("items", Function("items", _ => new PyList([new PyTuple(["b", 2L]), new PyTuple(["a", 1L])])));
        Assert.Equal("{\"a\": 1, \"b\": 2}", Json(New(cls), ["sort_keys"], [true]));
    }

    [Fact]
    public void CounterEmptyCopiesRawButNonEmptyAndSubtractUseItems()
    {
        int calls = 0;
        var cls = Class(c => c.SetAttr("items", Function("items", _ =>
        { calls++; return new PyList([new PyTuple(["virtual", 3L])]); })));
        var source = New(cls); var empty = new PyCounter(); Method(empty, "update", source);
        Assert.Equal(0, calls); Assert.Equal(2L, empty.Dictionary.Get("real"));
        Method(empty, "update", source); Assert.Equal(1, calls); Assert.Equal(3L, empty.Dictionary.Get("virtual"));
        var negative = new PyCounter(); Method(negative, "subtract", source);
        Assert.Equal(2, calls); Assert.Equal(-3L, negative.Dictionary.Get("virtual"));
    }

    [Fact]
    public void ProxyMethodsRemainPublicWhileItemUsesMissing()
    {
        var cls = Class(c =>
        {
            c.SetAttr("get", Function("get", _ => 7L));
            c.SetAttr("keys", Function("keys", _ => new PyList(["virtual"])));
            c.SetAttr("__missing__", Function("__missing__", _ => "fallback"));
        });
        var source = New(cls); var proxy = PyMappingProxy.Create([source], [], []);
        Assert.Equal(7L, Method(proxy, "get", "real"));
        Assert.Equal("['virtual']", PyRepr.Repr(Method(proxy, "keys")));
        Assert.Equal("fallback", PyOps.GetItem(proxy, "absent"));
        Assert.Equal(new object?[] { "real" }, PyOps.ToSequence(proxy));
        Assert.True(PyOps.Contains(proxy, "real")); Assert.False(PyOps.Contains(proxy, "absent"));
    }

    [Fact]
    public void PercentUsesRootMappingSlotAndMissing()
    {
        var source = New(Class(c => c.SetAttr("__missing__", Function("__missing__", _ => "fallback"))));
        Assert.Equal("fallback", PyPercentFormat.Format("%(absent)s", source));
        Assert.Equal("plain", PyPercentFormat.Format("plain", source));
    }

    [Fact]
    public void CopyAlgorithmsRejectRatherThanDiscardSubclassState()
    {
        var source = New(Class());
        foreach (string name in new[] { "copy", "deepcopy" })
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCopy.CreateAttributes()[name], [source], [], []));
        Assert.IsType<PyDict>(Method(source, "copy"));
    }
}
