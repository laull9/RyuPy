// namedtuple唯一内部提交点：先完整准备，再更新未发布目标的属性/布局并统一失效缓存。
// 不开放普通类事后转换；类型根/注解/finalizer规则留在专用冷构建器。
namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal void CommitNamedTuple(PyNamedTupleLayout.PreparedClass prepared)
    {
        if (!ReferenceEquals(prepared.Owner, this) || NamedTupleLayout is not null || _ordinaryFinalized
            || HasStringLayout || HasListLayout || HasDictLayout || HasEnumMetadata || DataclassActive)
            throw PyNamedTupleLayout.Unsupported("namedtuple layout can only be attached once to its prepared owner");
        PyTypingNamedTupleShell.ValidateCommit(this, prepared);
        // 此后没有Python回调；分配失败不承诺CLR OOM事务，但不能发布半成品。
        Attributes.Clear();
        foreach ((string name, object? value) in prepared.Attributes) Attributes.Add(name, value);
        NamedTupleLayout = prepared.Layout;
        PyTypingNamedTupleShell.CompleteCommit(this);
        ConstructionLayoutChanged();
    }
}
