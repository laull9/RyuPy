// 影子诊断合同：验证元数据观测而非合法特殊对象行为；不启用缓存，不提供并发或ABA证书。
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("捕获布局版本审计")]
public sealed class PyCaptureLayoutShadowTests
{
    private static PyClass Root(string name)
    {
        var cls = (PyClass)PyOps.MakeClass(name, [], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ObservedMutationAndRestorationIncludeNonFirstMiParent(int topology)
    {
        var changed = Root("ShadowChanged");
        PyClass subject = changed;
        if (topology == 1)
        {
            subject = (PyClass)PyOps.MakeClass("ShadowChild", [changed], null);
            PyOps.FinalizeOrdinaryClass(subject);
        }
        if (topology == 2)
        {
            var first = Root("ShadowFirst");
            subject = (PyClass)PyOps.PrepareOrdinaryMiClass("ShadowMi", [first, changed]);
            PyOps.FinalizeOrdinaryClass(subject);
            Assert.Same(first, subject.BaseClass);
        }
        var session = new PyPic.CaptureLayoutShadow();
        var firstResult = session.Observe(subject);
        Assert.True(firstResult.Eligible);
        Assert.True(firstResult.Changed);
        Assert.Equal(1L, firstResult.ObservedSequence);
        Assert.Equal(topology + 1, firstResult.DependencyCount);
        try
        {
            changed.ExceptionFactory = static (_, _) => new PyValueError("unused");
            var second = session.Observe(subject);
            Assert.False(second.Eligible);
            Assert.True(second.Changed);
            Assert.Equal(2L, second.ObservedSequence);
        }
        finally { changed.ExceptionFactory = null; }
        var restored = session.Observe(subject);
        Assert.True(restored.Eligible);
        Assert.Equal(3L, restored.ObservedSequence);
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("native")]
    [InlineData("string")]
    [InlineData("list")]
    [InlineData("dict")]
    [InlineData("generic")]
    [InlineData("io")]
    [InlineData("builtin")]
    public void MetadataFlagsAreRereadWithoutVersionNotification(string field)
    {
        var cls = Root("ShadowFlag");
        var generic = new PyTypingGenericDefinition(null);
        Action<bool> set = field switch
        {
            "enum" => value => cls.HasEnumMetadata = value,
            "native" => value => cls.IsNativeRuntimeClass = value,
            "string" => value => cls.HasStringLayout = value,
            "list" => value => cls.HasListLayout = value,
            "dict" => value => cls.HasDictLayout = value,
            "generic" => value => cls.GenericDefinition = value ? generic : null,
            "io" => value => cls.TypingIoKind = value ? PyTypingIoKind.Derived : PyTypingIoKind.None,
            "builtin" => value => cls.BuiltinBase = value ? "Exception" : null,
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
        var session = new PyPic.CaptureLayoutShadow();
        Assert.True(session.Observe(cls).Eligible);
        long version = PyClass.GlobalVersion;
        try
        {
            set(true);
            Assert.False(session.Observe(cls).Eligible);
            Assert.Equal(version, PyClass.GlobalVersion);
        }
        finally { set(false); }
        Assert.True(session.Observe(cls).Eligible);
        Assert.Equal(version, PyClass.GlobalVersion);
    }

    [Fact]
    public void RepeatedObservationAndUnobservedAbaDoNotClaimChangeDetection()
    {
        var cls = Root("ShadowAba");
        var session = new PyPic.CaptureLayoutShadow();
        var first = session.Observe(cls);
        Assert.False(session.Observe(cls).Changed);
        try { cls.HasEnumMetadata = true; }
        finally { cls.HasEnumMetadata = false; }
        var afterAba = session.Observe(cls);
        Assert.False(afterAba.Changed);
        Assert.Equal(first.ObservedSequence, afterAba.ObservedSequence);
        // 没观察到中间态，明确不承诺检测A→B→A。
    }

    [Fact]
    public void BaseClassDependencyChangesAreObserved()
    {
        var subject = Root("ShadowReparent");
        var parent = Root("ShadowNewParent");
        parent.ExceptionFactory = static (_, _) => new PyValueError("unused");
        var session = new PyPic.CaptureLayoutShadow();
        Assert.True(session.Observe(subject).Eligible);
        try
        {
            subject.BaseClass = parent;
            var changed = session.Observe(subject);
            Assert.False(changed.Eligible);
            Assert.Equal(2, changed.DependencyCount);
        }
        finally { subject.BaseClass = null; parent.ExceptionFactory = null; }
        Assert.True(session.Observe(subject).Eligible);
    }

    [Fact]
    public void ForeignPresenceClosureAndArrayIdentityAreObservedWithoutProviderHooks()
    {
        var parent = Root("ShadowForeignMetadata");
        var subject = (PyClass)PyOps.MakeClass("ShadowForeignChild", [parent], null);
        PyOps.FinalizeOrdinaryClass(subject);
        var session = new PyPic.CaptureLayoutShadow();
        Assert.True(session.Observe(subject).Eligible);
        try
        {
            // 只测已有nullable字段的存在性，空数组不是合法外部类声明；不安装假provider。
            parent.ForeignMiBases = new object?[0];
            var present = session.Observe(subject);
            Assert.False(present.Eligible);
            parent.ForeignMiBases = new object?[0];
            var replacement = session.Observe(subject);
            Assert.False(replacement.Eligible);
            Assert.True(replacement.Changed);
            Assert.Equal(present.ObservedSequence + 1, replacement.ObservedSequence);
        }
        finally { parent.ForeignMiBases = null; }
        Assert.True(session.Observe(subject).Eligible);
    }

    [Fact]
    public void CyclesRejectWithoutAdvancingPreviousObservation()
    {
        var cls = Root("ShadowCycle");
        var session = new PyPic.CaptureLayoutShadow();
        var first = session.Observe(cls);
        try
        {
            cls.BaseClass = cls;
            Assert.Throws<InvalidOperationException>(() => session.Observe(cls));
            Assert.Equal(1L, session.OracleCallCount);
        }
        finally { cls.BaseClass = null; }
        var after = session.Observe(cls);
        Assert.False(after.Changed);
        Assert.Equal(first.ObservedSequence, after.ObservedSequence);
    }

    [Fact]
    public void PendingAndFailedMiAreRejectedBeforeOracle()
    {
        var parent = Root("ShadowPendingParent");
        var pending = (PyClass)PyOps.PrepareOrdinaryMiClass("ShadowPending", [parent, parent]);
        var session = new PyPic.CaptureLayoutShadow();
        Assert.Throws<InvalidOperationException>(() => session.Observe(pending));
        Assert.Equal(0L, session.OracleCallCount);
        Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(pending));
        Assert.Throws<InvalidOperationException>(() => session.Observe(pending));
        Assert.Equal(0L, session.OracleCallCount);
        Assert.Equal(1L, session.Observe(parent).ObservedSequence);
    }
    [Fact]
    public void DependencyBudgetRejectsWithoutPublishing()
    {
        var subject = Root("ShadowBudget0");
        var tail = subject;
        for (int i = 1; i < 257; i++)
        {
            var next = Root("ShadowBudget" + i);
            tail.BaseClass = next;
            tail = next;
        }
        var session = new PyPic.CaptureLayoutShadow();
        try
        {
            Assert.Throws<InvalidOperationException>(() => session.Observe(subject));
            Assert.Equal(0L, session.OracleCallCount);
        }
        finally { subject.BaseClass = null; }
        Assert.Equal(1L, session.Observe(subject).ObservedSequence);
    }

    [Fact]
    public void UnfinalizedMetadataIsRejected()
    {
        var pending = new PyClass("ShadowNotFinalized");
        var session = new PyPic.CaptureLayoutShadow();
        Assert.Throws<InvalidOperationException>(() => session.Observe(pending));
        Assert.Equal(0L, session.OracleCallCount);
    }

    [Fact]
    public void OtherThreadIsRejectedWithoutReadingMetadata()
    {
        var cls = Root("ShadowThreadOwner");
        var session = new PyPic.CaptureLayoutShadow();
        Exception? failure = null;
        // 不并发修改类，只验证调用线程边界；Join不代表observer具有同步能力。
        var thread = new System.Threading.Thread(() =>
        {
            try { session.Observe(cls); }
            catch (Exception error) { failure = error; }
        });
        thread.Start();
        thread.Join();
        Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal(0L, session.OracleCallCount);
        Assert.Equal(1L, session.Observe(cls).ObservedSequence);
    }

    [Fact]
    public void FinalizedEmptySlotsRemainIneligible()
    {
        var cls = (PyClass)PyOps.MakeClass("ShadowReadySlots", [], null);
        cls.SetAttr("__slots__", new PyTuple([]));
        PyOps.FinalizeOrdinaryClass(cls);
        var session = new PyPic.CaptureLayoutShadow();
        Assert.False(session.Observe(cls).Eligible);
        Assert.Equal(1L, session.OracleCallCount);
    }

    [Fact]
    public void MiNonFirstParentCycleRejectsBeforeOracle()
    {
        var first = Root("ShadowCycleFirst");
        var second = Root("ShadowCycleSecond");
        var subject = (PyClass)PyOps.PrepareOrdinaryMiClass("ShadowCycleMi", [first, second]);
        PyOps.FinalizeOrdinaryClass(subject);
        var session = new PyPic.CaptureLayoutShadow();
        Assert.True(session.Observe(subject).Eligible);
        try
        {
            second.BaseClass = second;
            Assert.Throws<InvalidOperationException>(() => session.Observe(subject));
            Assert.Equal(1L, session.OracleCallCount);
        }
        finally { second.BaseClass = null; }
        var restored = session.Observe(subject);
        Assert.False(restored.Changed);
        Assert.Equal(1L, restored.ObservedSequence);
    }

    [Fact]
    public void ForeignClosureIncludesNodesOutsideFrozenMiMro()
    {
        var first = Root("ShadowClosureFirst");
        var second = Root("ShadowClosureSecond");
        var extra = Root("ShadowClosureExtra");
        var subject = (PyClass)PyOps.PrepareOrdinaryMiClass("ShadowClosureMi", [first, second]);
        PyOps.FinalizeOrdinaryClass(subject);
        Assert.DoesNotContain(extra, subject.MroTuple().Items);
        var session = new PyPic.CaptureLayoutShadow();
        Assert.True(session.Observe(subject).Eligible);
        try
        {
            // 只是元数据反例，不宣称Python允许已冻结C3之后直接改父链。
            second.BaseClass = extra;
            extra.ForeignMiBases = new object?[0];
            var observed = session.Observe(subject);
            Assert.False(observed.Eligible);
            Assert.Equal(4, observed.DependencyCount);
        }
        finally { second.BaseClass = null; extra.ForeignMiBases = null; }
        Assert.True(session.Observe(subject).Eligible);
    }

}
