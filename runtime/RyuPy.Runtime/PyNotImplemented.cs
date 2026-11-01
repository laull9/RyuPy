namespace RyuPy.Runtime;

/// <summary>富比较拒绝当前操作的返回哨兵；不是 NotImplementedError 异常。</summary>
public sealed class PyNotImplemented
{
    private PyNotImplemented() { }

    /// <summary>唯一的 Python NotImplemented 值。</summary>
    public static readonly PyNotImplemented Instance = new();

    /// <summary>通过 type(NotImplemented) 构造仍返回唯一哨兵，不接受任何参数。</summary>
    public static object Create(object?[] args, string[] names)
    {
        if (args.Length != 0 || names.Length != 0) throw new PyTypeError("NotImplementedType takes no arguments");
        return Instance;
    }

    /// <inheritdoc />
    public override string ToString() => "NotImplemented";
}
