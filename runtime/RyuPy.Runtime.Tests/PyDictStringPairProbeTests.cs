// 字符串对探针的独立实验门：共享存储、真实碰撞、拒绝回调键与命中分配；不代表MIR已接入。
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyDictStringPairProbeTests
{
    [Fact]
    public void HitKeepsOriginalKeyValueIdentityAndInsertionOrder()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        var key = new PyTuple(["ann", "buy"]); object original = new(), updated = new();
        dict.Set(key, original); dict.Set(new PyTuple(["bob", "view"]), null);
        long version = dict.Version;
        Assert.True(access.TryGet(new string("ann".ToCharArray()), "buy", out bool found, out object? value));
        Assert.True(found); Assert.Same(original, value);
        Assert.True(access.TrySet("ann", "buy", updated));
        Assert.Same(updated, dict.Get(key)); Assert.Equal(version, dict.Version);
        Assert.Same(key, dict.Keys.First());
        Assert.True(access.TryGet("bob", "view", out found, out value));
        Assert.True(found); Assert.Null(value);
        Assert.True(access.TryGet("missing", "key", out found, out value)); Assert.False(found);
    }

    [Fact]
    public void RealStringHashCollisionDoesNotMergeDistinctKeys()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        Assert.Equal(PyHash.TwoStringTupleHash("", "buy"), PyHash.TwoStringTupleHash("\0", "buy"));
        Assert.True(access.TrySet("", "buy", 1L)); Assert.True(access.TrySet("\0", "buy", 2L));
        Assert.Equal(2, dict.Count);
        Assert.Equal(1L, dict.Get(new PyTuple(["", "buy"])));
        Assert.Equal(2L, dict.Get(new PyTuple(["\0", "buy"])));
        Assert.True(access.TryGet("\0", "buy", out bool found, out object? value));
        Assert.True(found); Assert.Equal(2L, value);
    }

    [Fact]
    public void DeleteReinsertCompactAndClearShareTheGeneralStorage()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        for (int i = 0; i < 100; i++) Assert.True(access.TrySet(i.ToString(), "key", (long)i));
        for (int i = 0; i < 70; i++) dict.Delete(new PyTuple([i.ToString(), "key"]));
        Assert.True(access.TrySet("0", "key", -1L));
        Assert.Equal(31, dict.Count);
        Assert.Equal("70", ((PyTuple)dict.Keys.First()!).Items[0]);
        Assert.Equal("0", ((PyTuple)dict.Keys.Last()!).Items[0]);
        Assert.True(access.TryGet("99", "key", out bool found, out object? value));
        Assert.True(found); Assert.Equal(99L, value);
        dict.Clear(); Assert.True(access.TryGet("99", "key", out found, out value)); Assert.False(found);
    }

    [Fact]
    public void HeterogeneousMutationInvalidatesAndRemovalReestablishesProof()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        Assert.True(access.TrySet("ann", "buy", 1L));
        dict.Set("other", 2L);
        Assert.False(access.TryGet("ann", "buy", out _, out _));
        Assert.False(access.TrySet("ann", "buy", 3L));
        Assert.Equal(1L, dict.Get(new PyTuple(["ann", "buy"])));
        dict.Delete("other"); Assert.True(access.TrySet("ann", "buy", 3L));
        dict.Clear(); Assert.True(access.TrySet("ann", "buy", 4L));
    }

    [Fact]
    public void UserEqualitySeesRealFallbackTupleAndNeverAnAlternateProbe()
    {
        int comparisons = 0; object? observed = null;
        var cls = new PyClass("Key");
        cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("hash", _ => PyHash.TwoStringTupleHash("ann", "buy")));
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", args =>
        { comparisons++; observed = args[1]; return true; }));
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        dict.Set(new PyInstance(cls), 42L);
        Assert.False(access.TryGet("ann", "buy", out _, out _));
        Assert.False(access.TrySet("ann", "buy", 43L)); Assert.Equal(0, comparisons);
        var fallback = new PyTuple(["ann", "buy"]);
        Assert.Equal(42L, dict.Get(fallback)); Assert.Same(fallback, observed); Assert.Equal(1, comparisons);
    }

    [Fact]
    public void NestedUserElementsAndCustomComparersAreRejected()
    {
        var dict = new PyDict(); dict.Set(new PyTuple([1L, "buy"]), 1L);
        Assert.False(new PyDict.StringPairAccess(dict).TryGet("ann", "buy", out _, out _));
        var custom = new PyDict(EqualityComparer<object>.Default);
        Assert.False(new PyDict.StringPairAccess(custom).TrySet("ann", "buy", 1L));
        Assert.Equal(0, custom.Count);
    }

    [Fact]
    public void RepeatedHitsDoNotAllocateTupleOrReplacementValues()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict); object value = new();
        Assert.True(access.TrySet("ann", "buy", value));
        for (int i = 0; i < 100; i++)
        { access.TryGet("ann", "buy", out _, out _); access.TrySet("ann", "buy", value); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        { access.TryGet("ann", "buy", out _, out _); access.TrySet("ann", "buy", value); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated); Assert.Same(value, dict.Get(new PyTuple(["ann", "buy"])));
    }
}
