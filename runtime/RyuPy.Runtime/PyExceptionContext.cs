namespace RyuPy.Runtime;

/// <summary>异常处理链节点；空异常节点用作可临时连接调用者的生成器根。</summary>
internal sealed class PyExceptionContext(PyBaseException? exception, PyExceptionContext? parent)
{
    internal PyBaseException? Exception { get; } = exception;
    internal PyExceptionContext? Parent { get; set; } = parent;

    internal PyBaseException? FindException()
    {
        for (PyExceptionContext? node = this; node is not null; node = node.Parent)
            if (node.Exception is not null) return node.Exception;
        return null;
    }
}

public static partial class PyErrors
{
    // 节点 token 可保存在跨 yield 的局部槽；不把某次恢复借用的调用者异常永久捕获。
    internal static PyExceptionContext? ContextNode
    {
        get => _current;
        set => _current = value;
    }
}
