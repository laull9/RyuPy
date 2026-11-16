// 尚未接线的调用前计划合同：快照、重入、描述符回退、独立参数及实例生命周期。
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCapturedMethodPlanTests
{
    private static PyFunction Function(PyCall body, bool usesSuper = false) =>
        new("f", PySignature.Create("f", [], [], []), body, [], usesSuper);
    private static bool PicEnabled => Environment.GetEnvironmentVariable("RYUPY_PIC") != "0";

    [Fact]
    public void OnePlanCanServeDifferentInstancesWithoutBindingThem()
    {
        var cls = new PyClass("SharedPlan"); var first = new PyInstance(cls); var second = new PyInstance(cls);
        cls.SetAttr("f", Function(static (_, args, _, _) => args[0]));
        object? a = PyPic.CaptureMethodPlan(3500, first, "f");
        object? b = PyPic.CaptureMethodPlan(3500, second, "f");
        if (PicEnabled) Assert.Same(a, b);
        Assert.Same(first, PyPic.CallCapturedPlan0(3500, true, a, first, "f"));
        Assert.Same(second, PyPic.CallCapturedPlan0(3500, true, b, second, "f"));
    }

    [Fact]
    public void ClassAndInstanceRebindingCannotReplaceExistingSnapshot()
    {
        var cls = new PyClass("SnapshotPlan"); var self = new PyInstance(cls);
        cls.SetAttr("f", Function(static (_, _, _, _) => 9L));
        object? old = PyPic.CaptureMethodPlan(3501, self, "f");
        cls.SetAttr("f", Function(static (_, _, _, _) => 101L));
        object? changed = PyPic.CaptureMethodPlan(3501, self, "f");
        PyObject.SetAttr(self, "f", Function(static (_, _, _, _) => 202L));
        object? shadowed = PyPic.CaptureMethodPlan(3501, self, "f");
        Assert.Equal(9L, PyPic.CallCapturedPlan1(3501, true, old, self, "f", 1L));
        Assert.Equal(101L, PyPic.CallCapturedPlan1(3501, true, changed, self, "f", 1L));
        Assert.Equal(202L, PyPic.CallCapturedPlan1(3501, true, shadowed, self, "f", 1L));
    }

    [Fact]
    public void SameSiteReplacementDuringArgumentEvaluationPreservesOuterPlan()
    {
        var cls = new PyClass("OuterPlan"); var self = new PyInstance(cls);
        cls.SetAttr("f", Function(static (_, args, _, _) => args[1]));
        object? outer = PyPic.CaptureMethodPlan(3502, self, "f");
        // 同一站点轮转超过四种类；替换只能换缓存引用，不能修改outer对象。
        for (int i = 0; i < 8; i++)
        {
            var nestedClass = new PyClass("NestedPlan" + i); var nested = new PyInstance(nestedClass);
            nestedClass.SetAttr("f", Function(static (_, _, _, _) => -1L));
            object? inner = PyPic.CaptureMethodPlan(3502, nested, "f");
            Assert.Equal(-1L, PyPic.CallCapturedPlan0(3502, true, inner, nested, "f"));
        }
        Assert.Equal(73L, PyPic.CallCapturedPlan1(3502, true, outer, self, "f", 73L));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DescriptorOrGetattrReturningBoundMethodRunsOncePerCapture(bool getattr)
    {
        var cls = new PyClass("DynamicBound"); var self = new PyInstance(cls); int reads = 0;
        var function = Function(static (_, args, _, _) => args[0]);
        var getter = Function((_, _, _, _) => { reads++; return new PyBoundMethod(cls, self, function); });
        cls.SetAttr(getattr ? "__getattr__" : "f", getattr ? getter : new PyProperty(getter));
        for (int i = 0; i < 3; i++)
        {
            object? captured = PyPic.CaptureMethodPlan(3503, self, "f");
            Assert.IsType<PyBoundMethod>(captured);
            Assert.Equal(i + 1, reads);
            Assert.Same(self, PyPic.CallCapturedPlan0(3503, true, captured, self, "f"));
            Assert.Equal(i + 1, reads);
        }
    }

    [Fact]
    public void NewlyAllocatedShadowSlotInvalidatesNegativeSlotEntry()
    {
        var cls = new PyClass("SlotPlan"); var first = new PyInstance(cls); var other = new PyInstance(cls);
        cls.SetAttr("f", Function(static (_, _, _, _) => 1L));
        _ = PyPic.CaptureMethodPlan(3504, first, "f");
        // 给同类另一实例首次创建f槽，然后分别检查未遮蔽与遮蔽的实例。
        PyObject.SetAttr(other, "f", Function(static (_, _, _, _) => 2L));
        object? plain = PyPic.CaptureMethodPlan(3504, first, "f");
        object? shadow = PyPic.CaptureMethodPlan(3504, other, "f");
        Assert.Equal(1L, PyPic.CallCapturedPlan0(3504, true, plain, first, "f"));
        Assert.Equal(2L, PyPic.CallCapturedPlan0(3504, true, shadow, other, "f"));
    }

    [Fact]
    public void ParametersRemainIndependentAcrossAritiesAndReentry()
    {
        var cls = new PyClass("ArrayPlan"); var self = new PyInstance(cls);
        var saved = new List<object?[]>(); object? plan = null; bool nested = false;
        cls.SetAttr("f", Function((_, args, _, _) =>
        {
            saved.Add(args); var before = args.ToArray();
            if (!nested)
            {
                nested = true;
                Assert.Equal(2, PyPic.CallCapturedPlan1(3505, true, plan, self, "f", 88L));
                nested = false;
            }
            Assert.Equal(before, args); return args.Length;
        }));
        plan = PyPic.CaptureMethodPlan(3505, self, "f");
        Assert.Equal(1, PyPic.CallCapturedPlan0(3505, true, plan, self, "f"));
        Assert.Equal(2, PyPic.CallCapturedPlan1(3505, true, plan, self, "f", 1L));
        Assert.Equal(3, PyPic.CallCapturedPlan2(3505, true, plan, self, "f", 2L, 3L));
        Assert.Equal(4, PyPic.CallCapturedPlan3(3505, true, plan, self, "f", 4L, 5L, 6L));
        Assert.Equal(new object?[] { self, 4L, 5L, 6L }, saved[6]);
        for (int i = 0; i < saved.Count; i++)
            for (int j = i + 1; j < saved.Count; j++) Assert.NotSame(saved[i], saved[j]);
    }

    [Fact]
    public void StarArgumentsUseOriginalFlatteningAtEveryArity()
    {
        var cls = new PyClass("StarPlan"); var self = new PyInstance(cls);
        cls.SetAttr("f", Function(static (_, args, _, _) => args));
        object? plan = PyPic.CaptureMethodPlan(3506, self, "f");
        object star = PyOps.StarArg(new PyTuple([7L, 8L]));
        Assert.Equal(new object?[] { self, 7L, 8L }, Assert.IsType<object?[]>(PyPic.CallCapturedPlan1(3506, true, plan, self, "f", star)));
        Assert.Equal(new object?[] { self, 1L, 7L, 8L }, Assert.IsType<object?[]>(PyPic.CallCapturedPlan2(3506, true, plan, self, "f", 1L, star)));
        Assert.Equal(new object?[] { self, 1L, 7L, 8L, 2L }, Assert.IsType<object?[]>(PyPic.CallCapturedPlan3(3506, true, plan, self, "f", 1L, star, 2L)));
    }

    [Fact]
    public void SuperContextRestoresWhenPlanBodyThrows()
    {
        var cls = new PyClass("SuperPlan"); var self = new PyInstance(cls); var error = new PyValueError("body");
        cls.SetAttr("f", Function((_, _, _, _) =>
        {
            Assert.Same(cls, PyMethodContext.CurrentOwner); Assert.Same(self, PyMethodContext.CurrentSelf);
            throw error;
        }, true));
        object? plan = PyPic.CaptureMethodPlan(3507, self, "f");
        var owner = PyMethodContext.CurrentOwner; var receiver = PyMethodContext.CurrentSelf;
        Assert.Same(error, Assert.Throws<PyValueError>(() => PyPic.CallCapturedPlan0(3507, true, plan, self, "f")));
        Assert.Same(owner, PyMethodContext.CurrentOwner); Assert.Same(receiver, PyMethodContext.CurrentSelf);
    }

    [Fact]
    public void NonPlanAndDisabledSitesKeepOldCapturedProtocol()
    {
        var cls = new PyClass("FallbackPlan"); var self = new PyInstance(cls);
        cls.SetAttr("f", new PyClassMethod(Function(static (_, args, _, _) => args[0])));
        object? value = PyPic.CaptureMethodPlan(3508, self, "f");
        Assert.IsType<PyBoundMethod>(value);
        Assert.Same(cls, PyPic.CallCapturedPlan0(3508, true, value, self, "f"));
        cls.SetAttr("f", Function(static (_, args, _, _) => args[0]));
        foreach (int site in new[] { -1, 4096 })
        {
            value = PyPic.CaptureMethodPlan(site, self, "f");
            Assert.IsType<PyBoundMethod>(value);
            Assert.Same(self, PyPic.CallCapturedPlan0(site, true, value, self, "f"));
        }
        var list = new PyList();
        value = PyPic.CaptureMethodPlan(3508, list, "append");
        Assert.Same(list, value);
        PyPic.CallCapturedPlan1(3508, false, value, list, "append", 42L);
        Assert.Equal(42L, list.GetItem(0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CacheReceiver()
    {
        var cls = new PyClass("WeakReceiverPlan"); var self = new PyInstance(cls);
        cls.SetAttr("f", Function(static (_, args, _, _) => args.Length));
        object? plan = PyPic.CaptureMethodPlan(3509, self, "f");
        Assert.Equal(1, PyPic.CallCapturedPlan0(3509, true, plan, self, "f"));
        return new WeakReference(self);
    }

    [Fact]
    public void CacheDoesNotKeepReceiverAlive()
    {
        WeakReference receiver = CacheReceiver();
        for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.False(receiver.IsAlive);
        // 仅证明本例receiver可回收；类、owner和function仍由有界缓存强持有。
    }

    private static PySignature ScalarSignature(int count) => PySignature.Create("f",
        Enumerable.Range(0, count).Select(i => "p" + i).ToArray(),
        Enumerable.Repeat("pos", count).ToArray(), Enumerable.Repeat("0", count).ToArray());

    [Fact]
    public void ScalarAllAritiesKeepFunctionAndArgumentsWithoutArrayEntry()
    {
        for (int arity = 1; arity <= 4; arity++)
        {
            int arrays = 0; var cls = new PyClass("Scalar" + arity); var self = new PyInstance(cls);
            object?[] expected = [self, 1L, 2L, 3L];
            var fn = new PyFunction("f", ScalarSignature(arity), (f, args, _, _) =>
            { arrays++; return args[arity - 1]; }, [], false);
            PyPic.RegisterExperimentalScalar(fn, arity, (ctx, f, p0, p1, p2, p3) =>
            {
                Assert.Null(ctx); Assert.Same(fn, f); Assert.Same(self, p0);
                if (arity > 1) Assert.Equal(1L, p1);
                if (arity > 2) Assert.Equal(2L, p2);
                if (arity > 3) Assert.Equal(3L, p3);
                return arity switch { 1 => p0, 2 => p1, 3 => p2, _ => p3 };
            });
            cls.SetAttr("f", fn); object? plan = PyPic.CaptureMethodPlan(3520 + arity, self, "f");
            object? result = arity switch
            {
                1 => PyPic.CallCapturedPlan0(3520 + arity, true, plan, self, "f"),
                2 => PyPic.CallCapturedPlan1(3520 + arity, true, plan, self, "f", 1L),
                3 => PyPic.CallCapturedPlan2(3520 + arity, true, plan, self, "f", 1L, 2L),
                _ => PyPic.CallCapturedPlan3(3520 + arity, true, plan, self, "f", 1L, 2L, 3L),
            };
            Assert.Equal(expected[arity - 1], result); Assert.Equal(PicEnabled ? 0 : 1, arrays);
        }
    }

    [Fact]
    public void ScalarWrongArityAndStarKeepOriginalArrayProtocol()
    {
        var cls = new PyClass("ScalarFallback"); var self = new PyInstance(cls); int scalar = 0;
        var retained = new List<object?[]>();
        var fn = new PyFunction("f", ScalarSignature(2), (_, args, _, _) =>
        { retained.Add(args); return args; }, [], false);
        // 此测试只验证回退，不触发委托；不以不同Body语义作为产品契约。
        PyPic.RegisterExperimentalScalar(fn, 2, (_, _, _, _, _, _) => { scalar++; return null; });
        cls.SetAttr("f", fn); object? plan = PyPic.CaptureMethodPlan(3525, self, "f");
        PyPic.CallCapturedPlan0(3525, true, plan, self, "f");
        PyPic.CallCapturedPlan2(3525, true, plan, self, "f", 1L, 2L);
        PyPic.CallCapturedPlan1(3525, true, plan, self, "f", PyOps.StarArg(new PyTuple([8L])));
        Assert.Equal(0, scalar); Assert.Equal(3, retained.Count);
        Assert.Equal(new object?[] { self, 8L }, retained[2]); Assert.NotSame(retained[0], retained[1]);
    }

    [Fact]
    public void ScalarSnapshotSurvivesRebindingAndSameSiteReentry()
    {
        var cls = new PyClass("ScalarReentry"); var self = new PyInstance(cls); object? outer = null;
        bool nested = false; PyFunction? fn = null;
        object? Body(object? argument)
        {
            if (!nested)
            {
                nested = true; cls.SetAttr("f", Function(static (_, _, _, _) => -1L));
                object? inner = PyPic.CaptureMethodPlan(3526, self, "f");
                Assert.Equal(-1L, PyPic.CallCapturedPlan1(3526, true, inner, self, "f", 99L));
                Assert.Equal(7L, PyPic.CallCapturedPlan1(3526, true, outer, self, "f", 7L));
                nested = false;
            }
            return argument;
        }
        fn = new PyFunction("f", ScalarSignature(2), (_, args, _, _) => Body(args[1]), [], false);
        PyPic.RegisterExperimentalScalar(fn, 2, (_, f, p0, p1, _, _) =>
        { Assert.Same(fn, f); Assert.Same(self, p0); return Body(p1); });
        cls.SetAttr("f", fn); outer = PyPic.CaptureMethodPlan(3526, self, "f");
        Assert.Equal(5L, PyPic.CallCapturedPlan1(3526, true, outer, self, "f", 5L));
    }

    [Fact]
    public void ScalarSuperContextRestoresOnThrow()
    {
        var cls = new PyClass("ScalarSuper"); var self = new PyInstance(cls); var error = new PyValueError("scalar");
        object? Throw()
        {
            Assert.Same(cls, PyMethodContext.CurrentOwner); Assert.Same(self, PyMethodContext.CurrentSelf); throw error;
        }
        var fn = new PyFunction("f", ScalarSignature(1), (_, _, _, _) => Throw(), [], true);
        PyPic.RegisterExperimentalScalar(fn, 1, (_, _, _, _, _, _) => Throw());
        cls.SetAttr("f", fn); object? plan = PyPic.CaptureMethodPlan(3527, self, "f");
        var oldOwner = PyMethodContext.CurrentOwner; var oldSelf = PyMethodContext.CurrentSelf;
        Assert.Same(error, Assert.Throws<PyValueError>(() => PyPic.CallCapturedPlan0(3527, true, plan, self, "f")));
        Assert.Same(oldOwner, PyMethodContext.CurrentOwner); Assert.Same(oldSelf, PyMethodContext.CurrentSelf);
    }

    [Fact]
    public void ScalarOwnedFunctionPreservesOriginalMetadataContextAndCells()
    {
        var state = new PyModuleInstanceState(null); var builtins = new PyDict(); builtins.Set("answer", 9L);
        var context = new PyModuleExecutionContext(state, builtins);
        object?[] cells = [new object()]; object?[] defaults = [null];
        object source = PyTraceback.CreateCodeSource("scalar_owner", "scalar_owner.py", null);
        var signature = ScalarSignature(1);
        PyOps.FunctionDefinitionWithSource(signature, "f", 1, source);
        object? Read(object ctx, PyFunction f)
        {
            Assert.Same(cells, f.Env);
            long depth = PyTraceback.PushFrameWithOrigin("scalar_owner.py", "f", f);
            try
            {
                var caller = PyTraceback.CallerSource(1);
                Assert.Same(f, caller.Origin); Assert.Same(state.Globals, caller.Globals); Assert.Same(source, caller.Code);
                return PyModuleInstances.LoadName(ctx, "answer");
            }
            finally { PyTraceback.PopTo(depth); }
        }
        var fn = PyModuleInstances.CreateOwnedFunction(context, "f", signature,
            (ctx, f, _, _, _) => Read(ctx, f), defaults, false, cells);
        object? target = fn.Body.Target;
        PyPic.RegisterExperimentalScalar(fn, 1, (ctx, f, _, _, _, _) =>
        { Assert.Same(context, ctx); return Read(ctx!, f); });
        Assert.Same(target, fn.Body.Target); Assert.Same(defaults, fn.Defaults);
        Assert.Same(state.Globals, PyObject.GetAttr(fn, "__globals__")); Assert.Same(builtins, PyObject.GetAttr(fn, "__builtins__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(fn, "__closure__"));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(fn, "__globals__", new PyDict()));
        var cls = new PyClass("ScalarOwned"); var self = new PyInstance(cls); cls.SetAttr("f", fn);
        object? plan = PyPic.CaptureMethodPlan(3528, self, "f");
        Assert.Equal(9L, PyPic.CallCapturedPlan0(3528, true, plan, self, "f"));
        state.Globals.Set("__builtins__", new PyDict()); builtins.Set("answer", 10L);
        PyObject.SetAttr(fn, "__module__", "renamed");
        Assert.Equal(10L, PyPic.CallCapturedPlan0(3528, true, plan, self, "f"));
        state.Globals.Set("answer", 11L); Assert.Equal(11L, PyPic.CallCapturedPlan0(3528, true, plan, self, "f"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterScalarCycle(bool narrow)
    {
        PyFunction? fn = null;
        fn = new PyFunction("f", ScalarSignature(1), static (_, args, _, _) => args[0], [], false);
        // 值的闭包反向引用弱键；若误用普通Dictionary，此环会被静态根保留。
        if (narrow)
        {
            fn = new PyFunction("f", ScalarSignature(2), static (_, args, _, _) => args[1], [], false);
            PyPic.RegisterExperimentalScalar2(fn, (_, f, _, p1) => ReferenceEquals(fn, f) ? p1 : null);
        }
        else PyPic.RegisterExperimentalScalar(fn, 1, (_, f, p0, _, _, _) => ReferenceEquals(fn, f) ? p0 : null);
        return new WeakReference(fn);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScalarCapabilityWeakKeyDoesNotRootFunctionCycle(bool narrow)
    {
        WeakReference fn = RegisterScalarCycle(narrow);
        for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.False(fn.IsAlive);
    }

    [Fact]
    public void ScalarRegistrationRejectsDuplicateOrWrongSignature()
    {
        var fn = new PyFunction("f", ScalarSignature(1), static (_, args, _, _) => args[0], [], false);
        Assert.Throws<PyRuntimeError>(() => PyPic.RegisterExperimentalScalar(fn, 2, static (_, _, p0, _, _, _) => p0));
        PyPic.RegisterExperimentalScalar(fn, 1, static (_, _, p0, _, _, _) => p0);
        Assert.Throws<PyRuntimeError>(() => PyPic.RegisterExperimentalScalar(fn, 1, static (_, _, p0, _, _, _) => p0));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExperimentalDispatchPreservesOwnedReentrySuperAndFallback(bool narrow, bool usesSuper)
    {
        var state = new PyModuleInstanceState(null); var builtins = new PyDict(); builtins.Set("answer", 9L);
        var context = new PyModuleExecutionContext(state, builtins);
        var cls = new PyClass("DispatchContract"); var self = new PyInstance(cls);
        var error = new PyValueError("dispatch"); int arrays = 0; bool nested = false; object? plan = null;
        PyFunction? function = null;
        object? Invoke(object? argument) => narrow
            ? PyPic.CallExperimentalScalar2(3530, true, plan, self, "f", argument)
            : PyPic.CallExperimentalScalarNoSuper(3530, true, plan, self, "f", argument);
        object? Core(object ctx, PyFunction fn, object? receiver, object? arg)
        {
            Assert.Same(context, ctx); Assert.Same(function, fn); Assert.Same(self, receiver);
            Assert.Equal(9L, PyModuleInstances.LoadName(ctx, "answer"));
            if (usesSuper) { Assert.Same(cls, PyMethodContext.CurrentOwner); Assert.Same(self, PyMethodContext.CurrentSelf); }
            if (arg is string) throw error;
            if (!nested)
            {
                nested = true;
                cls.SetAttr("f", Function(static (_, _, _, _) => -1L));
                _ = PyPic.CaptureMethodPlan(3530, self, "f");
                Assert.Equal(7L, Invoke(7L)); nested = false;
            }
            return arg;
        }
        function = PyModuleInstances.CreateOwnedFunction(context, "f", ScalarSignature(2),
            (ctx, fn, args, _, _) => { arrays++; return Core(ctx, fn, args[0], args[1]); }, [], usesSuper, null);
        object? bodyTarget = function.Body.Target;
        if (narrow) PyPic.RegisterExperimentalScalar2(function, (ctx, fn, p0, p1) => Core(ctx!, fn, p0, p1));
        else PyPic.RegisterExperimentalScalar(function, 2, (ctx, fn, p0, p1, _, _) => Core(ctx!, fn, p0, p1));
        cls.SetAttr("f", function); plan = PyPic.CaptureMethodPlan(3530, self, "f");
        var oldOwner = PyMethodContext.CurrentOwner; var oldSelf = PyMethodContext.CurrentSelf;
        Assert.Equal(5L, Invoke(5L)); Assert.Equal(PicEnabled ? 0 : 2, arrays);
        Assert.Same(error, Assert.Throws<PyValueError>(() => Invoke("throw")));
        Assert.Same(oldOwner, PyMethodContext.CurrentOwner); Assert.Same(oldSelf, PyMethodContext.CurrentSelf);
        int before = arrays;
        Assert.Equal(8L, Invoke(PyOps.StarArg(new PyTuple([8L]))));
        Assert.True(arrays > before); Assert.Same(bodyTarget, function.Body.Target);
        Assert.Same(state.Globals, PyObject.GetAttr(function, "__globals__"));
        Assert.Same(builtins, PyObject.GetAttr(function, "__builtins__"));
        var list = new PyList();
        if (narrow) PyPic.CallExperimentalScalar2(3530, false, list, list, "append", 42L);
        else PyPic.CallExperimentalScalarNoSuper(3530, false, list, list, "append", 42L);
        Assert.Equal(42L, list.GetItem(0));
    }
}
