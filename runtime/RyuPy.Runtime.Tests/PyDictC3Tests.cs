// dict混合族的C3节点、唯一payload及私有frame验证；不以成功创建类替代真实成员优先级。
using System;
using System.Linq;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyDictC3Tests
{
    private static PyClass Plain(string name)
    { var cls = (PyClass)PyOps.MakeClass(name, [], null); PyOps.FinalizeOrdinaryClass(cls); return cls; }
    private static PyClass Mix(string name, params object?[] bases)
    { var cls = (PyClass)PyOps.PrepareOrdinaryMiClass(name, bases); PyOps.FinalizeOrdinaryClass(cls); return cls; }
    private static string[] Names(PyClass cls) => cls.MroTuple().Items.Select(v => v is PyClass c ? c.Name : ((PyType)v!).Name).ToArray();
    [Fact]
    public void NativePositionAndSolidBaseAreIndependent()
    {
        var mixin = Plain("Mixin");
        PyAbc.Method(mixin, "__iter__", ["self"], _ => PyIter.Create(new PyTuple(["mixin"])));
        PyAbc.Method(mixin, "__init__", ["self", "source"], a => { PyOps.SetItem(a[0], "mixin", 3L); return PyNone.Instance; });
        var first = Mix("First", PyType.Get("dict"), mixin); var second = Mix("Second", mixin, PyType.Get("dict"));
        Assert.Equal(new[] { "First", "dict", "Mixin", "object" }, Names(first));
        Assert.Equal(new[] { "Second", "Mixin", "dict", "object" }, Names(second));
        Assert.Same(PyType.Get("dict"), PyObject.GetAttr(first, "__base__")); Assert.Same(PyType.Get("dict"), PyObject.GetAttr(second, "__base__"));
        var data = new PyDict(); data.Set("root", 2L);
        var a = Assert.IsType<PyDictInstance>(first.Invoke([data], [], [])); var b = Assert.IsType<PyDictInstance>(second.Invoke([data], [], []));
        Assert.Equal(new object?[] { "root" }, PyOps.ToSequence(a)); Assert.Equal(new object?[] { "mixin" }, PyOps.ToSequence(b));
        Assert.True(first.TryResolveDictC3("__iter__", out var root)); Assert.Same(PyType.Get("dict"), root.Owner);
        Assert.False(first.TryLookup("__iter__", out _, out _)); Assert.True(second.TryLookup("__iter__", out var owner, out _)); Assert.Same(mixin, owner);
    }
    [Fact]
    public void NativeMethodMasksLaterPropertyButInstanceAttributeStillWins()
    {
        var mixin = Plain("Mixin"); var getter = PyAbc.Method(mixin, "getter", ["self"], _ => "property"); mixin.SetAttr("keys", new PyProperty(getter));
        var first = Mix("First", PyType.Get("dict"), mixin); var second = Mix("Second", mixin, PyType.Get("dict"));
        var a = first.Invoke([], [], []); var b = second.Invoke([], [], []);
        Assert.Equal("property", PyObject.GetAttr(b, "keys"));
        Assert.Empty(PyOps.ToSequence(PyOps.CallValue(PyObject.GetAttr(a, "keys"), [], [], [])));
        PyObject.SetAttr(a, "keys", "instance"); Assert.Equal("instance", PyObject.GetAttr(a, "keys"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(b, "keys", "blocked"));
    }
    [Fact]
    public void IndependentUserDictBasesShareOneNativeSolidLayout()
    {
        var a = (PyClass)PyOps.MakeClass("A", [PyType.Get("dict")], null); PyOps.FinalizeOrdinaryClass(a);
        var b = (PyClass)PyOps.MakeClass("B", [PyType.Get("dict")], null); PyOps.FinalizeOrdinaryClass(b);
        var combined = Mix("Combined", a, b);
        Assert.Equal(new[] { "Combined", "A", "B", "dict", "object" }, Names(combined)); Assert.Same(a, PyObject.GetAttr(combined, "__base__"));
        var value = Assert.IsType<PyDictInstance>(combined.Invoke([], [], [])); PyOps.SetItem(value, "x", 1L);
        Assert.Equal(1L, PyOps.GetItem(value, "x")); Assert.Equal(1, value.Payload.Count);
    }
    [Fact]
    public void FrameDefersConflictsAndFreezesAllBases()
    {
        var mixin = Plain("Mixin"); object?[] bases = [PyType.Get("dict"), mixin];
        object frame = PyLexicalClassFactory.Prepare("Local", "f.<locals>.Local", "test", bases); bases[0] = PyType.Get("list");
        PyLexicalClassFactory.NamespaceSet(frame, "body", 1L); var cls = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Same(PyType.Get("dict"), cls.DirectBasesTuple().Items[0]); Assert.Equal(1L, cls.Attributes["body"]);
        foreach (object?[] conflict in new object?[][] { [PyType.Get("dict"), PyType.Get("list")], [PyType.Get("dict"), PyType.Get("dict")] })
        {
            object bad = PyLexicalClassFactory.Prepare("Bad", "Bad", "test", conflict); PyLexicalClassFactory.NamespaceSet(bad, "body", 2L);
            Assert.Throws<PyTypeError>(() => PyLexicalClassFactory.Finish(bad)); Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.Finish(bad));
        }
    }
    [Fact]
    public void SuperBindsNativeDescriptorOnlyForInstanceReceiver()
    {
        var mixin = Plain("Mixin"); var cls = Mix("C", mixin, PyType.Get("dict")); var value = cls.Invoke([], [], []);
        object? bound = PyObject.GetAttr(new PySuper(mixin, value), "__init__"); var data = new PyDict(); data.Set("x", 2L);
        PyOps.CallValue(bound, [data], [], []); Assert.Equal(2L, PyOps.GetItem(value, "x"));
        object? unbound = PyObject.GetAttr(new PySuper(mixin, cls), "__init__");
        Assert.Same(PyDictDescriptors.Unbound("__init__"), unbound);
        PyOps.CallValue(unbound, [value, data], [], []);
    }
    [Fact]
    public void LateMixinMutationIsRevalidatedBeforeLookup()
    {
        var mixin = Plain("Mixin"); var cls = Mix("C", PyType.Get("dict"), mixin); var value = cls.Invoke([], [], []);
        _ = PyObject.GetAttr(value, "keys"); mixin.SetAttr("__getattribute__", PyNone.Instance);
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(value, "keys"));
        mixin.DeleteAttr("__getattribute__"); Assert.NotNull(PyObject.GetAttr(value, "keys"));
    }
    [Fact]
    public void CapabilityGuardAndSavedNativeBindingCannotBypassValidation()
    {
        var mixin = Plain("Mixin");
        Assert.NotNull(PyLexicalClassFactory.PrepareForMode("Plain", "Plain", "m", [mixin], false));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.PrepareForMode("Mixed", "Mixed", "m", [mixin, PyType.Get("dict")], false));
        var cls = Mix("C", mixin, PyType.Get("dict"));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.PrepareForMode("Child", "Child", "m", [cls], false));
        var value = cls.Invoke([], [], []); object? saved = PyObject.GetAttr(new PySuper(mixin, value), "__init__");
        object? fromkeys = PyObject.GetAttr(new PySuper(mixin, cls), "fromkeys");
        var made = Assert.IsType<PyDictInstance>(PyOps.CallValue(fromkeys, [new PyTuple(["key"]), 3L], [], []));
        Assert.Same(cls, made.Class); Assert.Equal(3L, PyOps.GetItem(made, "key"));
        var descriptor = Plain("Descriptor"); PyAbc.Method(descriptor, "__get__", ["self", "value", "owner"], _ => 1L);
        mixin.SetAttr("bad", descriptor.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(new PySuper(mixin, value), "__init__"));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(saved, [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyDictLayout.New([cls], [], []));
    }

}
