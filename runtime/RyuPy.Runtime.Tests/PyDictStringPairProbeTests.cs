// 字符串对探针的独立实验门：共享存储、真实碰撞、拒绝回调键与命中分配；不代表MIR已接入。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RyuPy.Runtime.Tests;

public sealed class PyDictStringPairProbeTests(ITestOutputHelper output)
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
    public void SameCountDeleteInsertInvalidatesByVersionNotCount()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        access.TrySet("ann", "buy", 1L); access.TrySet("bob", "view", 2L);
        Assert.True(access.TryGet("ann", "buy", out _, out _));
        int count = dict.Count; long version = dict.Version;
        dict.Delete(new PyTuple(["bob", "view"])); dict.Set("foreign", 3L);
        Assert.Equal(count, dict.Count); Assert.NotEqual(version, dict.Version);
        Assert.False(access.TryGet("ann", "buy", out _, out _));
        Assert.False(access.TrySet("ann", "buy", 4L));
        Assert.Equal(1L, dict.Get(new PyTuple(["ann", "buy"])));
        dict.Delete("foreign"); dict.Set(new PyTuple(["cyd", "cart"]), 5L);
        Assert.Equal(count, dict.Count);
        Assert.True(access.TryGet("cyd", "cart", out bool found, out object? value));
        Assert.True(found); Assert.Equal(5L, value);
        Assert.Equal("cyd", ((PyTuple)dict.Keys.Last()!).Items[0]);
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

    [Fact]
    public void OwnGrowthKeepsProofButExternalLargeMutationStaysRejected()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        for (int i = 0; i < 1024; i++) Assert.True(access.TrySet(i.ToString(), "key", 1L));
        Assert.True(access.TryGet("0", "key", out bool found, out _)); Assert.True(found);
        dict.Set(new PyTuple(["outside", "key"]), 2L);
        // 即使外部写入本身安全，也没有在扫描预算外重新接受域的证明。
        for (int i = 0; i < 3; i++)
        {
            Assert.False(access.TryGet("0", "key", out _, out _));
            Assert.False(access.TrySet("0", "key", 3L));
        }
        Assert.Equal(1L, dict.Get(new PyTuple(["0", "key"])));
        dict.Clear(); Assert.True(access.TrySet("recovered", "key", 4L));
    }

    [Fact]
    public void UserCallbackBetweenGetAndSetCannotReuseStaleDomain()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        Assert.True(access.TrySet("ann", "buy", 1L));
        Assert.True(access.TryGet("ann", "buy", out _, out _));
        var cls = new PyClass("CallbackKey");
        cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("hash", _ =>
        {
            // 外部插入的hash回调重入：尚未插入异构键，此刻原域仍然有效。
            Assert.True(access.TryGet("ann", "buy", out bool found, out object? value));
            Assert.True(found); Assert.Equal(1L, value); return 123L;
        }));
        dict.Set(new PyInstance(cls), 9L);
        Assert.False(access.TrySet("ann", "buy", 2L));
        Assert.False(access.TryGet("ann", "buy", out _, out _));
        // 原tuple回退仍正常，覆盖不会偷偷移除回调键从而恢复快路。
        dict.Set(new PyTuple(["ann", "buy"]), 2L);
        Assert.False(access.TrySet("ann", "buy", 3L)); Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void ActiveIteratorAllowsOverwriteButRejectsStructuralChange()
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
        access.TrySet("ann", "buy", 1L); access.TrySet("bob", "view", 2L);
        IPyIterator iterator = PyIter.Create(dict);
        Assert.True(iterator.MoveNext());
        Assert.True(access.TrySet("ann", "buy", 3L)); Assert.True(iterator.MoveNext());
        IPyIterator changed = PyIter.Create(dict); Assert.True(changed.MoveNext());
        Assert.True(access.TrySet("cyd", "buy", 4L));
        Assert.Throws<PyRuntimeError>(() => changed.MoveNext());
    }

    private sealed class DerivedTuple(object?[] items) : PyTuple(items);

    [Fact]
    public void TupleSubclassAndUserElementRejectWithoutCallingEquality()
    {
        var derived = new PyDict(); derived.Set(new DerivedTuple(["ann", "buy"]), 1L);
        Assert.False(new PyDict.StringPairAccess(derived).TryGet("ann", "buy", out _, out _));
        int calls = 0; var cls = new PyClass("Element");
        cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("hash", _ => PyHash.Of("ann")));
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { calls++; return true; }));
        var nested = new PyDict(); nested.Set(new PyTuple([new PyInstance(cls), "buy"]), 1L);
        Assert.False(new PyDict.StringPairAccess(nested).TryGet("ann", "buy", out _, out _));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void LongUnicodeStringsUseTheSameTupleHashAndOrdinalEquality()
    {
        string first = new string('中', 1024) + "😀", second = "é\0终";
        var key = new PyTuple([first, second]); var dict = new PyDict(); dict.Set(key, 7L);
        Assert.Equal(PyHash.Of(key), PyHash.TwoStringTupleHash(first, second));
        var access = new PyDict.StringPairAccess(dict);
        Assert.True(access.TryGet(new string(first.ToCharArray()), second, out bool found, out object? value));
        Assert.True(found); Assert.Equal(7L, value);
        Assert.True(access.TryGet(first, "e\u0301\0终", out found, out _)); Assert.False(found);
    }

    [Theory]
    [InlineData(4096)]
    [InlineData(16384)]
    public void HighCardinalityDiagnostic(int count)
    {
        string[] keys = Enumerable.Range(0, count).Select(i => i.ToString()).ToArray();
        object value = new();
        // 仅筛选灾难性退化，不是性能A/B验收：固定顺序、无交替/多轮，且CI共享机有噪声。
        // 不设墙钟收益阈值；原始测量写测试日志。
        foreach (bool probe in new[] { false, true })
        {
            var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict);
            long before = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
            foreach (string key in keys)
            {
                if (probe)
                {
                    if (!access.TrySet(key, "key", value)) throw new InvalidOperationException("自身插入丢失域证明");
                }
                else dict.Set(new PyTuple([key, "key"]), value);
            }
            timer.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(count, dict.Count);
            output.WriteLine($"records-probe count={count} probe={probe} ms={timer.Elapsed.TotalMilliseconds:F3} bytes={allocated}");
        }
    }
}
