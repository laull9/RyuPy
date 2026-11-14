// 有限 OrderedDict 核心回归：真实类型、双顺序、值别名、绑定、游标及比较回调。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyOrderedDictCoreTests
{
    private static object Type => PyCollections.CreateAttributes()["OrderedDict"]!;
    private static object New(params (string Key, object? Value)[] values) =>
        PyOps.CallValue(Type, [], values.Select(v => v.Key).ToArray(), values.Select(v => v.Value).ToArray())!;
    private static object? Call(object target, string name, params object?[] args) => PyOps.CallMethod(target, name, args, [], []);
    private static object? Named(object target, string name, object?[] args, string[] names, object?[] values) =>
        PyOps.CallValue(PyObject.GetAttr(target, name), args, names, values);
    private static object?[] Items(object? source)
    {
        var values = new List<object?>(); IPyIterator iterator = PyIter.Create(source);
        while (iterator.MoveNext()) values.Add(iterator.Current()); return values.ToArray();
    }
    private static PyInstance User(params (string Name, Func<object?[], object?> Body)[] methods)
    {
        var cls = (PyClass)PyOps.MakeClass("OrderedUser", [], null);
        foreach (var (name, body) in methods) cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, body));
        PyOps.FinalizeOrdinaryClass(cls); return Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }
    [Fact]
    public void TrueTypeAncestryAndIndependentStorage()
    {
        object od = New(("a", 1L));
        Assert.Same(Type, PyObject.GetAttr(od, "__class__")); Assert.NotSame(Type, PyType.Get("dict"));
        Assert.True((bool)PyOps.CallBuiltin("isinstance", [od, PyType.Get("dict")], [], [])!);
        Assert.True((bool)PyOps.CallBuiltin("issubclass", [Type, PyType.Get("dict")], [], [])!);
        Assert.True((bool)PyOps.CallBuiltin("isinstance", [od, PyAbc.CreateAttributes()["MutableMapping"]], [], [])!);
        Assert.Equal("unhashable type: 'collections.OrderedDict'", Assert.Throws<PyTypeError>(() => PyHash.Of(od)).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(od, "tag", 1L));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("Child", [Type], null));
    }
    [Fact]
    public void OverwriteReinitializeAndCopyPreserveOwnersAndValueAliases()
    {
        var shared = new PyList([]); object od = New(("a", shared), ("b", 2L));
        object view = Call(od, "items")!; object method = PyObject.GetAttr(od, "move_to_end")!;
        Assert.Same(od, PyObject.GetAttr(method, "__self__"));
        PyOps.CallValue(method, ["a"], [], []);
        Assert.Equal(new object?[] { "b", "a" }, Items(od));
        Named(od, "__init__", [], ["c", "a"], [3L, shared]);
        Assert.Equal(3, Items(view).Length); Assert.Same(shared, PyOps.GetItem(od, "a"));
        object copy = Call(od, "copy")!; Call(copy, "move_to_end", "b");
        Assert.Equal(new object?[] { "b", "a", "c" }, Items(od));
        Assert.Equal(new object?[] { "a", "c", "b" }, Items(copy)); Assert.Same(shared, PyOps.GetItem(copy, "a"));
        object from = Named(Type, "fromkeys", [], ["iterable", "value"], [new PyList(["b", "a", "b"]), shared])!;
        Assert.Equal(new object?[] { "b", "a" }, Items(from)); Assert.Same(PyOps.GetItem(from, "a"), PyOps.GetItem(from, "b"));
    }
    [Fact]
    public void NumericEquivalentKeysKeepFirstIdentityAndNoneNormalizes()
    {
        object od = New(); object first = true; PyOps.SetItem(od, first, 1L); PyOps.SetItem(od, 1L, 2L); PyOps.SetItem(od, 1.0, 3L);
        PyOps.SetItem(od, null, 4L);
        Assert.Same(first, Items(od)[0]); Assert.Equal(2L, PyOps.Len(od));
        Assert.Equal(3L, PyOps.GetItem(od, true)); Assert.Equal(4L, PyOps.GetItem(od, PyNone.Instance));
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(od, new PyTuple([1L]), 9L));
        Assert.Throws<PyTypeError>(() => PyOps.SetItem(od, new PyList([]), 9L));
    }
    [Fact]
    public void MoveAndPopTruthRunAfterBindingBeforeLookupAndUseMutatedState()
    {
        object od = New(("a", 1L), ("b", 2L)); int calls = 0;
        object truth = User(("__bool__", _ => { calls++; PyOps.SetItem(od, "c", 3L); return true; }));
        var pair = Assert.IsType<PyTuple>(Call(od, "popitem", truth)); Assert.Equal("c", pair.Items[0]); Assert.Equal(1, calls);
        Assert.Throws<PyTypeError>(() => Named(od, "move_to_end", ["a", truth], ["extra"], [1L])); Assert.Equal(1, calls);
        object empty = New(); Assert.Throws<PyKeyError>(() => Call(empty, "move_to_end", new PyList([])));
        Assert.Throws<PyTypeError>(() => Call(empty, "pop", new PyList([]), "default"));
        Assert.Equal("'dictionary is empty'", Assert.Throws<PyKeyError>(() => Call(empty, "popitem")).PyMessage);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void IteratorsObserveOverwriteNoopAndPoisonOnlyOnce(bool reverse)
    {
        object od = New(("a", 1L), ("b", 2L), ("c", 3L));
        object view = Call(od, "values")!;
        IPyIterator cursor = reverse ? (IPyIterator)Call(view, "__reversed__")! : PyIter.Create(view);
        Assert.True(cursor.MoveNext()); Call(od, "move_to_end", "c"); PyOps.SetItem(od, "b", 20L);
        Assert.True(cursor.MoveNext()); Assert.Equal(20L, cursor.Current());
        Call(od, "move_to_end", "a");
        Assert.Equal("OrderedDict mutated during iteration", Assert.Throws<PyRuntimeError>(() => cursor.MoveNext()).PyMessage);
        Assert.False(cursor.MoveNext()); Assert.False(cursor.MoveNext());
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NoNextKeyMeansExhaustionBeforeMutationCheck(bool reverse)
    {
        object od = New(); IPyIterator empty = reverse ? (IPyIterator)Call(od, "__reversed__")! : PyIter.Create(od);
        PyOps.SetItem(od, "a", 1L); Assert.False(empty.MoveNext());
        IPyIterator cursor = reverse ? (IPyIterator)Call(od, "__reversed__")! : PyIter.Create(od);
        Assert.True(cursor.MoveNext()); PyOps.SetItem(od, "b", 2L); Assert.False(cursor.MoveNext());
        Assert.Same(cursor, Call(cursor, "__iter__"));
    }
    [Fact]
    public void ContentCallbacksUseRawOrderAndOrderedOperandWhenPlainDictIsLeft()
    {
        var log = new List<string>();
        object Value(string name) => User(("__eq__", _ => { log.Add(name); return true; }));
        object od = New(("a", Value("a")), ("b", Value("b")), ("c", Value("c")));
        var plain = new PyDict(); plain.Set("c", Value("wrong")); plain.Set("b", Value("wrong")); plain.Set("a", Value("wrong"));
        Call(od, "move_to_end", "a"); Assert.True(PyOps.Compare(PyOps.Eq, plain, od));
        Assert.Equal(new[] { "a", "b", "c" }, log);
        object other = New(("b", PyOps.GetItem(od, "b")), ("c", PyOps.GetItem(od, "c")), ("a", PyOps.GetItem(od, "a")));
        Assert.True(PyOps.Compare(PyOps.Eq, od, other)); Call(other, "move_to_end", "b"); Assert.True(PyOps.Compare(PyOps.Ne, od, other));
    }
    [Fact]
    public void ForeignEqNeAreIndependentAndNonBoolAbiRejects()
    {
        var log = new List<string>(); object od = New();
        object foreign = User(("__eq__", _ => { log.Add("eq"); return true; }), ("__ne__", _ => { log.Add("ne"); return true; }));
        Assert.True(PyOps.Compare(PyOps.Eq, od, foreign)); Assert.True(PyOps.Compare(PyOps.Ne, od, foreign));
        Assert.Equal(new[] { "eq", "ne" }, log);
        object nonbool = User(("__eq__", _ => "result")); Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, od, nonbool));
        object refused = User(("__eq__", _ => throw new PyAssertionError("must not call eq")), ("__ne__", _ => PyNotImplemented.Instance));
        Assert.True(PyOps.Compare(PyOps.Ne, od, refused));
    }
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ArithmeticOpcodesNeverDispatchAsComparison(bool scalarLeft, bool subtract)
    {
        object od = New(); object left = scalarLeft ? 1L : od;
        var error = Assert.Throws<PyTypeError>(() => PyOps.Binary(subtract ? PyOps.Sub : PyOps.Add, left, od));
        string symbol = subtract ? "-" : "+";
        Assert.Equal($"unsupported operand type(s) for {symbol}: '{(scalarLeft ? "int" : "collections.OrderedDict")}' and 'collections.OrderedDict'", error.PyMessage);
    }
    [Fact]
    public void LiveViewsAndRecursiveReprRetainOwners()
    {
        object od = New(("a", 1L), ("b", 2L)); object keys = Call(od, "keys")!; object values = Call(od, "values")!;
        Call(od, "move_to_end", "a"); Assert.Equal("odict_keys(['b', 'a'])", PyRepr.Repr(keys));
        Assert.True(PyOps.Compare(PyOps.Eq, keys, Call(New(("a", 1L), ("b", 2L)), "keys")!));
        Assert.True(PyOps.Compare(PyOps.Eq, values, values)); Assert.False(PyOps.Compare(PyOps.Eq, values, Call(od, "values")!));
        object cycle = New(); PyOps.SetItem(cycle, "self", cycle); Assert.Equal("OrderedDict({'self': ...})", PyRepr.Repr(cycle));
        Call(od, "clear"); Assert.Empty(Items(keys)); PyOps.SetItem(od, "z", 9L); Assert.Equal(new object?[] { "z" }, Items(keys));
    }
    [Fact]
    public void ViewExplicitIdentitySlotsAndClassAbcMatchTheirInstances()
    {
        object od = New(("a", 1L)); object values = Call(od, "values")!;
        Assert.Same(PyNotImplemented.Instance, Call(values, "__eq__", Call(od, "values")));
        Assert.Same(PyNotImplemented.Instance, Call(values, "__ne__", Call(od, "values")));
        Assert.Equal(true, Call(values, "__eq__", values)); Assert.Equal(false, Call(values, "__ne__", values));
        foreach (string method in new[] { "keys", "values", "items" })
        {
            object view = Call(od, method)!; object iterator = PyIter.Create(view);
            Assert.True((bool)PyOps.CallBuiltin("issubclass", [PyObject.TypeOf(view), PyAbc.CreateAttributes()["Collection"]], [], [])!);
            Assert.True((bool)PyOps.CallBuiltin("issubclass", [PyObject.TypeOf(iterator), PyAbc.CreateAttributes()["Iterator"]], [], [])!);
        }
    }

    [Fact]
    public void EmptyMoveRejectsUnknownKeysBeforeReprAndKeepsTruthFirst()
    {
        var log = new List<string>(); object od = New();
        object key = User(("__hash__", _ => { log.Add("hash"); return 7L; }),
            ("__repr__", _ => { log.Add("repr"); throw new PyRuntimeError("must not format key"); }));
        object truth = User(("__bool__", _ => { log.Add("truth"); return true; }));
        foreach (object candidate in new object[] { key, new PyList([key]), new PyTuple([key]), new PyBytes([1]), new PyTuple([]) })
        {
            log.Clear(); Assert.Throws<PyNotImplementedError>(() => Call(od, "move_to_end", candidate, truth));
            Assert.Equal(new[] { "truth" }, log); Assert.Equal(0L, PyOps.Len(od));
        }
    }
    [Fact]
    public void EmptyMoveKeepsOnlyCallbackFreeBuiltinKeyErrors()
    {
        object od = New();
        foreach (object key in new object[] { "missing", 3L, PyNone.Instance, new PyList([]), new PyDict(), new PySet(), new PyByteArray([]) })
        {
            var error = Assert.Throws<PyKeyError>(() => Call(od, "move_to_end", key));
            if (key is PyNone) { Assert.Empty(error.Args); Assert.Equal("", error.PyMessage); }
            else { Assert.Same(key, error.Args[0]); Assert.Equal(PyRepr.Repr(key), error.PyMessage); }
        }
    }
    [Fact]
    public void EmptyMoveKeyBoundaryUsesStateAfterTruthCallback()
    {
        object od = New(("seed", 1L)); var key = new PyList([1L]); int calls = 0;
        object clear = User(("__bool__", _ => { calls++; Call(od, "clear"); return true; }));
        Assert.Throws<PyNotImplementedError>(() => Call(od, "move_to_end", key, clear)); Assert.Equal(0L, PyOps.Len(od));
        object fill = User(("__bool__", _ => { calls++; PyOps.SetItem(od, "new", 2L); return true; }));
        Assert.Throws<PyTypeError>(() => Call(od, "move_to_end", key, fill)); Assert.Equal(2, calls);
        Assert.Equal(2L, PyOps.GetItem(od, "new"));
    }

    [Fact]
    public void MissingNoneMoveKeepsEmptyArgsInEmptyAndFilledMappings()
    {
        foreach (object od in new[] { New(), New(("seed", 1L)) })
        {
            var error = Assert.Throws<PyKeyError>(() => Call(od, "move_to_end", PyNone.Instance));
            Assert.Empty(error.Args); Assert.Equal("", error.PyMessage);
            PyOps.SetItem(od, PyNone.Instance, 9L); Assert.Same(PyNone.Instance, Call(od, "move_to_end", PyNone.Instance));
            Assert.Equal(9L, PyOps.GetItem(od, PyNone.Instance));
            int truths = 0;
            object clear = User(("__bool__", _ => { truths++; Call(od, "clear"); return true; }));
            var cleared = Assert.Throws<PyKeyError>(() => Call(od, "move_to_end", PyNone.Instance, clear));
            Assert.Empty(cleared.Args); Assert.Equal(0L, PyOps.Len(od));
            object refill = User(("__bool__", _ => { truths++; PyOps.SetItem(od, PyNone.Instance, 10L); return false; }));
            Assert.Same(PyNone.Instance, Call(od, "move_to_end", PyNone.Instance, refill));
            Assert.Equal(2, truths); Assert.Equal(10L, PyOps.GetItem(od, PyNone.Instance));
        }
    }

}
