// F1b真实消费边界草稿：暖命中只绕过正文；空cold不执行逐项检查，但仍构造并采参。
// 未知域NI仅登记边界，绝不当作CP通过；完整CP回调/read/delete源仍保持pending。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingTupleCacheBoundariesTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("F1bBoundary");
    private static object? Apply(object? key) => PyOps.GetItem(Form("Tuple"), key);
    private static object? Other(string root, object? key) => PyOps.GetItem(Form(root), key);

    // 私有helper可能尚未导出；必须恢复真实存在性，不为测试伪造原始身份。
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

    private static void EvictEmptyThroughPublicSubscriptions()
    {
        // C#共享静态池，必须先插入128个全新成功键，再测真正冷空Tuple。
        for (int i = 0; i < 128; i++) Apply(Fresh());
    }

    [Fact]
    public void HotRawKeysSkipEveryColdHelperButNonemptyColdConsumesItsDependencies()
    {
        foreach (string helper in new[] { "_type_check", "_type_convert", "_collect_parameters", "_GenericAlias", "_TypingEllipsis", "Generic" })
        {
            object? key = Fresh(), cold = Fresh(), alias = Apply(key);
            WithChanged(helper, PyNone.Instance, () =>
            {
                Assert.Same(alias, Apply(key));
                Assert.Throws<PyNotImplementedError>(() => Apply(cold));
            });
            Assert.Same(alias, Apply(key));
        }
    }

    [Fact]
    public void ClassVarNoneExportAllowsNewNonemptyTupleRawKeys()
    {
        object? key = Fresh(), cold = Fresh(), warm = Apply(key);
        WithChanged("ClassVar", PyNone.Instance, () =>
        {
            Assert.Same(warm, Apply(key));
            object? created = Apply(new PyTuple([cold, PyType.Get("int")]));
            Assert.Equal(new[] { cold, PyType.Get("int") }, Assert.IsType<PyTuple>(Get(created, "__args__")).Items);
            Assert.Same(created, Apply(new PyTuple([cold, PyType.Get("int")])));
        });
    }

    [Fact]
    public void TupleNeverConsumesOrdinaryArityOrTupleTypeExport()
    {
        foreach (string helper in new[] { "_check_generic", "_TupleType" })
        {
            EvictEmptyThroughPublicSubscriptions();
            object? t = Fresh();
            WithChanged(helper, PyNone.Instance, () =>
            {
                foreach (object? raw in new object?[] { new PyTuple([]), t, new PyTuple([t]),
                    new PyTuple([t, PyType.Get("int"), PyType.Get("str")]),
                    new PyTuple([t, PyType.Get("int"), PyEllipsis.Instance]) })
                    Assert.Same(PyType.Get("tuple"), Get(Apply(raw), "__origin__"));
            });
        }
    }

    [Fact]
    public void EmptyColdTupleSkipsTypeChecksAndDoesNotReadTypingEllipsis()
    {
        foreach (string helper in new[] { "_type_check", "_type_convert", "_check_generic", "_TypingEllipsis", "Generic", "Protocol", "ClassVar", "Final", "Any", "_TupleType" })
        {
            EvictEmptyThroughPublicSubscriptions();
            WithChanged(helper, PyNone.Instance, () =>
            {
                object? alias = Apply(new PyTuple([]));
                Assert.Empty(Assert.IsType<PyTuple>(Get(alias, "__args__")).Items);
                Assert.Empty(Assert.IsType<PyTuple>(Get(alias, "__parameters__")).Items);
                Assert.Same(PyType.Get("tuple"), Get(alias, "__origin__"));
            });
        }
    }

    [Fact]
    public void EmptyColdTupleStillConsumesConstructorAndCollector()
    {
        foreach (string helper in new[] { "_GenericAlias", "_collect_parameters" })
        {
            EvictEmptyThroughPublicSubscriptions();
            WithChanged(helper, PyNone.Instance,
                () => Assert.Throws<PyNotImplementedError>(() => Apply(new PyTuple([]))));
            Assert.Empty(Assert.IsType<PyTuple>(Get(Apply(new PyTuple([])), "__args__")).Items);
        }
    }

    [Fact]
    public void StableMetadataSurvivesCollectorChangesIncludingFirstRead()
    {
        object? t = Fresh(), u = Fresh(), cold = Fresh();
        object? key = new PyTuple([t, Other("List", u), t, PyEllipsis.Instance]);
        object? alias = Apply(key), unread = Apply(new PyTuple([u, t]));
        object? args = Get(alias, "__args__"), parameters = Get(alias, "__parameters__");
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(parameters).Items);
        Assert.Same(args, Call(Form("get_args"), alias));
        WithChanged("_collect_parameters", PyNone.Instance, () =>
        {
            Assert.Same(alias, Apply(key));
            Assert.Same(args, Get(alias, "__args__"));
            Assert.Same(parameters, Get(alias, "__parameters__"));
            Assert.Equal(new[] { u, t }, Assert.IsType<PyTuple>(Get(unread, "__parameters__")).Items);
            Assert.Throws<PyNotImplementedError>(() => Apply(cold));
        });
        var plain = new PyClass("Plain");
        PyObject.SetAttr(plain, "__parameters__", new PyTuple([u]));
        Assert.Empty(Assert.IsType<PyTuple>(Get(Apply(plain), "__parameters__")).Items);
        Assert.NotSame(Get(Apply(t), "__args__"), Get(Apply(t), "__parameters__"));
    }

    [Fact]
    public void HotIdenticalLiteralKeyStillConsumesItsHashHelper()
    {
        object? literal = Other("Literal", Fresh()), alias = Apply(literal);
        WithChanged("_value_and_type_iter", PyNone.Instance,
            () => Assert.Throws<PyNotImplementedError>(() => Apply(literal)));
        Assert.Same(alias, Apply(literal));
    }

    [Fact]
    public void EquivalentNonidenticalAliasKeyStillConsumesEquality()
    {
        object? key = Fresh(), first = Other("Set", key), second = Other("Set", new PyTuple([key]));
        Assert.NotSame(first, second);
        Assert.True(PyHash.Equal(first, second));
        object? outer = Apply(first);
        WithChanged("_GenericAlias", PyNone.Instance, () =>
        {
            Assert.Same(outer, Apply(first));
            Assert.Throws<PyNotImplementedError>(() => Apply(second));
        });
        Assert.Same(outer, Apply(second));
    }

    private sealed class TupleChild(object?[] items) : PyTuple(items) { }

    [Fact]
    public void LegalUnknownEllipsisAndNonTypeDomainsRemainExplicitBoundaries()
    {
        object? t = Fresh();
        foreach (object? raw in new object?[] { PyEllipsis.Instance, new PyTuple([PyEllipsis.Instance]),
            new PyTuple([PyEllipsis.Instance, t]), new PyTuple([t, PyEllipsis.Instance, t]),
            new PyTuple([PyEllipsis.Instance, PyEllipsis.Instance]),
            "ForwardName", 7L, true, new PyList([]), new PyDict(), new TupleChild([t]) })
            Assert.Throws<PyNotImplementedError>(() => Apply(raw));
        // CP会先接受Ellipsis再遇非法tuple；本切不能先扫描tuple伪造CP错误。
        Assert.Throws<PyNotImplementedError>(() => Apply(new PyTuple([PyEllipsis.Instance, new PyTuple([])])));
        Assert.Throws<PyNotImplementedError>(() => Apply(new PyTuple([PyEllipsis.Instance, new PyTuple([]), PyEllipsis.Instance])));
    }

    [Fact]
    public void UserHashCallbacksRemainUnsupportedWithoutPretendingToExecuteThem()
    {
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("Key", [PyRuntimeBases.Validate(PyType.Get("object"))], null));
        int calls = 0;
        PyObject.SetAttr(cls, "__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 701L; }));
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Apply(Call(cls)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ClassVarRebindingSkipsWarmBodyButNewTupleRawKeyHasExactError()
    {
        object? key = Other("List", Fresh()), warm = Apply(key);
        WithChanged("ClassVar", PyType.Get("list"), () =>
        {
            Assert.Same(warm, Apply(key));
            // scalar暖键不变；单tuple新原键必须经过真正冷checker。
            Assert.Equal(PyRepr.Repr(key) + " is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Apply(new PyTuple([key]))).PyMessage);
        });
        Assert.Same(warm, Apply(key));
    }
}
