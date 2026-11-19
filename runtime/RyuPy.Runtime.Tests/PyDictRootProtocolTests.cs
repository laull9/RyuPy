// dict根协议独立测试：公开self、缺键钩子、有限键域、更新失败前缀、活视图和尾删。
using System;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictRootProtocolTests
{
    private static PyClass Class(string name = "D") => (PyClass)PyOps.MakeClass(name, [PyType.Get("dict")], null);
    private static PyInstance New(PyClass? cls = null, object? source = null)
    {
        cls ??= Class(); PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke(source is null ? [] : [source], [], []));
    }
    private static object? Root(string name, params object?[] args) => Call(PyObject.GetAttr(PyType.Get("dict"), name), args);
    private static object? Call(object? callable, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(callable, args, names ?? [], values ?? []);
    private static PyFunction Fn(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static PyDict Raw(params object?[] pairs)
    {
        var raw = new PyDict();
        for (int i = 0; i < pairs.Length; i += 2) raw.Set(pairs[i], pairs[i + 1]);
        return raw;
    }
    [Fact]
    public void MetadataExposesPublicSelfAndCanonicalObjectStr()
    {
        var value = New();
        foreach (string name in new[] { "get", "update", "__getitem__", "__contains__", "__len__", "__str__" })
        {
            object? bound = PyObject.GetAttr(value, name);
            Assert.Same(value, PyObject.GetAttr(bound, "__self__"));
            object? descriptor = PyObject.GetAttr(PyType.Get("dict"), name);
            Assert.Equal((name == "__str__" ? "object" : "dict") + "." + name,
                PyObject.GetAttr(descriptor, "__qualname__"));
        }
        Assert.Same(PyObject.GetAttr(PyType.Get("object"), "__str__"), PyObject.GetAttr(PyType.Get("dict"), "__str__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(value, "__hash__"));
        Assert.Same(value.Class, PyObject.GetAttr(PyObject.GetAttr(value, "fromkeys"), "__self__"));
        Assert.Same(PyType.Get("dict"), PyObject.GetAttr(PyObject.GetAttr(PyType.Get("dict"), "fromkeys"), "__self__"));
    }
    [Fact]
    public void SavedMethodsUseLivePayloadAndRootsBypassOverrides()
    {
        var cls = Class(); cls.SetAttr("get", Fn("get", _ => "business"));
        var value = New(cls, Raw("x", 1L));
        object? saved = PyObject.GetAttr(value, "__getitem__");
        Assert.Equal("business", Call(PyObject.GetAttr(value, "get"), ["x"]));
        Assert.Equal(1L, Root("get", value, "x"));
        Root("__setitem__", value, "x", 9L);
        Assert.Equal(9L, Call(saved, ["x"]));
        var copy = Assert.IsType<PyDict>(Root("copy", value));
        Assert.Equal(9L, copy.Get("x"));
    }
    [Fact]
    public void MissingIsClassOnlyAndOnlyForSubscript()
    {
        var cls = Class(); int calls = 0;
        cls.SetAttr("__missing__", Fn("missing", a => { calls++; return a[1]; }));
        var value = New(cls);
        PyObject.SetAttr(value, "__missing__", Fn("instance_missing", _ => "wrong"));
        Assert.Equal("absent", Root("__getitem__", value, "absent"));
        Assert.Same(PyNone.Instance, Root("get", value, "absent"));
        Assert.Equal(false, Root("__contains__", value, "absent"));
        Assert.Equal("fallback", Root("pop", value, "absent", "fallback"));
        Assert.Equal(1, calls);
        cls.SetAttr("__missing__", Fn("missing", _ => "late"));
        Assert.Equal("late", Root("__getitem__", value, "absent"));
    }
    [Fact]
    public void RootInitUpdatesWithoutClearingAndPreservesViews()
    {
        var value = New(null, Raw("old", 1L));
        var view = Assert.IsType<PyDictView>(Root("keys", value));
        PyObject.SetAttr(value, "note", "kept");
        Root("__init__", value, Raw("new", 2L)); Root("__init__", value); Root("__init__", value, value);
        Assert.Equal(new object?[] { "old", "new" }, view.ToArray());
        Assert.Equal("kept", PyObject.GetAttr(value, "note"));
        Assert.Throws<PyTypeError>(() => Root("update", value, Raw("bad", 1L), Raw("bad", 2L)));
        Assert.Equal(2L, Root("__len__", value));
    }
    [Fact]
    public void FromKeysCallsActualInitAndSharesValueReferences()
    {
        var cls = Class(); int calls = 0;
        cls.SetAttr("__init__", Fn("init", a => { calls++; Root("__setitem__", a[0], "initial", 7L); return PyNone.Instance; }));
        PyOps.FinalizeOrdinaryClass(cls);
        var shared = new PyList();
        var result = Assert.IsAssignableFrom<PyInstance>(Call(PyObject.GetAttr(cls, "fromkeys"), [new PyList(["a", "b"]), shared]));
        Assert.Same(cls, result.Class); Assert.Equal(1, calls);
        Assert.Equal(7L, Root("get", result, "initial"));
        Assert.Same(shared, Root("get", result, "a")); Assert.Same(shared, Root("get", result, "b"));
        Assert.IsType<PyDict>(Root("fromkeys", new PyList(["x"])));
    }
    [Fact]
    public void RawWrapperSourceDoesNotCallKeysOverride()
    {
        var cls = Class(); cls.SetAttr("keys", Fn("keys", _ => throw new PyAssertionError("must not call keys")));
        var source = New(cls, Raw("a", 1L));
        var target = New(); Root("update", target, source);
        Assert.Equal(1L, Root("get", target, "a"));
        var exact = new PyDict(); exact.Update(source); Assert.Equal(1L, exact.Get("a"));
    }
    [Fact]
    public void ScalarInvariantRejectsRawAndStreamingCustomKeysWithoutLosingPrefix()
    {
        var value = New(); var key = new PyTuple([1L]);
        Assert.Throws<PyNotImplementedError>(() => Root("update", value, Raw(key, 8L)));
        Assert.Equal(0L, Root("__len__", value));
        var pairs = new PyList([new PyTuple(["ok", 1L]), new PyTuple([key, 2L]), new PyTuple(["late", 3L])]);
        Assert.Throws<PyNotImplementedError>(() => Root("update", value, pairs));
        Assert.Equal(1L, Root("get", value, "ok"));
        Assert.Equal(false, Root("__contains__", value, "late"));
        Assert.Throws<PyNotImplementedError>(() => Root("__setitem__", value, key, 4L));
        Assert.Throws<PyNotImplementedError>(() => Root("setdefault", value, key));
        Assert.Throws<PyNotImplementedError>(() => Root("fromkeys", new PyList([key])));
    }
    [Fact]
    public void NoneNormalizesInAllRootLookupsAndDeletes()
    {
        var value = New(); Root("__setitem__", value, null, 3L);
        Assert.Equal(3L, Root("__getitem__", value, null));
        Assert.Equal(3L, Root("get", value, null));
        Assert.Equal(true, Root("__contains__", value, null));
        Assert.Equal(3L, Root("setdefault", value, null, 4L));
        Assert.Equal(3L, Root("pop", value, null));
        Root("__setitem__", value, null, 5L); Root("__delitem__", value, null);
        Assert.Equal(0L, Root("__len__", value));
    }
    [Fact]
    public void LiveItemsSeeReplacementAndNormalExhaustionSticks()
    {
        var value = New(null, Raw("a", 1L, "b", 2L));
        var items = Assert.IsType<PyDictView>(Root("items", value));
        IPyIterator iterator = PyIter.Create(items);
        Assert.True(iterator.MoveNext());
        Root("__setitem__", value, "b", 9L);
        Assert.True(iterator.MoveNext());
        Assert.Equal(9L, Assert.IsType<PyTuple>(iterator.Current()).Items[1]);
        Assert.False(iterator.MoveNext());
        Root("__setitem__", value, "c", 10L);
        Assert.False(iterator.MoveNext());
        Assert.Equal(3, items.Count);
    }
    [Fact]
    public void SizeErrorRepeatsEvenAfterSizeRestoration()
    {
        var value = New(null, Raw("a", 1L));
        var cursor = Assert.IsAssignableFrom<IPyIterator>(Root("__iter__", value));
        Root("__setitem__", value, "b", 2L);
        Assert.Throws<PyRuntimeError>(() => cursor.MoveNext());
        Root("__delitem__", value, "b");
        Assert.Throws<PyRuntimeError>(() => cursor.MoveNext());
        var sameSize = Assert.IsAssignableFrom<IPyIterator>(Root("__iter__", value));
        Root("__delitem__", value, "a"); Root("__setitem__", value, "replacement", 1L);
        Assert.Throws<PyNotImplementedError>(() => sameSize.MoveNext());
    }
    [Fact]
    public void ReverseReadsLiveValuesAndEmptyClearIsNoStructuralMutation()
    {
        var value = New(null, Raw("a", 1L, "b", 2L));
        var cursor = Assert.IsAssignableFrom<IPyIterator>(Root("__reversed__", value));
        Assert.True(cursor.MoveNext()); Assert.Equal("b", cursor.Current());
        Assert.True(cursor.MoveNext()); Assert.Equal("a", cursor.Current());
        Assert.False(cursor.MoveNext()); Root("clear", value); Assert.False(cursor.MoveNext());
        var empty = Assert.IsAssignableFrom<IPyIterator>(Root("__iter__", value));
        Root("clear", value); Assert.False(empty.MoveNext());
    }
    [Fact]
    public void IdentityValueEqualitySkipsNanAndUserEq()
    {
        object nan = double.NaN;
        var left = New(null, Raw("x", nan)); var right = New(null, Raw("x", nan));
        Assert.Equal(true, Root("__eq__", left, right)); Assert.Equal(false, Root("__ne__", left, right));
        Assert.True(Assert.IsType<PyDictView>(Root("values", left)).Contains(nan));
        Assert.True(Assert.IsType<PyDictView>(Root("items", left)).Contains(new PyTuple(["x", nan])));
        Assert.Same(PyNotImplemented.Instance, Root("__eq__", left, 1L));
        Assert.Same(PyNotImplemented.Instance, Root("__ne__", left, 1L));
    }
    [Fact]
    public void PopItemShrinksTailAndPreservesInsertionOrderAcrossTombstones()
    {
        var value = New();
        for (long i = 0; i < 256; i++) Root("__setitem__", value, i, i);
        for (long i = 1; i < 256; i += 2) Root("__delitem__", value, i);
        for (long i = 254; i >= 0; i -= 2)
        {
            var pair = Assert.IsType<PyTuple>(Root("popitem", value));
            Assert.Equal(i, pair.Items[0]); Assert.Equal(i, pair.Items[1]);
        }
        Assert.Throws<PyKeyError>(() => Root("popitem", value));
        Root("__setitem__", value, "after", 1L);
        Assert.Equal("after", Assert.IsType<PyTuple>(Root("popitem", value)).Items[0]);
    }
    [Fact]
    public void UnionDropsSubclassAndInPlaceUnionRetainsOwner()
    {
        var value = New(null, Raw("x", 1L));
        var copy = Assert.IsType<PyDict>(Root("__or__", value, Raw("y", 2L)));
        Assert.Equal(2, copy.Count); Assert.Equal(1L, Root("__len__", value));
        Assert.Same(value, Root("__ior__", value, new PyList([new PyTuple(["z", 3L])])));
        Assert.Equal(3L, Root("get", value, "z"));
        Assert.Same(PyNotImplemented.Instance, Root("__or__", value, new PyList()));
    }
    [Fact]
    public void ExactOldCustomKeyDomainIsNotRestrictedByNewWrapperGuard()
    {
        var key = new PyTuple([1L]); var source = Raw(key, "kept"); var exact = new PyDict();
        exact.Update(source); Assert.Equal("kept", exact.Get(key));
        var wrapper = New(null, Raw("a", 1L));
        Assert.Throws<PyNotImplementedError>(() => exact.Update(wrapper));
        Assert.Equal(1, exact.Count);
    }
    [Fact]
    public void ValueComparisonUsesTruthyAndPreservesUserExceptionIdentity()
    {
        var truthClass = (PyClass)PyOps.MakeClass("Truth", [], null);
        int truthCalls = 0;
        truthClass.SetAttr("__bool__", Fn("bool", _ => { truthCalls++; return true; }));
        PyOps.FinalizeOrdinaryClass(truthClass);
        object truth = truthClass.Invoke([], [], [])!;
        var eqClass = (PyClass)PyOps.MakeClass("EqValue", [], null);
        eqClass.SetAttr("__eq__", Fn("eq", _ => truth)); PyOps.FinalizeOrdinaryClass(eqClass);
        object item = eqClass.Invoke([], [], [])!;
        var left = New(null, Raw("x", item)); var right = New(null, Raw("x", 9L));
        Assert.Equal(true, Root("__eq__", left, right)); Assert.Equal(1, truthCalls);
        Assert.True(Assert.IsType<PyDictView>(Root("values", left)).Contains(9L));
        Assert.Equal(2, truthCalls);
        var sentinel = new PyValueError("same exception");
        eqClass.SetAttr("__eq__", Fn("eq", _ => throw sentinel));
        Assert.Same(sentinel, Assert.Throws<PyValueError>(() => Root("__eq__", left, right)));
    }
    [Fact]
    public void ForeignNeDoesNotCallForeignEqAndExplicitNiUsesIdentity()
    {
        var cls = (PyClass)PyOps.MakeClass("Foreign", [], null);
        int eq = 0, ne = 0;
        cls.SetAttr("__eq__", Fn("eq", _ => { eq++; return true; }));
        cls.SetAttr("__ne__", Fn("ne", _ => { ne++; return true; }));
        PyOps.FinalizeOrdinaryClass(cls);
        object foreign = cls.Invoke([], [], [])!; var value = New();
        Assert.True(PyOps.Compare(PyOps.Ne, value, foreign)); Assert.Equal(1, ne); Assert.Equal(0, eq);
        cls.SetAttr("__ne__", Fn("ne", _ => PyNotImplemented.Instance));
        Assert.True(PyOps.Compare(PyOps.Ne, value, foreign)); Assert.Equal(0, eq);
    }

    [Fact]
    public void RecursiveRootComparisonIsCatchableAndRestoresDepthAfterFailure()
    {
        var left = New(); var right = New();
        Root("__setitem__", left, "self", left); Root("__setitem__", right, "self", right);
        Assert.Equal(true, Root("__eq__", left, left));
        Assert.Equal(false, Root("__ne__", left, left));
        for (int repeat = 0; repeat < 2; repeat++)
        {
            foreach (string slot in new[] { "__eq__", "__ne__" })
                Assert.Equal("maximum recursion depth exceeded in comparison",
                    Assert.Throws<PyRecursionError>(() => Root(slot, left, right)).Message);
            Assert.Throws<PyRecursionError>(() => PyOps.Compare(PyOps.Eq, left, right));
            Assert.Throws<PyRecursionError>(() => PyOps.Compare(PyOps.Ne, left, right));
            // 失败后普通非身份比较必须重新获得完整预算，而不是只验证提前identity返回。
            var a = New(null, Raw("x", 1L)); var b = New(null, Raw("x", 1L));
            Assert.Equal(true, Root("__eq__", a, b));
            Assert.Equal(false, Root("__ne__", a, b));
        }
    }

}
