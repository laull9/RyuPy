using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>真Flag的缓存身份、严格位域、单bit迭代和既有Enum无退化，不经过CLR枚举桥接。</summary>
public class PyFlagTests
{
    private static PyClass Flag(string name, params (string Name, object Value)[] items)
    {
        var cls = (PyClass)PyEnum.Create(name, [PyEnum.CreateAttributes()["Flag"]], PyNone.Instance);
        var names = new object?[items.Length];
        for (int i = 0; i < items.Length; i++) { cls.SetAttr(items[i].Name, items[i].Value); names[i] = items[i].Name; }
        return (PyClass)PyEnum.Finalize(cls, names);
    }
    private static PyInstance Value(PyClass cls, object value) => (PyInstance)PyEnum.Construct(cls, [value], [], []);
    private static object? Method(object self, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(self, name), args, [], []);

    [Fact]
    public void StableNativeRootCannotBeReboundThroughModule()
    {
        PyNativeModules.Register();
        PyModule module = PyModules.Import("enum");
        object? original = module.GetAttr("Flag");
        Assert.Throws<PyNotImplementedError>(() => module.SetAttr("Flag", PyNone.Instance));
        Assert.Same(original, module.GetAttr("Flag"));
    }

    [Fact]
    public void NamedAndGeneratedCombinationsShareIdentityAndKeepSingleBitOrder()
    {
        var cls = Flag("Access", ("ZERO", 0L), ("B", 2L), ("A", 1L), ("C", 4L), ("AB", 3L), ("ALIAS", 1L));
        var a = Value(cls, 1L); var b = Value(cls, 2L);
        Assert.Same(a, PyEnum.GetItem(cls, "ALIAS"));
        Assert.Same(Value(cls, 3L), Method(a, "__or__", b));
        var combo = Value(cls, 5L);
        Assert.Same(combo, Method(a, "__or__", Value(cls, 4L)));
        Assert.Equal("Access.A|C", PyEnum.MemberRepr(combo, false));
        Assert.Equal(3L, PyEnum.Length(cls));
        Assert.Equal(new object?[] { b, a, Value(cls, 4L) }, PyOps.ToSequence(cls));
        Assert.Equal(new object?[] { b, a, Value(cls, 4L) }, PyOps.ToSequence(PyEnum.Iterate(cls)));
        Assert.Equal(new object?[] { a, Value(cls, 4L) }, PyOps.ToSequence(Method(combo, "__iter__")!));
        Assert.False(PyOps.Truthy(Value(cls, 0L)));
        Assert.Equal(2L, Method(combo, "__len__"));
        Assert.Equal(true, Method(combo, "__contains__", Value(cls, 0L)));
        Assert.Same(Value(cls, 6L), Method(a, "__invert__"));
    }

    [Fact]
    public void SparseStrictMaskAndUnknownZeroKeepPythonMetadata()
    {
        var cls = Flag("Sparse", ("A", 1L), ("B", 8L));
        Assert.Same(Value(cls, 8L), Value(cls, -8L));
        Assert.Throws<PyValueError>(() => Value(cls, -1L));
        Assert.Throws<PyValueError>(() => Value(cls, 2L));
        Assert.Same(Value(cls, 8L), Method(Value(cls, 1L), "__invert__"));
        var zero = Value(cls, 0L);
        Assert.Equal("Sparse(0)", PyEnum.MemberRepr(zero, false));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(zero, "name"));
        Assert.Equal("<flag 'Sparse'>", PyEnum.ClassRepr(cls));
        Assert.Same(Value(cls, 1L), Value(cls, 1.0));
        Assert.Throws<PyValueError>(() => Value(cls, 9.0));
        Assert.Same(Value(cls, 9L), Value(cls, 9.0));
    }

    [Fact]
    public void EmptyZeroOnlyAndUnsupportedDefinitionsAreDistinct()
    {
        var cls = Flag("ZeroOnly", ("ZERO", 0L));
        Assert.Equal(0L, PyEnum.Length(cls));
        Assert.Same(Value(cls, 0L), Value(cls, -1L));
        Assert.Throws<PyTypeError>(() => Value(Flag("Empty"), 0L));
        Assert.Throws<PyNotImplementedError>(() => Flag("CompositeOnly", ("AB", 3L)));
        Assert.Throws<PyNotImplementedError>(() => Flag("Negative", ("A", -1L)));
        var other = Flag("Other", ("A", 1L));
        var normal = Flag("Normal", ("A", 1L));
        Assert.Same(PyNotImplemented.Instance, Method(Value(normal, 1L), "__or__", Value(other, 1L)));
        Assert.Same(PyNotImplemented.Instance, Method(Value(normal, 1L), "__or__", 1L));
        Assert.Throws<PyTypeError>(() => Method(Value(normal, 1L), "__contains__", Value(other, 1L)));
    }
}
