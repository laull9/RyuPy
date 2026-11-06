// list真实布局、默认分配/初始化和类完成守卫；用独立断言锁住副作用与旧绑定安全。
using System;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyListLayoutTests
{
    private static PyClass Make(string name = "L", PyClass? parent = null) =>
        (PyClass)PyOps.MakeClass(name, [(object?)parent ?? PyType.Get("list")], null);
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);
    private static PyInstance New(PyClass cls, params object?[] args) => Assert.IsAssignableFrom<PyInstance>(cls.Invoke(args, [], []));
    private static PyList Raw(PyInstance instance) => (PyList)instance.GetType().GetField("Payload", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static object? Root(string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(PyType.Get("list"), name), args, [], []);

    [Fact]
    public void LayoutIsIndependentAndActualBaseIdentitySelectsIt()
    {
        var cls = Make(); Finish(cls); var first = New(cls); var second = New(cls);
        Assert.False(cls.IsException); Assert.Null(cls.BuiltinBase);
        Assert.NotSame(first, second); Assert.NotSame(Raw(first), Raw(second));
        Assert.Null(typeof(PyInstance).GetField("Payload", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.True(typeof(PyList).IsSealed);
        Assert.Same(PyType.Get("list"), PyRuntimeBases.Validate(PyType.Get("list")));
        Assert.Same(cls, PyRuntimeBases.Validate(cls));
        var ordinary = (PyClass)PyOps.MakeClass("list", [], null); Finish(ordinary);
        var child = (PyClass)PyOps.MakeClass("User", [ordinary], null); Finish(child);
        Assert.IsType<PyInstance>(child.Invoke([], [], []));
    }

    [Fact]
    public void DefaultInitCopiesSourceAndKeepsPayloadAcrossReinitialization()
    {
        var cls = Make(); Finish(cls); var source = new PyList([1L, 2L]); var value = New(cls, source);
        PyList payload = Raw(value); Assert.Equal(new object?[] { 1L, 2L }, payload.Items);
        Assert.NotSame(source, payload); PyObject.SetAttr(value, "note", "saved");
        Assert.Same(PyNone.Instance, Root("__init__", value, new PyTuple([3L])));
        Assert.Same(payload, Raw(value)); Assert.Equal(new object?[] { 3L }, payload.Items);
        Assert.Equal("saved", PyObject.GetAttr(value, "note"));
        Root("__init__", value, value); Assert.Empty(payload.Items);
    }

    [Fact]
    public void CustomInitSeesEmptyPayloadAndAllOriginalArguments()
    {
        var cls = Make(); object source = new PyList([1L]); int calls = 0;
        cls.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self", "source", "marker"], [null, null, null], 3,
            args => { calls++; Assert.Empty(Raw((PyInstance)args[0]!).Items); Assert.Same(source, args[1]);
                Assert.Equal(7L, args[2]); Raw((PyInstance)args[0]!).Append("custom"); return PyNone.Instance; }));
        Finish(cls); var value = Assert.IsAssignableFrom<PyInstance>(cls.Invoke([source], ["marker"], [7L]));
        Assert.Equal(1, calls); Assert.Equal(new object?[] { "custom" }, Raw(value).Items);
        cls.SetAttr("__init__", Method("bad", _ => 9L));
        Assert.Equal("__init__() should return None, not 'int'", Assert.Throws<PyTypeError>(() => New(cls)).Message);
    }

    [Fact]
    public void RootNewIgnoresExtraArgumentsAndNeverRunsInit()
    {
        var cls = Make(); cls.SetAttr("__init__", Method("init", _ => throw new Exception("must not execute"))); Finish(cls);
        object? allocator = PyObject.GetAttr(PyType.Get("list"), "__new__");
        var value = Assert.IsAssignableFrom<PyInstance>(PyOps.CallValue(allocator, [cls, 1L, 2L], ["arbitrary"], [3L]));
        Assert.Same(cls, value.Class); Assert.Empty(Raw(value).Items);
        Assert.Empty(Assert.IsType<PyList>(PyOps.CallValue(allocator, [PyType.Get("list"), 1L], ["x"], [2L])).Items);
        Assert.Equal("list.__new__(): not enough arguments", Assert.Throws<PyTypeError>(() => Root("__new__")).Message);
        Assert.Equal("list.__new__(dict): dict is not a subtype of list", Assert.Throws<PyTypeError>(() => Root("__new__", PyType.Get("dict"))).Message);
        Assert.Equal("list.__new__(X): X is not a type object (int)", Assert.Throws<PyTypeError>(() => Root("__new__", 1L)).Message);
    }

    [Fact]
    public void InitBindingFailurePreservesPayloadButBadSourceClearsIt()
    {
        var cls = Make(); Finish(cls); var value = New(cls, new PyList([1L]));
        object? init = PyObject.GetAttr(PyType.Get("list"), "__init__");
        var error = Assert.Throws<PyTypeError>(() => PyOps.CallValue(init, [value, 2L, 3L], ["bad"], [4L]));
        Assert.Equal("list() takes no keyword arguments", error.Message); Assert.Equal(1, Raw(value).Count);
        Assert.Equal("list expected at most 1 argument, got 2", Assert.Throws<PyTypeError>(() => Root("__init__", value, 2L, 3L)).Message);
        Assert.Equal(1, Raw(value).Count);
        Assert.Throws<PyTypeError>(() => Root("__init__", value, 2L)); Assert.Empty(Raw(value).Items);
    }

    [Fact]
    public void DescendantsObserveLateInitSetAndDeleteThroughGlobalVersion()
    {
        var parent = Make(); Finish(parent); var child = Make("Child", parent); Finish(child);
        Assert.Same(child, New(child).Class); int calls = 0;
        parent.SetAttr("__init__", Method("init", a => { calls++; Raw((PyInstance)a[0]!).Append(9L); return PyNone.Instance; }));
        Assert.Equal(9L, Raw(New(child)).GetItem(0L)); Assert.Equal(1, calls);
        Assert.True(parent.DeleteAttr("__init__")); Assert.Empty(Raw(New(child)).Items);
        Assert.Equal(2L, Raw(New(child, new PyList([2L]))).GetItem(0L));
    }

    [Fact]
    public void UnreadyOrFailedClassDoesNotPublishReadyState()
    {
        var cls = Make(); Assert.Throws<PyNotImplementedError>(() => New(cls));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(cls));
        cls.SetAttr("__iter__", Method("iter", _ => PyNone.Instance)); long version = cls.Version;
        Assert.Throws<PyNotImplementedError>(() => Finish(cls)); Assert.Equal(version, cls.Version);
        Assert.Throws<PyNotImplementedError>(() => New(cls)); Assert.True(cls.DeleteAttr("__iter__"));
        Finish(cls); Assert.Empty(Raw(New(cls)).Items); Assert.Throws<PyNotImplementedError>(() => Finish(cls));
    }

    [Fact]
    public void UnsupportedDefinitionAndLateSlotsFailAtomically()
    {
        foreach (string slot in new[] { "__new__", "__slots__", "__getattribute__", "__len__", "__bool__", "__iter__", "__getitem__", "__setitem__", "__delitem__", "__contains__", "__iadd__", "__imul__", "__eq__", "__hash__", "__rsub__", "__reversed__" })
        {
            var cls = Make(); cls.SetAttr(slot, PyNone.Instance); long version = cls.Version;
            Assert.Equal($"list subclass slot {slot} is not supported", Assert.Throws<PyNotImplementedError>(() => Finish(cls)).Message);
            Assert.Equal(version, cls.Version); cls.DeleteAttr(slot); Finish(cls); version = cls.Version;
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr(slot, PyNone.Instance));
            Assert.Throws<PyNotImplementedError>(() => cls.DeleteAttr(slot)); Assert.Equal(version, cls.Version);
        }
    }

    [Fact]
    public void BusinessDundersAndPropertiesAreAllowed()
    {
        var cls = Make(); object marker = new PyDict(); cls.SetAttr("__application_map__", marker);
        cls.SetAttr("size", new PyProperty(Method("size", _ => 42L))); Finish(cls); var value = New(cls);
        Assert.Same(marker, PyObject.GetAttr(value, "__application_map__")); Assert.Equal(42L, PyObject.GetAttr(value, "size"));
        cls.SetAttr("__application_map__", 7L); Assert.Equal(7L, PyObject.GetAttr(value, "__application_map__"));
        Assert.True(cls.DeleteAttr("__application_map__"));
    }

    [Fact]
    public void UnsafeAllocationAndMixedDataclassAreRejectedBeforeMutation()
    {
        var cls = Make(); Finish(cls); long version = cls.Version;
        Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(cls, false)); Assert.Equal(version, cls.Version);
        object? allocator = PyObject.GetAttr(PyBaseObject.Type, "__new__");
        Assert.Equal("object.__new__(L) is not safe, use L.__new__()", Assert.Throws<PyTypeError>(() => PyOps.CallValue(allocator, [cls], [], [])).Message);
    }

    [Fact]
    public void MetadataAndDefaultHashRemainReal()
    {
        var cls = Make(); Finish(cls); var value = New(cls);
        Assert.Same(cls, PyObject.GetAttr(value, "__class__")); Assert.Same(PyType.Get("list"), PyObject.GetAttr(cls, "__base__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(cls, "__hash__")); Assert.Same(PyNone.Instance, PyObject.GetAttr(value, "__hash__"));
        foreach (object target in new object[] { cls, value })
        {
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(target, "__dict__"));
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(target, "__dict__", new PyDict()));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(target, "__dict__"));
        }
    }

    [Fact]
    public void RootInitFailureKeepsStreamedPrefixAndUserExceptionChain()
    {
        var cls = Make(); Finish(cls); var value = New(cls, new PyList([0L]));
        var failure = new PyValueError("stream"); var cause = new PyKeyError("cause");
        var context = new PyRuntimeError("original"); PyErrors.WithCause(failure, cause); failure.Context = context;
        var sourceClass = new PyClass("Source"); var source = new PyInstance(sourceClass); int next = 0;
        sourceClass.SetAttr("__iter__", Method("iter", _ => source));
        sourceClass.SetAttr("__next__", Method("next", _ => next++ == 0 ? 7L : throw failure));
        var ambient = new PyTypeError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            Assert.Same(failure, Assert.Throws<PyValueError>(() => Root("__init__", value, source)));
            Assert.Equal(new object?[] { 7L }, Raw(value).Items);
            Assert.Same(cause, failure.Cause); Assert.Same(context, failure.Context); Assert.Same(ambient, PyErrors.Current);
            cls.SetAttr("__init__", Method("init", _ => throw failure));
            Assert.Same(failure, Assert.Throws<PyValueError>(() => New(cls)));
            Assert.Same(cause, failure.Cause); Assert.Same(context, failure.Context);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }


    [Fact]
    public void MissingAttributeFallbackDoesNotStealDefaultListProtocols()
    {
        var cls = Make(); int fallbacks = 0;
        cls.SetAttr("__getattr__", Method("getattr", args => { fallbacks++; return "missing:" + (string)args[1]!; }));
        Finish(cls); var value = New(cls, new PyList([1L]));
        Assert.Equal("missing:custom", PyObject.GetAttr(value, "custom"));
        PyOps.CallMethod(value, "append", [2L], [], []);
        Assert.Equal(2L, PyOps.CallBuiltin("len", [value], [], []));
        Assert.Equal(1, fallbacks); Assert.Equal(new object?[] { 1L, 2L }, Raw(value).Items);
    }


    [Fact]
    public void DataclassRejectsListSubclassDefaultBeforeActivatingLayout()
    {
        var listClass = Make(); Finish(listClass); var shared = New(listClass, new PyList([1L]));
        var record = (PyClass)PyOps.MakeClass("Record", [], null);
        record.SetAttr("items", shared); PyOps.DeclareDataclass(record, ["items"], false); Finish(record);
        long version = record.Version;
        var error = Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(record, false));
        Assert.Equal("mutable default <class 'L'> for field items is not allowed: use default_factory", error.Message);
        Assert.Equal(version, record.Version); Assert.Same(shared, record.Attributes["items"]);
        foreach (string generated in new[] { "__init__", "__repr__", "__eq__", "__hash__", "__dataclass_fields__" })
            Assert.False(record.Attributes.ContainsKey(generated));
        Assert.False((bool)typeof(PyClass).GetProperty("DataclassActive", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(record)!);
    }

    [Fact]
    public void DataclassAcceptsListSubclassFactoryAndOrdinaryListNamedDefault()
    {
        var listClass = Make(); Finish(listClass); int calls = 0;
        var factory = PyNativeFunctions.Create("factory", [], [], 0, _ => { calls++; return New(listClass, new PyList([1L])); });
        var record = new PyClass("FactoryRecord");
        record.SetAttr("items", PyOps.CallValue(PyDataclasses.CreateAttributes()["field"], [], ["default_factory"], [factory]));
        PyOps.DeclareDataclass(record, ["items"], false); Finish(record); PyOps.ApplyDataclass(record, false);
        Assert.Equal(0, calls);
        object? first = PyObject.GetAttr(record.Invoke([], [], []), "items");
        object? second = PyObject.GetAttr(record.Invoke([], [], []), "items");
        Assert.Equal(2, calls); Assert.NotSame(first, second); Assert.Same(listClass, Assert.IsAssignableFrom<PyInstance>(first).Class);
        var ordinary = (PyClass)PyOps.MakeClass("list", [], null); Finish(ordinary); var shared = new PyInstance(ordinary);
        var named = new PyClass("NamedRecord"); named.SetAttr("item", shared);
        PyOps.DeclareDataclass(named, ["item"], false); Finish(named); PyOps.ApplyDataclass(named, false);
        Assert.Same(shared, PyObject.GetAttr(named.Invoke([], [], []), "item"));
    }


    [Fact]
    public void ListClassReprAndDataclassErrorReadRawOwnMetadata()
    {
        var trapClass = new PyClass("Trap");
        trapClass.SetAttr("__str__", Method("str", _ => throw new Exception("must not convert module")));
        trapClass.SetAttr("__repr__", Method("repr", _ => throw new Exception("must not format module")));
        object trap = new PyInstance(trapClass);
        var textClass = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null);
        textClass.SetAttr("__str__", Method("str", _ => throw new Exception("must not convert string module")));
        Finish(textClass); object textModule = textClass.Invoke(["pkg.text"], [], [])!;
        foreach ((object? module, string expected) in new (object?, string)[]
        {
            ("__main__", "<class '__main__.Outer.Inner'>"), ("pkg.mod", "<class 'pkg.mod.Outer.Inner'>"),
            ("builtins", "<class 'L'>"), ("", "<class '.Outer.Inner'>"), (PyNone.Instance, "<class 'L'>"),
            (17L, "<class 'L'>"), (trap, "<class 'L'>"), (textModule, "<class 'pkg.text.Outer.Inner'>"),
        })
        {
            var cls = Make(); cls.SetAttr("__module__", module); cls.SetAttr("__qualname__", "Outer.Inner"); Finish(cls);
            Assert.Equal(expected, cls.ToString()); Assert.Equal(expected, PyRepr.Repr(cls));
            var record = new PyClass("Record"); record.SetAttr("xs", New(cls, new PyList([1L])));
            PyOps.DeclareDataclass(record, ["xs"], false);
            Assert.Equal($"mutable default {expected} for field xs is not allowed: use default_factory",
                Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(record, false)).Message);
        }
        // 未捕获定义帧的内部类保留Name后备，不继承基类module/qualname。
        var parent = Make(); parent.SetAttr("__module__", "pkg.parent"); parent.SetAttr("__qualname__", "Outer.Parent"); Finish(parent);
        var child = Make("Child", parent); Finish(child); Assert.Equal("<class 'Child'>", child.ToString());
    }

}
