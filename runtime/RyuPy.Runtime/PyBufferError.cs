namespace RyuPy.Runtime;

/// <summary>Python BufferError：活动buffer导出禁止改变可变存储的长度。</summary>
public class PyBufferError : PyException
{
    /// <summary>构造标准BufferError。</summary>
    public PyBufferError(string message) : base("BufferError", message) { }
    /// <summary>供Python用户异常子类构造器保留类型名与args。</summary>
    protected PyBufferError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}
