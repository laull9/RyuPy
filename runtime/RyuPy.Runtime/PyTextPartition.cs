// str.partition/rpartition的窄绑定与首末ordinal搜索；保原对象身份，不预建码点表或复制输入。
using System;

namespace RyuPy.Runtime;

internal static class PyTextPartition
{
    internal static PyTuple Invoke(object receiver, string method, object?[] args, string[] names)
    {
        // 根描述符已先校验self；所有调用形态均先拒绝keyword，再检查唯一位置参数。
        if (names.Length != 0)
            throw Error(new PyTypeError($"str.{method}() takes no keyword arguments"));
        if (args.Length != 1)
            throw Error(new PyTypeError($"str.{method}() takes exactly one argument ({args.Length} given)"));
        object? separator = args[0];
        if (!PyStringLayout.TryGet(separator, out string part))
            throw Error(new PyTypeError($"must be str, not {PyRepr.TypeName(separator)}"));
        if (part.Length == 0) throw Error(new PyValueError("empty separator"));

        string text = PyStringLayout.RequireRaw(receiver);
        bool reverse = method == "rpartition";
        int index = Find(text, part, reverse);
        // CP未命中保source本身，包含真实str子类和空子类，不返回其CLR Value替身。
        if (index < 0)
            return reverse ? new PyTuple([string.Empty, string.Empty, receiver])
                : new PyTuple([receiver, string.Empty, string.Empty]);
        // 两侧是exact str；中项必须保separator原对象，即使它是真实str子类。
        return new PyTuple([text[..index], separator, text[(index + part.Length)..]]);
    }

    private static int Find(string text, string part, bool reverse)
    {
        if (part.Length > text.Length) return -1;
        if (!reverse)
        {
            int start = 0;
            while (start <= text.Length - part.Length)
            {
                int relative = text.AsSpan(start).IndexOf(part.AsSpan(), StringComparison.Ordinal);
                if (relative < 0) return -1;
                int index = start + relative;
                if (Boundary(text, index) && Boundary(text, index + part.Length)) return index;
                // 只跳过本次起点，不跳整个separator，保留与无效候选重叠的合法命中。
                start = index + 1;
            }
        }
        else
        {
            int end = text.Length;
            while (end >= part.Length)
            {
                int index = text.AsSpan(0, end).LastIndexOf(part.AsSpan(), StringComparison.Ordinal);
                if (index < 0) return -1;
                if (Boundary(text, index) && Boundary(text, index + part.Length)) return index;
                // i+n<=end且n>0，故新长度在[0,end)；保所有起点j<i的重叠候选。
                // 检查仍使用完整text，缩窗不得将正常代理对的半边伪装成孤立代理。
                end = index + part.Length - 1;
            }
        }
        return -1;
    }

    // 成功ordinal匹配后常数时间检查两端，不切开正常补充码点。
    // 两个独立代理码点拼接丢失来源的D009仍独立保留，本切不重写字符串表示。
    internal static bool Boundary(string text, int offset) => offset == 0 || offset == text.Length
        || !char.IsHighSurrogate(text[offset - 1]) || !char.IsLowSurrogate(text[offset]);

    // 只准备本层自造错误；真实str内容读取不调用__str__或其它用户协议。
    private static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
