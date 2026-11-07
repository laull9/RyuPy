// 默认 CP3.12 加速 Element 的原值/别名/身份/绑定/游标回归；不把未开放 XML API 算入通过。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyXmlElementTests
{
    private static object? Call(object? f, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(f, args, names ?? [], values ?? []);
    private static PyXmlElement Element(object? tag, PyDict? attributes = null) =>
        Assert.IsType<PyXmlElement>(Call(PyXmlElementTree.ElementType, attributes is null ? [tag] : [tag, attributes]));
    private static object? Method(object? obj, string name, params object?[] args) => Call(PyObject.GetAttr(obj, name), args);

    [Fact]
    public void TypesStayStableAcrossAttributesAndModuleImports()
    {
        var first = PyXmlElementTree.CreateAttributes();
        var e = Element("root");
        var second = PyXmlElementTree.CreateAttributes();
        Assert.Same(first["Element"], second["Element"]);
        Assert.Same(first["QName"], second["QName"]);
        Assert.Same(first["ElementTree"], second["ElementTree"]);
        Assert.Same(first["Element"], PyObject.TypeOf(e));
        PyNativeModules.Register();
        var module = PyModules.Import("xml.etree.ElementTree");
        Assert.Same(module, PyModules.Import("xml.etree.ElementTree"));
        Assert.Same(module, PyModules.Import("xml.etree").GetAttr("ElementTree"));
        Assert.Same(PyModules.Import("xml.etree"), PyModules.Import("xml").GetAttr("etree"));
    }

    [Fact]
    public void ConstructorCopiesDictionaryButPreservesValuesAndOrder()
    {
        var value = new PyList(); var source = new PyDict(); source.Set("a", value); source.Set("b", 2L);
        var e = Assert.IsType<PyXmlElement>(Call(PyXmlElementTree.ElementType, ["x", source], ["a", "c"], [value, 3L]));
        var actual = Assert.IsType<PyDict>(e.GetAttr("attrib"));
        Assert.NotSame(source, actual); Assert.Same(value, actual.Get("a"));
        Assert.Equal(new object?[] { "a", "b", "c" }, actual.Keys);
        source.Set("late", true); Assert.False(actual.TryGet("late", out _));
        var replacement = new PyDict(); replacement.Set("z", 4L); e.SetAttr("attrib", replacement);
        Assert.Same(replacement, e.GetAttr("attrib")); Method(e, "set", "next", value);
        Assert.Same(value, replacement.Get("next"));
    }

    [Fact]
    public void CConstructorPositionOnlyTagAndAttribKeywordBranchAreDistinct()
    {
        var input = new PyDict(); input.Set("x", 1L);
        var e = Assert.IsType<PyXmlElement>(Call(PyXmlElementTree.ElementType, ["root"], ["attrib", "tag"], [input, "attr"]));
        Assert.Equal("root", e.GetAttr("tag")); Assert.Equal("attr", Method(e, "get", "tag"));
        Assert.Equal(1L, Method(e, "get", "x"));
        var f = Assert.IsType<PyXmlElement>(Call(PyXmlElementTree.ElementType, ["root", new PyDict()], ["attrib"], [input]));
        Assert.Same(input, Method(f, "get", "attrib"));
        Assert.Equal("Element() takes at least 1 argument (0 given)", Assert.Throws<PyTypeError>(() =>
            Call(PyXmlElementTree.ElementType, [], ["tag"], ["x"])).PyMessage);
    }

    [Fact]
    public void ArbitraryTagTextAndTailRetainReferences()
    {
        var marker = new PyList(); var e = Element(marker);
        e.SetAttr("text", marker); e.SetAttr("tail", marker);
        Assert.Same(marker, e.GetAttr("tag")); Assert.Same(marker, e.GetAttr("text")); Assert.Same(marker, e.GetAttr("tail"));
        foreach (string field in new[] { "tag", "text", "tail", "attrib" })
            Assert.Equal("can't delete element attribute", Assert.Throws<PyAttributeError>(() => e.DelAttr(field)).PyMessage);
    }

    [Fact]
    public void NodesAreSharedAndIdentityHashKeys()
    {
        var left = Element("left"); var right = Element("right"); var child = Element("child");
        Method(left, "append", child); Method(left, "append", child); Method(right, "insert", 0L, child);
        Assert.Same(child, PyOps.GetItem(left, -1L)); Assert.Same(child, PyOps.GetItem(right, 0L));
        Assert.Equal(2L, Method(left, "__len__"));
        var map = new PyDict(); map.Set(child, left); Assert.Same(left, map.Get(PyOps.GetItem(right, 0L)));
        Assert.False(PyHash.Equal(child, Element("child")));
    }

    [Fact]
    public void SliceIsNewListWithOriginalReferencesAndWideIntegers()
    {
        var e = Element("r"); var a = Element("a"); var b = Element("b"); Method(e, "append", a); Method(e, "append", b);
        var slice = Assert.IsType<PyList>(PyOps.GetItem(e, new PySlice(null, null, -1L)));
        Assert.Same(b, slice.Items[0]); Assert.Same(a, slice.Items[1]); slice.Items.Clear();
        Assert.Equal(2L, Method(e, "__len__"));
        var wide = System.Numerics.BigInteger.One << 100;
        Assert.Single(Assert.IsType<PyList>(PyOps.GetItem(e, new PySlice(null, null, wide))).Items);
        Assert.Throws<PyIndexError>(() => PyOps.GetItem(e, wide));
        Assert.Throws<PyValueError>(() => PyOps.GetItem(e, new PySlice("bad", null, 0L)));
    }

    [Fact]
    public void ChildrenIterationObservesAppendAndStaysExhausted()
    {
        var e = Element("r"); var a = Element("a"); Method(e, "append", a);
        Assert.Throws<PyAttributeError>(() => e.GetAttr("__iter__"));
        foreach (string name in new[] { "Iterable", "Iterator", "Sequence", "Container" })
            Assert.False(Assert.IsType<PyNativeType>(PyAbc.CreateAttributes()[name]).Accepts(e));
        Assert.True(Assert.IsType<PyNativeType>(PyAbc.CreateAttributes()["Sized"]).Accepts(e));
        var iterator = PyIter.Create(e); Assert.Same(iterator, PyIter.Create(iterator));
        Assert.True(iterator.MoveNext()); Assert.Same(a, iterator.Current());
        var b = Element("b"); Method(e, "append", b); Assert.True(iterator.MoveNext()); Assert.Same(b, iterator.Current());
        Assert.False(iterator.MoveNext()); Method(e, "append", a); Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void PreorderVisitsSharedChildrenAndRejectsActualCycles()
    {
        var r = Element("r"); var a = Element("a"); var b = Element("b");
        Method(r, "append", a); Method(a, "append", b); Method(r, "append", b);
        var iterator = Assert.IsAssignableFrom<IPyIterator>(Method(r, "iter"));
        var seen = new List<object?>(); while (iterator.MoveNext()) seen.Add(PyObject.GetAttr(iterator.Current(), "tag"));
        Assert.Equal(new object?[] { "r", "a", "b", "b" }, seen);
        var filtered = Assert.IsAssignableFrom<IPyIterator>(Method(r, "iter", "b"));
        Assert.True(filtered.MoveNext()); Assert.Same(b, filtered.Current()); Assert.True(filtered.MoveNext()); Assert.False(filtered.MoveNext());
        Method(b, "append", r); var cycle = Assert.IsAssignableFrom<IPyIterator>(Method(r, "iter"));
        Assert.True(cycle.MoveNext()); Assert.True(cycle.MoveNext()); Assert.True(cycle.MoveNext());
        Assert.Throws<PyNotImplementedError>(() => cycle.MoveNext());
    }

    [Fact]
    public void KeysAndItemsAreSnapshotsWhileGetDefaultPreservesIdentity()
    {
        var e = Element("r"); var marker = new PyList(); Method(e, "set", "a", marker);
        var keys = Assert.IsType<PyList>(Method(e, "keys")); var items = Assert.IsType<PyList>(Method(e, "items"));
        Method(e, "set", "b", 2L); Method(e, "set", "a", 3L);
        Assert.Equal(new object?[] { "a" }, keys.Items);
        Assert.Same(marker, Assert.IsType<PyTuple>(items.Items[0]).Items[1]);
        Assert.Same(marker, Method(e, "get", "absent", marker));
    }

    [Fact]
    public void MutatorErrorsLeaveGraphAndAttributeStateUntouched()
    {
        var e = Element("r"); var globals = PyXmlElementTree.CreateAttributes();
        Assert.Throws<PyTypeError>(() => Call(globals["SubElement"], [e, "x", new PyList()]));
        Assert.Throws<PyTypeError>(() => Method(e, "append", 1L));
        Assert.Throws<PyTypeError>(() => Method(e, "insert", 0L, 1L));
        Assert.Equal(0L, Method(e, "__len__"));
        var before = e.GetAttr("attrib"); Assert.Throws<PyTypeError>(() => e.SetAttr("attrib", 1L)); Assert.Same(before, e.GetAttr("attrib"));
        Assert.Throws<PyNotImplementedError>(() => Method(e, "set", 1L, "x"));
        foreach (string name in new[] { "clear", "extend", "remove", "itertext", "find", "__setitem__", "__delitem__" })
            Assert.Throws<PyNotImplementedError>(() => e.GetAttr(name));
    }

    [Fact]
    public void FactoriesReadCapturedGlobalsAndAliasRemainsOriginal()
    {
        var globals = PyXmlElementTree.CreateAttributes(); var original = globals["Comment"]; var pi = globals["PI"];
        var other = PyXmlElementTree.CreateAttributes();
        Assert.NotSame(original, other["Comment"]); Assert.Same(pi, globals["ProcessingInstruction"]);
        var marker = new PyList(); globals["Comment"] = marker;
        var e = Assert.IsType<PyXmlElement>(Call(original, [marker]));
        Assert.Same(marker, e.GetAttr("tag")); Assert.Same(marker, e.GetAttr("text"));
        globals["ProcessingInstruction"] = marker;
        var p = Assert.IsType<PyXmlElement>(Call(pi, ["target", "data"]));
        Assert.Same(marker, p.GetAttr("tag")); Assert.Equal("target data", p.GetAttr("text"));
        globals["Element"] = other["Comment"];
        Assert.Throws<PyNotImplementedError>(() => Call(original, ["x"]));
        Assert.IsType<PyXmlElement>(Call(globals["SubElement"], [e, "child"]));
    }

    [Fact]
    public void QNameIdentityTextHashAndMutationAreReal()
    {
        var q = Call(PyXmlElementTree.QNameType, ["urn:x", "item"]);
        Assert.Same(PyXmlElementTree.QNameType, PyObject.TypeOf(q)); Assert.Equal("{urn:x}item", PyRepr.Str(q));
        Assert.True(PyHash.Equal(q, "{urn:x}item")); Assert.True(PyHash.Equal("{urn:x}item", q));
        Assert.Equal(PyHash.Of("{urn:x}item"), PyHash.Of(q));
        PyObject.SetAttr(q, "text", "changed"); Assert.True(PyHash.Equal(q, "changed"));
        Assert.Equal(PyHash.Of("changed"), PyHash.Of(q));
        Assert.Equal(false, Method(q, "__eq__", 1L));
        Assert.Equal(true, Method(q, "__ne__", 1L));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(q, "text", double.NaN));
        Assert.Equal("changed", PyObject.GetAttr(q, "text"));
    }

    [Fact]
    public void ElementTreePreservesFalsyRootWithoutBooleanConversion()
    {
        var e = Element("empty"); var tree = Call(PyXmlElementTree.TreeType, [e]);
        Assert.Same(e, Method(tree, "getroot")); Assert.Equal(0L, Method(Call(PyXmlElementTree.TreeType, [0L]), "getroot"));
        Assert.IsType<PyNone>(Method(Call(PyXmlElementTree.TreeType, []), "getroot"));
        Assert.Throws<PyNotImplementedError>(() => Call(PyXmlElementTree.TreeType, [], ["file"], ["file.xml"]));
    }

    [Fact]
    public void BooleanUsesExistingWarningRecordsAndErrorFilters()
    {
        var warnings = PyWarnings.CreateAttributes();
        object? context = Call(warnings["catch_warnings"], [], ["record"], [true]);
        var seen = Assert.IsType<PyList>(Method(context, "__enter__"));
        try
        {
            Call(warnings["simplefilter"], ["always"]); var empty = Element("x");
            Assert.False(PyOps.Truthy(empty)); Method(empty, "append", Element("child")); Assert.True(PyOps.Truthy(empty));
            Assert.Equal(2, seen.Count); Assert.Same(PyType.Get("DeprecationWarning"), PyObject.GetAttr(seen.Items[0], "category"));
            Call(warnings["simplefilter"], ["error"]); Assert.Throws<PyDeprecationWarning>(() => PyOps.Truthy(empty));
        }
        finally { Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); }
    }
    [Fact]
    public void UserExceptionsCannotSilentlyEnterUnsupportedComparisonOrIndexPaths()
    {
        var error = new PyValueError("custom"); error.BindPythonClass(new PyClass("Custom"));
        var q = Call(PyXmlElementTree.QNameType, ["x"]);
        Assert.Throws<PyNotImplementedError>(() => Method(q, "__eq__", error));
        var e = Element("root");
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(e, error));
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(e, new PySlice(error, null, null)));
        Method(e, "append", Element(error));
        var filtered = Assert.IsAssignableFrom<IPyIterator>(Method(e, "iter", "x"));
        Assert.Throws<PyNotImplementedError>(() => filtered.MoveNext());
    }

    [Fact]
    public void QNameModuleRebindingIsRejectedBeforeChangingPublishedIdentity()
    {
        PyNativeModules.Register(); var module = PyModules.Import("xml.etree.ElementTree");
        var original = module.GetAttr("QName");
        module.SetAttr("QName", original);
        Assert.Throws<PyNotImplementedError>(() => module.SetAttr("QName", PyType.Get("int")));
        Assert.Same(original, module.GetAttr("QName"));
        Assert.Same(original, PyObject.TypeOf(Call(original, ["x"])));
    }

    [Fact]
    public void QNamePythonMethodAndInheritedSlotHaveDifferentBindingRules()
    {
        var q = Call(PyXmlElementTree.QNameType, ["x"]);
        Assert.Equal(true, Call(PyObject.GetAttr(q, "__eq__"), [], ["other"], ["x"]));
        Assert.Equal("wrapper __ne__() takes no keyword arguments", Assert.Throws<PyTypeError>(() =>
            Call(PyObject.GetAttr(q, "__ne__"), [], ["other"], ["x"])).PyMessage);
        Assert.Equal("QName.__eq__() takes 2 positional arguments but 3 were given", Assert.Throws<PyTypeError>(() =>
            Method(q, "__eq__", "x", "y")).PyMessage);
        Assert.Equal("QName.__str__() takes 1 positional argument but 2 were given", Assert.Throws<PyTypeError>(() =>
            Method(q, "__str__", "x")).PyMessage);
        var tree = Call(PyXmlElementTree.TreeType, []);
        Assert.Equal("ElementTree.getroot() takes 1 positional argument but 2 were given", Assert.Throws<PyTypeError>(() =>
            Method(tree, "getroot", "x")).PyMessage);
    }

    [Fact]
    public void InitializerOverarityRetainsPythonOptionalRangeMessages()
    {
        Assert.Equal("QName.__init__() takes from 2 to 3 positional arguments but 4 were given", Assert.Throws<PyTypeError>(() =>
            Call(PyXmlElementTree.QNameType, ["a", "b", "c"])).PyMessage);
        Assert.Equal("ElementTree.__init__() takes from 1 to 3 positional arguments but 4 were given", Assert.Throws<PyTypeError>(() =>
            Call(PyXmlElementTree.TreeType, [null, null, null])).PyMessage);
    }

    [Fact]
    public void UnsupportedRepresentationsDoNotReturnGenericNativePlaceholders()
    {
        var e = Element("x"); var tree = Call(PyXmlElementTree.TreeType, [e]);
        var q = Call(PyXmlElementTree.QNameType, ["x"]);
        foreach (object? value in new object?[] { e, tree, q, PyIter.Create(e), Method(e, "iter") })
            Assert.Throws<PyNotImplementedError>(() => PyRepr.Repr(value));
        Assert.Throws<PyNotImplementedError>(() => PyRepr.Str(e));
        Assert.Throws<PyNotImplementedError>(() => PyRepr.Str(tree));
        Assert.Equal("x", PyRepr.Str(q));
    }

}
