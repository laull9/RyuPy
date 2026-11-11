// 新定义位置类的事务冷层：namespace尚不是类，终结才校验并创建真实身份。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyLexicalClassFactoryTests
{
    private static PyClass Parent()
    {
        var parent = (PyClass)PyOps.MakeClass("Parent", [], null);
        PyOps.FinalizeOrdinaryClass(parent);
        return parent;
    }
    [Fact]
    public void FreshNamespacesFinishIntoFreshClassesWithExplicitMetadata()
    {
        object a = PyLexicalClassFactory.Prepare("C", "make.<locals>.C", "source", []);
        object b = PyLexicalClassFactory.Prepare("C", "make.<locals>.C", "source", []);
        Assert.False(a is PyClass);
        PyLexicalClassFactory.NamespaceSet(a, "value", PyNone.Instance);
        Assert.True(PyLexicalClassFactory.NamespaceContains(a, "value"));
        Assert.False(PyLexicalClassFactory.NamespaceContains(b, "value"));
        Assert.Throws<PyNameError>(() => PyLexicalClassFactory.NamespaceGet(b, "value"));
        var first = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(a));
        var second = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(b));
        Assert.NotSame(first, second);
        Assert.Equal("source", first.Attributes["__module__"]);
        Assert.Equal("make.<locals>.C", first.Attributes["__qualname__"]);
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(a));
    }
    [Fact]
    public void PreparationFreezesBaseReferenceAndRequiresCompletedOrdinaryAncestors()
    {
        var parent = Parent(); object?[] bases = [parent];
        object frame = PyLexicalClassFactory.Prepare("C", "C", "source", bases);
        bases[0] = PyType.Get("dict");
        var child = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Same(parent, child.BaseClass);
        Assert.Throws<PyNotImplementedError>(() =>
            PyLexicalClassFactory.Prepare("C", "C", "source", [new PyClass("Pending")]));
        foreach (object root in new[] { PyType.Get("dict"), PyType.Get("list"), PyType.Get("str") })
            Assert.Throws<PyNotImplementedError>(() =>
                PyLexicalClassFactory.Prepare("C", "C", "source", [root]));
        Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(
            PyLexicalClassFactory.Prepare("C", "C", "source", [PyBaseObject.Type])));
    }
    [Fact]
    public void HooksAreRejectedAfterBodyAndParentsAreRecheckedAtFinish()
    {
        object own = PyLexicalClassFactory.Prepare("C", "C", "source", []);
        PyLexicalClassFactory.NamespaceSet(own, "__init_subclass__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(own));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.NamespaceSet(own, "repair", 1L));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(own));
        var parent = Parent();
        object child = PyLexicalClassFactory.Prepare("C", "C", "source", [parent]);
        parent.SetAttr("__init_subclass__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(child));
    }
    [Fact]
    public void DescriptorClassLateMutationIsRecheckedWithoutInvokingIt()
    {
        var descriptor = Parent();
        var instance = new PyInstance(descriptor);
        object frame = PyLexicalClassFactory.Prepare("C", "C", "source", []);
        PyLexicalClassFactory.NamespaceSet(frame, "value", instance);
        descriptor.SetAttr("__get__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(frame));
    }
    [Fact]
    public void ClassAttributeAndExistingWrappersDoNotStandInForImplicitCell()
    {
        object frame = PyLexicalClassFactory.Prepare("C", "C", "source", []);
        PyLexicalClassFactory.NamespaceSet(frame, "__class__", "ordinary attribute");
        PyLexicalClassFactory.NamespaceSet(frame, "property", new PyProperty(PyNone.Instance));
        PyLexicalClassFactory.NamespaceSet(frame, "static", new PyStaticMethod(PyNone.Instance));
        PyLexicalClassFactory.NamespaceSet(frame, "class", new PyClassMethod(PyNone.Instance));
        var cls = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Equal("ordinary attribute", cls.Attributes["__class__"]);
        Assert.IsType<PyProperty>(cls.Attributes["property"]);
    }
}
