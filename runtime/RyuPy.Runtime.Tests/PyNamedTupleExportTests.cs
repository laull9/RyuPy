// C3 真实 collections 导出闭环；没有内部反射捷径，也不以本文件替代五个源码应用验收。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyNamedTupleExportTests
{
    private static object Factory => PyCollections.CreateAttributes()["namedtuple"]!;
    private static object? Get(object value, string name) => PyObject.GetAttr(value, name);
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);

    [Fact]
    public void PublicFactoryAndSavedAliasCreateRealTypesWithCompleteCoreHelpers()
    {
        object alias = Factory; var shared = new PyList();
        PyClass cls = Assert.IsType<PyClass>(PyOps.CallValue(alias, ["Envelope", "value"], ["defaults", "module"], [new PyTuple([shared]), "export.test"]));
        object value = Call(cls)!;
        Assert.Same(cls, PyObject.TypeOf(value)); Assert.IsAssignableFrom<PyTuple>(value); Assert.Same(shared, Get(value, "value"));
        Assert.Same(alias, Factory); Assert.NotSame(cls, PyOps.CallValue(alias, ["Envelope", "value"], ["module"], ["export.test"]));
        Assert.Equal("Envelope(value=[])", Call(Get(value, "__repr__")));
        Assert.Same(cls, PyObject.TypeOf(Call(Get(cls, "__new__"), cls)));
        object made = Call(Get(value, "_make"), new PyTuple([shared]))!; Assert.Same(cls, PyObject.TypeOf(made)); Assert.NotSame(value, made);
        object replaced = PyOps.CallValue(Get(value, "_replace"), [], ["value"], [7L])!; Assert.Equal(7L, Get(replaced, "value"));
        Assert.Same(shared, Assert.IsType<PyDict>(Call(Get(value, "_asdict"))).Get("value"));
        Assert.Same(shared, Assert.IsType<PyTuple>(Call(Get(value, "__getnewargs__"))).Items[0]);
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [value, PyType.Get("tuple")], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("issubclass", [cls, PyType.Get("tuple")], [], []));
    }

    [Fact]
    public void PublicFactoryUsesTheGenuineCallerWithoutAddingItsOwnFrame()
    {
        PyTraceback.Reset();
        try
        {
            Assert.Throws<PyNotImplementedError>(() => Call(Factory, "P", "x"));
            PyClass explicitClass = Assert.IsType<PyClass>(PyOps.CallValue(Factory, ["P", "x"], ["module"], [false]));
            Assert.Equal(false, Get(explicitClass, "__module__"));
            var rawClass = new PyClass("RawModule"); var raw = new PyInstance(rawClass); int truth = 0;
            rawClass.SetAttr("__bool__", PyNativeFunctions.CreateVariadic("truth", _ =>
            {
                truth++; Assert.Equal(1L, PyTraceback.CaptureDepth()); return true;
            }));
            PyTraceback.PushFrameWithOrigin("public_probe.py", "caller", PyTraceback.CreateModuleExecutionSource(raw));
            PyTraceback.SetLine(23);
            PyClass cls = Assert.IsType<PyClass>(Call(Factory, "P", "x")); Assert.Same(raw, Get(cls, "__module__")); Assert.Equal(1, truth);
            PyTypeError error = Assert.Throws<PyTypeError>(() => Call(Factory));
            Assert.Equal(new PyStackFrame("public_probe.py", "caller", 23), Assert.Single(error.Frames));
            Assert.Equal(1L, PyTraceback.CaptureDepth());
        }
        finally { PyTraceback.Reset(); }
    }

    [Fact]
    public void PublicFactoryRejectsBindingBeforeInputsAndProtectsUnsupportedReflection()
    {
        int conversions = 0; var inputClass = new PyClass("Input");
        inputClass.SetAttr("__str__", PyNativeFunctions.CreateVariadic("str", _ => { conversions++; return "P"; }));
        object input = new PyInstance(inputClass);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Factory, [input, input], ["unknown"], [1L])); Assert.Equal(0, conversions);
        foreach (string name in new[] { "__name__", "__qualname__", "__module__", "__defaults__" }) Assert.Same(Get(Factory, name), Get(Factory, name));
        Assert.Equal("namedtuple", Get(Factory, "__name__")); Assert.Equal("collections", Get(Factory, "__module__")); Assert.Equal("function", PyRepr.TypeName(Factory));
        foreach (string name in new[] { "__dict__", "__code__", "__kwdefaults__", "__doc__", "__get__" })
            Assert.Throws<PyNotImplementedError>(() => Get(Factory, name));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(Factory, "__module__", "changed"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(Factory, "__defaults__"));
        var cls = new PyClass("Borrower"); cls.SetAttr("factory", Factory);
        Assert.Same(Factory, Get(cls, "factory")); Assert.Throws<PyNotImplementedError>(() => Get(new PyInstance(cls), "factory"));
    }
}
