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
}
