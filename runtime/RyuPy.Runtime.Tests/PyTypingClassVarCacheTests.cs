// C1 必须接入既有 SpecialForm 共享 128 项原始键 LRU；保留旧对象才能证明真实淘汰。
// 全部通过公开下标入口，新 TypeVar 排空旧状态，不使用清空缓存或内部计数钩子。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingClassVarCacheTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("C1Key");
    private static object? Cv(object? key) => PyOps.GetItem(Form("ClassVar"), key);
    private static object? Union(object? a, object? b) => PyOps.GetItem(Form("Union"), new PyTuple([a, b]));
    private static object? Optional(object? key) => PyOps.GetItem(Form("Optional"), key);
    private static void Fill(int count)
    {
        for (int i = 0; i < count; i++) Union(Fresh(), PyType.Get("int"));
    }

    [Fact]
    public void UnionPressureEvictsClassVarVictimButRefreshesHotEntry()
    {
        Fill(128);
        object? hotKey = Fresh(), victimKey = Fresh();
        object? hot = Cv(hotKey), victim = Cv(victimKey);
        Fill(126);
        Assert.Same(hot, Cv(hotKey));
        Union(Fresh(), PyType.Get("int"));
        Assert.Same(hot, Cv(hotKey));
        Assert.NotSame(victim, Cv(victimKey));
    }

    [Fact]
    public void ClassVarPressureAlsoEvictsAnOldUnionEntry()
    {
        Fill(128);
        object? key = Fresh(), old = Union(key, PyType.Get("int"));
        for (int i = 0; i < 128; i++) Cv(Fresh());
        Assert.NotSame(old, Union(key, PyType.Get("int")));
    }

    [Fact]
    public void OptionalUsesTwoEntriesAndOuterHitDoesNotRefreshInnerUnion()
    {
        Fill(128);
        object? key = Fresh(), old = Cv(key);
        Fill(126);
        Optional(Fresh());
        Assert.NotSame(old, Cv(key));
        Fill(128);
        key = Fresh(); old = Optional(key);
        for (int i = 0; i < 126; i++) Cv(Fresh());
        Assert.Same(old, Optional(key));
        Cv(Fresh());
        Assert.Same(old, Optional(key));
        Assert.NotSame(old, Union(key, PyType.Get("NoneType")));
    }

    [Fact]
    public void RawNoneSlotsRemainDistinctAndLiteralDoesNotUseThePool()
    {
        Fill(128);
        object? oldNone = Cv(PyNone.Instance), oldType = Cv(PyType.Get("NoneType"));
        Fill(126);
        Assert.Same(oldType, Cv(PyType.Get("NoneType")));
        Union(Fresh(), PyType.Get("int"));
        Assert.NotSame(oldNone, Cv(PyNone.Instance));
        Assert.Same(oldType, Cv(PyType.Get("NoneType")));
        Fill(128);
        object? key = Fresh(), old = Cv(key);
        for (long i = 0; i < 150; i++) PyOps.GetItem(Form("Literal"), i);
        Assert.Same(old, Cv(key));
    }

    [Fact]
    public void StructurallyEqualAliasArgumentsShareOneOuterCacheEntry()
    {
        object? t = Fresh(), u = Fresh();
        object? a = Union(t, u), b = Union(u, t);
        Assert.NotSame(a, b);
        Assert.True(PyHash.Equal(a, b));
        Assert.Same(Cv(a), Cv(b));
        object? nativeA = PyOps.GetItem(PyType.Get("list"), t);
        object? old = Cv(nativeA);
        // 旧别名池与共享池独立，Literal 压力只用于确保 nativeB 不再是相同引用。
        for (long i = 0; i < 150; i++) PyOps.GetItem(Form("Literal"), i);
        object? nativeB = PyOps.GetItem(PyType.Get("list"), t);
        Assert.NotSame(nativeA, nativeB);
        bool had = Typing.TryGetValue("_GenericAlias", out object? saved);
        Typing["_GenericAlias"] = PyNone.Instance;
        try { Assert.Same(old, Cv(nativeB)); }
        finally { if (had) Typing["_GenericAlias"] = saved; else Typing.Remove("_GenericAlias"); }
    }
}
