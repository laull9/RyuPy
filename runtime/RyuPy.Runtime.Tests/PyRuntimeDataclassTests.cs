using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyRuntimeDataclassTests
{
    private static PyClass Class(string name) => (PyClass)PyOps.MakeClass(name, [], null);

    [Fact]
    public void DeclarationIsInactiveUntilAppliedAndConstructorBindsFields()
    {
        PyClass cls = Class("Point");
        PyOps.DeclareDataclass(cls, ["x", "y"], false);
        Assert.False(cls.Attributes.ContainsKey("__init__"));
        Assert.Throws<PyTypeError>(() => cls.Invoke([1L], [], []));
        Assert.Same(cls, PyOps.ApplyDataclass(cls, false));
        Assert.Equal("__init__", PyObject.GetAttr(cls.Attributes["__init__"], "__name__"));
        Assert.Equal("__repr__", PyObject.GetAttr(cls.Attributes["__repr__"], "__name__"));
        var first = Assert.IsType<PyInstance>(cls.Invoke([1L], ["y"], [2L]));
        var second = Assert.IsType<PyInstance>(cls.Invoke([], ["y", "x"], [2L, 1L]));
        Assert.True(PyObject.IsInstance(first, cls));
        Assert.NotSame(first, second);
        Assert.Equal("Point(x=1, y=2)", PyRepr.Repr(first));
        Assert.True(PyHash.Equal(first, second));
        Assert.Throws<PyTypeError>(() => PyHash.Of(first));
        Assert.Throws<PyTypeError>(() => cls.Invoke([1L], ["x"], [2L]));
        Assert.Throws<PyTypeError>(() => cls.Invoke([], [], []));
        Assert.Throws<PyTypeError>(() => cls.Invoke([1L, 2L, 3L], [], []));
        Assert.Throws<PyTypeError>(() => cls.Invoke([1L, 2L], ["bad"], [3L]));
        var namedSelf = Class("NamedSelf");
        PyOps.DeclareDataclass(namedSelf, ["__dataclass_self__"], false);
        PyOps.ApplyDataclass(namedSelf, false);
        object namedInstance = namedSelf.Invoke([], ["__dataclass_self__"], [3L])!;
        Assert.Equal(3L, PyObject.GetAttr(namedInstance, "__dataclass_self__"));
    }

    [Fact]
    public void FrozenAliasesRejectWritesAndDeletesAfterInternalInitialization()
    {
        PyClass cls = Class("Frozen");
        PyOps.DeclareDataclass(cls, ["value"], true);
        PyOps.ApplyDataclass(cls, true);
        object instance = cls.Invoke([42L], [], [])!;
        object alias = instance;
        Assert.Equal(42L, PyObject.GetAttr(alias, "value"));
        var error = Assert.Throws<PyFrozenInstanceError>(() => PyObject.SetAttr(alias, "value", 1L));
        Assert.IsAssignableFrom<PyAttributeError>(error);
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.SetAttr(alias, "other", 1L));
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.DelAttr(alias, "value"));
        Assert.Equal(42L, PyObject.GetAttr(instance, "value"));
        object equal = cls.Invoke([42L], [], [])!;
        Assert.Equal(PyHash.Of(instance), PyHash.Of(equal));
    }

    [Fact]
    public void ReplacementUsesItsOwnFieldsAndCustomMethodsSurvive()
    {
        PyClass original = Class("Original"), replacement = Class("Replacement");
        PyOps.DeclareDataclass(original, ["wrong"], true);
        PyOps.DeclareDataclass(replacement, ["right"], false);
        PyOps.ApplyDataclass(replacement, false);
        object instance = replacement.Invoke([5L], [], [])!;
        Assert.Equal(5L, PyObject.GetAttr(instance, "right"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "wrong"));
        PyObject.SetAttr(instance, "right", 6L);
        var empty = Class("Empty");
        PyOps.ApplyDataclass(empty, false);
        Assert.Equal("Empty()", PyRepr.Repr(empty.Invoke([], [], [])));
        Assert.Throws<PyTypeError>(() => PyOps.ApplyDataclass(42L, false));
        var custom = Class("Custom");
        var init = Fn("__init__", ["self"], _ => PyNone.Instance);
        var repr = Fn("__repr__", ["self"], _ => "custom");
        var eq = Fn("__eq__", ["self", "other"], _ => false);
        custom.SetAttr("__init__", init);
        custom.SetAttr("__repr__", repr);
        custom.SetAttr("__eq__", eq);
        PyOps.DeclareDataclass(custom, ["unused"], false);
        PyOps.ApplyDataclass(custom, false);
        Assert.Same(init, custom.Attributes["__init__"]);
        Assert.Same(repr, custom.Attributes["__repr__"]);
        Assert.Same(eq, custom.Attributes["__eq__"]);
        Assert.Equal("custom", PyRepr.Repr(custom.Invoke([], [], [])));
    }

    [Fact]
    public void FrozenOptionBelongsToAppliedMarkerEvenAfterClassReplacement()
    {
        var original = Class("Original");
        PyOps.DeclareDataclass(original, ["wrong"], true);
        var replacement = Class("Replacement");
        PyOps.DeclareDataclass(replacement, ["right"], false);
        PyOps.ApplyDataclass(replacement, true);
        object instance = replacement.Invoke([1L], [], [])!;
        Assert.Equal(1L, PyObject.GetAttr(instance, "right"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "wrong"));
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.SetAttr(instance, "right", 2L));
        var noDeclaration = Class("Empty");
        PyOps.ApplyDataclass(noDeclaration, true);
        object empty = noDeclaration.Invoke([], [], [])!;
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.SetAttr(empty, "extra", 3L));
    }

    [Fact]
    public void NamespaceLookupIsRawAndGeneratedReprHandlesCycles()
    {
        var cls = Class("Cycle");
        var function = Fn("method", ["self"], _ => null);
        var property = new PyProperty(function);
        cls.SetAttr("method", function);
        cls.SetAttr("property", property);
        Assert.Same(function, PyOps.ClassNamespaceGet(cls, "method"));
        Assert.Same(property, PyOps.ClassNamespaceGet(cls, "property"));
        Assert.Throws<PyNameError>(() => PyOps.ClassNamespaceGet(cls, "missing"));
        var derived = (PyClass)PyOps.MakeClass("Derived", [cls], null);
        Assert.Throws<PyNameError>(() => PyOps.ClassNamespaceGet(derived, "method"));
        PyOps.DeclareDataclass(cls, ["next"], false);
        PyOps.ApplyDataclass(cls, false);
        object instance = cls.Invoke([PyNone.Instance], [], [])!;
        PyObject.SetAttr(instance, "next", instance);
        Assert.Equal("Cycle(next=...)", PyRepr.Repr(instance));
    }

    [Fact]
    public void DynamicExceptionBasesRequireCompiledFactories()
    {
        Assert.Throws<PyTypeError>(() => PyOps.MakeClass("DynamicError", [PyType.Get("ValueError")], null));
        PyExceptionFactory factory = (_, _) => new PyValueError("compiled");
        var known = Assert.IsType<PyClass>(PyOps.MakeClass("KnownError", [PyType.Get("ValueError")], factory));
        Assert.IsType<PyValueError>(known.Invoke([], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.MakeClass("DynamicChild", [known], null));
        var child = Assert.IsType<PyClass>(PyOps.MakeClass("KnownChild", [known], factory));
        Assert.IsType<PyValueError>(child.Invoke([], [], []));
        var ordinary = Class("Ordinary");
        Assert.IsType<PyClass>(PyOps.MakeClass("Child", [ordinary], null));
    }

    [Fact]
    public void GeneratedInitializationUsesDescriptorsEvenWhenFrozen()
    {
        var cls = Class("WithProperty");
        var log = new List<object?>();
        var setter = Fn("set", ["self", "value"], args =>
        {
            log.Add(args[1]);
            PyObject.SetAttr(args[0], "stored", args[1]);
            return null;
        });
        cls.SetAttr("value", new PyProperty(Fn("get", ["self"], args => PyObject.GetAttr(args[0], "stored")), setter));
        PyOps.DeclareDataclass(cls, ["value"], false);
        PyOps.ApplyDataclass(cls, false);
        object instance = cls.Invoke([7L], [], [])!;
        Assert.Equal(7L, Assert.Single(log));
        Assert.Equal(7L, PyObject.GetAttr(instance, "value"));
        var frozen = Class("FrozenProperty");
        frozen.SetAttr("value", new PyProperty(null, Fn("set", ["self", "value"], args =>
        {
            log.Add(args[1]);
            return null;
        })));
        PyOps.DeclareDataclass(frozen, ["value"], true);
        PyOps.ApplyDataclass(frozen, true);
        object frozenInstance = frozen.Invoke([8L], [], [])!;
        Assert.Equal(new object?[] { 7L, 8L }, log);
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.SetAttr(frozenInstance, "value", 9L));
        frozen.SetAttr("value", new PyProperty(null, setter));
        // setter 中的普通属性写入仍受冻结约束，不能扩大为整个 __init__ 都可任意写入。
        Assert.Throws<PyFrozenInstanceError>(() => frozen.Invoke([10L], [], []));
        var readonlyClass = Class("ReadOnlyProperty");
        readonlyClass.SetAttr("value", new PyProperty(Fn("get", ["self"], _ => 1L)));
        PyOps.DeclareDataclass(readonlyClass, ["value"], false);
        PyOps.ApplyDataclass(readonlyClass, false);
        Assert.Throws<PyAttributeError>(() => readonlyClass.Invoke([2L], [], []));
    }

    [Fact]
    public void GeneratedEqualityUsesNotImplementedAndReflectedComparison()
    {
        var cls = Class("Record");
        PyOps.DeclareDataclass(cls, ["value"], false);
        PyOps.ApplyDataclass(cls, false);
        object record = cls.Invoke([1L], [], [])!;
        int calls = 0;
        var otherClass = Class("Other");
        otherClass.SetAttr("__eq__", Fn("eq", ["self", "other"], _ => { calls++; return true; }));
        object other = otherClass.Invoke([], [], [])!;
        Assert.Same(PyNotImplemented.Instance, PyOps.CallMethod(record, "__eq__", [other], [], []));
        Assert.Equal(0, calls);
        Assert.True(PyHash.Equal(record, other));
        Assert.Equal(1, calls);
        otherClass.SetAttr("__eq__", Fn("eq", ["self", "other"], _ => PyNotImplemented.Instance));
        Assert.False(PyHash.Equal(record, other));
        Assert.Equal("NotImplemented", PyRepr.Repr(PyNotImplemented.Instance));
        Assert.Equal("NotImplementedType", PyRepr.TypeName(PyNotImplemented.Instance));
    }

    [Fact]
    public void ReplacementFieldDefaultsAreRejectedBeforeGeneratingMethods()
    {
        var original = Class("Original");
        PyOps.DeclareDataclass(original, ["original_field"], false);
        foreach (object? defaultValue in new object?[] { 1L, PyNone.Instance, null, new PyList() })
        {
            var replacement = Class("Replacement");
            // 模拟普通替代类自己的标注声明，不能套用原类 original_field。
            PyOps.DeclareDataclass(replacement, ["replacement_field"], false);
            replacement.SetAttr("replacement_field", defaultValue);
            Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(replacement, true));
            Assert.False(replacement.Attributes.ContainsKey("__init__"));
            Assert.False(replacement.Attributes.ContainsKey("__repr__"));
            Assert.Same(defaultValue, replacement.Attributes["replacement_field"]);
            object instance = replacement.Invoke([], [], [])!;
            // 失败不能留下已激活的冻结状态。
            PyObject.SetAttr(instance, "still_mutable", 7L);
            Assert.Equal(7L, PyObject.GetAttr(instance, "still_mutable"));
        }
        var supported = Class("SupportedReplacement");
        PyOps.DeclareDataclass(supported, ["own_field"], false);
        PyOps.ApplyDataclass(supported, false);
        object result = supported.Invoke([], ["own_field"], [42L])!;
        Assert.Equal(42L, PyObject.GetAttr(result, "own_field"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(result, "original_field"));
    }

    internal static PyFunction Fn(string name, string[] parameters, Func<object?[], object?> body) =>
        new(name, PySignature.Create(name, parameters, parameters.Select(_ => "pos").ToArray(),
            parameters.Select(_ => "0").ToArray()), (_, args, _, _) => body(args), []);
}
