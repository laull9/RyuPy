// expandtabs 独立冷槽合同：类型查找、有限描述符、raw 三参和异常身份，不改通用绑定器。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextExpandTabsProtocolTests
{
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("expandtabs_slot", body);
    private static object? Expand(object? size) => PyOps.CallMethod("a\t", "expandtabs", [size], [], []);
    private static PyInstance Slot(object? raw, string name = "ExpandIndex")
    {
        var cls = new PyClass(name); cls.SetAttr("__index__", raw); return new PyInstance(cls);
    }

    [Fact]
    public void TypeSlotIgnoresInstanceAndIntFallbackButObservesClassMutation()
    {
        int count = 0; var value = Slot(Fn(_ => { count++; return 4L; }));
        PyObject.SetAttr(value, "__index__", Fn(_ => throw new PyValueError("instance shadow")));
        Assert.Equal("a   ", Expand(value)); Assert.Equal(1, count);
        value.Class.SetAttr("__index__", Fn(_ => 8L)); Assert.Equal("a       ", Expand(value));
        var cls = new PyClass("IntOnly"); cls.SetAttr("__int__", Fn(_ => throw new PyValueError("must not call int")));
        Assert.Equal("'IntOnly' object cannot be interpreted as an integer",
            Assert.Throws<PyTypeError>(() => Expand(new PyInstance(cls))).PyMessage);
    }

    [Fact]
    public void OrdinaryStaticClassPropertyAndCallableSlotsHaveDistinctReceivers()
    {
        var parent = new PyClass("ParentIndex"); var child = (PyClass)PyOps.MakeClass("ChildIndex", [parent], null);
        var value = new PyInstance(child);
        parent.SetAttr("__index__", Fn(args => { Assert.Single(args); Assert.Same(value, args[0]); return 4L; }));
        Assert.Equal("a   ", Expand(value));
        parent.SetAttr("__index__", new PyStaticMethod(Fn(args => { Assert.Empty(args); return 4L; })));
        Assert.Equal("a   ", Expand(value));
        parent.SetAttr("__index__", new PyClassMethod(Fn(args => { Assert.Single(args); Assert.Same(child, args[0]); return 4L; })));
        Assert.Equal("a   ", Expand(value));
        var events = new List<string>();
        parent.SetAttr("__index__", new PyProperty(Fn(args =>
        {
            Assert.Single(args); Assert.Same(value, args[0]); events.Add("get");
            return Fn(call => { Assert.Empty(call); events.Add("call"); return 4L; });
        })));
        Assert.Equal("a   ", Expand(value)); Assert.Equal(new[] { "get", "call" }, events);
        var callable = new PyClass("Callable"); var instance = new PyInstance(callable);
        callable.SetAttr("__call__", Fn(args => { Assert.Single(args); Assert.Same(instance, args[0]); return 4L; }));
        parent.SetAttr("__index__", instance); Assert.Equal("a   ", Expand(value));
    }

    [Fact]
    public void DescriptorLookupIgnoresItsInstanceShadowAndPassesActualIndexClass()
    {
        var descriptorClass = new PyClass("IndexDescriptor"); var descriptor = new PyInstance(descriptorClass);
        var parent = new PyClass("ParentIndex"); parent.SetAttr("__index__", descriptor);
        var child = (PyClass)PyOps.MakeClass("ChildIndex", [parent], null); var value = new PyInstance(child);
        int gets = 0, calls = 0;
        descriptorClass.SetAttr("__get__", Fn(args =>
        {
            Assert.Equal(3, args.Length); Assert.Same(descriptor, args[0]); Assert.Same(value, args[1]); Assert.Same(child, args[2]); gets++;
            return Fn(call => { Assert.Empty(call); calls++; return 4L; });
        }));
        PyObject.SetAttr(descriptor, "__get__", Fn(_ => throw new PyValueError("instance get")));
        Assert.Equal("a   ", Expand(value)); Assert.Equal(1, gets); Assert.Equal(1, calls);
    }

    [Fact]
    public void RawGetIsNotBoundAgainAndRejectsClassMethodAndProperty()
    {
        var descriptorClass = new PyClass("RawGet"); var descriptor = new PyInstance(descriptorClass); var value = Slot(descriptor);
        var raw = Fn(args =>
        {
            Assert.Equal(3, args.Length); Assert.Same(descriptor, args[0]); Assert.Same(value, args[1]); Assert.Same(value.Class, args[2]);
            return Fn(call => { Assert.Empty(call); return 4L; });
        });
        descriptorClass.SetAttr("__get__", new PyStaticMethod(raw)); Assert.Equal("a   ", Expand(value));
        var callableClass = new PyClass("RawCallable"); var callable = new PyInstance(callableClass);
        callableClass.SetAttr("__call__", Fn(args =>
        {
            Assert.Equal(4, args.Length); Assert.Same(callable, args[0]); Assert.Same(descriptor, args[1]);
            Assert.Same(value, args[2]); Assert.Same(value.Class, args[3]); return Fn(_ => 4L);
        }));
        descriptorClass.SetAttr("__get__", callable); Assert.Equal("a   ", Expand(value));
        foreach (object denied in new object[] { new PyClassMethod(raw), new PyStaticMethod(new PyClassMethod(raw)),
            new PyProperty(Fn(_ => throw new PyValueError("getter must not run"))) })
        {
            descriptorClass.SetAttr("__get__", denied);
            string type = denied is PyProperty ? "property" : "classmethod";
            Assert.Equal($"'{type}' object is not callable", Assert.Throws<PyTypeError>(() => Expand(value)).PyMessage);
        }
    }

    [Fact]
    public void GetterResultsRespectStrictCallableGateAndMissingGetterMessage()
    {
        Assert.Equal("property '__index__' of 'MissingGetter' object has no getter",
            Assert.Throws<PyAttributeError>(() => Expand(Slot(new PyProperty(null), "MissingGetter"))).PyMessage);
        foreach (object raw in new object[] { 4L, new PyClassMethod(Fn(_ => 4L)), new PyProperty(Fn(_ => 4L)),
            new PyStaticMethod(new PyClassMethod(Fn(_ => 4L))) })
        {
            string type = raw is long ? "int" : raw is PyProperty ? "property" : "classmethod";
            Assert.Equal($"'{type}' object is not callable",
                Assert.Throws<PyTypeError>(() => Expand(Slot(new PyProperty(Fn(_ => raw))))).PyMessage);
        }
    }

    [Fact]
    public void ExistingPythonExceptionIndexCarrierIsNotLost()
    {
        var cls = new PyClass("ExceptionIndex"); var value = new PyValueError("index payload"); value.BindPythonClass(cls);
        cls.SetAttr("__index__", Fn(args => { Assert.Single(args); Assert.Same(value, args[0]); return 4L; }));
        Assert.Equal("a   ", Expand(value));
        cls.SetAttr("__index__", new PyClassMethod(Fn(args => { Assert.Same(cls, args[0]); return 4L; })));
        Assert.Equal("a   ", Expand(value));
    }

    [Fact]
    public void UserBodiesKeepOriginalExceptionFramesCauseAndContext()
    {
        long depth = PyTraceback.PushFrame("expandtabs_identity.py", "caller");
        var marker = new PyValueError("marker"); var cause = new PyKeyError("cause"); marker.WithCause(cause);
        PyTraceback.SetLine(7); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
        var outer = new PyRuntimeError("outer"); object? token = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(41);
            var descriptorClass = new PyClass("ThrowDescriptor"); var descriptor = new PyInstance(descriptorClass);
            descriptorClass.SetAttr("__get__", Fn(_ => throw marker));
            object[] values = [Slot(Fn(_ => throw marker)), Slot(new PyProperty(Fn(_ => throw marker))), Slot(descriptor)];
            foreach (object value in values) Assert.Same(marker, Assert.Throws<PyValueError>(() => Expand(value)));
            descriptorClass.SetAttr("__get__", Fn(_ => Fn(_ => throw marker)));
            Assert.Same(marker, Assert.Throws<PyValueError>(() => Expand(Slot(descriptor))));
            Assert.Same(frames, marker.Frames); Assert.Same(cause, marker.Cause); Assert.Null(marker.Context); Assert.True(marker.SuppressContextFlag);
            var owned = Assert.Throws<PyTypeError>(() => Expand(Slot(Fn(_ => "bad"))));
            Assert.Same(outer, owned.Context); Assert.Equal(new PyStackFrame("expandtabs_identity.py", "caller", 41), owned.Frames[0]);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.PopTo(depth); }
    }
    [Fact]
    public void NoneSlotAndWideIndexReturnKeepPreciseFailuresAndSingleInvocation()
    {
        Assert.Equal("'NoneType' object is not callable",
            Assert.Throws<PyTypeError>(() => Expand(Slot(PyNone.Instance))).PyMessage);
        foreach (var wide in new[] { System.Numerics.BigInteger.One << 100, -(System.Numerics.BigInteger.One << 100) })
        {
            int calls = 0; var value = Slot(Fn(_ => { calls++; return wide; }));
            Assert.Equal("Python int too large to convert to C int", Assert.Throws<PyOverflowError>(() => Expand(value)).PyMessage);
            Assert.Equal(1, calls);
        }
    }

    [Fact]
    public void ExistingNativeSpecialIndexBoundaryRemainsAvailable()
    {
        var value = new NativeIndex(); Assert.Equal("a   ", Expand(value));
        Assert.Equal(1, value.Lookups); Assert.Equal(1, value.Calls);
    }

    [Fact]
    public void OrdinaryRawGetKeepsLegacySuperContextAndRestoresOuterContextOnBothExits()
    {
        // 此处只保护宿主原有 UsesSuper/TLS 域，不冒充新增 CP 的词法 super 合同。
        var baseClass = new PyClass("BaseDescriptor");
        var owner = (PyClass)PyOps.MakeClass("DescriptorOwner", [baseClass], null);
        var child = (PyClass)PyOps.MakeClass("DescriptorChild", [owner], null);
        var descriptor = new PyInstance(child); var value = Slot(descriptor);
        baseClass.SetAttr("base_value", Fn(args => { Assert.Single(args); Assert.Same(descriptor, args[0]); return 4L; }));
        var marker = new PyValueError("raw get marker"); bool fail = false; int calls = 0;
        PyFunction body = Fn(args =>
        {
            calls++; Assert.Equal(3, args.Length); Assert.Same(descriptor, args[0]);
            Assert.Same(value, args[1]); Assert.Same(value.Class, args[2]);
            var proxy = Assert.IsType<PySuper>(PyMethodContext.MakeSuper());
            Assert.Same(owner, proxy.Owner); Assert.Same(descriptor, proxy.Self);
            object? result = PyOps.CallValue(PyObject.GetAttr(proxy, "base_value"), [], [], []);
            Assert.Equal(4L, result);
            if (fail) throw marker;
            return Fn(call => { Assert.Empty(call); return result; });
        });
        owner.SetAttr("__get__", new PyFunction(body.Name, body.Signature, body.Body, body.Defaults, usesSuper: true));
        var outerOwner = new PyClass("OuterOwner"); var outerSelf = new PyInstance(outerOwner);
        PyClass? previousOwner = PyMethodContext.CurrentOwner; object? previousSelf = PyMethodContext.CurrentSelf;
        PyMethodContext.SetCurrent(outerOwner, outerSelf);
        try
        {
            Assert.Equal("a   ", Expand(value)); Assert.Equal(1, calls);
            Assert.Same(outerOwner, PyMethodContext.CurrentOwner); Assert.Same(outerSelf, PyMethodContext.CurrentSelf);
            fail = true; Assert.Same(marker, Assert.Throws<PyValueError>(() => Expand(value))); Assert.Equal(2, calls);
            Assert.Same(outerOwner, PyMethodContext.CurrentOwner); Assert.Same(outerSelf, PyMethodContext.CurrentSelf);
        }
        finally { PyMethodContext.SetCurrent(previousOwner, previousSelf); }
    }

    private sealed class NativeIndex : PyNativeObject
    {
        internal int Lookups, Calls;
        public override string TypeName => "NativeIndex";
        public override object? GetAttr(string name)
        {
            if (name != "__index__") throw Missing(name);
            Lookups++;
            return Fn(args => { Assert.Empty(args); Calls++; return 4L; });
        }
    }

}
