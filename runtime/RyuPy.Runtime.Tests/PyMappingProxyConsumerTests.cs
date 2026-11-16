using System;
using System.Collections.Generic;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>B2 七组消费者合同：从公开运行时入口经过真实绑定，不执行或伪造 CP 参考。</summary>
[Collection("生成器帧栈")]
public class PyMappingProxyConsumerTests
{
    private static PyMappingProxy Proxy(object mapping) =>
        Assert.IsType<PyMappingProxy>(PyMappingProxy.Create([mapping], [], []));
    private static object? Method(object value, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);
    private static PyFunction Slot(string name, string[] parameters, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, new object?[parameters.Length], parameters.Length, body);
    private static PyMappingProxy Mapping(Func<object?> keys, Func<object?, object?> item,
        Func<object?>? items = null)
    {
        var cls = new PyClass("ConsumerMapping");
        cls.SetAttr("keys", Slot("keys", ["self"], _ => keys()));
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], a => item(a[1])));
        if (items is not null) cls.SetAttr("items", Slot("items", ["self"], _ => items()));
        return Proxy(new PyInstance(cls));
    }
    private static object? Keywords(object?[] names, object?[] values) =>
        PyOps.CallValue(PyNativeFunctions.Create("capture", ["kw"], [null], 0,
            a => a[0], ["varkw"]), [], names, values);

    [Fact]
    public void ExactListKeysRemainLiveAndCopiesAreShallow()
    {
        var keys = new PyList(["a"]); var alias = new PyList([1L]);
        var log = new List<string>();
        PyMappingProxy proxy = Mapping(() => { log.Add("keys"); return keys; }, key =>
        {
            log.Add((string)key!);
            if ((string)key! == "a") keys.Items.Add("b");
            return alias;
        });
        PyDict copy = Assert.IsType<PyDict>(PyOps.CallBuiltin("dict", [proxy], [], []));
        Assert.Equal(new[] { "keys", "a", "b" }, log);
        Assert.Same(alias, copy.Get("a")); Assert.Same(alias, copy.Get("b"));
    }

    [Fact]
    public void NonListKeysFinishBeforeReadsAndFailurePreservesPartialUpdate()
    {
        var log = new List<string>(); int cursor = 0;
        var iteratorClass = new PyClass("ConsumerKeysIterator");
        var iterator = new PyInstance(iteratorClass);
        iteratorClass.SetAttr("__iter__", Slot("__iter__", ["self"], _ => { log.Add("iter"); return iterator; }));
        iteratorClass.SetAttr("__next__", Slot("__next__", ["self"], _ =>
        {
            log.Add("next" + cursor);
            return cursor++ switch { 0 => "a", 1 => "b", _ => throw new PyStopIteration() };
        }));
        var original = new PyValueError("read failed");
        var target = new PyDict();
        PyMappingProxy source = Mapping(() => iterator, key =>
        {
            log.Add("get:" + key);
            return (string)key! == "a" ? 7L : throw original;
        });
        Assert.Same(original, Assert.Throws<PyValueError>(() => target.Update(source)));
        Assert.Equal(new[] { "iter", "next0", "next1", "next2", "get:a", "get:b" }, log);
        Assert.Equal(7L, target.Get("a")); Assert.False(target.Contains("b"));
    }

    [Fact]
    public void KeywordDuplicatesStopBeforeReadAcrossAllGroups()
    {
        var reads = new List<object?>();
        PyMappingProxy source = Mapping(() => new PyList(["a", "b"]), key => { reads.Add(key); return 1L; });
        Assert.Contains("multiple values", Assert.Throws<PyTypeError>(() =>
            Keywords(["a", null], [0L, source])).Message);
        Assert.Empty(reads);
        Assert.Contains("multiple values", Assert.Throws<PyTypeError>(() =>
            Keywords([null, "a"], [source, 0L])).Message);
        Assert.Equal(new object?[] { "a", "b" }, reads);
        reads.Clear(); var dictionary = new PyDict(); dictionary.Set("a", 3L);
        Assert.Throws<PyTypeError>(() => Keywords([null, null], [dictionary, source]));
        Assert.Empty(reads);
        PyDict result = Assert.IsType<PyDict>(Keywords([null, "z"], [source, 9L]));
        Assert.Equal(9L, result.Get("z"));
    }

    [Fact]
    public void KeywordNoneNumericAliasesAndNanIdentityUsePythonDictionaryKeys()
    {
        object nan = double.NaN;
        foreach (object?[] keys in new[] {
            new object?[] { null, PyNone.Instance }, new object?[] { true, 1L },
            new object?[] { 1, BigInteger.One, 1.0 }, new object?[] { nan, nan } })
        {
            int reads = 0; object? first = keys[0];
            PyMappingProxy source = Mapping(() => new PyList(keys), key =>
            { Assert.Same(first, key); reads++; return 4L; });
            Assert.Contains("multiple values", Assert.Throws<PyTypeError>(() => Keywords([null], [source])).Message);
            Assert.Equal(1, reads);
        }
        int count = 0;
        PyMappingProxy nonString = Mapping(() => new PyList([1L, "later"]), _ => { count++; return 0L; });
        Assert.Equal("keywords must be strings", Assert.Throws<PyTypeError>(() => Keywords([null], [nonString])).Message);
        Assert.Equal(2, count);
        var original = new PyValueError("later read");
        PyMappingProxy fail = Mapping(() => new PyList([1L, "later"]), key => key is string ? throw original : 0L);
        Assert.Same(original, Assert.Throws<PyValueError>(() => Keywords([null], [fail])));
    }

    [Fact]
    public void CustomSourceAndTargetKeysAreRejectedWithoutOwnHashCallbacks()
    {
        int hashes = 0, equals = 0;
        var cls = new PyClass("ConsumerCustomKey");
        cls.SetAttr("__hash__", Slot("__hash__", ["self"], _ => { hashes++; return 1L; }));
        cls.SetAttr("__eq__", Slot("__eq__", ["self", "other"], _ => { equals++; return false; }));
        var key = new PyInstance(cls); int reads = 0;
        PyMappingProxy source = Mapping(() => new PyList([key]), _ => { reads++; return 1L; });
        Assert.Throws<PyNotImplementedError>(() => new PyDict().Update(source));
        Assert.Throws<PyNotImplementedError>(() => Keywords([null], [source]));
        Assert.Equal(0, reads); Assert.Equal(0, hashes); Assert.Equal(0, equals);
        var target = new PyDict(); target.Set(key, 4L); hashes = 0;
        PyMappingProxy safe = Mapping(() => new PyList([1L]), _ => 2L);
        Assert.Throws<PyNotImplementedError>(() => target.Update(safe));
        Assert.Equal(0, hashes); Assert.Equal(0, equals);
        var injectedTarget = new PyDict(); int callsAfterInjection = 0;
        PyMappingProxy injecting = Mapping(() => new PyList(["ok", 1L]), item =>
        {
            if (item is long) { injectedTarget.Set(key, 8L); callsAfterInjection = hashes; }
            return 3L;
        });
        Assert.Throws<PyNotImplementedError>(() => injectedTarget.Update(injecting));
        Assert.Equal(callsAfterInjection, hashes); Assert.Equal(0, equals);
        Assert.Equal(3L, injectedTarget.Get("ok")); Assert.Equal(2, injectedTarget.Count);
        // 代理自身读取仍原样委托，不由消费者白名单封禁。
        Assert.Equal(1L, PyOps.GetItem(source, key)); Assert.Equal(1, reads);
    }

    [Fact]
    public void DefaultDictAndCounterChooseKeysVersusItems()
    {
        var log = new List<string>();
        PyMappingProxy source = Mapping(() => { log.Add("keys"); return new PyList(["a"]); },
            _ => { log.Add("getitem"); return 2L; },
            () => { log.Add("items"); return new PyList([new PyTuple(["a", 3L])]); });
        var defaults = Assert.IsType<PyDefaultDict>(PyOps.CallValue(
            PyCollections.CreateAttributes()["defaultdict"], [PyNone.Instance], [], []));
        Method(defaults, "update", source);
        Assert.Equal(new[] { "keys", "getitem" }, log); log.Clear();
        var counter = new PyCounter(); Method(counter, "update", source);
        Assert.Equal(new[] { "keys", "getitem" }, log); log.Clear();
        Method(counter, "update", source); Assert.Equal(new[] { "items" }, log);
        Assert.Equal(5L, counter.Dictionary.Get("a")); log.Clear();
        Method(counter, "subtract", source); Assert.Equal(new[] { "items" }, log);
        Assert.Equal(2L, counter.Dictionary.Get("a"));
        foreach (object?[] malformed in new[] { Array.Empty<object?>(), new object?[] { "x" }, new object?[] { "x", 1L, 2L } })
        {
            PyMappingProxy broken = Mapping(() => new PyList(), _ => null,
                () => new PyList([new PyTuple(malformed)]));
            string expected = malformed.Length < 2
                ? $"not enough values to unpack (expected 2, got {malformed.Length})"
                : "too many values to unpack (expected 2)";
            Assert.Equal(expected, Assert.Throws<PyValueError>(() => Method(counter, "update", broken)).Message);
            Assert.Equal(1, counter.Dictionary.Count); Assert.Equal(2L, counter.Dictionary.Get("a"));
        }
    }

    [Fact]
    public void CounterRechecksTargetAfterUserArithmetic()
    {
        int hashes = 0;
        var keyClass = new PyClass("ArithmeticInjectedKey");
        keyClass.SetAttr("__hash__", Slot("__hash__", ["self"], _ => { hashes++; return 1L; }));
        var customKey = new PyInstance(keyClass); var counter = new PyCounter(); counter.Dictionary.Set("a", 1L);
        int after = 0; var numberClass = new PyClass("CallbackCount");
        numberClass.SetAttr("__add__", Slot("__add__", ["self", "other"], _ =>
        { counter.Dictionary.Set(customKey, 8L); after = hashes; return 9L; }));
        PyMappingProxy source = Mapping(() => new PyList(), _ => null,
            () => new PyList([new PyTuple(["a", new PyInstance(numberClass)])]));
        Assert.Throws<PyNotImplementedError>(() => Method(counter, "update", source));
        Assert.Equal(after, hashes); Assert.Equal(1L, counter.Dictionary.Get("a"));
    }
}
