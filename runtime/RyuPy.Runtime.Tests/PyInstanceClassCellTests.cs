// class cell的冷边界只检查真实类型级hook；不模拟CP调用，也不误拒普通包装值。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyInstanceClassCellTests
{
    [Fact]
    public void ClassCellGuardAllowsOrdinaryWrappersAndOwnFutureSubclassHook()
    {
        var cls = new PyClass("C");
        cls.SetAttr("__init_subclass__", PyNone.Instance);
        cls.SetAttr("property", new PyProperty(PyNone.Instance));
        cls.SetAttr("static", new PyStaticMethod(PyNone.Instance));
        cls.SetAttr("class_method", new PyClassMethod(PyNone.Instance));
        var plain = new PyInstance(new PyClass("Plain"));
        PyObject.SetAttr(plain, "__set_name__", PyNone.Instance);
        cls.SetAttr("plain", plain);
        PyModuleInstances.ClassCellGuard(cls);
    }

    [Fact]
    public void ClassCellGuardRejectsInheritedActualParentHookAfterBodyMutation()
    {
        var parent = new PyClass("Base");
        var cls = (PyClass)PyOps.MakeClass("C", [parent], null);
        PyModuleInstances.ClassCellGuard(cls);
        parent.SetAttr("__init_subclass__", PyNone.Instance);
        Assert.Contains("__init_subclass__", Assert.Throws<PyNotImplementedError>(
            () => PyModuleInstances.ClassCellGuard(cls)).Message);
    }

    [Fact]
    public void ClassCellGuardChecksDescriptorTypeChainBeforeParentHook()
    {
        var root = new PyClass("DescriptorRoot");
        root.SetAttr("__set_name__", PyNone.Instance);
        var descriptorType = (PyClass)PyOps.MakeClass("Descriptor", [root], null);
        var parent = new PyClass("Base");
        parent.SetAttr("__init_subclass__", PyNone.Instance);
        var cls = (PyClass)PyOps.MakeClass("C", [parent], null);
        cls.SetAttr("descriptor", new PyInstance(descriptorType));
        Assert.Contains("__set_name__", Assert.Throws<PyNotImplementedError>(
            () => PyModuleInstances.ClassCellGuard(cls)).Message);
    }

    [Fact]
    public void OwnedFunctionClosureReflectionCannotForgeOrExposeEnvironment()
    {
        object? context = null;
        string name = "class_cell_reflection_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "cell.py", 1, "/virtual/cell.py", false,
            value => context = value);
        var module = PyModuleInstances.Create(name);
        PyModuleInstances.Execute(module);
        object?[] cells = [PyOps.CellNew()];
        var function = PyModuleInstances.CreateOwnedFunction(context!, "f", PySignature.Create("f", [], [], []),
            static (_, _, _, _, _) => PyNone.Instance, [], false, cells);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("getattr", [function, "__closure__"], [], []));
        Assert.Equal("readonly attribute", Assert.Throws<PyAttributeError>(() =>
            PyOps.CallBuiltin("setattr", [function, "__closure__", PyNone.Instance], [], [])).Message);
        Assert.Equal("readonly attribute", Assert.Throws<PyAttributeError>(() =>
            PyOps.CallBuiltin("delattr", [function, "__closure__"], [], [])).Message);
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__"));
        attributes.Set("__closure__", 123L);
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__closure__"));
        Assert.Same(cells, function.Env);
    }
}
