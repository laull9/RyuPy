// translate的公开入口合同：映射/删除、严格返回类型与原异常传播；动态副作用另由CP差分覆盖。
using System;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextTranslateTests
{
    private static object? Call(string route, object source, object?[] args, string[]? names = null)
    {
        names ??= [];
        object?[] values = Array.ConvertAll(names, _ => (object?)new PyDict());
        if (route == "direct") return PyOps.CallMethod(source, "translate", args, names, values);
        if (route == "root")
            return PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), "translate"), [source, .. args], names, values);
        object? target = route == "saved" ? PyObject.GetAttr(source, "translate") : PyOps.PrepareMethod(source, "translate");
        return PyOps.CallValue(target, args, names, values);
    }

    [Fact]
    public void AllRoutesPreserveMappingsDeletionAndUnicode()
    {
        var mapping = new PyDict();
        mapping.Set(97L, "XY"); mapping.Set(98L, PyNone.Instance);
        mapping.Set(99L, 0x1F642L); mapping.Set(0x1F600L, "");
        foreach (string route in new[] { "direct", "saved", "prepared", "root" })
        {
            Assert.Equal("XY🙂é", Call(route, "abc😀é", [mapping]));
            Assert.Equal("xyAZ", Call(route, "\0\u0001AZ", ["xy"]));
            Assert.Equal("", Call(route, "", [PyNone.Instance]));
            Assert.Throws<PyTypeError>(() => Call(route, "a", []));
            Assert.Throws<PyTypeError>(() => Call(route, "a", [mapping], ["table"]));
        }
    }

    [Fact]
    public void InvalidReturnValuesFailWithoutIndexCoercion()
    {
        foreach (object invalid in new object[] { -1L, 0x110000L, BigInteger.Pow(10, 100) })
        {
            var map = new PyDict(); map.Set(97L, invalid);
            Assert.Equal("character mapping must be in range(0x110000)",
                Assert.Throws<PyValueError>(() => Call("direct", "a", [map])).Message);
        }
        foreach (object invalid in new object[] { 1.5, new PyList() })
        {
            var map = new PyDict(); map.Set(97L, invalid);
            Assert.Equal("character mapping must return integer, None or str",
                Assert.Throws<PyTypeError>(() => Call("direct", "a", [map])).Message);
        }
    }

    [Fact]
    public void TableErrorsKeepTheirIdentityAndContext()
    {
        var cls = (PyClass)PyOps.MakeClass("TranslateTable", [], null);
        var failure = new PyValueError("mapping failed");
        cls.SetAttr("__getitem__", PyNativeFunctions.CreateVariadic("getitem", _ => throw failure));
        PyOps.FinalizeOrdinaryClass(cls);
        object table = cls.Invoke([], [], [])!;
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Call("direct", "a", [table])));
    }
    [Fact]
    public void SwallowedLookupErrorsRestoreFramesButPropagatedErrorsKeepThem()
    {
        long caller = PyTraceback.PushFrame("translate_trace.py", "caller");
        var outer = new PyRuntimeError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            long before = PyTraceback.CaptureDepth();
            var cls = (PyClass)PyOps.MakeClass("MissingTranslateTable", [], null);
            cls.SetAttr("__getitem__", PyNativeFunctions.CreateVariadic("missing", _ =>
            {
                PyTraceback.PushFrame("translate_trace.py", "missing");
                throw (PyBaseException)PyErrors.PrepareRaise(new PyKeyError("missing"));
            }));
            PyOps.FinalizeOrdinaryClass(cls);
            object table = cls.Invoke([], [], [])!;
            string source = new string('é', 1500);
            Assert.Equal(source, Call("direct", source, [table]));
            Assert.Equal(before, PyTraceback.CaptureDepth());

            // 随后的本层错误不能携带已吞回调帧。
            var bad = new PyDict(); bad.Set(97L, -1L);
            var after = Assert.Throws<PyValueError>(() => Call("direct", "a", [bad]));
            Assert.Same(outer, after.Context);
            Assert.Single(after.Frames);
            Assert.Equal("caller", after.Frames[0].Function);

            cls.SetAttr("__getitem__", PyNativeFunctions.CreateVariadic("broken", _ =>
            {
                PyTraceback.PushFrame("translate_trace.py", "broken");
                throw (PyBaseException)PyErrors.PrepareRaise(new PyValueError("real failure"));
            }));
            var propagated = Assert.Throws<PyValueError>(() => Call("direct", "a", [table]));
            Assert.Same(outer, propagated.Context);
            Assert.Equal(before + 1, PyTraceback.CaptureDepth());
            Assert.Equal("broken", propagated.Frames[0].Function);
            Assert.Equal("caller", propagated.Frames[1].Function);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(caller); }
    }

    [Fact]
    public void NonSubscriptableTablesKeepActiveExceptionAndCaller()
    {
        long depth = PyTraceback.PushFrame("translate_context.py", "caller");
        var outer = new PyRuntimeError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            foreach (object table in new object[] { PyNone.Instance, 1L })
            {
                var error = Assert.Throws<PyTypeError>(() => Call("direct", "a", [table]));
                Assert.Same(outer, error.Context);
                Assert.Single(error.Frames);
                Assert.Equal("caller", error.Frames[0].Function);
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void DirectGetItemPreparesOnlyItsOwnFailure()
    {
        long depth = PyTraceback.PushFrame("getitem_context.py", "caller");
        var outer = new PyRuntimeError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var own = Assert.Throws<PyTypeError>(() => PyOps.GetItem(1L, 0L));
            Assert.Same(outer, own.Context);
            Assert.Single(own.Frames);
            var failure = new PyTypeError("user failure");
            long origin = PyTraceback.PushFrame("getitem_origin.py", "original");
            PyErrors.PrepareRaise(failure);
            PyTraceback.PopTo(origin);
            var frames = failure.Frames;
            var cls = (PyClass)PyOps.MakeClass("PreservedTable", [], null);
            cls.SetAttr("__getitem__", PyNativeFunctions.CreateVariadic("getitem", _ => throw failure));
            PyOps.FinalizeOrdinaryClass(cls);
            object table = cls.Invoke([], [], [])!;
            Assert.Same(failure, Assert.Throws<PyTypeError>(() => PyOps.GetItem(table, 0L)));
            Assert.Same(frames, failure.Frames);
            Assert.Same(outer, failure.Context);
            Assert.Equal("original", failure.Frames[0].Function);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

}
