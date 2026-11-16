// binascii.Error 的真实 ValueError 派生类型；静态 catch、动态异常匹配和模块导出共用同一身份。
namespace RyuPy.Runtime;

/// <summary>二进制文本转换格式错误。</summary>
public class PyBinasciiError : PyValueError
{
    /// <summary>按 Python 的 Error 名称构造格式异常。</summary>
    public PyBinasciiError(string message) : base("Error", message) { }
    /// <summary>供静态生成的用户异常派生类保留参数与名称。</summary>
    protected PyBinasciiError(string typeName, string message, object?[] args) : base(typeName, message, args) { }
}
