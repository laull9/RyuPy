namespace RyuPy.Runtime;

/// <summary>原生标准库对象的显式协议边界，不使用逐对象反射寻找Python方法。</summary>
public abstract class PyNativeObject
{
    /// <summary>用于Python类型判定和错误消息的名称。</summary>
    public abstract string TypeName { get; }

    /// <summary>读取公开属性或返回已经绑定接收者的可调用值。</summary>
    public abstract object? GetAttr(string name);

    /// <summary>默认对象不可动态添加属性；可写属性由具体对象明确开放。</summary>
    public virtual void SetAttr(string name, object? value) => throw Missing(name);

    /// <summary>默认不支持删除属性。</summary>
    public virtual void DelAttr(string name) => throw Missing(name);

    /// <summary>Python repr，不把CLR类型名泄漏为语言层表示。</summary>
    public virtual string PythonRepr() => $"<{TypeName} object>";

    /// <summary>Python str，路径等对象可以覆盖。</summary>
    public virtual string PythonStr() => PythonRepr();

    /// <summary>统一构造缺少属性错误。</summary>
    protected PyAttributeError Missing(string name) => new($"'{TypeName}' object has no attribute '{name}'");
}
