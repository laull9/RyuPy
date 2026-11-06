// F1b Tuple公开入口合同草稿：128项独立raw缓存、真实身份和跨池隔离。
// 只使用PyOps/GetAttr等公开入口和fresh TypeVar；不清缓存，不读取生产内部布局。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingTupleCacheTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("F1bTuple");
    private static object? Apply(object? key) => PyOps.GetItem(Form("Tuple"), key);
    private static object? Other(string root, object? key) => PyOps.GetItem(Form(root), key);

    [Fact]
    public void ScalarSingletonAndEquivalentAliasKeysPreserveTheRawBoundary()
    {
        object? key = Fresh(), scalar = Apply(key), tuple = Apply(new PyTuple([key]));
        Assert.Same(scalar, Apply(key));
        Assert.Same(tuple, Apply(new PyTuple([key])));
        Assert.NotSame(scalar, tuple);
        Assert.True(PyHash.Equal(scalar, tuple));
        Assert.Equal(PyHash.Of(scalar), PyHash.Of(tuple));
        object? first = Other("Set", key), second = Other("Set", new PyTuple([key]));
        Assert.NotSame(first, second);
        object? outer = Apply(first);
        Assert.Same(outer, Apply(second));
        Assert.Same(first, Assert.IsType<PyTuple>(Get(outer, "__args__")).Items[0]);
    }

    [Fact]
    public void FourNoneRawShapesAndTrailingNoneDoNotCollapse()
    {
        object?[] keys = [PyNone.Instance, PyType.Get("NoneType"), new PyTuple([PyNone.Instance]), new PyTuple([PyType.Get("NoneType")])];
        var aliases = new object?[4];
        for (int i = 0; i < keys.Length; i++)
        {
            aliases[i] = Apply(keys[i]);
            Assert.Same(aliases[i], Apply(keys[i]));
            for (int j = 0; j < i; j++)
            {
                Assert.NotSame(aliases[j], aliases[i]);
                Assert.True(PyHash.Equal(aliases[j], aliases[i]));
                Assert.Equal(PyHash.Of(aliases[j]), PyHash.Of(aliases[i]));
            }
        }
        object? a = Apply(new PyTuple([PyNone.Instance, PyEllipsis.Instance]));
        object? b = Apply(new PyTuple([PyType.Get("NoneType"), PyEllipsis.Instance]));
        Assert.NotSame(a, b);
        Assert.True(PyHash.Equal(a, b));
    }

    [Fact]
    public void EmptyAndMultipleCheckedPrefixesBeforeEllipsisAreLegal()
    {
        object? empty = Apply(new PyTuple([]));
        Assert.Same(empty, Apply(new PyTuple([])));
        Assert.Empty(Assert.IsType<PyTuple>(Get(empty, "__args__")).Items);
        Assert.Empty(Assert.IsType<PyTuple>(Get(empty, "__parameters__")).Items);
        Assert.Equal("typing.Tuple[()]", PyRepr.Repr(empty));
        object? t = Fresh();
        object?[] values = [t, PyType.Get("int"), PyType.Get("str"), PyEllipsis.Instance];
        object? alias = Apply(new PyTuple(values));
        Assert.Equal(values, Assert.IsType<PyTuple>(Get(alias, "__args__")).Items);
        Assert.Equal(new[] { t }, Assert.IsType<PyTuple>(Get(alias, "__parameters__")).Items);
        Assert.Same(PyType.Get("tuple"), Get(alias, "__origin__"));
        Assert.Equal("_GenericAlias", Get(PyObject.TypeOf(alias), "__name__"));
        Assert.Equal("_TupleType", Get(PyObject.TypeOf(Form("Tuple")), "__name__"));
    }

    [Fact]
    public void CapacityIs128AndOnlyTheRealLeastRecentlyUsedEntryIsEvicted()
    {
        var keys = new object?[128]; var aliases = new object?[128];
        for (int i = 0; i < keys.Length; i++) { keys[i] = Fresh(); aliases[i] = Apply(keys[i]); }
        Assert.Same(aliases[0], Apply(keys[0]));
        Apply(Fresh());
        Assert.Same(aliases[0], Apply(keys[0]));
        object? rebuilt = Apply(keys[1]);
        Assert.NotSame(aliases[1], rebuilt);
        Assert.True(PyHash.Equal(aliases[1], rebuilt));
        Assert.Same(rebuilt, Apply(keys[1]));
    }

    [Fact]
    public void RepeatedColdFailuresNeitherOccupyCapacityNorEvictSuccesses()
    {
        var keys = new object?[128]; var aliases = new object?[128];
        for (int i = 0; i < keys.Length; i++) { keys[i] = Fresh(); aliases[i] = Apply(keys[i]); }
        for (int i = 0; i < 160; i++)
            Assert.Throws<PyTypeError>(() => Apply(new PyTuple([new PyTuple([PyType.Get("int")]), Fresh()])));
        // 检查全部128项，避免只检查最新项掩盖失败路径偷偷驱逐老项。
        for (int i = 0; i < keys.Length; i++) Assert.Same(aliases[i], Apply(keys[i]));
    }

    [Fact]
    public void OtherPoolsCannotEvictTupleAndTupleCannotEvictTheirEntries()
    {
        object? key = Fresh(), alias = Apply(key);
        for (long i = 0; i < 140; i++)
        {
            object? p = Fresh();
            Other("List", p);
            Other("Union", new PyTuple([p, PyType.Get("int")]));
            Subscribe(p);
            Other("Literal", i);
            Other("Callable", new PyTuple([new PyList([p]), PyType.Get("int")]));
            PyOps.GetItem(PyType.Get("list"), p);
        }
        Assert.Same(alias, Apply(key));
        object? pair = new PyTuple([key, PyType.Get("int")]);
        object? callableKey = new PyTuple([new PyList([key]), PyType.Get("int")]);
        object? ordinary = Other("List", key), union = Other("Union", pair), generic = Subscribe(key);
        object? literal = Other("Literal", key), callable = Other("Callable", callableKey);
        for (int i = 0; i < 140; i++) Apply(Fresh());
        Assert.Same(ordinary, Other("List", key));
        Assert.Same(union, Other("Union", pair));
        Assert.Same(generic, Subscribe(key));
        Assert.Same(literal, Other("Literal", key));
        Assert.Same(callable, Other("Callable", callableKey));
        // CP原生list[T]不缓存身份，不对它或剩余旧池彼此关系作额外承诺。
    }

    [Fact]
    public void SavedReceiverAndBoundMethodKeepTheirOwnOriginAfterExportRebinding()
    {
        object? root = Form("Tuple"), method = Get(root, "__getitem__"), key = Fresh();
        object? alias = PyOps.GetItem(root, key);
        Typing["Tuple"] = PyNone.Instance;
        try
        {
            Assert.Same(alias, PyOps.GetItem(root, key));
            object? cold = Call(method, new PyTuple([key]));
            Assert.NotSame(alias, cold);
            Assert.True(PyHash.Equal(alias, cold));
            Assert.Same(cold, Call(method, new PyTuple([key])));
            Assert.Same(PyType.Get("tuple"), Get(cold, "__origin__"));
        }
        finally { Typing["Tuple"] = root; }
    }

    [Fact]
    public void SafeErrorsUseTheirRealTupleBranchAndRepeatWithoutCaching()
    {
        object? integer = PyType.Get("int");
        foreach (var (key, message) in new (object?, string)[] {
            (new PyTuple([new PyTuple([])]), "Tuple[t0, t1, ...]: each t must be a type. Got ()."),
            (new PyTuple([new PyTuple([integer])]), "Tuple[t0, t1, ...]: each t must be a type. Got (<class 'int'>,)."),
            (new PyTuple([integer, new PyTuple([integer]), PyEllipsis.Instance]), "Tuple[t, ...]: t must be a type. Got (<class 'int'>,)."),
            (new PyTuple([Form("Union"), PyEllipsis.Instance]), "Plain typing.Union is not valid as type argument"),
            (new PyTuple([Form("Generic"), PyEllipsis.Instance]), "Plain <class 'typing.Generic'> is not valid as type argument") })
            for (int repeat = 0; repeat < 2; repeat++)
                Assert.Equal(message, Assert.Throws<PyTypeError>(() => Apply(key)).Message);
    }
}
