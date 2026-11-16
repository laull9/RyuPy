// 捕获资格失效覆盖审计：只调用真实旧谓词，不复制判据，不启用任何资格缓存。
// 内部setter可形成未终结/不完整布局，仅验证谓词与版本；不调用这些对象的Python行为。
// 这些反例不证明对应修改是合法Python操作，也不构成并发、发布或完整布局验收。
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

// GlobalVersion是进程共享状态：本集合与其它集合也不并行，避免外部Touch污染断言。
[CollectionDefinition("捕获布局版本审计", DisableParallelization = true)]
public sealed class PyCaptureLayoutMutationAuditCollection { }

[Collection("捕获布局版本审计")]
public sealed class PyCaptureLayoutMutationAuditTests
{
    private static readonly MethodInfo Oracle = typeof(PyPic).GetMethod("IsPlainCaptureClass",
        BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("缺少真实捕获资格谓词");

    private static bool Plain(PyClass cls) => Assert.IsType<bool>(Oracle.Invoke(null, [cls]));

    public static IEnumerable<object[]> MutableCases()
    {
        string[] fields = ["ExceptionFactory", "HasEnumMetadata", "IsNativeRuntimeClass",
            "HasStringLayout", "HasListLayout", "HasDictLayout", "TypingIoKind",
            "GenericDefinition", "BuiltinBase"];
        foreach (string field in fields)
            for (int topology = 0; topology < 3; topology++)
                yield return [field, topology];
    }

    private static PyClass Root(string name)
    {
        var cls = (PyClass)PyOps.MakeClass(name, [], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }

    // 0=自身；1=单继承祖先；2=真实C3的非首父，避免只查BaseClass链的假阳性。
    private static (PyClass Subject, PyClass Changed) Family(int topology)
    {
        PyClass changed = Root("AuditChanged");
        if (topology == 0) return (changed, changed);
        if (topology == 1)
        {
            var child = (PyClass)PyOps.MakeClass("AuditChild", [changed], null);
            PyOps.FinalizeOrdinaryClass(child);
            return (child, changed);
        }
        var first = Root("AuditFirst");
        var multiple = (PyClass)PyOps.PrepareOrdinaryMiClass("AuditMultiple", [first, changed]);
        PyOps.FinalizeOrdinaryClass(multiple);
        Assert.Same(first, multiple.BaseClass);
        Assert.NotSame(changed, multiple.BaseClass);
        Assert.Contains(changed, multiple.MroTuple().Items);
        return (multiple, changed);
    }

    // 使用已有真实setter及真实载体，反射仅用于上面的private谓词。
    private static Action<bool> Mutation(PyClass cls, string field)
    {
        var generic = new PyTypingGenericDefinition(null);
        PyExceptionFactory factory = static (_, _) => new PyValueError("unused");
        return field switch
        {
            "ExceptionFactory" => set => cls.ExceptionFactory = set ? factory : null,
            "HasEnumMetadata" => set => cls.HasEnumMetadata = set,
            "IsNativeRuntimeClass" => set => cls.IsNativeRuntimeClass = set,
            "HasStringLayout" => set => cls.HasStringLayout = set,
            "HasListLayout" => set => cls.HasListLayout = set,
            "HasDictLayout" => set => cls.HasDictLayout = set,
            "TypingIoKind" => set => cls.TypingIoKind = set ? PyTypingIoKind.Derived : PyTypingIoKind.None,
            "GenericDefinition" => set => cls.GenericDefinition = set ? generic : null,
            "BuiltinBase" => set => cls.BuiltinBase = set ? "Exception" : null,
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
    }

    [Theory]
    [MemberData(nameof(MutableCases))]
    public void ExistingSettersChangeWarmEligibilityWithoutVersion(string field, int topology)
    {
        var (subject, changed) = Family(topology);
        Action<bool> mutate = Mutation(changed, field);
        Assert.True(Plain(subject));
        Assert.True(Plain(subject)); // 暖判据，不假装存在尚未实现的资格缓存。
        long global = PyClass.GlobalVersion;
        long own = changed.Version;
        try
        {
            mutate(true);
            Assert.False(Plain(subject));
            Assert.Equal(global, PyClass.GlobalVersion);
            Assert.Equal(own, changed.Version);
        }
        finally { mutate(false); }
        Assert.True(Plain(subject));
        Assert.Equal(global, PyClass.GlobalVersion);
        Assert.Equal(own, changed.Version);
    }

    [Fact]
    public void ExistingBaseSetterChangesSingleChainWithoutVersion()
    {
        var ordinary = Root("AuditOrdinaryParent");
        var special = Root("AuditSpecialParent");
        special.ExceptionFactory = static (_, _) => new PyValueError("unused");
        var subject = (PyClass)PyOps.MakeClass("AuditReparent", [ordinary], null);
        PyOps.FinalizeOrdinaryClass(subject);
        Assert.True(Plain(subject));
        long version = PyClass.GlobalVersion;
        try
        {
            // 内部直接改父链只是审计反例，不声称支持Python __bases__重绑。
            subject.BaseClass = special;
            Assert.False(Plain(subject));
            Assert.Equal(version, PyClass.GlobalVersion);
        }
        finally { subject.BaseClass = ordinary; }
        Assert.True(Plain(subject));
        Assert.Equal(version, PyClass.GlobalVersion);
    }

    [Fact]
    public void EmptySlotsFinalizationChangesEligibilityWithoutFinalVersionBump()
    {
        var subject = (PyClass)PyOps.MakeClass("AuditSlots", [], null);
        subject.SetAttr("__slots__", new PyTuple([]));
        // SetAttr已失效，但尚未提交HasSlotLayout；从这时起记录版本。
        Assert.True(Plain(subject));
        long version = PyClass.GlobalVersion;
        PyOps.FinalizeOrdinaryClass(subject);
        Assert.True(subject.HasSlotLayout);
        Assert.False(Plain(subject));
        Assert.Equal(version, PyClass.GlobalVersion);
        // private布局不强制清除：真实提交不可逆，不能伪造恢复路径。
    }

    [Fact]
    public void NamedTupleCommitHasExplicitVersionInvalidation()
    {
        var subject = new PyClass("AuditTuple");
        var prepared = PyNamedTupleLayout.PrepareClass(subject, [], [], PyNone.Instance);
        Assert.True(Plain(subject));
        long version = PyClass.GlobalVersion;
        subject.CommitNamedTuple(prepared);
        Assert.NotNull(subject.NamedTupleLayout);
        Assert.False(Plain(subject));
        Assert.NotEqual(version, PyClass.GlobalVersion);
    }

    [Fact]
    public void DictC3FinalizationHasVersionedCommit()
    {
        var mixin = Root("AuditMixin");
        var subject = (PyClass)PyOps.PrepareOrdinaryMiClass("AuditDictC3", [mixin, PyType.Get("dict")]);
        Assert.True(Plain(subject));
        long version = PyClass.GlobalVersion;
        PyOps.FinalizeOrdinaryClass(subject);
        Assert.True(subject.HasDictC3Layout);
        Assert.True(subject.HasDictLayout);
        Assert.False(Plain(subject));
        Assert.NotEqual(version, PyClass.GlobalVersion);
    }

    [Fact]
    public void GenericRootInitIsNotARuntimeVersionedMutation()
    {
        long version = PyClass.GlobalVersion;
        var ordinary = new PyClass("AuditNonGeneric");
        var generic = new PyClass("AuditGeneric") { IsTypingGenericRoot = true };
        Assert.True(Plain(ordinary));
        Assert.False(Plain(generic));
        Assert.Equal(version, PyClass.GlobalVersion);
        // init-only不是同一对象的可逆setter，不伪造运行期切换。
    }

    // 未强造ForeignBase/ForeignMiBases：它们依赖真实外部类型/构造协议，此文件不安装假provider。
    // ForeignClass本身不属于HasForeignHierarchy公式；这里不拿改ForeignClass冒充外部祖先覆盖。
}
