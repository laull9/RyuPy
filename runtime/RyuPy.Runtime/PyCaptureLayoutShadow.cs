// 串行影子诊断：每次完整重读元数据并对照原谓词，不为任何调用提供缓存资格证书。
// 多读及线程归属检查不是同步协议，不能证明并发稳定、观察间无修改或检测ABA。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public static partial class PyPic
{
    internal readonly record struct CaptureLayoutObservation(
        bool Eligible, bool Changed, long ObservedSequence, int DependencyCount);

    // 调用者必须保证整个相关类图无并发/重入修改。此类型不接入任何capture入口。
    internal sealed class CaptureLayoutShadow
    {
        private readonly int ownerThread = Environment.CurrentManagedThreadId;
        private ShadowSnapshot? previous;
        private long sequence;
        private bool active;
        // 仅记录尝试执行原谓词次数；不属于快照发布状态或资格证明。
        internal long OracleCallCount { get; private set; }

        internal CaptureLayoutObservation Observe(PyClass cls)
        {
            if (Environment.CurrentManagedThreadId != ownerThread || active)
                throw new InvalidOperationException("影子诊断只允许所属线程串行观察");
            active = true;
            try
            {
                ShadowSnapshot first = ReadShadow(cls);
                ShadowSnapshot second = ReadShadow(cls);
                if (!SameShadow(first, second)) throw new InvalidOperationException("影子元数据读取不稳定");
                bool eligible = true;
                foreach (ShadowNode node in second.Mro)
                    eligible &= node.Plain;
                // 始终执行真实原判据；没有用影子结果跳过MRO扫描。
                OracleCallCount++;
                bool actual = IsPlainCaptureClass(cls);
                if (eligible != actual) throw new InvalidOperationException("影子与原资格谓词不一致");
                ShadowSnapshot third = ReadShadow(cls);
                if (!SameShadow(second, third)) throw new InvalidOperationException("原谓词执行期间元数据改变");
                bool changed = previous is null || !SameShadow(previous, third);
                long next = changed ? checked(sequence + 1) : sequence;
                // 所有拒绝发生在发布之前；序号只是成功观测到的快照变化计数。
                var result = new CaptureLayoutObservation(eligible, changed, next, third.Dependencies.Count);
                previous = third;
                sequence = next;
                return result;
            }
            finally { active = false; }
        }
    }

    private sealed record ShadowSnapshot(PyClass Subject, List<ShadowNode> Mro, List<ShadowNode> Dependencies);

    private sealed class ShadowNode
    {
        internal required PyClass Class;
        internal required object?[] References;
        internal required object?[]? ForeignElements;
        internal required bool[] Flags;
        internal required int IoKind;
        internal bool Plain
        {
            get
            {
                foreach (bool flag in Flags) if (flag) return false;
                return References[1] is null && References[2] is null && References[3] is null;
            }
        }
    }

    private static ShadowSnapshot ReadShadow(PyClass cls)
    {
        var dependencies = new List<PyClass>();
        var known = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        CheckShadowChain(cls, dependencies, known);
        PyClass.CaptureShadowStructure structure = cls.ReadCaptureShadowStructure();
        if (structure.Mro is { } preparedMro)
            foreach (PyClass node in preparedMro)
            {
                if (node is null) throw new InvalidOperationException("未知MRO空节点");
                CheckShadowChain(node, dependencies, known);
            }
        var mroClasses = new List<PyClass>();
        var mroSeen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        // 单链已验无环；数组型MRO另拒绝重复节点。数量上限只是诊断拒绝边界。
        foreach (PyClass node in cls.MroClasses())
        {
            if (mroClasses.Count >= 256 || !mroSeen.Add(node))
                throw new InvalidOperationException("影子MRO重复或超出诊断边界");
            CheckShadowChain(node, dependencies, known);
            mroClasses.Add(node);
        }
        if (mroClasses.Count == 0 || !ReferenceEquals(mroClasses[0], cls))
            throw new InvalidOperationException("未知MRO结构");
        // 全部BaseClass闭包验证完成后才允许读取递归HasForeignHierarchy。
        var nodes = new Dictionary<PyClass, ShadowNode>(ReferenceEqualityComparer.Instance);
        var values = new List<ShadowNode>();
        foreach (PyClass node in dependencies)
        {
            var value = ReadShadowNode(node);
            nodes.Add(node, value);
            values.Add(value);
        }
        var mro = new List<ShadowNode>();
        foreach (PyClass node in mroClasses) mro.Add(nodes[node]);
        return new ShadowSnapshot(cls, mro, values);
    }

    private static void CheckShadowChain(PyClass start, List<PyClass> dependencies, HashSet<PyClass> known)
    {
        var path = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? node = start; node is not null; node = node.BaseClass)
        {
            if (!path.Add(node)) throw new InvalidOperationException("影子BaseClass闭包有环");
            if (!node.ReadCaptureShadowStructure().Ready)
                throw new InvalidOperationException("未完成、失败或终结中的类不在诊断范围");
            if (known.Add(node))
            {
                if (dependencies.Count >= 256) throw new InvalidOperationException("影子闭包超出诊断边界");
                dependencies.Add(node);
            }
        }
    }

    private static ShadowNode ReadShadowNode(PyClass cls) => new()
    {
        Class = cls,
        // 外来对象、委托、数组均只保存身份，不调用Equals/hash或任何外来协议。
        References = [cls.BaseClass, cls.NamedTupleLayout, cls.BuiltinBase, cls.ExceptionFactory,
            cls.ForeignBase, cls.ForeignMiBases, cls.GenericDefinition, cls.ReadCaptureShadowStructure().MroIdentity,
            cls.ReadCaptureShadowStructure().DictIdentity],
        ForeignElements = cls.ForeignMiBases is { } bases ? (object?[])bases.Clone() : null,
        Flags = [cls.HasSlotLayout, cls.HasDictC3Layout, cls.HasStringLayout, cls.HasListLayout,
            cls.HasDictLayout, cls.IsTypingGenericFamily, cls.IsTypingIoFamily, cls.HasEnumMetadata,
            cls.IsNativeRuntimeClass, cls.HasForeignHierarchy],
        IoKind = (int)cls.TypingIoKind,
    };

    private static bool SameShadow(ShadowSnapshot left, ShadowSnapshot right)
    {
        if (!ReferenceEquals(left.Subject, right.Subject) || left.Mro.Count != right.Mro.Count
            || left.Dependencies.Count != right.Dependencies.Count) return false;
        for (int i = 0; i < left.Mro.Count; i++)
            if (!ReferenceEquals(left.Mro[i].Class, right.Mro[i].Class)) return false;
        for (int i = 0; i < left.Dependencies.Count; i++)
        {
            ShadowNode a = left.Dependencies[i], b = right.Dependencies[i];
            if (!ReferenceEquals(a.Class, b.Class) || a.IoKind != b.IoKind
                || !SameShadowReferences(a.References, b.References)
                || !SameShadowReferences(a.ForeignElements, b.ForeignElements)) return false;
            for (int j = 0; j < a.Flags.Length; j++) if (a.Flags[j] != b.Flags[j]) return false;
        }
        return true;
    }

    private static bool SameShadowReferences(object?[]? left, object?[]? right)
    {
        if (left is null || right is null) return left is null && right is null;
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (!ReferenceEquals(left[i], right[i])) return false;
        return true;
    }
}


// 仅影子诊断的只读结构视图，不提供冻结/线程安全承诺，不改变原建类或捕获路径。
public sealed partial class PyClass
{
    internal readonly record struct CaptureShadowStructure(
        PyClass[]? Mro, object? MroIdentity, object? DictIdentity, bool Ready);

    internal CaptureShadowStructure ReadCaptureShadowStructure()
    {
        if (_ordinaryMro is { Length: > 256 })
            throw new InvalidOperationException("影子MRO超出诊断边界");
        return new(_ordinaryMro is null ? null : (PyClass[])_ordinaryMro.Clone(), _ordinaryMro, _dictC3,
            _ordinaryFinalized && !_lexicalFinalizing && _pendingMiBases is null && !_failedMi);
    }
}
