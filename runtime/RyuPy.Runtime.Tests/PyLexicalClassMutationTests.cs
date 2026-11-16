// 新族late能力拒绝不阻止旧父类变更；缓存、实例字段和构造都必须重新检查。
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyLexicalClassMutationTests
{
    private static (PyClass Parent, PyClass Child, PyInstance Instance) Pair()
    {
        var parent = (PyClass)PyOps.MakeClass("Parent", [], null);
        PyOps.FinalizeOrdinaryClass(parent);
        var child = (PyClass)PyLexicalClassFactory.Finish(
            PyLexicalClassFactory.Prepare("Child", "Child", "source", [parent]));
        var instance = new PyInstance(child);
        PyObject.SetAttr(instance, "field", 3L);
        return (parent, child, instance);
    }
    [Fact]
    public void LateParentHookInvalidatesWarmFieldPicAndConstructionOnlyForNewFamily()
    {
        var (parent, child, instance) = Pair();
        Assert.Equal(3L, PyPic.GetAttr(0, instance, "field"));
        PyPic.SetAttr(1, instance, "field", 4L);
        child.Invoke([], [], []);
        parent.SetAttr("__init_subclass__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyPic.GetAttr(0, instance, "field"));
        Assert.Throws<PyNotImplementedError>(() => PyPic.SetAttr(1, instance, "field", 5L));
        Assert.Throws<PyNotImplementedError>(() => child.Invoke([], [], []));
        Assert.IsType<PyInstance>(parent.Invoke([], [], []));
        parent.DeleteAttr("__init_subclass__");
        Assert.Equal(4L, PyPic.GetAttr(0, instance, "field"));
    }
    [Fact]
    public void DescriptorTypeMutationInvalidatesNegativeAndPlainValueLookup()
    {
        var (parent, child, instance) = Pair();
        var descriptorType = (PyClass)PyOps.MakeClass("Descriptor", [], null);
        PyOps.FinalizeOrdinaryClass(descriptorType);
        var descriptor = new PyInstance(descriptorType);
        parent.SetAttr("value", descriptor);
        Assert.Same(descriptor, PyPic.GetAttr(2, instance, "value"));
        Assert.False(child.TryLookup("missing", out _, out _));
        descriptorType.SetAttr("__get__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyPic.GetAttr(2, instance, "value"));
        Assert.Throws<PyNotImplementedError>(() => child.TryLookup("missing", out _, out _));
    }
    [Fact]
    public void OldFactoryDescendantsInheritNewFamilyCapabilityBoundary()
    {
        var (parent, child, _) = Pair();
        var descendant = (PyClass)PyOps.MakeClass("Descendant", [child], null);
        PyOps.FinalizeOrdinaryClass(descendant);
        parent.SetAttr("__slots__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => descendant.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(descendant, "__dict__"));
    }
    [Fact]
    public void ExistingSuperAndExplicitRootCallsCannotBypassLateCapabilityCheck()
    {
        var (parent, child, instance) = Pair();
        var proxy = new PySuper(child, instance);
        object init = PyBaseObjectMethods.Unbound("__init__");
        parent.SetAttr("__prepare__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(proxy, "__init__"));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(init, [instance], [], []));
    }
    [Fact]
    public void LateSpecializationCannotLaunderFamilyThroughOldFactoryEarlyReturn()
    {
        var child = (PyClass)PyLexicalClassFactory.Finish(
            PyLexicalClassFactory.Prepare("C", "C", "source", []));
        PyOps.ApplyDataclass(child, false);
        var descendant = (PyClass)PyOps.MakeClass("D", [child], null);
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(descendant));
        Assert.Throws<PyNotImplementedError>(() => descendant.Invoke([], [], []));
        var old = (PyClass)PyOps.MakeClass("Old", [], null);
        PyOps.FinalizeOrdinaryClass(old);
        PyOps.ApplyDataclass(old, false);
        var oldChild = (PyClass)PyOps.MakeClass("OldChild", [old], null);
        PyOps.FinalizeOrdinaryClass(oldChild);
        Assert.IsType<PyInstance>(oldChild.Invoke([], [], []));
    }
    [Fact]
    public void SavedBoundMethodChecksActualReceiverNotUnmarkedDefiningParent()
    {
        var (parent, child, instance) = Pair();
        int calls = 0;
        var method = new PyFunction("method",
            PySignature.Create("method", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { calls++; return 7L; }, []);
        parent.SetAttr("method", method);
        object? saved = PyObject.GetAttr(instance, "method");
        var oldInstance = new PyInstance(parent);
        object? oldSaved = PyObject.GetAttr(oldInstance, "method");
        Assert.Equal(7L, PyOps.CallValue(saved, [], [], []));
        Assert.Equal(7L, PyPic.CallMethod0(3, instance, "method"));
        parent.SetAttr("__slots__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(saved, [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyPic.CallMethod0(3, instance, "method"));
        Assert.Equal(2, calls);
        Assert.Equal(7L, PyOps.CallValue(oldSaved, [], [], []));
    }

    [Fact]
    public void FailedCoreFinalizationClearsTemporaryInternalBypass()
    {
        var (_, child, _) = Pair();
        var pending = (PyClass)PyOps.MakeClass("Pending", [child], null);
        pending.SetAttr("__qualname__", 1L);
        Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(pending));
        Assert.Throws<PyNotImplementedError>(() => pending.Invoke([], [], []));
    }

}
