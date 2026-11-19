// 显式super的双构造入口、真实身份、属性预捕获与直接PIC失效合同。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyExplicitSuperTests
{
    private static PyClass Child(string name, PyClass parent) => (PyClass)PyOps.MakeClass(name, [parent], null);
    private static PyFunction Function(string name, PyCall body) =>
        new(name, PySignature.Create(name, [], [], []), body, [], false);
    private static object? Construct(bool legacy, object?[] args, string[]? names = null) => legacy
        ? PyOps.CallBuiltin("super", args, names ?? [], names is null ? [] : [1L])
        : PyOps.CallValue(PyLexicalSuper.Type, args, names ?? [], names is null ? [] : [1L]);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothEntriesUseExplicitStartAndReceiver(bool legacy)
    {
        var root = new PyClass("Root"); var middle = Child("Middle", root); var leaf = Child("Leaf", middle);
        var first = new PyInstance(leaf); var second = new PyInstance(leaf);
        root.SetAttr("read", Function("read", (_, args, _, _) => args[0]));
        middle.SetAttr("read", Function("read", (_, _, _, _) => 42L));
        object? proxy = Construct(legacy, [middle, first]);
        Assert.Same(first, PyOps.CallMethod(proxy, "read", [], [], []));
        Assert.Same(second, PyOps.CallMethod(Construct(legacy, [middle, second]), "read", [], [], []));
        Assert.Equal(42L, PyOps.CallMethod(Construct(legacy, [leaf, first]), "read", [], [], []));
        Assert.Same(PyLexicalSuper.Type, PyObject.TypeOf(proxy));
        Assert.Throws<PyTypeError>(() => Construct(legacy, [new PyClass("Middle"), first]));
        Assert.Throws<PyTypeError>(() => Construct(legacy, [middle, new PyInstance(root)]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SignatureErrorsAndLegalUnsupportedFormsAreDistinct(bool legacy)
    {
        var cls = new PyClass("C"); var self = new PyInstance(cls);
        Assert.Equal("super() takes no keyword arguments", Assert.Throws<PyTypeError>(() => Construct(legacy, [1L, 2L, 3L], ["x"])).Message);
        Assert.Equal("super() expected at most 2 arguments, got 3", Assert.Throws<PyTypeError>(() => Construct(legacy, [1L, 2L, 3L])).Message);
        Assert.Equal("super() argument 1 must be a type, not int", Assert.Throws<PyTypeError>(() => Construct(legacy, [1L, self])).Message);
        Assert.Throws<PyTypeError>(() => Construct(legacy, [1L]));
        Assert.Throws<PyTypeError>(() => Construct(legacy, [cls, 1L]));
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [cls]));
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [cls, PyNone.Instance]));
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [cls, cls]));
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [PyBaseObject.Type, self]));
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [PyLexicalSuper.Type, self]));
        var list = (PyClass)PyOps.MakeClass("L", [PyType.Get("list")], null);
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [list, new PyInstance(list)]));
    }

    [Fact]
    public void ActualRelatedClassDoesNotReadProxyClassGetter()
    {
        var root = new PyClass("Root"); var cls = Child("C", root);
        int gets = 0;
        cls.SetAttr("__class__", new PyProperty(Function("get", (_, _, _, _) => { gets++; return root; })));
        Assert.IsType<PySuper>(Construct(false, [cls, new PyInstance(cls)]));
        Assert.Equal(0, gets);
        Assert.Throws<PyNotImplementedError>(() => Construct(false, [new PyClass("Other"), new PyInstance(cls)]));
        Assert.Equal(0, gets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteChainsRejectSpecialAncestorsAndReceiverSubclasses(bool legacy)
    {
        var nativeRoot = (PyClass)PyOps.MakeClass("ListRoot", [PyType.Get("list")], null);
        var derived = Child("Derived", nativeRoot);
        // 即使start自身命中，也不能跳过其后面的特殊祖先。
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [derived, new PyInstance(derived)]));
        var ordinary = new PyClass("Ordinary");
        var special = Child("SpecialReceiver", ordinary);
        special.ExceptionFactory = (_, _) => new PyValueError("unused");
        Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [ordinary, new PyInstance(special)]));
        foreach (string marker in new[] { "DataclassActive", "IsNativeRuntimeClass" })
        {
            var marked = Child("Marked", ordinary);
            // 只在测试设置内部专用布局标记；产品构造/查找没有新增反射。
            typeof(PyClass).GetProperty(marker, System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic)!.SetValue(marked, true);
            Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [marked, new PyInstance(marked)]));
            Assert.Throws<PyNotImplementedError>(() => Construct(legacy, [ordinary, new PyInstance(marked)]));
        }
    }

    [Fact]
    public void CustomDescriptorFailsOnlyWhenConsumedWithoutInvokingGetter()
    {
        var descriptor = new PyClass("Descriptor");
        int calls = 0;
        descriptor.SetAttr("__get__", Function("get", (_, _, _, _) => { calls++; return 1L; }));
        var root = new PyClass("Root"); var cls = Child("C", root);
        root.SetAttr("value", new PyInstance(descriptor));
        object? proxy = Construct(false, [cls, new PyInstance(cls)]);
        Assert.IsType<PySuper>(proxy);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CaptureMethodTarget(proxy, "value"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(proxy, "value"));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(1771)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void DirectPicUsesCurrentSelfAndInvalidatesWholeAncestorChain(int site)
    {
        var root = new PyClass("Root"); var middle = Child("Middle", root); var leaf = Child("Leaf", middle);
        var x = new PyInstance(leaf); var y = new PyInstance(leaf);
        root.SetAttr("read", Function("root", (_, args, _, _) => args[0]));
        middle.SetAttr("read", Function("middle", (_, args, _, _) => args[0]));
        object? px = Construct(false, [leaf, x]); object? py = Construct(false, [leaf, y]);
        object? saved = PyObject.GetAttr(px, "read");
        for (int i = 0; i < 3; i++)
        {
            Assert.Same(x, PyPic.CallMethod0(site, px, "read"));
            Assert.Same(y, PyPic.CallMethod0(site, py, "read"));
        }
        middle.SetAttr("read", Function("new", (_, _, _, _) => 42L));
        Assert.Equal(42L, PyPic.CallMethod0(site, px, "read"));
        Assert.Equal(42L, PyPic.CallMethod0(site, py, "read"));
        Assert.Same(x, PyOps.CallValue(saved, [], [], []));
        Assert.True(middle.DeleteAttr("read"));
        Assert.Same(x, PyPic.CallMethod0(site, px, "read"));
        Assert.Same(y, PyPic.CallMethod0(site, py, "read"));
        Assert.True(root.DeleteAttr("read"));
        Assert.Throws<PyAttributeError>(() => PyPic.CallMethod0(site, px, "read"));
        root.SetAttr("read", new PyProperty(Function("property", (_, _, _, _) => Function("call", (_, _, _, _) => 7L))));
        Assert.Equal(7L, PyPic.CallMethod0(site, px, "read"));
    }

    [Fact]
    public void CapturedTargetSurvivesRebindingAndDeletionWithoutCheckingCallable()
    {
        var root = new PyClass("Root"); var cls = Child("C", root); var self = new PyInstance(cls);
        root.SetAttr("f", Function("old", (_, args, _, _) => args[0]));
        object? proxy = Construct(false, [cls, self]);
        Assert.True(PyOps.RequiresEarlyMethodCapture(proxy));
        Assert.True(PyOps.IsModule(proxy)); // 历史ABI也是捕获判据，不是类型判断。
        object? target = PyOps.CaptureMethodTarget(proxy, "f");
        root.SetAttr("f", Function("new", (_, _, _, _) => 9L));
        Assert.Same(self, PyPic.CallCapturedMethod0(1772, true, target, "f"));
        target = PyOps.CaptureModuleTarget(proxy, "f");
        Assert.True(root.DeleteAttr("f"));
        Assert.Equal(9L, PyPic.CallCapturedMethod0(1772, true, target, "f"));
        Assert.Throws<PyAttributeError>(() => PyOps.CaptureMethodTarget(proxy, "f"));
        root.SetAttr("f", 42L);
        Assert.Equal(42L, PyOps.CaptureMethodTarget(proxy, "f"));
        Assert.Throws<PyTypeError>(() => PyPic.CallCapturedMethod0(1772, true, 42L, "f"));
        Assert.False(PyOps.RequiresEarlyMethodCapture(self));
        Assert.Same(self, PyOps.CaptureMethodTarget(self, "missing"));
    }
}
