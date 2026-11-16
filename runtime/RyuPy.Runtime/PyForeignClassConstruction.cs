// 类定义的冷构造事务：普通类保持原布局，只有外部类在弱键表中持有 prepared namespace。
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>外部类型构造的私有命名空间；Contains 冻结一次读取，Get 消费同一结果。</summary>
public interface IPyForeignClassDefinition : IDisposable
{
    /// <summary>执行一次真实命名空间读取并冻结其结果。</summary>
    bool Contains(string name);
    /// <summary>消费已冻结结果；直接读取时执行一次查询。</summary>
    object? Get(string name);
    /// <summary>按命名空间映射协议写入一个绑定。</summary>
    void Set(string name, object? value);
    /// <summary>删除自有绑定，缺名返回 false。</summary>
    bool Delete(string name);
}

internal static class PyForeignClassConstruction
{
    private static ConditionalWeakTable<PyClass, IPyForeignClassDefinition>? definitions;
    internal static void Begin(PyClass owner)
    {
        if (!owner.HasForeignHierarchy || TryGet(owner, out _)) return;
        IPyForeignClassDefinition? definition = PyForeignClassBridge.Require().PrepareDerivedClass(owner, owner.ForeignCreationBases());
        if (definition is null) return;
        LazyInitializer.EnsureInitialized(ref definitions).Add(owner, definition);
        try { foreach (var pair in owner.Attributes) definition.Set(pair.Key, pair.Value); }
        catch { Abort(owner); throw; }
    }
    internal static bool TryGet(PyClass owner, out IPyForeignClassDefinition value)
    {
        if (definitions is { } current) return current.TryGetValue(owner, out value!);
        value = null!; return false;
    }
    internal static void Forget(PyClass owner) => definitions?.Remove(owner);
    internal static void Abort(PyClass owner)
    {
        if (!TryGet(owner, out IPyForeignClassDefinition definition)) return;
        definitions!.Remove(owner); definition.Dispose();
    }
}
