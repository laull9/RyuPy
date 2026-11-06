// 公共转换身份与真实allocator闭点；反射仅调用内部根接点，descriptor公开接线另由B/C验收。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringObjectConversionTests
{
    private static PyClass TextClass(string name = "S")
    {
        var cls = (PyClass)PyOps.MakeClass(name, [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls); return cls;
    }
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static object Text(PyClass cls, object? value) => cls.Invoke([value], [], [])!;
    private static object? Call(string name, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallBuiltin(name, args, names ?? [], values ?? []);
    private static object? RootNew(object?[] args, string[]? names = null, object?[]? values = null)
    {
        Type helper = typeof(PyClass).Assembly.GetType("RyuPy.Runtime.PyStringLayout")!;
        try { return helper.GetMethod("New", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [args, names ?? [], values ?? []]); }
        catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException!).Throw(); throw; }
    }

    [Fact]
    public void DefaultsProduceBaseTextWithoutLosingPublicSubtypeIdentity()
    {
        var cls = TextClass(); object value = Text(cls, "a\n😀");
        Assert.Equal("a\n😀", Assert.IsType<string>(Call("str", [value])));
        Assert.Equal("'a\\n😀'", Assert.IsType<string>(Call("repr", [value])));
        string exact = new string(['a', 'b']); Assert.Same(exact, Call("str", [exact]));
        Assert.Equal("", Call("str", [])); Assert.Equal("None", Call("str", [PyNone.Instance]));
        Assert.Same(cls, PyObject.TypeOf(value));
    }

    [Fact]
    public void PublicStrAndReprRetainReturnedSubclassAndNeverReconvertIt()
    {
        var resultClass = TextClass("Result"); object result = Text(resultClass, "payload");
        resultClass.SetAttr("__str__", Method("forbidden", _ => throw new Exception("second conversion")));
        var cls = new PyClass("Producer"); int strCalls = 0, reprCalls = 0;
        cls.SetAttr("__str__", Method("str", _ => { strCalls++; return result; }));
        cls.SetAttr("__repr__", Method("repr", _ => { reprCalls++; return result; }));
        var value = new PyInstance(cls);
        Assert.Same(result, Call("str", [value])); Assert.Same(result, Call("repr", [value]));
        Assert.Equal("payload", PyRepr.Str(value)); Assert.Equal("payload", PyRepr.Repr(value));
        Assert.Equal(2, strCalls); Assert.Equal(2, reprCalls);
    }

    [Fact]
    public void OrdinaryStrReprFallbackAndStringDefaultDiffer()
    {
        var textClass = TextClass(); object result = Text(textClass, "repr-result");
        var ordinary = new PyClass("Ordinary"); ordinary.SetAttr("__repr__", Method("repr", _ => result));
        Assert.Same(result, Call("str", [new PyInstance(ordinary)]));
        textClass.SetAttr("__repr__", Method("repr", _ => result));
        object value = Text(textClass, "raw");
        Assert.Equal("raw", Call("str", [value])); Assert.Same(result, Call("repr", [value]));
    }

    [Fact]
    public void TypeSlotsIgnoreSameNamedInstanceAttributes()
    {
        var cls = TextClass(); object value = Text(cls, "raw");
        PyObject.SetAttr(value, "__str__", Method("instance", _ => throw new Exception("wrong slot")));
        Assert.Equal("raw", Call("str", [value]));
        cls.SetAttr("__str__", Method("type", _ => "actual")); Assert.Equal("actual", Call("str", [value]));
    }

    [Fact]
    public void ObjectAllocationConvertsOnceThenInitializesWithOriginalArgument()
    {
        var resultClass = TextClass("Result"); object returned = Text(resultClass, "converted");
        resultClass.SetAttr("__str__", Method("forbidden", _ => throw new Exception("second str")));
        var inputClass = new PyClass("Input"); var log = new List<string>();
        inputClass.SetAttr("__str__", Method("convert", _ => { log.Add("convert"); return returned; }));
        var input = new PyInstance(inputClass); var target = TextClass("Target");
        target.SetAttr("__init__", PyNativeFunctions.Create("init", ["self", "object"], [null, null], 2,
            args => { Assert.Same(input, args[1]); log.Add("init"); Assert.Equal("converted", PyRepr.Str(args[0])); return PyNone.Instance; }));
        object value = target.Invoke([], ["object"], [input])!;
        Assert.Same(target, PyObject.TypeOf(value)); Assert.NotSame(returned, value);
        Assert.Equal(new[] { "convert", "init" }, log);
    }

    [Fact]
    public void SourceSubclassOverrideIsNotSilentlyUnwrapped()
    {
        var sourceClass = TextClass("Source"); object source = Text(sourceClass, "payload"); int calls = 0;
        sourceClass.SetAttr("__str__", Method("str", _ => { calls++; return "override"; }));
        var target = TextClass("Target");
        Assert.Equal("override", PyRepr.Str(Text(target, source)));
        Assert.Equal("override", PyRepr.Str(RootNew([target, source])));
        Assert.Equal("override", RootNew([PyType.Get("str"), source])); Assert.Equal(3, calls);
    }

    [Fact]
    public void ExplicitRootNewDoesNotInitializeAndRootStrKeepsResultObject()
    {
        var cls = TextClass(); int inits = 0; cls.SetAttr("__init__", Method("init", _ => { inits++; return PyNone.Instance; }));
        object value = RootNew([cls], ["object"], ["raw"])!;
        Assert.Same(cls, PyObject.TypeOf(value)); Assert.Equal("raw", PyRepr.Str(value)); Assert.Equal(0, inits);
        var producerClass = new PyClass("Producer"); producerClass.SetAttr("__str__", Method("str", _ => value));
        Assert.Same(value, RootNew([PyType.Get("str"), new PyInstance(producerClass)]));
    }

    [Fact]
    public void RootNewRequiresActualStringSubtype()
    {
        Assert.Throws<PyTypeError>(() => RootNew([])); Assert.Throws<PyTypeError>(() => RootNew([1L]));
        Assert.Throws<PyTypeError>(() => RootNew([PyBaseObject.Type]));
        Assert.Throws<PyTypeError>(() => RootNew([new PyClass("str")]));
    }

    [Fact]
    public void FullBinderRejectsDuplicatesUnknownAndExcessBeforeConversion()
    {
        var cls = new PyClass("Never"); cls.SetAttr("__str__", Method("str", _ => throw new Exception("must not convert")));
        var input = new PyInstance(cls);
        Assert.Equal("argument for str() given by name ('object') and position (1)",
            Assert.Throws<PyTypeError>(() => Call("str", [input], ["object"], [input])).Message);
        Assert.Equal("'unknown' is an invalid keyword argument for str()",
            Assert.Throws<PyTypeError>(() => Call("str", [input], ["unknown"], [1L])).Message);
        Assert.Equal("str() takes at most 3 arguments (5 given)",
            Assert.Throws<PyTypeError>(() => Call("str", [input, 1L, 2L, 3L], ["unknown"], [1L])).Message);
        Assert.Equal("word", Call("str", [], ["object"], ["word"]));
    }

    [Fact]
    public void DecodeDomainStaysExplicitAndDoesNotDropArguments()
    {
        Assert.Throws<PyTypeError>(() => Call("str", ["word", "utf8"]));
        Assert.Throws<PyTypeError>(() => Call("str", [new PyBytes([])], ["encoding"], [PyNone.Instance]));
        Assert.Throws<PyNotImplementedError>(() => Call("str", [new PyBytes([65]), "utf8"]));
        Assert.Throws<PyNotImplementedError>(() => Call("str", [], ["errors"], ["strict"]));
    }

    [Fact]
    public void BadReturnAndUserErrorsPreserveSingleInvocation()
    {
        var cls = new PyClass("Bad"); int calls = 0; var error = new PyValueError("same");
        cls.SetAttr("__str__", Method("str", _ => { calls++; throw error; }));
        var value = new PyInstance(cls); Assert.Same(error, Assert.Throws<PyValueError>(() => Call("str", [value])));
        Assert.Equal(1, calls); cls.SetAttr("__str__", Method("str", _ => 5L));
        Assert.Throws<PyTypeError>(() => Call("str", [value]));
        cls.SetAttr("__repr__", Method("repr", _ => PyNone.Instance)); Assert.Throws<PyTypeError>(() => Call("repr", [value]));
    }

    [Fact]
    public void InitFailureIsNotReplacedOrRetried()
    {
        var cls = TextClass(); var error = new PyValueError("init"); int calls = 0;
        cls.SetAttr("__init__", Method("init", _ => { calls++; throw error; }));
        Assert.Same(error, Assert.Throws<PyValueError>(() => Text(cls, "raw"))); Assert.Equal(1, calls);
        cls.SetAttr("__init__", Method("init", _ => 1L)); Assert.Throws<PyTypeError>(() => Text(cls, "raw"));
    }
}
