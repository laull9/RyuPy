// 公共str.format窄接线验收：direct/saved/prepared/root/子类路径及原方法拒关键字不变。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringFormatBindingTests
{
    private static object? Direct(object receiver, object?[] args, string[] names, object?[] values) =>
        PyOps.CallMethod(receiver, "format", args, names, values);
    private static object? Invoke(object? callable, object?[] args, string[] names, object?[] values) =>
        PyOps.CallValue(callable, args, names, values);
    private static PyInstance Text(string text)
    {
        var cls = (PyClass)PyOps.MakeClass("Template", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke([text], [], []));
    }

    [Fact]
    public void DirectSavedPreparedAndRootAcceptCompleteKeywordArrays()
    {
        string template = "{0}:{name}";
        Assert.Equal("x:y", Direct(template, ["x"], ["name"], ["y"]));
        Assert.Equal("x:y", Invoke(PyObject.GetAttr(template, "format"), ["x"], ["name"], ["y"]));
        Assert.Equal("x:y", Invoke(PyOps.PrepareMethod(template, "format"), ["x"], ["name"], ["y"]));
        Assert.Equal("x:y", Invoke(PyObject.GetAttr(PyType.Get("str"), "format"), [template, "x"], ["name"], ["y"]));
    }

    [Fact]
    public void ZeroAndExtraArgumentsRemainValidWithOrdinaryNamedKeys()
    {
        Assert.Equal("abc", Direct("abc", [1L, 2L], ["unused"], [3L]));
        Assert.Equal("", Invoke(PyObject.GetAttr("", "format"), [], [], []));
        Assert.Equal("a:b", Direct("{self}:{format_string}", [], ["self", "format_string"], ["a", "b"]));
    }

    [Fact]
    public void SavedMetadataRetainsOriginalReceiverAndRootDescriptorClass()
    {
        string template = new(new[] { '{', '0', '}' }); object? saved = PyObject.GetAttr(template, "format");
        Assert.Same(template, PyObject.GetAttr(saved, "__self__"));
        Assert.Equal("format", PyObject.GetAttr(saved, "__name__"));
        Assert.Equal("str.format", PyObject.GetAttr(saved, "__qualname__"));
        Assert.Same(PyType.Get("str"), PyObject.GetAttr(PyObject.GetAttr(PyType.Get("str"), "format"), "__objclass__"));
    }

    [Fact]
    public void RootMissingAndInvalidReceiverUseFormatSpecificBindingErrors()
    {
        object? root = PyObject.GetAttr(PyType.Get("str"), "format");
        Assert.Equal("unbound method str.format() needs an argument", Assert.Throws<PyTypeError>(() => Invoke(root, [], [], [])).Message);
        Assert.Equal("unbound method str.format() needs an argument", Assert.Throws<PyTypeError>(() => Invoke(root, [], ["self"], ["x"])).Message);
        Assert.Equal("descriptor 'format' for 'str' objects doesn't apply to a 'int' object",
            Assert.Throws<PyTypeError>(() => Invoke(root, [42L], ["self"], ["x"])).Message);
    }

    [Fact]
    public void SubclassSavedRootAndSuperBypassLaterOrdinaryOverrides()
    {
        var template = Text("{0}:{x}"); var cls = template.Class;
        object? saved = PyObject.GetAttr(template, "format");
        Assert.Same(template, PyObject.GetAttr(saved, "__self__"));
        cls.SetAttr("format", PyNativeFunctions.CreateVariadic("format", _ => "override"));
        Assert.Equal("override", Direct(template, [], [], []));
        Assert.Equal("a:b", Invoke(saved, ["a"], ["x"], ["b"]));
        Assert.Equal("a:b", Invoke(PyObject.GetAttr(PyType.Get("str"), "format"), [template, "a"], ["x"], ["b"]));
        Assert.Equal("a:b", Direct(new PySuper(cls, template), ["a"], ["x"], ["b"]));
        PyObject.SetAttr(template, "format", PyNativeFunctions.CreateVariadic("format", _ => "instance"));
        Assert.Equal("instance", Direct(template, [], [], []));
        Assert.Equal("a:b", Invoke(saved, ["a"], ["x"], ["b"]));
    }

    [Fact]
    public void PublicEntryPreservesWholeSubclassTemplateAndResultIdentity()
    {
        var template = Text("literal");
        template.Class.SetAttr("__str__", PyNativeFunctions.CreateVariadic("__str__", _ => throw new Exception("template str")));
        Assert.Same(template, Direct(template, [], [], []));
        Assert.Same(template, Invoke(PyObject.GetAttr(PyType.Get("str"), "format"), [template], [], []));
        var cls = new PyClass("Returns"); cls.SetAttr("__format__", PyNativeFunctions.CreateVariadic("__format__", _ => template));
        Assert.Same(template, Direct("{0}", [new PyInstance(cls)], [], []));
    }

    [Fact]
    public void OtherExactStringMethodsKeepOriginalKeywordRejection()
    {
        Assert.Equal("AB", PyOps.CallMethod("ab", "upper", [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod("ab", "upper", [], ["x"], [1L]));
        Assert.Throws<PyTypeError>(() => Invoke(PyObject.GetAttr("ab", "upper"), [], ["x"], [1L]));
        Assert.Equal("xbx", PyOps.CallMethod("aba", "replace", ["a", "x"], [], []));
    }

    [Fact]
    public void RootBindingErrorsPrepareCallerAndAmbientWithoutChangingIt()
    {
        var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("template_binding.py", "caller");
        try
        {
            PyTraceback.SetLine(8); object? root = PyObject.GetAttr(PyType.Get("str"), "format");
            var error = Assert.Throws<PyTypeError>(() => Invoke(root, [], [], []));
            Assert.Same(ambient, error.Context); Assert.Null(error.Cause);
            Assert.Equal(new PyStackFrame("template_binding.py", "caller", 8), error.Frames[0]);
            Assert.Same(ambient, PyErrors.Current);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }
}
