// C1 重新接入真冷入口合同：保 F1 原始键与暖命中，ClassVar 只在既定 checker 相位消费。
// CP 来源逐名见 FACTS.zh.md；全部待冻结候选验收，不把静态草稿或 Skip 计入通过数。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingClassVarReintegrationCheckerTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Fresh() => Variable("C1Reintegration");
    private static object? Apply(string name, object? key) => PyOps.GetItem(Form(name), key);
    private static object? Callable(object? args, object? result) => Apply("Callable", new PyTuple([args, result]));
    private static PyTuple Args(object? value) => Assert.IsType<PyTuple>(Get(value, "__args__"));
    private static string Invalid(object? value) => PyRepr.Repr(value) + " is not valid as type argument";
    private static void WithClassVar(object? value, Action body)
    {
        bool existed = Typing.TryGetValue("ClassVar", out object? saved);
        Typing["ClassVar"] = value;
        try { body(); }
        finally { if (existed) Typing["ClassVar"] = saved; else Typing.Remove("ClassVar"); }
    }
    private static readonly string[] Roots = ["List", "Dict", "Set", "FrozenSet", "Iterable", "Iterator", "Sequence", "Mapping", "MutableMapping", "Collection", "Container"];
    private static bool PairRoot(string name) => name is "Dict" or "Mapping" or "MutableMapping";
    private static object? Raw(string root, object? value, object? marker) => PairRoot(root) ? new PyTuple([value, marker]) : new PyTuple([value]);

    [Fact]
    public void DictNoneAndNoneTypeAreDifferentRawKeysDespiteEqualNormalizedAliases()
    {
        object? key = Apply("List", Fresh());
        object? noneRaw = new PyTuple([PyNone.Instance, key]);
        object? typeRaw = new PyTuple([PyType.Get("NoneType"), key]);
        object? warm = Apply("Dict", noneRaw);
        WithClassVar(PyType.Get("list"), () =>
        {
            Assert.Same(warm, Apply("Dict", new PyTuple([PyNone.Instance, key])));
            for (int i = 0; i < 2; i++)
                Assert.Equal(Invalid(key), Assert.Throws<PyTypeError>(() => Apply("Dict", typeRaw)).PyMessage);
        });
        object? recovered = Apply("Dict", typeRaw);
        Assert.NotSame(warm, recovered);
        Assert.True(PyHash.Equal(warm, recovered));
        Assert.Equal(PyHash.Of(warm), PyHash.Of(recovered));
        Assert.Same(recovered, Apply("Dict", typeRaw));
    }

    [Fact]
    public void AllElevenOrdinaryRootsUseColdClassVarChecksInParameterOrder()
    {
        object? root = Form("ClassVar");
        foreach (string name in Roots)
        {
            object? marker = Fresh(), oldAlias = Apply("ClassVar", Fresh()), listAlias = Apply("List", Fresh());
            foreach (object? invalid in new[] { root, oldAlias })
            {
                string expected = ReferenceEquals(invalid, root) ? "Plain typing.ClassVar is not valid as type argument" : Invalid(invalid);
                Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Apply(name, Raw(name, invalid, marker))).PyMessage);
                if (PairRoot(name))
                    Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Apply(name, new PyTuple([marker, invalid]))).PyMessage);
            }
            object? warmRaw = PairRoot(name) ? new PyTuple([PyNone.Instance, listAlias]) : listAlias;
            object? coldRaw = PairRoot(name) ? new PyTuple([PyType.Get("NoneType"), listAlias]) : new PyTuple([listAlias]);
            object? warm = Apply(name, warmRaw);
            WithClassVar(PyType.Get("list"), () =>
            {
                Assert.Same(warm, Apply(name, warmRaw));
                Assert.Equal(Invalid(listAlias), Assert.Throws<PyTypeError>(() => Apply(name, coldRaw)).PyMessage);
                Assert.Equal(Invalid(listAlias), Assert.Throws<PyTypeError>(() => Apply(name, Raw(name, listAlias, marker))).PyMessage);
                if (PairRoot(name))
                    Assert.Equal(Invalid(listAlias), Assert.Throws<PyTypeError>(() => Apply(name, new PyTuple([marker, listAlias]))).PyMessage);
                // 类型校验早于 arity：双参根缺一项、单参根多一项，均先拒绝非法 alias。
                object? badArity = PairRoot(name) ? new PyTuple([listAlias]) : new PyTuple([listAlias, marker]);
                Assert.Equal(Invalid(listAlias), Assert.Throws<PyTypeError>(() => Apply(name, badArity)).PyMessage);
                Assert.Same(marker, Args(Apply(name, Raw(name, marker, PyType.Get("int")))).Items[0]);
                Assert.Same(oldAlias, Args(Apply(name, Raw(name, oldAlias, marker))).Items[0]);
            });
            object? recovered = Apply(name, coldRaw);
            Assert.NotSame(warm, recovered);
            Assert.True(PyHash.Equal(warm, recovered));
            Assert.Same(recovered, Apply(name, coldRaw));
            WithClassVar(PyNone.Instance, () =>
            {
                object? fresh = Fresh();
                Assert.Same(fresh, Args(Apply(name, Raw(name, fresh, marker))).Items[0]);
            });
        }
    }

    [Fact]
    public void TupleEmptySkipsCheckerWhileEveryTrailingEllipsisPrefixIsChecked()
    {
        object? listAlias = Apply("List", Fresh()), marker = Fresh();
        object? root = Form("ClassVar"), cvAlias = Apply("ClassVar", Fresh());
        foreach (var (raw, expected) in new (object?, string)[] {
            (new PyTuple([root]), "Plain typing.ClassVar is not valid as type argument"),
            (new PyTuple([marker, cvAlias, PyEllipsis.Instance]), Invalid(cvAlias)),
            (new PyTuple([cvAlias, root, PyEllipsis.Instance]), Invalid(cvAlias)) })
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Apply("Tuple", raw)).PyMessage);
        for (int i = 0; i < 128; i++) Apply("Tuple", Fresh());
        WithClassVar(PyType.Get("list"), () =>
        {
            Assert.Empty(Args(Apply("Tuple", new PyTuple([]))).Items);
            foreach (object? raw in new object?[] { new PyTuple([listAlias, PyEllipsis.Instance]),
                new PyTuple([marker, listAlias, PyEllipsis.Instance]),
                new PyTuple([listAlias, marker, PyEllipsis.Instance]) })
                Assert.Equal(Invalid(listAlias), Assert.Throws<PyTypeError>(() => Apply("Tuple", raw)).PyMessage);
            object? valid = Apply("Tuple", new PyTuple([marker, PyType.Get("int"), PyEllipsis.Instance]));
            Assert.Equal(new object?[] { marker, PyType.Get("int"), PyEllipsis.Instance }, Args(valid).Items);
        });
        WithClassVar(PyNone.Instance, () =>
        {
            object? none = Apply("Tuple", new PyTuple([PyNone.Instance, marker, PyEllipsis.Instance]));
            object? noneType = Apply("Tuple", new PyTuple([PyType.Get("NoneType"), marker, PyEllipsis.Instance]));
            Assert.NotSame(none, noneType);
            Assert.True(PyHash.Equal(none, noneType));
            Assert.Same(none, Apply("Tuple", new PyTuple([PyNone.Instance, marker, PyEllipsis.Instance])));
        });
    }

    [Fact]
    public void CallableAcceptsClassVarFormsInArgsButRejectsResultBeforeForwardConversion()
    {
        object? root = Form("ClassVar"), cvAlias = Apply("ClassVar", Fresh()), result = Fresh();
        foreach (object? argument in new[] { root, cvAlias })
        {
            object? listed = Callable(new PyList([argument]), result);
            Assert.Equal(new[] { argument, result }, Args(listed).Items);
            Assert.Same(listed, Callable(new PyTuple([argument]), result));
            object? scalar = Callable(argument, result);
            Assert.NotSame(listed, scalar);
            Assert.True(PyHash.Equal(listed, scalar));
            Assert.Equal(new[] { argument, result }, Args(scalar).Items);
            string expected = ReferenceEquals(argument, root) ? "Plain typing.ClassVar is not valid as type argument" : Invalid(argument);
            for (int i = 0; i < 2; i++)
                Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Callable(new PyList(["["]), argument)).PyMessage);
        }
    }

    [Fact]
    public void CallableEquivalentResultHitsWarmKeyButNewArgsForceRealResultCheck()
    {
        object? t = Fresh(), key = Apply("List", t), equalKey = Apply("List", new PyTuple([t]));
        Assert.NotSame(key, equalKey);
        Assert.True(PyHash.Equal(key, equalKey));
        object? result = Fresh(), warmArgs = Callable(new PyList([key]), result);
        object? warmResult = Callable(new PyList([]), key), coldArg = Fresh();
        WithClassVar(PyType.Get("list"), () =>
        {
            Assert.Same(warmArgs, Callable(new PyTuple([equalKey]), result));
            Assert.Same(warmResult, Callable(new PyList([]), equalKey));
            object? scalar = Callable(key, result);
            Assert.NotSame(warmArgs, scalar);
            Assert.True(PyHash.Equal(warmArgs, scalar));
            Assert.Equal(new[] { key, result }, Args(scalar).Items);
            Assert.Equal(new[] { key, coldArg }, Args(Callable(new PyList([key]), coldArg)).Items);
            Assert.Equal(Invalid(key), Assert.Throws<PyTypeError>(() => Callable(new PyList([coldArg]), key)).PyMessage);
        });
        object? recovered = Callable(new PyList([coldArg]), key);
        Assert.Same(recovered, Callable(new PyTuple([coldArg]), equalKey));
    }

    [Fact]
    public void ExistingTypeCheckConsumersFollowCurrentExportButConstraintsDoNotCheck()
    {
        object? root = Form("ClassVar"), cvAlias = Apply("ClassVar", Fresh()), listAlias = Apply("List", Fresh());
        Func<object?, object?>[] consumers = [
            value => Apply("Optional", value),
            value => Apply("Union", new PyTuple([value, Fresh()])),
            value => PyOps.CallValue(Form("TypeVar"), ["C1Bound"], ["bound"], [value]),
            value => Call(Form("NamedTuple"), "C1Named", new PyList([new PyTuple(["x", value])]))];
        foreach (var consume in consumers)
        {
            Assert.Equal("Plain typing.ClassVar is not valid as type argument", Assert.Throws<PyTypeError>(() => consume(root)).PyMessage);
            Assert.Equal(Invalid(cvAlias), Assert.Throws<PyTypeError>(() => consume(cvAlias)).PyMessage);
            WithClassVar(PyType.Get("list"), () =>
            {
                Assert.Equal(Invalid(listAlias), Assert.Throws<PyTypeError>(() => consume(listAlias)).PyMessage);
                Assert.NotNull(consume(cvAlias));
                Assert.NotNull(consume(PyType.Get("int")));
            });
            foreach (object? replacement in new object?[] { "changed", PyNone.Instance })
            {
                object? coldAlias = Apply("List", Fresh());
                WithClassVar(replacement, () => Assert.NotNull(consume(coldAlias)));
            }
        }
        object? constrained = Call(Form("TypeVar"), "C1Choices", root, cvAlias);
        Assert.Equal(new[] { root, cvAlias }, Assert.IsType<PyTuple>(Get(constrained, "__constraints__")).Items);
    }

    [Fact]
    public void NativeAliasesAndSupportedLiteralValuesDoNotReadClassVarExport()
    {
        object? root = Form("ClassVar"), alias = Apply("ClassVar", Fresh());
        object? abc = PyModules.Import("collections.abc");
        object?[] nativeRoots = [PyType.Get("list"), PyType.Get("dict"), PyType.Get("tuple"), PyType.Get("set"), PyType.Get("frozenset"),
            Get(abc, "Iterable"), Get(abc, "Iterator"), Get(abc, "Sequence"), Get(abc, "Mapping"),
            Get(abc, "MutableMapping"), Get(abc, "Collection"), Get(abc, "Container")];
        foreach (object? replacement in new object?[] { PyType.Get("list"), "changed", PyNone.Instance })
            WithClassVar(replacement, () =>
            {
                foreach (object? nativeRoot in nativeRoots)
                    foreach (object? value in new[] { root, alias })
                    {
                        object? native = PyOps.GetItem(nativeRoot, value);
                        Assert.Same(nativeRoot, Get(native, "__origin__"));
                        Assert.Equal(new[] { value }, Args(native).Items);
                    }
                object? literal = Apply("Literal", new PyTuple([104729L, "C1-native", PyNone.Instance]));
                Assert.Equal(new object?[] { 104729L, "C1-native", PyNone.Instance }, Args(literal).Items);
            });
    }
}
