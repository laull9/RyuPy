// namedtuple阶段B1的身份、字段描述符和写前保护；保留 B1 边界验证，不代表全部方法协议通过。
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNamedTupleIdentityTests
{
    private static PyClass Class(string name, string[] fields, object?[]? defaults = null, object? module = null)
    {
        Type layout = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyNamedTupleLayout")!;
        try
        {
            return (PyClass)layout.GetMethod("CreateClassWithModule", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [name, fields, defaults ?? [], module ?? PyNone.Instance])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static object Instance(PyClass cls, params object?[] args) => cls.Invoke(args, [], [])!;
    private static object? Get(object value, string name) => PyObject.GetAttr(value, name);
    private static object? Call(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);
    private static bool Builtin(string name, params object?[] args) => (bool)PyOps.CallBuiltin(name, args, [], [])!;

    [Fact]
    public void IdentityAndTupleAncestorsUseTheRealClass()
    {
        PyClass first = Class("Point", ["x", "y"]);
        PyClass second = Class("Point", ["x", "y"]);
        object point = Instance(first, 1L, 2L);
        Assert.Same(first, PyObject.TypeOf(point));
        Assert.Same(PyType.Get("type"), PyObject.TypeOf(first));
        Assert.Same(first, Get(point, "__class__"));
        Assert.True(Builtin("isinstance", point, first));
        Assert.False(Builtin("isinstance", point, second));
        Assert.True(Builtin("isinstance", point, PyType.Get("tuple")));
        Assert.True(Builtin("isinstance", point, PyBaseObject.Type));
        Assert.True(Builtin("issubclass", first, PyType.Get("tuple")));
        Assert.True(Builtin("issubclass", first, PyBaseObject.Type));
        Assert.False(Builtin("issubclass", first, second));
        Assert.Same(PyType.Get("tuple"), Get(first, "__base__"));
        Assert.Equal<object?>([PyType.Get("tuple")], Assert.IsType<PyTuple>(Get(first, "__bases__")).Items);
        Assert.Equal<object?>([first, PyType.Get("tuple"), PyBaseObject.Type], Assert.IsType<PyTuple>(Get(first, "__mro__")).Items);
        Assert.False(Builtin("callable", point));
        Assert.True(Builtin("callable", first));
        Assert.False(first.IsException);
        Assert.Same(Get(first, "__bases__"), Get(first, "__bases__"));
        Assert.Same(Get(first, "__mro__"), Get(first, "__mro__"));
        Assert.Throws<PyAttributeError>(() => Get(point, "__bases__"));
        Assert.Throws<PyAttributeError>(() => Get(point, "__mro__"));
    }

    [Fact]
    public void MetadataKeepsModuleAndDefaultsSeparateFromConstruction()
    {
        var module = new PyList();
        var shared = new PyList();
        PyClass cls = Class("Envelope", ["value"], [shared], module);
        object first = Instance(cls);
        Assert.Same(module, Get(cls, "__module__"));
        Assert.Same(module, Get(first, "__module__"));
        Assert.Equal("Envelope", Get(cls, "__name__"));
        Assert.Equal("Envelope", Get(cls, "__qualname__"));
        Assert.Equal("Envelope(value,)", Get(cls, "__doc__"));
        Assert.Same(Get(cls, "_fields"), Get(first, "_fields"));
        Assert.Same(Get(cls, "_fields"), Get(cls, "__match_args__"));
        Assert.Equal<object?>(["value"], Assert.IsType<PyTuple>(Get(cls, "_fields")).Items);
        Assert.Empty(Assert.IsType<PyTuple>(Get(cls, "__slots__")).Items);
        PyDict defaults = Assert.IsType<PyDict>(Get(cls, "_field_defaults"));
        defaults.Set("value", 99L);
        Assert.Same(shared, Get(Instance(cls), "value"));
        Assert.Equal(99L, defaults.Get("value"));
        Assert.Throws<PyAttributeError>(() => Get(first, "__name__"));
        Assert.Throws<PyAttributeError>(() => Get(first, "__qualname__"));
        Assert.Throws<PyAttributeError>(() => Get(first, "__dict__"));
    }

    [Fact]
    public void ReprUsesLabelsAndRealTypeNames()
    {
        Assert.Equal("Vacant()", PyRepr.Repr(Instance(Class("Vacant", []))));
        object one = Instance(Class("Single", ["x"], module: "pkg.unit"), 3L);
        Assert.Equal("Single(x=3)", PyRepr.Str(one));
        Assert.Equal("Single", PyRepr.TypeName(one));
        Assert.Equal("<class 'pkg.unit.Single'>", PyRepr.Repr(PyObject.TypeOf(one)));
        object pair = Instance(Class("Pair", ["x", "y"]), 1L, "text");
        Assert.Equal("Pair(x=1, y='text')", PyRepr.Repr(pair));
        Assert.Equal("<class 'Pair'>", PyRepr.Repr(PyObject.TypeOf(pair)));
    }

    [Fact]
    public void FieldLayoutIsPerPythonClassAndShadowsTupleNamesOnRead()
    {
        PyClass first = Class("First", ["x", "y"]);
        PyClass second = Class("Second", ["y", "x"]);
        object[] values = [Instance(first, 1L, 2L), Instance(second, 3L, 4L), Instance(first, 5L, 6L)];
        Assert.Equal(1L, Get(values[0], "x"));
        Assert.Equal(4L, Get(values[1], "x"));
        Assert.Equal(5L, Get(values[2], "x"));
        var function = PyNativeFunctions.Create("field", ["x"], [null], 1, args => args[0]);
        object shadow = Instance(Class("Shadow", ["self", "cls", "count", "index"]), 1L, 2L, function, 4L);
        Assert.Equal(1L, Get(shadow, "self"));
        Assert.Equal(2L, Get(shadow, "cls"));
        Assert.Same(function, Get(shadow, "count"));
        Assert.Equal(9L, Call(Get(shadow, "count"), 9L));
        Assert.Equal(4L, Get(shadow, "index"));
        Assert.Equal(1L, Call(Get(values[0], "count"), 1L));
    }

    [Fact]
    public void DescriptorReadsAnyTupleAndDoesNotValidateOwner()
    {
        PyClass cls = Class("Pair", ["first", "second"]);
        object descriptor = Get(cls, "second")!;
        object? getter = Get(descriptor, "__get__");
        Assert.Same(descriptor, Call(getter, PyNone.Instance, new object()));
        Assert.Equal("__get__(None, None) is invalid", Assert.Throws<PyTypeError>(() => Call(getter, PyNone.Instance)).PyMessage);
        Assert.Equal("__get__(None, None) is invalid", Assert.Throws<PyTypeError>(() => Call(getter, PyNone.Instance, PyNone.Instance)).PyMessage);
        Assert.Same(Get(descriptor, "__doc__"), Get(descriptor, "__doc__"));
        Assert.Equal(8L, Call(getter, new PyTuple([7L, 8L]), new object()));
        Assert.Equal(4L, Call(getter, Instance(Class("Other", ["a", "b"]), 3L, 4L)));
        Assert.Equal("Alias for field number 1", Get(descriptor, "__doc__"));
        Assert.Equal("tuple index out of range", Assert.Throws<PyIndexError>(() => Call(getter, new PyTuple([1L]))).PyMessage);
        Assert.Throws<PyTypeError>(() => Call(getter, new PyList([1L, 2L])));
        Assert.Equal("can't set attribute", Assert.Throws<PyAttributeError>(() => Call(Get(descriptor, "__set__"), new PyTuple([1L, 2L]), 5L)).PyMessage);
        Assert.Equal("can't delete attribute", Assert.Throws<PyAttributeError>(() => Call(Get(descriptor, "__delete__"), new PyTuple([1L, 2L]))).PyMessage);
    }

    [Fact]
    public void ClassDescriptorAndInstanceMutationFailBeforeChangingState()
    {
        PyClass cls = Class("Guarded", ["value"], [5L]);
        object value = Instance(cls);
        object field = Get(cls, "value")!;
        object getter = Get(field, "__get__")!;
        object originalFields = Get(cls, "_fields")!;
        foreach (string name in new[] { "_fields", "_field_defaults", "__module__", "value", "__new__", "extra" })
        {
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(cls, name, 7L));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(cls, name));
        }
        Assert.Throws<PyNotImplementedError>(() => Get(cls, "__dict__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(field, "__doc__", "changed"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(field, "__doc__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(getter, "__defaults__", new PyTuple([])));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(getter, "__code__"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, "value", 9L));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(value, "value"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, "extra", 9L));
        Assert.Same(originalFields, Get(cls, "_fields"));
        Assert.Same(field, Get(cls, "value"));
        Assert.Equal(5L, Get(value, "value"));
        Assert.Equal(5L, Get(Instance(cls), "value"));
    }

    [Fact]
    public void SubclassAndDataclassPathsRejectBeforeMutation()
    {
        PyClass cls = Class("Final", ["value"]);
        int count = cls.Attributes.Count;
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(cls));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("Child", [cls], null));
        Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(cls, false));
        Assert.Throws<PyNotImplementedError>(() => PyOps.DeclareDataclass(cls, ["extra"], false));
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(cls));
        Assert.Equal(count, cls.Attributes.Count);
        Assert.Equal(3L, Get(Instance(cls, 3L), "value"));
    }

    [Fact]
    public void SequenceAbcsUseLayoutRatherThanClassSpelling()
    {
        PyClass cls = Class("UnrelatedName", ["x"]);
        object value = Instance(cls, 1L);
        foreach (string name in new[] { "Sequence", "Collection", "Iterable", "Sized", "Container" })
        {
            object? abc = PyAbc.CreateAttributes()[name];
            Assert.True(Builtin("isinstance", value, abc));
            Assert.True(Builtin("issubclass", cls, abc));
        }
        Assert.False(Builtin("isinstance", value, PyAbc.CreateAttributes()["MutableSequence"]));
        Assert.False(Builtin("isinstance", value, PyAbc.CreateAttributes()["Callable"]));
    }

    [Fact]
    public void ConstructorErrorsPrepareAmbientContextAndCorrectLocalText()
    {
        PyClass cls = Class("ErrorShape", ["x", "y", "z"]);
        var ambient = new PyValueError("ambient");
        object? previous = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("namedtuple_probe.py", "caller");
        PyTraceback.SetLine(27);
        try
        {
            var error = Assert.Throws<PyTypeError>(() => Instance(cls));
            Assert.Same(ambient, error.Context);
            Assert.Equal(new PyStackFrame("namedtuple_probe.py", "caller", 27), error.Frames[0]);
            Assert.Equal("ErrorShape.__new__() missing 3 required positional arguments: 'x', 'y', and 'z'", error.PyMessage);
            Assert.Equal("ErrorShape.__new__() missing 2 required positional arguments: 'y' and 'z'",
                Assert.Throws<PyTypeError>(() => Instance(cls, 1L)).PyMessage);
            PyClass optional = Class("Optional", ["x", "y"], [8L]);
            Assert.Equal("Optional.__new__() takes from 2 to 3 positional arguments but 4 were given",
                Assert.Throws<PyTypeError>(() => Instance(optional, 1L, 2L, 3L)).PyMessage);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        // 原内部阶段的未发布守门在 C3 升级为真实、稳定导出，不删除此处覆盖。
        object? factory = PyCollections.CreateAttributes()["namedtuple"];
        Assert.IsAssignableFrom<IPyNativeCallable>(factory);
        Assert.Same(factory, PyCollections.CreateAttributes()["namedtuple"]);
    }
}
