// 普通MI冷层与共享缓存的真实对象测试；不代替编译链和CP差分。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyOrdinaryMiTests
{
    private static PyClass Root(string name, PyClass? parent = null)
    {
        var cls = (PyClass)PyOps.MakeClass(name, parent is null ? [] : [parent], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static PyClass Mi(string name, params object?[] bases)
    {
        var cls = (PyClass)PyOps.PrepareOrdinaryMiClass(name, bases);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    [Fact]
    public void MiMetadataTuplesHaveStableIdentity()
    {
        var d = Mi("D", Root("A"), Root("B"));
        Assert.Same(PyObject.GetAttr(d, "__bases__"), PyObject.GetAttr(d, "__bases__"));
        Assert.Same(PyObject.GetAttr(d, "__mro__"), PyObject.GetAttr(d, "__mro__"));
    }
    [Fact]
    public void DiamondLookupIsC3AndSingleBaseDescendantKeepsSecondBranch()
    {
        var a = Root("A"); a.SetAttr("value", "A");
        var b = Root("B", a); var c = Root("C", a); c.SetAttr("value", "C");
        var d = Mi("D", b, c); var e = Mi("E", d);
        Assert.True(d.TryLookup("value", out var owner, out var value));
        Assert.Same(c, owner); Assert.Equal("C", value);
        Assert.True(e.IsSubclassOf(c)); Assert.True(e.IsSubclassOf(a));
        c.SetAttr("value", "changed");
        Assert.True(e.TryLookup("value", out owner, out value));
        Assert.Same(c, owner); Assert.Equal("changed", value);
    }
    [Fact]
    public void DuplicateBasesFailAtFinalizeAfterBodyWrites()
    {
        var a = Root("A");
        var pending = (PyClass)PyOps.PrepareOrdinaryMiClass("D", [a, a]);
        pending.SetAttr("body", 42L);
        var error = Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(pending));
        Assert.Equal("duplicate base class A", error.Message);
        Assert.Equal(42L, pending.Attributes["body"]);
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(pending));
    }
    [Fact]
    public void ConflictingMroFailsAfterNamespaceAndMatchesCandidateHeads()
    {
        var a = Root("A"); var b = Root("B");
        var ab = Mi("AB", a, b); var ba = Mi("BA", b, a);
        var pending = (PyClass)PyOps.PrepareOrdinaryMiClass("D", [ab, ba]);
        pending.SetAttr("body", true);
        var error = Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(pending));
        Assert.Equal("Cannot create a consistent method resolution\norder (MRO) for bases A, B", error.Message);
        Assert.Equal(true, pending.Attributes["body"]);
    }
    [Fact]
    public void OldConstructorCannotSilentlyConsumeMiFamily()
    {
        var d = Mi("D", Root("A"), Root("B"));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("E", [d], null));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Prepare("E", "E", "m", [d]));
        Assert.Throws<PyNotImplementedError>(() => d.SetAttr("__bases__", new PyTuple([])));
    }
    [Fact]
    public void RightBranchHooksAndLateMutationCannotBypassFamilyGuard()
    {
        var a = Root("A"); var b = Root("B");
        var pending = (PyClass)PyOps.PrepareOrdinaryMiClass("D", [a, b]);
        b.SetAttr("__init_subclass__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(pending));
        b.DeleteAttr("__init_subclass__");
        var d = Mi("D", a, b);
        b.SetAttr("__getattribute__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => d.TryLookup("missing", out _, out _));
    }
    [Fact]
    public void NewDomainRejectsNativeLayoutsWithoutChangingOldSingleBase()
    {
        var a = Root("A");
        foreach (string name in new[] { "dict", "str", "list" })
            Assert.Throws<PyNotImplementedError>(() => PyOps.PrepareOrdinaryMiClass("D", [a, PyType.Get(name)]));
        Assert.IsType<PyClass>(PyOps.MakeClass("Old", [PyType.Get("list")], null));
    }
    [Fact]
    public void BaseArrayIsFrozenAndObjectIdentityParticipatesInC3()
    {
        var a = Root("A"); var b = Root("B"); object?[] bases = [a, b];
        var pending = (PyClass)PyOps.PrepareOrdinaryMiClass("D", bases);
        bases[1] = PyType.Get("list"); PyOps.FinalizeOrdinaryClass(pending);
        Assert.True(pending.IsSubclassOf(b));
        var bad = (PyClass)PyOps.PrepareOrdinaryMiClass("Bad", [PyBaseObject.Type, a]);
        Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(bad));
    }
}
