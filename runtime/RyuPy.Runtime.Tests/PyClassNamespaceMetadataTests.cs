// Runtime冷metadata内部合同；source ClassLocal/docstring由独立compiler资产覆盖。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingNamedTupleDefinitionTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyClassNamespaceMetadataTests
{
    private static PyClass Ordinary(string name = "Made", object?[]? bases = null) =>
        (PyClass)PyOps.MakeClass(name, bases ?? [], null);

    [Fact]
    public void KnownSourceIsReadOnceAndRawNamespacePreservesValues()
    {
        int reads = 0;
        object module = new PyList();
        InModule(() => { reads++; return module; }, () =>
        {
            PyClass cls = Ordinary();
            Assert.Equal(1, reads);
            Assert.Same(module, PyOps.ClassNamespaceGet(cls, "__module__"));
            Assert.Equal("Made", PyOps.ClassNamespaceGet(cls, "__qualname__"));
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.Equal(1, reads);
            Assert.Same(module, Get(cls, "__module__"));
        });
    }

    [Fact]
    public void ExplicitMetadataOverridesPreserveIdentityIncludingNone()
    {
        InModule(() => "initial", () =>
        {
            PyClass cls = Ordinary();
            string qualified = new string("Outer.Made".ToCharArray());
            cls.SetAttr("__module__", PyNone.Instance);
            cls.SetAttr("__qualname__", qualified);
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.Same(PyNone.Instance, Get(cls, "__module__"));
            Assert.Same(qualified, Get(cls, "__qualname__"));
        });
    }

    [Fact]
    public void UnknownInternalSourceKeepsLegacyConstruction()
    {
        Assert.Equal(0, PyTraceback.CaptureDepth());
        PyClass cls = Ordinary();
        Assert.False(PyOps.ClassNamespaceContains(cls, "__module__"));
        Assert.False(PyOps.ClassNamespaceContains(cls, "__qualname__"));
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.Equal("Made", Get(cls, "__qualname__"));
        Assert.Equal("__main__", Get(cls, "__module__"));
    }

    [Fact]
    public void OnlyExplicitUnboundIsMissingAndGetterExceptionsKeepIdentity()
    {
        InModule(() => PyUnbound.Instance, () =>
            Assert.Throws<PyNotImplementedError>(() => Ordinary()));
        foreach (PyBaseException error in new PyBaseException[] { new PyNameError("host"), new PyAttributeError("host") })
            InModule(() => throw error, () =>
            {
                try { Ordinary(); Assert.Fail("expected source getter error"); }
                catch (PyBaseException actual) { Assert.Same(error, actual); }
            });
    }

    [Fact]
    public void InvalidOrdinaryQualnameFailsAtFinishAfterBody()
    {
        InModule(() => "initial", () =>
        {
            PyClass cls = Ordinary();
            cls.SetAttr("__qualname__", 7L);
            cls.SetAttr("body_completed", true);
            Assert.True((bool)PyOps.ClassNamespaceGet(cls, "body_completed")!);
            Assert.Equal("type __qualname__ must be a str, not int",
                Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(cls)).Message);
        });
    }

    [Fact]
    public void TypedSourceAlsoReadsOnceAndInvalidQualnameUsesAssignmentError()
    {
        WithRoot(() =>
        {
            int reads = 0;
            InModule(() => { reads++; return "initial"; }, () =>
            {
                PyClass cls = Begin("Made");
                Assert.Equal(1, reads);
                cls.SetAttr("__qualname__", 7L);
                Assert.Equal("can only assign string to Made.__qualname__, not 'int'",
                    Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(cls)).Message);
                Assert.Equal(1, reads);
                Assert.Throws<PyNotImplementedError>(() => cls.Invoke([], [], []));
            });
        });
    }

    [Fact]
    public void IoDefinitionReadsAndOverridesQualnameButLateMutationStaysGuarded()
    {
        PyNativeModules.Register();
        object io = PyObject.GetAttr(PyModules.Import("typing"), "IO")!;
        int reads = 0;
        InModule(() => { reads++; return "io_source"; }, () =>
        {
            PyClass cls = Ordinary("IoMade", [io]);
            Assert.Equal(1, reads);
            Assert.Equal("IoMade", PyOps.ClassNamespaceGet(cls, "__qualname__"));
            cls.SetAttr("__qualname__", "Outer.IoMade");
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.Equal("Outer.IoMade", Get(cls, "__qualname__"));
            Assert.Equal("io_source", Get(cls, "__module__"));
            Assert.Equal(1, reads);
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr("__qualname__", "Late"));
            Assert.Throws<PyNotImplementedError>(() => cls.DeleteAttr("__qualname__"));
            PyClass bad = Ordinary("BadIo", [io]);
            bad.SetAttr("__qualname__", 7L);
            Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(bad));
        });
    }
    [Theory]
    [InlineData("string")]
    [InlineData("user")]
    [InlineData("exception")]
    [InlineData("exception_child")]
    public void OtherActualMakeClassFamiliesKeepSourceAndFinalOverrides(string family)
    {
        int reads = 0;
        object source = new PyList();
        InModule(() => { reads++; return source; }, () =>
        {
            object parent;
            object? factory = null;
            if (family == "string") parent = PyType.Get("str");
            else if (family == "user")
            {
                parent = Ordinary("Parent");
                PyOps.FinalizeOrdinaryClass(parent);
            }
            else
            {
                factory = new PyExceptionFactory((_, _) => new PyValueError("compiled"));
                parent = PyType.Get("ValueError");
                if (family == "exception_child")
                {
                    parent = PyOps.MakeClass("ParentError", [parent], factory);
                    PyOps.FinalizeOrdinaryClass(parent);
                }
            }
            int before = reads;
            var cls = (PyClass)PyOps.MakeClass("Made", [parent], factory);
            Assert.Equal(before + 1, reads);
            Assert.Same(source, PyOps.ClassNamespaceGet(cls, "__module__"));
            Assert.Equal("Made", PyOps.ClassNamespaceGet(cls, "__qualname__"));
            string qualified = new string("Outer.Made".ToCharArray());
            object module = new PyList();
            cls.SetAttr("__module__", module);
            cls.SetAttr("__qualname__", qualified);
            PyOps.FinalizeOrdinaryClass(cls);
            Assert.Same(module, Get(cls, "__module__"));
            Assert.Same(qualified, Get(cls, "__qualname__"));
            Assert.Equal(before + 1, reads);
        });
    }

}
