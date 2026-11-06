// getopt异常使用真实CLR类型；业务字段与固定两项args独立，消息转换延迟到显示消费。
using System;
namespace RyuPy.Runtime;

/// <summary>getopt.GetoptError；error导出复用同一类型，不支持生成用户派生ABI。</summary>
public class PyGetoptError : PyException
{
    private static readonly PySignature Signature = PySignature.Create("GetoptError.__init__",
        ["self", "msg", "opt"], ["pos", "pos", "pos"], ["0", "0", "1"]);
    private static readonly object?[] Defaults = [null, null, ""];

    /// <summary>解析器错误入口；不转换msg/opt，不触发任一用户回调。</summary>
    public PyGetoptError(object? msg, object? opt) : base("GetoptError", "", [msg, opt])
    {
        UserAttributes["msg"] = msg;
        UserAttributes["opt"] = opt;
    }

    /// <summary>CLR文本消费者展开已验证字符串；Python直接__str__不走此入口。</summary>
    public override string PyMessage => PyRepr.Str(this);

    // 隐式self参与签名诊断，保留CPython的__init__名字和参数计数。
    internal static PyBaseException Construct(object?[] args, string[] names, object?[] values)
    {
        var supplied = new object?[args.Length + 1];
        supplied[0] = PyNone.Instance;
        Array.Copy(args, 0, supplied, 1, args.Length);
        object?[] bound;
        try { bound = PyBinder.Bind(Signature, supplied, names, values, Defaults); }
        catch (PyBaseException error)
        {
            // 仅补本构造器默认参数范围的冷诊断；原绑定器仍决定错误优先级。
            if (error is PyTypeError && supplied.Length > 3
                && error.PyMessage == $"GetoptError.__init__() takes 3 positional arguments but {supplied.Length} were given")
                throw Own(new PyTypeError($"GetoptError.__init__() takes from 2 to 3 positional arguments but {supplied.Length} were given"));
            PyErrors.PrepareRaise(error);
            throw;
        }
        return new PyGetoptError(bound[1], bound[2]);
    }

    // msg/opt删除后回落CP类默认值；直接方法保留返回的原始字符串子类/非字符串对象。
    internal object? Field(string name)
    {
        if (UserAttributes.TryGetValue(name, out object? value)) return value;
        if (name is "msg" or "opt") return "";
        if (name == "__str__") return StrMethod();
        if (name == "__dict__") throw Own(new PyNotImplementedError("getopt exception __dict__ is not supported yet"));
        throw Own(new PyAttributeError($"'{PyTypeName}' object has no attribute '{name}'"));
    }

    // 特殊槽查找绕过实例同名属性；显式属性读取仍按实例字典优先。
    internal PyFunction StrMethod() => PyNativeFunctions.Create("GetoptError.__str__", [], [], 0, _ => Field("msg"));

    internal void Write(string name, object? value)
    {
        GuardDescriptor(name);
        if (name == "args") throw Own(new PyNotImplementedError("getopt exception args reassignment is not supported yet"));
        UserAttributes[name] = value;
    }

    internal void Delete(string name)
    {
        GuardDescriptor(name);
        if (name == "args") throw Own(new PyTypeError("args may not be deleted"));
        if (name == "__suppress_context__") throw Own(new PyNotImplementedError("getopt exception descriptor deletion is not supported yet"));
        if (!UserAttributes.Remove(name)) throw Own(new PyAttributeError($"'{PyTypeName}' object has no attribute '{name}'"));
    }

    private static void GuardDescriptor(string name)
    {
        if (name is "__class__" or "__traceback__" or "__cause__" or "__context__" or "__dict__")
            throw Own(new PyNotImplementedError("getopt exception descriptor mutation is not supported yet"));
    }
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
