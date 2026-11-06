// object A 的有限真实类型/实例合同；不把普通类的 __new__ 协议混入首切。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBaseObjectTests
{
    private static PyType Root => PyType.Get("object");
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static object? Method(object? target, string name, params object?[] args) =>
        Call(PyObject.GetAttr(target, name), args);
    private static bool Instance(object? value, object? cls) =>
        (bool)PyOps.CallBuiltin("isinstance", [value, cls], [], [])!;
    private static bool Subclass(object? value, object? cls) =>
        (bool)PyOps.CallBuiltin("issubclass", [value, cls], [], [])!;

    [Fact]
    public void DirectAndSavedConstructorCreateDistinctRealRootInstances()
    {
        object? a = PyOps.CallBuiltin("object", [], [], []), b = Call(Root);
        Assert.IsType<PyBaseObject>(a); Assert.IsType<PyBaseObject>(b); Assert.NotSame(a, b);
        Assert.Same(Root, PyOps.TypeObject("object")); Assert.Same(Root, PyObject.TypeOf(a));
        Assert.Same(Root, PyObject.GetAttr(a, "__class__"));
        Assert.Same(PyType.Get("type"), PyObject.TypeOf(Root));
        Assert.True((bool)PyOps.CallBuiltin("callable", [Root], [], [])!);
    }

    [Fact]
    public void RootMetadataIsRealAndNotAnInventedUserClass()
    {
        Assert.Equal("object", PyObject.GetAttr(Root, "__name__"));
        Assert.Equal("builtins", PyObject.GetAttr(Root, "__module__"));
        Assert.Same(PyType.Get("type"), PyObject.GetAttr(Root, "__class__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(Root, "__base__"));
        Assert.Empty(Assert.IsType<PyTuple>(PyObject.GetAttr(Root, "__bases__")).Items);
        Assert.Same(Root, Assert.Single(Assert.IsType<PyTuple>(PyObject.GetAttr(Root, "__mro__")).Items));
        Assert.Equal("<class 'object'>", PyRepr.Repr(Root));
    }

    [Fact]
    public void SentinelHasStableHashAndIdentityReprWithoutWritableAttributes()
    {
        object a = Call(Root)!;
        Assert.StartsWith("<object object at 0x", PyRepr.Repr(a));
        Assert.EndsWith(">", PyRepr.Repr(a)); Assert.Equal(PyRepr.Repr(a), PyRepr.Str(a));
        Assert.Equal(PyHash.Of(a), PyHash.Of(a));
        Assert.Equal(PyHash.Of(a), Method(a, "__hash__"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(a, "__dict__"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(a, "value", 1L));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(a, "value"));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(a, "__class__", Root));
        Assert.Throws<PyTypeError>(() => PyObject.DelAttr(a, "__class__"));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(Root, "value", 1L));
        Assert.Throws<PyTypeError>(() => PyObject.DelAttr(Root, "__module__"));
        Assert.Equal("builtins", PyObject.GetAttr(Root, "__module__"));
        var values = new PyDict(); values.Set(a, PyNone.Instance);
        Assert.Same(PyNone.Instance, values.Get(a));
    }

    [Fact]
    public void ComparisonRetainsNotImplementedAndRightHandProtocol()
    {
        object a = Call(Root)!, b = Call(Root)!;
        Assert.Equal(true, Method(a, "__eq__", a)); Assert.Equal(false, Method(a, "__ne__", a));
        Assert.Same(PyNotImplemented.Instance, Method(a, "__eq__", b));
        Assert.Same(PyNotImplemented.Instance, Method(a, "__ne__", b));
        Assert.Same(PyNotImplemented.Instance, Method(Root, "__eq__", a, b));
        Assert.True(PyOps.Compare(PyOps.Eq, a, a)); Assert.True(PyOps.Compare(PyOps.Ne, a, b));
        Assert.Throws<PyTypeError>(() => PyOps.Compare(PyOps.Lt, a, b));
        int calls = 0; var cls = new PyClass("Right");
        cls.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2,
            args => { calls++; Assert.Same(a, args[1]); return true; }));
        var right = new PyInstance(cls);
        Assert.True(PyOps.ValuesEqual(a, right)); Assert.Equal(1, calls);
        Assert.Equal(false, Method(Root, "__ne__", right, a)); Assert.Equal(2, calls);
    }

    [Fact]
    public void ExplicitNeDoesNotReplaceAlreadyPreparedCallbackError()
    {
        var error = new PyValueError("callback");
        var active = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(active);
        long depth = PyTraceback.PushFrame("object-test.py", "caller");
        PyStackFrame[]? originalFrames = null;
        var cls = new PyClass("Raises");
        cls.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2, _ =>
        {
            long inner = PyTraceback.PushFrame("callback.py", "__eq__");
            try
            {
                PyErrors.PrepareRaise(error); originalFrames = error.Frames;
                throw error;
            }
            finally { PyTraceback.PopTo(inner); }
        }));
        try
        {
            Assert.Same(error, Assert.Throws<PyValueError>(() => Method(Root, "__ne__", new PyInstance(cls), Call(Root))));
            Assert.Same(originalFrames, error.Frames); Assert.Same(active, error.Context);
            Assert.Contains(error.Frames, f => f.File == "callback.py");
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void ConstructorErrorsPreserveCallerAndActiveContext()
    {
        var active = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(active);
        long depth = PyTraceback.PushFrame("object-test.py", "caller"); PyTraceback.SetLine(12);
        try
        {
            foreach (Func<object?> action in new Func<object?>[] {
                () => Call(Root, 1L), () => PyOps.CallValue(Root, [], ["value"], [1L]),
                () => Method(Root, "__new__"), () => Method(Root, "__init__"),
                () => Method(Call(Root), "__eq__") })
            {
                PyTypeError error = Assert.Throws<PyTypeError>(() => action());
                Assert.Same(active, error.Context);
                Assert.Contains(error.Frames, f => f.File == "object-test.py" && f.Line == 12);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void ActualMethodAndPicDispatchUseOnlyCanonicalRootType()
    {
        object a = Call(Root)!, b = Call(Root)!;
        Assert.Same(PyNotImplemented.Instance, PyOps.CallMethod(Root, "__eq__", [a, b], [], []));
        Assert.Same(PyNotImplemented.Instance, PyPic.CallMethod2(0, Root, "__eq__", a, b));
        Assert.IsType<PyBaseObject>(PyPic.CallMethod1(0, Root, "__new__", Root));
        Assert.Throws<PyTypeError>(() => PyPic.CallMethod0(0, Root, "__new__"));
        var plain = new PyClass("Plain"); int initCalls = 0;
        plain.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self"], [null], 1,
            _ => { initCalls++; return PyNone.Instance; }));
        Assert.Same(plain, Assert.IsType<PyInstance>(PyOps.CallMethod(Root, "__new__", [plain], [], [])).Class);
        Assert.Same(plain, Assert.IsType<PyInstance>(PyPic.CallMethod1(0, Root, "__new__", plain)).Class);
        Assert.Equal(0, initCalls);
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(Root, "__new__", [Root], ["extra"], [1L]));
        // ADR0104开放真实list allocator：缺cls由其binder报错，其它未接根类型仍拒绝。
        var listError = Assert.Throws<PyTypeError>(() => PyOps.CallMethod(PyType.Get("list"), "__new__", [], [], []));
        Assert.Equal("list.__new__(): not enough arguments", listError.Message);
        // ADR0106加入dict根allocator，缺cls现在是真实签名错误；tuple仍未准入。
        var dictError = Assert.Throws<PyTypeError>(() => PyOps.CallMethod(PyType.Get("dict"), "__new__", [], [], []));
        Assert.Equal("dict.__new__(): not enough arguments", dictError.Message);
        Assert.Throws<PyAttributeError>(() => PyOps.CallMethod(PyType.Get("tuple"), "__new__", [], [], []));
    }

    [Fact]
    public void NewAndInitSupportBareObjectWithoutInventingDerivedAllocation()
    {
        object a = Method(Root, "__new__", Root)!;
        Assert.IsType<PyBaseObject>(a); Assert.NotSame(a, Call(Root));
        Assert.Same(PyNone.Instance, Method(Root, "__init__", a));
        Assert.Same(PyNone.Instance, Method(a, "__init__"));
        Assert.Same(PyNone.Instance, Method(Root, "__init__", 1L));
        Assert.Throws<PyTypeError>(() => Method(Root, "__new__", 1L));
        Assert.Throws<PyTypeError>(() => Method(Root, "__new__", PyType.Get("int")));
        Assert.Throws<PyTypeError>(() => Method(Root, "__new__", Root, 1L));
        Assert.Throws<PyTypeError>(() => Method(Root, "__init__", a, 1L));
        var plain = new PyClass("Plain");
        Assert.Same(plain, Assert.IsType<PyInstance>(Method(Root, "__new__", plain)).Class);
        int calls = 0; var withInit = new PyClass("WithInit");
        withInit.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self"], [null], 1,
            _ => { calls++; return PyNone.Instance; }));
        Assert.Same(withInit, Assert.IsType<PyInstance>(Method(Root, "__new__", withInit)).Class);
        Assert.Equal(0, calls);
        Assert.IsType<PyInstance>(plain.Invoke([], [], []));
        Assert.Throws<PyTypeError>(() => plain.Invoke([1L], [], []));
    }

    [Fact]
    public void RootInstanceChecksApplyToAllExistingValueRepresentations()
    {
        var plain = new PyClass("Plain");
        object?[] values = [Call(Root), 1L, true, PyNone.Instance, "text", new PyList([]),
            new PyDict(), plain, new PyInstance(plain), Root, PyType.Get("type"),
            PyNativeFunctions.Create("f", [], [], 0, _ => PyNone.Instance), new PyValueError("x")];
        foreach (object? value in values) Assert.True(Instance(value, Root));
        Assert.False(Instance(Call(Root), plain));
        Assert.Throws<PyTypeError>(() => Instance(1L, 42L));
    }

    [Fact]
    public void TupleShortCircuitAndTypedDictRejectionAreNotReordered()
    {
        Assert.True(Instance(1L, new PyTuple([Root, 42L])));
        Assert.Throws<PyTypeError>(() => Instance(1L, new PyTuple([42L, Root])));
        Assert.False(Instance(1L, new PyTuple([])));
        Assert.True(Subclass(PyType.Get("int"), new PyTuple([Root, 42L])));
        Assert.Throws<PyTypeError>(() => Subclass(PyType.Get("int"), new PyTuple([42L, Root])));
        Assert.False(Subclass(42L, new PyTuple([]))); Assert.Throws<PyTypeError>(() => Subclass(42L, Root));
        object typed = PyTypedDict.Finish(PyTypedDict.Begin("Record", PyTypedDict.Factory, true, "test"));
        Assert.Throws<PyTypeError>(() => Instance(new PyDict(), typed));
        Assert.Throws<PyTypeError>(() => Subclass(new PyClass("Plain"), typed));
        Assert.True(Instance(typed, Root)); Assert.True(Subclass(typed, Root));
    }

    [Fact]
    public void OrdinaryAndExplicitRootInheritanceKeepActualIdentity()
    {
        var plain = (PyClass)PyOps.MakeClass("Plain", [], null);
        var direct = (PyClass)PyOps.MakeClass("Direct", [PyRuntimeBases.Validate(Root)], null);
        var child = (PyClass)PyOps.MakeClass("Child", [direct], null);
        Assert.Same(Root, PyRuntimeBases.Validate(Root));
        // list/dict均按canonical身份准入，仍不把任意内建布局当普通基类。
        Assert.Same(PyType.Get("list"), PyRuntimeBases.Validate(PyType.Get("list")));
        Assert.Same(PyType.Get("dict"), PyRuntimeBases.Validate(PyType.Get("dict")));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(PyType.Get("tuple")));
        Assert.Same(Root, Assert.Single(Assert.IsType<PyTuple>(PyObject.GetAttr(plain, "__bases__")).Items));
        Assert.Same(Root, Assert.Single(Assert.IsType<PyTuple>(PyObject.GetAttr(direct, "__bases__")).Items));
        Assert.Same(direct, Assert.Single(Assert.IsType<PyTuple>(PyObject.GetAttr(child, "__bases__")).Items));
        Assert.Same(Root, PyObject.GetAttr(plain, "__base__")); Assert.Same(direct, PyObject.GetAttr(child, "__base__"));
        foreach (object cls in new object[] { plain, direct, child, Root, PyType.Get("int"), PyType.Get("type") })
            Assert.True(Subclass(cls, Root));
        Assert.True(Subclass(child, direct)); Assert.False(Subclass(direct, child));
        object instance = child.Invoke([], [], [])!; Assert.True(Instance(instance, direct));
        PyObject.SetAttr(instance, "sentinel", Root); Assert.Same(Root, PyObject.GetAttr(instance, "sentinel"));
    }
}
