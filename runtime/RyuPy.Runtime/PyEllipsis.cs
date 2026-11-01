namespace RyuPy.Runtime;

/// <summary>Python Ellipsis真实单例；字面量与内建名字共用身份，不伪装为函数。</summary>
public sealed class PyEllipsis : PyNativeObject
{
    /// <summary>唯一公开实例。</summary>
    public static PyEllipsis Instance { get; } = new();
    private PyEllipsis() { }
    /// <inheritdoc />
    public override string TypeName => "ellipsis";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name == "__class__" ? PyType.Get("ellipsis") : throw Missing(name);
    /// <inheritdoc />
    public override string PythonRepr() => "Ellipsis";
    /// <inheritdoc />
    public override string PythonStr() => "Ellipsis";

    /// <summary>type(Ellipsis)()只接受无参构造并返回同一单例。</summary>
    /// <param name="args">位置实参。</param><param name="names">关键字名。</param>
    public static object Create(object?[] args, string[] names)
    {
        if (args.Length != 0 || names.Length != 0)
            throw new PyTypeError("EllipsisType takes no arguments");
        return Instance;
    }
}
