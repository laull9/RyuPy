// OrderedDict 消费者专项：经公开工厂和动态入口验证双顺序，不依赖内部类型可见性。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyOrderedDictConsumerTests
{
    private static object New()
    {
        var source = new PyDict(); source.Set("a", 1L); source.Set("b", 2L); source.Set("c", 3L);
        object value = PyOps.CallValue(PyCollections.CreateAttributes()["OrderedDict"], [source], [], [])!;
        Method(value, "move_to_end", "a");
        return value;
    }
    private static object? Method(object value, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);
    private static object? Root(string name, params object?[] args) => Method(PyType.Get("dict"), name, args);
    private static string Keys(object? value)
    {
        IPyIterator iterator = PyIter.Create(value); var keys = new System.Collections.Generic.List<string>();
        while (iterator.MoveNext()) keys.Add(PyRepr.Str(iterator.Current()));
        return string.Join(",", keys);
    }
    private static object? Keywords(object?[] names, object?[] values) =>
        PyOps.CallValue(PyNativeFunctions.Create("capture", ["kw"], [null], 0, a => a[0], ["varkw"]), [], names, values);

    [Fact]
    public void PublicConsumersCopyMovedOrderButRootsReadRawOrder()
    {
        object ordered = New();
        Assert.Equal("b,c,a", Keys(ordered));
        Assert.Equal("a,b,c", Keys(Root("keys", ordered)));
        Assert.Equal("1,2,3", Keys(Root("values", ordered)));
        Assert.Equal("('a', 1),('b', 2),('c', 3)", Keys(Root("items", ordered)));
        Assert.Equal("a,b,c", Keys(Root("__iter__", ordered)));
        Assert.Equal("c,b,a", Keys(Root("__reversed__", ordered)));
        Assert.Equal("{'a': 1, 'b': 2, 'c': 3}", Root("__repr__", ordered));
        Assert.Equal(3L, Root("__len__", ordered));
        Assert.Equal(1L, Root("get", ordered, "a"));
        Assert.Equal(2L, Root("__getitem__", ordered, "b"));
        Assert.Equal(true, Root("__contains__", ordered, "c"));
        Assert.Equal("b,c,a", Keys(Root("copy", ordered)));
        Assert.Equal("b,c,a", Keys(PyOps.CallBuiltin("dict", [ordered], [], [])));
        var target = new PyDict(); Method(target, "update", ordered);
        Assert.Equal("b,c,a", Keys(target));
        var cls = (PyClass)PyOps.MakeClass("ConsumerDict", [PyType.Get("dict")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object derived = cls.Invoke([], [], [])!; Method(derived, "update", ordered);
        Assert.Equal("b,c,a", Keys(derived));
        object other = New(); Method(other, "move_to_end", "b");
        Assert.Equal(true, Root("__eq__", ordered, other));
        Assert.Equal(false, Root("__ne__", ordered, other));
    }

    [Fact]
    public void RootViewsAreLiveAndCopyKeepsValueAliases()
    {
        object ordered = New(); object? rootItems = Root("items", ordered);
        var alias = new PyList([9L]); PyOps.SetItem(ordered, "b", alias);
        Assert.Equal("('a', 1),('b', [9]),('c', 3)", Keys(rootItems));
        var copy = Assert.IsType<PyDict>(Root("copy", ordered));
        Assert.Same(alias, copy.Get("b"));
        Method(ordered, "move_to_end", "b");
        Assert.Equal("b,c,a", Keys(copy));
        Assert.Equal("a,b,c", Keys(Root("keys", ordered)));
    }

    [Fact]
    public void BothKeywordExpansionPathsUsePublicOrder()
    {
        object ordered = New();
        Assert.Equal("b,c,a", Keys(Keywords([PyNone.Instance], [ordered])));
        var tail = new PyDict(); tail.Set("z", 4L);
        object proxy = PyMappingProxy.Create([tail], [], []);
        Assert.Equal("b,c,a,z", Keys(Keywords([PyNone.Instance, PyNone.Instance], [ordered, proxy])));
        Assert.Equal("z,b,c,a", Keys(Keywords([PyNone.Instance, PyNone.Instance], [proxy, ordered])));
        Assert.Throws<PyTypeError>(() => Keywords([PyNone.Instance, PyNone.Instance], [ordered, ordered]));
        var duplicate = new PyDict(); duplicate.Set("a", 5L);
        Assert.Throws<PyTypeError>(() => Keywords([PyNone.Instance, PyNone.Instance], [ordered, duplicate]));
        Assert.Throws<PyTypeError>(() => Keywords([PyNone.Instance, PyNone.Instance], [duplicate, ordered]));
        Assert.Throws<PyTypeError>(() => Keywords(["a", PyNone.Instance], [5L, ordered]));
        Assert.Throws<PyTypeError>(() => Keywords([PyNone.Instance, "a"], [ordered, 5L]));
        tail.Set("a", 5L);
        Assert.Throws<PyTypeError>(() => Keywords([PyNone.Instance, PyNone.Instance], [ordered, proxy]));
        PyOps.SetItem(ordered, 4L, "numeric");
        Assert.Throws<PyTypeError>(() => Keywords([PyNone.Instance], [ordered]));
    }

    [Fact]
    public void RootMutatorsRejectBeforeConsumptionAndValidateSignatureFirst()
    {
        object ordered = New(); int calls = 0;
        var cls = new PyClass("NeverConsume");
        cls.SetAttr("keys", PyNativeFunctions.Create("keys", ["self"], [null], 1,
            _ => { calls++; return new PyList(); }));
        var source = new PyInstance(cls);
        Assert.Throws<PyNotImplementedError>(() => Root("update", ordered, source));
        Assert.Throws<PyNotImplementedError>(() => Root("__init__", ordered, source));
        Assert.Throws<PyNotImplementedError>(() => Root("__setitem__", ordered, "x", 8L));
        Assert.Throws<PyNotImplementedError>(() => Root("__delitem__", ordered, "a"));
        Assert.Throws<PyNotImplementedError>(() => Root("clear", ordered));
        Assert.Throws<PyNotImplementedError>(() => Root("pop", ordered, "a"));
        Assert.Throws<PyNotImplementedError>(() => Root("popitem", ordered));
        Assert.Throws<PyNotImplementedError>(() => Root("setdefault", ordered, "x", 8L));
        Assert.Throws<PyNotImplementedError>(() => Root("__ior__", ordered, source));
        Assert.Throws<PyNotImplementedError>(() => Root("__or__", ordered, source));
        Assert.Throws<PyNotImplementedError>(() => Root("__ror__", ordered, source));
        Assert.Throws<PyNotImplementedError>(() => Root("__new__", PyCollections.CreateAttributes()["OrderedDict"]));
        Assert.Throws<PyTypeError>(() => Root("update", ordered, source, source));
        Assert.Throws<PyTypeError>(() => Root("clear", ordered, 1L));
        Assert.Throws<PyTypeError>(() => Root("__setitem__", ordered, "x"));
        Assert.Equal(0, calls); Assert.Equal("b,c,a", Keys(ordered));
    }

    [Fact]
    public void UnsafeDestinationKeysFailBeforeWriting()
    {
        object ordered = New(); var target = new PyDict();
        target.Set(new PyTuple([1L]), 7L);
        Assert.Throws<PyNotImplementedError>(() => Method(target, "update", ordered));
        Assert.Equal(1, target.Count);
    }

    [Fact]
    public void DeferredConsumersRejectWithoutFallbackCallbacks()
    {
        object ordered = New(); int callbacks = 0;
        var fallback = PyNativeFunctions.Create("default", ["value"], [null], 1,
            _ => { callbacks++; return "fallback"; });
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyJsonModule.CreateAttributes()["dumps"],
            [ordered], ["default"], [fallback]));
        Assert.Equal(0, callbacks);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCollections.CreateAttributes()["Counter"], [ordered], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCollections.CreateAttributes()["defaultdict"], [PyNone.Instance, ordered], [], []));
        var counter = new PyCounter(); counter.Dictionary.Set("old", 3L);
        Assert.Throws<PyNotImplementedError>(() => Method(counter, "update", ordered));
        Assert.Throws<PyNotImplementedError>(() => Method(counter, "subtract", ordered));
        Assert.Equal("old", Keys(counter));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(fallback, "__annotations__", ordered));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(fallback, "__dict__", ordered));
        Assert.Throws<PyNotImplementedError>(() => PyMappingProxy.Create([ordered], [], []));
    }
    [Fact]
    public void LocalsRecognizesMappingAndPercentFormattingHasExplicitBoundary()
    {
        object ordered = New();
        Assert.Equal("locals() takes no keyword arguments", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([], [PyNone.Instance], [ordered])).Message);
        Method(ordered, "clear");
        PyOps.FunctionLocalsValidate([], [PyNone.Instance], [ordered]);
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format("%(a)s", ordered));
    }

    [Fact]
    public void ExactIdentityAndRegisteredAncestorsAreReadOnlyObservations()
    {
        object ordered = New(); object type = PyCollections.CreateAttributes()["OrderedDict"]!;
        Assert.Same(type, PyObject.TypeOf(ordered));
        Assert.Same(type, PyObject.GetAttr(ordered, "__class__"));
        Assert.NotSame(PyType.Get("dict"), type);
        Assert.Equal("OrderedDict", PyObject.GetAttr(type, "__name__"));
        Assert.Equal("collections", PyObject.GetAttr(type, "__module__"));
        Assert.Same(PyType.Get("dict"), PyObject.GetAttr(type, "__base__"));
        foreach (object parent in new object[] { PyType.Get("dict"), PyBaseObject.Type,
            PyAbc.CreateAttributes()["Mapping"]!, PyAbc.CreateAttributes()["MutableMapping"]! })
        {
            Assert.Equal(true, PyOps.CallBuiltin("isinstance", [ordered, parent], [], []));
            Assert.Equal(true, PyOps.CallBuiltin("issubclass", [type, parent], [], []));
        }
        Assert.Same(PyNone.Instance, PyObject.GetAttr(ordered, "__hash__"));
        Assert.Throws<PyTypeError>(() => PyHash.Of(ordered));
        Assert.Equal("b,c,a", Keys(ordered));
    }

    [Fact]
    public void PublicCopiesPreserveScalarKeysAndExistingTargetPosition()
    {
        object type = PyCollections.CreateAttributes()["OrderedDict"]!;
        object ordered = PyOps.CallValue(type, [], [], [])!;
        object?[] keys = [PyNone.Instance, "text", true, 8, 9L, BigInteger.Pow(10, 30), 2.5];
        var alias = new PyList([1L]);
        foreach (object? key in keys) PyOps.SetItem(ordered, key, alias);
        Method(ordered, "move_to_end", PyNone.Instance);
        var copied = Assert.IsType<PyDict>(PyOps.CallBuiltin("dict", [ordered], [], []));
        Assert.Equal(keys.Length, copied.Count); Assert.Equal(Keys(ordered), Keys(copied));
        foreach (object? key in keys) Assert.Same(alias, copied.Get(key));
        var target = new PyDict(); target.Set("first", 0L); target.Set("text", 0L);
        Method(target, "update", ordered);
        Assert.Equal("first,text,True,8,9,1000000000000000000000000000000,2.5,None", Keys(target));
        Assert.Same(alias, target.Get("text")); Assert.Equal(0L, target.Get("first"));
        // 空 OrderedDict 仍检查已有目标域，不能以无条目绕开显式边界。
        Method(ordered, "clear"); var unsafeTarget = new PyDict(); unsafeTarget.Set(new PyTuple([1L]), 0L);
        Assert.Throws<PyNotImplementedError>(() => Method(unsafeTarget, "update", ordered));
        Assert.Equal(1, unsafeTarget.Count);
    }

    [Fact]
    public void RootUpdateAndInitAcceptKeywordDataButRejectBeforeWriting()
    {
        object ordered = New();
        foreach (string name in new[] { "update", "__init__" })
        {
            object? descriptor = PyObject.GetAttr(PyType.Get("dict"), name);
            // self 和 iterable 是数据键；根更新没有可被关键字覆盖的位置参数名。
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(descriptor,
                [ordered], ["self", "iterable"], [1L, 2L]));
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(descriptor,
                [ordered, new PyDict()], ["a"], [99L]));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(descriptor,
                [ordered, new PyDict(), new PyDict()], ["a"], [99L]));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(descriptor,
                [1L], ["a"], [99L]));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(descriptor,
                [], ["self"], [ordered]));
        }
        Assert.Equal("b,c,a", Keys(ordered)); Assert.Equal(1L, PyOps.GetItem(ordered, "a"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(PyType.Get("dict"), "clear"),
            [ordered], ["unexpected"], [1L]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(PyType.Get("dict"), "__setitem__"),
            [ordered, "a", 1L], ["unexpected"], [1L]));
    }

    [Fact]
    public void DataclassRejectsMutableOrderedDefaultAtDecoration()
    {
        object ordered = New(); var cls = new PyClass("OrderedDefault");
        cls.SetAttr("items", ordered); PyOps.DeclareDataclass(cls, ["items"], false);
        Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(cls, false));
        Assert.Equal("b,c,a", Keys(ordered));
    }

    [Fact]
    public void DeferredMetadataAndXmlInputsFailWithoutReplacingStorage()
    {
        object ordered = New(); var function = PyNativeFunctions.Create("identity", ["value"], [null], 1, a => a[0]);
        PyObject.SetAttr(function, "__qualname__", "identity");
        object partial = PyOps.CallValue(PyFunctools.CreateAttributes()["partial"], [function], [], [])!;
        object cache = PyOps.CallValue(PyFunctools.CreateAttributes()["lru_cache"], [function], [], [])!;
        object values = PyOps.CallValue(PyOptparse.CreateAttributes()["Values"], [], [], [])!;
        foreach (object owner in new[] { partial, cache, values })
        {
            object? original = PyObject.GetAttr(owner, "__dict__");
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(owner, "__dict__", ordered));
            Assert.Same(original, PyObject.GetAttr(owner, "__dict__"));
        }
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyXmlElementTree.ElementType, ["root", ordered], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyXmlElementTree.ElementType, ["root"], ["attrib"], [ordered]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyXmlElementTree.ElementType, ["root", ordered, 1L], [], []));
        object element = PyOps.CallValue(PyXmlElementTree.ElementType, ["root"], [], [])!;
        object? originalAttributes = PyObject.GetAttr(element, "attrib");
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(element, "attrib", ordered));
        Assert.Same(originalAttributes, PyObject.GetAttr(element, "attrib"));
    }

    [Fact]
    public void LoggingRejectsOrderedMappingBeforeConversionButAfterFiltering()
    {
        object ordered = New(); int conversions = 0;
        var cls = new PyClass("MessageProbe");
        cls.SetAttr("__str__", PyNativeFunctions.Create("__str__", ["self"], [null], 1,
            _ => { conversions++; return "%(a)s"; }));
        object message = new PyInstance(cls);
        object logger = PyOps.CallValue(PyLogging.CreateAttributes()["getLogger"],
            ["od.consumer." + Guid.NewGuid().ToString("N")], [], [])!;
        Method(logger, "setLevel", 20L); PyObject.SetAttr(logger, "propagate", false);
        Method(logger, "debug", message, ordered);
        Assert.Throws<PyNotImplementedError>(() => Method(logger, "info", message, ordered));
        Assert.Throws<PyTypeError>(() => Method(logger, "info"));
        Assert.Equal(0, conversions); Assert.Equal("b,c,a", Keys(ordered));
    }

    [Fact]
    public void DictRootUnionRejectsOrderedOperandBeforeTargetWrites()
    {
        object ordered = New(); var plain = new PyDict(); plain.Set("kept", 41L); plain.Set("a", 42L);
        var cls = (PyClass)PyOps.MakeClass("UnionTarget", [PyType.Get("dict")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object child = cls.Invoke([plain], [], [])!;
        foreach (object target in new object[] { plain, child })
        {
            IPyIterator sourceCursor = PyIter.Create(ordered);
            Assert.True(sourceCursor.MoveNext()); Assert.Equal("b", sourceCursor.Current());
            foreach (string name in new[] { "__or__", "__ror__", "__ior__" })
            {
                Assert.Throws<PyNotImplementedError>(() => Root(name, target, ordered));
                Assert.Throws<PyTypeError>(() => Root(name, target, ordered, ordered));
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(PyType.Get("dict"), name),
                    [target, ordered], ["unexpected"], [1L]));
                Assert.Equal("kept,a", Keys(target));
                Assert.Equal(41L, PyOps.GetItem(target, "kept"));
                Assert.Equal(42L, PyOps.GetItem(target, "a"));
            }
            Assert.Throws<PyNotImplementedError>(() => PyOps.InPlaceBinary(PyOps.BitOr, target, ordered));
            Assert.Throws<PyNotImplementedError>(() => PyOps.Binary(PyOps.BitOr | PyOps.InPlaceFlag, target, ordered));
            Assert.Equal("kept,a", Keys(target)); Assert.Equal(42L, PyOps.GetItem(target, "a"));
            Assert.Equal("b,c,a", Keys(ordered));
            Assert.True(sourceCursor.MoveNext()); Assert.Equal("c", sourceCursor.Current());
            Assert.True(sourceCursor.MoveNext()); Assert.Equal("a", sourceCursor.Current());
            Assert.False(sourceCursor.MoveNext());
        }
    }

}
