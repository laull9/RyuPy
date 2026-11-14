namespace RyuPy.Runtime;

/// <summary>列表 clear/pop/reverse/copy 的窄绑定器：拒绝错误实参后才执行原容器操作。</summary>
public static partial class PyOps
{
    // 三个调用点只传入这四个名字；不把其它容器方法纳入全表签名检查。
    // 实参已经完整求值并展开，直接与保存/暂停调用共用相同的错误优先级。
    internal static object? CallListCheckedMethod(PyList list, string name, object?[] args, string[] names)
    {
        if (names.Length > 0)
        {
            throw ListMethodBindingError($"list.{name}() takes no keyword arguments");
        }
        if (name == "pop")
        {
            if (args.Length > 1)
            {
                throw ListMethodBindingError($"pop expected at most 1 argument, got {args.Length}");
            }
            // 先完成显式索引转换，再让 Pop 读取回调后的当前长度。
            return args.Length == 0 ? list.Pop() : list.Pop(ListPopIndex(args[0]));
        }
        if (args.Length != 0)
        {
            throw ListMethodBindingError($"list.{name}() takes no arguments ({args.Length} given)");
        }
        return name switch
        {
            "clear" => Void(list.Clear),
            "reverse" => Void(list.Reverse),
            "copy" => list.Copy(),
            _ => throw MissingAttribute("list", name),
        };
    }

    // 只准备本绑定器新建的错误；成功路没有 EH，也不截获/改写原容器正文异常。
    private static PyTypeError ListMethodBindingError(string message)
    {
        var error = new PyTypeError(message);
        PyErrors.PrepareRaise(error);
        return error;
    }
}
