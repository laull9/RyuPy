// slice公开有限类型合同：原对象身份、任意精度indices、绑定、比较短路、哈希副作用与明确边界。
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PySlicePublicTests
{
    private static PySlice S(params object?[] args) => Assert.IsType<PySlice>(PyOps.CallValue(PyType.Get("slice"), args, [], []));
    private static object? Call(object receiver, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(receiver, name), args, [], []);
    private static PyTuple Indices(PySlice slice, object? length) => Assert.IsType<PyTuple>(Call(slice, "indices", length));
    private static PyInstance Slot(string name, Func<object?> callback)
    {
        var cls = new PyClass("Callback");
        cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => callback()));
        return new PyInstance(cls);
    }
    private static void Tuple(PyTuple actual, params object?[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Assert.True(PyHash.Equal(expected[i], actual.Items[i]));
    }

    [Fact]
    public void ConstructorUsesRealTypeAndRetainsFieldsWithoutConversion()
    {
        int calls = 0; object field = Slot("__index__", () => { calls++; return 3L; });
        var one = S(field); var two = S(field, field); var three = S(field, field, field);
        Assert.Same(PyType.Get("slice"), PyObject.TypeOf(one));
        Assert.True(one.Start is null or PyNone); Assert.Same(field, one.Stop); Assert.True(one.Step is null or PyNone);
        Assert.Same(field, two.Start); Assert.Same(field, two.Stop); Assert.True(two.Step is null or PyNone);
        Assert.Same(field, three.Start); Assert.Same(field, three.Stop); Assert.Same(field, three.Step);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ConstructorRejectsKeywordsBeforeArityAndFieldsAreReadonly()
    {
        Assert.Equal("slice expected at least 1 argument, got 0", Assert.Throws<PyTypeError>(() => S()).PyMessage);
        Assert.Equal("slice expected at most 3 arguments, got 4", Assert.Throws<PyTypeError>(() => S(1L, 2L, 3L, 4L)).PyMessage);
        Assert.Equal("slice() takes no keyword arguments", Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyType.Get("slice"), [1L, 2L, 3L, 4L], ["stop"], [1L])).PyMessage);
        var slice = S(3L);
        foreach (string field in new[] { "start", "stop", "step" })
        {
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(slice, field, 5L));
            Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(slice, field));
        }
    }

    [Fact]
    public void PublicIndicesPreservesUnboundedIntegersAndNegativeDefaults()
    {
        BigInteger huge = BigInteger.Pow(10, 100);
        Tuple(Indices(S((object?)null), huge), 0L, huge, 1L);
        Tuple(Indices(S(null, null, -1L), huge), huge - 1, -1L, -1L);
        Tuple(Indices(S(-huge, huge, huge), 7L), 0L, 7L, huge);
        Tuple(Indices(S(huge, -huge, -huge), 7L), 6L, -1L, -huge);
        Tuple(Indices(S(null, -1L, -1L), 7L), 6L, 6L, -1L);
        Tuple(Indices(S(false, true, true), true), 0L, 1L, 1L);
        Tuple(Indices(S(null, null, -1L), 0L), -1L, -1L, -1L);
        Tuple(Assert.IsType<PyTuple>(Call(PyType.Get("slice"), "indices", S((object?)null), huge)), 0L, huge, 1L);
        Assert.Equal((0L, 7L, 1L, 7L), new PySlice(null, null, null).Indices(7));
    }

    [Fact]
    public void IndicesConvertsLengthStepStartStopInOrderAndStopsAtFailure()
    {
        var events = new List<string>();
        object I(string name, object? value) => Slot("__index__", () => { events.Add(name); return value; });
        var slice = S(I("start", 1L), I("stop", 8L), I("step", 2L));
        Tuple(Indices(slice, I("length", 10L)), 1L, 8L, 2L);
        Assert.Equal(new[] { "length", "step", "start", "stop" }, events);
        events.Clear(); Assert.Throws<PyValueError>(() => Indices(slice, I("length", -1L))); Assert.Equal(new[] { "length" }, events);
        events.Clear(); Assert.Throws<PyValueError>(() => Indices(S(I("start", 1L), I("stop", 2L), I("step", 0L)), I("length", 3L)));
        Assert.Equal(new[] { "length", "step" }, events);
        Assert.Throws<PyTypeError>(() => Indices(slice, null));
    }

    [Fact]
    public void IndexUserExceptionsKeepIdentityAtEveryPosition()
    {
        foreach (int position in new[] { 0, 1, 2, 3 })
        {
            var marker = new PyLookupError("index marker");
            object bad = Slot("__index__", () => throw marker);
            object?[] fields = [0L, 3L, 1L]; if (position < 3) fields[position] = bad;
            Assert.Same(marker, Assert.Throws<PyLookupError>(() => Indices(S(fields), position == 3 ? bad : 5L)));
        }
    }

    [Fact]
    public void BoolReturningIndexAndComplexDescriptorAreExplicitlyUnsupported()
    {
        Assert.Throws<PyNotImplementedError>(() => Indices(S((object?)null), Slot("__index__", () => true)));
        int calls = 0; var descriptor = Slot("__get__", () => { calls++; return 3L; });
        var cls = new PyClass("Indexed"); cls.SetAttr("__index__", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Indices(S((object?)null), new PyInstance(cls)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void PublicMethodsBindReceiverAndRejectWrongArity()
    {
        var root = PyType.Get("slice"); var slice = S(3L);
        Assert.Equal("slice.indices() takes exactly one argument (0 given)", Assert.Throws<PyTypeError>(() => Call(slice, "indices")).PyMessage);
        Assert.Equal("unbound method slice.indices() needs an argument", Assert.Throws<PyTypeError>(() => Call(root, "indices")).PyMessage);
        Assert.Throws<PyTypeError>(() => Call(root, "indices", 1L, 2L));
        Assert.Throws<PyTypeError>(() => Call(slice, "indices", 1L, 2L));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(slice, "indices"), [], ["length"], [1L]));
        Assert.Equal("slice(None, 3, None)", Call(slice, "__repr__"));
        Assert.Equal(Call(slice, "__repr__"), Call(root, "__repr__", slice));
        foreach (string method in new[] { "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__" })
        {
            Assert.Same(PyNotImplemented.Instance, Call(slice, method, 1L));
            Assert.Same(PyNotImplemented.Instance, Call(root, method, slice, 1L));
        }
    }

    [Fact]
    public void ComparisonSkipsIdenticalFieldsAndNeverPrechecksLaterFields()
    {
        int calls = 0; object field = Slot("__eq__", () => { calls++; throw new PyRuntimeError("unvisited"); });
        Assert.True(PyHash.Equal(S(field, 1L), S(field, 1L)));
        Assert.Equal(true, Call(S(field, 1L), "__lt__", S(field, 2L)));
        Assert.Equal(true, Call(S(1L, field), "__lt__", S(2L, new PyList())));
        Assert.Equal(false, Call(S(1L, field), "__eq__", S(2L, new PyList())));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DistinctUserAndContainerFieldsFailWithoutCallingComparison()
    {
        int calls = 0;
        object A() => Slot("__eq__", () => { calls++; return true; });
        Assert.Throws<PyNotImplementedError>(() => PyHash.Equal(S(A()), S(A())));
        Assert.Throws<PyNotImplementedError>(() => Call(S(A()), "__lt__", S(A())));
        Assert.Throws<PyNotImplementedError>(() => PyHash.Equal(S(new PyList()), S(new PyList())));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void EqualSlicesShareHashAndDictionarySetLookup()
    {
        var first = S(1L, 4L, 2L); var equal = S(true, 4.0, 2L);
        Assert.True(PyHash.Equal(first, equal)); Assert.Equal(PyHash.Of(first), PyHash.Of(equal));
        var dict = new PyDict(); dict.Set(first, "value"); Assert.Equal("value", dict.Get(equal));
        var set = new PySet(); set.Add(first); Assert.True(set.Contains(equal));
        Assert.Equal(PyHash.Of(first), Call(first, "__hash__"));
        Assert.Equal(PyHash.Of(first), Call(PyType.Get("slice"), "__hash__", first));
    }

    [Fact]
    public void HashRunsEveryComponentInOrderWithoutCachingAndStopsOnFailure()
    {
        var events = new List<string>();
        object H(string name) => Slot("__hash__", () => { events.Add(name); return 1L; });
        var slice = S(H("start"), H("stop"), H("step"));
        PyHash.Of(slice); PyHash.Of(slice);
        Assert.Equal(new[] { "start", "stop", "step", "start", "stop", "step" }, events);
        events.Clear(); Assert.Throws<PyTypeError>(() => PyHash.Of(S(H("start"), new PyList(), H("step"))));
        Assert.Equal(new[] { "start" }, events);
        var marker = new PyRuntimeError("hash marker");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => PyHash.Of(S(Slot("__hash__", () => throw marker)))));
    }

    [Fact]
    public void ComparisonHandlesNestedSlicesBytesNaNAndUnvisitedContainers()
    {
        Assert.True(PyHash.Equal(S(S(1L, 2L)), S(S(1L, 2L))));
        Assert.True(PyHash.Equal(S(new PyBytes([97])), S(new PyByteArray([97]))));
        Assert.Equal(true, Call(S(new PyBytes([97])), "__lt__", S(new PyBytes([98]))));
        object nan = double.NaN;
        Assert.True(PyHash.Equal(S(nan), S(nan)));
        Assert.False(PyHash.Equal(S((object)double.NaN), S((object)double.NaN)));
        Assert.Equal(false, Call(S((object)double.NaN), "__lt__", S(1L)));
        Assert.Throws<PyTypeError>(() => Call(S(null, 1L), "__lt__", S(1L, 1L)));
        var container = new PyList(); Assert.True(PyHash.Equal(S(container), S(container)));
    }

    [Fact]
    public void EveryIndexFailurePreventsLaterConversions()
    {
        foreach (int failure in new[] { 0, 1, 2, 3 })
        {
            var events = new List<string>(); var marker = new PyValueError("conversion failed");
            object I(string name, int order, long result) => Slot("__index__", () =>
            {
                events.Add(name); if (order == failure) throw marker; return result;
            });
            var slice = S(I("start", 2, 0), I("stop", 3, 4), I("step", 1, 1));
            Assert.Same(marker, Assert.Throws<PyValueError>(() => Indices(slice, I("length", 0, 5))));
            string[] order = ["length", "step", "start", "stop"];
            Assert.Equal(order[..(failure + 1)], events);
        }
    }

    [Fact]
    public void PublicFailuresCaptureAmbientContextAndUserFrame()
    {
        PyTraceback.Reset(); var ambient = new PyRuntimeError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("slice_public.py", "caller"); PyTraceback.SetLine(17);
            PyBaseException[] errors = [
                Assert.Throws<PyValueError>(() => Indices(S(null, null, 0L), 3L)),
                Assert.Throws<PyTypeError>(() => Indices(S((object?)null), "bad")),
                Assert.Throws<PyTypeError>(() => PyHash.Of(S(new PyList()))),
                Assert.Throws<PyTypeError>(() => S())];
            foreach (var error in errors)
            {
                Assert.Same(ambient, error.Context); Assert.Equal("caller", Assert.Single(error.Frames).Function);
                Assert.Equal(17, error.Frames[0].Line);
            }
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }


    [Fact]
    public void MakeClassRejectsSliceBaseAndPreservesAmbientContextAndFrame()
    {
        // 消息复用既有CP3.12.13 reference.py的subclass参考，不增加公共应用。
        PyTraceback.Reset(); var ambient = new PyRuntimeError("base ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("slice_subclass.py", "build_child"); PyTraceback.SetLine(23);
            var error = Assert.Throws<PyTypeError>(() => PyOps.MakeClass("Child", [PyType.Get("slice")], null));
            Assert.Equal("type 'slice' is not an acceptable base type", error.PyMessage);
            Assert.Same(ambient, error.Context);
            Assert.Equal("build_child", Assert.Single(error.Frames).Function);
            Assert.Equal(23, error.Frames[0].Line);
            var aliasError = Assert.Throws<PyTypeError>(() => PyRuntimeBases.Validate(PyType.Get("slice")));
            Assert.Equal("type 'slice' is not an acceptable base type", aliasError.PyMessage);
            Assert.Same(ambient, aliasError.Context);
            Assert.Equal("build_child", Assert.Single(aliasError.Frames).Function);
            Assert.Equal(23, aliasError.Frames[0].Line);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }


    [Fact]
    public void DirectAndPreparedMethodRoutesUseRealSliceBindings()
    {
        var slice = S(1L, 9L, 2L); var root = PyType.Get("slice");
        Tuple(Assert.IsType<PyTuple>(PyOps.CallMethod(slice, "indices", [10L], [], [])), 1L, 9L, 2L);
        Tuple(Assert.IsType<PyTuple>(PyOps.CallMethod(root, "indices", [slice, 10L], [], [])), 1L, 9L, 2L);
        object? saved = PyOps.PrepareMethod(slice, "indices");
        slice = S(2L);
        Tuple(Assert.IsType<PyTuple>(PyOps.CallValue(saved, [10L], [], [])), 1L, 9L, 2L);
        Tuple(Assert.IsType<PyTuple>(PyOps.CallValue(PyOps.PrepareMethod(root, "indices"), [slice, 10L], [], [])), 0L, 2L, 1L);
        Assert.Equal("slice(None, 2, None)", PyOps.CallMethod(slice, "__repr__", [], [], []));
        Assert.Equal(true, PyOps.CallMethod(root, "__eq__", [slice, S(2L)], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(root, "indices", [1L, 2L], [], []));
    }

    [Fact]
    public void MissingSliceMethodFailsBeforeStarArgumentExpansion()
    {
        foreach (object receiver in new object[] { S(2L), PyType.Get("slice") })
        {
            int calls = 0;
            var star = PyOps.StarArg(new EmptyIterator(() => calls++));
            Assert.Throws<PyAttributeError>(() => PyOps.CallMethod(receiver, "missing", [star], [], []));
            Assert.Throws<PyAttributeError>(() => PyOps.PrepareMethod(receiver, "missing"));
            Assert.Equal(0, calls);
        }
        int expanded = 0;
        var empty = PyOps.StarArg(new EmptyIterator(() => expanded++));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(S(2L), "indices", [empty], [], []));
        Assert.Equal(1, expanded);
    }

    [Fact]
    public void MixedUserComparisonsFailExplicitlyInBothDirectionsWithoutCallbacks()
    {
        int calls = 0; var cls = new PyClass("Mixed");
        foreach (string name in new[] { "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__" })
            cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => { calls++; return new PyList(); }));
        var other = new PyInstance(cls); var slice = S(2L);
        foreach (int op in new[] { PyOps.Eq, PyOps.Ne, PyOps.Lt, PyOps.Le, PyOps.Gt, PyOps.Ge })
        {
            Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(op, slice, other));
            Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(op, other, slice));
        }
        Assert.Equal(0, calls);
    }

    private sealed class EmptyIterator(Action action) : IPyIterator
    {
        private bool _done;
        public bool MoveNext()
        {
            if (_done) return false;
            _done = true; action(); return false;
        }
        public object? Current() => throw new InvalidOperationException("empty iterator has no current item");
    }


    [Fact]
    public void ComplexProtocolSlotsAreRejectedWithoutCallingUserCode()
    {
        int calls = 0;
        var callable = Slot("__call__", () => { calls++; return 3L; });
        var getter = PyNativeFunctions.CreateVariadic("getter", _ => { calls++; return 3L; });
        object?[] shapes = [new PyProperty(getter), callable, new UnknownNative(), PyObject.GetAttr(S(3L), "indices")];
        foreach (string slot in new[] { "__index__", "__hash__" })
        foreach (bool exception in new[] { false, true })
        foreach (object? shape in shapes)
        {
            var cls = new PyClass("Complex"); cls.SetAttr(slot, shape);
            var error = new PyRuntimeError("custom"); error.BindPythonClass(cls);
            object value = exception ? error : new PyInstance(cls);
            if (slot == "__index__") Assert.Throws<PyNotImplementedError>(() => Indices(S((object?)null), value));
            else Assert.Throws<PyNotImplementedError>(() => PyHash.Of(S(value)));
        }
        foreach (string slot in new[] { "__index__", "__hash__" })
        {
            var cls = new PyClass("Noncallable"); cls.SetAttr(slot, 3L); var value = new PyInstance(cls);
            if (slot == "__index__") Assert.Throws<PyTypeError>(() => Indices(S((object?)null), value));
            else Assert.Throws<PyTypeError>(() => PyHash.Of(S(value)));
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    public void HashUnsupportedLaterFieldsKeepEarlierCallbacksAndSupportedWrappers()
    {
        var events = new List<string>();
        foreach (object unsupported in new object[] { new PyTuple([1L]), new PyFrozenSet([1L]), new UnknownNative() })
        {
            events.Clear();
            var first = Slot("__hash__", () => { events.Add("first"); return 1L; });
            var last = Slot("__hash__", () => { events.Add("last"); return 2L; });
            Assert.Throws<PyNotImplementedError>(() => PyHash.Of(S(first, unsupported, last)));
            Assert.Equal(new[] { "first" }, events);
        }
        var fn = PyNativeFunctions.CreateVariadic("constant", _ => 3L);
        foreach (object slot in new object[] { fn, new PyStaticMethod(fn), new PyClassMethod(fn) })
        {
            var cls = new PyClass("Supported"); cls.SetAttr("__index__", slot); cls.SetAttr("__hash__", slot);
            var value = new PyInstance(cls); Tuple(Indices(S((object?)null), value), 0L, 3L, 1L);
            Assert.Equal(PyHash.Of(S(value)), PyHash.Of(S(value)));
        }
        var rootHash = new PyClass("RootHash"); rootHash.SetAttr("__hash__", PyObject.GetAttr(PyType.Get("object"), "__hash__"));
        var plain = new PyInstance(rootHash); Assert.Equal(PyHash.Of(S(plain)), PyHash.Of(S(plain)));
    }

    [Fact]
    public void MethodIdentityAndMixedExceptionComparisonsRespectFiniteBoundary()
    {
        var slice = S(3L); object? first = PyObject.GetAttr(slice, "indices"), same = PyObject.GetAttr(slice, "indices");
        Assert.True(PyHash.Equal(first, same)); Assert.Equal(PyHash.Of(first), PyHash.Of(same));
        Assert.False(PyHash.Equal(first, PyObject.GetAttr(S(3L), "indices")));
        Assert.Throws<PyNotImplementedError>(() => PyRepr.Repr(first));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(first, "__text_signature__"));
        int calls = 0; var cls = new PyClass("MixedError");
        foreach (string name in new[] { "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__" })
            cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => { calls++; return new PyList(); }));
        var other = new PyRuntimeError("custom"); other.BindPythonClass(cls);
        foreach (int op in new[] { PyOps.Eq, PyOps.Ne, PyOps.Lt, PyOps.Le, PyOps.Gt, PyOps.Ge })
        {
            Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(op, slice, other));
            Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(op, other, slice));
        }
        Assert.Equal(0, calls);
    }

    private sealed class UnknownNative : PyNativeObject
    {
        public override string TypeName => "unknown";
        public override object? GetAttr(string name) => throw Missing(name);
    }

}
