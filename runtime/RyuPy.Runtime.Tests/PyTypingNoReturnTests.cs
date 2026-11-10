// NoReturn 跨消费者合同：CP3.12.13 固定完整源双跑已留证；这里是待 hosted 执行的产品测试。
// 模块全局每次恢复，原始键用新 TypeVar 隔离缓存；不把源码审查当作运行通过。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingNoReturnTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Apply(string name, object? value) => PyOps.GetItem(Form(name), value);
    private static object? Bound(object? value) => PyOps.CallValue(Form("TypeVar"), ["NBound"], ["bound"], [value]);
    private static object? Named(object? value) => Call(Form("NamedTuple"), "NPoint", new PyList([new PyTuple(["x", value])]));
    private static object? Typed(object? value)
    {
        var fields = new PyDict(); fields.Set("x", value);
        return Call(Form("TypedDict"), "NRecord", fields);
    }
    private static object? Annotation(object? value) => PyOps.GetItem(Get(value, "__annotations__"), "x");
    private static void WithExport(object? value, Action action, bool delete = false)
    {
        object? original = Form("NoReturn");
        if (delete) Typing.Remove("NoReturn"); else Typing["NoReturn"] = value;
        try { action(); }
        finally { Typing["NoReturn"] = original; }
    }

    [Fact]
    public void RootUsesCanonicalSpecialFormAndAccuratePublicProtocols()
    {
        object? n = Form("NoReturn");
        Assert.Same(n, Get(PyModules.Import("typing"), "NoReturn"));
        Assert.Same(PyObject.TypeOf(Form("Union")), PyObject.TypeOf(n));
        Assert.Same(PyObject.TypeOf(n), Get(n, "__class__"));
        Assert.Equal("typing.NoReturn", PyRepr.Repr(n));
        Assert.Equal("NoReturn", Get(n, "__name__"));
        Assert.Equal("NoReturn", Get(n, "__qualname__"));
        Assert.Equal("typing", Get(n, "__module__"));
        Assert.Same(PyNone.Instance, Call(Form("get_origin"), n));
        Assert.Empty(Assert.IsType<PyTuple>(Call(Form("get_args"), n)).Items);
        foreach (string name in new[] { "__origin__", "__args__", "__parameters__" })
            Assert.Equal(name, Assert.Throws<PyAttributeError>(() => Get(n, name)).PyMessage);
        foreach (object? target in new[] { n, Get(n, "__call__") })
            Assert.Equal("Cannot instantiate typing.NoReturn",
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(target, [1L], ["x"], [2L])).PyMessage);
        Assert.Equal("typing.NoReturn is not subscriptable",
            Assert.Throws<PyTypeError>(() => PyOps.GetItem(n, PyType.Get("int"))).PyMessage);
        Assert.Equal("Cannot subclass typing.NoReturn", Assert.Throws<PyTypeError>(() => PyRuntimeBases.Validate(n)).PyMessage);
        Assert.Equal("Cannot subclass typing.NoReturn",
            Assert.Throws<PyTypeError>(() => Call(Get(n, "__mro_entries__"), new PyTuple([n]))).PyMessage);
    }

    [Fact]
    public void RuntimeClassChecksKeepTupleShortCircuitAndRootError()
    {
        object? n = Form("NoReturn");
        foreach (string operation in new[] { "isinstance", "issubclass" })
        {
            object? matched = operation == "isinstance" ? 1L : PyType.Get("int");
            object? missed = operation == "isinstance" ? "text" : PyType.Get("str");
            string expected = "typing.NoReturn cannot be used with " + operation + "()";
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Builtin(operation, matched, n)).PyMessage);
            Assert.Equal(true, Builtin(operation, matched, new PyTuple([PyType.Get("int"), n])));
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Builtin(operation, missed, new PyTuple([PyType.Get("int"), n]))).PyMessage);
        }
        Assert.Equal("typing.NoReturn cannot be used with issubclass()",
            Assert.Throws<PyTypeError>(() => Builtin("issubclass", 42L, n)).PyMessage);
    }

    [Fact]
    public void EveryTypeCheckingConsumerAcceptsOriginalNoReturn()
    {
        object? n = Form("NoReturn"), marker = Variable("NMarker");
        foreach (string root in new[] { "List", "Set", "FrozenSet", "Iterable", "Iterator", "Sequence", "Collection", "Container", "ClassVar", "Tuple" })
        {
            object? alias = Apply(root, n);
            Assert.Same(n, Assert.IsType<PyTuple>(Get(alias, "__args__")).Items[0]);
            Assert.Same(alias, Apply(root, n));
        }
        foreach (string root in new[] { "Dict", "Mapping", "MutableMapping" })
            Assert.Same(n, Assert.IsType<PyTuple>(Get(Apply(root, new PyTuple([marker, n])), "__args__")).Items[1]);
        Assert.Same(n, Apply("Union", n));
        Assert.Same(n, Assert.IsType<PyTuple>(Get(Apply("Optional", n), "__args__")).Items[0]);
        object? callable = Apply("Callable", new PyTuple([new PyList([n]), n]));
        Assert.Equal(new[] { n, n }, Assert.IsType<PyTuple>(Get(callable, "__args__")).Items);
        Assert.Same(n, Get(Bound(n), "__bound__"));
        Assert.Same(n, Annotation(Named(n)));
        Assert.Same(n, Annotation(Typed(n)));
        Assert.Same(n, Assert.IsType<PyTuple>(Get(PyOps.GetItem(PyType.Get("list"), n), "__args__")).Items[0]);
        Assert.Equal("typing.Union[typing.NoReturn, int]", PyRepr.Repr(PyOps.Binary(PyOps.BitOr, n!, PyType.Get("int"))));
        Assert.Equal("typing.Union[int, typing.NoReturn]", PyRepr.Repr(PyOps.Binary(PyOps.BitOr, PyType.Get("int"), n!)));
    }

    [Fact]
    public void MissingExportBreaksColdConsumersButNotWarmOrUncheckedPositions()
    {
        object? n = Form("NoReturn"), marker = Variable("NMissing"), warm = Apply("List", marker);
        WithExport(null, () =>
        {
            Assert.Same(warm, Apply("List", marker));
            Func<object?>[] cold = [() => Apply("List", new PyTuple([marker])), () => Bound(Form("Any")),
                () => Named(Form("Any")), () => Typed(Form("Any"))];
            foreach (var consume in cold)
                Assert.Equal("name 'NoReturn' is not defined", Assert.Throws<PyNameError>(() => consume()).PyMessage);
            Assert.NotNull(Call(Form("TypedDict"), "Empty", new PyDict()));
            Assert.Empty(Assert.IsType<PyTuple>(Get(Apply("Tuple", new PyTuple([])), "__args__")).Items);
            Assert.Same(n, Assert.IsType<PyTuple>(Get(PyOps.GetItem(PyType.Get("list"), n), "__args__")).Items[0]);
            object? constraints = Call(Form("TypeVar"), "Constraints", n, Form("Any"));
            Assert.Equal(new[] { n, Form("Any") }, Assert.IsType<PyTuple>(Get(constraints, "__constraints__")).Items);
        }, delete: true);
        Assert.NotSame(warm, Apply("List", new PyTuple([marker])));
    }

    [Fact]
    public void RebindingChangesMembershipAndSavedRootStillHasOwnIdentity()
    {
        object? n = Form("NoReturn"), marker = Variable("NRebound");
        WithExport(PyType.Get("int"), () =>
        {
            Func<object?>[] cold = [() => Apply("Union", new PyTuple([n, marker])), () => Bound(n), () => Named(n), () => Typed(n)];
            foreach (var consume in cold)
                Assert.Equal("Plain typing.NoReturn is not valid as type argument", Assert.Throws<PyTypeError>(() => consume()).PyMessage);
            Assert.Same(PyType.Get("int"), Get(Bound(PyType.Get("int")), "__bound__"));
            Assert.Equal("typing.NoReturn", PyRepr.Repr(n));
        });
        WithExport(Form("Union"), () =>
        {
            Assert.Same(Form("Union"), Apply("Union", new PyTuple([Form("Union")])));
            Assert.Same(Form("Union"), Get(Bound(Form("Union")), "__bound__"));
            Assert.Same(Form("Union"), Annotation(Named(Form("Union"))));
        });
        WithExport(Form("ClassVar"), () => Assert.Same(Form("ClassVar"), Annotation(Typed(Form("ClassVar")))));
        WithExport(Form("Generic"), () =>
        {
            Assert.Same(Form("Generic"), Get(Bound(Form("Generic")), "__bound__"));
            Assert.Same(Form("Generic"), Annotation(Named(Form("Generic"))));
            Assert.Same(Form("Generic"), Annotation(Typed(Form("Generic"))));
        });
    }

    [Fact]
    public void UnknownComparisonOnlyRejectsAtActualMembershipAfterAliasChecks()
    {
        object? cv = Apply("ClassVar", Variable("NBadAlias"));
        var unknown = new Probe();
        WithExport(unknown, () =>
        {
            Assert.Same(Form("Any"), Get(Bound(Form("Any")), "__bound__"));
            Assert.Same(Form("Any"), Annotation(Named(Form("Any"))));
            Assert.Same(Form("Any"), Annotation(Typed(Form("Any"))));
            Assert.Throws<PyNotImplementedError>(() => Bound(PyType.Get("int")));
            Assert.Throws<PyNotImplementedError>(() => Named(PyType.Get("int")));
            Assert.Throws<PyNotImplementedError>(() => Typed(PyType.Get("int")));
            foreach (Func<object?, object?> consume in new Func<object?, object?>[] { Bound, Named, Typed })
                Assert.Equal(PyRepr.Repr(cv) + " is not valid as type argument", Assert.Throws<PyTypeError>(() => consume(cv)).PyMessage);
            Assert.Equal(0, unknown.Lookups);
        });
        WithExport(null, () => Assert.Throws<PyTypeError>(() => Bound(cv)), delete: true);
    }

    [Fact]
    public void NoneReplacementPreservesOldOrdinaryBoundNamedAndCallableBehavior()
    {
        object? marker = Variable("NNone"), any = Form("Any");
        WithExport(PyNone.Instance, () =>
        {
            Assert.Same(PyType.Get("int"), Get(Bound(PyType.Get("int")), "__bound__"));
            Assert.Same(PyType.Get("int"), Annotation(Named(PyType.Get("int"))));
            object? callable = Apply("Callable", new PyTuple([new PyTuple([marker]), PyType.Get("int")]));
            Assert.Equal(new[] { marker, PyType.Get("int") }, Assert.IsType<PyTuple>(Get(callable, "__args__")).Items);
            Assert.Same(any, Get(Bound(any), "__bound__"));
        });
    }

    [Fact]
    public void LaterHelperIsConsumedBeforeAnyEarlyReturnAndUnknownHashRemainsExplicit()
    {
        Typing["Never"] = PyNone.Instance;
        try { Assert.Throws<PyNotImplementedError>(() => Bound(Form("Any"))); }
        finally { Typing.Remove("Never"); }
        var key = new Probe();
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Form("NoReturn"), key));
        Assert.Equal(0, key.Lookups);
    }

    [Fact]
    public void LiteralOnlyAddsImmutableFormIdentityAndPreservesOldValueDomain()
    {
        object? n = Form("NoReturn"), union = Form("Union"), any = Form("Any");
        object? one = Apply("Literal", n), duplicate = Apply("Literal", new PyTuple([n, n]));
        // 不断言不同原始键的 alias 身份；旧归一化缓存不是 CP 原始键缓存。
        Assert.Equal(new[] { n }, Assert.IsType<PyTuple>(Get(duplicate, "__args__")).Items);
        Assert.Same(one, Apply("Literal", n));
        Assert.True(PyHash.Equal(one, duplicate));
        Assert.Equal(PyHash.Of(one), PyHash.Of(duplicate));
        object? many = Apply("Literal", new PyTuple([n, union, any, n]));
        Assert.Equal(new[] { n, union, any }, Assert.IsType<PyTuple>(Get(many, "__args__")).Items);
        Assert.False(PyHash.Equal(one, Apply("Literal", union)));
        Assert.False(PyHash.Equal(Apply("Literal", true), Apply("Literal", 1L)));
        object? old = Apply("Literal", new PyTuple([PyNone.Instance, true, 1L, "value", true]));
        Assert.Equal(new object?[] { PyNone.Instance, true, 1L, "value" }, Assert.IsType<PyTuple>(Get(old, "__args__")).Items);
        var unknown = new Probe();
        Assert.Throws<PyNotImplementedError>(() => Apply("Literal", unknown));
        Assert.Equal(0, unknown.Lookups);
    }

    private sealed class Probe : PyNativeObject
    {
        internal int Lookups;
        public override string TypeName => "NProbe";
        public override object? GetAttr(string name) { Lookups++; throw Missing(name); }
    }
}
