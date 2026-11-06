// 阶段A内部载体与发布守卫；不把尚未闭合的对象转换/序列方法计作字符串支持。
using System;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringLayoutTests
{
    private static PyClass Make(string name = "S", PyClass? parent = null) =>
        (PyClass)PyOps.MakeClass(name, [(object?)parent ?? PyType.Get("str")], null);
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);
    private static PyInstance New(PyClass cls, params object?[] args) => Assert.IsAssignableFrom<PyInstance>(cls.Invoke(args, [], []));
    private static string Raw(PyInstance instance) => (string)instance.GetType().GetField("Value", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);

    [Fact]
    public void LayoutIsRealStringClassAndNotAnExceptionName()
    {
        var cls = Make(); Finish(cls); var value = New(cls, "abc");
        Assert.False(cls.IsException); Assert.Null(cls.BuiltinBase);
        Assert.Same(PyType.Get("str"), PyRuntimeBases.Validate(PyType.Get("str"))); Assert.Same(cls, PyRuntimeBases.Validate(cls));
        Assert.Same(cls, PyObject.TypeOf(value)); Assert.Equal(true, Builtin("isinstance", value, PyType.Get("str")));
        Assert.Equal(true, Builtin("issubclass", cls, PyType.Get("str")));
        Assert.Equal(false, Builtin("isinstance", value, PyType.Get("Exception")));
        Assert.Same(PyType.Get("str"), PyObject.GetAttr(cls, "__base__"));
    }

    [Fact]
    public void ExactTextAllocationKeepsImmutablePayloadAndFreshIdentity()
    {
        var cls = Make(); Finish(cls); string text = new string("x😀".ToCharArray());
        var first = New(cls, text); var second = New(cls, text);
        Assert.NotSame(first, second); Assert.Same(text, Raw(first)); Assert.Equal("", Raw(New(cls)));
        Assert.Equal("word", Raw(Assert.IsAssignableFrom<PyInstance>(cls.Invoke([], ["object"], ["word"]))));
    }

    [Fact]
    public void UnreadyAndFailedClassesCannotAllocateOrCommitReady()
    {
        var cls = Make(); Assert.Throws<PyNotImplementedError>(() => New(cls));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(cls));
        cls.SetAttr("__len__", Method("len", _ => 1L)); long version = cls.Version;
        Assert.Throws<PyNotImplementedError>(() => Finish(cls)); Assert.Equal(version, cls.Version);
        Assert.Throws<PyNotImplementedError>(() => New(cls));
        Assert.True(cls.DeleteAttr("__len__")); Finish(cls); Assert.Equal("ok", Raw(New(cls, "ok")));
    }

    [Fact]
    public void InitReceivesOriginalKeywordValueOnceAndChecksNone()
    {
        var cls = Make(); int calls = 0; string input = new string(['o', 'k']); object? seen = null;
        cls.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self", "object"], [null, null], 2,
            args => { calls++; seen = args[1]; Assert.Equal(input, Raw((PyInstance)args[0]!)); return PyNone.Instance; }));
        Finish(cls); cls.Invoke([], ["object"], [input]); Assert.Equal(1, calls); Assert.Same(input, seen);
        cls.SetAttr("__init__", Method("bad", _ => 7L));
        Assert.Throws<PyTypeError>(() => New(cls));
    }

    [Fact]
    public void DescendantsRetainLayoutAndObserveValidLateInit()
    {
        var parent = Make(); Finish(parent); var child = Make("Child", parent); Finish(child);
        var value = New(child, "payload"); Assert.Same(child, value.Class); Assert.Equal("payload", Raw(value));
        Assert.Same(parent, PyObject.GetAttr(child, "__base__"));
        int calls = 0; parent.SetAttr("__init__", Method("init", _ => { calls++; return PyNone.Instance; }));
        New(child); Assert.Equal(1, calls); Assert.Throws<PyNotImplementedError>(() => Finish(child));
    }

    [Fact]
    public void UnsupportedDefinitionSlotsFailBeforeReady()
    {
        foreach (string slot in new[] { "__new__", "__hash__", "__eq__", "__getitem__", "__rsub__", "__ifloordiv__", "__format__", "__slots__", "__dict__" })
        {
            var cls = Make(); cls.SetAttr(slot, PyNone.Instance); long version = cls.Version;
            var error = Assert.Throws<PyNotImplementedError>(() => Finish(cls));
            Assert.Equal($"str subclass slot {slot} is not supported", error.Message);
            Assert.Equal(version, cls.Version); Assert.Throws<PyNotImplementedError>(() => New(cls));
        }
    }

    [Fact]
    public void LateForbiddenWritesAndDeletesAreAtomic()
    {
        var cls = Make(); Finish(cls);
        foreach (string slot in new[] { "__new__", "__hash__", "__rshift__", "__class__", "__name__" })
        {
            long version = cls.Version; int count = cls.Attributes.Count;
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr(slot, 1L));
            Assert.Throws<PyNotImplementedError>(() => cls.DeleteAttr(slot));
            Assert.Equal(version, cls.Version); Assert.Equal(count, cls.Attributes.Count);
        }
    }

    [Fact]
    public void NumericUnaryAndOrdinaryDunderNamesAreNotBlanketRejected()
    {
        var cls = Make();
        foreach (string slot in new[] { "__int__", "__float__", "__index__", "__neg__", "__pos__", "__invert__", "__abs__", "__next__", "__call__", "__enter__", "__exit__", "__fspath__" })
            cls.SetAttr(slot, Method(slot, _ => 1L));
        object data = new PyDict(); cls.SetAttr("__application_map__", data); Finish(cls);
        Assert.Same(data, PyObject.GetAttr(New(cls), "__application_map__"));
        cls.SetAttr("__index__", Method("index", _ => 2L)); Assert.True(cls.DeleteAttr("__index__"));
    }

    [Fact]
    public void InstanceMetadataCannotShadowActualClassOrExposeFakeDictionary()
    {
        var cls = Make(); Finish(cls); var value = New(cls);
        Assert.Same(cls, PyObject.GetAttr(value, "__class__"));
        foreach (object target in new object[] { cls, value })
        {
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(target, "__dict__"));
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(target, "__dict__", new PyDict()));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(target, "__dict__"));
        }
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(value, "__class__", 1L));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "__class__"));
    }

    [Fact]
    public void OrdinaryAttributesPropertiesAndCreationHookRejectionRemainReal()
    {
        var cls = Make(); cls.SetAttr("size", new PyProperty(Method("get", _ => 9L))); Finish(cls);
        var value = New(cls); PyObject.SetAttr(value, "note", "hello");
        Assert.Equal("hello", PyObject.GetAttr(value, "note")); Assert.Equal(9L, PyObject.GetAttr(value, "size"));
        PyObject.DelAttr(value, "note"); Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "note"));
        foreach (string slot in new[] { "__set_name__", "__set__", "__delete__" })
        {
            var descriptor = new PyClass("Descriptor"); descriptor.SetAttr(slot, Method("hook", _ => throw new Exception("must not execute")));
            var invalid = Make(); invalid.SetAttr("field", new PyInstance(descriptor));
            Assert.Throws<PyNotImplementedError>(() => Finish(invalid));
        }
    }

    [Fact]
    public void DataclassAndUnsafeObjectAllocationCannotBypassPayload()
    {
        var cls = Make(); Finish(cls); long version = cls.Version; int count = cls.Attributes.Count;
        Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(cls, false));
        Assert.Equal(version, cls.Version); Assert.Equal(count, cls.Attributes.Count);
        object? rootNew = PyObject.GetAttr(PyBaseObject.Type, "__new__");
        var error = Assert.Throws<PyTypeError>(() => PyOps.CallValue(rootNew, [cls], [], []));
        Assert.Equal("object.__new__(S) is not safe, use S.__new__()", error.Message);
    }

    [Fact]
    public void OrdinaryClassesAndStageBoundariesAreUnchanged()
    {
        var cls = (PyClass)PyOps.MakeClass("Ordinary", [], null); Finish(cls);
        Assert.IsType<PyInstance>(cls.Invoke([], [], [])); Assert.IsType<PyDict>(PyObject.GetAttr(cls, "__dict__"));
        var text = Make(); Finish(text);
        cls.SetAttr("__str__", Method("str", _ => "converted"));
        Assert.Equal("converted", Raw(Assert.IsAssignableFrom<PyInstance>(text.Invoke([new PyInstance(cls)], [], []))));
        Assert.Throws<PyTypeError>(() => text.Invoke(["abc"], ["encoding"], ["utf8"]));
    }
}
