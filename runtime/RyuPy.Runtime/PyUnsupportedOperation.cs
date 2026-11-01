// io.UnsupportedOperation 的 CLR 主继承链为 OSError；ValueError 额外父边由 Python 层次表和 MIR catch 展开共同实现。
namespace RyuPy.Runtime;
/// <summary>I/O 流不支持所请求操作，同时属于 Python OSError 与 ValueError。</summary>
public class PyUnsupportedOperation : PyOSError
{
    /// <summary>构造无参数异常，不伪造空字符串参数。</summary>
    public PyUnsupportedOperation() : this([]) { }
    /// <summary>运行时流能力检查的单消息入口。</summary>
    public PyUnsupportedOperation(string message) : this([message]) { }
    private PyUnsupportedOperation(object?[] args) : base("UnsupportedOperation", "", args) { }
    /// <summary>生成的用户异常子类保留独立类型名和参数身份。</summary>
    protected PyUnsupportedOperation(string typeName, string message, object?[] args) : base(typeName, message, args)
    {
        if (args.Length > 1) throw new PyNotImplementedError("UnsupportedOperation errno/multiple argument construction is not supported yet");
    }
    /// <summary>异常构造不调用用户str；只有显示消息时才读取参数。</summary>
    public override string PyMessage => Args.Length switch
    { 0 => "", 1 => PyRepr.Str(Args[0]), _ => PyRepr.Str(new PyTuple(Args)) };
    /// <summary>有限显式构造：不吞关键字或尚未实现的 OSError errno 多参数协议。</summary>
    public static object Construct(object?[] args, string[] names)
    {
        if (names.Length != 0) throw new PyTypeError("UnsupportedOperation() takes no keyword arguments");
        if (args.Length > 1) throw new PyNotImplementedError("UnsupportedOperation errno/multiple argument construction is not supported yet");
        return new PyUnsupportedOperation((object?[])args.Clone());
    }
}
