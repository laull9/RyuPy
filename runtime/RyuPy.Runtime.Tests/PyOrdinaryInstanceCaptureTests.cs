// 普通实例预捕获合同：按生成代码的捕获→求参→调用顺序验证，不将直接PIC当成预捕获。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyOrdinaryInstanceCaptureTests
{
    private static PyFunction Function(string name, PyCall body) =>
        new(name, PySignature.Create(name, [], [], []), body, [], false);

    private static object? Invoke(PyInstance instance, string name, Func<object?[]> arguments)
    {
        bool captured = PyOps.RequiresEarlyMethodCapture(instance);
        object? target = PyOps.CaptureMethodTarget(instance, name);
        Assert.True(captured);
        return PyOps.CallCapturedMethod(captured, target, name, arguments(), [], []);
    }

    [Fact]
    public void InstanceAndClassRebindingCannotReplaceCapturedFunction()
    {
        var cls = new PyClass("CapturePlain"); var instance = new PyInstance(cls);
        cls.SetAttr("f", Function("old", (_, args, _, _) => args[0]));
        object? original = PyOps.CaptureMethodTarget(instance, "f");
        PyObject.SetAttr(instance, "f", Function("instance", (_, _, _, _) => 99L));
        cls.SetAttr("f", Function("class", (_, _, _, _) => 101L));
        Assert.Same(instance, PyPic.CallCapturedMethod0(4820, true, original, "f"));
        object? replacement = PyOps.CaptureMethodTarget(instance, "f");
        PyObject.DelAttr(instance, "f");
        Assert.Equal(99L, PyPic.CallCapturedMethod0(4820, true, replacement, "f"));
        replacement = PyOps.CaptureMethodTarget(instance, "f");
        Assert.True(cls.DeleteAttr("f"));
        Assert.Equal(101L, PyPic.CallCapturedMethod0(4820, true, replacement, "f"));
        Assert.Throws<PyAttributeError>(() => PyOps.CaptureMethodTarget(instance, "f"));
    }

    [Fact]
    public void PropertyAndGetattrRunOnceBeforeArguments()
    {
        foreach (bool fallback in new[] { false, true })
        {
            var cls = new PyClass("CaptureLookup"); var instance = new PyInstance(cls);
            var events = new List<string>();
            object? function = Function("old", (_, _, _, _) => { events.Add("old"); return 7L; });
            PyFunction getter = Function("getter", (_, _, _, _) => { events.Add("get"); return function; });
            cls.SetAttr(fallback ? "__getattr__" : "f", fallback ? getter : new PyProperty(getter));
            Assert.Equal(7L, Invoke(instance, "f", () =>
            {
                events.Add("args"); function = Function("new", (_, _, _, _) => 99L); return [];
            }));
            Assert.Equal(new[] { "get", "args", "old" }, events);
        }
    }

    [Fact]
    public void OwnedMissingErrorsPrecedeArgumentsAndCarryCallerContext()
    {
        var cls = new PyClass("CaptureMissing"); var instance = new PyInstance(cls);
        cls.SetAttr("unreadable", new PyProperty(null));
        long depth = PyTraceback.PushFrame("instance_capture.py", "caller");
        var active = new PyValueError("active"); object? previous = PyErrors.EnterHandler(active);
        int arguments = 0;
        try
        {
            PyTraceback.SetLine(37);
            foreach (string name in new[] { "missing", "unreadable" })
            {
                var error = Assert.Throws<PyAttributeError>(() => Invoke(instance, name, () =>
                { arguments++; return []; }));
                Assert.Same(active, error.Context);
                Assert.Equal(new PyStackFrame("instance_capture.py", "caller", 37), error.Frames[0]);
            }
            Assert.Equal(0, arguments);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void UserGetterErrorsRetainIdentityWithoutBroadPreparation()
    {
        foreach (bool fallback in new[] { false, true })
        {
            var cls = new PyClass("CaptureUserError"); var instance = new PyInstance(cls);
            var sentinel = new PyValueError("from getter");
            PyFunction getter = Function("getter", (_, _, _, _) => throw sentinel);
            cls.SetAttr(fallback ? "__getattr__" : "f", fallback ? getter : new PyProperty(getter));
            var active = new PyKeyError("caller handler"); object? previous = PyErrors.EnterHandler(active);
            try
            {
                Assert.Same(sentinel, Assert.Throws<PyValueError>(() => PyOps.CaptureMethodTarget(instance, "f")));
                Assert.Null(sentinel.Context); Assert.Empty(sentinel.Frames);
            }
            finally { PyErrors.LeaveHandler(previous); }
        }
    }

    [Fact]
    public void NonCallableWaitsForArgumentExpansionAndArgumentErrorsWin()
    {
        var cls = new PyClass("CaptureNonCallable"); var instance = new PyInstance(cls);
        PyObject.SetAttr(instance, "f", 42L);
        object? target = PyOps.CaptureMethodTarget(instance, "f"); int calls = 0;
        Assert.Throws<PyTypeError>(() => PyOps.CallCapturedMethod(true, target, "f",
            [PyOps.StarArg(new Once(() => calls++))], [], []));
        Assert.Equal(1, calls);
        var sentinel = new PyValueError("argument failure");
        Assert.Same(sentinel, Assert.Throws<PyValueError>(() => PyOps.CallCapturedMethod(true, target, "f",
            [PyOps.StarArg(new Once(() => throw sentinel))], [], [])));
    }

    [Fact]
    public void DirectArrayEntryCapturesBeforeStarAndDoubleStarExpansion()
    {
        var cls = new PyClass("CaptureArrays"); var instance = new PyInstance(cls);
        PyFunction old = Function("old", (_, args, names, values) =>
        {
            Assert.Equal(new object?[] { 1L }, args); Assert.Equal(new[] { "b" }, names);
            Assert.Equal(new object?[] { 2L }, values); return 3L;
        });
        PyObject.SetAttr(instance, "f", old);
        var keywords = new PyDict(); keywords.Set("b", 2L);
        var iterator = new Once(() => PyObject.SetAttr(instance, "f", Function("new", (_, _, _, _) => 99L)));
        Assert.Equal(3L, PyOps.CallMethod(instance, "f", [PyOps.StarArg(iterator)], [null], [keywords]));
    }

    [Fact]
    public void FixedArityAndArrayCapturedCallsRetainIndependentArgumentsAcrossReentry()
    {
        var cls = new PyClass("CaptureRetention"); var instance = new PyInstance(cls);
        var retained = new List<object?[]>(); bool nested = false; object? target = null;
        var function = Function("retained", (_, args, _, _) =>
        {
            retained.Add(args); object?[] before = args.ToArray();
            if (!nested)
            {
                nested = true;
                PyPic.CallCapturedMethod1(4821, true, target, "f", 99L);
                nested = false;
            }
            Assert.Equal(before, args); return args.Length;
        });
        // 实例字典里的函数不额外绑定self，便于直接核对传入数组。
        PyObject.SetAttr(instance, "f", function); target = PyOps.CaptureMethodTarget(instance, "f");
        Assert.Equal(0, PyPic.CallCapturedMethod0(4821, true, target, "f"));
        Assert.Equal(1, PyPic.CallCapturedMethod1(4821, true, target, "f", 10L));
        Assert.Equal(2, PyPic.CallCapturedMethod2(4821, true, target, "f", 20L, 21L));
        Assert.Equal(3, PyPic.CallCapturedMethod3(4821, true, target, "f", 30L, 31L, 32L));
        Assert.Equal(4, PyOps.CallCapturedMethod(true, target, "f", [40L, 41L, 42L, 43L], [], []));
        Assert.Equal(new object?[] { 20L, 21L }, retained[4]);
        Assert.Equal(new object?[] { 30L, 31L, 32L }, retained[6]);
        for (int i = 0; i < retained.Count; i++)
            for (int j = i + 1; j < retained.Count; j++)
                if (retained[i].Length != 0 && retained[j].Length != 0) Assert.NotSame(retained[i], retained[j]);
    }

    [Fact]
    public void StaticAndClassMethodsKeepTheirOriginalBinding()
    {
        var cls = new PyClass("CaptureDescriptors"); var instance = new PyInstance(cls);
        cls.SetAttr("s", new PyStaticMethod(Function("static", (_, args, _, _) => args.Length)));
        cls.SetAttr("c", new PyClassMethod(Function("class", (_, args, _, _) => args[0])));
        object? stat = PyOps.CaptureMethodTarget(instance, "s");
        object? bound = PyOps.CaptureMethodTarget(instance, "c");
        cls.SetAttr("s", 42L); cls.SetAttr("c", 42L);
        Assert.Equal(0, PyPic.CallCapturedMethod0(4822, true, stat, "s"));
        Assert.Same(cls, PyPic.CallCapturedMethod0(4822, true, bound, "c"));
    }

    [Fact]
    public void EmptyDictionaryAndUninitializedCallableSlotsPrepareOwnedLookupErrors()
    {
        var empty = new PyClass("CaptureEmptySlots"); empty.PrepareMultipleBases([]);
        empty.Attributes["__slots__"] = new PyTuple([]); PyOps.FinalizeOrdinaryClass(empty);
        var storage = new PyClass("CaptureNativeSlots"); storage.PrepareMultipleBases([]);
        storage.ConfigureNativeSlots(["_mapping"]); PyOps.FinalizeOrdinaryClass(storage);
        var emptyInstance = new PyInstance(empty); var slotInstance = new PyInstance(storage);
        long depth = PyTraceback.PushFrame("capture_slots.py", "caller");
        var active = new PyValueError("active-slot"); object? previous = PyErrors.EnterHandler(active);
        int arguments = 0;
        try
        {
            PyTraceback.SetLine(51);
            foreach (var pair in new[] { (emptyInstance, "__dict__"), (slotInstance, "_mapping") })
            {
                var error = Assert.Throws<PyAttributeError>(() => Invoke(pair.Item1, pair.Item2, () =>
                { arguments++; return []; }));
                Assert.Same(active, error.Context);
                Assert.Equal(new PyStackFrame("capture_slots.py", "caller", 51), error.Frames[0]);
                // 直接属性读取保留原始错误路径，不继承捕获入口的prepareMissing=true。
                var original = Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(pair.Item1, pair.Item2));
                Assert.Empty(original.Frames); Assert.Null(original.Context);
            }
            Assert.Equal(0, arguments);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
        // 认证native槽可保存一等函数；晚写回不改变已捕获的原函数。
        PyObject.SetAttr(slotInstance, "_mapping", Function("slot-old", (_, _, _, _) => 7L));
        object? target = PyOps.CaptureMethodTarget(slotInstance, "_mapping");
        PyObject.SetAttr(slotInstance, "_mapping", Function("slot-new", (_, _, _, _) => 9L));
        Assert.Equal(7L, PyPic.CallCapturedMethod0(4823, true, target, "_mapping"));
    }

    [Fact]
    public void DictC3ReceiverKeepsCapturedNativeMethodAndInstanceShadowing()
    {
        var mixin = (PyClass)PyOps.MakeClass("CaptureMixin", [], null); PyOps.FinalizeOrdinaryClass(mixin);
        var cls = (PyClass)PyOps.PrepareOrdinaryMiClass("CaptureDictC3", [mixin, PyType.Get("dict")]);
        PyOps.FinalizeOrdinaryClass(cls);
        var instance = Assert.IsType<PyDictInstance>(cls.Invoke([], [], []));
        PyOps.SetItem(instance, "key", 3L);
        Assert.True(PyOps.RequiresEarlyMethodCapture(instance));
        object? target = PyOps.CaptureMethodTarget(instance, "get");
        PyObject.SetAttr(instance, "get", Function("shadow", (_, _, _, _) => 99L));
        Assert.Equal(3L, PyPic.CallCapturedMethod1(4824, true, target, "get", "key"));
        target = PyOps.CaptureMethodTarget(instance, "get");
        PyObject.DelAttr(instance, "get");
        Assert.Equal(99L, PyPic.CallCapturedMethod1(4824, true, target, "get", "key"));
    }

    [Fact]
    public void ForeignBackedProxyCapturesExternalLookupOnceWithoutPreparingUserFailure()
    {
        var foreign = new ForeignCaptureObject();
        var instance = new PyInstance(new PyClass("CaptureForeignProxy"), foreign);
        foreign.Target = Function("foreign-old", (_, _, _, _) => 7L);
        object? target = PyOps.CaptureMethodTarget(instance, "f");
        Assert.True(PyOps.RequiresEarlyMethodCapture(instance)); Assert.Equal(1, foreign.Reads);
        foreign.Target = Function("foreign-new", (_, _, _, _) => 99L);
        Assert.Equal(7L, PyPic.CallCapturedMethod0(4825, true, target, "f"));
        Assert.Equal(1, foreign.Reads);
        var sentinel = new PyValueError("foreign-user-error"); foreign.Failure = sentinel;
        var active = new PyKeyError("active"); object? previous = PyErrors.EnterHandler(active);
        try
        {
            Assert.Same(sentinel, Assert.Throws<PyValueError>(() => PyOps.CaptureMethodTarget(instance, "f")));
            Assert.Null(sentinel.Context); Assert.Empty(sentinel.Frames);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    // 只覆盖Runtime中性外部对象转发，不冒充真实libpython/GIL/句柄验证。
    private sealed class ForeignCaptureObject : PyNativeObject, IPyForeignObject
    {
        public object? Target; public PyBaseException? Failure; public int Reads;
        public override string TypeName => "CaptureForeign";
        public object ForeignType => PyType.Get("object");
        public bool IsExceptionClass => false;
        public override object? GetAttr(string name)
        { Reads++; if (Failure is not null) throw Failure; return Target; }
        public object? Invoke(object?[] args, string[] names, object?[] values) => throw new PyTypeError("not callable");
        public object ReadScalar(string kind) => throw new PyTypeError("not scalar");
        public object? Item(object? key) => throw new PyTypeError("not indexed");
        public void WriteItem(object? key, object? value, bool deleting) => throw new PyTypeError("not indexed");
        public IPyIterator Iterate() => throw new PyTypeError("not iterable");
        public long Length() => throw new PyTypeError("not sized");
        public bool Truth() => true;
        public long Hash() => 1;
        public object Binary(int operation, object? other, bool reverse) => PyNotImplemented.Instance;
        public bool Compare(int operation, object? other, bool reverse) => false;
        public object RichCompare(int operation, object? other, bool reverse) => PyNotImplemented.Instance;
        public object Unary(int operation) => throw new PyTypeError("no unary");
        public bool IsInstanceOf(object type) => ReferenceEquals(type, ForeignType);
        public bool Accepts(object? value) => false;
        public bool MatchesException(PyBaseException error) => false;
        public PyStackFrame[] TracebackFrames() => [];
        public void SynchronizeException(PyBaseException error, bool write) { }
    }

    private sealed class Once(Action action) : IPyIterator
    {
        private bool consumed;
        public bool MoveNext()
        {
            if (consumed) return false;
            consumed = true; action(); return true;
        }
        public object Current() => 1L;
    }
}
