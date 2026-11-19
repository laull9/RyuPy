// find_spec 的普通Python签名、正文首个属性消费和延期协议禁止提前执行用户函数。
using System;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyImportlibUtilBindingTests
{
    private static object? Call(object?[] args, string[]? names = null, object?[]? values = null)
    {
        PyNativeModules.Register();
        return PyOps.CallValue(PyModules.ImportAttr("importlib.util", "find_spec"), args, names ?? [], values ?? []);
    }

    [Fact]
    public void BindingErrorsHavePythonMessagesBeforeBody()
    {
        Assert.Equal("find_spec() missing 1 required positional argument: 'name'",
            Assert.Throws<PyTypeError>(() => Call([])).Message);
        Assert.Equal("find_spec() takes from 1 to 2 positional arguments but 3 were given",
            Assert.Throws<PyTypeError>(() => Call([1L, 2L, 3L])).Message);
        Assert.Equal("find_spec() got multiple values for argument 'name'",
            Assert.Throws<PyTypeError>(() => Call([1L], ["name"], [2L])).Message);
        Assert.Equal("find_spec() got multiple values for argument 'package'",
            Assert.Throws<PyTypeError>(() => Call([1L, 2L], ["package"], [3L])).Message);
        Assert.Equal("find_spec() got an unexpected keyword argument 'other'",
            Assert.Throws<PyTypeError>(() => Call([], ["other"], [1L])).Message);
        // CP先处理关键字冲突，再报告过多位置参数。
        Assert.Equal("find_spec() got multiple values for argument 'name'",
            Assert.Throws<PyTypeError>(() => Call([1L, 2L, 3L], ["name"], [4L])).Message);
    }

    [Fact]
    public void MissingStartswithUsesAttributeErrorInsteadOfBlanketTypeError()
    {
        Assert.Equal("'int' object has no attribute 'startswith'",
            Assert.Throws<PyAttributeError>(() => Call([1L])).Message);
        Assert.Equal("'NoneType' object has no attribute 'startswith'",
            Assert.Throws<PyAttributeError>(() => Call([PyNone.Instance])).Message);
        Assert.Equal("startswith first arg must be bytes or a tuple of bytes, not str",
            Assert.Throws<PyTypeError>(() => Call([new PyBytes([1])])).Message);
        var cls = new PyClass("NoNameProtocol"); object value = PyOps.CallValue(cls, [], [], [])!;
        Assert.Equal("'NoNameProtocol' object has no attribute 'startswith'",
            Assert.Throws<PyAttributeError>(() => Call([value])).Message);
    }

    [Theory]
    [InlineData("method")]
    [InlineData("property")]
    [InlineData("getattr")]
    public void LegalCustomNameProtocolIsDeferredWithoutExecutingIt(string protocol)
    {
        int calls = 0; var cls = new PyClass("NameProtocol");
        var function = PyNativeFunctions.Create("hook", ["self", "value"], [null, PyNone.Instance], 1,
            _ => { calls++; return false; });
        if (protocol == "property") cls.SetAttr("startswith", new PyProperty(function));
        else cls.SetAttr(protocol == "getattr" ? "__getattr__" : "startswith", function);
        object name = PyOps.CallValue(cls, [], [], [])!;
        Assert.Throws<PyTypeError>(() => Call([name], ["name"], [name])); Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => Call([name])); Assert.Equal(0, calls);
    }

    [Fact]
    public void RelativePackageTruthAndRsplitHaveDistinctPhases()
    {
        Assert.Throws<PyImportError>(() => Call([".child", 0L]));
        Assert.Equal("'int' object has no attribute 'rsplit'",
            Assert.Throws<PyAttributeError>(() => Call([".child", 1L])).Message);
        int calls = 0; var cls = new PyClass("PackageTruth");
        cls.SetAttr("__bool__", PyNativeFunctions.Create("__bool__", ["self"], [null], 1,
            _ => { calls++; return true; }));
        object package = PyOps.CallValue(cls, [], [], [])!;
        Assert.Throws<PyNotImplementedError>(() => Call([".child", package])); Assert.Equal(0, calls);
        var exception = new PyValueError("package"); exception.BindPythonClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Call([".child", exception])); Assert.Equal(0, calls);
        string name = "cached_package_" + Guid.NewGuid().ToString("N"); object marker = new PyList([]);
        PyModules.RegisterNative(name, new() { ["__spec__"] = marker }); PyModules.Import(name);
        Assert.Same(marker, Call([name, package])); Assert.Equal(0, calls);
    }

    [Fact]
    public void ModuleNameGetterIsRejectedWithoutReadingUserProtocol()
    {
        string name = "name_protocol_" + Guid.NewGuid().ToString("N"); int reads = 0;
        var method = PyNativeFunctions.Create("startswith", ["prefix"], [null], 1, _ => false);
        PyModules.Register(name, "", "protocol.py", () => { }, key =>
        {
            if (key is "startswith" or "rsplit") { reads++; return method; }
            throw new PyAttributeError(key);
        }, PyModules.SetDynamic);
        PyModule module = PyModules.Import(name);
        Assert.Throws<PyNotImplementedError>(() => Call([module])); Assert.Equal(0, reads);
        Assert.Throws<PyNotImplementedError>(() => Call([".child", module])); Assert.Equal(0, reads);
    }

    [Fact]
    public void ExceptionNameDescriptorIsRejectedBeforeBinding()
    {
        int binds = 0; var cls = new PyClass("ExceptionNameProtocol");
        cls.SetAttr("startswith", new CountingDescriptor(() => binds++));
        cls.SetAttr("rsplit", new CountingDescriptor(() => binds++));
        var value = new PyValueError("name"); value.BindPythonClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Call([cls])); Assert.Equal(0, binds);
        Assert.Throws<PyNotImplementedError>(() => Call([value])); Assert.Equal(0, binds);
        Assert.Throws<PyNotImplementedError>(() => Call([".child", value])); Assert.Equal(0, binds);
    }

    [Theory]
    [InlineData("__getattr__")]
    [InlineData("__getattribute__")]
    public void ExceptionFallbackAttributeProtocolsAreRejectedWithoutCallingThem(string hook)
    {
        int calls = 0; var cls = new PyClass("ExceptionFallbackProtocol");
        cls.SetAttr(hook, PyNativeFunctions.Create(hook, ["self", "name"], [null, null], 2,
            _ => { calls++; return PyNone.Instance; }));
        var value = new PyValueError("name"); value.BindPythonClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Call([value])); Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => Call([".child", value])); Assert.Equal(0, calls);
    }

    private sealed class CountingDescriptor(Action bound) : IPyNativeDescriptor
    {
        public object? Bind(PyClass owner, object? instance)
        {
            bound();
            return PyNativeFunctions.Create("protocol", ["value"], [null], 1, _ => false);
        }
    }

}
