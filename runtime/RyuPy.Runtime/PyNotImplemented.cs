namespace RyuPy.Runtime;

/// <summary>富比较拒绝当前操作的返回哨兵；不是 NotImplementedError 异常。</summary>
public sealed class PyNotImplemented
{
    private PyNotImplemented() { }

    /// <summary>唯一的 Python NotImplemented 值。</summary>
    public static readonly PyNotImplemented Instance = new();

    /// <inheritdoc />
    public override string ToString() => "NotImplemented";
}
