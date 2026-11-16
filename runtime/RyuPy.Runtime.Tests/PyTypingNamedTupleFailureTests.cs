// B2有限域错误先后与失败壳封口；用户比较回调/方法/property成功仍留C或pending。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingNamedTupleDefinitionTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingNamedTupleFailureTests
{
    [Fact]
    public void DefaultOrderFailurePrecedesUnsupportedAnnotationAndLeavesShellClosed()
    {
        Supported(() =>
        {
            PyClass cls = Begin();
            Annotations(cls).Set("x", new PyList());
            Annotations(cls).Set("y", PyType.Get("int"));
            cls.SetAttr("x", 1L);
            Assert.Equal("Non-default namedtuple field y cannot follow default field x",
                Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(cls)).Message);
            Assert.Equal(1L, PyOps.ClassNamespaceGet(cls, "x"));
            Assert.Throws<PyNotImplementedError>(() => cls.Invoke([], [], []));
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(cls));
            Assert.Throws<PyNotImplementedError>(() => PyOps.DeclareDataclass(cls, ["x"], false));
        });
    }

    [Fact]
    public void MissingModuleArgumentPrecedesAnnotationConversion()
    {
        Supported(() =>
        {
            PyClass cls = Begin();
            Annotations(cls).Set("x", new PyList());
            Assert.True(cls.DeleteAttr("__module__"));
            Assert.Throws<PyKeyError>(() => PyOps.FinalizeOrdinaryClass(cls));
        });
    }

    [Fact]
    public void AnnotationFailurePrecedesActualFactoryReplacementAndRestorationWorks()
    {
        Supported(() =>
        {
            PyModule collections = PyModules.Import("collections");
            object? original = collections.GetAttr("namedtuple");
            try
            {
                collections.SetAttr("namedtuple", PyNone.Instance);
                PyClass badAnnotation = Begin("BadAnnotation");
                Annotations(badAnnotation).Set("x", Typing["Union"]);
                Assert.Contains("Plain typing.Union", Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(badAnnotation)).Message);
                PyClass badFactory = Begin("BadFactory");
                Annotations(badFactory).Set("x", PyType.Get("int"));
                Assert.Contains("replaced collections.namedtuple", Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(badFactory)).Message);
            }
            finally { collections.SetAttr("namedtuple", original); }
            PyClass restored = Begin("Restored");
            PyOps.FinalizeOrdinaryClass(restored);
            Assert.IsAssignableFrom<PyTuple>(restored.Invoke([], [], []));
        });
    }

    [Fact]
    public void NameValidationPrecedesProhibitedMemberAndNoneModuleFallback()
    {
        Supported(() =>
        {
            PyClass cls = Begin();
            Annotations(cls).Set("_x", PyType.Get("int"));
            cls.SetAttr("__new__", PyNone.Instance);
            cls.SetAttr("__module__", PyNone.Instance);
            Assert.Contains("underscore", Assert.Throws<PyValueError>(() => PyOps.FinalizeOrdinaryClass(cls)).Message);
            PyClass forbidden = Begin("Forbidden");
            forbidden.SetAttr("_make", 1L);
            Assert.Equal("Cannot overwrite NamedTuple attribute _make",
                Assert.Throws<PyAttributeError>(() => PyOps.FinalizeOrdinaryClass(forbidden)).Message);
            Assert.Throws<PyNotImplementedError>(() => forbidden.Invoke([], [], []));
        });
    }

    [Fact]
    public void NonDictAnnotationsAndMagicRemainBoundariesWhileOrdinaryMethodsWork()
    {
        Supported(() =>
        {
            PyClass mapping = Begin("Mapping");
            mapping.SetAttr("__annotations__", new PyList());
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(mapping));
            PyClass method = Begin("Method");
            method.SetAttr("method", PyNativeFunctions.Create("method", ["self"], [null], 1, _ => 1L));
            PyOps.FinalizeOrdinaryClass(method);
            Assert.Equal(1L, PyOps.CallMethod(method.Invoke([], [], []), "method", [], [], []));
            PyClass magic = Begin("Magic");
            magic.SetAttr("__repr__", PyNone.Instance);
            Assert.Contains("class slot __repr__", Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(magic)).Message);
        });
    }

    [Fact]
    public void AddedTypingCollectionsGlobalCannotBeIgnored()
    {
        Supported(() =>
        {
            bool existed = Typing.TryGetValue("collections", out object? old);
            try
            {
                Typing["collections"] = PyNone.Instance;
                PyClass cls = Begin();
                Assert.Contains("typing.collections", Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(cls)).Message);
            }
            finally { if (existed) Typing["collections"] = old; else Typing.Remove("collections"); }
        });
    }
}
