// 动态单基冷入口只授权延迟发布合同，不放宽旧构造、native MI或异常factory能力。
using System;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyRuntimeDerivedClassTests
{
    private static PyClass Ordinary(string name, params object?[] bases)
    {
        var cls = (PyClass)PyOps.MakeClass(name, bases, null); PyOps.FinalizeOrdinaryClass(cls); return cls;
    }
    private static PyClass Multiple(string name, params object?[] bases)
    {
        var cls = (PyClass)PyOps.PrepareOrdinaryMiClass(name, bases); PyOps.FinalizeOrdinaryClass(cls); return cls;
    }
    [Fact]
    public void DynamicMiDescendantKeepsC3AndFrozenBaseIdentity()
    {
        var a = Ordinary("A"); var b = Ordinary("B", a); var c = Ordinary("C", a); c.SetAttr("value", 42L);
        var d = Multiple("D", b, c); object?[] bases = [d];
        var result = (PyClass)PyOps.PrepareRuntimeDerivedClass("Result", bases); bases[0] = PyType.Get("dict");
        result.SetAttr("body", 7L); PyOps.FinalizeOrdinaryClass(result);
        Assert.True(result.IsSubclassOf(c)); Assert.Same(d, result.DirectBasesTuple().Items[0]);
        Assert.True(result.TryLookup("value", out var owner, out var value)); Assert.Same(c, owner); Assert.Equal(42L, value);
        Assert.IsType<PyInstance>(result.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("Old", [d], null));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Prepare("Old", "Old", "m", [d]));
    }
    [Theory]
    [InlineData("str")]
    [InlineData("list")]
    [InlineData("dict")]
    public void OldSingleBaseLayoutsAreNotRoutedThroughMi(string name)
    {
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClassForMode("Direct", [PyType.Get(name)], false);
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.False(cls.HasMultipleInheritance);
        Assert.Equal(name == "str", cls.HasStringLayout); Assert.Equal(name == "list", cls.HasListLayout); Assert.Equal(name == "dict", cls.HasDictLayout);
        var child = (PyClass)PyOps.PrepareRuntimeDerivedClassForMode("Indirect", [cls], false);
        PyOps.FinalizeOrdinaryClass(child); Assert.Same(cls, child.BaseClass); Assert.False(child.HasMultipleInheritance);
        Assert.NotNull(child.Invoke([], [], []));
    }
    [Fact]
    public void DynamicCodeCapabilityOnlyGuardsActualMiBranch()
    {
        var root = Ordinary("A");
        var plain = (PyClass)PyOps.PrepareRuntimeDerivedClassForMode("Plain", [root], false);
        PyOps.FinalizeOrdinaryClass(plain); Assert.IsType<PyInstance>(plain.Invoke([], [], []));
        var mi = Multiple("MI", root, Ordinary("B"));
        var error = Assert.Throws<PyNotImplementedError>(() => PyOps.PrepareRuntimeDerivedClassForMode("NoDynamic", [mi], false));
        Assert.Contains("dynamic code support", error.Message, StringComparison.Ordinal);
        Assert.IsType<PyClass>(PyOps.PrepareRuntimeDerivedClassForMode("Dynamic", [mi], true));
    }
    [Fact]
    public void OldNativeAndExceptionRejectionsRemain()
    {
        Assert.Throws<PyTypeError>(() => PyOps.PrepareRuntimeDerivedClass("Zero", []));
        Assert.Throws<PyTypeError>(() => PyOps.PrepareRuntimeDerivedClass("Two", [PyBaseObject.Type, PyBaseObject.Type]));
        Assert.Throws<PyTypeError>(() => PyOps.PrepareRuntimeDerivedClass("Value", [1L]));
        Assert.Throws<PyNotImplementedError>(() => PyOps.PrepareRuntimeDerivedClass("Ordered", [PyOrderedDict.Type]));
        Assert.Throws<PyTypeError>(() => PyOps.PrepareRuntimeDerivedClass("Proxy", [PyMappingProxy.Type]));
        Assert.Throws<PyTypeError>(() => PyOps.PrepareRuntimeDerivedClass("Slice", [PySlice.Type]));
        var error = Assert.Throws<PyTypeError>(() => PyOps.PrepareRuntimeDerivedClass("Exception", [PyType.Get("Exception")]));
        Assert.Contains("compiled exception factory", error.Message, StringComparison.Ordinal);
        Assert.Throws<PyNotImplementedError>(() => PyOps.PrepareOrdinaryMiClass("NativeMI", [Ordinary("A"), PyType.Get("list")]));
    }
    [Fact]
    public void LateRightBranchMutationStillFailsAtFinalize()
    {
        var a = Ordinary("A"); var b = Ordinary("B"); var mi = Multiple("MI", a, b);
        var pending = (PyClass)PyOps.PrepareRuntimeDerivedClass("Pending", [mi]); pending.SetAttr("body", 9L);
        b.SetAttr("__init_subclass__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(pending));
        Assert.Equal(9L, pending.Attributes["body"]);
    }
    [Fact]
    public void TypingIoStillUsesExistingDedicatedDefinition()
    {
        var io = PyTypingIo.Root("IO");
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClassForMode("Stream", [io], false);
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.True(cls.IsTypingIoFamily); Assert.False(cls.HasMultipleInheritance); Assert.Same(io, cls.BaseClass);
        Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }
    [Fact]
    public void GenericAliasKeepsOriginalBasesAndDedicatedMetadata()
    {
        using var frame = new PyTypingGenericRootTests();
        object? variable = PyTypingGenericRootTests.Variable("T");
        object? alias = PyTypingGenericRootTests.Subscribe(variable);
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClassForMode("Box", [alias], false);
        cls.SetAttr("__orig_bases__", PyNone.Instance); PyOps.FinalizeOrdinaryClass(cls);
        Assert.False(cls.HasMultipleInheritance); Assert.True(cls.IsTypingGenericFamily);
        Assert.Same(alias, Assert.IsType<PyTuple>(PyObject.GetAttr(cls, "__orig_bases__")).Items[0]);
        Assert.Same(variable, Assert.IsType<PyTuple>(PyObject.GetAttr(cls, "__parameters__")).Items[0]);
        Assert.Same(PyTypingGeneric.Root, cls.BaseClass);
    }

}
