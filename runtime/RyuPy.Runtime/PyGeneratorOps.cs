namespace RyuPy.Runtime;

public static partial class PyOps
{
    private sealed class GeneratorRetryMarker { }
    private static readonly object RetryMarker = new GeneratorRetryMarker();

    /// <summary>在逻辑异常路由后请求同一次恢复继续运行，而不向 Python 产出值。</summary>
    public static object GeneratorRetry() => RetryMarker;

    /// <summary>在生成器入口或 yield 恢复点消费发送值及注入异常。</summary>
    /// <param name="frame">程序计数器、发送值、注入异常、待处理异常及局部槽数组。</param>
    public static object? GeneratorInput(object frame)
    {
        if (frame is not object?[] slots || slots.Length < 4)
            throw new PyRuntimeError("invalid generator frame state");
        object? injected = slots[2];
        object? sent = slots[1];
        slots[1] = null;
        slots[2] = null;
        if (injected is PyBaseException exception)
        {
            PyTraceback.Attach(exception);
            throw PyErrors.Prepare(exception);
        }
        if (injected is not null and not PyNone)
            throw new PyRuntimeError("invalid generator injected exception");
        return sent ?? PyNone.Instance;
    }
}
