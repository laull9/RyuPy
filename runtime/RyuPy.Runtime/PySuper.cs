// super 的中性身份：普通代理只保存定义类与接收者两个引用，保持原生热布局。
// 外部代理、读取委托及懒导出上下文集中在显式冷目标中；Runtime 不读取 CPython 布局。
using System;

namespace RyuPy.Runtime;

/// <summary>本地词法 super 或经过 provider 精确类型验证的外部 super。</summary>
public sealed class PySuper
{
    // target 只取 PyClass 或 ForeignTarget；普通构造不创建包装、委托或旁表条目。
    private object target;

    private sealed class ForeignTarget(PyClass? owner, IPyForeignObject proxy, Func<string, object?> read)
    {
        internal readonly PyClass? Owner = owner;
        internal readonly IPyForeignObject Proxy = proxy;
        internal readonly Func<string, object?> Read = read;
    }

    /// <summary>本地词法方法的定义类；外部不透明代理的属性通过 provider 读取。</summary>
    public PyClass Owner => target as PyClass ?? (target as ForeignTarget)?.Owner
        ?? throw new PyNotImplementedError("foreign super has no local class owner");
    /// <summary>本地词法接收者；不透明外部代理不复制可能被 MRO 同名项遮蔽的元数据。</summary>
    public object? Self { get; }
    /// <summary>已经建立的真实外部代理；读取此属性不创建对象，也不进入外部执行域。</summary>
    public IPyForeignObject? ForeignObject => (target as ForeignTarget)?.Proxy;
    internal bool IsForeignBacked => target is ForeignTarget;
    internal object? ReadForeign(string name) => ((ForeignTarget)target).Read(name);

    /// <summary>构造本地词法代理；其实际接收者可具有完整外部层级。</summary>
    public PySuper(PyClass owner, object? self) { target = owner; Self = self; }
    private PySuper(IPyForeignObject foreign, Func<string, object?> readForeign)
    { target = new ForeignTarget(null, foreign, readForeign); }

    /// <summary>provider 验证真实 super 类型后包装其 owned 代理，读属性仍由该代理执行。</summary>
    public static PySuper FromForeign(IPyForeignObject foreign, Func<string, object?> readAttribute) =>
        new(foreign, readAttribute);

    // 仅外部布局的本地代理能导出为真实 super；冷目标发布后保留同一个外部对象身份，
    // 同时保存原本的本地 owner，外部往返与后续释放不改变原接收者或词法起点。
    internal IPyForeignObject? ExportForeign()
    {
        if (target is ForeignTarget existing) return existing.Proxy;
        var owner = (PyClass)target;
        bool external = Self is PyInstance { Class.HasForeignHierarchy: true }
            or PyClass { HasForeignHierarchy: true } || owner.ForeignClass is not null;
        if (!external) return null;
        PySuper actual = PyForeignClassBridge.Require().CreateSuper(owner, Self);
        var exported = (ForeignTarget)actual.target;
        target = new ForeignTarget(owner, exported.Proxy, exported.Read);
        return exported.Proxy;
    }
}
