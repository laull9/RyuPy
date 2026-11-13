// 压缩模块的真实 CLR 异常与 Python 身份；zlib.error 使用限定键，和 re.error 完全分离。
namespace RyuPy.Runtime;

/// <summary>zlib.error，继承 Exception。</summary>
public sealed class PyZlibError : PyException
{
    /// <summary>以原生压缩错误消息构造异常。</summary>
    public PyZlibError(string message) : base("zlib.error", message) { }
}

/// <summary>gzip.BadGzipFile，继承 OSError。</summary>
public sealed class PyBadGzipFile : PyOSError
{
    /// <summary>以容器验证错误消息构造异常。</summary>
    public PyBadGzipFile(string message) : base("BadGzipFile", message) { }
}

/// <summary>有界资源申请失败，供压缩输出限制及普通 Python catch 共用。</summary>
public class PyMemoryError : PyException
{
    /// <summary>构造带说明的内存边界错误。</summary>
    public PyMemoryError(string message) : base("MemoryError", message) { }
    /// <summary>用户派生异常的编译构造 ABI。</summary>
    protected PyMemoryError(string typeName, string message, object?[] args) : base(typeName, message, args) { }
}
