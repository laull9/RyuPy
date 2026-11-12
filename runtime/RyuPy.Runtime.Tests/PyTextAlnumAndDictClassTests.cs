// 真实dict派生载体的类属性遮蔽，以及固定Unicode表isalnum的四入口合同。
namespace RyuPy.Runtime.Tests;

public class PyTextAlnumAndDictClassTests
{
    private static PyClass DictClass(string name)
        => (PyClass)PyOps.MakeClass(name, [PyType.Get("dict")], null);
    private static PyDictInstance Instance(PyClass cls)
    {
        PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsType<PyDictInstance>(PyOps.CallValue(cls, [], [], []));
    }

    [Fact]
    public void DictClassRootPropertyAndOrdinaryOverrideKeepRealType()
    {
        var plain = DictClass("D"); var d = Instance(plain);
        Assert.Same(plain, PyObject.GetAttr(d, "__class__"));
        var masked = DictClass("Mask");
        masked.SetAttr("__class__", new PyProperty(PyNativeFunctions.CreateVariadic("getclass", a =>
        { Assert.IsType<PyDictInstance>(a[0]); return "masked"; })));
        var value = Instance(masked);
        Assert.Equal("masked", PyObject.GetAttr(value, "__class__"));
        Assert.Same(masked, PyObject.TypeOf(value));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, "__class__", plain));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(d, "__class__", masked));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(d, "__class__"));
        var child = (PyClass)PyOps.MakeClass("ChildMask", [masked], null);
        var inherited = Instance(child);
        Assert.Equal("masked", PyObject.GetAttr(inherited, "__class__"));
        Assert.Same(child, PyObject.TypeOf(inherited));
        var explicitClass = DictClass("Explicit"); explicitClass.SetAttr("__class__", PyNone.Instance);
        var explicitValue = Instance(explicitClass);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(explicitValue, "__class__"));
        Assert.Same(explicitClass, PyObject.TypeOf(explicitValue));
        explicitClass.SetAttr("__class__", "late");
        Assert.Equal("late", PyObject.GetAttr(explicitValue, "__class__"));
    }

    [Fact]
    public void RealDictPropertySetterDeleterAndC3FieldShadowDoNotChangeLayout()
    {
        var cls = DictClass("WritableMask");
        cls.SetAttr("__class__", new PyProperty(
            PyNativeFunctions.CreateVariadic("getter", a => Assert.IsType<PyDictInstance>(a[0]).Payload.Get("v")),
            PyNativeFunctions.CreateVariadic("setter", a =>
            { Assert.IsType<PyDictInstance>(a[0]).Payload.Set("v", a[1]); return PyNone.Instance; }),
            PyNativeFunctions.CreateVariadic("deleter", a =>
            { Assert.IsType<PyDictInstance>(a[0]).Payload.Set("v", "deleted"); return PyNone.Instance; })));
        var value = Instance(cls);
        PyObject.SetAttr(value, "__class__", "written");
        Assert.Equal("written", PyObject.GetAttr(value, "__class__"));
        PyObject.DelAttr(value, "__class__");
        Assert.Equal("deleted", PyObject.GetAttr(value, "__class__"));
        Assert.Same(cls, PyObject.TypeOf(value));

        var mixin = (PyClass)PyOps.MakeClass("ClassMetadataMixin", [], null);
        mixin.SetAttr("__class__", "mixin"); PyOps.FinalizeOrdinaryClass(mixin);
        var derived = (PyClass)PyOps.PrepareOrdinaryMiClass("DictWithMixin", [mixin, PyType.Get("dict")]);
        var mixed = Instance(derived);
        Assert.Equal("mixin", PyObject.GetAttr(mixed, "__class__"));
        PyObject.SetAttr(mixed, "__class__", "own");
        Assert.Equal("own", PyObject.GetAttr(mixed, "__class__"));
        PyObject.DelAttr(mixed, "__class__");
        Assert.Equal("mixin", PyObject.GetAttr(mixed, "__class__"));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(mixed, "__class__"));
        Assert.Same(derived, PyObject.TypeOf(mixed));
    }

    [Theory]
    [InlineData("abc123", true)]
    [InlineData("١٢三²½𝟙𐐀", true)]
    [InlineData("", false)]
    [InlineData("_", false)]
    [InlineData("a b", false)]
    [InlineData("a\u0301", false)]
    [InlineData("😀", false)]
    [InlineData("\ud800", false)]
    public void ClassificationSharesFourRoutes(string text, bool expected)
    {
        Assert.Equal(expected, PyOps.CallMethod(text, "isalnum", [], [], []));
        Assert.Equal(expected, PyOps.CallValue(PyObject.GetAttr(text, "isalnum"), [], [], []));
        Assert.Equal(expected, PyOps.CallValue(PyOps.PrepareMethod(text, "isalnum"), [], [], []));
        Assert.Equal(expected, PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), "isalnum"), [text], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(text, "isalnum", [1L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(text, "isalnum", [], ["x"], [1L]));
    }

    [Fact]
    public void AllCodepointsUseThePinnedTableWithoutSurrogateReplacement()
    {
        int[] ranges = PyRegexUnicodeData.Word;
        int range = 0;
        for (int cp = 0; cp <= 0x10ffff; cp++)
        {
            while (range < ranges.Length && cp > ranges[range + 1]) range += 2;
            bool expected = cp != '_' && range < ranges.Length && cp >= ranges[range];
            Assert.Equal(expected, PyTextAlnum.Contains(cp));
        }
    }
}
