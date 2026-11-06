// 独立根协议断言：元数据/严格绑定、公开self、原位身份、活游标及用户异常链。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyListRootProtocolTests
{
    private static PyClass Class(string name = "L") => (PyClass)PyOps.MakeClass(name, [PyType.Get("list")], null);
    private static PyInstance New(PyClass? cls = null, params object?[] values)
    {
        cls ??= Class(); PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke([new PyList(values)], [], []));
    }
    private static object? Root(string name, params object?[] args) => Call(PyObject.GetAttr(PyType.Get("list"), name), args);
    private static object? Call(object? callable, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(callable, args, names ?? [], values ?? []);
    private static PyFunction Fn(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static PyInstance User(string name, string slot, Func<object?[], object?> body)
    {
        var cls = (PyClass)PyOps.MakeClass(name, [], null);
        cls.SetAttr(slot, Fn(slot, body)); PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }
    [Fact]
    public void RootMetadataAndSavedSelfExposeOwnerNotPayload()
    {
        var value = New(null, 1L, 2L);
        foreach (string name in new[] { "append", "__getitem__", "__reversed__", "__len__", "__str__" })
        {
            var root = PyObject.GetAttr(PyType.Get("list"), name);
            var bound = PyObject.GetAttr(value, name);
            Assert.Same(value, PyObject.GetAttr(bound, "__self__"));
            string owner = name == "__str__" ? "object" : "list";
            Assert.Equal(owner + "." + name, PyObject.GetAttr(root, "__qualname__"));
            Assert.Same(PyType.Get(owner), PyObject.GetAttr(root, "__objclass__"));
        }
        Assert.Same(PyObject.GetAttr(PyType.Get("object"), "__str__"), PyObject.GetAttr(PyType.Get("list"), "__str__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(value, "__hash__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(PyType.Get("list"), "__hash__"));
        Assert.Same(PyType.Get("list"), PyObject.GetAttr(PyObject.GetAttr(value, "__new__"), "__self__"));
    }
    [Theory]
    [InlineData("append", "unbound method list.append() needs an argument")]
    [InlineData("__getitem__", "unbound method list.__getitem__() needs an argument")]
    [InlineData("__len__", "descriptor '__len__' of 'list' object needs an argument")]
    [InlineData("__str__", "descriptor '__str__' of 'object' object needs an argument")]
    public void MissingSelfIsCheckedBeforeRemainingBinding(string name, string expected)
    {
        object? descriptor = PyObject.GetAttr(PyType.Get("list"), name);
        Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Call(descriptor, [], ["x"], [1L])).Message);
    }
    [Fact]
    public void StrictBindingDoesNotMutateAndWrongReceiverWins()
    {
        var value = New(null, 3L, 1L);
        Assert.Equal("descriptor 'append' for 'list' objects doesn't apply to a 'int' object",
            Assert.Throws<PyTypeError>(() => Root("append", 1L)).Message);
        Assert.Equal("descriptor '__len__' requires a 'list' object but received a 'int'",
            Assert.Throws<PyTypeError>(() => Root("__len__", 1L, 9L)).Message);
        Assert.Equal("list.append() takes exactly one argument (0 given)", Assert.Throws<PyTypeError>(() => Root("append", value)).Message);
        Assert.Equal(" expected 2 arguments, got 0", Assert.Throws<PyTypeError>(() => Root("__setitem__", value)).Message);
        Assert.Equal("list.__reversed__() takes no arguments (1 given)", Assert.Throws<PyTypeError>(() => Root("__reversed__", value, 1L)).Message);
        Assert.Equal("[3, 1]", Root("__repr__", value));
    }
    [Fact]
    public void RootsBypassNamedOverridesAndDerivedCopiesAreExact()
    {
        var cls = Class(); cls.SetAttr("append", Fn("append", _ => "override"));
        var value = New(cls, 1L);
        Assert.Equal("override", Call(PyObject.GetAttr(value, "append"), [2L]));
        Assert.Same(PyNone.Instance, Root("append", value, 2L));
        foreach (object? result in new[] { Root("copy", value), Root("__getitem__", value, new PySlice(null, null, null)),
            Root("__add__", value, new PyList()), Root("__mul__", value, 1L) })
        {
            var copy = Assert.IsType<PyList>(result); Assert.Equal(new object?[] { 1L, 2L }, copy.Items);
        }
        PyObject.SetAttr(value, "note", "kept");
        Assert.Same(value, Root("__iadd__", value, value));
        Assert.Equal("[1, 2, 1, 2]", Root("__repr__", value));
        Assert.Same(value, Root("__imul__", value, 0L));
        Assert.Equal("kept", PyObject.GetAttr(value, "note"));
        Assert.Equal(0L, Root("__len__", value));
    }
    [Fact]
    public void RootStrUsesReprOverrideAndRootReprUsesPublicCycleGuard()
    {
        var cls = Class(); cls.SetAttr("__str__", Fn("str", _ => "str-only"));
        cls.SetAttr("__repr__", Fn("repr", _ => "repr-only"));
        var value = New(cls, 1L);
        Assert.Equal("repr-only", Root("__str__", value));
        Assert.Equal("[1]", Root("__repr__", value));
        var cyclic = New(); Root("append", cyclic, cyclic);
        Assert.Equal("[[...]]", Root("__repr__", cyclic));
    }
    [Fact]
    public void BothIteratorsAreLiveAndStickAfterExhaustion()
    {
        var value = New(null, 1L, 2L);
        var forward = Assert.IsAssignableFrom<IPyIterator>(Root("__iter__", value));
        Assert.True(forward.MoveNext()); Assert.Equal(1L, forward.Current());
        Root("append", value, 3L); Assert.True(forward.MoveNext()); Assert.Equal(2L, forward.Current());
        Assert.True(forward.MoveNext()); Assert.Equal(3L, forward.Current()); Assert.False(forward.MoveNext());
        Root("append", value, 4L); Assert.False(forward.MoveNext());
        var reverse = Assert.IsAssignableFrom<IPyIterator>(Root("__reversed__", value));
        Assert.True(reverse.MoveNext()); Assert.Equal(4L, reverse.Current());
        Root("__setitem__", value, 2L, 8L); Assert.True(reverse.MoveNext()); Assert.Equal(8L, reverse.Current());
        Root("clear", value); Assert.False(reverse.MoveNext()); Root("append", value, 9L); Assert.False(reverse.MoveNext());
    }
    [Fact]
    public void SearchCallbacksCanMutateAndIdentitySkipsEquality()
    {
        foreach (string name in new[] { "count", "remove", "index" })
        {
            var value = New(); int calls = 0;
            var element = User("E", "__eq__", _ => { calls++; Root("clear", value); return true; });
            Root("append", value, element);
            object? result = Root(name, value, 42L);
            Assert.Equal(1, calls); Assert.Equal(0L, Root("__len__", value));
            if (name == "count") Assert.Equal(1L, result);
            if (name == "index") Assert.Equal(0L, result);
        }
        var bomb = User("Bomb", "__eq__", _ => throw new Exception("identity must win"));
        var identities = New(null, bomb);
        Assert.Equal(true, Root("__contains__", identities, bomb));
        Assert.Equal(1L, Root("count", identities, bomb));
        Assert.Equal(true, Root("__eq__", identities, new PyList([bomb])));
        Assert.Same(PyNotImplemented.Instance, Root("__eq__", identities, new PyTuple([bomb])));
    }
    [Fact]
    public void EqualityPrioritizesStrictSubclassAndTruthTestsItsResultExactlyOnce()
    {
        var log = new System.Collections.Generic.List<string>();
        var truth = User("Truth", "__bool__", _ => { log.Add("bool"); return true; });
        var parent = (PyClass)PyOps.MakeClass("Parent", [], null);
        parent.SetAttr("__eq__", Fn("eq", _ => { log.Add("left"); return false; }));
        PyOps.FinalizeOrdinaryClass(parent);
        var child = (PyClass)PyOps.MakeClass("Child", [parent], null);
        child.SetAttr("__eq__", Fn("eq", _ => { log.Add("right"); return truth; }));
        PyOps.FinalizeOrdinaryClass(child);
        object? left = parent.Invoke([], [], []), right = child.Invoke([], [], []);
        var value = New(null, left);
        foreach (string name in new[] { "count", "__contains__", "__eq__" })
        {
            log.Clear(); object? result = Root(name, value, name == "__eq__" ? new PyList([right]) : right);
            if (name == "count") Assert.Equal(1L, result); else Assert.Equal(true, result);
            Assert.Equal(new[] { "right", "bool" }, log);
        }
        parent.SetAttr("__eq__", Fn("eq", _ => { log.Add("left"); return PyNotImplemented.Instance; }));
        child.SetAttr("__eq__", Fn("eq", _ => { log.Add("right"); return PyNotImplemented.Instance; }));
        foreach (string name in new[] { "count", "__contains__", "__eq__" })
        {
            log.Clear(); object? result = Root(name, value, name == "__eq__" ? new PyList([right]) : right);
            if (name == "count") Assert.Equal(0L, result); else Assert.Equal(false, result);
            Assert.Equal(new[] { "right", "left" }, log);
        }
        log.Clear(); Assert.Equal(true, Root("__contains__", value, left)); Assert.Empty(log);
    }
    [Fact]
    public void ElementOrderingHonorsReflectedSubclassPriorityAndDoesNotRepeatNI()
    {
        var log = new System.Collections.Generic.List<string>();
        var first = (PyClass)PyOps.MakeClass("First", [], null);
        first.SetAttr("__eq__", Fn("eq", _ => false));
        first.SetAttr("__lt__", Fn("lt", _ => { log.Add("left"); return PyNotImplemented.Instance; }));
        PyOps.FinalizeOrdinaryClass(first);
        var second = (PyClass)PyOps.MakeClass("Second", [first], null);
        second.SetAttr("__gt__", Fn("gt", _ => { log.Add("right"); return true; }));
        PyOps.FinalizeOrdinaryClass(second);
        object? a = first.Invoke([], [], []), b = second.Invoke([], [], []);
        Assert.Equal(true, Root("__lt__", New(null, a), new PyList([b])));
        Assert.Equal(new[] { "right" }, log);
        log.Clear();
        var foreign = User("Foreign", "__gt__", _ => { log.Add("foreign"); return true; });
        Assert.Equal(true, Root("__lt__", New(null, a), new PyList([foreign])));
        Assert.Equal(new[] { "left", "foreign" }, log);
        log.Clear(); second.SetAttr("__gt__", Fn("gt", _ => { log.Add("right"); return PyNotImplemented.Instance; }));
        Assert.Throws<PyTypeError>(() => Root("__lt__", New(null, a), new PyList([b])));
        Assert.Equal(new[] { "right", "left" }, log);
    }
    [Fact]
    public void NonBooleanOrderingIsExplicitlyUnsupportedWithoutTruthinessCallback()
    {
        int truthCalls = 0;
        var result = User("Result", "__bool__", _ => { truthCalls++; return true; });
        var element = User("Element", "__lt__", _ => result);
        Assert.Throws<PyNotImplementedError>(() => Root("__lt__", New(null, element), new PyList([42L])));
        Assert.Equal(0, truthCalls);
    }
    [Fact]
    public void SliceValidationPrecedesSourceAndNormalSelfSliceUsesRawCopy()
    {
        var value = New(null, 1L, 2L, 3L); int calls = 0;
        var source = User("Source", "__iter__", _ => { calls++; throw new Exception("source must not be touched"); });
        Assert.Equal("slice step cannot be zero", Assert.Throws<PyValueError>(() => Root("__setitem__", value, new PySlice(null, null, 0L), source)).Message);
        Assert.Equal(0, calls);
        Root("__setitem__", value, new PySlice(1L, 2L, null), value);
        Assert.Equal("[1, 1, 2, 3, 3]", Root("__repr__", value));
    }
    [Fact]
    public void SliceConsumesCursorHintWithoutSourceLengthAndCommitsOnlyAfterSuccess()
    {
        var log = new System.Collections.Generic.List<string>();
        var cursorClass = (PyClass)PyOps.MakeClass("Cursor", [], null);
        cursorClass.SetAttr("__iter__", Fn("iter", args => { log.Add("cursor.iter"); return args[0]; }));
        cursorClass.SetAttr("__length_hint__", Fn("hint", _ => { log.Add("cursor.hint"); return 4L; }));
        int next = 0;
        cursorClass.SetAttr("__next__", Fn("next", _ => { log.Add("next"); if (next++ == 0) return 7L; throw new PyValueError("broken"); }));
        PyOps.FinalizeOrdinaryClass(cursorClass);
        object? cursor = cursorClass.Invoke([], [], []);
        var sourceClass = (PyClass)PyOps.MakeClass("Source", [], null);
        sourceClass.SetAttr("__iter__", Fn("iter", _ => { log.Add("source.iter"); return cursor; }));
        sourceClass.SetAttr("__len__", Fn("len", _ => throw new Exception("source length must not run")));
        PyOps.FinalizeOrdinaryClass(sourceClass);
        var value = New(null, 1L, 2L);
        Assert.Throws<PyValueError>(() => Root("__setitem__", value, new PySlice(0L, 1L, null), sourceClass.Invoke([], [], [])));
        Assert.Equal(new[] { "source.iter", "cursor.iter", "cursor.hint", "next", "next" }, log);
        Assert.Equal("[1, 2]", Root("__repr__", value));
    }
    [Fact]
    public void RootRepeatAndIndexUseCorrectErrorTypesAndSingleIndexCall()
    {
        var value = New(null, 9L); int calls = 0;
        var index = User("Index", "__index__", _ => { calls++; return BigInteger.Pow(10, 100); });
        Assert.Equal("cannot fit 'Index' into an index-sized integer", Assert.Throws<PyIndexError>(() => Root("__getitem__", value, index)).Message);
        Assert.Equal(1, calls);
        Assert.Equal("'float' object cannot be interpreted as an integer", Assert.Throws<PyTypeError>(() => Root("__mul__", value, 1.5)).Message);
        Assert.Throws<PyOverflowError>(() => Root("__imul__", value, index)); Assert.Equal(2, calls);
        Assert.Equal("[9]", Root("__repr__", value));
    }
    [Fact]
    public void SortHasExplicitCallbackBoundaryAndSupportsNativeScalars()
    {
        var value = New(null, 3L, 1L, 2L);
        object? method = PyObject.GetAttr(value, "sort");
        Assert.Same(PyNone.Instance, Call(method, [], ["reverse"], [true]));
        Assert.Equal("[3, 2, 1]", Root("__repr__", value));
        Assert.Throws<PyNotImplementedError>(() => Call(method, [], ["key"], [Fn("key", _ => throw new Exception("must not call"))]));
        Assert.Equal("[3, 2, 1]", Root("__repr__", value));
    }
    [Fact]
    public void SortRejectsSurrogateStringsBeforeMutationAndSortsBmpStrings()
    {
        foreach (string unsupported in new[] { "\U00010000", "\ud800", "\udfff" })
        {
            var value = New(null, unsupported, "\ue000", "a");
            var before = Assert.IsType<PyList>(Root("copy", value));
            var error = Assert.Throws<PyNotImplementedError>(() => Root("sort", value));
            Assert.Equal("list root sort supports only BMP strings without surrogate code units", error.Message);
            var after = Assert.IsType<PyList>(Root("copy", value));
            Assert.Equal(before.Items, after.Items);
            for (int i = 0; i < before.Count; i++) Assert.Same(before.GetItem(i), after.GetItem(i));
        }
        var bmp = New(null, "\uffff", "\ue000", "中", "a");
        Assert.Same(PyNone.Instance, Root("sort", bmp));
        Assert.Equal(new object?[] { "a", "中", "\ue000", "\uffff" }, Assert.IsType<PyList>(Root("copy", bmp)).Items);
    }
    [Fact]
    public void OwnBindingErrorsCaptureContextWhileUserIndexErrorKeepsIdentityAndChain()
    {
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        long frame = PyTraceback.PushFrame("list_roots.py", "caller");
        try
        {
            PyTraceback.SetLine(31); var value = New(null, 1L);
            var own = Assert.Throws<PyTypeError>(() => Root("append", value));
            Assert.Same(outer, own.Context); Assert.Null(own.Cause); Assert.False(own.SuppressContextFlag);
            Assert.Contains(new PyStackFrame("list_roots.py", "caller", 31), own.Frames);
            var error = new PyValueError("from-index"); var cause = new PyTypeError("cause");
            error.WithCause(cause);
            PyErrors.PrepareRaise(error);
            var before = error.Frames;
            var index = User("Index", "__index__", _ => throw error);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Root("__getitem__", value, index)));
            Assert.Same(cause, error.Cause); Assert.True(error.SuppressContextFlag);
            Assert.Same(outer, error.Context); Assert.Equal(before, error.Frames);
            var element = User("Element", "__lt__", _ => throw error);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Root("__lt__", New(null, element), new PyList([42L]))));
            Assert.Same(cause, error.Cause); Assert.Same(outer, error.Context);
            Assert.Same(before, error.Frames); Assert.True(error.SuppressContextFlag);
            var reprClass = Class(); reprClass.SetAttr("__repr__", Fn("repr", _ => throw error));
            var reprValue = New(reprClass);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Root("__str__", reprValue)));
            Assert.Same(cause, error.Cause); Assert.Same(outer, error.Context);
            Assert.Same(before, error.Frames); Assert.True(error.SuppressContextFlag);
        }
        finally { PyTraceback.PopTo(frame); PyErrors.LeaveHandler(previous); }
    }
}
