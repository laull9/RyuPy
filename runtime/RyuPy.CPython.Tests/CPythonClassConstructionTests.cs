// C1 原生两阶段类构造：prepared namespace 的时序、单读、重入身份及确定失败清理。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    private static string[] ClassStrings(object? value) => PyOps.ToSequence(value).Select(PyRepr.Str).ToArray();

    [Fact]
    public void NativeConstruction_PreparesBeforeBodyAndReadsMappingOnce()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        SuperCall(module.GetAttr("reset"));
        object frame = PyLexicalClassFactory.Prepare("C1Constructed", "C1Constructed", "test_mod", [module.GetAttr("Base")]);
        object? bodyValue = SuperCall(module.GetAttr("body"));
        PyLexicalClassFactory.NamespaceSet(frame, "body_value", bodyValue);
        Assert.True(PyLexicalClassFactory.NamespaceContains(frame, "prepared"));
        Assert.Equal(7L, PyLexicalClassFactory.NamespaceGet(frame, "prepared"));
        Assert.True(PyLexicalClassFactory.NamespaceContains(frame, "body_value"));
        Assert.Same(bodyValue, PyLexicalClassFactory.NamespaceGet(frame, "body_value"));
        PyLexicalClassFactory.NamespaceSet(frame, "__metaclass__", 17L);
        object? callbackReceiver = null;
        var callback = new PyFunction("on_construct", PySignature.Create("on_construct", ["cls"], ["pos"], ["0"]),
            (_, args, _, _) => callbackReceiver = args[0], [], usesSuper: false);
        PyLexicalClassFactory.NamespaceSet(frame, "on_construct", new PyClassMethod(callback));
        var created = Assert.IsType<PyClass>(PyLexicalClassFactory.Finish(frame));
        Assert.Same(created, callbackReceiver);
        Assert.Equal(17L, PyObject.GetAttr(created, "__metaclass__"));
        object? snapshot = SuperCall(module.GetAttr("snapshot"));
        Assert.Equal(new[] { "prepare:C1Constructed", "body", "write:body_value", "new:C1Constructed", "init_subclass:C1Constructed" },
            ClassStrings(PyOps.GetItem(snapshot, 0L)));
        Assert.Equal(new[] { "prepared", "body_value" }, ClassStrings(PyOps.GetItem(snapshot, 1L)));
        Assert.Equal(1L, SuperCall(module.GetAttr("disposed")));
    }

    [Fact]
    public void NativeConstruction_PrepareErrorsAndConflictPrecedeBody()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        SuperCall(module.GetAttr("reset"));
        var error = Assert.Throws<PyValueError>(() => PyLexicalClassFactory.Prepare("NoBody", "NoBody", "test_mod", [module.GetAttr("FailPrepareBase")]));
        Assert.Equal("prepare marker", error.Message);
        Assert.Throws<PyTypeError>(() => PyLexicalClassFactory.Prepare("NonePrepare", "NonePrepare", "test_mod", [module.GetAttr("NonePrepareBase")]));
        Assert.Throws<PyTypeError>(() => PyLexicalClassFactory.Prepare("Conflict", "Conflict", "test_mod", [module.GetAttr("Base"), module.GetAttr("OtherBase")]));
        object? snapshot = SuperCall(module.GetAttr("snapshot"));
        Assert.Empty(ClassStrings(PyOps.GetItem(snapshot, 0L)));
    }

    [Fact]
    public void NativeConstruction_AbortAndFailedMetaInitRemoveProvisionalIdentity()
    {
        RequireHost();
        PyModule module = Bootstrap.Host.Import("bridge_fixture.class_construction");
        SuperCall(module.GetAttr("reset"));
        object abandoned = PyLexicalClassFactory.Prepare("Abandoned", "Abandoned", "test_mod", [module.GetAttr("Base")]);
        PyLexicalClassFactory.Abort(abandoned);
        PyLexicalClassFactory.Abort(abandoned);
        Assert.Equal(1L, SuperCall(module.GetAttr("disposed")));
        Assert.Throws<PyNotImplementedError>(() => PyLexicalClassFactory.NamespaceSet(abandoned, "x", 1L));
        object frame = PyLexicalClassFactory.Prepare("FailingInit", "FailingInit", "test_mod", [module.GetAttr("FailInitBase")]);
        PyClass? temporary = null;
        var callback = new PyFunction("on_construct", PySignature.Create("on_construct", ["cls"], ["pos"], ["0"]),
            (_, args, _, _) => temporary = Assert.IsType<PyClass>(args[0]), [], usesSuper: false);
        PyLexicalClassFactory.NamespaceSet(frame, "on_construct", new PyClassMethod(callback));
        var error = Assert.Throws<PyValueError>(() => PyLexicalClassFactory.Finish(frame));
        Assert.Equal("meta init marker", error.Message);
        Assert.NotNull(temporary);
        Assert.Null(PyForeignClassBridge.GetForeignClass(temporary));
        Assert.Equal(2L, SuperCall(module.GetAttr("disposed")));
    }
}
