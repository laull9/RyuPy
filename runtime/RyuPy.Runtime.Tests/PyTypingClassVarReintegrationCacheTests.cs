// C1 失败不得进池或驱逐成功项：仅用公开订阅、完整128项身份及恢复后的首次成功证明。
// 四池分别测量，不依赖内部 clear/count，不将同形 alias 的不同对象误当新 raw 键。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingClassVarReintegrationCacheTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("C1FailureKey");
    private static object? Apply(string root, object? raw) => PyOps.GetItem(Form(root), raw);
    private static object? Callable(object? args, object? result) => Apply("Callable", new PyTuple([args, result]));
    private static object? Success(string family, object? key) => family switch
    {
        "Callable" => Callable(new PyList([]), key),
        "Union" => Apply("Union", new PyTuple([key, PyType.Get("str")])),
        _ => Apply(family, key),
    };
    private static object? AliasResult(string family, object? alias, object? marker) => family switch
    {
        "Callable" => Callable(new PyList([marker]), alias),
        "Union" => Apply(family, new PyTuple([alias, marker])),
        _ => Apply(family, new PyTuple([alias])),
    };
    private static void WithClassVar(object? value, Action body)
    {
        bool existed = Typing.TryGetValue("ClassVar", out object? saved);
        Typing["ClassVar"] = value;
        try { body(); }
        finally { if (existed) Typing["ClassVar"] = saved; else Typing.Remove("ClassVar"); }
    }

    private static void AssertFailedMissesPreserveCapacityAndRecover(string family)
    {
        // 先构造全部失败 alias，再填目标池，避免准备 List alias 时污染普通池的128项证明。
        var bad = new object?[160]; var markers = new object?[160];
        for (int i = 0; i < bad.Length; i++) { bad[i] = Apply("List", Fresh()); markers[i] = Fresh(); }
        var keys = new object?[128]; var old = new object?[128];
        for (int i = 0; i < keys.Length; i++) { keys[i] = Fresh(); old[i] = Success(family, keys[i]); }
        WithClassVar(PyType.Get("list"), () =>
        {
            for (int i = 0; i < bad.Length; i++)
                for (int repeat = 0; repeat < 2; repeat++)
                    Assert.Equal(PyRepr.Repr(bad[i]) + " is not valid as type argument",
                        Assert.Throws<PyTypeError>(() => AliasResult(family, bad[i], markers[i])).PyMessage);
            // 从最旧到最新触碰全部条目，任何错误插入/错误驱逐都会在这里暴露。
            for (int i = 0; i < keys.Length; i++) Assert.Same(old[i], Success(family, keys[i]));
        });
        object? recovered = AliasResult(family, bad[0], markers[0]);
        Assert.Same(recovered, AliasResult(family, bad[0], markers[0]));
        var recoveredArgs = Assert.IsType<PyTuple>(Get(recovered, "__args__")).Items;
        Assert.Same(bad[0], family == "Union" ? recoveredArgs[0] : recoveredArgs[^1]);
        // 恢复后第一次真正成功只新增一项；旧第0项被驱逐，第1项仍留存。
        Assert.Same(old[1], Success(family, keys[1]));
        object? rebuilt = Success(family, keys[0]);
        Assert.NotSame(old[0], rebuilt);
        Assert.True(PyHash.Equal(old[0], rebuilt));
    }

    [Fact]
    public void OrdinaryFailedClassVarChecksDoNotInsertEvictOrPoisonRecovery() =>
        AssertFailedMissesPreserveCapacityAndRecover("List");

    [Fact]
    public void TupleFailedClassVarChecksDoNotInsertEvictOrPoisonRecovery() =>
        AssertFailedMissesPreserveCapacityAndRecover("Tuple");

    [Fact]
    public void CallableFailedClassVarResultsDoNotInsertEvictOrPoisonRecovery() =>
        AssertFailedMissesPreserveCapacityAndRecover("Callable");

    [Fact]
    public void SharedSpecialFormFailedClassVarChecksDoNotInsertEvictOrPoisonRecovery() =>
        AssertFailedMissesPreserveCapacityAndRecover("Union");

    [Fact]
    public void EvictedWarmAliasKeysMustRecheckClassVarAndRecoverAfterExportRestoration()
    {
        foreach (string family in new[] { "List", "Tuple", "Callable", "Union" })
        {
            object? alias = Apply("List", Fresh()), marker = Fresh();
            object? warm = AliasResult(family, alias, marker);
            WithClassVar(PyType.Get("list"), () =>
            {
                Assert.Same(warm, AliasResult(family, alias, marker));
                for (int i = 0; i < 128; i++) Success(family, Fresh());
                for (int repeat = 0; repeat < 2; repeat++)
                    Assert.Equal(PyRepr.Repr(alias) + " is not valid as type argument",
                        Assert.Throws<PyTypeError>(() => AliasResult(family, alias, marker)).PyMessage);
            });
            object? rebuilt = AliasResult(family, alias, marker);
            Assert.NotSame(warm, rebuilt);
            Assert.True(PyHash.Equal(warm, rebuilt));
            Assert.Same(rebuilt, AliasResult(family, alias, marker));
        }
    }
}
