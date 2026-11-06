// 根字符串方法复用原文本算法；参数只读取真实文本，不经过__str__。
namespace RyuPy.Runtime;

internal static class PyStringMethods
{
    internal static bool Has(string name) => name is "encode" or "upper" or "lower" or "strip" or "lstrip"
        or "rstrip" or "split" or "splitlines" or "join" or "replace" or "find" or "rfind"
        or "index" or "rindex" or "count" or "startswith" or "endswith" or "format"
        or "partition" or "rpartition";
    internal static object? Invoke(object receiver, string name, object?[] args, string[] names, object?[] values, bool bound)
    {
        // format的终端writer可返回原模板子类，必须在仅提取raw文本之前保住receiver。
        if (name == "format") return PyStringTemplate.Format(receiver, args, names, values);
        // partition未命中保source身份，命中保separator身份，不能提前剥离真实子类。
        if (name is "partition" or "rpartition") return PyTextPartition.Invoke(receiver, name, args, names);
        string text = PyStringLayout.RequireRaw(receiver);
        return PyOps.StringRootMethod(text, name, args, names, values, bound);
    }
}

public static partial class PyOps
{
    internal static object? StringRootMethod(string text, string name, object?[] args, string[] names, object?[] values, bool bound)
    {
        if (name is "startswith" or "endswith")
            return PyTextAffix.Match(text, name, args, names, qualified: !bound);
        // 仅准备本入口绑定错误，正文中的用户真假/迭代回调不被广catch。
        try
        {
            ValidatePreparedArity(text, name, args.Length);
            RejectPreparedKeywords(text, name, names);
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
        return StringMethod(text, name, args);
    }
    internal static object RepeatRootString(string text, object count)
    {
        int times = RepeatCount(text, count);
        return times == 0 || text.Length == 0 ? string.Empty
            : new System.Text.StringBuilder(text.Length * times).Insert(0, text, times).ToString();
    }
}
