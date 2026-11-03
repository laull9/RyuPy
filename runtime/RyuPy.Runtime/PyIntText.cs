// int 文本解析：固定 Unicode15 十进制数字、严格 underscore/前缀语法，二幂基数用线性位打包。
using System;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>str、bytes 和 bytearray 的严格整数文本解析。</summary>
public static class PyIntText
{
    // 从项目既有 CPython3.12.13 / Unicode15 Nd 表提取各十数字组零点，不依赖宿主分类版本。
    private static readonly int[] DecimalZeros = [
        0x30, 0x660, 0x6f0, 0x7c0, 0x966, 0x9e6, 0xa66, 0xae6, 0xb66, 0xbe6,
        0xc66, 0xce6, 0xd66, 0xde6, 0xe50, 0xed0, 0xf20, 0x1040, 0x1090, 0x17e0,
        0x1810, 0x1946, 0x19d0, 0x1a80, 0x1a90, 0x1b50, 0x1bb0, 0x1c40, 0x1c50, 0xa620,
        0xa8d0, 0xa900, 0xa9d0, 0xa9f0, 0xaa50, 0xabf0, 0xff10, 0x104a0, 0x10d30, 0x11066,
        0x110f0, 0x11136, 0x111d0, 0x112f0, 0x11450, 0x114d0, 0x11650, 0x116c0, 0x11730, 0x118e0,
        0x11950, 0x11c50, 0x11d50, 0x11da0, 0x11f50, 0x16a60, 0x16ac0, 0x16b50, 0x1d7ce, 0x1d7d8,
        0x1d7e2, 0x1d7ec, 0x1d7f6, 0x1e140, 0x1e2f0, 0x1e4f0, 0x1e950, 0x1fbf0,
    ];

    /// <summary>actual 文本解析共用冷入口，单参数和带 base 构造都记录原调用现场错误帧。</summary>
    public static object Parse(object original, int radix)
    {
        try { return ParseCore(original, radix); }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    private static object ParseCore(object original, int radix)
    {
        string text = original switch
        {
            string unicode => NormalizeUnicode(unicode),
            PyBytes bytes => Encoding.Latin1.GetString(bytes.Bytes),
            PyByteArray bytes => Encoding.Latin1.GetString(bytes.Bytes.ToArray()),
            _ => throw new PyTypeError("int() can't convert non-string with explicit base"),
        };
        if (radix != 0 && (radix < 2 || radix > 36))
            throw new PyValueError("int() base must be >= 2 and <= 36, or 0");
        int start = 0, end = text.Length;
        while (start < end && Space(text[start])) start++;
        while (end > start && Space(text[end - 1])) end--;
        bool negative = false;
        if (start < end && (text[start] is '+' or '-')) { negative = text[start] == '-'; start++; }
        if (start == end) throw Invalid(original, radix);
        bool zeroFirst = text[start] == '0', prefix = false;
        int effective = radix == 0 ? 10 : radix;
        if (zeroFirst && end - start >= 2)
        {
            int indicated = text[start + 1] switch { 'b' or 'B' => 2, 'o' or 'O' => 8, 'x' or 'X' => 16, _ => 0 };
            if (indicated != 0 && (radix == 0 || radix == indicated))
            {
                effective = indicated; prefix = true; start += 2;
                if (start < end && text[start] == '_') start++;
            }
        }
        int digits = 0, significant = 0, firstSignificant = -1, firstDigit = 0;
        bool previousDigit = false;
        for (int index = start; index < end; index++)
        {
            if (text[index] == '_')
            {
                if (!previousDigit) throw Invalid(original, radix);
                previousDigit = false; continue;
            }
            int digit = Digit(text[index]);
            if (digit < 0 || digit >= effective) throw Invalid(original, radix);
            previousDigit = true; digits++;
            if (firstSignificant < 0 && digit != 0) { firstSignificant = index; firstDigit = digit; }
            if (firstSignificant >= 0) significant++;
        }
        if (!previousDigit || digits == 0) throw Invalid(original, radix);
        if (radix == 0 && !prefix && zeroFirst && firstSignificant >= 0) throw Invalid(original, radix);
        bool powerOfTwo = (effective & (effective - 1)) == 0;
        // CP 默认限制也计算前导零，但不计算符号、空白、分隔下划线；配置API不在本切。
        if (!powerOfTwo && digits > 4300)
            throw new PyValueError($"Exceeds the limit (4300 digits) for integer string conversion: value has {digits} digits; use sys.set_int_max_str_digits() to increase the limit");
        if (firstSignificant < 0) return 0L;
        BigInteger result;
        if (powerOfTwo) result = Pack(text, firstSignificant, end, effective, significant, firstDigit);
        else
        {
            result = BigInteger.Zero;
            for (int index = firstSignificant; index < end; index++)
                if (text[index] != '_') result = result * effective + Digit(text[index]);
        }
        return PyInt.Normalize(negative ? -result : result);
    }

