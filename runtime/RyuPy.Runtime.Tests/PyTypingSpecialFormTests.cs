// F0公开入口合同：原始键、共享128项LRU、稳定元数据及真实类型分类。
// 不使用私有清空钩子；新TypeVar排空历史项，测试顺序不影响容量观测。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingSpecialFormTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Apply(string name, object? key) => PyOps.GetItem(Form(name), key);
    private static object? Union(object? left, object? right) => Apply("Union", new PyTuple([left, right]));
    private static object? Fresh() => Variable("F0Key");
    private static void Fill(int count)
    {
        for (int i = 0; i < count; i++) Union(Fresh(), PyType.Get("int"));
    }

    [Fact]
    public void EquivalentRawKeysDoNotCollapseToTheSameAlias()
    {
        object? key = Fresh(), integer = PyType.Get("int");
        object? a = Union(key, integer);
        object? b = Apply("Union", new PyTuple([key, integer, key]));
        object? c = Union(integer, key);
        Assert.Same(a, Union(key, integer));
        Assert.NotSame(a, b); Assert.NotSame(a, c);
        Assert.True(PyHash.Equal(a, b)); Assert.True(PyHash.Equal(a, c));
        Assert.Equal(PyHash.Of(a), PyHash.Of(b));
        Assert.Same(integer, Apply("Union", integer));
        Assert.Same(integer, Apply("Union", new PyTuple([integer])));
        Assert.Same(integer, Union(integer, integer));
        Assert.Same(PyType.Get("NoneType"), Apply("Optional", PyNone.Instance));
        object? none = Union(PyNone.Instance, key), noneType = Union(PyType.Get("NoneType"), key);
        Assert.NotSame(none, noneType); Assert.True(PyHash.Equal(none, noneType));
    }

    [Fact]
    public void SafeAliasAndTypeVariableKeysKeepPythonEqualityAndIdentity()
    {
        object? key = Fresh(), other = Fresh();
        Assert.NotSame(Apply("Optional", key), Apply("Optional", other));
        foreach (object? arg in new[] { Apply("List", key), PyOps.GetItem(PyType.Get("list"), key), Apply("Literal", 1L), Form("Any") })
        {
            object? result = Apply("Optional", arg);
            Assert.Same(result, Apply("Optional", arg));
            Assert.Same(result, Union(arg, PyType.Get("NoneType")));
        }
    }

    [Fact]
    public void EquivalentAliasKeysShareOuterEntryAndRetainedInnerResultSurvivesOuterEviction()
    {
        object? key = Fresh(), integer = PyType.Get("int");
        object? first = Union(key, integer), reversed = Union(integer, key);
        Assert.NotSame(first, reversed);
        object? outer = Apply("Optional", first);
        Assert.Same(outer, Apply("Optional", reversed));
        var keys = new object?[65]; var aliases = new object?[64];
        for (int i = 0; i < keys.Length; i++) keys[i] = Fresh();
        for (int i = 0; i < aliases.Length; i++) aliases[i] = Apply("Optional", keys[i]);
        Assert.Same(aliases[0], Union(keys[0], PyType.Get("NoneType")));
        Apply("Optional", keys[64]);
        Assert.Same(aliases[0], Apply("Optional", keys[0]));
    }

    [Fact]
    public void SharedCacheTouchesHotKeysAndEvictsTheLeastRecentEntry()
    {
        Fill(128);
        object? key = Fresh(), firstKey = Fresh(), integer = PyType.Get("int");
        object? hot = Union(key, integer), first = Union(firstKey, integer);
        Fill(126);
        Assert.Same(hot, Union(key, integer));
        Union(Fresh(), integer);
        Assert.Same(hot, Union(key, integer));
        Assert.NotSame(first, Union(firstKey, integer));
    }

    [Fact]
    public void OptionalUsesTwoEntriesAndHotOuterHitDoesNotTouchInnerUnion()
    {
        Fill(128);
        object? key = Fresh(), alias = Apply("Optional", key);
        Fill(126);
        Assert.Same(alias, Apply("Optional", key));
        Union(Fresh(), PyType.Get("int"));
        Assert.Same(alias, Apply("Optional", key));
        Assert.NotSame(alias, Union(key, PyType.Get("NoneType")));
        Fill(128);
        key = Fresh(); alias = Union(key, PyType.Get("int"));
        Fill(126);
        Apply("Optional", Fresh());
        Assert.NotSame(alias, Union(key, PyType.Get("int")));
    }

    [Fact]
    public void LiteralDoesNotConsumeSpecialFormPool()
    {
        Fill(128);
        object? key = Fresh(), alias = Union(key, PyType.Get("int"));
        for (long i = 0; i < 150; i++) Apply("Literal", i);
        Assert.Same(alias, Union(key, PyType.Get("int")));
    }

    [Fact]
    public void NonCallableArgsAndParametersAreStableAndGenericTypeIsUnchanged()
    {
        object? key = Fresh();
        foreach (object? alias in new[] { Union(key, PyType.Get("int")), Apply("Optional", key), Apply("List", key), Apply("Literal", 1L), PyOps.GetItem(PyType.Get("list"), key), Subscribe(key) })
        {
            object? args = Get(alias, "__args__");
            Assert.IsType<PyTuple>(args);
            Assert.Same(args, Get(alias, "__args__"));
            Assert.Same(args, Call(Form("get_args"), alias));
            Assert.Same(Get(alias, "__parameters__"), Get(alias, "__parameters__"));
        }
        Assert.Same(PyObject.TypeOf(Subscribe(key)), PyObject.TypeOf(Apply("List", key)));
        Assert.NotSame(PyObject.TypeOf(Subscribe(key)), PyObject.TypeOf(Union(key, PyType.Get("int"))));
    }

    [Fact]
    public void RootTypesHaveTypingMetadataWithoutClaimingAnyClassBehavior()
    {
        Assert.Same(PyObject.TypeOf(Form("Union")), PyObject.TypeOf(Form("Optional")));
        Assert.NotSame(PyObject.TypeOf(Form("Union")), PyObject.TypeOf(Form("Literal")));
        foreach (var (name, expected) in new[] { ("Union", "_SpecialForm"), ("Optional", "_SpecialForm"), ("Literal", "_LiteralSpecialForm"), ("List", "_SpecialGenericAlias"), ("Any", "_AnyMeta") })
        {
            object? value = Form(name), type = PyObject.TypeOf(value);
            Assert.Same(type, Get(value, "__class__"));
            Assert.Equal("typing", Get(type, "__module__"));
            Assert.Equal(expected, Get(type, "__name__"));
            Assert.Equal($"<class 'typing.{expected}'>", PyRepr.Repr(type));
        }
    }

    [Fact]
    public void KnownInvalidTuplesKeepExactMessages()
    {
        foreach (object?[] values in new[] { Array.Empty<object?>(), new object?[] { PyType.Get("int") }, new object?[] { PyType.Get("int"), PyType.Get("str") } })
        {
            var key = new PyTuple(values);
            Assert.Equal($"typing.Optional requires a single type. Got {PyRepr.Repr(key)}.", Assert.Throws<PyTypeError>(() => Apply("Optional", key)).Message);
        }
        Assert.Equal("Cannot take a Union of no types.", Assert.Throws<PyTypeError>(() => Apply("Union", new PyTuple([]))).Message);
        Assert.Equal("Union[arg, ...]: each arg must be a type. Got (<class 'int'>,).", Assert.Throws<PyTypeError>(() => Union(new PyTuple([PyType.Get("int")]), PyType.Get("str"))).Message);
        Assert.Equal("Plain typing.Union is not valid as type argument", Assert.Throws<PyTypeError>(() => Apply("Optional", Form("Union"))).Message);
        Assert.Equal("Plain typing.Optional is not valid as type argument", Assert.Throws<PyTypeError>(() => Union(Form("Optional"), PyType.Get("int"))).Message);
    }
}
