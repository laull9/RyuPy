// 字面量**独立能力合同：mapping-only、逐项失败前缀、用户异常身份及自定义hash键精确拒绝。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyDictUnpackTests
{
    [Fact]
    public void NativeIteratorNamesUseOriginalContainerRatherThanClrCarrier()
    {
        Assert.Equal("list_iterator", PyRepr.TypeName(PyIter.Create(new PyList())));
        Assert.Equal("tuple_iterator", PyRepr.TypeName(PyIter.Create(new PyTuple([]))));
        Assert.Equal("dict_keyiterator", PyRepr.TypeName(PyIter.Create(new PyDict())));
        Assert.Equal("str_iterator", PyRepr.TypeName(PyIter.Create("text")));
        Assert.Equal("bytes_iterator", PyRepr.TypeName(PyIter.Create(new PyBytes([]))));
        Assert.Equal("bytearray_iterator", PyRepr.TypeName(PyIter.Create(new PyByteArray([]))));
    }

    private static PyInstance Mapping(Func<object?> keys, Func<object?, object?> get)
    {
        var cls = new PyClass("SpreadMapping");
        cls.SetAttr("keys", PyNativeFunctions.Create("keys", ["self"], [null], 1, _ => keys()));
        cls.SetAttr("__getitem__", PyNativeFunctions.Create("__getitem__", ["self", "key"], [null, null], 2, args => get(args[1])));
        return new PyInstance(cls);
    }

    [Fact]
    public void PairsIterableCannotPassAsMapping()
    {
        var target = new PyDict();
        var source = new PyList([new PyTuple(["key", 1L])]);
        Assert.Equal("'list' object is not a mapping", Assert.Throws<PyTypeError>(() => PyOps.DictUnpackMapping(target, source)).Message);
        Assert.Equal(0, target.Count);
    }

    [Fact]
    public void FailedGetItemPreservesCommittedPrefixAndSameUserException()
    {
        var events = new List<string>();
        var marker = new PyLookupError("second key failed");
        var source = Mapping(() => { events.Add("keys"); return new PyList(["first", "second", "third"]); }, key =>
        {
            events.Add((string)key!);
            if (Equals(key, "second")) throw marker;
            return 7L;
        });
        var target = new PyDict();
        target.Set("prefix", 1L);
        Assert.Same(marker, Assert.Throws<PyLookupError>(() => PyOps.DictUnpackMapping(target, source)));
        Assert.Equal(new[] { "keys", "first", "second" }, events);
        Assert.Equal(2, target.Count);
        Assert.Equal(7L, target.Get("first"));
        Assert.Equal(1L, target.Get("prefix"));
    }

    [Fact]
    public void KeysBodyTypeErrorRemainsOriginalAndInvalidKeysIteratorIsRewritten()
    {
        var marker = new PyTypeError("keys body");
        var failed = Mapping(() => throw marker, _ => 0L);
        Assert.Same(marker, Assert.Throws<PyTypeError>(() => PyOps.DictUnpackMapping(new PyDict(), failed)));
        var invalid = Mapping(() => 4L, _ => 0L);
        Assert.Equal("SpreadMapping.keys() returned a non-iterable (type int)",
            Assert.Throws<PyTypeError>(() => PyOps.DictUnpackMapping(new PyDict(), invalid)).Message);
    }

    [Fact]
    public void StoredCustomHashIsNotRecomputedBeforeExplicitUnsupportedCopy()
    {
        int calls = 0;
        var cls = new PyClass("StoredHashKey");
        cls.SetAttr("__hash__", PyNativeFunctions.Create("__hash__", ["self"], [null], 1, _ => { calls++; return 37L; }));
        var key = new PyInstance(cls);
        var source = new PyDict();
        source.Set(key, 1L);
        Assert.Equal(1, calls);
        calls = 0;
        Assert.Throws<PyNotImplementedError>(() => PyOps.DictUnpackMapping(new PyDict(), source));
        Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => PyOps.DictUnpackMapping(source, new PyDict()));
        Assert.Equal(0, calls);
    }
}
