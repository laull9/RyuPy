using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>有限原生协议桥：验证可调用不等于描述符，映射复制不误消费键为键值对。</summary>
public class PyNativeProtocolTests
{
    [Fact]
    public void CallableWithoutDescriptorDoesNotBindInstance()
    {
        var value = new Callable(); var cls = new PyClass("Owner"); cls.SetAttr("partial", value);
        Assert.Same(value, PyObject.GetAttr(new PyInstance(cls), "partial"));
        Assert.Equal(2L, PyOps.CallValue(value, [1L, 2L], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("callable", [value], [], []));
    }

    [Fact]
    public void NativeNonDataDescriptorBindsOnlyInstanceAccess()
    {
        var value = new Descriptor(); var cls = new PyClass("Owner"); cls.SetAttr("cached", value);
        var instance = new PyInstance(cls);
        Assert.Same(value, PyObject.GetAttr(cls, "cached"));
        Assert.Same(instance, PyObject.GetAttr(instance, "cached"));
        PyObject.SetAttr(instance, "cached", 7L);
        Assert.Equal(7L, PyObject.GetAttr(instance, "cached"));
    }

    [Fact]
    public void MappingCopyPreservesEntriesWithoutMissingFactory()
    {
        var value = new Mapping(); value.Dictionary.Set("long key", 3L);
        var copy = new PyDict(); copy.Update(value);
        Assert.Equal(3L, copy.Get("long key"));
        copy.Set("long key", 4L); Assert.Equal(3L, value.Dictionary.Get("long key"));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [value, PyType.Get("dict")], [], []));
        var type = new PyNativeType("MappingTest", "test", null, candidate => candidate is Mapping, "dict");
        Assert.Equal(true, PyOps.CallBuiltin("issubclass", [type, PyType.Get("dict")], [], []));
    }

    private class Callable : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "native_callable_test";
        public override object? GetAttr(string name) => throw Missing(name);
        public object? Invoke(object?[] args, string[] names, object?[] values) => (long)args.Length;
    }
    private sealed class Descriptor : Callable, IPyNativeDescriptor
    {
        public object? Bind(PyClass owner, object? self) => self ?? this;
    }
    private sealed class Mapping : PyNativeObject, IPyNativeMapping
    {
        public PyDict Dictionary { get; } = new();
        public override string TypeName => "mapping_test";
        public override object? GetAttr(string name) => throw Missing(name);
    }
}
