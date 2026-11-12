// 有限slots布局的存储权威、字典别名、描述符优先级与缓存回退合同。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyOrdinarySlotsTests
{
    private static PyClass Make(string name, bool empty, params object?[] bases)
    {
        var cls = new PyClass(name);
        cls.PrepareMultipleBases(bases);
        if (empty) cls.Attributes["__slots__"] = new PyTuple([]);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static PyClass Storage(string name)
    {
        var cls = new PyClass(name);
        cls.PrepareMultipleBases([]);
        cls.ConfigureNativeSlots(["_mapping"]);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    [Fact]
    public void EmptySlotsAndRestoredDictionaryHaveRealAliases()
    {
        var empty = Make("Empty", true);
        var e = new PyInstance(empty);
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(e, "__dict__"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(e, "field", 1L));
        var c = new PyInstance(Make("Child", false, empty));
        var dict = Assert.IsType<PyDict>(PyObject.GetAttr(c, "__dict__"));
        PyObject.SetAttr(c, "field", 1L); Assert.Equal(1L, dict.Get("field"));
        dict.Set("field", 2L); Assert.Equal(2L, PyObject.GetAttr(c, "field"));
        dict.Delete("field"); Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(c, "field"));
        dict.Set("__dict__", 3L); Assert.Same(dict, PyObject.GetAttr(c, "__dict__"));
        var replacement = new PyDict(); replacement.Set("field", 4L);
        PyObject.SetAttr(c, "__dict__", replacement); Assert.Same(replacement, PyObject.GetAttr(c, "__dict__"));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(c, "__dict__", 5L));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(c, "__dict__", new PyDictInstance(new PyClass("DictChild"))));
        PyObject.DelAttr(c, "__dict__"); Assert.Equal(0, Assert.IsType<PyDict>(PyObject.GetAttr(c, "__dict__")).Count);
        Assert.Equal(4L, replacement.Get("field"));
    }
    [Fact]
    public void NativeStorageIsSeparateAndDescriptorRebindingIsObserved()
    {
        var root = Storage("View"); var cls = Make("Child", false, root); var instance = new PyInstance(cls);
        var dict = Assert.IsType<PyDict>(PyObject.GetAttr(instance, "__dict__"));
        dict.Set("_mapping", "shadow");
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "_mapping"));
        PyPic.SetAttr(70, instance, "_mapping", 9L); Assert.Equal(9L, PyPic.GetAttr(71, instance, "_mapping"));
        Assert.Equal("shadow", dict.Get("_mapping"));
        var slot = Assert.IsType<PyInstanceSlotDescriptor>(root.Attributes["_mapping"]);
        Assert.Throws<PyTypeError>(() => slot.Read(new PyInstance(Make("Other", false))));
        Assert.Equal(9L, slot.Bind(root, instance));
        Assert.Equal(9L, PyObject.GetAttr(new PySuper(cls, instance), "_mapping"));
        PyObject.DelAttr(instance, "_mapping"); Assert.Throws<PyAttributeError>(() => slot.Read(instance));
        slot.Write(instance, 10L); root.SetAttr("_mapping", "class");
        Assert.Equal("shadow", PyObject.GetAttr(instance, "_mapping"));
        dict.Delete("_mapping"); Assert.Equal("class", PyObject.GetAttr(instance, "_mapping"));
    }
    [Fact]
    public void MethodCacheCannotHideDirectDictionaryMutations()
    {
        var cls = Make("Child", false, Make("Empty", true));
        cls.SetAttr("method", PyNativeFunctions.Create("method", ["self"], [], 1, _ => 1L));
        var instance = new PyInstance(cls);
        for (int i = 0; i < 5; i++) Assert.Equal(1L, PyPic.CallMethod0(72, instance, "method"));
        var dict = Assert.IsType<PyDict>(PyObject.GetAttr(instance, "__dict__"));
        dict.Set("method", PyNativeFunctions.Create("shadow", [], [], 0, _ => 2L));
        Assert.Equal(2L, PyPic.CallMethod0(72, instance, "method"));
        dict.Delete("method"); Assert.Equal(1L, PyPic.CallMethod0(72, instance, "method"));
    }
    [Fact]
    public void FrozenLayoutsPreserveDictionaryAncestorAndRejectConflicts()
    {
        var empty = Make("Empty", true); empty.SetAttr("__slots__", new PyTuple(["late"]));
        var child = new PyInstance(Make("StillEmpty", true, empty));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(child, "late", 1L));
        var dictAncestor = Make("Plain", false);
        var retained = new PyInstance(Make("Retained", true, dictAncestor));
        PyObject.SetAttr(retained, "value", 2L);
        Assert.Equal(2L, Assert.IsType<PyDict>(PyObject.GetAttr(retained, "__dict__")).Get("value"));
        Assert.Throws<PyTypeError>(() => Make("Conflict", true, Storage("One"), Storage("Two")));
        var unsupported = new PyClass("Unsupported"); unsupported.Attributes["__slots__"] = new PyTuple(["value"]);
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(unsupported));
        Assert.Throws<PyNotImplementedError>(() => empty.SetAttr("__dict__", new PyDict()));
    }
}
