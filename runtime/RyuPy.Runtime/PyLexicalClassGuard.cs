// 词法类族的late能力门：旧普通类仅一个false分支；新族用统一类版本失效，不做用户调用。
namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal bool IsLexicalFactoryFamily { get; private set; }
    private long _lexicalValidatedVersion = -1;
    private bool _lexicalFinalizing;

    internal void InheritLexicalFactoryFamily(PyClass parent)
    {
        // 身份传播不依赖当前类是否已完成；终结包装负责在内部构造检查时暂时放行。
        IsLexicalFactoryFamily = parent.IsLexicalFactoryFamily;
    }

    internal void EnableLexicalFactoryFamily()
    {
        IsLexicalFactoryFamily = true;
        RequireLexicalFactoryReady();
    }

    internal void RequireLexicalFactoryReady()
    {
        if (!IsLexicalFactoryFamily || _lexicalFinalizing || _lexicalValidatedVersion == GlobalVersion) return;
        // 失败不记有效版本；删除域外项后，下一消费可重新证明支持。
        PyLexicalClassFactory.ValidateFamily(this);
        _lexicalValidatedVersion = GlobalVersion;
    }
}

internal static class PyLexicalClassGuard
{
    // super检查实际receiver，不能仅验证可能未标记的旧owner。
    internal static void Receiver(object? receiver)
    {
        switch (receiver)
        {
            case PyInstance instance: instance.Class.RequireLexicalFactoryReady(); break;
            case PyClass cls: cls.RequireLexicalFactoryReady(); break;
            case PySuper super: Receiver(super.Self); break;
        }
    }
}
