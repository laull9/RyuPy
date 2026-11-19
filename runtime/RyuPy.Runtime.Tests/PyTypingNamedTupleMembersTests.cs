// C-members内部成员/访问器与旧模式隔离测试；class源码与compiler元数据由独立资产验证。
using System;
using System.Collections.Generic;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingNamedTupleDefinitionTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingNamedTupleMembersTests
{
    private static PyFunction Function(string name, string[] parameters, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, new object?[parameters.Length], parameters.Length, body);
    private static PyClass FieldClass()
    {
        PyClass cls = Begin("Members");
        Annotations(cls).Set("x", PyType.Get("int"));
        return cls;
    }

    [Fact]
    public void OrdinaryStaticAndClassMethodsKeepTheirRealReceivers()
    {
        Supported(() =>
        {
            PyClass cls = FieldClass();
            var function = Function("read", ["self"], args => Get(args[0], "x"));
            cls.SetAttr("read", function);
            cls.SetAttr("add", new PyStaticMethod(Function("add", ["a", "b"], args => (long)args[0]! + (long)args[1]!)));
            cls.SetAttr("owner", new PyClassMethod(Function("owner", ["cls"], args => args[0])));
            cls.SetAttr("constant", "kept");
            PyOps.FinalizeOrdinaryClass(cls);
            object instance = cls.Invoke([3L], [], [])!;
            object? bound = Get(instance, "read");
            Assert.Equal(3L, Call(bound));
            Assert.Same(function, Get(cls, "read"));
            Assert.Same(instance, Assert.IsType<PyBoundMethod>(bound).Self);
            Assert.Same(cls, Assert.IsType<PyBoundMethod>(bound).Owner);
            Assert.Equal(9L, Call(Get(instance, "add"), 4L, 5L));
            Assert.Equal(9L, Call(Get(cls, "add"), 4L, 5L));
            Assert.Same(cls, Call(Get(instance, "owner")));
            Assert.Same(cls, Call(Get(cls, "owner")));
            Assert.Equal("kept", Get(instance, "constant"));
        });
    }

    [Fact]
    public void PropertyGetSetDeleteUsesAccessorAndNeverChangesFields()
    {
        Supported(() =>
        {
            var trace = new List<string>();
            PyClass cls = FieldClass();
            var property = new PyProperty(
                Function("get", ["self"], args => { trace.Add("get"); return Get(args[0], "x"); }),
                Function("set", ["self", "value"], args => { trace.Add("set:" + args[1]); return 99L; }),
                Function("del", ["self"], _ => { trace.Add("delete"); return 88L; }));
            cls.SetAttr("value", property);
            PyOps.FinalizeOrdinaryClass(cls);
            object instance = cls.Invoke([3L], [], [])!;
            Assert.Same(property, Get(cls, "value"));
            Assert.Equal(3L, Get(instance, "value"));
            PyObject.SetAttr(instance, "value", 7L);
            PyObject.DelAttr(instance, "value");
            Assert.Equal(3L, Get(instance, "x"));
            Assert.Equal(new[] { "get", "set:7", "delete" }, trace);
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "x", 8L));
            Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(instance, "x"));
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "unknown", 8L));
        });
    }

    [Fact]
    public void MissingAccessorsRaiseAttributeErrorWithoutCreatingStorage()
    {
        Supported(() =>
        {
            PyClass cls = FieldClass();
            cls.SetAttr("empty", new PyProperty(null));
            PyOps.FinalizeOrdinaryClass(cls);
            object instance = cls.Invoke([3L], [], [])!;
            Assert.Contains("no getter", Assert.Throws<PyAttributeError>(() => Get(instance, "empty")).Message);
            Assert.Contains("no setter", Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "empty", 2L)).Message);
            Assert.Contains("no deleter", Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(instance, "empty")).Message);
        });
    }

    [Fact]
    public void AccessorErrorsKeepIdentityAndExternalEffects()
    {
        Supported(() =>
        {
            var trace = new List<string>();
            var error = new PyValueError("setter-error");
            PyClass cls = FieldClass();
            cls.SetAttr("value", new PyProperty(null, Function("set", ["self", "value"], _ =>
            {
                trace.Add("before-error");
                throw error;
            })));
            PyOps.FinalizeOrdinaryClass(cls);
            object instance = cls.Invoke([3L], [], [])!;
            Assert.Same(error, Assert.Throws<PyValueError>(() => PyObject.SetAttr(instance, "value", 8L)));
            Assert.Equal(new[] { "before-error" }, trace);
            Assert.Equal(3L, Get(instance, "x"));
        });
    }

    [Fact]
    public void LateClassMutationRejectsBeforeVersionOrSavedMethodChanges()
    {
        Supported(() =>
        {
            PyClass cls = FieldClass();
            var method = Function("read", ["self"], args => Get(args[0], "x"));
            cls.SetAttr("read", method);
            PyOps.FinalizeOrdinaryClass(cls);
            object? bound = Get(cls.Invoke([4L], [], []), "read");
            long before = cls.Version;
            Assert.Throws<PyNotImplementedError>(() => cls.SetAttr("read", PyNone.Instance));
            Assert.Throws<PyNotImplementedError>(() => cls.DeleteAttr("read"));
            Assert.Equal(before, cls.Version);
            Assert.Same(method, Get(cls, "read"));
            Assert.Equal(4L, Call(bound));
        });
    }

    [Fact]
    public void MutableDescriptorCandidatesAreRejectedAsConstantsButRemainValidDefaults()
    {
        Supported(() =>
        {
            var objectClass = new PyClass("LaterDescriptor");
            PyOps.FinalizeOrdinaryClass(objectClass);
            object candidate = objectClass.Invoke([], [], [])!;
            PyClass bad = Begin("BadConstant");
            bad.SetAttr("constant", candidate);
            Assert.Contains("descriptor", Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(bad)).Message);
            PyClass good = FieldClass();
            good.SetAttr("x", candidate);
            PyOps.FinalizeOrdinaryClass(good);
            Assert.Same(candidate, Get(good.Invoke([], [], []), "x"));
        });
    }

    [Fact]
    public void CollectionsModeDoesNotAcquireTypedPropertySemantics()
    {
        PyNativeModules.Register();
        var cls = (PyClass)PyOps.CallValue(PyModules.Import("collections").GetAttr("namedtuple"),
            ["Plain", "x"], ["module"], ["source_module"])!;
        int calls = 0;
        var property = new PyProperty(Function("get", ["self"], _ => { calls++; return 1L; }));
        // 仅测试内部旧mode隔离；Python类属性修改仍受GuardMutation禁止。
        cls.Attributes["value"] = property;
        object instance = cls.Invoke([2L], [], [])!;
        Assert.Same(property, Get(instance, "value"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "value", 3L));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(instance, "value"));
        Assert.Equal(0, calls);
    }
}