    private static BigInteger Pack(string text, int start, int end, int radix, int digits, int first)
    {
        int width = radix switch { 2 => 1, 4 => 2, 8 => 3, 16 => 4, 32 => 5, _ => throw new PyRuntimeError("invalid power-of-two radix") };
        int leadingBits = 0;
        for (int value = first; value != 0; value >>= 1) leadingBits++;
        long bits = checked((long)(digits - 1) * width + leadingBits);
        long bytes = checked((bits + 7) / 8);
        if (bytes > Array.MaxLength) throw new PyOverflowError("integer literal exceeds managed array capacity");
        var packed = new byte[(int)bytes];
        long position = 0;
        for (int index = end - 1; index >= start; index--)
        {
            if (text[index] == '_') continue;
            int digit = Digit(text[index]);
            int offset = (int)(position & 7), slot = (int)(position >> 3);
            packed[slot] |= (byte)(digit << offset);
            if (offset + width > 8 && slot + 1 < packed.Length) packed[slot + 1] |= (byte)(digit >> (8 - offset));
            position += width;
        }
        return new BigInteger(packed, isUnsigned: true, isBigEndian: false);
    }

    private static bool Space(char value) => value == ' ' || value is >= '\t' and <= '\r';
    private static int Digit(char value) => value is >= '0' and <= '9' ? value - '0'
        : value is >= 'a' and <= 'z' ? value - 'a' + 10 : value is >= 'A' and <= 'Z' ? value - 'A' + 10 : -1;
    private static PyValueError Invalid(object original, int radix) =>
        new($"invalid literal for int() with base {radix}: {PyRepr.Repr(original)}");

    private static string NormalizeUnicode(string text)
    {
        bool ascii = true;
        foreach (char value in text) if (value > 127) { ascii = false; break; }
        if (ascii) return text;
        var normalized = new char[text.Length]; int count = 0;
        for (int index = 0; index < text.Length; index++)
        {
            int scalar = text[index];
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                scalar = char.ConvertToUtf32(text[index], text[index + 1]); index++;
            }
            if (scalar < 128) { normalized[count++] = (char)scalar; continue; }
            int digit = Decimal(scalar);
            normalized[count++] = digit >= 0 ? (char)('0' + digit) : UnicodeSpace(scalar) ? ' ' : '?';
        }
        return new string(normalized, 0, count);
    }

    private static int Decimal(int scalar)
    {
        int low = 0, high = DecimalZeros.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2, zero = DecimalZeros[middle];
            if (scalar < zero) high = middle - 1;
            else if (scalar >= zero + 10) low = middle + 1;
            else return scalar - zero;
        }
        return -1;
    }
    private static bool UnicodeSpace(int scalar) => scalar is 0x85 or 0xa0 or 0x1680
        or >= 0x2000 and <= 0x200a or 0x2028 or 0x2029 or 0x202f or 0x205f or 0x3000;
}
