// optparse 的真实 CLR 异常树；业务字段保原对象，args 只保实际位置参数。
namespace RyuPy.Runtime;

/// <summary>optparse 错误共同基类，不开放生成用户派生构造 ABI。</summary>
public class PyOptParseError : PyException
{
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public PyOptParseError(object? msg) : this("OptParseError", [msg], msg) { }
    internal PyOptParseError(string name, object?[] args, object? msg) : base(name, "", args)
        => UserAttributes["msg"] = msg;
    /// <summary>按当前业务字段延迟显示，不从args重建消息。</summary>
    public override string PyMessage => PyOptParseExceptions.RequireMessage(Field("msg"));
    internal object? Field(string name) => UserAttributes.TryGetValue(name, out var value) ? value
        : throw (PyBaseException)PyErrors.PrepareRaise(new PyAttributeError($"'{PyTypeName}' object has no attribute '{name}'"));
    internal void Write(string name, object? value)
    {
        GuardDescriptor(name);
        if (name == "args") throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("optparse exception args reassignment is not supported yet"));
        UserAttributes[name] = value;
    }
    internal void Delete(string name)
    {
        GuardDescriptor(name);
        if (name == "__suppress_context__") throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("optparse exception descriptor deletion is not supported yet"));
        if (name == "args") throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError("args may not be deleted"));
        if (!UserAttributes.Remove(name)) _ = Field(name);
    }
    private static void GuardDescriptor(string name)
    {
        if (name is "__class__" or "__traceback__" or "__cause__" or "__context__" or "__dict__")
            throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("optparse exception descriptor mutation is not supported yet"));
    }
}

/// <summary>选项声明错误；仅 option 在构造时转换一次。</summary>
public class PyOptionError : PyOptParseError
{
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public PyOptionError(object? msg, object? option) : this("OptionError", [msg, option], msg, option) { }
    internal PyOptionError(string name, object?[] args, object? msg, object? option) : base(name, args, msg)
        => UserAttributes["option_id"] = PyRepr.Str(option);
    /// <summary>按当前业务字段延迟显示，不从args重建消息。</summary>
    public override string PyMessage
    {
        get
        {
            if (!PyOps.Truthy(Field("option_id")))
                return PyOptParseExceptions.RequireMessage(Field("msg"));
            // 真值回调后重读两个字段，先冻结格式元组再触发任一字符串转换。
            object? id = Field("option_id");
            object? message = Field("msg");
            return PyPercentFormat.Format("option %s: %s", new PyTuple([id, message]));
        }
    }
}

/// <summary>选项名冲突，保 OptionError 的实际父类及签名。</summary>
public sealed class PyOptionConflictError : PyOptionError
{
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public PyOptionConflictError(object? msg, object? option) : this([msg, option], msg, option) { }
    internal PyOptionConflictError(object?[] args, object? msg, object? option)
        : base("OptionConflictError", args, msg, option) { }
}

/// <summary>命令行值转换错误。</summary>
public sealed class PyOptionValueError : PyOptParseError
{
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public PyOptionValueError(object? msg) : this([msg], msg) { }
    internal PyOptionValueError(object?[] args, object? msg) : base("OptionValueError", args, msg) { }
}

/// <summary>未知选项；不生成基类 msg 字段。</summary>
public class PyBadOptionError : PyOptParseError
{
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public PyBadOptionError(object? option) : this("BadOptionError", [option], option) { }
    internal PyBadOptionError(string name, object?[] args, object? option) : base(name, args, null)
    {
        UserAttributes.Remove("msg");
        UserAttributes["opt_str"] = option;
    }
    /// <summary>按当前业务字段延迟显示，不从args重建消息。</summary>
    public override string PyMessage => PyPercentFormat.Format("no such option: %s", Field("opt_str"));
}

/// <summary>长选项缩写歧义，possibilities 到显示时才遍历。</summary>
public sealed class PyAmbiguousOptionError : PyBadOptionError
{
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public PyAmbiguousOptionError(object? option, object? possibilities) : this([option, possibilities], option, possibilities) { }
    internal PyAmbiguousOptionError(object?[] args, object? option, object? possibilities)
        : base("AmbiguousOptionError", args, option) => UserAttributes["possibilities"] = possibilities;
    /// <summary>按当前业务字段延迟显示，不从args重建消息。</summary>
    public override string PyMessage
    {
        get
        {
            object? option = Field("opt_str");
            object? choices = PyOps.CallMethod(", ", "join", [Field("possibilities")], [], []);
            return PyPercentFormat.Format("ambiguous option: %s (%s?)", new PyTuple([option, choices]));
        }
    }
}
