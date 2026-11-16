// G2公开入口合同：真实根与普通后代、冷终结和原始元数据，CP原源先双跑冻结。
// 不读取内部family/native状态，不根据生产实现结构造期望；构建由父任务统一执行。
using System;
using System.Collections.Generic;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingGenericBasesTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static PyClass Make(string name, object? parent) =>
        Assert.IsType<PyClass>(PyOps.MakeClass(name, [PyRuntimeBases.Validate(parent)], null));
    private static void Set(object? value, string name, object? item) => PyObject.SetAttr(value, name, item);
    private static PyTuple Tuple(object? value, string name) => Assert.IsType<PyTuple>(Get(value, name));
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);

    [Fact]
    public void AliasBaseKeepsRealRootAndOriginalIdentityAfterBodyOverrides()
    {
        object? t = Variable("T"), alias = Subscribe(t), root = Form("Generic");
        Assert.Same(alias, PyRuntimeBases.Validate(alias));
        PyClass cls = Make("Box", alias);
        Set(cls, "__orig_bases__", PyNone.Instance);
        Set(cls, "__parameters__", new PyTuple(["body"]));
        Finish(cls);
        Assert.Same(PyType.Get("type"), PyObject.TypeOf(cls));
        Assert.Same(root, Get(cls, "__base__"));
        Assert.Equal(new[] { root }, Tuple(cls, "__bases__").Items);
        Assert.Equal(new object?[] { cls, root, PyType.Get("object") }, Tuple(cls, "__mro__").Items);
        Assert.Same(alias, Tuple(cls, "__orig_bases__").Items[0]);
        Assert.Same(t, Tuple(cls, "__parameters__").Items[0]);
        Assert.Same(PyNone.Instance, Call(Form("get_origin"), cls));
        Assert.Empty(Assert.IsType<PyTuple>(Call(Form("get_args"), cls)).Items);
        Assert.Equal("generic_contract", Get(cls, "__module__"));
    }

    [Fact]
    public void ChildHasEmptyOwnParametersAndInheritedOriginalTuple()
    {
        PyClass parent = Make("Parent", Subscribe(Variable("T"))); Finish(parent);
        PyClass child = Make("Child", parent);
        Set(child, "__parameters__", new PyTuple(["body"])); Finish(child);
        PyClass grandchild = Make("Grandchild", child); Finish(grandchild);
        foreach (PyClass cls in new[] { child, grandchild })
        {
            Assert.Empty(Tuple(cls, "__parameters__").Items);
            Assert.Same(Get(parent, "__orig_bases__"), Get(cls, "__orig_bases__"));
            Assert.Equal(true, Builtin("issubclass", cls, Form("Generic")));
        }
        foreach (string name in new[] { "__bases__", "__mro__", "__orig_bases__", "__parameters__" })
            Assert.Same(Get(child, name), Get(child, name));
        object? first = Call(Get(child, "mro")), second = Call(Get(child, "mro"));
        Assert.NotSame(first, second);
        Assert.Equal(PyRepr.Repr(first), PyRepr.Repr(second));
    }

    [Fact]
    public void BareGenericUsesFinalOwnTupleIncludingTypeVariableAndListAlias()
    {
        object? t = Variable("T"), alias = Subscribe(t);
        object?[] values = [new PyTuple([]), new PyTuple([alias]), new PyTuple([PyType.Get("int")]),
            new PyTuple(["bad"]), new PyTuple([t]), new PyTuple([PyOps.GetItem(Form("List"), t)])];
        int[] lengths = [0, 1, 0, 0, 1, 1];
        for (int i = 0; i < values.Length; i++)
        {
            PyClass cls = Make("Bare", Form("Generic"));
            Set(cls, "__orig_bases__", values[i]); Finish(cls);
            Assert.Same(values[i], Get(cls, "__orig_bases__"));
            Assert.Equal(lengths[i], Tuple(cls, "__parameters__").Items.Length);
            if (lengths[i] != 0) Assert.Same(t, Tuple(cls, "__parameters__").Items[0]);
        }
    }

    [Fact]
    public void KnownInvalidBareMetadataFailsAtFinalizeAndCannotBecomeCallable()
    {
        object? root = Form("Generic"), alias = Subscribe(Variable("T"));
        object?[] values = [new PyTuple([root]), PyNone.Instance, 1L, new PyTuple([alias, alias])];
        string[] messages = ["Cannot inherit from plain Generic", "argument of type 'NoneType' is not iterable",
            "argument of type 'int' is not iterable", "Cannot inherit from Generic[...] multiple times."];
        for (int i = 0; i < values.Length; i++)
        {
            PyClass cls = Make("Bad", root);
            Set(cls, "body_ran", true); Set(cls, "__orig_bases__", values[i]);
            Assert.Equal(messages[i], Assert.Throws<PyTypeError>(() => Finish(cls)).PyMessage);
            Assert.Throws<PyNotImplementedError>(() => Call(cls));
        }
        PyClass plain = Make("Plain", root);
        Set(plain, "body_ran", true);
        Assert.Equal("Cannot inherit from plain Generic", Assert.Throws<PyTypeError>(() => Finish(plain)).PyMessage);
    }

    [Fact]
    public void OrdinaryNewInitAndObjectAllocationKeepActualClassAndWritableFields()
    {
        PyClass parent = Make("Factory", Subscribe(Variable("T")));
        var events = new List<string>();
        Set(parent, "__new__", PyNativeFunctions.CreateVariadic("__new__", args =>
        {
            events.Add("new");
            return Call(Get(PyType.Get("object"), "__new__"), args[0]);
        }));
        Set(parent, "__init__", PyNativeFunctions.CreateVariadic("__init__", args =>
        {
            events.Add("init"); Set(args[0], "value", args[1]); return PyNone.Instance;
        }));
        Finish(parent);
        PyClass child = Make("Child", parent); Finish(child);
        object? value = Call(child, 7L);
        Assert.Same(child, PyObject.TypeOf(value));
        Assert.Equal(7L, Get(value, "value"));
        Assert.Equal(new[] { "new", "init" }, events);
        Assert.Equal(true, Builtin("isinstance", value, Form("Generic")));
        object? raw = Call(Get(PyType.Get("object"), "__new__"), child);
        Set(raw, "field", 9L); Assert.Equal(9L, Get(raw, "field"));
    }

    [Fact]
    public void ForeignNewSkipsInitAndClassSubscriptionDoesNotHijackInstanceGetitem()
    {
        PyClass cls = Make("Foreign", Subscribe(Variable("T"))); int calls = 0;
        Set(cls, "__new__", PyNativeFunctions.CreateVariadic("__new__", _ => 17L));
        Set(cls, "__init__", PyNativeFunctions.CreateVariadic("__init__", _ => { calls++; return PyNone.Instance; }));
        Finish(cls); Assert.Equal(17L, Call(cls)); Assert.Equal(0, calls);
        PyClass indexed = Make("Indexed", Subscribe(Variable("U")));
        Set(indexed, "__getitem__", PyNativeFunctions.CreateVariadic("__getitem__", args => args[1]));
        Finish(indexed);
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(indexed, PyType.Get("int")));
        Assert.Equal(42L, PyOps.GetItem(Call(indexed), 42L));
    }
}
