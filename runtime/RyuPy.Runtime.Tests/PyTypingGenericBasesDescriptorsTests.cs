// G2显式属性绑定边界：未知descriptor在真正__get__消费处NI，不伪造TypeError。
// data/non-data优先级独立于支持未知回调；普通函数与已支持包装保持原协议。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingGenericBasesDescriptorsTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static PyClass Make(string name, object? parent)
    {
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass(name, [PyRuntimeBases.Validate(parent)], null));
        PyOps.FinalizeOrdinaryClass(cls); return cls;
    }
    private static void Set(object? value, string name, object? item) => PyObject.SetAttr(value, name, item);
    private static object? Descriptor(string? dataSlot, Action called)
    {
        var cls = Make("Descriptor", PyType.Get("object"));
        Set(cls, "__get__", PyNativeFunctions.CreateVariadic("__get__", _ =>
        { called(); return PyNone.Instance; }));
        if (dataSlot is not null)
            Set(cls, dataSlot, PyNativeFunctions.CreateVariadic(dataSlot, _ =>
            { called(); return PyNone.Instance; }));
        return Call(cls);
    }

    [Fact]
    public void RootInheritedAndOwnDescriptorsRejectBothClassAndInstanceBinding()
    {
        object? root = Form("Generic");
        PyClass box = Make("Box", Subscribe(Variable("T")));
        object? instance = Call(box); int calls = 0;
        object? descriptor = Descriptor(null, () => calls++);
        const string name = "g2_descriptor_read";
        Set(root, name, descriptor);
        try
        {
            Assert.Throws<PyNotImplementedError>(() => Get(box, name));
            Assert.Throws<PyNotImplementedError>(() => Get(instance, name));
            Set(box, "own_descriptor", descriptor);
            Assert.Throws<PyNotImplementedError>(() => Get(box, "own_descriptor"));
            Assert.Throws<PyNotImplementedError>(() => Get(instance, "own_descriptor"));
            Assert.Equal(0, calls);
        }
        finally { PyObject.DelAttr(root, name); }
    }

    [Fact]
    public void NonDataDescriptorStillAllowsExistingInstanceFieldToShadowIt()
    {
        PyClass box = Make("Box", Subscribe(Variable("T")));
        object? instance = Call(box); int calls = 0;
        Set(instance, "field", 42L);
        Set(box, "field", Descriptor(null, () => calls++));
        Assert.Equal(42L, Get(instance, "field"));
        Assert.Throws<PyNotImplementedError>(() => Get(box, "field"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void EitherSetOrDeleteMakesDataDescriptorPrecedeExistingField()
    {
        foreach (string slot in new[] { "__set__", "__delete__" })
        {
            PyClass box = Make("Box", Subscribe(Variable("T")));
            object? instance = Call(box); int calls = 0;
            Set(instance, "field", 42L);
            Set(box, "field", Descriptor(slot, () => calls++));
            Assert.Throws<PyNotImplementedError>(() => Get(instance, "field"));
            Assert.Throws<PyNotImplementedError>(() => Get(box, "field"));
            Assert.Equal(0, calls);
        }
    }

    [Fact]
    public void OrdinaryFunctionStaticmethodAndClassmethodKeepExplicitBinding()
    {
        PyClass box = Make("Box", Subscribe(Variable("T")));
        object? instance = Call(box);
        Set(box, "plain", PyNativeFunctions.CreateVariadic("plain", args => args[0]));
        Set(box, "static", new PyStaticMethod(PyNativeFunctions.CreateVariadic("static", args => args[0])));
        Set(box, "class_method", new PyClassMethod(PyNativeFunctions.CreateVariadic("class_method", args => args[0])));
        Assert.Same(instance, Call(Get(box, "plain"), instance));
        Assert.Same(instance, Call(Get(instance, "plain")));
        Assert.Equal(17L, Call(Get(box, "static"), 17L));
        Assert.Equal(18L, Call(Get(instance, "static"), 18L));
        Assert.Same(box, Call(Get(box, "class_method")));
        Assert.Same(box, Call(Get(instance, "class_method")));
    }

    [Fact]
    public void ModuleMetadataReturnsRawObjectWithoutDescriptorBinding()
    {
        object? root = Form("Generic"), savedModule = Get(root, "__module__");
        PyClass box = Make("Box", Subscribe(Variable("T"))); int calls = 0;
        object? descriptor = Descriptor(null, () => calls++);
        try
        {
            Set(root, "__module__", descriptor);
            Set(box, "__module__", descriptor);
            Assert.Same(descriptor, Get(root, "__module__"));
            Assert.Same(descriptor, Get(box, "__module__"));
            Assert.Equal(0, calls);
            // 只有专用module元数据返回原值；普通属性仍真正消费绑定并NI。
            Set(box, "ordinary_descriptor", descriptor);
            Assert.Throws<PyNotImplementedError>(() => Get(box, "ordinary_descriptor"));
            Assert.Equal(0, calls);
        }
        finally { Set(root, "__module__", savedModule); }
    }

}
