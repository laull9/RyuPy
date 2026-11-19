// C1 公开对象合同：固定 CPython 3.12.13 双跑源先于实现冻结；此文件不依赖候选内部类型。
// 测试覆盖真实身份、稳定元数据、公开 hash 关系及精确禁用协议，不能以导入成功代替支持。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingClassVarContractTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Cv(object? key) => PyOps.GetItem(Form("ClassVar"), key);

    [Fact]
    public void RootHasCanonicalSpecialFormIdentityAndAccurateMissingMetadata()
    {
        object? root = Form("ClassVar"), type = PyObject.TypeOf(root);
        Assert.Same(root, Form("ClassVar"));
        Assert.Same(type, Get(root, "__class__"));
        Assert.Same(type, PyObject.TypeOf(Form("Union")));
        Assert.Same(type, PyObject.TypeOf(Form("Optional")));
        Assert.NotSame(type, PyObject.TypeOf(Form("Literal")));
        Assert.Equal("_SpecialForm", Get(type, "__name__"));
        Assert.Equal("typing", Get(type, "__module__"));
        Assert.Equal("<class 'typing._SpecialForm'>", PyRepr.Repr(type));
        Assert.Equal("typing.ClassVar", PyRepr.Repr(root));
        Assert.Equal("ClassVar", Get(root, "__name__"));
        Assert.Equal("ClassVar", Get(root, "__qualname__"));
        Assert.Equal("typing", Get(root, "__module__"));
        Assert.Equal(false, Builtin("isinstance", root, PyType.Get("type")));
        Assert.Same(PyNone.Instance, Call(Form("get_origin"), root));
        Assert.Empty(Assert.IsType<PyTuple>(Call(Form("get_args"), root)).Items);
        foreach (string name in new[] { "__origin__", "__args__", "__parameters__" })
            Assert.Equal(name, Assert.Throws<PyAttributeError>(() => Get(root, name)).PyMessage);
    }

    [Fact]
    public void SafeAliasesPreserveStableMetadataAndRawNoneKeys()
    {
        object? t = Variable("T"), u = Variable("U"), root = Form("ClassVar");
        object? repeated = PyOps.GetItem(PyType.Get("tuple"), new PyTuple([t, t, u]));
        object? callable = PyOps.GetItem(Form("Callable"), new PyTuple([new PyList([t, u]), t]));
        foreach (object? key in new[] { PyType.Get("int"), PyType.Get("bytes"), Form("Any"), t,
            PyOps.GetItem(Form("List"), t), PyOps.GetItem(PyType.Get("list"), t), repeated, callable })
        {
            object? alias = Cv(key), args = Get(alias, "__args__"), parameters = Get(alias, "__parameters__");
            Assert.Same(alias, Cv(key));
            Assert.Same(root, Get(alias, "__origin__"));
            Assert.Same(root, Call(Form("get_origin"), alias));
            Assert.Same(args, Get(alias, "__args__"));
            Assert.Same(args, Call(Form("get_args"), alias));
            Assert.Same(parameters, Get(alias, "__parameters__"));
            Assert.Equal(new[] { key }, Assert.IsType<PyTuple>(args).Items);
            Assert.Equal("ClassVar", Get(alias, "__name__"));
            Assert.Equal("ClassVar", Get(alias, "__qualname__"));
            Assert.Equal("typing", Get(alias, "__module__"));
            Assert.Same(PyObject.TypeOf(alias), Get(alias, "__class__"));
            Assert.Same(PyObject.TypeOf(Subscribe(t)), PyObject.TypeOf(alias));
            Assert.Equal(false, Builtin("isinstance", alias, PyType.Get("type")));
        }
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(Get(Cv(repeated), "__parameters__")).Items);
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(Get(Cv(callable), "__parameters__")).Items);
        object? none = Cv(PyNone.Instance), noneType = Cv(PyType.Get("NoneType"));
        Assert.NotSame(none, noneType);
        Assert.True(PyHash.Equal(none, noneType));
        Assert.Equal(PyHash.Of(none), PyHash.Of(noneType));
        Assert.NotSame(Cv(t), Cv(Variable("T")));
    }

    [Fact]
    public void AliasHashMatchesOriginAndArgumentsCompoundHash()
    {
        object? t = Variable("HashT");
        foreach (object? key in new[] { PyType.Get("int"), (object)PyNone.Instance, t, PyOps.GetItem(Form("List"), t) })
        {
            object? alias = Cv(key);
            Assert.Equal(PyHash.Of(new PyTuple([Get(alias, "__origin__"), Get(alias, "__args__")])), PyHash.Of(alias));
        }
        object? left = Cv(PyNone.Instance), right = Cv(PyType.Get("NoneType"));
        Assert.NotSame(left, right);
        Assert.True(PyHash.Equal(left, right));
        Assert.Equal(PyHash.Of(left), PyHash.Of(right));
    }

    [Fact]
    public void InvalidTuplesSpecialArgumentsAndClosedResubscriptionKeepExactErrors()
    {
        foreach (object?[] items in new[] { Array.Empty<object?>(), new object?[] { PyType.Get("int") },
            new object?[] { PyType.Get("int"), PyType.Get("str") }, new object?[] { new PyTuple([PyType.Get("int")]) } })
        {
            var key = new PyTuple(items);
            Assert.Equal("typing.ClassVar accepts only single type. Got " + PyRepr.Repr(key) + ".",
                Assert.Throws<PyTypeError>(() => Cv(key)).PyMessage);
        }
        foreach (string name in new[] { "ClassVar", "Union", "Optional", "Literal" })
            Assert.Equal("Plain typing." + name + " is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Cv(Form(name))).PyMessage);
        Assert.Equal("typing.ClassVar[int] is not valid as type argument",
            Assert.Throws<PyTypeError>(() => Cv(Cv(PyType.Get("int")))).PyMessage);
        Assert.Equal("Plain <class 'typing.Generic'> is not valid as type argument",
            Assert.Throws<PyTypeError>(() => Cv(Form("Generic"))).PyMessage);
        object? generic = Subscribe(Variable("T"));
        Assert.Equal("typing.Generic[~T] is not valid as type argument", Assert.Throws<PyTypeError>(() => Cv(generic)).PyMessage);
        Assert.Equal("typing.ClassVar[int] is not a generic class",
            Assert.Throws<PyTypeError>(() => PyOps.GetItem(Cv(PyType.Get("int")), PyType.Get("str"))).PyMessage);
    }

    [Fact]
    public void CallsClassChecksAndBasesUseRealRootAndAliasErrors()
    {
        object? root = Form("ClassVar"), alias = Cv(PyType.Get("int"));
        foreach (object? target in new[] { root, alias })
        {
            Assert.Equal("Cannot instantiate typing.ClassVar", Assert.Throws<PyTypeError>(() => Call(target)).PyMessage);
            Assert.Equal("Cannot instantiate typing.ClassVar",
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(target, [1L], ["name"], ["value"])).PyMessage);
            string expectedBase = "Cannot subclass " + PyRepr.Repr(target);
            Assert.Equal(expectedBase, Assert.Throws<PyTypeError>(() => Call(Get(target, "__mro_entries__"), new PyTuple([target]))).PyMessage);
            Assert.Equal(expectedBase, Assert.Throws<PyTypeError>(() => PyRuntimeBases.Validate(target)).PyMessage);
            foreach (string operation in new[] { "isinstance", "issubclass" })
            {
                object? operand = operation == "isinstance" ? 1L : PyType.Get("int");
                string expected = ReferenceEquals(target, root)
                    ? "typing.ClassVar cannot be used with " + operation + "()"
                    : "Subscripted generics cannot be used with class and instance checks";
                Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Builtin(operation, operand, target)).PyMessage);
                Assert.Equal(true, Builtin(operation, operand, new PyTuple([PyType.Get("int"), target])));
                object? missed = operation == "isinstance" ? "text" : PyType.Get("str");
                Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Builtin(operation, missed, new PyTuple([PyType.Get("int"), target]))).PyMessage);
            }
        }
    }
}
