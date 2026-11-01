namespace RyuPy.Runtime;

/// <summary>警告异常保留原始args，只在Python读取消息时调用str；CLR继承树与Python一致。</summary>
public class PyWarning : PyException
{
    /// <summary>无参或字符串异常工厂兼容入口。</summary>
    public PyWarning(string message = "") : this("Warning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留用户原参数，不提前格式化。</summary>
    public PyWarning(object?[] args) : this("Warning", "", args) { }
    /// <summary>供标准/用户警告子类传入真实名称及参数。</summary>
    protected PyWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyWarning(string typeName, string message, object?[] args) : base(typeName, "", (object?[])args.Clone()) { }
    /// <inheritdoc />
    public override string PyMessage => PyErrors.UserExceptionMessage(Args);
}

/// <summary>内建UserWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyUserWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyUserWarning(string message = "") : base("UserWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyUserWarning(object?[] args) : base("UserWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyUserWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyUserWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建DeprecationWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyDeprecationWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyDeprecationWarning(string message = "") : base("DeprecationWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyDeprecationWarning(object?[] args) : base("DeprecationWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyDeprecationWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyDeprecationWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建PendingDeprecationWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyPendingDeprecationWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyPendingDeprecationWarning(string message = "") : base("PendingDeprecationWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyPendingDeprecationWarning(object?[] args) : base("PendingDeprecationWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyPendingDeprecationWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyPendingDeprecationWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建RuntimeWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyRuntimeWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyRuntimeWarning(string message = "") : base("RuntimeWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyRuntimeWarning(object?[] args) : base("RuntimeWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyRuntimeWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyRuntimeWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建SyntaxWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PySyntaxWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PySyntaxWarning(string message = "") : base("SyntaxWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PySyntaxWarning(object?[] args) : base("SyntaxWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PySyntaxWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PySyntaxWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建FutureWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyFutureWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyFutureWarning(string message = "") : base("FutureWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyFutureWarning(object?[] args) : base("FutureWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyFutureWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyFutureWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建ImportWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyImportWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyImportWarning(string message = "") : base("ImportWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyImportWarning(object?[] args) : base("ImportWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyImportWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyImportWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建UnicodeWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyUnicodeWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyUnicodeWarning(string message = "") : base("UnicodeWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyUnicodeWarning(object?[] args) : base("UnicodeWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyUnicodeWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyUnicodeWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建BytesWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyBytesWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyBytesWarning(string message = "") : base("BytesWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyBytesWarning(object?[] args) : base("BytesWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyBytesWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyBytesWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>内建ResourceWarning；保留父类Warning的延迟消息与异常匹配。</summary>
public class PyResourceWarning : PyWarning
{
    /// <summary>兼容字符串工厂。</summary>
    public PyResourceWarning(string message = "") : base("ResourceWarning", "", message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始Python参数。</summary>
    public PyResourceWarning(object?[] args) : base("ResourceWarning", "", args) { }
    /// <summary>供用户子类构造。</summary>
    protected PyResourceWarning(string typeName, string message) : this(typeName, message, message.Length == 0 ? [] : [message]) { }
    /// <summary>保留原始参数的派生构造入口。</summary>
    protected PyResourceWarning(string typeName, string message, object?[] args) : base(typeName, "", args) { }
}

/// <summary>警告的原始参数工厂；不通过字符串异常工厂丢弃对象身份。</summary>
public static class PyWarningTypes
{
    /// <summary>按已支持的内建警告类型名构造。</summary>
    public static PyBaseException Create(string name, object?[] args) => name switch
    {
        "Warning" => new PyWarning(args),
        "UserWarning" => new PyUserWarning(args),
        "DeprecationWarning" => new PyDeprecationWarning(args),
        "PendingDeprecationWarning" => new PyPendingDeprecationWarning(args),
        "RuntimeWarning" => new PyRuntimeWarning(args),
        "SyntaxWarning" => new PySyntaxWarning(args),
        "FutureWarning" => new PyFutureWarning(args),
        "ImportWarning" => new PyImportWarning(args),
        "UnicodeWarning" => new PyUnicodeWarning(args),
        "BytesWarning" => new PyBytesWarning(args),
        "ResourceWarning" => new PyResourceWarning(args),
        _ => throw new PyTypeError("category must be a Warning subclass"),
    };
}
