// 公共format的绑定、类型级协议、返回身份与异常边界；不通过旧f-string间接验收。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBuiltinFormatProtocolTests
{
    private static object? Format(params object?[] args) => PyOps.CallBuiltin("format", args, [], []);
    private static PyFunction Method(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("__format__", body);
    private static PyClass TextClass()
    {
        var cls = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls); return cls;
    }

    [Fact]
    public void BinderChecksKeywordsThenArityThenSpecWithoutCallingUserCode()
    {
        var cls = new PyClass("Never"); int calls = 0;
        cls.SetAttr("__format__", Method(_ => { calls++; return "bad"; }));
        var value = new PyInstance(cls);
        Assert.Equal("format() takes no keyword arguments", Assert.Throws<PyTypeError>(() =>
            PyOps.CallBuiltin("format", [value, null, 3L], ["z"], [4L])).Message);
        Assert.Equal("format expected at least 1 argument, got 0", Assert.Throws<PyTypeError>(() => Format()).Message);
        Assert.Equal("format expected at most 2 arguments, got 3", Assert.Throws<PyTypeError>(() => Format(value, "", 3L)).Message);
        Assert.Equal("format() argument 2 must be str, not None", Assert.Throws<PyTypeError>(() => Format(value, PyNone.Instance)).Message);
        Assert.Equal("format() argument 2 must be str, not int", Assert.Throws<PyTypeError>(() => Format(value, 1L)).Message);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void UserSlotGetsOriginalSpecAndReturnsOriginalStringSubclassOnce()
    {
        var text = TextClass(); object spec = text.Invoke(["arbitrary !?"], [], [])!;
        object returned = text.Invoke(["result"], [], [])!;
        text.SetAttr("__str__", Method(_ => throw new Exception("must not convert")));
        var cls = new PyClass("Value"); var value = new PyInstance(cls); int calls = 0;
        cls.SetAttr("__format__", Method(args =>
        {
            Assert.Same(value, args[0]); Assert.Same(spec, args[1]); calls++; return returned;
        }));
        PyObject.SetAttr(value, "__format__", Method(_ => throw new Exception("instance attribute")));
        Assert.Same(returned, Format(value, spec)); Assert.Equal(1, calls);
        Assert.Same(returned, PyOps.CallValue(PyOps.BuiltinFunction("format"), [value, spec], [], []));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void DefaultEmptyUsesStrObjectAndNonemptyNeverCallsStr()
    {
        var text = TextClass(); object result = text.Invoke(["raw"], [], [])!;
        var cls = new PyClass("Default"); int calls = 0;
        cls.SetAttr("__str__", Method(_ => { calls++; return result; }));
        var value = new PyInstance(cls);
        Assert.Same(result, Format(value)); Assert.Equal(1, calls);
        Assert.Equal("unsupported format string passed to Default.__format__", Assert.Throws<PyTypeError>(() => Format(value, ">9")).Message);
        Assert.Equal(1, calls);
        Assert.Equal("None", Format(PyNone.Instance)); Assert.Equal("[]", Format(new PyList()));
        Assert.Throws<PyTypeError>(() => Format(new PyList(), ">3"));
    }

    [Fact]
    public void ClassLookupSupportsLateMutationAndStaticClassPropertyAndCallableShapes()
    {
        var parent = new PyClass("Parent"); var child = (PyClass)PyOps.MakeClass("Child", [parent], null);
        var value = new PyInstance(child);
        parent.SetAttr("__format__", Method(_ => "old")); Assert.Equal("old", Format(value));
        parent.SetAttr("__format__", new PyStaticMethod(Method(args => { Assert.Single(args); return "static"; })));
        Assert.Equal("static", Format(value));
        parent.SetAttr("__format__", new PyClassMethod(Method(args => { Assert.Same(child, args[0]); return "class"; })));
        Assert.Equal("class", Format(value));
        var log = new List<string>();
        parent.SetAttr("__format__", new PyProperty(Method(args =>
        {
            Assert.Same(value, args[0]); log.Add("get");
            return Method(call => { Assert.Single(call); log.Add("call"); return "property"; });
        })));
        Assert.Equal("property", Format(value)); Assert.Equal(new[] { "get", "call" }, log);
        var callable = new PyClass("Callable");
        callable.SetAttr("__call__", Method(args => { Assert.Equal(2, args.Length); Assert.Equal("spec", args[1]); return "callable"; }));
        parent.SetAttr("__format__", new PyInstance(callable)); Assert.Equal("callable", Format(value, "spec"));
    }

    [Fact]
    public void InvalidReturnsAndBodyExceptionsDoNotFallbackOrReinvoke()
    {
        var cls = new PyClass("Bad"); var value = new PyInstance(cls); int calls = 0;
        cls.SetAttr("__str__", Method(_ => throw new Exception("fallback forbidden")));
        foreach (object? result in new object?[] { PyNone.Instance, 7L, PyNotImplemented.Instance })
        {
            cls.SetAttr("__format__", Method(_ => { calls++; return result; }));
            Assert.Throws<PyTypeError>(() => Format(value));
        }
        Assert.Equal(3, calls);
        var error = new PyValueError("body sentinel");
        cls.SetAttr("__format__", Method(_ => { calls++; throw error; }));
        Assert.Same(error, Assert.Throws<PyValueError>(() => Format(value))); Assert.Equal(4, calls);
        cls.SetAttr("__format__", PyNone.Instance);
        Assert.Equal("'NoneType' object is not callable", Assert.Throws<PyTypeError>(() => Format(value)).Message);
    }

    [Fact]
    public void UnknownNativeAndPythonDescriptorsFailExplicitlyWithoutAttributeProbing()
    {
        var native = new UnknownNative(); Assert.Throws<PyNotImplementedError>(() => Format(native));
        Assert.Equal(0, native.Calls);
        var desc = new PyClass("Descriptor"); desc.SetAttr("__get__", Method(_ => throw new Exception("unsupported getter")));
        var cls = new PyClass("Holder"); cls.SetAttr("__format__", new PyInstance(desc));
        Assert.Throws<PyNotImplementedError>(() => Format(new PyInstance(cls)));
    }

    [Fact]
    public void OwnedBindingParsingAndReturnErrorsCaptureContextWithoutChangingAmbientHandler()
    {
        var outer = new PyValueError("outer"); object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("format_use.py", "caller");
        try
        {
            PyTraceback.SetLine(12);
            var cls = new PyClass("BadReturn"); cls.SetAttr("__format__", Method(_ => 7L));
            Action[] failures = [() => Format(), () => Format("x", "+s"), () => Format(new PyInstance(cls))];
            foreach (Action fail in failures)
            {
                PyBaseException error = Assert.IsAssignableFrom<PyBaseException>(Record.Exception(fail));
                Assert.Same(outer, error.Context); Assert.Null(error.Cause);
                Assert.Equal(new PyStackFrame("format_use.py", "caller", 12), error.Frames[0]);
                Assert.Same(outer, PyErrors.Current);
            }
            object?[] targets = [PyNone.Instance, new PyStaticMethod(PyNone.Instance),
                new PyClassMethod(PyNone.Instance), new PyProperty(Method(_ => 7L)), new PyProperty(7L),
                new PyInstance(new PyClass("NotCallable")), PyBaseObject.Create([], [])];
            foreach (object? target in targets)
            {
                cls.SetAttr("__format__", target);
                var error = Assert.Throws<PyTypeError>(() => Format(new PyInstance(cls)));
                Assert.Same(outer, error.Context); Assert.Null(error.Cause);
                Assert.Equal(new PyStackFrame("format_use.py", "caller", 12), error.Frames[0]);
                Assert.Same(outer, PyErrors.Current);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    private sealed class UnknownNative : PyNativeObject
    {
        internal int Calls;
        public override string TypeName => "Unknown";
        public override object? GetAttr(string name) { Calls++; throw new Exception("ordinary GetAttr forbidden"); }
        public override string PythonStr() => throw new Exception("fallback forbidden");
    }
}
