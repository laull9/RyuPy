// 已物化key的融合合同：保留插入/命中身份，拒绝后完整原语义只执行一次；尚未MIR接线。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictMaterializedIncrementTests
{
    private static void Ordinary(PyDict dict, object key) =>
        dict.Set(key, PyOps.Binary(PyOps.Add, dict.GetOrDefault(key, 0L)!, 1L));

    [Fact]
    public void MissingKeyStoresTheCallerTupleWhileHitKeepsTheOlderTuple()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        var first = new PyTuple(["ann", "buy"]);
        Assert.True(access.TryIncrementMaterializedKey(first));
        Assert.Same(first, dict.Keys.Single()); Assert.Same(PyInt.Box(1L), dict.Get(first));
        long version = dict.Version; var later = new PyTuple(["ann", "buy"]);
        object previous = dict.Get(first)!;
        Assert.True(access.TryIncrementMaterializedKey(later));
        Assert.Same(first, dict.Keys.Single()); Assert.NotSame(later, dict.Keys.Single());
        Assert.Equal(version, dict.Version); Assert.Equal(1L, previous); Assert.Equal(2L, dict.Get(first));
        dict.Delete(first); Assert.True(access.TryIncrementMaterializedKey(later));
        Assert.Same(later, dict.Keys.Single()); Assert.Equal(1L, dict.Get(later));
    }

    [Fact]
    public void ProductSmallIntegerIdentityMatchesOrdinaryArithmetic()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        var key = new PyTuple(["ann", "buy"]);
        foreach (long value in new long[] { -6, -5, 0, 1, 255 })
        {
            dict.Set(key, PyInt.Box(value));
            object expected = PyOps.Binary(PyOps.Add, PyInt.Box(value), PyInt.Box(1L));
            Assert.True(access.TryIncrementMaterializedKey(key));
            Assert.Same(expected, dict.Get(key));
        }
        dict.Set(key, PyInt.Box(256L));
        Assert.True(access.TryIncrementMaterializedKey(key));
        Assert.Equal(257L, dict.Get(key)); Assert.NotSame(PyInt.Box(257L), dict.Get(key));
    }

    [Fact]
    public void OverflowRejectsBeforeWriteAndOrdinaryFallbackPromotesOnce()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        var key = new PyTuple(["ann", "buy"]); dict.Set(key, long.MaxValue);
        object old = dict.Get(key)!; long version = dict.Version;
        Assert.False(access.TryIncrementMaterializedKey(key));
        Assert.Same(old, dict.Get(key)); Assert.Equal(version, dict.Version);
        Ordinary(dict, key);
        Assert.Equal(new BigInteger(long.MaxValue) + 1, Assert.IsType<BigInteger>(dict.Get(key)));
        Assert.Same(key, dict.Keys.Single()); Assert.Equal(version, dict.Version);
        dict.Set(key, true); Assert.False(access.TryIncrementMaterializedKey(key));
        Ordinary(dict, key); Assert.Equal(2L, dict.Get(key));
        dict.Set(key, long.MinValue); Assert.True(access.TryIncrementMaterializedKey(key));
        Assert.Equal(long.MinValue + 1, dict.Get(key));
    }

    [Fact]
    public void RejectedValueCallbackObservesSameKeyAndRunsOnlyInFallback()
    {
        int calls = 0; var dict = new PyDict(); var key = new PyTuple(["ann", "buy"]);
        var cls = new PyClass("ObservedCounter");
        cls.SetAttr("__add__", PyNativeFunctions.CreateVariadic("add", args =>
        {
            calls++; Assert.Same(key, dict.Keys.First()); Assert.Equal(1L, args[1]);
            dict.Set("callback", 7L); return 41L;
        }));
        var old = new PyInstance(cls); dict.Set(key, old);
        var access = new PyDict.StringPairAccess(dict);
        Assert.False(access.TryIncrementMaterializedKey(key));
        Assert.Equal(0, calls); Assert.Same(old, dict.Get(key)); Assert.Equal(1, dict.Count);
        Ordinary(dict, key); Assert.Equal(1, calls); Assert.Equal(41L, dict.Get(key));
        Assert.Same(key, dict.Keys.First()); Assert.Equal(7L, dict.Get("callback"));
        Assert.False(access.TryIncrementMaterializedKey(key));
    }

    [Fact]
    public void ThrowingFallbackPreservesOldValueAndDoesNotReplayCallback()
    {
        int calls = 0; var dict = new PyDict(); var key = new PyTuple(["ann", "buy"]);
        var cls = new PyClass("ThrowingCounter");
        cls.SetAttr("__add__", PyNativeFunctions.CreateVariadic("add", _ =>
        { calls++; throw new PyValueError("counter failed"); }));
        var old = new PyInstance(cls); dict.Set(key, old); long version = dict.Version;
        Assert.False(new PyDict.StringPairAccess(dict).TryIncrementMaterializedKey(key));
        Assert.Equal(0, calls);
        Assert.Throws<PyValueError>(() => Ordinary(dict, key));
        Assert.Equal(1, calls); Assert.Same(old, dict.Get(key)); Assert.Equal(version, dict.Version);
    }

    private sealed class DerivedTuple(object?[] items) : PyTuple(items);

    [Fact]
    public void ShapeAndComparerRejectionsDoNotHashOrAllocateStoredKeys()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict); int hashes = 0;
        var cls = new PyClass("HashCounter");
        cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("hash", _ => { hashes++; return 1L; }));
        object?[] rejected = [null, "ann", new DerivedTuple(["ann", "buy"]),
            new PyTuple(["ann"]), new PyTuple(["ann", new PyInstance(cls)])];
        foreach (object? key in rejected) Assert.False(access.TryIncrementMaterializedKey(key));
        Assert.Equal(0, hashes); Assert.Equal(0, dict.Count); Assert.Equal(0, dict.Version);
        var custom = new PyDict(EqualityComparer<object>.Default);
        Assert.False(new PyDict.StringPairAccess(custom).TryIncrementMaterializedKey(new PyTuple(["ann", "buy"])));
        Assert.Equal(0, custom.Count);
    }

    [Fact]
    public void IncrementDifferentialPreservesAllValuesKeysAndOrder()
    {
        var ordinary = new PyDict(); var fused = new PyDict(); var access = new PyDict.StringPairAccess(fused);
        for (int i = 0; i < 600; i++)
        {
            // 同一对象供两臂使用，检查新插入的真实实体而不是仅比较repr。
            var key = new PyTuple([i % 9 == 0 ? "" : i % 9 == 1 ? "\0" : "user" + i % 17, "buy"]);
            Ordinary(ordinary, key);
            if (!access.TryIncrementMaterializedKey(key)) Ordinary(fused, key);
            var a = ordinary.Entries().ToArray(); var b = fused.Entries().ToArray();
            Assert.Equal(a.Length, b.Length);
            for (int j = 0; j < a.Length; j++) { Assert.Same(a[j].Key, b[j].Key); Assert.Same(a[j].Value, b[j].Value); }
            Assert.Equal(ordinary.Version, fused.Version);
        }
    }
}
