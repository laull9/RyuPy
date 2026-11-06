// 真实dict派生的公开消费者接线；经动态ABI验证身份、根协议与活视图，不直接调用内部helper。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictRuntimeWiringTests
{
    private static PyInstance New(params object?[] keys)
    {
        var cls = (PyClass)PyOps.MakeClass("WiredDict", [PyType.Get("dict")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        var source = new PyDict();
        foreach (object? key in keys) source.Set(key, key);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke([source], [], []));
    }
    private static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);
    private static object? Method(object value, string name, params object?[] args) => PyOps.CallMethod(value, name, args, [], []);

    [Fact]
    public void PublicMappingSlotsAndIdentityAreWired()
    {
        var value = New("a", "b");
        Assert.Equal(2L, PyOps.Len(value)); Assert.True(PyOps.Truthy(value));
        PyOps.SetItem(value, "a", 9L); Assert.Equal(9L, PyOps.GetItem(value, "a"));
        Assert.True(PyOps.Contains(value, "a")); PyOps.DelItem(value, "b");
        Assert.Equal("{'a': 9}", PyRepr.Repr(value));
        Assert.Same(value.Class, PyObject.GetAttr(value, "__class__"));
        Assert.Equal(true, Builtin("isinstance", value, PyType.Get("dict")));
        Assert.Equal(true, Builtin("issubclass", value.Class, PyType.Get("dict")));
        Assert.Equal(true, Builtin("isinstance", value, PyAbc.CreateAttributes()["MutableMapping"]));
        Assert.Equal(true, Builtin("issubclass", value.Class, PyAbc.CreateAttributes()["Mapping"]));
        Assert.IsType<PyDict>(Method(value, "copy"));
    }

    [Fact]
    public void SavedMethodsAndRootCallsKeepPublicSelf()
    {
        var value = New(); object? saved = PyOps.PrepareMethod(value, "update");
        Assert.Same(value, PyObject.GetAttr(saved, "__self__"));
        PyObject.SetAttr(value, "update", 99L);
        Assert.Equal(99L, PyOps.CaptureModuleTarget(value, "update"));
        PyOps.CallValue(saved, [], ["a"], [1L]);
        Assert.Equal(1L, PyOps.GetItem(value, "a"));
        PyObject.DelAttr(value, "update");
        PyOps.CallMethod(value, "update", [], ["b"], [2L]);
        Assert.Equal(2L, PyOps.GetItem(value, "b"));
    }

    [Fact]
    public void LiveViewsReadOverwrittenValuesAndStickyExhaustion()
    {
        var value = New("a"); object? items = Method(value, "items");
        var iterator = PyIter.Create(items);
        PyOps.SetItem(value, "a", 7L);
        Assert.True(iterator.MoveNext()); Assert.Equal("('a', 7)", PyRepr.Repr(iterator.Current()));
        Assert.False(iterator.MoveNext()); PyOps.SetItem(value, "b", 8L); Assert.False(iterator.MoveNext());
        Assert.True(PyOps.Contains(items, new PyTuple(["b", 8L])));
        Assert.Equal(2L, PyOps.Len(items));
        var reverse = Assert.IsAssignableFrom<IPyIterator>(Builtin("reversed", value));
        Assert.True(reverse.MoveNext()); Assert.Equal("b", reverse.Current());
        var reverseView = Assert.IsAssignableFrom<IPyIterator>(Builtin("reversed", items));
        Assert.True(reverseView.MoveNext()); Assert.Equal("('b', 8)", PyRepr.Repr(reverseView.Current()));
        var invalid = PyIter.Create(value); PyOps.SetItem(value, "c", 9L);
        Assert.Throws<PyRuntimeError>(() => invalid.MoveNext());
        Assert.Throws<PyRuntimeError>(() => invalid.MoveNext());
    }

    [Fact]
    public void ReprCycleTracksWrapperAndCustomReprWins()
    {
        var value = New(); PyOps.SetItem(value, "self", value);
        Assert.Equal("{'self': {...}}", PyRepr.Repr(value));
        value.Class.SetAttr("__repr__", PyNativeFunctions.CreateVariadic("masked", _ => "masked"));
        Assert.Equal("masked", PyRepr.Str(value));
        Assert.Equal("{'self': masked}", PyOps.CallValue(PyObject.GetAttr(PyType.Get("dict"), "__repr__"), [value], [], []));
    }

    [Fact]
    public void EqualityAndEmptyHashConsumersKnowDictFamily()
    {
        var value = New("a"); var exact = new PyDict(); exact.Set("a", "a");
        Assert.True(PyHash.Equal(value, exact)); Assert.True(PyHash.Equal(exact, value));
        Assert.True(PyOps.Compare(PyOps.Eq, value, exact)); Assert.False(PyOps.Compare(PyOps.Ne, exact, value));
        Assert.Contains("WiredDict", Assert.Throws<PyTypeError>(() => PyHash.Of(value)).Message);
        Assert.Throws<PyTypeError>(() => new PyDict().Set(value, 1L));
        Assert.Throws<PyTypeError>(() => Builtin("set", new PyList([value])));
        Assert.Throws<PyTypeError>(() => Builtin("frozenset", new PyList([value])));
    }

    [Fact]
    public void UnpackAndListExtendConsumeKeysInsteadOfAttributes()
    {
        var value = New("a", "b", "c");
        Assert.Equal("('a', 'b', 'c')", PyRepr.Repr(PyOps.UnpackSequence(value, 3)));
        Assert.Equal("('a', ['b'], 'c')", PyRepr.Repr(PyOps.UnpackEx(value, 1, 1)));
        var list = new PyList(); Method(list, "extend", value);
        Assert.Equal("['a', 'b', 'c']", PyRepr.Repr(list));
        Assert.Equal("['a', 'b', 'c']", PyRepr.Repr(Builtin("list", value)));
    }

    [Fact]
    public void UnionKeepsInPlaceOwnerAndForeignReflection()
    {
        var value = New("a"); var other = new PyDict(); other.Set("b", 2L);
        Assert.IsType<PyDict>(PyOps.Binary(PyOps.BitOr, value, other));
        Assert.IsType<PyDict>(PyOps.Binary(PyOps.BitOr, other, value));
        Assert.Same(value, PyOps.InPlaceBinary(PyOps.BitOr, value, new PyList([new PyTuple(["c", 3L])])));
        Assert.Equal(3L, PyOps.GetItem(value, "c"));
        var cls = new PyClass("Foreign");
        cls.SetAttr("__ror__", PyNativeFunctions.CreateVariadic("ror", _ => "reflected"));
        Assert.Equal("reflected", PyOps.Binary(PyOps.BitOr, value, new PyInstance(cls)));
    }

    [Fact]
    public void ViewsCompareByContentAndRejectUnclosedSetOperations()
    {
        var first = New("a"); var second = New("a");
        object keys = Method(first, "keys")!; object equalKeys = Method(second, "keys")!;
        Assert.True(PyOps.Compare(PyOps.Eq, keys, equalKeys));
        Assert.False(PyOps.Compare(PyOps.Ne, keys, equalKeys));
        Assert.True(PyHash.Equal(keys, equalKeys));
        Assert.False(PyOps.Compare(PyOps.Eq, Method(first, "values")!, Method(first, "values")!));
        Assert.Throws<PyTypeError>(() => PyHash.Of(keys));
        Assert.Throws<PyTypeError>(() => new PyDict().Set(keys, 1L));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Binary(PyOps.BitOr, keys, equalKeys));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(keys, "mapping"));
    }

    [Fact]
    public void DictConstructionAndKeywordsBypassBusinessKeysOverride()
    {
        var value = New("a");
        value.Class.SetAttr("keys", PyNativeFunctions.CreateVariadic("keys", _ => throw new PyRuntimeError("must not call")));
        Assert.Equal("{'a': 'a'}", PyRepr.Repr(Builtin("dict", value)));
        object callable = PyNativeFunctions.Create("identity", ["a"], [null], 1, args => args[0]);
        Assert.Equal("a", PyOps.CallValue(callable, [], [PyNone.Instance], [value]));
    }
}
