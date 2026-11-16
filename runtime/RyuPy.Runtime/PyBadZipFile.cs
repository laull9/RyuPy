// BadZipFile拥有真实CLR继承，typed catch和生成的用户子类都使用同一类型；参数保存不提前格式化。
namespace RyuPy.Runtime;

/// <summary>zipfile.BadZipFile：普通Exception子类，不冒充OSError或ValueError。</summary>
public class PyBadZipFile : PyException
{
    /// <summary>无参异常的args为空，与显式空字符串不同。</summary>
    public PyBadZipFile() : this([]) { }
    /// <summary>文件识别器的消息入口。</summary>
    public PyBadZipFile(string message) : this([message]) { }
    private PyBadZipFile(object?[] args) : base("BadZipFile", "", args) { }
    /// <summary>供静态生成的用户异常子类保留真实名字和原始参数。</summary>
    protected PyBadZipFile(string typeName, string message, object?[] args)
        : base(typeName, "", args) { }
    /// <summary>只在显式显示时按BaseException规则格式化参数。</summary>
    public override string PyMessage => PyErrors.UserExceptionMessage(Args);
    /// <summary>普通异常构造接受任意位置参数，不接受关键字。</summary>
    public static object Construct(object?[] args, string[] names)
    {
        if (names.Length != 0)
            throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError("BadZipFile() takes no keyword arguments"));
        return new PyBadZipFile((object?[])args.Clone());
    }
}
