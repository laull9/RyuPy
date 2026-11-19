// F1a 普通typing根的公开入口合同：原始键、十一根共享128项真LRU及原池隔离。
// 只使用新TypeVar建立独立键，不清缓存、不调用生产内部缓存；预期先由固定CP双跑冻结。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingLegacyCacheTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static readonly string[] Roots = ["List", "Dict", "Set", "FrozenSet", "Iterable", "Iterator", "Sequence", "Mapping", "MutableMapping", "Collection", "Container"];
    private static readonly string[] UnaryRoots = ["List", "Set", "FrozenSet", "Iterable", "Iterator", "Sequence", "Collection", "Container"];
    private static object? Fresh() => Variable("F1aKey");
    private static object? Apply(string name, object? key) => PyOps.GetItem(Form(name), key);
    private static object? Key(string name, object? value) => name is "Dict" or "Mapping" or "MutableMapping" ? new PyTuple([value, PyType.Get("int")]) : value;

    [Fact]
    public void ScalarAndSingletonTupleKeepSeparateRawEntriesForEveryUnaryRoot()
    {
        foreach (string name in UnaryRoots)
        {
            object? key = Fresh(), scalar = Apply(name, key), tuple = Apply(name, new PyTuple([key]));
            Assert.Same(scalar, Apply(name, key));
            Assert.Same(tuple, Apply(name, new PyTuple([key])));
            Assert.NotSame(scalar, tuple);
            Assert.True(PyHash.Equal(scalar, tuple));
            Assert.Equal(PyHash.Of(scalar), PyHash.Of(tuple));
            Assert.NotSame(scalar, Apply(name, Fresh()));
        }
    }

    [Fact]
    public void NoneAndNoneTypeNormalizeEquallyWithoutCollapsingOriginalKeys()
    {
        foreach (string name in Roots)
        {
            object? fresh = Fresh();
            object? noneKey = name is "Dict" or "Mapping" or "MutableMapping" ? new PyTuple([fresh, PyNone.Instance]) : PyNone.Instance;
            object? typeKey = name is "Dict" or "Mapping" or "MutableMapping" ? new PyTuple([fresh, PyType.Get("NoneType")]) : PyType.Get("NoneType");
            object? none = Apply(name, noneKey), noneType = Apply(name, typeKey);
            Assert.NotSame(none, noneType);
            Assert.True(PyHash.Equal(none, noneType));
            Assert.Equal(PyHash.Of(none), PyHash.Of(noneType));
            Assert.Same(none, Apply(name, noneKey));
            Assert.Same(noneType, Apply(name, typeKey));
        }
    }

    [Fact]
    public void EqualNonidenticalAliasKeysReuseTheFirstRawEntryAndArgumentIdentity()
    {
        object? key = Fresh();
        object? first = Apply("Set", key), second = Apply("Set", new PyTuple([key]));
        Assert.NotSame(first, second);
        Assert.True(PyHash.Equal(first, second));
        object? outer = Apply("List", first);
        Assert.Same(outer, Apply("List", second));
        Assert.Same(first, Assert.IsType<PyTuple>(Get(outer, "__args__")).Items[0]);
        Assert.NotSame(outer, Apply("Sequence", first));
        Assert.False(PyHash.Equal(outer, Apply("Sequence", first)));
    }

    [Fact]
    public void ElevenRootsShareOneCapacityAndTouchTheRealLeastRecentlyUsedEntry()
    {
        var keys = new object?[128]; var values = new object?[128];
        for (int i = 0; i < keys.Length; i++)
        {
            string root = Roots[i % Roots.Length];
            keys[i] = Key(root, Fresh()); values[i] = Apply(root, keys[i]);
        }
        Assert.Same(values[0], Apply("List", keys[0]));
        Apply("Container", Fresh());
        Assert.Same(values[0], Apply("List", keys[0]));
        object? rebuilt = Apply("Dict", keys[1]);
        Assert.NotSame(values[1], rebuilt);
        Assert.True(PyHash.Equal(values[1], rebuilt));
    }

    [Fact]
    public void EveryOrdinaryRootCanBeEvictedByAnotherOrdinaryRootWithoutOldInternResurrection()
    {
        foreach (string name in Roots)
        {
            object? key = Key(name, Fresh()), old = Apply(name, key);
            string pressure = name == "List" ? "Set" : "List";
            for (int i = 0; i < 128; i++) Apply(pressure, Fresh());
            object? rebuilt = Apply(name, key);
            Assert.NotSame(old, rebuilt);
            Assert.True(PyHash.Equal(old, rebuilt));
            Assert.Same(rebuilt, Apply(name, key));
        }
    }

    [Fact]
    public void OtherPoolPressureCannotEvictAnOrdinaryRootEntry()
    {
        object? key = Fresh(), alias = Apply("List", key);
        for (long i = 0; i < 140; i++)
        {
            object? pressure = Fresh();
            Apply("Union", new PyTuple([pressure, PyType.Get("int")]));
            Subscribe(pressure);
            Apply("Literal", i);
            Apply("Tuple", pressure);
            Apply("Callable", new PyTuple([new PyList([pressure]), PyType.Get("int")]));
            PyOps.GetItem(PyType.Get("list"), pressure);
        }
        Assert.Same(alias, Apply("List", key));
    }

    [Fact]
    public void OrdinaryRootPressureDoesNotEvictSpecialGenericOrRemainingOldPoolEntries()
    {
        object? key = Fresh(), pair = new PyTuple([key, PyType.Get("int")]);
        object? callableKey = new PyTuple([new PyList([key]), PyType.Get("int")]);
        object? union = Apply("Union", pair), generic = Subscribe(key), literal = Apply("Literal", key);
        object? tuple = Apply("Tuple", key), callable = Apply("Callable", callableKey);
        for (int i = 0; i < 140; i++) Apply("List", Fresh());
        Assert.Same(union, Apply("Union", pair));
        Assert.Same(generic, Subscribe(key));
        Assert.Same(literal, Apply("Literal", key));
        Assert.Same(tuple, Apply("Tuple", key));
        Assert.Same(callable, Apply("Callable", callableKey));
    }

    [Fact]
    public void SavedOrdinaryReceiverKeepsItsOwnOriginAfterExportRebinding()
    {
        object? root = Form("List"), key = Fresh(), cold = Fresh();
        object? alias = PyOps.GetItem(root, key);
        Typing["List"] = PyNone.Instance;
        try
        {
            Assert.Same(alias, PyOps.GetItem(root, key));
            Assert.Same(PyType.Get("list"), Get(PyOps.GetItem(root, cold), "__origin__"));
        }
        finally { Typing["List"] = root; }
    }
}
