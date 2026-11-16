// C1 真实 CPython 方法描述符测试：绑定、动态元数据、候选缺口与原有方法上下文。
// 默认包装器和显式原生候选共用同一行为断言，精确 CodeType 缺口仅在候选开启时检查。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    [Fact]
    public void DescriptorBindingAndMutableNames_PreserveLiveBoundMethodObservation()
    {
        Assert.True(fixture.Available);
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClass("NativeMethodDict", [userDict]);
        cls.SetAttr("sample", new PyFunction("sample",
            PySignature.Create("sample", ["self", "value"], ["pos", "pos"], ["0", "0"]),
            (_, args, _, _) => args.Length == 2 ? args[1] : throw new PyTypeError("sample requires self and value"), []));
        PyOps.FinalizeOrdinaryClass(cls);
        var instance = Assert.IsType<PyInstance>(PyOps.CallValue(cls, [], [], []));
        IPyForeignObject foreignClass = PyForeignClassBridge.GetForeignClass(cls)!;
        IPyForeignObject foreignInstance = PyForeignClassBridge.GetForeignInstance(instance)!;
        object descriptor = PyObject.GetAttr(foreignClass, "sample")!;
        object bound = PyObject.GetAttr(foreignInstance, "sample")!;
        Assert.Same(instance, PyObject.GetAttr(bound, "__self__"));
        Assert.Same(descriptor, PyObject.GetAttr(bound, "__func__"));
        Assert.Equal(42L, PyOps.CallValue(bound, [42L], [], []));
        object get = PyObject.GetAttr(descriptor, "__get__")!;
        Assert.Same(descriptor, PyOps.CallValue(get, [PyNone.Instance, foreignClass], [], []));
        Assert.Equal(21L, PyOps.CallValue(descriptor, [instance, 21L], [], []));

        PyObject.SetAttr(descriptor, "__name__", "renamed");
        PyObject.SetAttr(descriptor, "__qualname__", "Changed.renamed");
        Assert.Equal("renamed", PyObject.GetAttr(bound, "__name__"));
        Assert.Equal("Changed.renamed", PyObject.GetAttr(bound, "__qualname__"));
        foreach (string name in new[] { "__name__", "__qualname__" })
        {
            Assert.Throws<PyTypeError>(() => PyObject.SetAttr(descriptor, name, 1L));
            Assert.Throws<PyTypeError>(() => PyObject.DelAttr(descriptor, name));
        }
        // 元数据写入不能改掉底层托管函数的真实参数绑定。
        Assert.Equal(99L, PyOps.CallValue(bound, [99L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(bound, [], [], []));
        if (Environment.GetEnvironmentVariable("RYUPY_CPYTHON_NATIVE_METHODS") == "1")
        {
            foreach (string name in new[] { "__code__", "__globals__", "__closure__" })
            {
                PyNotImplementedError error = Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(descriptor, name));
                Assert.Contains("CPython CodeType", error.Message);
                Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(descriptor, name, PyNone.Instance));
            }
        }
    }

    [Fact]
    public void DescriptorMetadataDictionaryAndWeakReferences_UseNativeObjectOwnership()
    {
        Assert.True(fixture.Available);
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClass("NativeMetadataDict", [userDict]);
        cls.SetAttr("sample", new PyFunction("sample",
            PySignature.Create("sample", ["self"], ["pos"], ["0"]), (_, _, _, _) => PyNone.Instance, []));
        PyOps.FinalizeOrdinaryClass(cls);
        object descriptor = PyObject.GetAttr(PyForeignClassBridge.GetForeignClass(cls), "sample")!;
        PyObject.SetAttr(descriptor, "tag", descriptor);
        object dictionary = PyObject.GetAttr(descriptor, "__dict__")!;
        Assert.Same(descriptor, PyOps.GetItem(dictionary, "tag"));
        PyObject.SetAttr(descriptor, "__module__", "changed");
        PyObject.SetAttr(descriptor, "__doc__", "live doc");
        Assert.Equal("changed", PyObject.GetAttr(descriptor, "__module__"));
        Assert.Equal("live doc", PyObject.GetAttr(descriptor, "__doc__"));
        PyObject.DelAttr(descriptor, "__module__");
        PyObject.DelAttr(descriptor, "__doc__");
        Assert.Same(PyNone.Instance, PyObject.GetAttr(descriptor, "__module__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(descriptor, "__doc__"));
        PyObject.SetAttr(descriptor, "__defaults__", new PyTuple([42L]));
        Assert.Equal(42L, PyOps.GetItem(PyObject.GetAttr(descriptor, "__defaults__"), 0L));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(descriptor, "__defaults__", 1L));
        object weakref = Bootstrap.Host.Import("weakref").GetAttr("ref")!;
        object reference = PyOps.CallValue(weakref, [descriptor], [], [])!;
        Assert.Same(descriptor, PyOps.CallValue(reference, [], [], []));
        // 断开测试自环；引用由原生GC/宿主关闭协议持有，无托管finalizer进入解释器。
        PyObject.DelAttr(descriptor, "tag");
    }
}
