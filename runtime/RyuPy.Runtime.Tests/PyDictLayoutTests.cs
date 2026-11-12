// dict独立布局的构造、冷完成、晚改与原子拒绝；所有公开身份仍是wrapper。
using System;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictLayoutTests
{
    private static PyClass Make(string name = "D", PyClass? parent = null) =>
        (PyClass)PyOps.MakeClass(name, [(object?)parent ?? PyType.Get("dict")], null);
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);
    private static PyInstance New(PyClass cls, params object?[] args) => Assert.IsAssignableFrom<PyInstance>(cls.Invoke(args, [], []));
    private static PyDict Raw(PyInstance instance) => (PyDict)instance.GetType().GetField("Payload", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static object? Root(string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(PyType.Get("dict"), name), args, [], []);

    [Fact]
    public void WrapperAndPayloadAreIndependentAndBaseUsesIdentity()
    {
        var cls = Make(); Finish(cls); var first = New(cls); var second = New(cls);
        Assert.False(cls.IsException); Assert.Null(cls.BuiltinBase);
        Assert.NotSame(first, second); Assert.NotSame(Raw(first), Raw(second));
        Assert.Null(typeof(PyInstance).GetField("Payload", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.True(typeof(PyDict).IsSealed);
        Assert.Same(PyType.Get("dict"), PyRuntimeBases.Validate(PyType.Get("dict")));
        Assert.Same(cls, PyRuntimeBases.Validate(cls));
        var ordinary = (PyClass)PyOps.MakeClass("dict", [], null); Finish(ordinary);
        var child = (PyClass)PyOps.MakeClass("User", [ordinary], null); Finish(child);
        Assert.IsType<PyInstance>(child.Invoke([], [], []));
    }

    [Fact]
    public void ReinitializationUpdatesSamePayloadAndPreservesAttributes()
    {
        var cls = Make(); Finish(cls); var source = new PyDict(); source.Set("old", 1L);
        var value = New(cls, source); var payload = Raw(value);
        Assert.NotSame(source, payload); PyObject.SetAttr(value, "note", "saved");
        var extra = new PyDict(); extra.Set("new", 2L);
        Assert.Same(PyNone.Instance, Root("__init__", value, extra));
        Assert.Same(payload, Raw(value)); Assert.Equal(1L, payload.Get("old")); Assert.Equal(2L, payload.Get("new"));
        Assert.Equal("saved", PyObject.GetAttr(value, "note"));
        Root("__init__", value, value); Assert.Equal(2, payload.Count);
        Root("__init__", value); Assert.Equal(2, payload.Count);
        Assert.Throws<PyTypeError>(() => Root("__init__", value, extra, extra)); Assert.Equal(2, payload.Count);
    }

    [Fact]
    public void CustomInitReceivesAllArgumentsBeforeDefaultBinding()
    {
        var cls = Make(); int calls = 0;
        cls.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self", "one", "two", "marker"], [null, null, null, null], 4,
            args => { calls++; var raw = Raw((PyInstance)args[0]!); Assert.Equal(0, raw.Count);
                Assert.Equal(1L, args[1]); Assert.Equal(2L, args[2]); Assert.Equal(3L, args[3]);
                raw.Set("custom", 9L); return PyNone.Instance; }));
        Finish(cls); var value = Assert.IsAssignableFrom<PyInstance>(cls.Invoke([1L, 2L], ["marker"], [3L]));
        Assert.Equal(1, calls); Assert.Equal(9L, Raw(value).Get("custom"));
        cls.SetAttr("__init__", Method("bad", _ => 9L));
        Assert.Equal("__init__() should return None, not 'int'", Assert.Throws<PyTypeError>(() => New(cls)).Message);
    }

    [Fact]
    public void RootAllocatorNeverRunsInitAndUnsafeObjectAllocatorRejects()
    {
        var cls = Make(); cls.SetAttr("__init__", Method("init", _ => throw new Exception("must not run"))); Finish(cls);
        var allocated = Assert.IsAssignableFrom<PyInstance>(Root("__new__", cls, 1L, 2L));
        Assert.Same(cls, allocated.Class); Assert.Equal(0, Raw(allocated).Count);
        Assert.IsType<PyDict>(Root("__new__", PyType.Get("dict"), 1L));
        Assert.Throws<PyTypeError>(() => Root("__new__"));
        Assert.Throws<PyTypeError>(() => Root("__new__", PyType.Get("list")));
        object? allocator = PyObject.GetAttr(PyBaseObject.Type, "__new__");
        Assert.Equal("object.__new__(D) is not safe, use D.__new__()", Assert.Throws<PyTypeError>(() => PyOps.CallValue(allocator, [cls], [], [])).Message);
    }

    [Fact]
    public void LateInitAndDeleteInvalidateDescendantConstruction()
    {
        var parent = Make(); Finish(parent); var child = Make("Child", parent); Finish(child);
        Assert.Same(child, New(child).Class);
        parent.SetAttr("__init__", Method("init", a => { Raw((PyInstance)a[0]!).Set("late", 7L); return PyNone.Instance; }));
        Assert.Equal(7L, Raw(New(child)).Get("late"));
        Assert.True(parent.DeleteAttr("__init__")); Assert.Equal(0, Raw(New(child)).Count);
    }

    [Fact]
    public void FailedFinalizationAndLateSlotsAreAtomic()
    {
        foreach (string slot in new[] { "__new__", "__slots__", "__getattribute__", "__len__", "__bool__", "__getitem__", "__contains__", "__eq__", "__hash__", "__rsub__" })
        {
            var cls = Make(); Assert.Throws<PyNotImplementedError>(() => New(cls));
            Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(cls));
            cls.SetAttr(slot, PyNone.Instance); long version = cls.Version;
            Assert.Equal($"dict subclass slot {slot} is not supported", Assert.Throws<PyNotImplementedError>(() => Finish(cls)).Message);
            Assert.Equal(version, cls.Version); Assert.Throws<PyNotImplementedError>(() => New(cls));
            cls.DeleteAttr(slot); Finish(cls); version = cls.Version;
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr(slot, PyNone.Instance));
            Assert.Throws<PyNotImplementedError>(() => cls.DeleteAttr(slot)); Assert.Equal(version, cls.Version);
        }
    }

    [Fact]
    public void SupportedSlotsAllowLateReplacementButStillRejectCustomDescriptors()
    {
        foreach (string slot in new[] { "__setitem__", "__delitem__", "__iter__", "__reversed__", "__or__", "__ror__", "__ior__" })
        {
            var cls = Make(); cls.SetAttr(slot, Method(slot, _ => PyNone.Instance)); Finish(cls);
            cls.SetAttr(slot, Method(slot, _ => 42L));
            long version = cls.Version;
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr(slot, new PyProperty(Method("get", _ => 1L))));
            Assert.Equal(version, cls.Version);
            Assert.True(cls.DeleteAttr(slot));
            // 认证根描述符别名是可绑定的，不能据此放开任意 native callable。
            cls.SetAttr(slot, PyObject.GetAttr(PyType.Get("dict"), slot));
            Assert.True(cls.DeleteAttr(slot));
        }
    }

    [Fact]
    public void BusinessMethodsPropertiesAndMissingCanBeDeclaredAndChanged()
    {
        var cls = Make(); cls.SetAttr("__application_map__", 8L);
        cls.SetAttr("size", new PyProperty(Method("size", _ => 42L)));
        cls.SetAttr("__missing__", Method("missing", _ => "fallback")); Finish(cls); var value = New(cls);
        Assert.Equal(8L, PyObject.GetAttr(value, "__application_map__")); Assert.Equal(42L, PyObject.GetAttr(value, "size"));
        Assert.Equal("fallback", Root("__getitem__", value, "absent"));
        cls.SetAttr("__missing__", Method("missing", _ => "late")); Assert.Equal("late", Root("__getitem__", value, "absent"));
        Assert.True(cls.DeleteAttr("__missing__")); Assert.Throws<PyKeyError>(() => Root("__getitem__", value, "absent"));
    }

    [Fact]
    public void ClassFromkeysUsesActualClassIncludingSubclassAndCustomInit()
    {
        var parent = Make(); parent.SetAttr("__init__", Method("init", a => { Raw((PyInstance)a[0]!).Set("initial", 0L); return PyNone.Instance; }));
        Finish(parent); var child = Make("Child", parent); Finish(child);
        foreach (object receiver in new object[] { child, New(child) })
        {
            var result = Assert.IsAssignableFrom<PyInstance>(PyOps.CallValue(PyObject.GetAttr(receiver, "fromkeys"), [new PyList(["key"]), 7L], [], []));
            Assert.Same(child, result.Class); Assert.Equal(0L, Raw(result).Get("initial")); Assert.Equal(7L, Raw(result).Get("key"));
        }
    }

    [Fact]
    public void DataclassRejectsLayoutAndMutableDefaultBeforePublishingGeneratedMethods()
    {
        var cls = Make(); Finish(cls); long version = cls.Version;
        Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(cls, false)); Assert.Equal(version, cls.Version);
        var shared = New(cls); var record = new PyClass("Record"); record.SetAttr("items", shared);
        PyOps.DeclareDataclass(record, ["items"], false); Finish(record); version = record.Version;
        Assert.Equal("mutable default <class 'D'> for field items is not allowed: use default_factory",
            Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(record, false)).Message);
        Assert.Equal(version, record.Version); Assert.Same(shared, record.Attributes["items"]);
        Assert.False(record.Attributes.ContainsKey("__init__")); Assert.False(record.Attributes.ContainsKey("__dataclass_fields__"));
    }
    [Fact]
    public void MetadataMutationAndMixedLayoutsAreRejected()
    {
        var cls = Make(); Finish(cls); var value = New(cls);
        Assert.Same(cls, PyObject.GetAttr(value, "__class__"));
        Assert.Same(PyType.Get("dict"), PyObject.GetAttr(cls, "__base__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(cls, "__hash__"));
        foreach (object target in new object[] { cls, value })
        {
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(target, "__dict__"));
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(target, "__dict__", new PyDict()));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(target, "__dict__"));
        }
        // 内部构造者也不能让任一finalizer接纳冲突布局；真实Python多基类另由Make拒绝。
        foreach (string layout in new[] { "HasListLayout", "HasStringLayout" })
        {
            var mixed = Make();
            typeof(PyClass).GetProperty(layout, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(mixed, true);
            long version = mixed.Version;
            Assert.Throws<PyNotImplementedError>(() => Finish(mixed)); Assert.Equal(version, mixed.Version);
        }
    }

    [Fact]
    public void NonCallableMissingFailsAtInvocationAndUserInitExceptionIsNotRewritten()
    {
        var cls = Make(); cls.SetAttr("__missing__", PyNone.Instance); Finish(cls);
        Assert.Throws<PyTypeError>(() => Root("__getitem__", New(cls), "missing"));
        var failure = new PyValueError("user"); var cause = new PyKeyError("cause");
        var context = new PyRuntimeError("context"); PyErrors.WithCause(failure, cause); failure.Context = context;
        cls.SetAttr("__init__", Method("init", _ => throw failure));
        var ambient = new PyTypeError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            Assert.Same(failure, Assert.Throws<PyValueError>(() => New(cls)));
            Assert.Same(cause, failure.Cause); Assert.Same(context, failure.Context); Assert.Same(ambient, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

}
