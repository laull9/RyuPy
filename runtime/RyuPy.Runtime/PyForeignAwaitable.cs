// 外部 await 在值所属域查类型槽，暂停、完成和异常不折叠为普通 for 的布尔结果。
namespace RyuPy.Runtime;

/// <summary>外部执行域的类型级 await 协议；没有槽时返回 null，槽执行失败照常传播。</summary>
public interface IPyForeignAwaitable
{
    /// <summary>从真实类型槽取一个迭代器；不调用实例上的同名属性。</summary>
    IPyForeignAwaitIterator? GetAwaitIterator();
}

/// <summary>保留原迭代对象供 send/throw/close 查找，以及带终值的逐步恢复。</summary>
public interface IPyForeignAwaitIterator : IPyIterator
{
    /// <summary>由通用委托执行 send/throw/close 的迭代器对象。</summary>
    object Target { get; }
    /// <summary>下一项，或带有终值的 StopIteration。</summary>
    object? NextValue();
}
