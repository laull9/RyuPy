// len 冷长度校验与共享真值的有限合同；只用公开入口，错误现场测试串行避免全局帧栈交错。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyLenProtocolTests
{
    private static object? Len(object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallBuiltin("len", args, names ?? [], values ?? []);
    private static PyInstance Slot(string name, Func<object?> body)
    {
        var cls = new PyClass("LengthProtocol");
        cls.SetAttr(name, PyNativeFunctions.Create(name, ["self"], [null], 1, _ => body()));
        return new PyInstance(cls);
    }
    private static PyInstance Length(object? result) => Slot("__len__", () => result);
    private static void Caller(Action<PyValueError> body)
    {
        long depth = PyTraceback.PushFrame("len_test.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try { PyTraceback.SetLine(37); body(outer); }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
    private static void Prepared(PyBaseException error, PyBaseException outer)
    {
        Assert.Same(outer, error.Context);
        Assert.Single(error.Frames);
        Assert.Equal(new PyStackFrame("len_test.py", "caller", 37), error.Frames[0]);
    }
    private sealed class NativeSlot(string name, Func<object?> body) : PyNativeObject
    {
        public override string TypeName => "NativeLength";
        public override object? GetAttr(string attribute) => attribute == name
            ? PyNativeFunctions.Create(name, [], [], 0, _ => body())
            : throw Missing(attribute);
    }

    [Fact]
    public void ExistingExactContainersAndUnicodeStringLayoutKeepTheirLengths()
    {
        var dict = new PyDict(); dict.Set("a", 1L);
        object[] one = [new PyList([1L]), new PyTuple([1L]), dict, new PySet([1L]),
            new PyFrozenSet([1L]), new PyDictView(dict, PyDictViewKind.Keys), new PyBytes([1]), new PyByteArray([1])];
        foreach (object value in one)
        {
            Assert.Equal(1L, PyOps.Len(value)); Assert.Equal(1L, Len([value])); Assert.True(PyOps.Truthy(value));
        }
        Assert.Equal(2L, PyOps.Len("😀a")); Assert.Equal(0L, PyOps.Len(""));
        var cls = (PyClass)PyOps.MakeClass("LengthText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object text = cls.Invoke(["😀a"], [], [])!;
        Assert.Equal(2L, PyOps.Len(text)); Assert.True(PyOps.Truthy(text));
        Assert.Throws<PyNotImplementedError>(() => cls.SetAttr("__len__", PyNone.Instance));
    }

    [Fact]
    public void DirectIntegersAndBooleansReturnMachineLengthAndSharedTruth()
    {
        foreach (var (result, expected) in new (object, long)[] {
            (false, 0L), (true, 1L), (0, 0L), (3, 3L), (0L, 0L), (3L, 3L),
            ((long)int.MaxValue + 1, (long)int.MaxValue + 1), (long.MaxValue, long.MaxValue),
            (BigInteger.Zero, 0L), (new BigInteger(3), 3L), (new BigInteger(long.MaxValue), long.MaxValue) })
        {
            var value = Length(result);
            Assert.Equal(expected, PyOps.Len(value)); Assert.Equal(expected, Assert.IsType<long>(Len([value])));
            Assert.Equal(expected != 0, PyOps.Truthy(value));
        }
    }

    [Fact]
    public void NegativeValuesPrecedeOverflowAndPositiveOverflowHasPythonMessage()
    {
        foreach (object result in new object[] { -1, -1L, long.MinValue, -(BigInteger.One << 100) })
        {
            var value = Length(result);
            Assert.Equal("__len__() should return >= 0", Assert.Throws<PyValueError>(() => PyOps.Len(value)).Message);
            Assert.Equal("__len__() should return >= 0", Assert.Throws<PyValueError>(() => PyOps.Truthy(value)).Message);
        }
        foreach (object result in new object[] { (BigInteger)long.MaxValue + 1, BigInteger.One << 100 })
        {
            var value = Length(result);
            const string message = "cannot fit 'int' into an index-sized integer";
            Assert.Equal(message, Assert.Throws<PyOverflowError>(() => PyOps.Len(value)).Message);
            Assert.Equal(message, Assert.Throws<PyOverflowError>(() => PyOps.Truthy(value)).Message);
        }
    }

    [Fact]
    public void IndexRunsOnceAndNativeReceiversUseTheSameColdValidation()
    {
        int lengths = 0, indices = 0; object current = 3L;
        var index = Slot("__index__", () => { indices++; return current; });
        var value = Slot("__len__", () => { lengths++; return index; });
        Assert.Equal(3L, PyOps.Len(value)); Assert.Equal(1, lengths); Assert.Equal(1, indices);
        current = 0L; Assert.False(PyOps.Truthy(value)); Assert.Equal(2, lengths); Assert.Equal(2, indices);
        current = -(BigInteger.One << 100); Assert.Throws<PyValueError>(() => PyOps.Len(value));
        current = BigInteger.One << 100; Assert.Throws<PyOverflowError>(() => PyOps.Len(value));
        Assert.Equal(4, lengths); Assert.Equal(4, indices);
        var nativeIndex = new NativeSlot("__index__", () => { indices++; return 7L; });
        var native = new NativeSlot("__len__", () => { lengths++; return nativeIndex; });
        Assert.Equal(7L, PyOps.Len(native)); Assert.True(PyOps.Truthy(native));
        Assert.Equal(6, lengths); Assert.Equal(6, indices);
    }

    [Fact]
    public void InvalidResultsNeverUseIntOrRecursivelyConvertIndexResults()
    {
        foreach (var (result, type) in new (object, string)[] { (1.5, "float"), (PyNone.Instance, "NoneType"), ("2", "str") })
            Assert.Equal($"'{type}' object cannot be interpreted as an integer",
                Assert.Throws<PyTypeError>(() => PyOps.Len(Length(result))).Message);
        int calls = 0;
        var intOnly = Slot("__int__", () => { calls++; return 2L; });
        Assert.Throws<PyTypeError>(() => PyOps.Len(Length(intOnly))); Assert.Equal(0, calls);
        var nested = Slot("__index__", () => { calls++; return 2L; });
        Assert.Equal("__index__ returned non-int (type LengthProtocol)",
            Assert.Throws<PyTypeError>(() => PyOps.Len(Length(Slot("__index__", () => nested)))).Message);
        Assert.Equal(0, calls);
        Assert.Equal("__index__ returned non-int (type float)",
            Assert.Throws<PyTypeError>(() => PyOps.Len(Length(Slot("__index__", () => 1.5)))).Message);
        var disabled = new PyInstance(new PyClass("DisabledIndex")); disabled.Class.SetAttr("__index__", PyNone.Instance);
        Assert.Equal("'NoneType' object is not callable", Assert.Throws<PyTypeError>(() => PyOps.Len(Length(disabled))).Message);
    }

    [Fact]
    public void DeprecatedIndexBoolKeepsExplicitBoundaryAndOldTruthMessage()
    {
        foreach (bool result in new[] { false, true })
        {
            var value = Length(Slot("__index__", () => result));
            Assert.Equal("len(): __index__ returning bool requires DeprecationWarning support",
                Assert.Throws<PyNotImplementedError>(() => PyOps.Len(value)).Message);
            Assert.Equal("truth testing: __index__ returning bool requires DeprecationWarning support",
                Assert.Throws<PyNotImplementedError>(() => PyOps.Truthy(value)).Message);
            Assert.Equal(result ? 1L : 0L, PyOps.Len(Length(result)));
        }
    }

    [Fact]
    public void TypeSlotsIgnoreInstanceShadowAndKeepBoolPriorityAndRawMethodResults()
    {
        int lengths = 0, indices = 0, booleans = 0;
        var index = Slot("__index__", () => { indices++; return 3L; });
        var value = Slot("__len__", () => { lengths++; return index; });
        PyObject.SetAttr(value, "__len__", PyNativeFunctions.Create("shadow", [], [], 0, _ => 99L));
        Assert.Equal(3L, PyOps.Len(value)); Assert.Equal(1, lengths); Assert.Equal(1, indices);
        // 普通直接方法调用仍取实例属性，不被 len 的结果转换侵入。
        Assert.Equal(99L, PyOps.CallMethod(value, "__len__", [], [], []));
        var raw = Length(index);
        Assert.Same(index, PyOps.CallMethod(raw, "__len__", [], [], [])); Assert.Equal(1, indices);
        value.Class.SetAttr("__bool__", PyNativeFunctions.Create("__bool__", ["self"], [null], 1, _ => { booleans++; return false; }));
        Assert.False(PyOps.Truthy(value)); Assert.Equal(1, booleans); Assert.Equal(1, lengths);
        value.Class.SetAttr("__len__", PyNativeFunctions.Create("__len__", ["self"], [null], 1, _ => 4L));
        Assert.Equal(4L, PyOps.Len(value));
        var plain = new PyInstance(new PyClass("Plain")); Assert.True(PyOps.Truthy(plain));
        Assert.Equal("object of type 'Plain' has no len()", Assert.Throws<PyTypeError>(() => PyOps.Len(plain)).Message);
        plain.Class.SetAttr("__len__", PyNone.Instance);
        Assert.Equal("'NoneType' object is not callable", Assert.Throws<PyTypeError>(() => PyOps.Len(plain)).Message);
    }

    [Fact]
    public void LocalBinderRejectsKeywordsBeforeArityWithoutCallingLength()
    {
        int calls = 0; var value = Slot("__len__", () => { calls++; return 3L; });
        Assert.Equal("len() takes exactly one argument (0 given)", Assert.Throws<PyTypeError>(() => Len([])).Message);
        Assert.Equal("len() takes exactly one argument (2 given)", Assert.Throws<PyTypeError>(() => Len([value, value])).Message);
        foreach (object?[] arguments in new object?[][] { [], [value], [value, value] })
            Assert.Equal("len() takes no keyword arguments", Assert.Throws<PyTypeError>(() => Len(arguments, ["obj"], [value])).Message);
        Assert.Throws<PyTypeError>(() => Len([PyOps.StarArg(new PyList([]))]));
        Assert.Throws<PyTypeError>(() => Len([PyOps.StarArg(new PyList([value, value]))]));
        Assert.Equal(0, calls);
        Assert.Equal(3L, Len([PyOps.StarArg(new PyList([value]))]));
        Assert.Equal(3L, Len([value], [PyNone.Instance], [new PyDict()]));
        Assert.Equal(3L, PyOps.CallValue(PyOps.BuiltinFunction("len"), [value], [], [])); Assert.Equal(3, calls);
    }

    [Fact]
    public void OwnErrorsPrepareOnlyCallerFrameAndAmbientContext()
    {
        Caller(outer =>
        {
            Prepared(Assert.Throws<PyTypeError>(() => Len([])), outer);
            Prepared(Assert.Throws<PyTypeError>(() => Len([1L, 2L], ["x"], [3L])), outer);
            Prepared(Assert.Throws<PyTypeError>(() => PyOps.Len(1L)), outer);
            Prepared(Assert.Throws<PyTypeError>(() => PyOps.Len(Length(1.5))), outer);
            Prepared(Assert.Throws<PyTypeError>(() => PyOps.Len(Length(Slot("__index__", () => 1.5)))), outer);
            Prepared(Assert.Throws<PyValueError>(() => PyOps.Len(Length(-(BigInteger.One << 100)))), outer);
            Prepared(Assert.Throws<PyOverflowError>(() => PyOps.Len(Length(BigInteger.One << 100))), outer);
            Prepared(Assert.Throws<PyNotImplementedError>(() => PyOps.Len(Length(Slot("__index__", () => true)))), outer);
            Prepared(Assert.Throws<PyValueError>(() => PyOps.Truthy(Length(-1L))), outer);
        });
    }

    [Fact]
    public void UserLengthAndIndexFailuresKeepIdentityFramesAndExistingContext()
    {
        foreach (bool indexed in new[] { false, true })
        {
            var error = new PyLookupError("user protocol");
            var failing = Slot(indexed ? "__index__" : "__len__", () => throw error);
            var value = indexed ? Length(failing) : failing;
            Caller(_ => Assert.Same(error, Assert.Throws<PyLookupError>(() => PyOps.Len(value))));
            Assert.Empty(error.Frames); Assert.Null(error.Context);
            Caller(_ => PyErrors.PrepareRaise(error));
            var frames = error.Frames; var context = error.Context;
            Caller(_ =>
            {
                Assert.Same(error, Assert.Throws<PyLookupError>(() => PyOps.Len(value)));
                Assert.Same(error, Assert.Throws<PyLookupError>(() => PyOps.Truthy(value)));
            });
            Assert.Same(frames, error.Frames); Assert.Same(context, error.Context);
        }
    }
}
