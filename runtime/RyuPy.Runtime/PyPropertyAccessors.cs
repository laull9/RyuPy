namespace RyuPy.Runtime;

public sealed partial class PyProperty
{
    /// <summary>deleter（可调用值）；没有时为 null。</summary>
    public object? Deleter { get; }

    /// <summary>构造包含 getter、setter、deleter 的属性描述符。</summary>
    /// <param name="getter">读取函数。</param>
    /// <param name="setter">写入函数。</param>
    /// <param name="deleter">删除函数。</param>
    public PyProperty(object? getter, object? setter, object? deleter)
    {
        Getter = getter is PyNone ? null : getter;
        Setter = setter is PyNone ? null : setter;
        Deleter = deleter is PyNone ? null : deleter;
    }

    internal object? GetAccessor(string name) => name switch
    {
        "fget" => Getter ?? PyNone.Instance,
        "fset" => Setter ?? PyNone.Instance,
        "fdel" => Deleter ?? PyNone.Instance,
        "getter" or "setter" or "deleter" => new PyBuiltinMethod(this, name, PyOps.InvokePropertyAccessor),
        _ => throw new PyAttributeError($"'property' object has no attribute '{name}'"),
    };
}

public static partial class PyOps
{
    internal static object InvokePropertyAccessor(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        var property = (PyProperty)receiver;
        // accessor 装饰器返回新描述符，不修改此前已经保存的 property 别名。
        return name switch
        {
            "getter" => new PyProperty(args[0], property.Setter, property.Deleter),
            "setter" => new PyProperty(property.Getter, args[0], property.Deleter),
            "deleter" => new PyProperty(property.Getter, property.Setter, args[0]),
            _ => throw new PyAttributeError($"'property' object has no attribute '{name}'"),
        };
    }
}
