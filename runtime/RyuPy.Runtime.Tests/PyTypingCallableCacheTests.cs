// F1c Callable测试先行合同：公开订阅、原始键身份、独立128项LRU及真实消费顺序。
// 不读取/清理内部缓存，不反射生产布局；fresh TypeVar隔离静态池历史，模块改绑必恢复。
// NI只登记有限域边界，不代表CP完整程序通过；ClassVar/C1完整合同仍pending。
// 测试先行不代表候选已通过；统一冻结、编译及验收由父任务负责。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingCallableCacheTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("F1cCallable");
    private static PyTuple Pair(object? args, object? result) => new([args, result]);
    private static object? Apply(object? args, object? result) => PyOps.GetItem(Form("Callable"), Pair(args, result));
    private static object? Other(string name, object? key) => PyOps.GetItem(Form(name), key);
    private static object? One(object? key) => Apply(new PyList([key]), PyType.Get("int"));
    private static PyTuple Arguments(object? alias) => Assert.IsType<PyTuple>(Get(alias, "__args__"));

    // helper未导出也有意义：退出时恢复原存在性，而非遗留伪造的模块属性。
    private static void WithChanged(string name, object? value, Action action)
    {
        bool existed = Typing.TryGetValue(name, out object? saved);
        Typing[name] = value;
        try { action(); }
        finally { if (existed) Typing[name] = saved; else Typing.Remove(name); }
    }

    [Fact]
    public void OuterShapeErrorsAreExactAndPrecedeHashAndColdHelpers()
    {
        const string message = "Callable must be used as Callable[[arg, ...], result].";
        object? root = Form("Callable"), key = Fresh();
        object? literal = Other("Literal", key);
        foreach (string helper in new[] { "_type_check", "_type_convert", "_CallableGenericAlias", "_value_and_type_iter" })
            WithChanged(helper, PyNone.Instance, () =>
            {
                foreach (object? invalid in new object?[] { PyNone.Instance, key, 3L,
                    new PyList([key, key]), new PyTuple([]), new PyTuple([literal]), new PyTuple([literal, key, key]) })
                    for (int repeat = 0; repeat < 2; repeat++)
                        Assert.Equal(message, Assert.Throws<PyTypeError>(() => PyOps.GetItem(root, invalid)).Message);
            });
    }

    [Fact]
    public void ExactListSnapshotsShareTupleKeysButNotScalarKeys()
    {
        object? t = Fresh(), u = Fresh(), result = Fresh();
        var list = new PyList([t]);
        object? first = Apply(list, result), scalar = Apply(t, result);
        Assert.Same(first, Apply(new PyTuple([t]), result));
        Assert.NotSame(first, scalar);
        Assert.True(PyHash.Equal(first, scalar));
        Assert.Equal(PyHash.Of(first), PyHash.Of(scalar));
        PyOps.SetItem(list, 0L, u);
        object? changed = Apply(list, result);
        Assert.NotSame(first, changed);
        Assert.Equal(new[] { t, result }, Arguments(first).Items);
        Assert.Equal(new[] { u, result }, Arguments(changed).Items);
        PyOps.SetItem(list, 0L, t);
        Assert.Same(first, Apply(list, result));
        object? empty = Apply(new PyList([]), result);
        Assert.Same(empty, Apply(new PyTuple([]), result));
        Assert.Equal(new[] { result }, Arguments(empty).Items);
    }

    [Fact]
    public void ArgumentAndResultNoneConversionsDoNotMergeRawKeys()
    {
        object? marker = Fresh();
        object?[] none = [PyNone.Instance, PyType.Get("NoneType")];
        var aliases = new object?[4];
        for (int i = 0; i < 4; i++)
        {
            aliases[i] = Apply(new PyList([marker, none[i / 2]]), none[i % 2]);
            Assert.Same(aliases[i], Apply(new PyTuple([marker, none[i / 2]]), none[i % 2]));
            Assert.Equal(new[] { marker, PyType.Get("NoneType"), PyType.Get("NoneType") }, Arguments(aliases[i]).Items);
            for (int j = 0; j < i; j++)
            {
                Assert.NotSame(aliases[j], aliases[i]);
                Assert.True(PyHash.Equal(aliases[j], aliases[i]));
                Assert.Equal(PyHash.Of(aliases[j]), PyHash.Of(aliases[i]));
            }
        }
    }

    [Fact]
    public void SavedRootAndMethodSurvivePublicExportRebinding()
    {
        object? root = Form("Callable"), method = Get(root, "__getitem__"), t = Fresh(), u = Fresh();
        object? warmKey = Pair(new PyList([t]), t), coldKey = Pair(new PyTuple([u]), t);
        object? warm = PyOps.GetItem(root, warmKey);
        WithChanged("Callable", PyNone.Instance, () =>
        {
            Assert.Same(warm, PyOps.GetItem(root, warmKey));
            Assert.Same(warm, Call(method, warmKey));
            object? cold = Call(method, coldKey);
            Assert.Same(cold, PyOps.GetItem(root, coldKey));
            Assert.Equal(new[] { u, t }, Arguments(cold).Items);
            Assert.Same(Get(PyModules.Import("collections.abc"), "Callable"), Get(cold, "__origin__"));
        });
    }

    [Fact]
    public void WarmIdentitySkipsAllColdDependenciesAndColdMissConsumesThem()
    {
        foreach (string helper in new[] { "_type_check", "_type_convert", "Generic", "Protocol", "Final",
            "_GenericAlias", "Any", "LiteralString", "Never", "Self", "TypeAlias", "_SpecialForm",
            "_CallableGenericAlias", "_TypingEllipsis", "_collect_parameters" })
        {
            object? t = Fresh(), cold = Fresh(), alias = One(t);
            WithChanged(helper, PyNone.Instance, () =>
            {
                Assert.Same(alias, One(t));
                Assert.Same(alias, Apply(new PyTuple([t]), PyType.Get("int")));
                Assert.Throws<PyNotImplementedError>(() => One(cold));
            });
            Assert.Same(alias, One(t));
            Assert.Same(One(cold), One(cold));
        }
    }

    [Fact]
    public void ClassVarNoneExportAllowsColdCallableWithOrdinaryResult()
    {
        object? key = Fresh(), cold = Fresh(), warm = One(key);
        WithChanged("ClassVar", PyNone.Instance, () =>
        {
            Assert.Same(warm, One(key));
            object? created = One(cold);
            Assert.Same(created, Apply(new PyTuple([cold]), PyType.Get("int")));
            Assert.Equal(new[] { cold, PyType.Get("int") }, Arguments(created).Items);
        });
    }

    [Fact]
    public void CallableTypeAndOrdinaryArityHelpersAreNeverConsumed()
    {
        foreach (string helper in new[] { "_CallableType", "_check_generic" })
        {
            object? result = Fresh(), t = Fresh(), u = Fresh();
            WithChanged(helper, PyNone.Instance, () =>
            {
                foreach (object? args in new object?[] { new PyList([]), t, new PyTuple([t]), new PyList([t, u, t]) })
                {
                    object? alias = Apply(args, result);
                    Assert.Same(alias, Apply(args, result));
                    Assert.Same(result, Arguments(alias).Items[^1]);
                }
            });
        }
    }

    [Fact]
    public void EmptyArgsStillCheckResultConstructConsumeSentinelAndCollect()
    {
        foreach (string helper in new[] { "_type_check", "_type_convert", "_CallableGenericAlias", "_TypingEllipsis", "_collect_parameters" })
        {
            // 每个result均新建，确保空参数订阅是真miss，无需反射清池。
            object? result = Fresh();
            WithChanged(helper, PyNone.Instance,
                () => Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([]), result)));
            object? alias = Apply(new PyList([]), result);
            Assert.Equal(new[] { result }, Arguments(alias).Items);
            Assert.Equal(new[] { result }, Assert.IsType<PyTuple>(Get(alias, "__parameters__")).Items);
        }
    }

    [Fact]
    public void InvalidResultWinsOverForwardArgumentConversionAndHasExactErrors()
    {
        object? t = Fresh(), genericAlias = Subscribe(t);
        foreach (var (result, message) in new (object?, string)[] {
            (new PyTuple([]), "Callable[args, result]: result must be a type. Got ()."),
            (new PyTuple([PyType.Get("int")]), "Callable[args, result]: result must be a type. Got (<class 'int'>,)."),
            (Form("Generic"), "Plain <class 'typing.Generic'> is not valid as type argument"),
            (genericAlias, "typing.Generic[~F1cCallable] is not valid as type argument"),
            (Form("Union"), "Plain typing.Union is not valid as type argument") })
            for (int repeat = 0; repeat < 2; repeat++)
                Assert.Equal(message, Assert.Throws<PyTypeError>(() => Apply(new PyList(["ForwardArgument"]), result)).Message);
    }

    [Fact]
    public void BareFormsAreLegalArgumentsAndAreNotPassedThroughResultTypeCheck()
    {
        object? result = Fresh();
        foreach (string name in new[] { "Generic", "Union", "Optional", "Literal" })
        {
            object? argument = Form(name), alias = Apply(new PyList([argument]), result);
            Assert.Equal(new[] { argument, result }, Arguments(alias).Items);
            Assert.Same(alias, Apply(new PyTuple([argument]), result));
            object? scalar = Apply(argument, result);
            Assert.NotSame(alias, scalar);
            Assert.True(PyHash.Equal(alias, scalar));
            Assert.Equal(new[] { result }, Assert.IsType<PyTuple>(Get(alias, "__parameters__")).Items);
        }
    }

    [Fact]
    public void TrueLru128TouchesHitsAndRebuildsEvictedAliasesFresh()
    {
        var keys = new object?[129]; var aliases = new object?[128];
        for (int i = 0; i < keys.Length; i++) keys[i] = Fresh();
        for (int i = 0; i < aliases.Length; i++) aliases[i] = One(keys[i]);
        Assert.Same(aliases[0], One(keys[0]));
        One(keys[128]);
        Assert.Same(aliases[0], One(keys[0]));
        object? rebuilt = One(keys[1]);
        Assert.NotSame(aliases[1], rebuilt);
        Assert.True(PyHash.Equal(aliases[1], rebuilt));
        Assert.Same(rebuilt, One(keys[1]));
    }

    [Fact]
    public void RepeatedFailedResultsNeitherEvictNorOccupyCapacity()
    {
        var keys = new object?[129]; var aliases = new object?[128];
        for (int i = 0; i < keys.Length; i++) keys[i] = Fresh();
        for (int i = 0; i < aliases.Length; i++) aliases[i] = One(keys[i]);
        for (int i = 0; i < 160; i++)
            Assert.Throws<PyTypeError>(() => Apply(new PyList([Fresh()]), i % 2 == 0 ? new PyTuple([]) : Form("Generic")));
        // 顺序触碰全部条目后仍保持原先LRU顺序；不是只检查最新项。
        for (int i = 0; i < aliases.Length; i++) Assert.Same(aliases[i], One(keys[i]));
        One(keys[128]);
        Assert.Same(aliases[1], One(keys[1]));
        Assert.NotSame(aliases[0], One(keys[0]));
    }

    private static readonly string[] OrdinaryRoots = ["List", "Dict", "Set", "FrozenSet", "Iterable", "Iterator", "Sequence", "Mapping", "MutableMapping", "Collection", "Container"];
    private static object? Ordinary(string root, object? key) => Other(root,
        root is "Dict" or "Mapping" or "MutableMapping" ? new PyTuple([key, PyType.Get("int")]) : key);

    [Fact]
    public void CallableIsBidirectionallyIsolatedFromEveryOtherCacheFamily()
    {
        object? key = Fresh(), callable = One(key);
        for (long i = 0; i < 140; i++)
        {
            object? t = Fresh();
            foreach (string root in OrdinaryRoots) Ordinary(root, t);
            Other("Tuple", t); Other("Optional", t);
            Other("Union", Pair(t, PyType.Get("int")));
            Subscribe(t); Other("Literal", i); PyOps.GetItem(PyType.Get("list"), t);
        }
        Assert.Same(callable, One(key));
        var ordinary = new object?[OrdinaryRoots.Length];
        for (int i = 0; i < ordinary.Length; i++) ordinary[i] = Ordinary(OrdinaryRoots[i], key);
        object? tuple = Other("Tuple", key), optional = Other("Optional", key);
        object? union = Other("Union", Pair(key, PyType.Get("int"))), generic = Subscribe(key), literal = Other("Literal", key);
        for (int i = 0; i < 140; i++) One(Fresh());
        for (int i = 0; i < ordinary.Length; i++) Assert.Same(ordinary[i], Ordinary(OrdinaryRoots[i], key));
        Assert.Same(tuple, Other("Tuple", key)); Assert.Same(optional, Other("Optional", key));
        Assert.Same(union, Other("Union", Pair(key, PyType.Get("int"))));
        Assert.Same(generic, Subscribe(key)); Assert.Same(literal, Other("Literal", key));
        // native list[T]没有身份缓存；不虚构其驻留身份，也不承诺Literal/native旧池彼此隔离。
    }

    [Fact]
    public void MetadataIsStableButGetArgsReturnsFreshOuterTupleAndArgumentList()
    {
        object? t = Fresh(), u = Fresh(), nested = Other("List", u);
        object? alias = Apply(new PyList([t, nested, t]), u), unread = Apply(new PyList([u, t]), u);
        object? args = Get(alias, "__args__"), parameters = Get(alias, "__parameters__");
        Assert.Equal(new[] { t, nested, t, u }, Assert.IsType<PyTuple>(args).Items);
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(parameters).Items);
        WithChanged("_collect_parameters", PyNone.Instance, () =>
        {
            Assert.Same(args, Get(alias, "__args__"));
            Assert.Same(parameters, Get(alias, "__parameters__"));
            Assert.Equal(new[] { u, t }, Assert.IsType<PyTuple>(Get(unread, "__parameters__")).Items);
        });
        var first = Assert.IsType<PyTuple>(Call(Form("get_args"), alias));
        var second = Assert.IsType<PyTuple>(Call(Form("get_args"), alias));
        Assert.NotSame(first, second); Assert.NotSame(first.Items[0], second.Items[0]);
        Assert.IsType<PyList>(first.Items[0]); Assert.IsType<PyList>(second.Items[0]);
        Assert.Same(u, first.Items[1]);
        PyOps.SetItem(first.Items[0], 0L, PyType.Get("str"));
        Assert.Same(t, PyOps.GetItem(second.Items[0], 0L));
        Assert.Same(t, PyOps.GetItem(Assert.IsType<PyTuple>(Call(Form("get_args"), alias)).Items[0], 0L));
        Assert.Same(t, Arguments(alias).Items[0]);
    }

    [Fact]
    public void CallableAliasHasItsRealTypeNameAndCanonicalMetadata()
    {
        object? alias = Apply(new PyList([PyType.Get("int"), PyType.Get("str")]), PyType.Get("bool"));
        Assert.Equal("_CallableGenericAlias", Get(PyObject.TypeOf(alias), "__name__"));
        Assert.Same(PyObject.TypeOf(alias), Get(alias, "__class__"));
        Assert.Equal("_CallableType", Get(PyObject.TypeOf(Form("Callable")), "__name__"));
        Assert.Equal("typing", Get(alias, "__module__")); Assert.Equal("Callable", Get(alias, "__name__"));
        object? origin = Get(PyModules.Import("collections.abc"), "Callable");
        Assert.Same(origin, Get(alias, "__origin__")); Assert.Same(origin, Call(Form("get_origin"), alias));
        Assert.Equal("typing.Callable[[int, str], bool]", PyRepr.Repr(alias));
        Assert.Empty(Assert.IsType<PyTuple>(Get(alias, "__parameters__")).Items);
        object? ordinaryType = PyObject.TypeOf(Other("List", Fresh())), callableType = PyObject.TypeOf(alias);
        Assert.Equal("_GenericAlias", Get(ordinaryType, "__name__"));
        // 专用Callable类的最小继承边也属于可观察身份；完整MRO合同仍pending。
        Assert.Equal(true, Builtin("isinstance", alias, ordinaryType));
        Assert.Equal(true, Builtin("issubclass", callableType, ordinaryType));
        Assert.Equal(false, Builtin("issubclass", ordinaryType, callableType));
        Assert.Equal(false, Builtin("isinstance", Other("List", Fresh()), callableType));
        Assert.Equal("_GenericAlias", Get(PyObject.TypeOf(Other("Tuple", Fresh())), "__name__"));
    }

    [Fact]
    public void IdenticalWarmLiteralKeysStillConsumeTheirRealHashHelper()
    {
        object? literal = Other("Literal", Fresh()), result = Fresh();
        object? argumentAlias = Apply(new PyList([literal]), result), resultAlias = Apply(new PyList([]), literal);
        WithChanged("_value_and_type_iter", PyNone.Instance, () =>
        {
            Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([literal]), result));
            Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([]), literal));
        });
        Assert.Same(argumentAlias, Apply(new PyList([literal]), result));
        Assert.Same(resultAlias, Apply(new PyList([]), literal));
    }

    [Fact]
    public void EquivalentNonidenticalArgsAndResultsStillConsumeRealEquality()
    {
        object? t = Fresh(), result = Fresh(), a = Other("Set", t), b = Other("Set", new PyTuple([t]));
        Assert.NotSame(a, b); Assert.True(PyHash.Equal(a, b));
        object? argAlias = Apply(new PyList([a]), result), resultAlias = Apply(new PyList([]), a);
        WithChanged("_GenericAlias", PyNone.Instance, () =>
        {
            Assert.Same(argAlias, Apply(new PyTuple([a]), result));
            Assert.Same(resultAlias, Apply(new PyList([]), a));
            Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([b]), result));
            Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([]), b));
        });
        Assert.Same(argAlias, Apply(new PyList([b]), result));
        Assert.Same(resultAlias, Apply(new PyList([]), b));
    }

    private sealed class TupleChild(object?[] items) : PyTuple(items) { }

    [Fact]
    public void UnknownKeysAndDerivedContainersRemainExplicitProtocolBoundaries()
    {
        object? t = Fresh();
        var listClass = (PyClass)PyOps.MakeClass("F1cListChild", [PyType.Get("list")], null);
        PyOps.FinalizeOrdinaryClass(listClass);
        object? listChild = Call(listClass, new PyList([t]));
        Assert.Throws<PyNotImplementedError>(() => Apply(listChild, t));
        Assert.Throws<PyNotImplementedError>(() => Apply(new TupleChild([t]), t));
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Form("Callable"), new TupleChild([new PyList([t]), t])));
        var cls = (PyClass)PyOps.MakeClass("F1cUnknownKey", [PyType.Get("object")], null);
        int calls = 0;
        PyObject.SetAttr(cls, "__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 701L; }));
        PyOps.FinalizeOrdinaryClass(cls);
        object? unknown = Call(cls);
        Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([unknown]), t));
        // 未知外层对象可能通过__class__影响Python isinstance；不能冒静态TypeError。
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Form("Callable"), unknown));
        Assert.Equal(0, calls);
        // 真实派生迭代和用户hash/eq执行/重试整源另保pending；这里没有声称执行其回调。
    }

    [Fact]
    public void ForwardEllipsisNestedTupleAndGenericCollectorStayPending()
    {
        object? t = Fresh(), generic = Subscribe(t);
        foreach (object? args in new object?[] { PyEllipsis.Instance, new PyList([PyEllipsis.Instance]),
            "ForwardName", new PyList(["ForwardName"]), new PyList([new PyTuple([t])]),
            new PyList([generic]), new PyList([new PyList([t])]), new PyList([new PyDict()]) })
            Assert.Throws<PyNotImplementedError>(() => Apply(args, t));
        Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([t]), "ForwardResult"));
        // Generic[T]参数是CP合法值；必须走到构造/采参相位，不能被通用type checker拒绝。
        WithChanged("_CallableGenericAlias", PyNone.Instance, () =>
            Assert.Contains("_CallableGenericAlias", Assert.Throws<PyNotImplementedError>(() => Apply(new PyList([generic]), t)).Message));
    }

    // 仅ClassVar部分迁为准确错误；上面的ForwardRef/Ellipsis/嵌套tuple/Generic collector保持NI。
    [Fact]
    public void ClassVarListExportPreservesWarmArgsButColdResultHasExactError()
    {
        object? t = Fresh(), aliasKey = Other("List", Fresh()), warm = Apply(new PyList([aliasKey]), t);
        WithChanged("ClassVar", PyType.Get("list"), () =>
        {
            Assert.Same(warm, Apply(new PyTuple([aliasKey]), t));
            Assert.Equal(PyRepr.Repr(aliasKey) + " is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Apply(new PyList([t]), aliasKey)).PyMessage);
        });
        object? recovered = Apply(new PyList([t]), aliasKey);
        Assert.Same(recovered, Apply(new PyTuple([t]), aliasKey));
    }
}
