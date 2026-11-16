// 构造期间的真实 CPython 重入：set_name、隐式 new、property 写删及已完成 Mixin 的继承方法。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    private static PyFunction ReentryFunction(string name, string[] parameters, Func<object?[], object?> body) =>
        new(name, PySignature.Create(name, parameters, parameters.Select(_ => "pos").ToArray(), parameters.Select(_ => "0").ToArray()),
            (_, args, _, _) => body(args), [], usesSuper: false);

    [Fact]
    public void NativeConstruction_SetNameKeepsDescriptorOwnerCanonical()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        var descriptorClass = new PyClass("ConstructionDescriptor");
        object? observed = null;
        descriptorClass.SetAttr("__set_name__", ReentryFunction("__set_name__", ["self", "owner", "name"], args =>
        { observed = args[1]; return PyNone.Instance; }));
        PyOps.FinalizeOrdinaryClass(descriptorClass);
        object? descriptor = SuperCall(descriptorClass);
        var created = Assert.IsType<PyClass>(PyOps.PrepareRuntimeDerivedClass("SetNameOwner", [module.GetAttr("SetNameBase")]));
        created.SetAttr("marker", descriptor);
        PyOps.FinalizeOrdinaryClass(created);
        Assert.Same(created, observed);
    }

    [Fact]
    public void NativeConstruction_ImplicitNewKeepsClassCanonical()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        object? observed = null;
        object frame = PyLexicalClassFactory.Prepare("EagerNew", "EagerNew", "test", [module.GetAttr("EagerBase")]);
        PyLexicalClassFactory.NamespaceSet(frame, "eager_new", true);
        PyLexicalClassFactory.NamespaceSet(frame, "__new__", ReentryFunction("__new__", ["cls"], args =>
        { observed = args[0]; return SuperCall(PyObject.GetAttr(PyBaseObject.Type, "__new__"), args[0]); }));
        var created = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Same(created, observed);
    }

    [Theory]
    [InlineData("setter")]
    [InlineData("deleter")]
    public void NativeConstruction_PropertyWriteAndDeleteKeepReceiverCanonical(string mode)
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        object? observed = null;
        var setter = ReentryFunction("set_value", ["self", "value"], args => { observed = args[0]; return PyNone.Instance; });
        var deleter = ReentryFunction("del_value", ["self"], args => { observed = args[0]; return PyNone.Instance; });
        object frame = PyLexicalClassFactory.Prepare("EagerProperty", "EagerProperty", "test", [module.GetAttr("EagerBase")]);
        PyLexicalClassFactory.NamespaceSet(frame, "eager_property", mode);
        PyLexicalClassFactory.NamespaceSet(frame, "value", new PyProperty(null, setter, deleter));
        var created = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Same(created, Assert.IsType<PyInstance>(observed).Class);
    }

    [Fact]
    public void NativeConstruction_InheritedMixinMethodKeepsClassCanonical()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        object? observed = null;
        var mixin = new PyClass("ConstructionMixin");
        mixin.SetAttr("inherited_callback", new PyClassMethod(ReentryFunction("inherited_callback", ["cls"], args => observed = args[0])));
        PyOps.FinalizeOrdinaryClass(mixin);
        object frame = PyLexicalClassFactory.Prepare("EagerInherited", "EagerInherited", "test", [mixin, module.GetAttr("EagerBase")]);
        PyLexicalClassFactory.NamespaceSet(frame, "eager_inherited", true);
        var created = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Same(created, observed);
    }
}
