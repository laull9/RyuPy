// 构造期间的真实 CPython 重入：set_name、隐式 new、property 写删及已完成 Mixin 的继承方法。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    private static PyFunction ReentryFunction(string name, string[] parameters, Func<object?[], object?> body) =>
        new(name, PySignature.Create(name, parameters, parameters.Select(_ => "pos").ToArray(), parameters.Select(_ => "0").ToArray()),
            (_, args, _, _) => body(args), [], usesSuper: false);

    [Theory]
    [InlineData("EagerBase")]
    [InlineData("SetNameBase")]
    public void NativeConstruction_SetNameKeepsDescriptorOwnerCanonical(string baseName)
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        var descriptorClass = new PyClass("ConstructionDescriptor");
        object? observed = null;
        descriptorClass.SetAttr("__set_name__", ReentryFunction("__set_name__", ["self", "owner", "name"], args =>
        { observed = args[1]; return PyNone.Instance; }));
        PyOps.FinalizeOrdinaryClass(descriptorClass);
        object? descriptor = SuperCall(descriptorClass);
        var created = Assert.IsType<PyClass>(PyOps.PrepareRuntimeDerivedClass("SetNameOwner", [module.GetAttr(baseName)]));
        created.SetAttr("marker", descriptor);
        PyOps.FinalizeOrdinaryClass(created);
        Assert.Same(created, observed);
        PyModuleInstances.ClassCellGuard(created);
        Assert.Same(descriptor, SuperCall(module.GetAttr("echo"), descriptor));
        Assert.Equal(new[] { "True", "False", "False", "False" }, ClassStrings(SuperCall(module.GetAttr("descriptor_capabilities"), descriptor)));
        Assert.Equal(new[] { "False", "False", "False", "False" }, ClassStrings(SuperCall(module.GetAttr("descriptor_capabilities"), new PyInstance(new PyClass("Plain")))));
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
        object? first = SuperCall(created);
        Assert.IsType<PyInstance>(SuperCall(PyObject.GetAttr(first, "__new__"), created));
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

    [Fact]
    public void NativeConstruction_SameNameAndBasesWithoutFrozenMemberCannotClaimOwner()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        object? observed = null;
        object frame = PyLexicalClassFactory.Prepare("Decoy", "Decoy", "test", [module.GetAttr("DecoyBase")]);
        PyLexicalClassFactory.NamespaceSet(frame, "on_construct", new PyClassMethod(ReentryFunction("on_construct", ["cls"], args => observed = args[0])));
        var created = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.NotNull(observed);
        Assert.IsNotType<PyClass>(observed);
        Assert.NotSame(created, observed);
        Assert.Equal("Decoy", PyRepr.Str(PyObject.GetAttr(observed, "__name__")));
    }

    [Fact]
    public void NativeConstruction_PreexistingOpaqueIdentityIsNeverReplaced()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        object? observed = null;
        var observer = ReentryFunction("observe", ["cls"], args => { observed = args[0]; return PyNone.Instance; });
        SuperCall(module.GetAttr("set_identity_observer"), observer);
        object frame = PyLexicalClassFactory.Prepare("Conflicted", "Conflicted", "test", [module.GetAttr("IdentityConflictBase")]);
        PyLexicalClassFactory.NamespaceSet(frame, "on_construct", new PyClassMethod(ReentryFunction("on_construct", ["cls"], args => args[0])));
        var error = Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(frame));
        Assert.Contains("another type identity", error.Message);
        Assert.NotNull(observed);
        Assert.IsNotType<PyClass>(observed);
        Assert.Equal("Conflicted", PyRepr.Str(PyObject.GetAttr(observed, "__name__")));
        SuperCall(module.GetAttr("set_identity_observer"), PyNone.Instance);
    }


    [Fact]
    public void NativeConstruction_CachedPlainProxyCannotSilentlyGainSetName()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        var kind = new PyClass("LateDescriptor");
        PyOps.FinalizeOrdinaryClass(kind);
        var value = new PyInstance(kind);
        PyObject.SetAttr(value, "__set_name__", ReentryFunction("instance_only", ["owner", "name"], _ => PyNone.Instance));
        Assert.Same(value, SuperCall(module.GetAttr("retain"), value));
        try
        {
            Assert.Equal(new[] { "False", "False", "False", "False" }, ClassStrings(SuperCall(module.GetAttr("descriptor_capabilities"), value)));
            kind.SetAttr("__set_name__", ReentryFunction("__set_name__", ["self", "owner", "name"], _ => PyNone.Instance));
            var error = Assert.Throws<PyNotImplementedError>(() => SuperCall(module.GetAttr("echo"), value));
            Assert.Contains("preserving its identity", error.Message);
        }
        finally { SuperCall(module.GetAttr("clear_retained")); }
    }

    [Fact]
    public void NativeConstruction_SetNameDoesNotGrantUnbridgedDataDescriptorSlots()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        var kind = new PyClass("UnbridgedDescriptor");
        kind.SetAttr("__set_name__", ReentryFunction("__set_name__", ["self", "owner", "name"], _ => PyNone.Instance));
        kind.SetAttr("__get__", ReentryFunction("__get__", ["self", "instance", "owner"], _ => PyNone.Instance));
        PyOps.FinalizeOrdinaryClass(kind);
        var error = Assert.Throws<PyNotImplementedError>(() => SuperCall(module.GetAttr("echo"), new PyInstance(kind)));
        Assert.Contains("__get__/__set__/__delete__", error.Message);
    }


    [Fact]
    public void NativeConstruction_CachedDescriptorWithDeletedHookUsesCurrentTypeLookup()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        var kind = new PyClass("RemovedDescriptor");
        int calls = 0;
        kind.SetAttr("__set_name__", ReentryFunction("__set_name__", ["self", "owner", "name"], _ => { calls++; return PyNone.Instance; }));
        PyOps.FinalizeOrdinaryClass(kind);
        var value = new PyInstance(kind);
        Assert.Same(value, SuperCall(module.GetAttr("retain"), value));
        try
        {
            PyObject.DelAttr(kind, "__set_name__");
            var created = Assert.IsType<PyClass>(PyOps.PrepareRuntimeDerivedClass("AfterRemoval", [module.GetAttr("EagerBase")]));
            created.SetAttr("marker", value);
            PyOps.FinalizeOrdinaryClass(created);
            Assert.Equal(0, calls);
            Assert.Same(value, SuperCall(module.GetAttr("echo"), value));
        }
        finally { SuperCall(module.GetAttr("clear_retained")); }
    }

}
