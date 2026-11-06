// G2未知hook的拒绝相位：先允许类体写入，到真实终结消费才NI；不执行未知回调。
// CP的完整hooks.py给真实结果；这里明确测有限支持边界，不把NI当CP逐字通过。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingGenericBasesGuardsTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static PyClass Make(string name, object? parent) =>
        Assert.IsType<PyClass>(PyOps.MakeClass(name, [PyRuntimeBases.Validate(parent)], null));
    private static void Set(object? value, string name, object? item) => PyObject.SetAttr(value, name, item);
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);

    [Fact]
    public void DefiningOwnSubclassHookSucceedsButCreatingItsChildRejectsAtFinalize()
    {
        PyClass parent = Make("Parent", Subscribe(Variable("T"))); int calls = 0;
        Set(parent, "__init_subclass__", PyNativeFunctions.CreateVariadic("__init_subclass__", _ =>
        { calls++; return PyNone.Instance; }));
        Finish(parent); Assert.Equal(0, calls);
        PyClass child = Make("Child", parent);
        Set(child, "body_ran", true);
        Set(child, "__parameters__", new PyTuple(["body"]));
        Assert.Throws<PyNotImplementedError>(() => Finish(child));
        Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => Call(child));
    }

    [Fact]
    public void UnknownDescriptorRejectsAtFinalizeBeforeBareGenericError()
    {
        PyClass descriptor = Make("Descriptor", PyType.Get("object")); int calls = 0;
        Set(descriptor, "__set_name__", PyNativeFunctions.CreateVariadic("__set_name__", _ =>
        { calls++; return PyNone.Instance; })); Finish(descriptor);
        PyClass cls = Make("Bare", Form("Generic"));
        Set(cls, "field", Call(descriptor));
        Set(cls, "body_ran", true);
        Assert.Throws<PyNotImplementedError>(() => Finish(cls));
        Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => Call(cls));
    }

    [Fact]
    public void OwnClassGetitemIsNotConsumedByDefinitionOrOrdinaryConstruction()
    {
        PyClass cls = Make("OwnSubscription", Subscribe(Variable("T"))); int calls = 0;
        Set(cls, "__class_getitem__", PyNativeFunctions.CreateVariadic("__class_getitem__", _ =>
        { calls++; return PyNone.Instance; }));
        Finish(cls);
        Assert.Same(cls, PyObject.TypeOf(Call(cls)));
        Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(cls, PyType.Get("int")));
        Assert.Equal(0, calls);
    }
}
