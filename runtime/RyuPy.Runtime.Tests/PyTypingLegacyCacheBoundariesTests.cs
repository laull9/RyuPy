// F1a 消费边界：暖命中只绕过冷body，原始键hash/eq仍必须消费。
// NI断言仅登记未支持边界，不是CP通过；完整CP回调和ClassVar冷错误源保持pending。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingLegacyCacheBoundariesTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("F1aBoundary");
    private static object? Apply(string name, object? key) => PyOps.GetItem(Form(name), key);

    // 模块helper在F0未导出时也不伪造原身份；结束后恢复原来的存在性。
    private static void WithChanged(string name, object? value, Action action)
    {
        bool existed = Typing.TryGetValue(name, out object? saved);
        Typing[name] = value;
        try { action(); }
        finally
        {
            if (existed) Typing[name] = saved;
            else Typing.Remove(name);
        }
    }

    [Fact]
    public void HotOrdinaryEntrySkipsColdHelpersWhileNewRawKeyIsExplicitlyUnsupported()
    {
        foreach (string helper in new[] { "_type_check", "_type_convert", "_check_generic", "_collect_parameters", "_GenericAlias", "_TypingEllipsis", "Generic" })
        {
            object? key = Fresh(), cold = Fresh(), alias = Apply("List", key);
            WithChanged(helper, PyNone.Instance, () =>
            {
                Assert.Same(alias, Apply("List", key));
                Assert.Throws<PyNotImplementedError>(() => Apply("List", cold));
            });
            Assert.Same(alias, Apply("List", key));
        }
    }

    // ClassVar=None不是冷helper失效：单独成功合同，不能删除其它helper的NI断言。
    [Fact]
    public void ClassVarNoneExportAllowsNewOrdinaryRawKeys()
    {
        object? key = Fresh(), cold = Fresh(), warm = Apply("List", key);
        WithChanged("ClassVar", PyNone.Instance, () =>
        {
            Assert.Same(warm, Apply("List", key));
            object? created = Apply("List", cold);
            Assert.Same(created, Apply("List", cold));
            Assert.Equal(new[] { cold }, Assert.IsType<PyTuple>(Get(created, "__args__")).Items);
        });
    }

    [Fact]
    public void HotIdenticalLiteralKeyStillConsumesItsHashHelper()
    {
        object? literal = Apply("Literal", Fresh()), alias = Apply("List", literal);
        WithChanged("_value_and_type_iter", PyNone.Instance,
            () => Assert.Throws<PyNotImplementedError>(() => Apply("List", literal)));
        Assert.Same(alias, Apply("List", literal));
    }

    [Fact]
    public void HotEquivalentAliasKeyStillConsumesEqualityButIdenticalKeyDoesNot()
    {
        object? key = Fresh(), first = Apply("Set", key), second = Apply("Set", new PyTuple([key]));
        Assert.NotSame(first, second);
        Assert.True(PyHash.Equal(first, second));
        object? outer = Apply("List", first);
        WithChanged("_GenericAlias", PyNone.Instance, () =>
        {
            Assert.Same(outer, Apply("List", first));
            Assert.Throws<PyNotImplementedError>(() => Apply("List", second));
        });
        Assert.Same(outer, Apply("List", second));
    }

    [Fact]
    public void StableMetadataIsReadableAfterColdCollectorChangesButNewCreationConsumesIt()
    {
        object? t = Fresh(), u = Fresh(), cold = Fresh();
        object? nested = Apply("Dict", new PyTuple([t, Apply("List", u)]));
        object? simple = Apply("List", t), unread = Apply("Sequence", t);
        object? args = Get(nested, "__args__"), parameters = Get(nested, "__parameters__");
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(parameters).Items);
        Assert.Same(args, Call(Form("get_args"), nested));
        Assert.NotSame(Get(simple, "__args__"), Get(simple, "__parameters__"));
        WithChanged("_collect_parameters", PyNone.Instance, () =>
        {
            Assert.Same(args, Get(nested, "__args__"));
            Assert.Same(parameters, Get(nested, "__parameters__"));
            Assert.Equal(new[] { t }, Assert.IsType<PyTuple>(Get(unread, "__parameters__")).Items);
            Assert.Same(simple, Apply("List", t));
            Assert.Throws<PyNotImplementedError>(() => Apply("List", cold));
        });
        var plain = new PyClass("Plain");
        PyObject.SetAttr(plain, "__parameters__", new PyTuple([u]));
        Assert.Empty(Assert.IsType<PyTuple>(Get(Apply("Container", plain), "__parameters__")).Items);
    }

    [Fact]
    public void SafeArityAndTypeErrorsRepeatWithTypeValidationBeforeArity()
    {
        object? integer = PyType.Get("int"), text = PyType.Get("str");
        foreach (var (root, key, message) in new (string, object?, string)[] {
            ("List", new PyTuple([]), "Too few arguments for typing.List; actual 0, expected 1"),
            ("List", new PyTuple([integer, text]), "Too many arguments for typing.List; actual 2, expected 1"),
            ("Dict", integer, "Too few arguments for typing.Dict; actual 1, expected 2"),
            ("Dict", new PyTuple([integer, text, PyType.Get("bytes")]), "Too many arguments for typing.Dict; actual 3, expected 2"),
            ("List", new PyTuple([new PyTuple([integer])]), "Parameters to generic types must be types. Got (<class 'int'>,)."),
            ("List", Form("Union"), "Plain typing.Union is not valid as type argument"),
            ("List", new PyTuple([Form("Generic"), integer]), "Plain <class 'typing.Generic'> is not valid as type argument"),
            ("Dict", new PyTuple([Form("Generic")]), "Plain <class 'typing.Generic'> is not valid as type argument") })
            for (int repeat = 0; repeat < 2; repeat++)
                Assert.Equal(message, Assert.Throws<PyTypeError>(() => Apply(root, key)).Message);
    }

    [Fact]
    public void KnownUnhashablePythonKeysRemainExplicitlyUnknownRatherThanFalseTypeErrors()
    {
        // CP可合法构造这些别名；这里只证明NI未知域，不证明实际hash的TypeError回退。
        foreach (object? key in new object?[] { new PyList([]), new PyDict(), new PyTuple([new PyList([])]) })
            Assert.Throws<PyNotImplementedError>(() => Apply("List", key));
        Assert.Throws<PyNotImplementedError>(() => Apply("Dict", new PyTuple([new PyList([]), PyType.Get("int")])));
    }

    private sealed class TupleChild(object?[] items) : PyTuple(items) { }

    [Fact]
    public void UnknownForwardNumericTupleSubclassAndUserCallbacksAreNotFalseCpythonPasses()
    {
        foreach (object? key in new object?[] { "Thing", 42L, true, 2.5, PyEllipsis.Instance, new TupleChild([Fresh()]) })
            Assert.Throws<PyNotImplementedError>(() => Apply("List", key));
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("Key", [PyRuntimeBases.Validate(PyType.Get("object"))], null));
        int calls = 0;
        PyObject.SetAttr(cls, "__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 701L; }));
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Apply("List", Call(cls)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void OriginalR1WarmListSurvivesClassVarRebindingButNewTupleKeyHasExactError()
    {
        object? key = Apply("List", PyType.Get("int")), warm = Apply("List", key);
        object? freshKey = Apply("List", Fresh()), freshWarm = Apply("List", freshKey);
        WithChanged("ClassVar", PyType.Get("list"), () =>
        {
            Assert.Same(warm, Apply("List", key));
            // 原R1的scalar暖断言原样保留；新的raw tuple在C1真冷checker准确拒绝。
            Assert.Same(freshWarm, Apply("List", freshKey));
            Assert.Equal(PyRepr.Repr(freshKey) + " is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Apply("List", new PyTuple([freshKey]))).PyMessage);
            object? rawPlain = Apply("List", new PyTuple([PyType.Get("int")]));
            Assert.Equal(new object?[] { PyType.Get("int") }, Assert.IsType<PyTuple>(Get(rawPlain, "__args__")).Items);
        });
        Assert.Same(warm, Apply("List", key));
    }
}
