// 语法错误及ElementTree ParseError的真实CLR父链；定位元组协议单独明确拒绝。
namespace RyuPy.Runtime;

/// <summary>SyntaxError的零/单消息域，原args/msg不提前字符串化。</summary>
public class PySyntaxError : PyException
{
    /// <summary>运行时自身产生的单条语法错误消息。</summary>
    public PySyntaxError(string message) : this("SyntaxError", message) { }
    // 仅本程序集的真实ParseError子类使用，不为用户派生提供假ABI。
    internal PySyntaxError(string name, string message) : base(name, message, [message]) { }
    /// <inheritdoc />
    public override string PyMessage => Args.Length == 0 ? "None" : (string)Args[0]!;

    internal object? Field(string name) => name switch
    {
        "msg" => Args.Length == 0 ? PyNone.Instance : Args[0],
        "filename" or "lineno" or "offset" or "text" or "end_lineno" or "end_offset" => PyNone.Instance,
        _ => throw PyXmlTreeBuilderErrors.Attribute($"'{PyTypeName}' object has no attribute '{name}'"),
    };
    internal static PySyntaxError Construct(string name, object?[] args, string[] keywords)
    {
        if (keywords.Length != 0) throw PyXmlTreeBuilderErrors.Type($"{name}() takes no keyword arguments");
        if (args.Length > 1) throw PyXmlTreeBuilderErrors.Unsupported(name + " positional location details");
        if (args.Length == 1 && args[0] is not string)
            throw PyXmlTreeBuilderErrors.Unsupported(name + " message other than exact str");
        PySyntaxError error = name == "ParseError" ? new PyXmlParseError("") : new PySyntaxError("");
        error.InitializePositionalArguments(args);
        return error;
    }
}

/// <summary>ElementTree自身解析错误，CLR与Python都真正继承SyntaxError。</summary>
public sealed class PyXmlParseError : PySyntaxError
{
    /// <summary>构造一条ParseError消息，不伪造parser的code/position。</summary>
    public PyXmlParseError(string message) : base("ParseError", message) { }
}
