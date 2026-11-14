// Generic导出迁移的三个独立消费守卫；删除/改绑不能以首次观察值冒canonical。
// CP冻结证据区分空功能式、类体终结和非空注解消费，不把边界NI当CP的NameError。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingGenericRootGuardsTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Form(string name) => PyTypingGenericRootTests.Form(name);
    private static object? Call(object? target, params object?[] args) => PyTypingGenericRootTests.Call(target, args);
    private static object? Get(object? value, string name) => PyObject.GetAttr(value, name);
    private static object? Bound(object? bound) => PyOps.CallValue(Form("TypeVar"), ["B"], ["bound"], [bound]);
    private static PyClass Begin(string name = "Record") => (PyClass)PyOps.MakeClass(name, [Form("NamedTuple")], null);
    private static PyDict Annotations(PyClass cls) => (PyDict)PyOps.ClassNamespaceGet(cls, "__annotations__")!;
    private static object? Functional(bool filled) => Call(Form("NamedTuple"), filled ? "Record" : "Empty",
        new PyList(filled ? [new PyTuple(["x", PyType.Get("int")])] : []));

    [Fact]
    public void CanonicalExportKeepsEmptyNonemptyAndFunctionalNamedTupleWorking()
    {
        foreach (bool filled in new[] { false, true })
        {
            PyClass cls = Begin(filled ? "Record" : "Empty");
            if (filled) Annotations(cls).Set("x", PyType.Get("int"));
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.IsAssignableFrom<PyTuple>(Call(cls, filled ? [7L] : []));
            object? functional = Functional(filled);
            Assert.IsAssignableFrom<PyTuple>(Call(functional, filled ? [7L] : []));
        }
        foreach (object? value in new[] { PyType.Get("int"), PyType.Get("str"), (object)PyNone.Instance })
            Assert.Same(value, Get(Bound(value), "__bound__"));
    }

    [Fact]
    public void PlainRootAndRootAliasAreInvalidBoundAndAnnotationsRatherThanClassesAllowedThrough()
    {
        object? root = Form("Generic");
        object? alias = PyOps.GetItem(root, Call(Form("TypeVar"), "T"));
        foreach (object? annotation in new[] { root, alias })
        {
            string expected = ReferenceEquals(annotation, root)
                ? "Plain <class 'typing.Generic'> is not valid as type argument"
                : "typing.Generic[~T] is not valid as type argument";
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Bound(annotation)).PyMessage);
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Call(Form("NamedTuple"), "Bad",
                new PyList([new PyTuple(["x", annotation])]))).PyMessage);
            PyClass cls = Begin("Bad");
            Annotations(cls).Set("x", annotation);
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(cls)).PyMessage);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedGenericIsCheckedOnlyWhenTheActualConsumerNeedsIt(bool delete)
    {
        var typing = PyTypingGenericRootTests.Typing;
        object? original = Form("Generic");
        // 类壳及类体namespace先于最终Generic读取；不能在MakeClass提前拒绝。
        PyClass empty = Begin("Empty"), filled = Begin("Filled");
        Annotations(filled).Set("x", PyType.Get("int"));
        if (delete) typing.Remove("Generic"); else typing["Generic"] = PyType.Get("int");
        try
        {
            Assert.IsAssignableFrom<PyTuple>(Call(Functional(false)));
            Assert.Same(PyNone.Instance, Get(Call(Form("TypeVar"), "T"), "__bound__"));
            Assert.Same(PyNone.Instance, Get(Bound(PyNone.Instance), "__bound__"));
            Assert.Throws<PyNotImplementedError>(() => Functional(true));
            Assert.Throws<PyNotImplementedError>(() => Bound(PyType.Get("int")));
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(empty));
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(filled));
        }
        finally { typing["Generic"] = original; }
        // 恢复canonical后新调用有效，失败不污染静态根来源。
        Assert.Same(PyType.Get("int"), Get(Bound(PyType.Get("int")), "__bound__"));
        Assert.IsAssignableFrom<PyTuple>(Call(Functional(false)));
    }

    [Fact]
    public void EarlierNamedTupleDefinitionAndTypeVarBinderFailuresStillWin()
    {
        var typing = PyTypingGenericRootTests.Typing;
        object? original = Form("Generic");
        PyClass defaults = Begin("Defaults");
        Annotations(defaults).Set("a", PyType.Get("int"));
        Annotations(defaults).Set("b", PyType.Get("int"));
        defaults.SetAttr("a", 1L);
        typing.Remove("Generic");
        try
        {
            Assert.Equal("Non-default namedtuple field b cannot follow default field a",
                Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(defaults)).PyMessage);
            Assert.Equal("typevar() missing required argument 'name' (pos 1)",
                Assert.Throws<PyTypeError>(() => Call(Form("TypeVar"))).PyMessage);
            Assert.Equal("Bivariant types are not supported.", Assert.Throws<PyValueError>(
                () => PyOps.CallValue(Form("TypeVar"), ["T"], ["bound", "covariant", "contravariant"],
                    [PyType.Get("int"), true, true])).PyMessage);
        }
        finally { typing["Generic"] = original; }
    }

    [Fact]
    public void ClassBodyCanObserveChangedExportBeforeFinalConsumption()
    {
        var typing = PyTypingGenericRootTests.Typing;
        object? original = Form("Generic");
        typing.Remove("Generic");
        try
        {
            PyClass cls = Begin("BodyFirst");
            cls.SetAttr("body_ran", true);
            Assert.Equal(true, PyOps.ClassNamespaceGet(cls, "body_ran"));
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(cls));
        }
        finally { typing["Generic"] = original; }
    }
}
