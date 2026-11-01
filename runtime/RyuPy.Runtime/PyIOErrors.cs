// 文件类型不匹配必须保留 Python OSError 子类，不能仅把 CLR 消息包装成普通 OSError。
namespace RyuPy.Runtime;

/// <summary>Python I/O 协议的显式运行时实现。</summary>
public class PyIsADirectoryError : PyOSError
{
    /// <summary>按给定配置构造运行时对象。</summary>
    public PyIsADirectoryError(string message) : base("IsADirectoryError", message) { }
    /// <summary>按给定配置构造运行时对象。</summary>
    protected PyIsADirectoryError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}
/// <summary>Python I/O 协议的显式运行时实现。</summary>
public class PyNotADirectoryError : PyOSError
{
    /// <summary>按给定配置构造运行时对象。</summary>
    public PyNotADirectoryError(string message) : base("NotADirectoryError", message) { }
    /// <summary>按给定配置构造运行时对象。</summary>
    protected PyNotADirectoryError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}
