// str.translate：整数码点查表、LookupError保留原文、None删除；ASCII局部缓存保CP可观察回调次数。
// 继承既有D009：CLR字符串不能区分独立代理拼接与原生代理对；本切不改变字符串布局。
using System;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyTextTranslate
{
    internal static string Invoke(object receiver, object?[] args, string[] names)
    {
        if (names.Length != 0)
            throw Error(new PyTypeError("str.translate() takes no keyword arguments"));
        if (args.Length != 1)
            throw Error(new PyTypeError($"str.translate() takes exactly one argument ({args.Length} given)"));
        string text = PyStringLayout.RequireRaw(receiver);
        // 原生映射合同尚未接入外部包装值；明确拒绝，不隐式转换或部分冒充兼容。
        if (args[0] is IPyForeignObject)
            throw Error(new PyNotImplementedError("native str.translate does not support foreign tables yet"));
        var result = new StringBuilder(text.Length);
        int offset = 0;
        // CP仅对完整ASCII输入开启缓存。遇扩展字符串/非ASCII替换时在原位置重新查表，
        // 随后逐码点处理；缓存仅属于本次调用，不隐藏下次调用或通用路径的用户副作用。
        bool ascii = true;
        foreach (char character in text)
            if (character > 127) { ascii = false; break; }
        if (ascii)
        {
            Span<int> cache = stackalloc int[128];
            cache.Fill(-2); // -2未知，-1删除，其余是ASCII替换。
            while (offset < text.Length)
            {
                int code = text[offset];
                int mapped = cache[code];
                if (mapped == -2)
                {
                    object? value = Lookup(args[0], code);
                    if (value is null or PyNone) mapped = -1;
                    else if (PyInt.IsInt(value))
                    {
                        int ordinal = CheckedOrdinal(value);
                        if (ordinal > 127) break;
                        mapped = ordinal;
                    }
                    else if (PyStringLayout.TryGet(value, out string replacement))
                    {
                        if (replacement.Length != 1 || replacement[0] > 127) break;
                        mapped = replacement[0];
                    }
                    else throw InvalidMapping();
                    cache[code] = mapped;
                }
                if (mapped >= 0) result.Append((char)mapped);
                offset++;
            }
        }
        while (offset < text.Length)
        {
            string character = PyText.ReadAt(text, ref offset);
            int code = character.Length == 2 ? char.ConvertToUtf32(character, 0) : character[0];
            object? value = Lookup(args[0], code);
            if (value is null or PyNone)
            {
                // CP通用路径合并连续删除段，并预查首个非删除值；该位置随后重查。
                // 预查仍验证返回类型/范围，不能将用户错误推迟或吞掉。
                while (offset < text.Length)
                {
                    int next = offset;
                    string following = PyText.ReadAt(text, ref next);
                    int nextCode = following.Length == 2 ? char.ConvertToUtf32(following, 0) : following[0];
                    object? probe = Lookup(args[0], nextCode);
                    if (probe is null or PyNone) { offset = next; continue; }
                    if (PyInt.IsInt(probe)) _ = CheckedOrdinal(probe);
                    else if (!PyStringLayout.TryGet(probe, out _)) throw InvalidMapping();
                    break;
                }
                continue;
            }
            if (PyInt.IsInt(value))
            {
                int ordinal = CheckedOrdinal(value);
                // CLR ConvertFromUtf32拒绝孤立代理；Python允许其作为独立码点。
                if (ordinal <= 0xFFFF) result.Append((char)ordinal);
                else result.Append(char.ConvertFromUtf32(ordinal));
            }
            else if (PyStringLayout.TryGet(value, out string replacement)) result.Append(replacement);
            else throw InvalidMapping();
        }
        return result.ToString();
    }

    private static object? Lookup(object? table, int code)
    {
        long depth = PyTraceback.CaptureDepth();
        try
        {
            object? value = PyOps.GetItem(table, (long)code);
            if (value is IPyForeignObject)
                throw Error(new PyNotImplementedError("native str.translate does not support foreign mapping values yet"));
            return value;
        }
        // 按真实Python继承链匹配，包含用户LookupError子类；不吞掉其它回调异常。
        catch (PyBaseException error) when (PyExceptionMatch.Match(error, PyType.Get("LookupError")))
        {
            // 仅吞异常时恢复回调栈，不触碰外层活动异常及真正传播错误的帧。
            PyTraceback.PopTo(depth + 1);
            return (long)code;
        }
    }

    private static int CheckedOrdinal(object value)
    {
        var ordinal = PyInt.ToBigInteger(value);
        if (ordinal < 0 || ordinal > 0x10FFFF)
            throw Error(new PyValueError("character mapping must be in range(0x110000)"));
        return (int)ordinal;
    }

    private static PyTypeError InvalidMapping() =>
        Error(new PyTypeError("character mapping must return integer, None or str"));

    // 只准备本层创建的异常，不改用户映射抛出的身份/上下文。
    private static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
