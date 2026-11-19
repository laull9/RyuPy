// 真实list派生的公共入口闭合：不借反射调用内部协议，数值守卫只用反射观测存储身份。
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyListRuntimeWiringTests
{
    private static PyInstance New(params object?[] items)
    {
        var cls = (PyClass)PyOps.MakeClass("WiredList", [PyType.Get("list")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke([new PyList(items)], [], []));
    }
    private static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);
    private static object? Method(object value, string name, params object?[] args) => PyOps.CallMethod(value, name, args, [], []);

    [Fact]
    public void PublicSequenceAndInPlaceSlotsRetainIdentity()
    {
        var value = New(1L, 2L);
        Assert.Equal(2L, PyOps.Len(value)); Assert.True(PyOps.Truthy(value));
        Assert.Equal(1L, PyOps.GetItem(value, 0L));
        PyOps.SetItem(value, 0L, 9L); Assert.True(PyOps.Contains(value, 9L));
        PyOps.DelItem(value, 1L); Assert.Equal("[9]", PyRepr.Repr(value));
        Assert.Same(value, PyOps.InPlaceBinary(PyOps.Add, value, value));
        Assert.Same(value, PyOps.InPlaceBinary(PyOps.Mul, value, 2L));
        Assert.Equal("[9, 9, 9, 9]", PyRepr.Str(value));
        Assert.IsType<PyList>(PyOps.Binary(PyOps.Add, value, new PyList()));
        Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, 1L, value));
        Assert.IsType<PyList>(Method(value, "copy"));
    }

    [Fact]
    public void IteratorsRemainLiveAndExhaustionIsSticky()
    {
        var value = New(1L); var iterator = PyIter.Create(value);
        Assert.True(iterator.MoveNext()); Assert.Equal(1L, iterator.Current());
        Method(value, "append", 2L); Assert.True(iterator.MoveNext()); Assert.Equal(2L, iterator.Current());
        Assert.False(iterator.MoveNext()); Method(value, "append", 3L); Assert.False(iterator.MoveNext());
        var reverse = Assert.IsAssignableFrom<IPyIterator>(Builtin("reversed", value));
        Assert.True(reverse.MoveNext()); Assert.Equal(3L, reverse.Current());
        PyOps.SetItem(value, 1L, 8L); Assert.True(reverse.MoveNext()); Assert.Equal(8L, reverse.Current());
    }

    [Fact]
    public void ReprUsesPublicOwnerAndCustomReprStillWins()
    {
        var value = New(); Method(value, "append", value);
        Assert.Equal("[[...]]", PyRepr.Repr(value));
        value.Class.SetAttr("__repr__", PyNativeFunctions.CreateVariadic("masked", _ => "masked"));
        Assert.Equal("masked", PyRepr.Str(value));
        Assert.Equal("[masked]", PyOps.CallValue(PyObject.GetAttr(PyType.Get("list"), "__repr__"), [value], [], []));
    }

    [Fact]
    public void SavedRootMethodRetainsPublicSelfAndShadowingUsesRealAttribute()
    {
        var value = New(); object? saved = PyOps.PrepareMethod(value, "append");
        Assert.Same(value, PyObject.GetAttr(saved, "__self__"));
        PyObject.SetAttr(value, "append", 99L);
        Assert.Equal(99L, PyOps.CaptureModuleTarget(value, "append"));
        PyOps.CallValue(saved, [7L], [], []); Assert.Equal("[7]", PyRepr.Repr(value));
        PyObject.DelAttr(value, "append"); Method(value, "append", 8L);
        Assert.Equal("[7, 8]", PyRepr.Repr(value));
    }

    [Fact]
    public void RootProtocolsDoNotSwallowForeignReflectedArithmetic()
    {
        var value = New(1L); var cls = new PyClass("Foreign");
        cls.SetAttr("__radd__", PyNativeFunctions.CreateVariadic("radd", _ => "foreign add"));
        cls.SetAttr("__rmul__", PyNativeFunctions.CreateVariadic("rmul", _ => "foreign mul"));
        var foreign = new PyInstance(cls);
        Assert.Equal("foreign add", PyOps.Binary(PyOps.Add, value, foreign));
        Assert.Equal("foreign mul", PyOps.Binary(PyOps.Mul, value, foreign));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(PyType.Get("list"), "__add__"), [value, foreign], [], []));
    }

    [Fact]
    public void EqualityTypeAndHashPathsKnowTheListFamily()
    {
        var value = New(1L, 2L); var exact = new PyList([1L, 2L]);
        Assert.True(PyHash.Equal(value, exact)); Assert.True(PyHash.Equal(exact, value));
        Assert.True(PyOps.Compare(PyOps.Le, exact, value));
        Assert.Equal(true, Builtin("isinstance", value, PyType.Get("list")));
        Assert.Equal(true, Builtin("issubclass", value.Class, PyType.Get("list")));
        Assert.Contains("WiredList", Assert.Throws<PyTypeError>(() => PyHash.Of(value)).Message);
        Assert.Throws<PyTypeError>(() => new PyDict().Set(value, 1L));
        Assert.Throws<PyTypeError>(() => Builtin("set", new PyList([value])));
        Assert.Throws<PyTypeError>(() => Builtin("frozenset", new PyList([value])));
        Assert.Equal(true, Builtin("isinstance", value, PyAbc.CreateAttributes()["MutableSequence"]));
    }

    [Fact]
    public void IndependentConsumersUseDefaultListIteration()
    {
        var value = New(1L, 2L, 3L);
        Assert.Equal("(1, 2, 3)", PyRepr.Repr(PyOps.UnpackSequence(value, 3)));
        Assert.Equal("(1, [2], 3)", PyRepr.Repr(PyOps.UnpackEx(value, 1, 1)));
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<PyByteArray>(Builtin("bytearray", value)).Bytes);
        var bytes = new PyByteArray([9]);
        bytes.SetSlice(new PySlice(0L, 1L, PyNone.Instance), value);
        Assert.Equal(new byte[] { 1, 2, 3 }, bytes.Bytes);
        foreach (string method in new[] { "copy", "deepcopy" })
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCopy.CreateAttributes()[method], [value], [], []));
    }

    [Fact]
    public void JsonUsesListPayloadAndDetectsPublicCycleBeforeDefault()
    {
        var value = New(1L, 2L); object? dumps = PyJsonModule.CreateAttributes()["dumps"];
        Assert.Equal("[1, 2]", PyOps.CallValue(dumps, [value], [], []));
        int calls = 0;
        object fallback = PyNativeFunctions.CreateVariadic("default", _ => { calls++; return "wrong"; });
        Method(value, "append", value);
        Assert.Throws<PyValueError>(() => PyOps.CallValue(dumps, [value], ["default"], [fallback]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void NumericGuardsDoNotReadOrMaterializeDerivedPayload()
    {
        var value = New(1.5); var payload = (PyList)value.GetType().GetField("Payload", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
        var field = typeof(PyList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object? before = field.GetValue(payload);
        Assert.False(PyNumericListOps.CanReadF64(value, 0L)); Assert.Null(PyNumericListOps.PeekList(value, 0L));
        Assert.Throws<PyTypeError>(() => PyNumericListOps.ReadF64(value, 0L));
        int calls = 0; var indexClass = new PyClass("IndexProbe");
        indexClass.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ => { calls++; return 0L; }));
        var index = new PyInstance(indexClass);
        Assert.False(PyNumericListOps.CanReadF64(value, index)); Assert.Null(PyNumericListOps.PeekList(value, index));
        Assert.Equal(0, calls);
        Assert.Same(before, field.GetValue(payload)); Assert.Equal(1, payload.Count);
    }
}
