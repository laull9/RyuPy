// 文本按Python码点截断/填充，整数保精确BigInteger；无变换文本返回原对象而非剥离子类。
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

internal static partial class PyBuiltinFormat
{
    private static object Text(object original, string text, Spec spec)
    {
        Validate(spec, "str");
        int width = Width(spec), offset = 0, length = 0;
        while (offset < text.Length && (spec.Precision < 0 || length < spec.Precision))
        { offset += Unit(text, offset); length++; }
        bool truncated = offset < text.Length;
        string body = truncated ? text[..offset] : text;
        int padding = Math.Max(0, width - length);
        if (!truncated && padding == 0) return original;
        string fill = spec.Zero && !spec.ExplicitFill ? "0" : spec.Fill;
        char align = spec.Align == '\0' ? '<' : spec.Align;
        return Pad(body, padding, fill, align);
    }

    private static string Integer(BigInteger value, Spec spec, string typeName)
    {
        Validate(spec, typeName);
        if (spec.Group != '\0' || spec.Type is "c" or "n"
            || (spec.Type.Length == 1 && "eEfFgG%".Contains(spec.Type[0])))
            throw Own(new PyNotImplementedError("format: this integer specification is not supported yet"));
        int width = Width(spec);
        string type = spec.Type.Length == 0 ? "d" : spec.Type;
        BigInteger magnitude = BigInteger.Abs(value);
        string digits = type == "d" ? DecimalDigits(magnitude)
            : BaseDigits(magnitude, type == "b" ? 2 : type == "o" ? 8 : 16, type == "X");
        string sign = value.Sign < 0 ? "-" : spec.Sign == '+' ? "+" : spec.Sign == ' ' ? " " : "";
        string prefix = spec.Alternate ? type switch { "b" => "0b", "o" => "0o", "x" => "0x", "X" => "0X", _ => "" } : "";
        char align = spec.Align == '\0' ? (spec.Zero ? '=' : '>') : spec.Align;
        string fill = spec.Zero && !spec.ExplicitFill ? "0" : spec.Fill;
        int padding = Math.Max(0, width - sign.Length - prefix.Length - digits.Length);
        Capacity((long)sign.Length + prefix.Length + digits.Length + (long)fill.Length * padding);
        if (align == '=')
        {
            return sign + prefix + Repeat(fill, padding) + digits;
        }
        return Pad(sign + prefix + digits, padding, fill, align);
    }

    private static string DecimalDigits(BigInteger magnitude)
    {
        // 固定CP默认4300位政策与已有int文本构造一致；配置API尚未支持。
        // 10^4300的bit length为14285；更大的bit length无需先分配巨大十进制串。
        const string message = "Exceeds the limit (4300 digits) for integer string conversion; use sys.set_int_max_str_digits() to increase the limit";
        if (magnitude.GetBitLength() > 14285) throw Own(new PyValueError(message));
        string digits = magnitude.ToString(CultureInfo.InvariantCulture);
        if (digits.Length > 4300) throw Own(new PyValueError(message));
        return digits;
    }

    private static string BaseDigits(BigInteger value, int radix, bool upper)
    {
        if (value.IsZero) return "0";
        string alphabet = upper ? "0123456789ABCDEF" : "0123456789abcdef";
        int bits = radix == 2 ? 1 : radix == 8 ? 3 : 4;
        long count = (value.GetBitLength() + bits - 1) / bits;
        Capacity(count);
        // 二幂基数按无符号字节直接取位；不逐digit做BigInteger除法和复制。
        byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        return string.Create((int)count, (bytes, bits, alphabet), static (output, state) =>
        {
            for (int i = 0; i < output.Length; i++)
            {
                long offset = (long)(output.Length - 1 - i) * state.bits;
                int index = (int)(offset / 8), shift = (int)(offset % 8);
                int pair = state.bytes[index];
                if (index + 1 < state.bytes.Length) pair |= state.bytes[index + 1] << 8;
                output[i] = state.alphabet[(pair >> shift) & ((1 << state.bits) - 1)];
            }
        });
    }

    private static string Pad(string body, int padding, string fill, char align)
    {
        if (padding == 0) return body;
        Capacity((long)body.Length + (long)fill.Length * padding);
        if (align == '<') return body + Repeat(fill, padding);
        if (align == '^')
        {
            // format居中把奇数余量放右侧；这不同于str.center的独立规则。
            int left = padding / 2;
            return Repeat(fill, left) + body + Repeat(fill, padding - left);
        }
        return Repeat(fill, padding) + body;
    }

    private static string Repeat(string fill, int count)
    {
        if (count == 0) return string.Empty;
        if (fill.Length == 1) return new string(fill[0], count);
        var result = new StringBuilder(fill.Length * count);
        for (int i = 0; i < count; i++) result.Append(fill);
        return result.ToString();
    }
}
