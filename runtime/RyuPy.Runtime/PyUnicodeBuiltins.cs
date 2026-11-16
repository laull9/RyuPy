using System;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>Unicode基础内建：码点/单字节ord与整数协议chr，错误统一为Python异常。</summary>
public static partial class PyOps
{
    private static long UnicodeOrd(object? value)
    {
        switch (value)
        {
            case string text:
                // BMP 与孤立代理项都是合法的单码点；合法代理对表示一个补充平面码点。
                if (text.Length == 1) return text[0];
                if (text.Length == 2 && char.IsSurrogatePair(text, 0))
                    return char.ConvertToUtf32(text, 0);
                throw OrdLength(PyText.Length(text));
            case PyStringInstance text:
                return UnicodeOrd(text.Value);
            case PyBytes bytes:
                if (bytes.Count == 1) return bytes.Bytes[0];
                throw OrdLength(bytes.Count);
            case PyByteArray bytes:
                if (bytes.Count == 1) return bytes.Bytes[0];
                throw OrdLength(bytes.Count);
            default:
                throw new PyTypeError($"ord() expected string of length 1, but {PyRepr.TypeName(value)} found");
        }
    }

    private static PyTypeError OrdLength(long length) =>
        new($"ord() expected a character, but string of length {length} found");

    private static string UnicodeChr(object? value)
    {
        if (!TryUnicodeInteger(value, out BigInteger number))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out object? indexed))
                throw new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer");
            // __index__ 只调用一次，结果必须本身是整数，不能继续递归转换或接受浮点数。
            if (!TryUnicodeInteger(indexed, out number))
                throw new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(indexed)})");
        }
        if (number < int.MinValue || number > int.MaxValue)
            throw new PyOverflowError("Python int too large to convert to C int");
        if (number < 0 || number > 0x10ffff)
            throw new PyValueError("chr() arg not in range(0x110000)");
        int codePoint = (int)number;
        // ConvertFromUtf32/Rune 会拒绝代理项，因此 BMP 直接构造一个 UTF-16 单元。
        return codePoint <= 0xffff ? new string((char)codePoint, 1) : char.ConvertFromUtf32(codePoint);
    }

    private static bool TryUnicodeInteger(object? value, out BigInteger number)
    {
        switch (value)
        {
            case bool boolean: number = boolean ? 1 : 0; return true;
            case int integer: number = integer; return true;
            case long integer: number = integer; return true;
            case BigInteger integer: number = integer; return true;
            default: number = default; return false;
        }
    }
}
